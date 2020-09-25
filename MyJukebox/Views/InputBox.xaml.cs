using MyJukebox.BLL;
using System.Windows;

namespace MyJukebox.Views
{
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {
        public InputBox()
        {
            InitializeComponent();

            textboxLocation.Text = SettingsDb.RecordEditorLocation;
        }

        private void CommandClose_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void CommandSave_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = textboxLocation.Text != "";
        }

        private void CommandSave_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            SettingsDb.RecordEditorLocation = textboxLocation.Text;
        }
    }
}
