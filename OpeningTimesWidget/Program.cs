// Entry point — single-instance floating widget.
namespace OpeningTimesWidget;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal static class AppIcons
{
    public static Icon Load(int size = 32)
    {
        try
        {
            var icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(icoPath))
                return new Icon(icoPath, size, size);

            var associated = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (associated is not null)
                return associated;
        }
        catch
        {
            // fall through to generic
        }

        return SystemIcons.Application;
    }
}
