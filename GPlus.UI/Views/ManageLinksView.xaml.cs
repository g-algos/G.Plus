using GPlus.UI.ViewsModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace GPlus.UI.Views
{
    /// <summary>
    /// Interaction logic for ManageLinksWindow.xaml
    /// </summary>
    public partial class ManageLinksView : Window
    {
        public ManageLinksView(DataLinksVM viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
        internal DataLinksVM ViewModel => (DataLinksVM)DataContext;
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is DataLinksVM vm && e.PropertyName == nameof(vm.LinkToScroll))
            {
                if (vm.LinkToScroll is ScheduleLinkVM item)
                {
                    LocalizationItems.Dispatcher.BeginInvoke(
                        () => LocalizationItems.ScrollIntoView(item),
                        DispatcherPriority.Background);

                    vm.LinkToScroll = null;
                }
            }
        }
    }
}
