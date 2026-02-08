using Avalonia;
using HyPrism.Services.Core;
using HyPrism.UI;

using Avalonia.ReactiveUI;
using Serilog;
using System.Runtime;
using System.Text;

namespace HyPrism;

internal sealed class FilteringTextWriter : TextWriter
{
    private readonly TextWriter _inner;
    private readonly string[] _suppressTokens;

    public FilteringTextWriter(TextWriter inner, string[] suppressTokens)
    {
        _inner = inner;
        _suppressTokens = suppressTokens;
    }

    public override Encoding Encoding => _inner.Encoding;

    public override void Write(string? value)
    {
        if (ShouldSuppress(value)) return;
        _inner.Write(value);
    }

    public override void WriteLine(string? value)
    {
        if (ShouldSuppress(value)) return;
        _inner.WriteLine(value);
    }

    private bool ShouldSuppress(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        foreach (var token in _suppressTokens)
        {
            if (value.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Memory optimization: compact LOH on full GC, aggressive memory reclaim
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        System.Runtime.GCSettings.LatencyMode = GCLatencyMode.Interactive;

        // Initialize Logger
        var appDir = UtilityService.GetEffectiveAppDir();
        var logsDir = Path.Combine(appDir, "Logs"); // Changed to Uppercase
        Directory.CreateDirectory(logsDir);

        // Generate timestamped log filename
        var logFileName = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.log";
        var logFilePath = Path.Combine(logsDir, logFileName);

        // Write ASCII Header
        try
        {
            File.WriteAllText(logFilePath, """
 .-..-.      .---.       _                
 : :; :      : .; :     :_;               
 :    :.-..-.:  _.'.--. .-. .--. ,-.,-.,-.
 : :: :: :; :: :   : ..': :`._-.': ,. ,. :
 :_;:_;`._. ;:_;   :_;  :_;`.__.':_;:_;:_;
        .-. :                             
        `._.'                     launcher

""" + Environment.NewLine);
        }
        catch { /* Ignore */ }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .WriteTo.File(
                path: logFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 20 // Keep last 20 logs
            )
            .CreateLogger();

        try
        {
            try
            {
                Console.WriteLine("""

 .-..-.      .---.       _                
 : :; :      : .; :     :_;               
 :    :.-..-.:  _.'.--. .-. .--. ,-.,-.,-.
 : :: :: :; :: :   : ..': :`._-.': ,. ,. :
 :_;:_;`._. ;:_;   :_;  :_;`.__.':_;:_;:_;
        .-. :                             
        `._.'                     launcher

""");
            }
            catch { /* Ignore if console is not available */ }

            Logger.Info("Boot", "Starting HyPrism...");
            Logger.Info("Boot", $"App Directory: {appDir}");

            // Check for wrapper mode flag
            if (args.Contains("--wrapper"))
            {
                // In wrapper mode, launch the wrapper UI
                // This is used by Flatpak/AppImage to manage the installation of the actual HyPrism binary
                Logger.Info("Wrapper", "Running in wrapper mode");
                // The wrapper UI will use WrapperGetStatus, WrapperInstallLatest, WrapperLaunch methods
            }

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application crashed unexpectedly");
            Logger.Error("Crash", $"Application crashed: {ex.Message}");
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseReactiveUI()
            .With(new SkiaOptions { UseOpacitySaveLayer = true })
            .LogToTrace();

}
