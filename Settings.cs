namespace AutoPolarAlign
{
    public class Settings
    {
        public double AzimuthBacklash = 42;
        public double AltitudeBacklash = 13;
        public double AzimuthCalibrationDistance = 90;
        public double AltitudeCalibrationDistance = 90;
        public double AzimuthLimit = 300;
        public double AltitudeLimit = 300;
        public int SamplesPerCalibration = 1;
        public int SamplesPerMeasurement = 6;
        public int MaxAlignmentIterations = 16;
        public double AlignmentThreshold = 0.2;
        public bool AcceptBestEffort = false;
        public double StartAggressiveness = 0.95;
        public double EndAggressiveness = 0.25;
    }
}
