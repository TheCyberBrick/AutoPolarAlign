using ASCOM.DriverAccess;
using System.Globalization;
using System;
using System.Threading;

namespace AutoPolarAlign
{
    public class StarGoMount : IPolarAlignmentMount
    {
        public int Aux1Steps { get; set; } = 380;

        public int Aux2Steps { get; set; } = 570;

        public int Aux1Speed { get; set; } = 9;

        public int Aux2Speed { get; set; } = 9;

        public bool SetAuxSpeed { get; set; } = true;

        public bool ReverseAltitude { get; set; } = false;

        public bool ReverseAzimuth { get; set; } = false;

        public double SettlingTime { get; set; } = 1.0;


        private Telescope telescope;

        public void Connect()
        {
            try
            {
                if (telescope == null)
                {
                    telescope = new Telescope("ASCOM.AvalonStarGo.NET.Telescope");
                }
                telescope.Connected = true;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                throw new Exception("Failed connecting to StarGo", ex);
            }
        }

        public void Disconnect()
        {
            if (telescope != null)
            {
                telescope.Connected = false;
            }
        }

        public void Dispose()
        {
            telescope?.Dispose();
        }

        private double EstimateDurationForMove(double amount, int auxsteps, int auxspeed)
        {
            int stepsPerSecond;
            switch (auxspeed)
            {
                case 10:
                    stepsPerSecond = 2000;
                    break;
                case 9:
                    stepsPerSecond = 1200;
                    break;
                case 8:
                    stepsPerSecond = 500;
                    break;
                default:
                    stepsPerSecond = 250;
                    break;
            }

            int totalSteps = (int)Math.Ceiling(Math.Abs(amount * auxsteps));

            return totalSteps / (double)stepsPerSecond + 1 /* Compensate ramp up time */ + SettlingTime;
        }

        private void SendSetSpeedAltitude(int auxspeed)
        {
            string cmd;
            switch (auxspeed)
            {
                case 10:
                    cmd = ":X1D0060*50#";
                    break;
                case 9:
                    cmd = ":X1D0100*40#";
                    break;
                case 8:
                    cmd = ":X1D0250*30#";
                    break;
                case 7:
                    cmd = ":X1D0500*20#";
                    break;
                case 6:
                    cmd = ":X1D0750*10#";
                    break;
                case 5:
                    cmd = ":X1D1000*05#";
                    break;
                case 4:
                    cmd = ":X1D2500*01#";
                    break;
                case 3:
                    cmd = ":X1D4000*01#";
                    break;
                case 2:
                    cmd = ":X1D6000*01#";
                    break;
                default:
                    cmd = ":X1D9000*01#";
                    break;
            }
            SendCommand(cmd);
        }

        private void SendSyncAltitude()
        {
            SendCommand(":X11500001#");
        }

        private double SendPositionAltitude(double pos)
        {
            double steps = pos * Aux2Steps;

            if (Math.Abs(steps) > 99998)
            {
                steps = Math.Sign(steps) * 99998;
                pos -= steps / Aux2Steps;
            }
            else
            {
                pos = 0;
            }

            if (ReverseAltitude)
            {
                steps = -steps;
            }

            if (steps > 0)
            {
                SendCommand(string.Format(CultureInfo.InvariantCulture, ":X175{0:D5}#", (int)Math.Ceiling(steps)));
            }
            else
            {
                SendCommand(string.Format(CultureInfo.InvariantCulture, ":X174{0:D5}#", (int)Math.Floor(100000 + steps)));
            }

            return pos;
        }

        public void MoveAltitude(double amount)
        {
            while (Math.Abs(amount) > double.Epsilon)
            {
                SendSyncAltitude();

                if (SetAuxSpeed)
                {
                    SendSetSpeedAltitude(Aux2Speed);
                }

                double startAmount = amount;

                amount = SendPositionAltitude(amount);

                double moveAmount = amount - startAmount;

                Thread.Sleep(TimeSpan.FromSeconds(EstimateDurationForMove(moveAmount, Aux2Steps, Aux2Speed)));

                StopAltitude();
            }
        }

        public void StopAltitude()
        {
            SendCommand(":X0FAUX2ST#");
        }

        private void SendSetSpeedAzimuth(int auxspeed)
        {
            string cmd;
            switch (auxspeed)
            {
                case 10:
                    cmd = ":X1C0060*50#";
                    break;
                case 9:
                    cmd = ":X1C0100*40#";
                    break;
                case 8:
                    cmd = ":X1C0250*30#";
                    break;
                case 7:
                    cmd = ":X1C0500*20#";
                    break;
                case 6:
                    cmd = ":X1C0750*10#";
                    break;
                case 5:
                    cmd = ":X1C1000*05#";
                    break;
                case 4:
                    cmd = ":X1C2500*01#";
                    break;
                case 3:
                    cmd = ":X1C4000*01#";
                    break;
                case 2:
                    cmd = ":X1C6000*01#";
                    break;
                default:
                    cmd = ":X1C9000*01#";
                    break;
            }
            SendCommand(cmd);
        }

        private void SendSyncAzimuth()
        {
            SendCommand(":X0C500001#");
        }

        private double SendPositionAzimuth(double pos)
        {
            double steps = pos * Aux1Steps;

            if (Math.Abs(steps) > 99998)
            {
                steps = Math.Sign(steps) * 99998;
                pos -= steps / Aux1Steps;
            }
            else
            {
                pos = 0;
            }

            if (ReverseAzimuth)
            {
                steps = -steps;
            }

            if (steps > 0)
            {
                SendCommand(string.Format(CultureInfo.InvariantCulture, ":X165{0:D5}#", (int)Math.Ceiling(steps)));
            }
            else
            {
                SendCommand(string.Format(CultureInfo.InvariantCulture, ":X164{0:D5}#", (int)Math.Floor(100000 + steps)));
            }

            return pos;
        }

        public void MoveAzimuth(double amount)
        {
            while (Math.Abs(amount) > double.Epsilon)
            {
                SendSyncAzimuth();

                if (SetAuxSpeed)
                {
                    SendSetSpeedAzimuth(Aux1Speed);
                }

                double startAmount = amount;

                amount = SendPositionAzimuth(amount);

                double moveAmount = amount - startAmount;

                Thread.Sleep(TimeSpan.FromSeconds(EstimateDurationForMove(moveAmount, Aux1Steps, Aux1Speed)));

                StopAzimuth();
            }
        }

        public void StopAzimuth()
        {
            SendCommand(":X0AAUX1ST#");
        }

        private void SendCommand(string command)
        {
            try
            {
                telescope.CommandString(command, false);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed sending command to mount", ex);
            }
        }
    }
}
