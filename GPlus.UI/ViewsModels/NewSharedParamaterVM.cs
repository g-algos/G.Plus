using CommunityToolkit.Mvvm.ComponentModel;

namespace GPlus.UI.ViewsModels
{
    public partial class NewSharedParamaterVM:ObservableObject
    {
        public string Name { get; set; }
        [ObservableProperty] private IdentityForgeVM? _specType;

        public NewSharedParamaterVM(string name)
        {
            Name = name;
        }
    }
}
