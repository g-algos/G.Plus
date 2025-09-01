namespace GPlus.Base.Models
{
    public class LocalizationModel : IEquatable<LocalizationModel>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public HashSet<ElementId> Categories { get; set; }
        public ElementId Parameter { get; set; }
        public bool Valid { get; set; }
        public bool ByValue { get; set; }
        public bool IncludeElementsFromLinks { get; set; }
        public int? Step { get; set; }
        public HashSet<LocalizationItemModel> Items { get; set; }
        public bool Equals(LocalizationModel? other) => Id == other?.Id;
    }
}
