using GPlus.Base.Enums;
using System.Globalization;
using System.Windows.Data;

namespace GPlus.UI.Helpers.Converters;

public class ActionToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ElementAction action)
        {
            return action switch
            {
                ElementAction.Created => Brushes.LightGreen,
                ElementAction.Edited => Brushes.LightBlue,
                ElementAction.Deleted => Brushes.LightCoral,
                _ => Brushes.Transparent
            };
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
