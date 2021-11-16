using MyJukeboxWMPDapper.DataAccess;
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
            string theme = GetSetData.GetSetting("Theme");
            string image = $"Images/Splash{theme.Replace(".xaml","")}.png";

            SplashScreen splash = new SplashScreen(image);
            splash.Show(true, true);

            base.OnStartup(e);
        }
    }
}
