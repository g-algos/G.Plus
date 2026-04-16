using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GPlus.UI.ViewsModels;

public partial class NewVersionVM: ObservableObject
{
    public double CurrentVersion { get; init; }
    [ObservableProperty] private bool _isMajor;
    public event EventHandler<bool?> RequestClose;

    public NewVersionVM(double currentVersion, bool isMajor = true)
    {
        CurrentVersion = currentVersion;
        IsMajor = isMajor;
    }

    [RelayCommand]
    void OnClose() => RequestClose?.Invoke(this, true);
}
