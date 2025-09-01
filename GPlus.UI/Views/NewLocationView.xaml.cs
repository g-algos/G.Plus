using GPlus.UI.ViewsModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for NewLocationView.xaml
    /// </summary>
    public partial class NewLocationView : Window
    {
        public NewLocationView(NewLocationVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += (s, e) => {
                this.DialogResult = e;
                this.Close();
            };

        }
        internal NewLocationVM ViewModel => (NewLocationVM)DataContext;

        private static readonly Regex _lettersOnlyRegex = new Regex("^[a-zA-ZÀ-ÖØ-öø-ÿ_\\- ]+$");

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_lettersOnlyRegex.IsMatch(e.Text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(System.Windows.DataFormats.Text))
            {
                string text = e.DataObject.GetData(System.Windows.DataFormats.Text) as string ?? "";
                if (!_lettersOnlyRegex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
