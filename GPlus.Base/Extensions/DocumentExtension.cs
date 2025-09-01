using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.DB.Mechanical;

namespace GPlus.Base.Extensions
{
    public static class DocumentExtension
    {
        public static string GetParameterName(this Document doc, ElementId parameterId)
        {
#if V2023
            var parameterid = parameterId.IntegerValue;
#else
            var parameterid = parameterId.Value;
#endif
            if (parameterid < 0)
            {
                BuiltInParameter parameter = (BuiltInParameter)parameterid;
                return LabelUtils.GetLabelFor(parameter);
            }
            return doc.GetElement(parameterId).Name;
        }

        public static string GetParameterName(this Document doc, ElementId parameterId, out StorageType storageType)
        {
#if V2023
            var parameterid = parameterId.IntegerValue;
#else
            var parameterid = parameterId.Value;
#endif
            if (parameterid < 0)
            {
                BuiltInParameter parameter = (BuiltInParameter)parameterid;
                storageType = doc.get_TypeOfStorage(parameter);
                return LabelUtils.GetLabelFor(parameter);
            }
            var element = doc.GetElement(parameterId);

            if (element == null)
                storageType = StorageType.None;


            if (element is ParameterElement parameterElement)
                storageType = GetTypeOfStorage(parameterElement.GetDefinition().GetDataType());
            else if (element is SharedParameterElement sParameterElement)
                storageType = GetTypeOfStorage(sParameterElement.GetDefinition().GetDataType());
            else
                storageType = StorageType.None;

            return element?.Name ?? "";
        }
        private static StorageType GetTypeOfStorage(ForgeTypeId specType)
        {
            if (UnitUtils.IsMeasurableSpec(specType))
                return StorageType.Double;
            else if (specType == SpecTypeId.Boolean.YesNo)
                return StorageType.Integer;
            else if (specType == SpecTypeId.String.Text
                  || specType == SpecTypeId.String.MultilineText
                  || specType == SpecTypeId.String.Url)
                return StorageType.String;
            else
                return StorageType.ElementId;
        }
        public static StorageType GetParameterType(this Document doc, ElementId parameterId)
        {
#if V2023
            var parameterid = parameterId.IntegerValue;
#else
            var parameterid = parameterId.Value;
#endif
            if (parameterid < 0)
            {
                BuiltInParameter parameter = (BuiltInParameter)parameterid;
                return doc.get_TypeOfStorage(parameter);
            }
            var element = doc.GetElement(parameterId);
            if (element == null)
                return StorageType.None;
            else if (element is ParameterElement parameterElement)
            {
                return doc.GetTypeOfStorage(parameterElement.GetDefinition().GetDataType());
            }
            else if (element is SharedParameterElement sParameterElement)
            {
                return doc.GetTypeOfStorage(sParameterElement.GetDefinition().GetDataType());
            }
            else
            {
                return StorageType.None;
            }
        }
        public static List<Category> GetCuttableCategories(this Document doc)
        {
            var categories = new List<Category>();
            foreach (Category category in doc.Settings.Categories)
            {
                if (category.BuiltInCategory == BuiltInCategory.INVALID)
                    continue;
                if (category.CategoryType == CategoryType.Model && category.IsCuttable && category.IsVisibleInUI && category.AllowsBoundParameters)
                {
                    categories.Add(category);
                }
            }
            return categories.OrderBy(c => c.Name).ToList();
        }
        public static List<FillPatternElement> GetfillPatterns(this Document doc)
        {
            return new FilteredElementCollector(doc)
                            .OfClass(typeof(FillPatternElement))
                            .Cast<FillPatternElement>()
                            .OrderBy(fp => fp.Name)
                            .ToList();
        }

        public static bool CreateRoom(this Document doc, Phase phase, out ElementId? elementId)
        {
            elementId = null;
            try
            {
                Room room = doc.Create.NewRoom(phase);
                elementId = room.Id;
                doc.Application.FailuresProcessing += new EventHandler<FailuresProcessingEventArgs>(DoFailureProcessing);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool CreateArea(this Document doc, ElementId areaSchemaId, ElementId levelId, out ElementId? elementId)
        {
            elementId = null;
            try
            {
                ViewPlan areaView = ViewPlan.CreateAreaPlan(doc, areaSchemaId, levelId);
                Area area = doc.Create.NewArea(areaView, new UV(0, 0));
                elementId = area.Id;
                doc.Regenerate();
                doc.Delete(areaView.Id);
                doc.Application.FailuresProcessing += new EventHandler<FailuresProcessingEventArgs>(DoFailureProcessing);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CreateSpace(this Document doc, Phase phase, out ElementId? elementId)
        {
            elementId = null;
            try
            {
                Space space = doc.Create.NewSpace(phase);
                elementId = space.Id;
                doc.Application.FailuresProcessing += new EventHandler<FailuresProcessingEventArgs>(DoFailureProcessing);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool CreateSheet(this Document doc, Phase phase, out ElementId? elementId)
        {
            elementId = null;
            try
            {
                ViewSheet sheet = ViewSheet.Create(doc, ElementId.InvalidElementId);
                elementId = sheet.Id;
                doc.Application.FailuresProcessing += new EventHandler<FailuresProcessingEventArgs>(DoFailureProcessing);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private static void DoFailureProcessing(object sender, FailuresProcessingEventArgs args)
        {
            FailuresAccessor fa = args.GetFailuresAccessor();
            fa.DeleteAllWarnings();
        }

        public static bool ValidParameter(this Document doc, ElementId parameterId)
        {
#if V2023
            var parameterid = parameterId.IntegerValue;
#else
            var parameterid = parameterId.Value;
#endif
            if( parameterid < 0)
            {
                BuiltInParameter? parameter = (BuiltInParameter)parameterid;
                return parameter!=null;
            }
            var element = doc.GetElement(parameterId);
            if(element != null &&( element is ParameterElement || element is SharedParameterElement))
            {
                return true;
            }
            return false;
        }
    }
}
