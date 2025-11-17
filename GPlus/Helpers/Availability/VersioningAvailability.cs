using GPlus.Base.Models;
using GPlus.Base.Schemas;

namespace GPlus.Helpers.Availability;

public class VersioningAvailability : IExternalCommandAvailability
{
    public bool IsCommandAvailable(UIApplication appData, CategorySet selectedCategories)
    {
        if (appData?.ActiveUIDocument?.Document == null)
            return false; // not available with no document open

        if (appData.ActiveUIDocument.Document.IsFamilyDocument)
            return false; // not available for family documents

        List<VersioningModel> versions = new();
        if (!VersioningSchema.TryGetSchema(appData.ActiveUIDocument.Document.ProjectInformation, out versions, out bool isRecording) || isRecording == false || versions.Count() < 2)
            return false;

        return true;

    }
}
