
# Auto Polar Align

A simple command line tool for automated polar alignment with Avalon mounts and iOptron iPolar.

Get the application [here](https://github.com/TheCyberBrick/Auto-Polar-Align/releases).

[![AutoPolarAlign Youtube Video](http://img.youtube.com/vi/hSZZqV73a8I/0.jpg)](https://youtu.be/hSZZqV73a8I "AutoPolarAlign Youtube Video")

## Requirements
- [ASCOM Platform](https://ascom-standards.org/)
- Avalon mount with StarGo and Motorized Polar Alignment upgrade
- StarGo ASCOM driver
- iOptron iPolar installed at `%LocalAppData%/iOptron iPolar/iOptron iPolar.exe`
- Make sure iOptron iPolar already has a dark frame and is calibrated 

## Usage
Simply start AutoPolarAlign.exe and watch it do its thing.  
If everything is working properly it will connect to StarGo, start and connect iPolar, do a short calibration and then home in towards the pole.

**Important: If StarGo was installed with admin privileges then you must also run AutoPolarAlign with admin privileges, otherwise it will not be able to connect to StarGo.**

```
$ AutoPolarAlign.exe --help
Auto Polar Align 1.0.0.0
Copyright © TheCyberBrick 2023

  --az-backlash                       (Default: 80) Azimuth backlash. Must be greater or equal the actual backlash amount or the exact backlash amount if backlash calibration is disabled.

  --alt-backlash                      (Default: 40) Altitude backlash. Must be greater or equal the actual backlash amount or the exact backlash amount if backlash calibration is disabled.

  --az-calibration-distance           (Default: 60) Azimuth calibration distance

  --alt-calibration-distance          (Default: 60) Altitude calibration distance

  --az-backlash-calibration           (Default: yes) Azimuth backlash calibration

  --alt-backlash-calibration          (Default: yes) Altitude backlash calibration

  --az-limit                          (Default: 600) Azimuth limit

  --alt-limit                         (Default: 600) Altitude limit

  --reverse-az                        (Default: no) Reverse azimuth axis

  --reverse-alt                       (Default: no) Reverse altitude axis

  --resist-direction-change           (Default: yes) Resist changing direction when alignment is already close enough

  --start-at-low-alt                  (Default: yes) Start altitude alignment at a position below pole

  --start-at-opposite-az              (Default: yes) Start azîmuth alignment at opposite position of pole

  --samples-per-calibration           (Default: 1) Samples taken per axis calibration

  --samples-per-measurement           (Default: 6) Samples taken per polar alignment measurement

  --max-alignment-iterations          (Default: 32) Maximum number of alignment iterations

  --max-positioning-attempts          (Default: 3) Maximum positioning attempts

  --target-alignment                  (Default: 0.5) Target polar alignment

  --acceptance-threshold              (Default: 3) Alignment acceptance threshold in multiples of target alignment

  --accept-best-effort                (Default: no) Shorthand for infinite acceptance threshold

  --start-aggressiveness              (Default: 0.95) Start correction aggressiveness

  --end-aggressiveness                (Default: 0.5) End correction aggressiveness

  --wait-until-consecutive-solving    (Default: 5) Wait with alignment until the specified number of platesolves succeed consecutively

  --wait-seconds-between-solving      (Default: 1) Number of seconds to wait between each platesolve

  --max-wait-seconds                  (Default: 3600) Maximum number of seconds to wait for consecutive platesolves before aborting

  --settling-seconds                  (Default: 1) Number of seconds to wait after each move

  --help                              Display this help screen.

  --version                           Display version information.
```

There are many settings that can be changed but the default values should work just fine.

## TODOs
- Integrate with Starkeeper Voyager (DragScript), N.I.N.A (TPPA?)
- Maybe QHY PoleMaster support
