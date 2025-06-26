using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace GPlus.UI.ViewsModels
{
    public partial class LocalizationVM: ObservableObject
    {
        private LocalizationVM() { }

        public LocalizationVM(Guid id, string name, List<IdentityVM> categories, IdentityVM? parameter,bool includeLinks, bool byValue, int? step)
        {
            Id = id;
            Name = name;
            Categories = categories;
            Parameter = parameter;
            IncludeLinks = includeLinks;
            ByValue = byValue;
            Step = step;
        }

        public Guid Id { get; init; }
        [ObservableProperty] private string _name;
        public List<IdentityVM> Categories { get; private set; }
        public IdentityVM? Parameter { get; private set; }

        public int? Step { get; private set; }
        public ObservableCollection<LocationItemVM> Items { get; set; } = new();
        [ObservableProperty] private bool _includeLinks;
        public bool ByValue { get; private set; }
        public event EventHandler<(string,object)> UpdateSchema;
        partial void OnNameChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
                UpdateSchema?.Invoke(this, (nameof(Name),value));
        }
        partial void OnIncludeLinksChanged(bool value)
        {
            UpdateSchema?.Invoke(this, (nameof(IncludeLinks), value));
        }

    }
}
