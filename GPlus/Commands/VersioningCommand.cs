using Autodesk.Revit.Attributes;
using ClosedXML.Report;
using GPlus.Base.Enums;
using GPlus.Base.Extensions;
using GPlus.Base.Models;
using GPlus.Base.Schemas;
using System.Windows;

namespace GPlus.Commands;

[Transaction(TransactionMode.Manual)]
public class VersioningCommand : IExternalCommand
{
    private LogsView LogsView { get; set; }
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements
    )
    {
        ActiveCommandModel.Set(commandData.Application);
        try
        {
            List<VersioningModel> versions = new();
            if (!VersioningSchema.TryGetSchema(ActiveCommandModel.Document.ProjectInformation, out versions, out bool isRecording) || isRecording==false || versions.Count()<2)
            {
                MessageBox.Show(
                    Base.Resources.Localizations.Messages.NoPreviousVersions,
                    Base.Resources.Localizations.Messages.Wait,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return Result.Cancelled;
            }

            versions = versions.Where(e => e.VersionGuid != Document.GetDocumentVersion(ActiveCommandModel.Document).VersionGUID).ToList();
            LogsVM viewModel = new LogsVM(
            versions.Select(e => new VersionVM()
            {
                Id = e.VersionGuid,
                Name = "V" + e.Order.ToString(),
                CreatedOn = e.CreatedOn,

            }).ToList(), ActiveCommandModel.Document.Title);
            
            viewModel.Export += (s, e) => { Export(e.Item1, e.Item2, ActiveCommandModel.Document.Title); };
            viewModel.View += (s, e) => { ViewChanges(e.Item1, e.Item2); };
            viewModel.Close += (s, e) => { LogsView.Close(); };
            LogsView = new LogsView(viewModel);
            LogsView.Topmost = true;
            var revitWin = Autodesk.Windows.ComponentManager.ApplicationWindow;
            new System.Windows.Interop.WindowInteropHelper(LogsView) { Owner = revitWin };
            _ = LogsView.ShowDialog();
        
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

    private void ViewChanges(VersionVM version, List<ElementLogVM> logs)
    {

        ViewFamilyType? view3dType = new FilteredElementCollector(ActiveCommandModel.Document)
                   .OfClass(typeof(ViewFamilyType))
                   .Cast<ViewFamilyType>()
                   .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

        if (view3dType == null)
        {
            MessageBox.Show(
               Base.Resources.Localizations.Messages.NoView3dTemplate,
               Base.Resources.Localizations.Messages.OOOps + " - " + Base.Resources.Localizations.Messages.Error,
               MessageBoxButton.OK,
               MessageBoxImage.Error
           );
            return;
        }

        using (Transaction t = new Transaction(ActiveCommandModel.Document, "Create 3D View"))
        {
            t.Start();

            View3D view3d = CreateView(version, logs, view3dType);
            t.Commit();

            ActiveCommandModel.UIDocument.ActiveView = view3d;
        }
        LogsView.Activate();
    }

    private static View3D CreateView(VersionVM version, List<ElementLogVM> logs, ViewFamilyType view3dType)
    {
        View3D view3d = View3D.CreateIsometric(ActiveCommandModel.Document, view3dType.Id);
        view3d.DisplayStyle = DisplayStyle.FlatColors;

        view3d.Name = $"{DateTime.Now:yyMMdd}_{version.Name}";
        var pattern = ActiveCommandModel.Document.GetfillPatterns().FirstOrDefault(e => e.GetFillPattern().IsSolidFill);
        var ids = new List<ElementId>();

        foreach (var log in logs)
        {
            Element? element = ActiveCommandModel.Document.GetElement(log.ElementId);
            if (element != null)
            {
                OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                ogs.SetSurfaceForegroundPatternId(pattern.Id);
                switch (log.Action)
                {
                    case ElementAction.Created:
                        ids.Add(element.Id);
                        ogs.SetSurfaceForegroundPatternColor(new Color(0, 255, 0)); // Green
                        break;
                    case ElementAction.Edited:
                        ids.Add(element.Id);
                        ogs.SetSurfaceForegroundPatternColor(new Color(255, 255, 0)); // Yellow
                        break;
                }
                view3d.SetElementOverrides(element.Id, ogs);
            }
        }

        view3d.IsolateElementsTemporary(ids);
        view3d.ConvertTemporaryHideIsolateToPermanent();
        //var filepath = System.IO.Path.GetTempFileName() + view3d.Name;
        //ActiveCommandModel.Document.ExportImage(new ImageExportOptions()
        //{
        //    FilePath = System.IO.Path.GetTempFileName()+view3d.Name,
        //    FitDirection = FitDirectionType.Horizontal,
        //    HLRandWFViewsFileType = ImageFileType.JPEGMedium,
        //    ImageResolution = ImageResolution.DPI_300,
        //    ExportRange = ExportRange.CurrentView,
        //});
        return view3d;
    }

    private void Export(VersionVM version, List<ElementLogVM> logs, string currentDocument)
    {
        var appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var templatePath = System.IO.Path.Combine(appPath!, "Resources/VersioningReportTemplate.xlsx");
        using var template = new XLTemplate(templatePath);
        var data = new
        {
            Model = currentDocument,
            Date = version.CreatedOn.ToString("dd/MM/yy"),
            Version = version.Name,
            Logs = logs.Select(e => new ElementLog
            {
                Action = e.Action,
                Category = e.Category,
                Level = e.Level,
                Name = e.Name,
                ElementId = e.ElementId,
            }).ToList()
        };

        template.AddVariable(nameof(data.Model), data.Model);
        template.AddVariable(nameof(data.Date), data.Date);
        template.AddVariable(nameof(data.Version), data.Version);
        template.AddVariable(nameof(data.Logs), data.Logs);
        template.Generate();
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"{version.CreatedOn:yyMMdd}_{currentDocument}_{version.Name}.xlsx"
        };
        bool? result = saveFileDialog.ShowDialog();
        if (result == true)
        {
            string filePath = saveFileDialog.FileName;
            template.SaveAs(filePath);
        }
        LogsView.Activate();
    }
}
