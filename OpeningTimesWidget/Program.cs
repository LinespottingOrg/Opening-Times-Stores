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
