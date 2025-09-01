using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPlus.UI.Views;
using System.Collections.ObjectModel;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace GPlus.UI.ViewsModels;

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

    public Func<LocalizationVM,(bool, string)> RemoveSchema;
    public Func<NewLocationVM, (bool, string)> AddSchema;
    public Func<LocalizationVM, (bool, string)> RefreshSchema;
    public Func<(string,object), (bool, string)> UpdateSchema;

    [RelayCommand]
    private void OnAddSchema()
    {
        var viewModel = new NewLocationVM();
        var window = new NewLocationView(viewModel);
        var result = window.ShowDialog();
        if (result == true)
        {
            (bool success, string message) response = AddSchema?.Invoke(viewModel) ?? (false, "");
            if (!response.success)
                MessageBox.Show(
                    response.message,
                    Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
        }

    }

    [RelayCommand]
    public void OnDeleteSchema()
    {
        if (SelectedSchema == null) return;
        var result = MessageBox.Show(
            string.Format(Base.Resources.Localizations.Messages.DeleteSchemaConfirmation, new[] { SelectedSchema.Name }),
            Base.Resources.Localizations.Content.DeleteSchema,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );
        if (result == MessageBoxResult.Yes)
        {
            (bool success, string message) response = RemoveSchema?.Invoke(SelectedSchema) ?? (false, "");
            if (!response.success)
                MessageBox.Show(
                    response.message,
                    Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
        }
    }

    [RelayCommand]
    public void OnRefreshSchema()
    {
        if (SelectedSchema == null) return;
    }

    private void OnUpdateSchema((string, object) param)
    {
        if (SelectedSchema == null || param.Item1 == null) return;
        (bool success, string message) response = UpdateSchema?.Invoke(param) ?? (false, "");
        if (!response.success)
            MessageBox.Show(
                response.message,
                Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
    }
}
