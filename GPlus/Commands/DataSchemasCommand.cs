using Autodesk.Revit.Attributes;
using GPlus.Base.Models;

namespace GPlus.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class DataSchemasCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements
        )
        {
            ActiveCommandModel.Set(commandData.Application);
            return Result.Succeeded;
        }
    }
}
