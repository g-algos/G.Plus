using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Color = Autodesk.Revit.DB.Color;

namespace GPlus.UI.ViewsModels
{
    public partial class LocationItemVM: ObservableObject
    {
        public LocationItemVM(string value, Color color, FillPatternImageVM? fillPattern)
        {
            this.Value = value;
            this.Color = color;
            this.FillPattern = fillPattern;
        }

        public string Value { get; private set; }
        [ObservableProperty] private Color _color;
        [ObservableProperty] private FillPatternImageVM? _fillPattern;

        public event EventHandler<(string, object)> UpdateItem;
        [RelayCommand]
        private void OnPickColor(object param)
        {
            if (param == null) return;

            var item = param;
            var prop = item.GetType().GetProperty(nameof(LocationItemVM.Color));
            if (prop == null) return;

            var dialog = new ColorDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var selected = dialog.Color;
                var revitColor = new Color(selected.R, selected.G, selected.B);
                prop.SetValue(item, revitColor);
                UpdateItem?.Invoke(this, (nameof(LocationItemVM.Color), revitColor));
            }
        }

        [RelayCommand]
        private void OnPickFillPattern(object? selected)
        {
            if (selected is FillPatternImageVM pattern)
            {
                FillPattern = pattern;
                UpdateItem?.Invoke(this, (nameof(LocationItemVM.FillPattern), pattern));
            }
        }
    }
}
