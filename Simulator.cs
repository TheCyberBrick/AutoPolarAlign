using System;

namespace AutoPolarAlign
{
    public class Simulator : IPolarAlignmentSolver, IPolarAlignmentMount
    {
        private Vec2 _trueAlignmentOffset;
        public Vec2 TrueAlignmentOffset
        {
            get => _trueAlignmentOffset;
            set
            {
                _trueAlignmentOffset = value;
            }
        }

        private Vec2? _alignmentOffset;
        public Vec2 AlignmentOffset
        {
            get
            {
                if (!_alignmentOffset.HasValue)
                {
                    throw new Exception("Not yet solved");
                }
                return _alignmentOffset.Value;
            }
            private set => _alignmentOffset = value;
        }

        private readonly Random rng;

        private readonly double offsetJitter;
        private readonly double moveJitter;

        private readonly double altBacklash;
        private readonly double azBacklash;

        public double ActualAltBacklash { get; private set; }
        public double ActualAzBacklash { get; private set; }

        public Vec2 TrueAltAxis { get; }
        public Vec2 TrueAzAxis { get; }


        private bool connected;

        public Simulator(Random rng, Vec2 initialAlignmentOffset, Vec2 altAxis, Vec2 azAxis, double offsetJitter, double moveJitter, double altBacklash, double azBacklash, double initialAltBacklash, double initialAzBacklash)
        {
            this.rng = rng;

            this.offsetJitter = offsetJitter;
            this.moveJitter = moveJitter;

            this.altBacklash = altBacklash;
            this.azBacklash = azBacklash;

            ActualAltBacklash = initialAltBacklash;
            ActualAzBacklash = initialAzBacklash;

            TrueAltAxis = altAxis;
            TrueAzAxis = azAxis;

            TrueAlignmentOffset = initialAlignmentOffset;
        }

        public void Connect()
        {
            connected = true;
        }

        public void Disconnect()
        {
            connected = false;
        }

        public void Dispose()
        {
            Disconnect();
        }

        private void CheckConnected()
        {
            if (!connected)
            {
                throw new Exception("Not connected");
            }
        }

        public void Solve()
        {
            CheckConnected();

            AlignmentOffset = TrueAlignmentOffset + new Vec2((float)(rng.NextDouble() - 0.5) * offsetJitter, (float)(rng.NextDouble() - 0.5) * offsetJitter);
        }

        public void MoveAltitude(double amount)
        {
            CheckConnected();

            ActualAltBacklash += amount;
            if (ActualAltBacklash > altBacklash * 0.5f)
            {
                amount = Math.Sign(amount) * (ActualAltBacklash - altBacklash * 0.5f);
                ActualAltBacklash = altBacklash * 0.5f;
            }
            else if (ActualAltBacklash < -altBacklash * 0.5f)
            {
                amount = Math.Sign(amount) * (-altBacklash * 0.5f - ActualAltBacklash);
                ActualAltBacklash = -altBacklash * 0.5f;
            }
            else
            {
                amount = 0;
            }

            TrueAlignmentOffset += TrueAltAxis * (amount + (float)(rng.NextDouble() - 0.5) * moveJitter / TrueAltAxis.Length);
        }

        public void MoveAzimuth(double amount)
        {
            CheckConnected();

            ActualAzBacklash += amount;
            if (ActualAzBacklash > azBacklash * 0.5f)
            {
                amount = Math.Sign(amount) * (ActualAzBacklash - azBacklash * 0.5f);
                ActualAzBacklash = azBacklash * 0.5f;
            }
            else if (ActualAzBacklash < -azBacklash * 0.5f)
            {
                amount = Math.Sign(amount) * (-azBacklash * 0.5f - ActualAzBacklash);
                ActualAzBacklash = -azBacklash * 0.5f;
            }
            else
            {
                amount = 0;
            }

            TrueAlignmentOffset += TrueAzAxis * (amount + (float)(rng.NextDouble() - 0.5) * moveJitter / TrueAzAxis.Length);
        }

        public void StopAltitude()
        {
            CheckConnected();
        }

        public void StopAzimuth()
        {
            CheckConnected();
        }
    }
}
