using Autodesk.Revit.DB.ExtensibleStorage;
using GPlus.Base.Models;
using System.Data;
using System.Text.Json;

namespace GPlus.Base.Schemas;
public static class VersioningSchema
{
    private static Guid Id = Guid.Parse(Resources.Identifiers.VersioningSchema);
    private const string Name = "GVersioning";

    private const bool IsRecording = false;
    private static IList<string> Versions = new List<string>();
    private static Schema Create() => SchemaManager.CreateSchema(Id, Name, new Dictionary<string, Type> {
        { nameof(IsRecording), IsRecording.GetType() }, 
        { nameof(Versions), Versions.GetType() },
    });
    public static List<VersioningModel> GetVersions(ProjectInfo projectInfo, out bool isRecording)
    {
        List<VersioningModel> versions = new();
        TryGetSchema(projectInfo, out versions, out isRecording);

        return versions;
    }
    public static bool TryGetSchema(ProjectInfo projectInfo, out List<VersioningModel> versions, out bool isRecording)
    {
        versions = new();
        isRecording = false;
        if (!SchemaManager.TryGetSchema(Id, out var _schema))
            _schema = Create();
        if (!SchemaManager.TryGetEntity(projectInfo, Id, out Entity? entity))
            return false;

        isRecording = entity!.Get<bool>(nameof(IsRecording));
        var items = entity.Get<IList<string>>(nameof(Versions));
        versions = items?.Select(e => JsonSerializer.Deserialize<VersioningModel>(e))?.ToList() ?? new();

        return true;
    }
    public static void SartRecording(Document doc)
    {
        var version = new List<VersioningModel>()
        {
            new VersioningModel()
            {
                Order = 0,
                VersionGuid = Guid.Empty,
                CreatedOn = DateTime.MinValue,
            },
            new VersioningModel()
            {
                Order = 1,
                VersionGuid = Document.GetDocumentVersion(doc).VersionGUID,
                CreatedOn = DateTime.Now,
            }
        };
        Entity entity = SchemaManager.AssignToElement(doc.ProjectInformation, Id,
            new Dictionary<string, object> 
            { 
                { nameof(IsRecording), true }, 
                { nameof(Versions),version.Select(e => JsonSerializer.Serialize(e)).ToList() },
            });
        doc.ProjectInformation.SetEntity(entity);
    }

    public static void StartNewVersion(Document doc)
    {
        if (!TryGetSchema(doc.ProjectInformation, out var versions, out var isRecording) || !isRecording)
            return;
        var newVersion = new VersioningModel()
        {
            Order = versions.Count,
            VersionGuid = Document.GetDocumentVersion(doc).VersionGUID,
            CreatedOn = DateTime.Now,
        };
        versions.Add(newVersion);
        Entity entity = SchemaManager.AssignToElement(doc.ProjectInformation, Id,
            new Dictionary<string, object>
            {
                { nameof(IsRecording), true },
                { nameof(Versions), versions.Select(e => JsonSerializer.Serialize(e)).ToList() },
            });
        doc.ProjectInformation.SetEntity(entity);
    }
    public static void RemoveSchema(ProjectInfo projectInfo)
    {
        if (!SchemaManager.TryGetSchema(Id, out var schema))
            return;
        projectInfo.DeleteEntity(schema);
    }

}
