using StackPilot.Services;

namespace StackPilot;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var catalog = CatalogLoader.Load();
            Application.Run(new MainForm(catalog));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "StackPilot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
