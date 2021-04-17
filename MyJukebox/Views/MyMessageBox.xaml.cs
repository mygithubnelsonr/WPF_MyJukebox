using System.Windows;

namespace MyJukeboxWMPDapper.Views
{
    /// <summary>
    /// Interaction logic for MessageBox.xaml
    /// </summary>
    public partial class MyMessageBox : Window
    {
        public string MTitle { get; set; }
        public string MMessage { get; set; }

        public MyMessageBox(string title, string message)
        {
            MTitle = title;
            MMessage = message;
        }

        public MyMessageBox()
        {
            InitializeComponent();

            MTitle = this.Title;
            MMessage = this.textblockMessage.Text;
        }

        private void CommandClose_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Clipboard.Clear();
            Clipboard.SetText(MMessage);
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = MTitle;
            textblockMessage.Text = MMessage;
        }
    }
}
