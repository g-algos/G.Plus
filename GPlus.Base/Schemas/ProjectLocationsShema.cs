using Autodesk.Revit.DB.ExtensibleStorage;
using GPlus.Base.Models;
using View = Autodesk.Revit.DB.View;

namespace GPlus.Base.Schemas
{
    public static class ProjectLocationsShema
    {
        private static Guid Id = Guid.Parse(Resources.Identifiers.ProjectLocalizationsSchema);
        private const string Name = "GProjectLocalizations";
        public static IList<Guid> Values = new List<Guid>();

        public static Schema Create() => SchemaManager.CreateSchema(Id, Name, new Dictionary<string, Type> { { nameof(Values), Values.GetType() } });

        public static List<LocalizationModel> GetLocalizations(ProjectInfo project)
        {
            List<LocalizationModel> localizations = new();

            if (!SchemaManager.TryGetEntity(project, Id, out var entity))
                return null;

            var localizationsIds = entity.Get<IList<Guid>>(nameof(Values));
            foreach(Guid id in localizationsIds)
            {
                if (!SchemaManager.TryGetSchema(Id, out var _schema))
                    continue;
                var model = LocationSchema.GetModel(id, project);
                localizations.Add(model);
            }
            return localizations;
        }
            
        public static void AddLocalizationModel(ProjectInfo project, Guid localization)
        {
            if (!SchemaManager.TryGetSchema(localization, out var locSchema))
                return;
            if (!SchemaManager.TryGetEntity(project, Id, out var entity))
                entity = new Entity(Id);


            IList<Guid> values = new List<Guid>();

            var localizationsIds = entity.Get<IList<Guid>>(nameof(Values));
            localizationsIds.Add(localization);
            entity.Set<IList<Guid>>(nameof(Values), localizationsIds);
            project.SetEntity(entity);
        }
        public static void RemoveLocalizationModel(ProjectInfo project, Guid localization)
        {
            if (!SchemaManager.TryGetSchema(Id, out var locSchema))
                return;
            if (!SchemaManager.TryGetEntity(project, Id, out var entity))
                return;

            IList<Guid> values = new List<Guid>();
            var localizationsIds = entity.Get<IList<Guid>>(nameof(Values));
            localizationsIds.Remove(localization);
            entity.Set<IList<Guid>>(nameof(Values), localizationsIds);
            project.SetEntity(entity);
            var viewCollector = new FilteredElementCollector(project.Document)
                .OfClass(typeof(View))
                .WhereElementIsNotElementType()
                .Where(e=> e is View3D || e is ViewPlan || e is ViewSection || e is ViewDrafting)
                .Cast<View>();
            foreach (var view in viewCollector)
            {
                if (ViewLocationSchema.TryGetLocalization(view, out var locModel) && locModel.Id == localization)
                {
                    ViewLocationSchema.RemoveLocalization(view);
                }
            }
        }
    }
}
