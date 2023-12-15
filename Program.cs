using AutoPolarAlign;
using System;

class MountInitializer
{
    public static void Main(string[] args)
    {
        var settings = new Settings();
        AutoPolarAlign(settings);
    }

    private static void AutoPolarAlign(Settings settings)
    {
        using (var mount = new StarGoMount())
        using (var solver = new OptronIPolarSolver())
        {
            Console.WriteLine("Connecting to mount...");

            try
            {
                mount.Connect();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to connect to mount", ex);
            }

            Console.WriteLine("Connecting to plate solver...");

            try
            {
                solver.Connect();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to connect to solver", ex);
            }

            var alignment = new AutoPolarAlignment(mount, solver, settings);
            alignment.Run();
        }

    }
}
