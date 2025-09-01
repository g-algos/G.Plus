using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using GPlus.Base.Extensions;
using GPlus.Base.Models;
using System.Windows;
using View = Autodesk.Revit.DB.View;

namespace GPlus.Base.Schemas
{
    public static class ViewLocationSchema
    {
        private static Guid Id = Guid.Parse(Resources.Identifiers.ViewLocalizationLinkSchema);
        private const string Name = "GViewLocalizationSchema";
        public static Schema Create()=> SchemaManager.CreateSchema(Id, Name, new Dictionary<string, Type> { { nameof(LocationSchema), typeof(Guid) } });
        public static bool HasSchema(View view)=> SchemaManager.TryGetEntity(view, Id, out Entity? entity);

        public static bool TryGetLocalization(View view, out LocalizationModel? localization)
        {
            localization = null;
            if (!SchemaManager.TryGetSchema(Id, out Schema? schema))
                return false;
            if (!SchemaManager.TryGetEntity(view, Id, out Entity? entity))
                return false;
            if (entity == null || !entity.IsValid())
                return false;

            var locSchema = entity.Get<Guid>(nameof(LocationSchema));
            if (locSchema == null)
                return false;

            localization = LocationSchema.GetModel(locSchema, view.Document.ProjectInformation);
            return true;
        }
        public static void RemoveLocalization(View view)
        {
            if (!SchemaManager.TryGetSchema(Id, out Schema? schema))
                return;
            view.DeleteEntity(schema);
            var doc = view.Document;
            var collector = new FilteredElementCollector(doc, view.Id)
                .WhereElementIsNotElementType()
                .Where(e=> e.Category.CategoryType == CategoryType.Model && e.Category.IsCuttable && e.Category.IsVisibleInUI && e.Category.AllowsBoundParameters)
                .Select(e=> e.Id);

            foreach (var id in collector)
            {
                try
                {
                    view.SetElementOverrides(id, new OverrideGraphicSettings());
                }
                catch{ }
            }
        }
        public static void SetLocalization(View view, Guid localization)
        {
            if (!SchemaManager.TryGetSchema(Id, out Schema? schema))
                return;
            Entity entity = new Entity(Id);
            entity.Set(nameof(LocationSchema), localization);
            view.SetEntity(entity);
        }
        public static void Refresh(View view, LocalizationModel localization) 
        {
            try
            {
                List<Element> elements = new();
                List<Document> documents = new() { view.Document };

                if(!localization.Valid)
                    throw new InvalidOperationException(Resources.Localizations.Messages.LocalizationNotValid);
                if (localization!.IncludeElementsFromLinks)
                {
                    documents.AddRange(new FilteredElementCollector(view.Document).OfCategory(BuiltInCategory.OST_RvtLinks)
                        .WhereElementIsNotElementType()
                        .Select(e => (e as RevitLinkInstance).GetLinkDocument())
                        .ToList());
                }
                foreach (Document doc in documents)
                {
                    try
                    {
                        foreach (ElementId category in localization.Categories)
                        {
                            elements.AddRange(new FilteredElementCollector(doc, view.Id).OfCategoryId(category).WhereElementIsNotElementType().ToElements().ToList());
                        }
                    }
                    catch { }
                }

                bool isTypeParameter = false;

                // Detecta onde está o parâmetro (tipo ou instância) apenas uma vez
                var firstElement = elements.FirstOrDefault();
                Parameter? testParam = firstElement?.GetParameter(localization.Parameter);
                if (testParam == null || !testParam.HasValue)
                {
                    var type = firstElement?.Document.GetElement(firstElement.GetTypeId());
                    if (type != null)
                    {
                        testParam = type.GetParameter(localization.Parameter);
                        if (testParam != null)
                            isTypeParameter = true;
                    }
                }

                if (localization.ByValue)
                {

                    foreach (Element element in elements)
                    {
                        try
                        {
                            
                            var ogs = new OverrideGraphicSettings();
                            Parameter? parameter = isTypeParameter
                                ? element.Document.GetElement(element.GetTypeId())?.GetParameter(localization.Parameter)
                                : element.GetParameter(localization.Parameter);

                            if (parameter == null || !parameter.HasValue)
                                continue;

                            var value = parameter.GetValue();
                            if (value == null)
                                continue;

                            var item = localization.Items.FirstOrDefault(e => e.Value == value.ToString());
                            if (item == null)
                                continue;

                            if (view is View3D)
                            {
                                ogs.SetSurfaceForegroundPatternColor(item.Color);
                                ogs.SetSurfaceForegroundPatternId(new ElementId(item.FillPattern));
                            }
                            else
                            {
                                ogs.SetCutBackgroundPatternColor(item.Color);
                                ogs.SetCutBackgroundPatternId(new ElementId(item.FillPattern));
                            }

                            view.SetElementOverrides(element.Id, ogs);
                        }
                        catch { }
                    }
                }
                else
                {
                    var ranges = localization.Items
                        .Select(value=>
                        {
                            var min = double.Parse(value.Value.Split(":")[0]);
                            var max = double.Parse(value.Value.Split(":")[1]);
                        return new { Min = min, Max = max, Color = value.Color, FillPattern = value.FillPattern};

                        });
                    foreach (Element element in elements)
                    {
                        try
                        {
                            var ogs = new OverrideGraphicSettings();

                            Parameter? parameter = isTypeParameter
                                ? element.Document.GetElement(element.GetTypeId())?.GetParameter(localization.Parameter)
                                : element.GetParameter(localization.Parameter);

                            if (parameter == null || !parameter.HasValue)
                                continue;

                            if (!(parameter.StorageType == StorageType.Integer || parameter.StorageType == StorageType.Double))
                                break;
                            double value = parameter.StorageType == StorageType.Integer? parameter.AsInteger():  parameter.AsDouble();
                            if (value == null)
                                continue;

                            var item = ranges.FirstOrDefault(e => value >= e.Min && value <= e.Max);
                            if (item == null)
                                continue;

                            if (view is View3D)
                            {
                                ogs.SetSurfaceForegroundPatternColor(item.Color);
                                ogs.SetSurfaceForegroundPatternId(new ElementId(item.FillPattern));
                            }
                            else
                            {
                                ogs.SetCutBackgroundPatternColor(item.Color);
                                ogs.SetCutBackgroundPatternId(new ElementId(item.FillPattern));
                            }

                            view.SetElementOverrides(element.Id, ogs);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

    }
}
