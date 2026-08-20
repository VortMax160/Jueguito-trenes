using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace TrainGame;

public partial class App : Application
{
    private static readonly string LogPath = @"D:\Proyecto\Prueba Gemini\debug.log";

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("App.OnStartup called");

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Log($"UnhandledException: {args.ExceptionObject}");
        };

        DispatcherUnhandledException += (s, args) =>
        {
            Log($"DispatcherUnhandledException: {args.Exception}");
            args.Handled = false;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Log($"UnobservedTaskException: {args.Exception}");
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log($"App.OnExit called with code: {e.ApplicationExitCode}");
        base.OnExit(e);
    }
}
