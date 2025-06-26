using System.Windows;

namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for TakeABreak.xaml
    /// </summary>
    public partial class TakeABreak : Window
    {
        public TakeABreak()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar window = new ProgressBar();
            var result = window.ShowDialog();
            this.Close();
        }
    }
}
