using System;
using System.Collections.Generic;
using System.Threading;

namespace AutoPolarAlign
{
    public class AutoPolarAlignment
    {
        private readonly IPolarAlignmentMount mount;

        private readonly IPolarAlignmentSolver solver;

        public Axis Altitude { get; } = new Axis("altitude");

        public Axis Azimuth { get; } = new Axis("azimuth");

        protected Vec2 lastMeasuredOffset;

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
            if (!WaitUntilConsecutiveSolving())
            {
                throw new Exception("Timed out waiting for successful plate solving");
            }

            if (!Calibrate())
            {
                throw new Exception("Calibration failed");
            }

            bool success = Align();

            if (success)
            {
                Console.WriteLine("Alignment success");
            }
            else
            {
                Console.WriteLine("Alignment failure");
            }

            return success;
        }

        protected bool WaitUntilConsecutiveSolving()
        {
            if (settings.WaitUntilConsecutiveSolving <= 0)
            {
                return true;
            }

            Console.WriteLine("Waiting for plate solving...");

            var startTime = DateTime.Now;

            int consecutiveSolves = 0;

            while ((DateTime.Now - startTime).TotalSeconds < settings.MaxWaitSeconds)
            {
                if (solver.Solve(repeatUntilSuccess: false))
                {
                    consecutiveSolves++;

                    if (consecutiveSolves >= settings.WaitUntilConsecutiveSolving)
                    {
                        return true;
                    }
                }
                else
                {
                    consecutiveSolves = 0;
                }

                if (settings.WaitSecondsBetweenSolving > double.Epsilon)
                {
                    Thread.Sleep((int)Math.Ceiling(settings.WaitSecondsBetweenSolving * 1000));
                }
            }

            return false;
        }

        public bool Align()
        {
            Console.WriteLine("Starting alignment...");

            bool success = AlignToTarget();

            if (settings.AcceptBestEffort)
            {
                success = true;
            }

            Vec2 correction = EstimateCorrection();

            Console.WriteLine("Final distance: " + correction.Length);

            if (!success)
            {
                success = correction.Length < settings.TargetAlignment * Math.Max(1, settings.AcceptanceThreshold);
            }

            return success;
        }

        protected bool AlignToTarget()
        {
            Vec2 previousCorrection = new Vec2();

            for (int i = 0; i < settings.MaxAlignmentIterations; ++i)
            {
                double aggressiveness = settings.MaxAlignmentIterations > 1 ? (settings.EndAggressiveness + (settings.StartAggressiveness - settings.EndAggressiveness) / (settings.MaxAlignmentIterations - 1) * (settings.MaxAlignmentIterations - 1 - i)) : settings.EndAggressiveness;

                var correction = EstimateCorrection();

                Console.WriteLine("Distance: " + correction.Length);

                if (i > 0)
                {
                    // If new correction is in opposite direction then it overshot
                    // which means that the backlash compensation is too large, so
                    // it must be reduced by the overshot amount

                    if (settings.AltitudeBacklashCalibration && Math.Sign(previousCorrection.Altitude) != Math.Sign(correction.Altitude))
                    {
                        double prev = Altitude.BacklashCompensation;

                        Altitude.BacklashCompensation = Math.Max(0, Altitude.BacklashCompensation - Math.Abs(correction.Altitude));

                        Console.WriteLine("Adjusted altitude backlash: " + Altitude.BacklashCompensation + " (" + (Altitude.BacklashCompensation - prev).ToString("+0.###;-0.###") + ")");
                    }

                    if (settings.AzimuthBacklashCalibration && Math.Sign(previousCorrection.Azimuth) != Math.Sign(correction.Azimuth))
                    {
                        double prev = Azimuth.BacklashCompensation;

                        Azimuth.BacklashCompensation = Math.Max(0, Azimuth.BacklashCompensation - Math.Abs(correction.Azimuth));

                        Console.WriteLine("Adjusted azimuth backlash: " + Azimuth.BacklashCompensation + " (" + (Azimuth.BacklashCompensation - prev).ToString("+0.###;-0.###") + ")");
                    }
                }

                previousCorrection = correction;

                if (settings.ResistDirectionChange)
                {
                    double resistThreshold = Math.Sqrt(0.5 * settings.TargetAlignment * settings.TargetAlignment);

                    bool resistAltitudeChange = Altitude.LastDirection != 0 && Math.Sign(correction.Altitude) != Altitude.LastDirection && Math.Abs(correction.Altitude) < resistThreshold;
                    bool resistAzimuthChange = Azimuth.LastDirection != 0 && Math.Sign(correction.Azimuth) != Azimuth.LastDirection && Math.Abs(correction.Azimuth) < resistThreshold;

                    if (resistAltitudeChange && resistAzimuthChange)
                    {
                        // Already below threshold, avoid correction altogether
                        return true;
                    }
                    else if (resistAltitudeChange)
                    {
                        correction.Altitude = 0;
                    }
                    else if (resistAzimuthChange)
                    {
                        correction.Azimuth = 0;
                    }
                }

                Console.WriteLine("Correction: " + correction);

                Move(correction, aggressiveness: aggressiveness);

                if (correction.Length < settings.TargetAlignment)
                {
                    return true;
                }
            }

            return false;
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

            lastMeasuredOffset = offset;

            return offset;
        }

        private bool CalibrateAltitude()
        {
            // Reverse altitude calibration dir to later help
            // StartAtLowAltitude if enabled
            return CalibrateAxis(Altitude, settings.AltitudeBacklashCalibration, reverse: true, margin: settings.StartAtLowAltitude ? -settings.AltitudeCalibrationDistance * 0.25 : 0);
        }

        private bool CalibrateAzimuth()
        {
            double margin = 0;
            bool reverse = false;

            if (settings.StartAtOppositeAzimuth && Altitude.CalibratedMagnitude > double.Epsilon && Altitude.CalibratedDirection.Length > double.Epsilon && lastMeasuredOffset.Length > double.Epsilon)
            {
                // Calibrate azimuth axis towards pole such that the maximum distance
                // from the pole during calibration is minimized.
                // Assumes that azimuth axis is orthogonal to altitude axis and that
                // positive azimuth points to the right when positive altitude points
                // up.

                var estimatedAzimuthDirection = new Vec2(Altitude.CalibratedDirection.Y, -Altitude.CalibratedDirection.X);

                int estimatedAzimuthOffsetDir = Math.Sign(estimatedAzimuthDirection.Dot(lastMeasuredOffset));

                reverse = estimatedAzimuthOffsetDir > 0;
                margin = -estimatedAzimuthOffsetDir * settings.AzimuthCalibrationDistance * 0.25;
            }

            return CalibrateAxis(Azimuth, settings.AzimuthBacklashCalibration, reverse: reverse, margin: margin);
        }

        private bool CalibrateAxis(Axis axis, bool calibrateBacklash, bool reverse = false, double margin = 0)
        {
            Console.WriteLine("Calibrating " + axis.Name + " axis...");

            double calibrationDir = reverse ? -1 : 1;
            double calibrationDistance = axis.CalibrationDistance;

            Console.WriteLine("Clearing backlash (" + (axis.BacklashCompensation * calibrationDir).ToString("+0.###;-0.###") + ")");

            // Remove any backlash before calibration
            MoveAxisWithoutCompensation(axis, axis.BacklashCompensation * calibrationDir);

            // Estimate origin
            var startOffset = MeasureCurrentOffset();

            Vec2 dir;
            Vec2 endOffset;
            double dst;

            Console.WriteLine("Calibrating axis (" + (calibrationDistance * calibrationDir).ToString("+0.###;-0.###") + ")");

            if (settings.SamplesPerCalibration > 1)
            {
                // Take samples to estimate axis direction and scale
                double stepDistance = calibrationDistance / settings.SamplesPerCalibration * calibrationDir;
                var samples = new List<Vec2>();
                for (int i = 0; i < settings.SamplesPerCalibration; ++i)
                {
                    MoveAxisWithoutCompensation(axis, stepDistance);
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
                MoveAxisWithoutCompensation(axis, calibrationDistance * calibrationDir);
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

            Console.WriteLine("Direction: " + axis.CalibratedDirection);
            Console.WriteLine("Magnitude: " + axis.CalibratedMagnitude);

            bool isMarginSet = Math.Abs(margin) > double.Epsilon;

            if (calibrateBacklash)
            {
                double expectedMoveDistance = axis.BacklashCompensation + calibrationDistance;

                if (isMarginSet)
                {
                    double compensatedMargin = margin * 1.1 + expectedMoveDistance * calibrationDir;

                    double axisOffset = AlignmentOffsetToAxisOffset(axis, endOffset);

                    double distanceToMargin = compensatedMargin - axisOffset;

                    // Try to move axis further in the same direction such that
                    // after backlash calibration it already is past the specified
                    // margin, which makes the MoveAxisPastMargin at the end a no-op
                    // and also has the benefit that backlash will already be 0 when
                    // alignment finally begins. This only works if axis limit won't
                    // be exceeded, otherwise the MoveAxisPastMargin must be done at
                    // the end.
                    if (Math.Sign(distanceToMargin) == Math.Sign(compensatedMargin) && Math.Sign(distanceToMargin) == Math.Sign(calibrationDir) && Math.Abs(axis.Position + distanceToMargin) < axis.Limit * 0.99)
                    {
                        Console.WriteLine("Positioning to " + (axis.Position + distanceToMargin) + " (" + distanceToMargin.ToString("+0.###;-0.###") + ")");

                        MoveAxisWithoutCompensation(axis, distanceToMargin);
                        endOffset = MeasureCurrentOffset();
                    }
                }

                Console.WriteLine("Calibrating backlash (" + (-expectedMoveDistance * calibrationDir).ToString("+0.###;-0.###") + ")");

                // Move back to start position of calibration
                MoveAxisWithoutCompensation(axis, -expectedMoveDistance * calibrationDir);

                var newEndOffset = MeasureCurrentOffset();

                double actualMoveDistance = -AlignmentOffsetToAxisOffset(axis, newEndOffset - endOffset) * calibrationDir;

                if (actualMoveDistance <= 0)
                {
                    // Axis moved in wrong direction
                    return false;
                }

                axis.BacklashCompensation = (expectedMoveDistance - actualMoveDistance) * 0.95;

                // Clear backlash in case backlash compensation has increased
                axis.ClearBacklash(Math.Sign(-expectedMoveDistance * calibrationDir));

                Console.WriteLine("Backlash: " + axis.BacklashCompensation);
            }

            if (isMarginSet && !MoveAxisPastMargin(axis, margin))
            {
                return false;
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

        private bool MoveAxisPastMargin(Axis axis, double margin)
        {
            int i = 0;
            while (true)
            {
                double axisOffset = AlignmentOffsetToAxisOffset(axis, MeasureCurrentOffset());

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

                double distanceToMargin = margin * 1.1 - axisOffset;

                Console.WriteLine("Positioning to " + (axis.Position + distanceToMargin) + " (" + distanceToMargin.ToString("+0.###;-0.###") + ")");

                MoveAxisWithCompensation(axis, distanceToMargin);
            }

            return true;
        }

        protected double AlignmentOffsetToAxisOffset(Axis axis, Vec2 offset)
        {
            return axis.CalibratedDirection.Dot(offset) * axis.CalibratedMagnitude;
        }

        protected Vec2 AlignmentOffsetToAltAzOffset(Vec2 offset)
        {
            double alt = AlignmentOffsetToAxisOffset(Altitude, offset);
            double az = AlignmentOffsetToAxisOffset(Azimuth, offset);
            return new Vec2(az, alt);
        }

        protected void MoveAxisWithoutCompensation(Axis axis, double amount)
        {
            MoveAxisWithCompensation(axis, amount, backlashCompensationPercent: 0.0);
        }

        protected void MoveAxisWithCompensation(Axis axis, double amount, double backlashCompensationPercent = 1.0)
        {
            if (Math.Abs(amount) <= double.Epsilon)
            {
                return;
            }

            double compensatedAmount = axis.EstimateCompensatedMove(amount, backlashCompensationPercent);

            Console.WriteLine("Move " + axis.Name + " (" + compensatedAmount.ToString("+0.###;-0.###") + " BL: " + Math.Abs(compensatedAmount - amount).ToString("0.###") + ")");

            if (axis.Move(compensatedAmount, out amount))
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

            if (settings.SettlingSeconds > double.Epsilon)
            {
                Thread.Sleep((int)Math.Ceiling(settings.SettlingSeconds * 1000));
            }
        }
    }
}
