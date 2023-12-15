namespace AutoPolarAlign
{
    public class Settings
    {
        public double AzimuthBacklash = 80;
        public double AltitudeBacklash = 40;
        public double AzimuthCalibrationDistance = 60;
        public double AltitudeCalibrationDistance = 60;
        public bool AzimuthBacklashCalibration = true;
        public bool AltitudeBacklashCalibration = true;
        public double AzimuthLimit = 600;
        public double AltitudeLimit = 600;
        public bool ResistDirectionChange = true;
        public bool StartAtLowAltitude = true;
        public int SamplesPerCalibration = 1;
        public int SamplesPerMeasurement = 6;
        public int MaxAlignmentIterations = 32;
        public int MaxPositioningAttempts = 3;
        public double AlignmentThreshold = 0.5;
        public bool AcceptBestEffort = false;
        public double StartAggressiveness = 0.95;
        public double EndAggressiveness = 0.5;
        public int WaitUntilConsecutiveSolving = 5;
        public int WaitSecondsBetweenSolving = 1;
        public int MaxWaitSeconds = 3600;
        public int SettlingSeconds = 1;
    }
}
