using GPlus.Base.Enums;

namespace GPlus.Base.Models;

public class ElementLog
{
    public ElementId ElementId { get; set; }
    public string Category { get; set; }
    public string? Level { get; set; }
    public string Name { get; set; }
    public ElementAction Action { get; set; }
}
