namespace GPlus.Base.Models
{
    public class LocalizationItemModel: IEquatable<LocalizationItemModel>
    {

        public LocalizationItemModel(string value, Autodesk.Revit.DB.Color color,
#if V2023
            int fillPattern)
#else
            long fillPattern)
#endif
        {
            Value = value;
            Color = color;
            FillPattern = fillPattern;
        }

        public string Value { get; set; }
        public Autodesk.Revit.DB.Color Color { get; set; }
#if V2023
        public int FillPattern { get; set; }
#else
        public long FillPattern { get; set; }
#endif
        public bool Equals(LocalizationItemModel? other) => Value == other?.Value;
    }
}
