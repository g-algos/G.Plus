namespace GPlus.Base.Models
{
    public class DataLinkModel
    {
        public ElementId ViewId { get; set; }
        public string Link { get; set; }
        public bool IsInstance { get; set; }
        public DateTime LastSync { get; set; }
        public List<ElementId> Categories { get; set; }
        public bool IsRelative { get; set; }
    }
}
