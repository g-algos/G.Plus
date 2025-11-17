using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GPlus.Base.Enums;
using GPlus.Base.Models;
using System.Collections.ObjectModel;

namespace GPlus.UI.ViewsModels;

public partial class LogsVM : ObservableObject
{
    public string CurrentDocument {get;set;}
    public ObservableCollection<VersionVM> Versions { get; set; } = new();
    [ObservableProperty] private VersionVM _selectedVersion;
    [ObservableProperty] private ObservableCollection<ElementLogVM> _logs;

    public LogsVM(List<VersionVM> versions, string currentDocument)
    {
        Logs = new ObservableCollection<ElementLogVM>();
        Versions = new ObservableCollection<VersionVM>(versions);
        CurrentDocument = currentDocument;
    }
    partial void OnSelectedVersionChanged(VersionVM value)
    {
        List<ElementLogVM> logs = new();
        //TODO move this to the command
        var ids = ActiveCommandModel.Document.GetChangedElements(value.Id);
        foreach (var id in ids.GetModifiedElementIds())
        {
            var element = ActiveCommandModel.Document.GetElement(id);
            if(element == null)
                continue;
            var category = element.Category;
            if (category == null ||!category.IsValid || !category.CanAddSubcategory || !category.IsVisibleInUI || category.CategoryType != CategoryType.Model)
                continue;
            logs.Add(new ElementLogVM()
            {
                ElementId = id,
                Action = ElementAction.Edited,
                Category = category.Name,
                Name = element.Name,
                Level = ActiveCommandModel.Document.GetElement(element.LevelId)?.Name,
            });
        }
        foreach (var id in ids.GetCreatedElementIds())
        {
            var element = ActiveCommandModel.Document.GetElement(id);
            if (element == null)
                continue;
            var category = element.Category;
            if (category ==null || !category.IsValid || !category.IsVisibleInUI || category.CategoryType != CategoryType.Model)
                continue;
            logs.Add(new ElementLogVM()
            {
                ElementId = id,
                Action = ElementAction.Created,
                Category = category.Name,
                Name = element.Name,
                Level = ActiveCommandModel.Document.GetElement(element.LevelId)?.Name,
            });
        }
        foreach (var id in ids.GetDeletedElementIds())
        {
            logs.Add(new ElementLogVM()
            {
                ElementId = id,
                Action = ElementAction.Deleted,
                Category = "",
                Name = "",
                Level = "",
            });
        }
        Logs = new ObservableCollection<ElementLogVM>(logs);
    }
    public event EventHandler<Tuple<VersionVM, List<ElementLogVM>>> Export;
    public event EventHandler<Tuple<VersionVM, List<ElementLogVM>>> View;
    public event EventHandler<bool> Close;
    [RelayCommand]
    void OnExport()
    {
        Export?.Invoke(this, new Tuple<VersionVM, List<ElementLogVM>>(SelectedVersion, Logs.ToList()));
    }

    [RelayCommand]
    void OnView()
    {
        View?.Invoke(this, new Tuple<VersionVM, List<ElementLogVM>>(SelectedVersion, Logs.ToList()));
    }

    [RelayCommand]
    void OnClose()
    {
        Close?.Invoke(this, true);
    }
}

