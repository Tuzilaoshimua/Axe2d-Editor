using System.Text;
using System.Runtime.ExceptionServices;

namespace Axe2DEditor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) =>
            {
                LogUnhandledException("UI", e.Exception);
                ExceptionDispatchInfo.Capture(e.Exception).Throw();
            };
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception exception)
                {
                    LogUnhandledException("AppDomain", exception);
                }
            };
            Application.Run(new MainForm());
        }

        private static void LogUnhandledException(string source, Exception exception)
        {
            try
            {
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Axe2DEditor",
                    "Logs");
                Directory.CreateDirectory(directory);

                var file = Path.Combine(directory, "unhandled-exceptions.log");
                var text = new StringBuilder()
                    .AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}")
                    .AppendLine(exception.ToString())
                    .AppendLine()
                    .ToString();
                File.AppendAllText(file, text);
            }
            catch
            {
                // Exception logging must never create a second crash.
            }
        }
    }
}
