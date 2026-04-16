using GPlus.UI.ViewsModels;
using System.Windows;

namespace GPlus.UI.Views;

/// <summary>
/// Interaction logic for NewVersion.xaml
/// </summary>
public partial class NewVersion : Window
{
    public NewVersion(NewVersionVM viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += (s, e) => {
            this.DialogResult = e;
            this.Close();
        };
    }
    internal NewVersionVM ViewModel => (NewVersionVM)DataContext;
}
