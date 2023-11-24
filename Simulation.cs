using System.Collections.Generic;
using System;

namespace AutoPolarAlign
{
    public class Simulation
    {
        public static void Run()
        {
            int numRuns = 10;

            var rng = new Random();

            List<double> results = new List<double>();
            List<double> totals = new List<double>();

            for (int i = 0; i < numRuns; ++i)
            {
                Vec2 initialAlignmentOffset = new Vec2(42.0f, -87.0f);

                Vec2 altAxis = new Vec2(1, 1);
                Vec2 azAxis = new Vec2(-1, 1);

                float altAxisScale = 3.0f;
                float azAxisScale = 2.0f;

                float backlashScale = 1.0f;
                float compensationScale = 1.0f;

                float aggressiveness = 1.0f;

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

                var aligner = new AutoPolarAlignment(simulator, simulator, new Settings()
                {
                    AltitudeBacklash = altBacklashCompensation * backlashScale,
                    AltitudeCalibrationDistance = altCalibrationDistance,
                    AzimuthBacklash = azBacklashCompensation * backlashScale,
                    AzimuthCalibrationDistance = azCalibrationDistance
                });

                try
                {
                    aligner.Connect();

                    aligner.Calibrate();

                    double totalOffsets = 0.0;

                    int numSteps = 6;
                    for (int j = 0; j < numSteps; ++j)
                    {
                        aligner.AlignOnce(aggressiveness, compensationScale);
                        totalOffsets += simulator.TrueAlignmentOffset.Length;
                    }

                    results.Add(simulator.TrueAlignmentOffset.Length);
                    totals.Add(totalOffsets);
                }
                finally
                {
                    aligner.Dispose();
                }
            }

            double resultsSum = 0;
            double totalsSum = 0;
            for (int i = 0; i < results.Count; ++i)
            {
                Console.WriteLine(results[i] + " " + totals[i]);
                resultsSum += results[i];
                totalsSum += totals[i];
            }

            Console.WriteLine("Avg.: " + (resultsSum / results.Count) + " " + (totalsSum / results.Count));
        }
    }
}
