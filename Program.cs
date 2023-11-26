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
            try
            {
                mount.Connect();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to connect to mount", ex);
            }

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
