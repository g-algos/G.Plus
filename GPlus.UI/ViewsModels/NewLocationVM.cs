using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPlus.Base.Extensions;
using GPlus.Base.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace GPlus.UI.ViewsModels
{
    public partial class NewLocationVM : ObservableObject
    {
          public NewLocationVM()
        {
            Id = Guid.NewGuid();
            AllCategories = ActiveCommandModel.Document.GetCuttableCategories()
                .Select(e => new IdentityVM() { Id = e.Id, Name = e.Name })
                .ToList();
            SelectedCategories = [AllCategories.FirstOrDefault()];
            SelectedCategories.CollectionChanged += SelectedCategories_CollectionChanged;
            ByValue = true;
            UpdateParameters();
        }
        public Guid Id { get; init; }
        [ObservableProperty] private string _name;
        [ObservableProperty] private bool _includeLinks;
        [ObservableProperty] private bool _byValue;
        [ObservableProperty] private int _step;
        public List<IdentityVM> AllCategories { get; set; }
        [ObservableProperty] private ObservableCollection<IdentityVM> _selectedCategories;
        [ObservableProperty] private List<ParameterIdentityVM>? _allParameters;
        [ObservableProperty] private ParameterIdentityVM? _selectedParameter;

        public event EventHandler<bool?> RequestClose;

        partial void OnSelectedParameterChanged(ParameterIdentityVM? value)
        {
            if(value == null)
            {
                return;
            }
            if (!(value.StorageType == StorageType.Double))
            {
                ByValue = true;
            }
        }
        private void SelectedCategories_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateParameters();
        }
        partial void OnSelectedCategoriesChanged(ObservableCollection<IdentityVM> value)
        {
            if (_selectedCategories != null)
                _selectedCategories.CollectionChanged -= SelectedCategories_CollectionChanged;

            SelectedCategories = value ?? new ObservableCollection<IdentityVM>();
            SelectedCategories.CollectionChanged += SelectedCategories_CollectionChanged;
        }
        private void UpdateParameters()
        {
            if (SelectedCategories == null || !SelectedCategories.Any())
            {
                AllParameters = new List<ParameterIdentityVM>();
                SelectedParameter = null;
                return;
            }

            //RULE: even if we have an include elemens from links, the parameters need to be in the current document.
            AllParameters = ParameterFilterUtilities.GetFilterableParametersInCommon(ActiveCommandModel.Document, SelectedCategories.Select(e => e.Id).ToList())
            .Select(p => new ParameterIdentityVM
            {
                Id = p,
                Name = ActiveCommandModel.Document.GetParameterName(p, out StorageType storageType),
                StorageType = storageType
            })
            .OrderBy(e => e.Name)
            .ToList();


            if (SelectedParameter != null && !AllParameters.Any(e => e.Id == SelectedParameter.Id))
                SelectedParameter = null;
        }
        [RelayCommand]
        void OnCreateAndClose()
        {
            if (string.IsNullOrEmpty(Name))
            {
                MessageBox.Show(
                    Base.Resources.Localizations.Messages.LocNameMissing,
                    Base.Resources.Localizations.Messages.Wait,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }
            if (SelectedParameter == null)
            {
                MessageBox.Show(
                      Base.Resources.Localizations.Messages.LocParamMissing,
                      Base.Resources.Localizations.Messages.Wait,
                      MessageBoxButton.OK,
                      MessageBoxImage.Warning
                  );
                return;
            }
            if (!SelectedCategories.Any())
            {
                MessageBox.Show(
                      Base.Resources.Localizations.Messages.LocCategorieMissing,
                      Base.Resources.Localizations.Messages.Wait,
                      MessageBoxButton.OK,
                      MessageBoxImage.Warning
                  );
                return;
            }

            RequestClose?.Invoke(this, true);
        }

    }
}
