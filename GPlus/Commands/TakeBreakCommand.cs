using Autodesk.Revit.Attributes;

namespace GPlus.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class TakeBreakCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements
        )
        {
            TakeABreak window = new TakeABreak();
            window.ShowDialog();
            return Result.Succeeded;
        }
    }
}
