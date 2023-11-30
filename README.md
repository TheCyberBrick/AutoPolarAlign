# Auto Polar Align

A simple command line tool for automated polar alignment with Avalon mounts and iOptron iPolar.

Get the application [here](https://github.com/TheCyberBrick/Auto-Polar-Align/releases).

[![AutoPolarAlign Youtube Video](http://img.youtube.com/vi/LklTLXXU3LY/0.jpg)](http://www.youtube.com/watch?v=LklTLXXU3LY "AutoPolarAlign Youtube Video")

## Requirements
- Avalon mount with StarGo and Motorized Polar Alignment upgrade
- StarGo ASCOM driver
- iOptron iPolar installed at `%LocalAppData%/iOptron iPolar/iOptron iPolar.exe`
- Make sure iOptron iPolar already has a dark frame and is calibrated 

## Usage
Simply start AutoPolarAlign.exe and watch it do its thing.  
If everything is working properly it will connect to StarGo, start and connect iPolar, do a short calibration and then home in towards the pole.

Note: If StarGo was installed with admin privileges then you must also run AutoPolarAlign with admin privileges, otherwise it will not be able to connect to StarGo.

## TODOs
- Configuration
- Determine backlash automatically
- Integrate with Starkeeper Voyager (DragScript), N.I.N.A (TPPA?)
- Maybe QHY PoleMaster support
