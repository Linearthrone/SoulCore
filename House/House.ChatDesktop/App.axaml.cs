using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using House.ChatDesktop.Services;

namespace House.ChatDesktop;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        PresenceStartupLog.Write("Framework initialization completed");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                PresenceStartupLog.Write("Creating MainWindow");
                desktop.MainWindow = new MainWindow();
                PresenceStartupLog.Write("MainWindow created");
            }
            catch (Exception ex)
            {
                PresenceStartupLog.WriteException("MainWindow ctor", ex);
                PresenceStartupLog.ShowFatal(
                    "SoulCore Presence failed to open",
                    ex.Message);
                throw;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
