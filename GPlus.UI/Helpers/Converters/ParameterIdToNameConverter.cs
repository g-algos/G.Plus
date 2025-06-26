using GPlus.Base.Models;
using System.Globalization;
using System.Windows.Data;

namespace GPlus.UI.Helpers.Converters
{
    [Obsolete]
    public class ParameterIdToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "None";
            ElementId element = (ElementId)value;
#if V2023
            var id = element.IntegerValue;
#else
            var id = element.Value;
#endif
            if (id < 0)
                return LabelUtils.GetLabelFor((BuiltInParameter)id);
            else
                return ActiveCommandModel.Document.GetElement(element)?.Name ?? "Unknown Element";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
