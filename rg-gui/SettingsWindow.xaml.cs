using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace rg_gui
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public string Theme { get; set; }
        public int MaxSearchTerms { get; set; }
        public bool Multicolor { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (MaxSearchTerms < 1)
            {
                MessageBox.Show("Maximum search terms must be at least 1.");
                return;
            }

            this.DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void txtMaxTerms_TextChanged(object sender, TextChangedEventArgs e)
        {
            var input = txtMaxTerms.Text;
            txtMaxTerms.Text = new string(input.Where(c => char.IsDigit(c)).ToArray());
        }
    }
}
