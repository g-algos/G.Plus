using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace GPlus.UI.ViewsModels
{
    public partial class ApplyLocationSchemaVM : ObservableObject
    {
        public ObservableCollection<IdentityGuidVM> Localizations { get; set; } = new();
        [ObservableProperty] private IdentityGuidVM? _selectedLocalization;
        public ApplyLocationSchemaVM(List<IdentityGuidVM> localizations, IdentityGuidVM? selectedLocalization)
        {
            Localizations = new ObservableCollection<IdentityGuidVM>(localizations);
            SelectedLocalization = selectedLocalization;

        }

        public event EventHandler<Guid?> ApplySchema;
        [RelayCommand]
        void OnApplyClose()
        {
            ApplySchema?.Invoke(this, SelectedLocalization?.Id);
        }
    }
}
