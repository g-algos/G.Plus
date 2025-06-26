using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB.ExtensibleStorage;
using GPlus.Base.Schemas;

namespace GPlus.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class QuickCommand
       : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            using (Transaction transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "Quick Command"))
            {
                transaction.Start();
                TrialSchema.Create();
                TrialSchemaCollection.Create();
                transaction.Commit();

            }
            using (Transaction transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "Quick Command"))
            {
                transaction.Start();
                var schemas = Schema.ListSchemas();
                foreach (var schema in schemas)
                {
                    if (schema.VendorId == "ETC-TEC")
                    {
                        var entity = doc.ProjectInformation.GetEntity(schema);
                        if (entity != null) doc.ProjectInformation.DeleteEntity(schema);
                    }
                }
                transaction.Commit();

            }

            return Result.Succeeded;
        }
    }
}
