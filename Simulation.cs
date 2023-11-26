using System.Collections.Generic;
using System;

namespace AutoPolarAlign
{
    public class Simulation
    {
        private class Aligner : AutoPolarAlignment
        {
            public double TotalOffsets { get; set; }

            public Vec2 CurrentOffset { get; set; }

            public int Iterations { get; set; }

            public double CompensationScale { get; set; } = 1.0;

            private readonly Simulator simulator;

            public Aligner(Simulator simulator, Settings settings) : base(simulator, simulator, settings)
            {
                this.simulator = simulator;
            }

            public void Reset()
            {
                TotalOffsets = 0;
                Iterations = 0;
                CurrentOffset = new Vec2();
            }

            public override bool AlignOnce(double correctionThreshold, double aggressiveness = 1, double backlashCompensationPercent = 1)
            {
                bool result = base.AlignOnce(correctionThreshold, aggressiveness, backlashCompensationPercent * CompensationScale);

                TotalOffsets += simulator.TrueAlignmentOffset.Length;
                CurrentOffset = simulator.TrueAlignmentOffset;

                if (result)
                {
                    ++Iterations;
                }

                return result;
            }
        }

        public static void Run()
        {
            int numRuns = 20;

            var rng = new Random();

            List<double> results = new List<double>();
            List<double> totals = new List<double>();
            List<int> iterations = new List<int>();

            for (int i = 0; i < numRuns; ++i)
            {
                Vec2 initialAlignmentOffset = new Vec2(42.0f, -87.0f);

                Vec2 altAxis = new Vec2(1, 1);
                Vec2 azAxis = new Vec2(-1, 1);

                float altAxisScale = 3.0f;
                float azAxisScale = 2.0f;

                float backlashScale = 1.0f;
                float compensationScale = 1.0f;

                float startAggressiveness = 1.0f;
                float endAggressiveness = 0.25f;

                float alignmentThreshold = 1.0f;

                float randomnessScale = 5.0f;
                float offsetJitter = 1.0f;
                float moveJitter = 0.1f;

                float altBacklash = 30.0f;
                float azBacklash = 30.0f;

                float altBacklashCompensation = 30.0f;
                float azBacklashCompensation = 30.0f;

                float altCalibrationDistance = 150.0f;
                float azCalibrationDistance = 150.0f;

                float initialAltBacklash = 15.0f;
                float initialAzBacklash = -5.0f;

                var simulator = new Simulator(
                    rng,
                    initialAlignmentOffset,
                    altAxis * altAxisScale, azAxis * azAxisScale,
                    offsetJitter * randomnessScale, moveJitter * randomnessScale,
                    altBacklash * backlashScale, azBacklash * backlashScale,
                    initialAltBacklash * backlashScale, -initialAzBacklash * backlashScale
                    );

                var settings = new Settings()
                {
                    AltitudeBacklash = altBacklashCompensation * backlashScale,
                    AltitudeCalibrationDistance = altCalibrationDistance,
                    AzimuthBacklash = azBacklashCompensation * backlashScale,
                    AzimuthCalibrationDistance = azCalibrationDistance,
                    StartAggressiveness = startAggressiveness,
                    EndAggressiveness = endAggressiveness,
                    AlignmentThreshold = alignmentThreshold
                };

                var aligner = new Aligner(simulator, settings)
                {
                    CompensationScale = compensationScale
                };

                try
                {
                    simulator.Connect();

                    aligner.Reset();

                    aligner.Calibrate();

                    aligner.Align();

                    results.Add(aligner.CurrentOffset.Length);
                    totals.Add(aligner.TotalOffsets);
                    iterations.Add(aligner.Iterations);
                }
                finally
                {
                    simulator.Dispose();
                }
            }

            Console.WriteLine("┌────────────────────────────────────────────┐");
            Console.WriteLine("│  Run      Offset        Total   Iterations │");
            Console.WriteLine("├────────────────────────────────────────────┤");
            double resultsSum = 0;
            double totalsSum = 0;
            int totalIterations = 0;
            for (int i = 0; i < results.Count; ++i)
            {
                Console.WriteLine(string.Format("│ {0,4:###0} {1,11:#######0.00}  {2,11:#######0.00}  {3,11:#######0} │", i, results[i], totals[i], iterations[i]));
                resultsSum += results[i];
                totalsSum += totals[i];
                totalIterations += iterations[i];
            }
            Console.WriteLine("├────────────────────────────────────────────┤");
            Console.WriteLine(string.Format("│ Avg. {0,11:#######0.00}  {1,11:#######0.00}  {2,11:#######0.00} │", resultsSum / results.Count, totalsSum / results.Count, totalIterations / (float)results.Count));
            Console.WriteLine("└────────────────────────────────────────────┘");
        }
    }
}
