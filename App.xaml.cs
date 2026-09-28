using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Chat;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnDispatcherUnhandledException(
        object sender,
        DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Chat");

            Directory.CreateDirectory(folder);

            var logPath = Path.Combine(folder, "crash.log");
            File.AppendAllText(
                logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never create another exception.
        }

        MessageBox.Show(
            $"Chat encountered an unexpected error and kept the window open.\n\n" +
            $"{e.Exception.GetType().Name}: {e.Exception.Message}",
            "Chat Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}
