using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;


namespace GPlus.UI.ViewsModels
{
    public partial class NewSharedParametersVM:ObservableObject
    {
        public NewSharedParametersVM(List<IdentityForgeVM> specTypes, List<NewSharedParamaterVM> parameters)
        {
            SpecTypes = specTypes.OrderBy(e=> e.Name).ToList();
            Parameters = new ObservableCollection<NewSharedParamaterVM>(parameters);
        }

        public List<IdentityForgeVM> SpecTypes { get; set; }
        [ObservableProperty] private NewSharedParamaterVM _selectedParameter;
        [ObservableProperty] private ObservableCollection<NewSharedParamaterVM> _parameters;

        public event EventHandler<bool?> RequestClose;
        [RelayCommand]
        private void OnRemove(NewSharedParamaterVM item)
        {
            if (item != null)
                Parameters.Remove(item);
        }

        [RelayCommand]
        private void OnCreateAndClose()
        {
            RequestClose?.Invoke(this, true);
        }
    }
}
