using MyJukeboxWMPDapper.DataAccess;
using System.Windows;

namespace MyJukeboxWMPDapper.Views
{
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {
        private bool isLoaded = false;

        public InputBox()
        {
            InitializeComponent();

            textboxLocation.Text = SettingsDb.Settings["RecordEditorLocation"];
        }

        private void CommandClose_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void CommandSave_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            if (!isLoaded) return;

            e.CanExecute = textboxLocation.Text != "";
        }

        private void CommandSave_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            SettingsDb.Settings["RecordEditorLocation"] = textboxLocation.Text;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            isLoaded = true;
        }
    }
}
