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

        private readonly Settings settings;

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

            return Align();
        }

        public bool Align()
        {
            for (int i = 0; i < settings.MaxAlignmentIterations; ++i)
            {
                double aggressiveness = settings.MaxAlignmentIterations > 1 ? (settings.EndAggressiveness + (settings.StartAggressiveness - settings.EndAggressiveness) / (settings.MaxAlignmentIterations - 1) * (settings.MaxAlignmentIterations - 1 - i)) : settings.EndAggressiveness;
                if (!AlignOnce(settings.AlignmentThreshold, aggressiveness))
                {
                    return true;
                }
            }

            if (settings.AcceptBestEffort)
            {
                return true;
            }

            var correction = EstimateCorrection();

            return correction.Length < settings.AlignmentThreshold;
        }

        public bool Calibrate()
        {
            return CalibrateAltitude() && CalibrateAzimuth();
        }

        public bool CalibrateAltitude()
        {
            return Calibrate(Altitude);
        }

        public bool CalibrateAzimuth()
        {
            return Calibrate(Azimuth);
        }

        private bool Calibrate(Axis axis)
        {
            // Remove any backlash before calibration
            MoveAxisWithCompensation(axis, 1.25f * axis.BacklashCompensation, backlashCompensationPercent: 0.0f);

            // Estimate origin
            var startPosition = new Vec2();
            for (int i = 0; i < settings.SamplesPerMeasurement; ++i)
            {
                solver.Solve();
                startPosition += solver.AlignmentOffset;
            }
            startPosition /= settings.SamplesPerMeasurement;

            // Take samples to estimate axis direction and scale
            double calibrationDistance = axis.CalibrationDistance;
            double stepDistance = calibrationDistance / settings.SamplesPerCalibration;
            var samples = new List<Vec2>();
            for (int i = 0; i < settings.SamplesPerCalibration; ++i)
            {
                MoveAxisWithCompensation(axis, stepDistance);
                var offset = new Vec2();
                for (int j = 0; j < settings.SamplesPerMeasurement; ++j)
                {
                    solver.Solve();
                    offset += solver.AlignmentOffset;
                }
                samples.Add(offset / settings.SamplesPerMeasurement);
            }

            Vec2 dir;
            Vec2 endPosition;

            if (settings.SamplesPerCalibration > 1)
            {
                // Try to find a linear fit for samples
                if (!LinearFit(samples, out dir, out var center))
                {
                    return false;
                }

                // Calculate average of extrapolated end position
                endPosition = new Vec2();
                for (int i = 0; i < samples.Count; ++i)
                {
                    var sample = samples[i];
                    var projection = center + dir * dir.Dot(sample - center);
                    endPosition += (projection - startPosition) / (i + 1) * samples.Count;
                }
                endPosition /= samples.Count;
                endPosition += startPosition;
            }
            else
            {
                endPosition = samples[samples.Count - 1];
                dir = (startPosition - endPosition).Normalized();
            }

            // Set calibration value so that the move
            // amount can be calculated given an alignment
            // offset
            double dst = (endPosition - startPosition).Length;
            axis.CalibratedMagnitude = calibrationDistance / dst;
            axis.CalibratedDirection = dir;

            // Flip if it points in opposite direction
            if (axis.CalibratedDirection.Dot(endPosition - startPosition) < 0)
            {
                axis.CalibratedDirection *= -1;
            }

            return true;
        }

        private bool LinearFit(List<Vec2> positions, out Vec2 dir, out Vec2 center)
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

        private Vec2 AlignmentOffsetToAltAzOffset(Vec2 offset)
        {
            double alt = Altitude.CalibratedDirection.Dot(offset) * Altitude.CalibratedMagnitude;
            double az = Azimuth.CalibratedDirection.Dot(offset) * Azimuth.CalibratedMagnitude;
            return new Vec2(az, alt);
        }

        private void MoveAxisWithCompensation(Axis axis, double amount, double backlashCompensationPercent = 1.0)
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

        private Vec2 EstimateCorrection()
        {
            Vec2 offset = new Vec2();
            for (int i = 0; i < settings.SamplesPerMeasurement; ++i)
            {
                solver.Solve();
                offset += solver.AlignmentOffset;
            }
            offset /= settings.SamplesPerMeasurement;

            return AlignmentOffsetToAltAzOffset(-offset);
        }

        public virtual bool AlignOnce(double correctionThreshold, double aggressiveness = 1.0, double backlashCompensationPercent = 1.0)
        {
            var correction = EstimateCorrection();

            if (correction.Length < correctionThreshold)
            {
                return false;
            }

            MoveAxisWithCompensation(Altitude, correction.Altitude * aggressiveness, backlashCompensationPercent);
            MoveAxisWithCompensation(Azimuth, correction.Azimuth * aggressiveness, backlashCompensationPercent);

            return true;
        }
    }
}
