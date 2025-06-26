namespace GPlus.Helpers.Availability
{
    public class ModelViewAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appData, CategorySet selectedCategories)
        {
            var view = appData.ActiveUIDocument?.ActiveView;

            if (view == null)
                return false;

            return view is View3D
                || view is ViewPlan
                || view is ViewSection
                || view is ViewDrafting;
        }
    }
}
