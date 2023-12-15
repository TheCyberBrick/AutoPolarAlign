using System;

namespace AutoPolarAlign
{
    public class Axis
    {
        private double _position;
        public double Position
        {
            get => _position;
            set
            {
                var offset = value - _position;
                MoveBacklash(offset);
                _position = value;
                LastDirection = Math.Sign(offset);
            }
        }

        public double Limit { get; set; } = double.MaxValue;

        private double _backlashCompensation;
        public double BacklashCompensation
        {
            get => _backlashCompensation;
            set
            {
                _backlashCompensation = value;
                ConstrainEstimatedBacklash();
            }
        }

        public double CalibrationDistance { get; set; }

        public Vec2 CalibratedDirection { get; set; }

        public double CalibratedMagnitude { get; set; }

        public double EstimatedBacklash { get; private set; }

        public int LastDirection { get; private set; }

        public string Name { get; }

        public Axis(string name)
        {
            Name = name;
        }

        public void Reset(double position = 0.0f)
        {
            Position = position;
            EstimatedBacklash = 0.0f;
            LastDirection = 0;
        }

        public void ClearBacklash(int direction)
        {
            if (direction > 0)
            {
                EstimatedBacklash = BacklashCompensation * 0.5;
            }
            else if (direction < 0)
            {
                EstimatedBacklash = -BacklashCompensation * 0.5;
            }
        }

        public double EstimateCompensatedMove(double amount, double backlashCompensationPercent = 1.0)
        {
            if (Math.Sign(amount) != Math.Sign(EstimatedBacklash))
            {
                amount += Math.Sign(amount) * (BacklashCompensation * 0.5 + Math.Abs(EstimatedBacklash)) * backlashCompensationPercent;
            }
            else
            {
                amount += Math.Sign(amount) * (BacklashCompensation * 0.5 - Math.Abs(EstimatedBacklash)) * backlashCompensationPercent;
            }

            return amount;
        }

        public bool Move(double amount, out double movedAmount)
        {
            movedAmount = 0;

            if (amount > 0)
            {
                if (Position >= Limit - double.Epsilon)
                {
                    return false;
                }
                else if (Position + amount >= Limit)
                {
                    movedAmount = Limit - Position;
                    Position = Limit;
                    return true;
                }
            }
            else
            {
                if (Position <= -Limit + double.Epsilon)
                {
                    return false;
                }
                else if (Position + amount <= -Limit)
                {
                    movedAmount = -Limit - Position;
                    Position = -Limit;
                    return true;
                }
            }

            movedAmount = amount;
            Position += amount;
            return true;
        }

        private void MoveBacklash(double amount)
        {
            EstimatedBacklash += amount;
            ConstrainEstimatedBacklash();
        }

        private void ConstrainEstimatedBacklash()
        {
            if (EstimatedBacklash > BacklashCompensation * 0.5)
            {
                ClearBacklash(1);
            }
            else if (EstimatedBacklash < -BacklashCompensation * 0.5)
            {
                ClearBacklash(-1);
            }
        }
    }
}
