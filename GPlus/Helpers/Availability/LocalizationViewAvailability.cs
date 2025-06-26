using GPlus.Base.Schemas;

namespace GPlus.Helpers.Availability
{
    public class LocalizationViewAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appData, CategorySet selectedCategories)
        {
            var view = appData.ActiveUIDocument?.ActiveView;

            if (view == null)
                return false;

            bool isValid = view is View3D || view is ViewPlan || view is ViewSection || view is ViewDrafting;
            if (!isValid)
                return false;
            return ViewLocationSchema.HasSchema(view);
        }
    }
}
