using GPlus.UI.ViewsModels;
using System.Windows;

namespace GPlus.UI.Views;

/// <summary>
/// Interaction logic for LogsView.xaml
/// </summary>
/// 
public partial class LogsView : Window
{
    public LogsView(LogsVM viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
    internal LogsVM ViewModel => (LogsVM)DataContext;
}
