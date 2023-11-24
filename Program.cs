using AutoPolarAlign;
using System;

class MountInitializer
{
    public static void Main(string[] args)
    {
        Simulation.Run();
        Console.ReadLine();

        //AutoPolarAlign();
    }

    private static void AutoPolarAlign()
    {
        var mount = new StarGoMount();
        var solver = new OptronIPolarSolver();

        var settings = new Settings();

        using (var alignment = new AutoPolarAlignment(mount, solver, settings))
        {
            alignment.Connect();
            alignment.Run();
        }
    }
}
