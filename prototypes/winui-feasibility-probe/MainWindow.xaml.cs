using Microsoft.UI.Xaml;

namespace WinUiFeasibilityProbe;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Detail.Text = $"Runtime: .NET {Environment.Version}. This window closes itself in 3 seconds.";

        // A probe must not leave a window on the user's desktop.
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };
        timer.Start();
    }
}
