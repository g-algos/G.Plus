using CommunityToolkit.Mvvm.ComponentModel;

namespace GPlus.UI.ViewsModels
{
    public partial class ScheduleLinkVM: ObservableObject
    {
        [ObservableProperty] private IdentityVM _schedule;
        [ObservableProperty] private bool _status;
        [ObservableProperty] private string _path;
        [ObservableProperty] private string _pathType;
        [ObservableProperty] private DateTime _lastSync;
        [ObservableProperty] private string _linkType;
    }
}
