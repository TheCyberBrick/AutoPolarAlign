using CommandLine;

namespace AutoPolarAlign
{
    public enum YesNo
    {
        no,
        yes
    }

    public class Settings
    {
        [Option("az-backlash", Required = false, Default = 80, HelpText = "Azimuth backlash. Must be greater or equal the actual backlash amount or the exact backlash amount if backlash calibration is disabled.")]
        public double AzimuthBacklash { get; set; }

        [Option("alt-backlash", Required = false, Default = 40, HelpText = "Altitude backlash. Must be greater or equal the actual backlash amount or the exact backlash amount if backlash calibration is disabled.")]
        public double AltitudeBacklash { get; set; }

        [Option("az-calibration-distance", Required = false, Default = 60, HelpText = "Azimuth calibration distance")]
        public double AzimuthCalibrationDistance { get; set; }

        [Option("alt-calibration-distance", Required = false, Default = 60, HelpText = "Altitude calibration distance")]
        public double AltitudeCalibrationDistance { get; set; }

        [Option("az-backlash-calibration", Required = false, Default = YesNo.yes, HelpText = "Azimuth backlash calibration")]
        public YesNo AzimuthBacklashCalibrationYesNo { get; set; }
        public bool AzimuthBacklashCalibration
        {
            get => AzimuthBacklashCalibrationYesNo == YesNo.yes;
            set => AzimuthBacklashCalibrationYesNo = value ? YesNo.yes : YesNo.no;
        }

        [Option("alt-backlash-calibration", Required = false, Default = YesNo.yes, HelpText = "Altitude backlash calibration")]
        public YesNo AltitudeBacklashCalibrationYesNo { get; set; }
        public bool AltitudeBacklashCalibration
        {
            get => AltitudeBacklashCalibrationYesNo == YesNo.yes;
            set => AltitudeBacklashCalibrationYesNo = value ? YesNo.yes : YesNo.no;
        }

        [Option("az-limit", Required = false, Default = 600, HelpText = "Azimuth limit")]
        public double AzimuthLimit { get; set; }

        [Option("alt-limit", Required = false, Default = 600, HelpText = "Altitude limit")]
        public double AltitudeLimit { get; set; }

        [Option("reverse-az", Required = false, Default = YesNo.no, HelpText = "Reverse azimuth axis")]
        public YesNo ReverseAzimuthYesNo { get; set; }
        public bool ReverseAzimuth
        {
            get => ReverseAzimuthYesNo == YesNo.yes;
            set => ReverseAzimuthYesNo = value ? YesNo.yes : YesNo.no;
        }

        [Option("reverse-alt", Required = false, Default = YesNo.no, HelpText = "Reverse altitude axis")]
        public YesNo ReverseAltitudeYesNo { get; set; }
        public bool ReverseAltitude
        {
            get => ReverseAltitudeYesNo == YesNo.yes;
            set => ReverseAltitudeYesNo = value ? YesNo.yes : YesNo.no;
        }

        [Option("resist-direction-change", Required = false, Default = YesNo.yes, HelpText = "Resist changing direction when alignment is already close enough")]
        public YesNo ResistDirectionChangeYesNo { get; set; }
        public bool ResistDirectionChange
        {
            get => ResistDirectionChangeYesNo == YesNo.yes;
            set => ResistDirectionChangeYesNo = value ? YesNo.yes : YesNo.no;
        }

        [Option("start-at-low-alt", Required = false, Default = YesNo.yes, HelpText = "Start altitude alignment at a position below pole")]
        public YesNo StartAtLowAltitudeYesNo { get; set; }
        public bool StartAtLowAltitude
        {
            get => StartAtLowAltitudeYesNo == YesNo.yes;
            set => StartAtLowAltitudeYesNo = value ? YesNo.yes : YesNo.no;
        }

        [Option("start-at-opposite-az", Required = false, Default = YesNo.yes, HelpText = "Start azîmuth alignment at opposite position of pole")]
        public YesNo StartAtOppositeAzimuthYesNo { get; set; }
        public bool StartAtOppositeAzimuth
        {
            get => StartAtOppositeAzimuthYesNo == YesNo.yes;
            set => StartAtOppositeAzimuthYesNo = value ? YesNo.yes : YesNo.no;
        }

        [Option("samples-per-calibration", Required = false, Default = 1, HelpText = "Samples taken per axis calibration")]
        public int SamplesPerCalibration { get; set; }

        [Option("samples-per-measurement", Required = false, Default = 6, HelpText = "Samples taken per polar alignment measurement")]
        public int SamplesPerMeasurement { get; set; }

        [Option("max-alignment-iterations", Required = false, Default = 32, HelpText = "Maximum number of alignment iterations")]
        public int MaxAlignmentIterations { get; set; }

        [Option("max-positioning-attempts", Required = false, Default = 3, HelpText = "Maximum positioning attempts")]
        public int MaxPositioningAttempts { get; set; }

        [Option("target-alignment", Required = false, Default = 0.5, HelpText = "Target polar alignment")]
        public double TargetAlignment { get; set; }

        [Option("acceptance-threshold", Required = false, Default = 3, HelpText = "Alignment acceptance threshold in multiples of target alignment")]
        public double AcceptanceThreshold { get; set; }

        [Option("accept-best-effort", Required = false, Default = YesNo.no, HelpText = "Shorthand for infinite acceptance threshold")]
        public YesNo AcceptBestEffortYesNo { get; set; }
        public bool AcceptBestEffort
        {
            get => AcceptBestEffortYesNo == YesNo.yes;
            set => AcceptBestEffortYesNo = value ? YesNo.yes : YesNo.no;
        }

        [Option("start-aggressiveness", Required = false, Default = 0.95, HelpText = "Start correction aggressiveness")]
        public double StartAggressiveness { get; set; }

        [Option("end-aggressiveness", Required = false, Default = 0.5, HelpText = "End correction aggressiveness")]
        public double EndAggressiveness { get; set; }

        [Option("wait-until-consecutive-solving", Required = false, Default = 5, HelpText = "Wait with alignment until the specified number of platesolves succeed consecutively")]
        public int WaitUntilConsecutiveSolving { get; set; }

        [Option("wait-seconds-between-solving", Required = false, Default = 1, HelpText = "Number of seconds to wait between each platesolve")]
        public double WaitSecondsBetweenSolving { get; set; }

        [Option("max-wait-seconds", Required = false, Default = 3600, HelpText = "Maximum number of seconds to wait for consecutive platesolves before aborting")]
        public double MaxWaitSeconds { get; set; }

        [Option("settling-seconds", Required = false, Default = 1, HelpText = "Number of seconds to wait after each move")]
        public double SettlingSeconds { get; set; }
    }
}
