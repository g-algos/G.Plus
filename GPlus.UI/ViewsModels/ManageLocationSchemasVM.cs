using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPlus.UI.Views;
using System.Collections.ObjectModel;

namespace GPlus.UI.ViewsModels
{
    public partial class ManageLocationSchemasVM : ObservableObject
    {
        public ManageLocationSchemasVM(List<LocalizationVM> allSchemas, LocalizationVM? selectedSchema, List<FillPatternImageVM> fillPatterns)
        {
            FillPatterns = fillPatterns;
            AllSchemas = allSchemas.OrderBy(e => e.Name).ToList();
            LocalizationSchemas = new ObservableCollection<LocalizationVM>(AllSchemas);
            SelectedSchema = selectedSchema;
        }
        public List<FillPatternImageVM> FillPatterns { get; set; }
        public List<LocalizationVM> AllSchemas { get; set; }
        [ObservableProperty] private ObservableCollection<LocationItemVM> _localizationSchemaItems;
        public ObservableCollection<LocalizationVM> LocalizationSchemas { get; set; }

        [ObservableProperty] private LocalizationVM? _selectedSchema;
        [ObservableProperty] private string _searchText = string.Empty;
        partial void OnSelectedSchemaChanged(LocalizationVM value)
        {
            value.UpdateSchema += (s, e) => { OnUpdateSchema(e); };
            var orderedItems = value.Items.OrderBy(e => e.Value).ToList();
            LocalizationSchemaItems = new ObservableCollection<LocationItemVM>(orderedItems);
        }

        partial void OnLocalizationSchemaItemsChanged(ObservableCollection<LocationItemVM> value)
        {
            foreach (var v in value)
            {
                v.UpdateItem += (s, e) => { OnUpdateSchema((nameof(LocalizationVM.Items), LocalizationSchemaItems.ToList())); };
            }
        }
        partial void OnSearchTextChanged(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                LocalizationSchemas = new ObservableCollection<LocalizationVM>(AllSchemas);
            }
            else
            {
                LocalizationSchemas = new ObservableCollection<LocalizationVM>(AllSchemas.Where(ls => ls.Name.ToLowerInvariant().StartsWith(value!.ToLowerInvariant())));
            }
        }

        public event EventHandler<LocalizationVM> RemoveSchema;
        public event EventHandler<NewLocationVM> AddSchema;
        public event EventHandler<LocalizationVM> RefreshSchema;
        public event EventHandler<(string,object)> UpdateSchema;

        [RelayCommand]
        private void OnAddSchema()
        {
            var viewModel = new NewLocationVM();
            var window = new NewLocationView(viewModel);
            var result = window.ShowDialog();
            if (result == true)
            {
                AddSchema?.Invoke(this, viewModel);
            }

        }

        [RelayCommand]
        public void OnDeleteSchema()
        {
            if (SelectedSchema == null) return;
            var result = MessageBox.Show(
                $"Are you sure you want to delete the schema '{SelectedSchema.Name}'?",
                "Delete Schema",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );
            if (result == DialogResult.Yes)
                RemoveSchema?.Invoke(this, SelectedSchema);
        }

        [RelayCommand]
        public void OnRefreshSchema()
        {
            if (SelectedSchema == null) return;
        }

        private void OnUpdateSchema((string, object) param)
        {
            if (SelectedSchema == null || param.Item1 == null) return;
            UpdateSchema?.Invoke(this, param);
        }
    }
}
