using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace MyJukeboxWMPDapper.Views
{
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : Window
    {
        private bool isLoaded = false;
        private bool _firsttextenter = false;

        public string ITitle { get; set; } = "Inputbox";
        public string ILabel { get; set; } = "Enter Value:";
        public string IText { get; set; } = "";
        public bool IKeepText { get; set; } = false;

        public InputBox()
        {
            InitializeComponent();

            DynamicLoadStyles("Blue");

            textboxInput.Focus();
        }

        public InputBox(string title, string label, string text)
        {
            this.Title = title;
            textblockInput.Text = label;
            textboxInput.Text = text;
        }

        private void CommandClose_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void CommandSave_CanExecute(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            if (!isLoaded) return;

            e.CanExecute = textboxInput.Text != "";
        }

        private void CommandSave_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            IText = textboxInput.Text;
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = ITitle;
            this.textblockInput.Text = ILabel != "" ? ILabel : textblockInput.Text;
            this.textboxInput.Text = IText;
            isLoaded = true;
        }

        private void textboxInput_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_firsttextenter == false && IKeepText == false)
            {
                textboxInput.Text = "";
                _firsttextenter = true;
            }
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }

        private void Window_Move(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void DynamicLoadStyles(string text)
        {
            string fileName;


            if (text == "None")
            {
                // Clear any previous dictionaries loaded
                Resources.MergedDictionaries.Clear();
            }
            else
            {
                fileName = Environment.CurrentDirectory +
                             @"\Dictionaries\" + text + ".xaml";

                if (File.Exists(fileName))
                {
                    using (FileStream fs = new FileStream(fileName, FileMode.Open))
                    {
                        // Read in ResourceDictionary File
                        ResourceDictionary dic = (ResourceDictionary)XamlReader.Load(fs);
                        // Clear any previous dictionaries loaded
                        Resources.MergedDictionaries.Clear();
                        // Add in newly loaded Resource Dictionary
                        Resources.MergedDictionaries.Add(dic);
                    }
                }
                else
                    MessageBox.Show("File: " + text +
                       " does not exist. Please re-enter the name.");
            }
        }

    }
}
