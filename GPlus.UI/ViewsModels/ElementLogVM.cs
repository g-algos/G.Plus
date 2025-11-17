using CommunityToolkit.Mvvm.ComponentModel;
using GPlus.Base.Enums;

namespace GPlus.Base.Models;

public partial class ElementLogVM : ObservableObject
{
    public ElementId ElementId { get; set; }
    public string Category { get; set; }
    public string? Level { get; set; }
    public string Name { get; set; }
    [ObservableProperty] private ElementAction _action;

    [ObservableProperty] private bool _isSelected;
}
