namespace GradeTracker.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Must be called before any window is created.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowFatalError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                ShowFatalError(ex);
            }
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static void ShowFatalError(Exception ex)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{ex.GetType().Name}: {ex.Message}",
            "Grade Tracker",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
