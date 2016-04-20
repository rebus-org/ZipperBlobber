using GoCommando;
using Serilog;

namespace ZipperBlobber
{
    class Program
    {
        const string LogTemplate = "{Timestamp:HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}";

        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole(outputTemplate: LogTemplate)
                .WriteTo.RollingFile(@"C:\logs\zipperblobber\log.txt")
                .MinimumLevel.Debug()
                .CreateLogger();

            Go.Run();
        }
    }
}
