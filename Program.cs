using AutoPolarAlign;
using CommandLine;
using System;
using System.IO;

class MountInitializer
{
    public static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<Settings>(args).MapResult(opts => Run(opts), errs => 3);
    }

    private static int Run(Settings settings)
    {
        try
        {
            if (!AutoPolarAlign(settings))
            {
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Aborting due to an error: " + ex.Message);
            Console.WriteLine(ex.ToString());
            return 2;
        }
        return 0;
    }

    private static bool AutoPolarAlign(Settings settings)
    {
        using (var mount = new StarGoMount())
        using (var solver = new OptronIPolarSolver())
        {
            mount.ReverseAltitude = settings.ReverseAltitude;
            mount.ReverseAzimuth = settings.ReverseAzimuth;

            Console.WriteLine("Connecting to mount...");

            try
            {
                mount.Connect();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed connecting to mount", ex);
            }

            Console.WriteLine("Connecting to plate solver...");

            try
            {
                solver.Connect();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed connecting to solver", ex);
            }

            var alignment = new AutoPolarAlignment(mount, solver, settings);

            return alignment.Run();
        }
    }
}
