using Autodesk.Revit.Attributes;
using GPlus.Base.Models;
using GPlus.Base.Schemas;

namespace GPlus.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class RefreshViewLocationSchemaCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements
        )
        {
            ActiveCommandModel.Set(commandData.Application);
            if (!ViewLocationSchema.TryGetLocalization(ActiveCommandModel.View, out LocalizationModel? localizationModel))
                return Result.Cancelled;
            using (Transaction transaction = new Transaction(ActiveCommandModel.Document, "Manage Localization Schemas"))
            {
                transaction.Start();
                try
                {
                    bool isValid = ActiveCommandModel.View is View3D || ActiveCommandModel.View is ViewPlan || ActiveCommandModel.View is ViewSection || ActiveCommandModel.View is ViewDrafting;
                    if(!isValid)
                        return Result.Cancelled;
                    ViewLocationSchema.Refresh(ActiveCommandModel.View, localizationModel);

                    transaction.Commit();
                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    var dialog = new TaskDialog(Base.Resources.Localizations.Messages.OOOps)
                    {
                        MainInstruction = Base.Resources.Localizations.Messages.Error,
                        MainContent = ex.Message,
                        CommonButtons = TaskDialogCommonButtons.Close
                    };
                    dialog.Show();
                    return Result.Failed;
                }
            }
        }
    }
}
