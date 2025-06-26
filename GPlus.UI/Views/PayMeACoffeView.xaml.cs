using System.Diagnostics;
using System.Windows;

namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for PayMeACoffe.xaml
    /// </summary>
    public partial class PayMeACoffeView : Window
    {
        public PayMeACoffeView()
        {
            InitializeComponent();
        }

        private void DonateButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://donate.stripe.com/5kQ14m8cp2gpclo7PI1Jm00",
                UseShellExecute = true
            });
            this.Close();
        }
    }
}
