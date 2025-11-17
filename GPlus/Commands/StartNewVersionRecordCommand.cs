using Autodesk.Revit.Attributes;
using GPlus.Base.Models;
using GPlus.Base.Schemas;
using System.Windows;

namespace GPlus.Commands;

[Transaction(TransactionMode.Manual)]
public class StartNewVersionRecordCommand : IExternalCommand
{
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements
    )
    {
        ActiveCommandModel.Set(commandData.Application);
        try
        {
            var taskDialog = new TaskDialog(Base.Resources.Localizations.Messages.Wait);
            if (ActiveCommandModel.Document.IsWorkshared)
                taskDialog.MainContent = String.Format(Base.Resources.Localizations.Messages.SaveSync, Base.Resources.Localizations.Content.Synchronized);
            else
                taskDialog.MainContent = String.Format(Base.Resources.Localizations.Messages.SaveSync, Base.Resources.Localizations.Content.Saved);

            taskDialog.CommonButtons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel;

            var result = taskDialog.Show();
            if (result == TaskDialogResult.Cancel)
                return Result.Cancelled;

            if (ActiveCommandModel.Document.IsWorkshared)
                ActiveCommandModel.Document.SynchronizeWithCentral(new TransactWithCentralOptions(), new SynchronizeWithCentralOptions() { SaveLocalAfter = true });
            else
                ActiveCommandModel.Document.Save();

            using (Transaction transaction = new Transaction(ActiveCommandModel.Document, "SaveVersion"))
            {
                try
                {
                    transaction.Start();

                    if (!VersioningSchema.TryGetSchema(ActiveCommandModel.Document.ProjectInformation, out List<VersioningModel> versions, out bool isRecording))
                        VersioningSchema.SartRecording(ActiveCommandModel.Document);
                    else
                    {
                        var lastVersion = versions.OrderByDescending(e => e.Order).FirstOrDefault();
                        if (lastVersion != null && lastVersion.VersionGuid == Document.GetDocumentVersion(ActiveCommandModel.Document).VersionGUID)
                            return Result.Succeeded;
                        VersioningSchema.StartNewVersion(ActiveCommandModel.Document);
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {

                    MessageBox.Show(
                        ex.Message,
                        Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    transaction.RollBack();
                    return Result.Failed;
                }
            }
            if (ActiveCommandModel.Document.IsWorkshared)
                ActiveCommandModel.Document.SynchronizeWithCentral(new TransactWithCentralOptions(), new SynchronizeWithCentralOptions() { SaveLocalAfter = true });
            else
                ActiveCommandModel.Document.Save();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return Result.Failed;
        }

    }

}
