using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace GPlus.UI.ViewsModels;
public partial class SelectDocumentVM : ObservableObject
{
    public ObservableCollection<IdentityGuidVM> Documents { get; set; } = new();
    [ObservableProperty] private IdentityGuidVM? _selectedDocument;

    public SelectDocumentVM(List<IdentityGuidVM> documents, IdentityGuidVM? selectedDocument)
    {
        Documents = new ObservableCollection<IdentityGuidVM>(documents);
        SelectedDocument = selectedDocument;

    }

    public event EventHandler<Guid?> SelectDocument;
    [RelayCommand]
    void OnApplyClose()
    {
        SelectDocument?.Invoke(this, SelectedDocument?.Id);
    }
}
