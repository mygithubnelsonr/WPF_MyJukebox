using System.Windows;

namespace MyJukeboxWMPDapper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            SplashScreen splash = new SplashScreen("Images/Splash.png");
            splash.Show(true, true);

            base.OnStartup(e);
        }
    }
}
