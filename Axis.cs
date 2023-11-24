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
                MoveBacklash(value - _position);
                _position = value;
            }
        }

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

        public void Reset(double position = 0.0f)
        {
            Position = position;
            EstimatedBacklash = 0.0f;
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

        public void Move(double amount)
        {
            Position += amount;
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
                EstimatedBacklash = BacklashCompensation * 0.5;
            }
            else if (EstimatedBacklash < -BacklashCompensation * 0.5)
            {
                EstimatedBacklash = -BacklashCompensation * 0.5;
            }
        }
    }
}
