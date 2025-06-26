using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for ProgressBar.xaml
    /// </summary>
    public partial class ProgressBar : Window
    {
        private readonly DispatcherTimer _timer;
        private int _secondsElapsed = 0;

        public ProgressBar()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _secondsElapsed++;
            PBar.Value = (_secondsElapsed / 300.0) * 100;

            if (_secondsElapsed >= 300)
            {
                _timer.Stop();
                this.Close();
            }
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
