namespace GPlus.Helpers.Availability;

public class DocumentAvailability : IExternalCommandAvailability
{
    public bool IsCommandAvailable(UIApplication appData, CategorySet selectedCategories)
    {
        if (appData?.ActiveUIDocument?.Document == null)
            return false; // not available with no document open

        if (appData.ActiveUIDocument.Document.IsFamilyDocument)
            return false; // not available for family documents

        return true;

    }
}
