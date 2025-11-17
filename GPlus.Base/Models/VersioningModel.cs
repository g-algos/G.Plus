namespace GPlus.Base.Models;

public class VersioningModel
{
    public Guid VersionGuid { get; set; }
    public double Order { get; set; }
    public DateTime CreatedOn { get; set; }
}
