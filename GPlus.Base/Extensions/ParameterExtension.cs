using System.Globalization;

namespace GPlus.Base.Extensions
{
    public static class ParameterExtension
    {
        public static object? GetValue(this Parameter parameter)
        {
            if (!parameter.HasValue) return null;
            var forgeTypeId = parameter.Definition.GetDataType();

            switch (parameter.StorageType)
            {
                case StorageType.String:
                    string asString = parameter.AsString();
                    return asString == "null" ? string.Empty : asString;
                case StorageType.ElementId:
                    return parameter.AsValueString();
                case StorageType.Integer:
                    if (forgeTypeId == SpecTypeId.Boolean.YesNo) return parameter.AsInteger() != 0;
                    if (forgeTypeId == SpecTypeId.Number) return parameter.AsInteger();
                    else return parameter.AsValueString();
                case StorageType.Double:
                    double asDouble = parameter.AsDouble();
                    return UnitUtils.ConvertFromInternalUnits(asDouble, parameter.GetUnitTypeId());
                default:
                    return parameter.AsValueString();
            }
        }

        internal static void SetValue(this Parameter parameter, object? value, ForgeTypeId? unitTypeId = null)
        {
            if (parameter.IsReadOnly) return;
            //if(parameter.UserModifiable == false) return;
            var forgeTypeId = parameter.Definition.GetDataType();
            switch (parameter.StorageType)
            {
                case StorageType.String:
                    parameter.Set(value?.ToString() ?? string.Empty); // TODO: check if Revit accepts empty strings
                    break;
                case StorageType.ElementId:
#if V2023
                    parameter.Set(new ElementId(Convert.ToInt32(value, CultureInfo.InvariantCulture)));
#else
                    parameter.Set(new ElementId(Convert.ToInt64(value, CultureInfo.InvariantCulture)));
#endif
                    break;
                case StorageType.Integer:
                    parameter.Set(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                    break;
                case StorageType.Double:
                    double asDouble = UnitUtils.ConvertToInternalUnits(Convert.ToDouble(value, CultureInfo.InvariantCulture), parameter.GetUnitTypeId());
                    parameter.Set(asDouble);
                    break;
                default:
                    break;
            }
        }

    }
}
