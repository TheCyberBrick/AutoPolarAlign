using System;
using System.Collections.Generic;

namespace AutoPolarAlign
{
    public class AutoPolarAlignment
    {
        private readonly IPolarAlignmentMount mount;

        private readonly IPolarAlignmentSolver solver;

        public Axis Altitude { get; } = new Axis();
        public Axis Azimuth { get; } = new Axis();

        protected readonly Settings settings;

        public AutoPolarAlignment(IPolarAlignmentMount mount, IPolarAlignmentSolver solver, Settings settings)
        {
            this.mount = mount;
            this.solver = solver;
            this.settings = settings;

            Altitude.Limit = settings.AltitudeLimit;
            Altitude.BacklashCompensation = settings.AltitudeBacklash;
            Altitude.CalibrationDistance = settings.AltitudeCalibrationDistance;

            Azimuth.Limit = settings.AzimuthLimit;
            Azimuth.BacklashCompensation = settings.AzimuthBacklash;
            Azimuth.CalibrationDistance = settings.AzimuthCalibrationDistance;
        }

        public bool Run()
        {
            if (!Calibrate())
            {
                throw new Exception("Calibration failed");
            }

            if (settings.StartAtLowAltitude && !PositionAxisToSide(Altitude, -Math.Max(Altitude.BacklashCompensation, settings.AltitudeBacklash)))
            {
                throw new Exception("Failed positioning altitude below pole");
            }

            return Align();
        }

        public bool Align()
        {
            Vec2 previousCorrection = new Vec2();

            for (int i = 0; i < settings.MaxAlignmentIterations; ++i)
            {
                double aggressiveness = settings.MaxAlignmentIterations > 1 ? (settings.EndAggressiveness + (settings.StartAggressiveness - settings.EndAggressiveness) / (settings.MaxAlignmentIterations - 1) * (settings.MaxAlignmentIterations - 1 - i)) : settings.EndAggressiveness;

                var correction = EstimateCorrection();

                if (i > 0)
                {
                    // If new correction is in opposite direction then it overshot
                    // which means that the backlash compensation is too large, so
                    // it must be reduced by the overshot amount

                    if (settings.AltitudeBacklashCalibration && Math.Sign(previousCorrection.Altitude) != Math.Sign(correction.Altitude))
                    {
                        Altitude.BacklashCompensation = Math.Max(0, Altitude.BacklashCompensation - Math.Abs(correction.Altitude));
                    }

                    if (settings.AzimuthBacklashCalibration && Math.Sign(previousCorrection.Azimuth) != Math.Sign(correction.Azimuth))
                    {
                        Azimuth.BacklashCompensation = Math.Max(0, Azimuth.BacklashCompensation - Math.Abs(correction.Azimuth));
                    }
                }

                previousCorrection = correction;

                Move(correction, aggressiveness: aggressiveness);

                if (correction.Length < settings.AlignmentThreshold)
                {
                    return true;
                }
            }

            if (settings.AcceptBestEffort)
            {
                return true;
            }

            return EstimateCorrection().Length < settings.AlignmentThreshold;
        }

        public bool Calibrate()
        {
            return CalibrateAltitude() && CalibrateAzimuth();
        }

        private Vec2 MeasureCurrentOffset()
        {
            var offset = new Vec2();
            for (int i = 0; i < settings.SamplesPerMeasurement; ++i)
            {
                solver.Solve();
                offset += solver.AlignmentOffset;
            }
            offset /= settings.SamplesPerMeasurement;
            return offset;
        }

        private bool CalibrateAltitude()
        {
            // Reverse altitude calibration dir to later help
            // StartAtLowAltitude if enabled
            return CalibrateAxis(Altitude, settings.AltitudeBacklashCalibration, reverse: true);
        }

        private bool CalibrateAzimuth()
        {
            return CalibrateAxis(Azimuth, settings.AzimuthBacklashCalibration);
        }

        private bool CalibrateAxis(Axis axis, bool calibrateBacklash, bool reverse = false)
        {
            double calibrationDir = reverse ? -1 : 1;
            double calibrationDistance = axis.CalibrationDistance;

            // Remove any backlash before calibration
            MoveAxisWithCompensation(axis, axis.BacklashCompensation * calibrationDir, backlashCompensationPercent: 0.0f);

            // Estimate origin
            var startOffset = MeasureCurrentOffset();

            Vec2 dir;
            Vec2 endOffset;
            double dst;

            if (settings.SamplesPerCalibration > 1)
            {
                // Take samples to estimate axis direction and scale
                double stepDistance = calibrationDistance / settings.SamplesPerCalibration * calibrationDir;
                var samples = new List<Vec2>();
                for (int i = 0; i < settings.SamplesPerCalibration; ++i)
                {
                    MoveAxisWithCompensation(axis, stepDistance);
                    samples.Add(MeasureCurrentOffset());
                }
                endOffset = samples[samples.Count - 1];

                // Try to find a linear fit for samples
                if (!LinearFit(samples, out dir, out var center))
                {
                    return false;
                }

                // Calculate average of extrapolated end position
                Vec2 avgEndOffset = new Vec2();
                for (int i = 0; i < samples.Count; ++i)
                {
                    var sample = samples[i];
                    var projection = center + dir * dir.Dot(sample - center);
                    avgEndOffset += (projection - startOffset) / (i + 1) * samples.Count;
                }
                avgEndOffset /= samples.Count;
                avgEndOffset += startOffset;
                dst = (avgEndOffset - startOffset).Length;
            }
            else
            {
                MoveAxisWithCompensation(axis, calibrationDistance * calibrationDir);
                endOffset = MeasureCurrentOffset();
                dir = (startOffset - endOffset).Normalized();
                dst = (endOffset - startOffset).Length;
            }

            // Set calibration value so that the move
            // amount can be calculated given an alignment
            // offset
            axis.CalibratedMagnitude = calibrationDistance / dst;

            // Flip if it points in opposite direction
            if (dir.Dot(endOffset - startOffset) < 0)
            {
                axis.CalibratedDirection = -dir * calibrationDir;
            }
            else
            {
                axis.CalibratedDirection = dir * calibrationDir;
            }

            if (calibrateBacklash)
            {
                var expectedMoveDistance = axis.BacklashCompensation + calibrationDistance;

                MoveAxisWithCompensation(axis, -expectedMoveDistance * calibrationDir, backlashCompensationPercent: 0.0f);

                var newEndOffset = MeasureCurrentOffset();

                var actualMoveDistance = -axis.CalibratedDirection.Dot(newEndOffset - endOffset) * calibrationDir;

                if (actualMoveDistance <= 0)
                {
                    // Axis moved in wrong direction
                    return false;
                }

                axis.BacklashCompensation = (expectedMoveDistance - actualMoveDistance * axis.CalibratedMagnitude) * 0.99;
            }

            return true;
        }

        protected bool LinearFit(List<Vec2> positions, out Vec2 dir, out Vec2 center)
        {
            dir = new Vec2();
            center = new Vec2();

            int n = positions.Count;
            if (n < 2)
            {
                return false;
            }

            double x = 0;
            double y = 0;
            double x2 = 0;
            double y2 = 0;
            double xy = 0;

            foreach (var position in positions)
            {
                x += position.X;
                y += position.Y;
                x2 += position.X * position.X;
                y2 += position.Y * position.Y;
                xy += position.X * position.Y;
            }

            x /= n;
            y /= n;
            x2 /= n;
            y2 /= n;
            xy /= n;

            center = new Vec2(x, y);

            double dy = xy - x * y;
            double dx;

            double xx = x2 - x * x;
            double yy = y2 - y * y;
            if (Math.Abs(xx) < Math.Abs(yy))
            {
                dx = -dy;
                dy = -yy;
            }
            else
            {
                dx = xx;
            }

            dir = new Vec2(dx, dy);
            if (Math.Abs(dir.Length) > 1e-6)
            {
                dir /= dir.Length;
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool PositionAxisToSide(Axis axis, double margin)
        {
            int i = 0;
            while (true)
            {
                var axisOffset = axis.CalibratedDirection.Dot(MeasureCurrentOffset()) * axis.CalibratedMagnitude;

                if (margin < 0 && axisOffset <= margin)
                {
                    break;
                }
                else if (margin >= 0 && axisOffset >= margin)
                {
                    break;
                }

                if (i++ > settings.MaxPositioningAttempts + 1)
                {
                    return false;
                }

                MoveAxisWithCompensation(axis, margin * 1.1 - axisOffset);
            }

            return true;
        }

        protected Vec2 AlignmentOffsetToAltAzOffset(Vec2 offset)
        {
            double alt = Altitude.CalibratedDirection.Dot(offset) * Altitude.CalibratedMagnitude;
            double az = Azimuth.CalibratedDirection.Dot(offset) * Azimuth.CalibratedMagnitude;
            return new Vec2(az, alt);
        }

        protected void MoveAxisWithCompensation(Axis axis, double amount, double backlashCompensationPercent = 1.0)
        {
            if (axis.Move(axis.EstimateCompensatedMove(amount, backlashCompensationPercent), out amount))
            {
                if (axis == Altitude)
                {
                    mount.MoveAltitude(amount);
                }
                else if (axis == Azimuth)
                {
                    mount.MoveAzimuth(amount);
                }
            }
            else
            {
                if (axis == Altitude)
                {
                    throw new Exception("Altitude limit reached");
                }
                else if (axis == Azimuth)
                {
                    throw new Exception("Azimuth limit reached");
                }
            }
        }

        protected Vec2 EstimateCorrection()
        {
            var offset = MeasureCurrentOffset();
            return AlignmentOffsetToAltAzOffset(-offset);
        }

        public virtual void Move(Vec2 correction, double aggressiveness = 1.0, double backlashCompensationPercent = 1.0)
        {
            MoveAxisWithCompensation(Altitude, correction.Altitude * aggressiveness, backlashCompensationPercent);
            MoveAxisWithCompensation(Azimuth, correction.Azimuth * aggressiveness, backlashCompensationPercent);
        }
    }
}
