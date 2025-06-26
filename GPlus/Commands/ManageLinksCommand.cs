using Autodesk.Revit.Attributes;
using GPlus.Base.Helpers;
using GPlus.Base.Models;
using GPlus.Base.Schemas;
using System.Data;
using System.IO;
using System.Windows.Forms;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;


namespace GPlus.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ManageLinksCommand : IExternalCommand
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
                var links = DataLinkSchema.GetAll(ActiveCommandModel.Document)
                .Select(e => new ScheduleLinkVM()
                {
                    LastSync = e.LastSync,
                    LinkType = e.IsInstance ? Base.Resources.Localizations.Content.Instance : Base.Resources.Localizations.Content.Type,
                    Path = e.Link,
                    PathType = e.IsRelative ? Base.Resources.Localizations.Content.Relative : Base.Resources.Localizations.Content.Absolute,
                    Schedule = new IdentityVM() { Id = e.ViewId, Name = ActiveCommandModel.Document.GetElement(e.ViewId).Name },
                    Status = File.Exists(e.IsRelative ? FileHelpers.ResolvePath(e.Link.Split("|").First(), ActiveCommandModel.Document) : e.Link.Split("|").First())
                }).ToList();
                var scheduleIds = links.Select(e => e.Schedule.Id).ToList();
                var schedules = new FilteredElementCollector(ActiveCommandModel.Document)
                    .OfClass(typeof(ViewSchedule))
                    .Cast<ViewSchedule>()
                    .Where(vs => !vs.IsTitleblockRevisionSchedule && !vs.IsTemplate)
                    .Select(e => new IdentityVM()
                    {
                        Id = e.Id,
                        Name = e.Name
                    })
                    .OrderBy(e=> e.Name)
                    .ToList();
                var selected = links.FirstOrDefault(s => s.Schedule.Id == ActiveCommandModel.View.Id);

                DataLinksVM viewModel = new DataLinksVM(links, selected, schedules);
                viewModel.AddLinkCallback = (vm) =>
                {
                    bool isOk = AddLink(vm, ActiveCommandModel.Document);
                    return isOk;
                };
                viewModel.RemoveLink += (s, e) => { RemoveLink(e, ActiveCommandModel.Document); };
                viewModel.Push += (s, e) => { Push(e, ActiveCommandModel.Document); };
                viewModel.Pull += (s, e) => { Pull(e, ActiveCommandModel.Document); };
                viewModel.Sync += (s, e) => { Sync(e, ActiveCommandModel.Document); };
                viewModel.Merge += (s, e) => { Merge(e, ActiveCommandModel.Document); };
                viewModel.GetComparisonTable = Compare;
                var window = new ManageLinksView(viewModel);
                _ = window.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
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

        private static void Merge(Tuple<ElementId, DataTable?, DataTable?> e, Document doc)
        {
            using (Transaction transaction = new Transaction(ActiveCommandModel.Document, "Sync"))
            {
                try
                {
                    transaction.Start();
                    if (e == null)
                        return;
                    var view = doc.GetElement(e.Item1) as ViewSchedule;
                    if (view == null) return;

                    if (!DataLinkSchema.TryGetSchema(view, out var link)) return;
                    Dictionary<int, ElementId> createdElements = new();
                    //Excel
                    if (e.Item3 != null && e.Item3.Rows.Count > 0)
                    {
                        DataLinkSchema.MapColumns(doc, view, link.IsInstance, e.Item3, out List<DataColumn> UnmatchedColumns);
                        AddParameters(doc, view, link, e.Item3, UnmatchedColumns);
                        if (!DataLinkSchema.TryPull(doc, view, link, e.Item3, out createdElements))
                            throw new Exception(Base.Resources.Localizations.Messages.PushFailed);

                    }
                    //Revit
                    if ((e.Item2 != null && e.Item2.Rows.Count > 0) || createdElements.Any())
                    {
                        if (!DataLinkSchema.TryPush(doc, link, e.Item2, createdElements))
                            throw new Exception(Base.Resources.Localizations.Messages.PushFailed);
                    }
                    DataLinkSchema.Update(view);
                    transaction.Commit();
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
                    return;
                }
            }
        }

        private static Tuple<DataTable, DataTable>? Compare(ElementId id)
        {
            DataTable rvtTable = null;
            DataTable xlsTable = null;
            try
            {
                var view = ActiveCommandModel.Document.GetElement(id) as ViewSchedule;
                if (view == null) return null;

                if (DataLinkSchema.CompareTables(view, out rvtTable, out xlsTable))
                    return new Tuple<DataTable, DataTable>(rvtTable, xlsTable);
                else return null;
            }
            catch (Exception ex)
            {
                var dialog = new TaskDialog(Base.Resources.Localizations.Messages.OOOps)
                {
                    MainInstruction = Base.Resources.Localizations.Messages.Error,
                    MainContent = ex.Message,
                    CommonButtons = TaskDialogCommonButtons.Close
                };
                dialog.Show();
                return null;
            }
        }
        private void Sync(ElementId e, Document doc)
        {
            using (Transaction transaction = new Transaction(ActiveCommandModel.Document, "Sync"))
            {
                try
                {
                    transaction.Start();
                    var view = doc.GetElement(e);
                    if (view == null || view is not ViewSchedule schedule) return;
                    if (!DataLinkSchema.TryGetSchema(schedule, out var link)) return;

                    if (!Pull(doc, schedule, link)) throw new Exception(Base.Resources.Localizations.Messages.PullFailed);
                    if (!DataLinkSchema.TryPush(doc, link)) throw new Exception(Base.Resources.Localizations.Messages.PushFailed) ;
                    DataLinkSchema.Update(schedule);
                    transaction.Commit();
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
                    return;
                }
            }
        }
        private void Pull(ElementId e, Document doc)
        {
            using (Transaction transaction = new Transaction(ActiveCommandModel.Document, "Pull"))
            {
                try
                {
                    transaction.Start();
                    var view = doc.GetElement(e);
                    if (view == null || view is not ViewSchedule schedule) return;
                    if (!DataLinkSchema.TryGetSchema(schedule, out var link)) return;
                    if (!Pull(doc, schedule, link)) return;
                    DataLinkSchema.Update(schedule);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    var dialog = new Autodesk.Revit.UI.TaskDialog(Base.Resources.Localizations.Messages.OOOps)
                {
                    MainInstruction = Base.Resources.Localizations.Messages.Error,
                    MainContent = ex.Message,
                    CommonButtons = TaskDialogCommonButtons.Close
                };
                    dialog.Show();
                    return;
                }
            }
        }
        private bool Pull(Document doc, ViewSchedule schedule, DataLinkModel? link)
        {
            if (!DataLinkSchema.AnalyzePull(doc, link, out DataTable? data, out List<DataColumn> unMatched)) return false;
            if(unMatched.Any())
            AddParameters(doc, schedule, link, data, unMatched);

            try
            {
                DataLinkSchema.TryPull(doc, schedule, link, data!, out Dictionary<int, ElementId> createdEntities);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void AddParameters(Document doc, ViewSchedule schedule, DataLinkModel link, DataTable data, List<DataColumn> unMatched)
        {
            var specs = SpecUtils.GetAllSpecs().Select(e => new IdentityForgeVM { Id = e, Name = LabelUtils.GetLabelForSpec(e) }).DistinctBy(e => e.Name).ToList();
            NewSharedParametersVM viewModel = new(specs, unMatched.Select(e => new NewSharedParamaterVM(e.ColumnName)).ToList());

            var view = new NewSharedParametersView(viewModel);
            view.ShowDialog();
            var parameters = viewModel.Parameters.Where(e => e.SpecType != null)
                .Select(p => (p.Name, p.SpecType!.Id))
                .ToList();
            foreach (var param in viewModel.Parameters.Where(e => e.SpecType == null))
            {
                var column = unMatched.FirstOrDefault(e => e.Caption == param.Name || e.ColumnName == param.Name);
                data!.Columns.Remove(column);
            }
            try
            {
                string oldFile = doc.Application.SharedParametersFilename;
                var definitions = SharedParameters.AddSharedParameterToTempFile(doc.Application, parameters);
                CategorySet catSet = doc.Application.Create.NewCategorySet();

                foreach (var kv in link.Categories)
                {
#if V2023
                    var id = kv.IntegerValue;
#else
                    var id = kv.Value;
#endif
                    var cat = doc.Settings.Categories.get_Item((BuiltInCategory)id);
                    if (cat != null)
                        catSet.Insert(cat);
                }

                foreach (var definition in definitions)
                {

                    if (!SharedParameters.TryCreateSharedParameter(doc, definition, link.IsInstance, catSet, out ElementId? id))
                    {
                        var column = unMatched.FirstOrDefault(e => e.Caption == definition.Name || e.ColumnName == definition.Name);
                        data!.Columns.Remove(column);
                    }
                    else
                    {
                        SchedulableField? field = schedule.Definition.GetSchedulableFields().FirstOrDefault(e => e.GetName(doc) == definition.Name);
                        if (field != null)
                        {
                            var column = unMatched.FirstOrDefault(e => e.Caption == definition.Name || e.ColumnName == definition.Name);
                            column!.ColumnName = id.ToString();
                            column!.Caption = definition.Name;
                            schedule.Definition.AddField(field);
                        }
                        else
                        {
                            var column = unMatched.FirstOrDefault(e => e.Caption == definition.Name || e.ColumnName == definition.Name);
                            data!.Columns.Remove(column);
                        }
                    }
                }

                doc.Application.SharedParametersFilename = oldFile;
            }
            catch { }
        }

        private void Push(ElementId e, Document doc)
        {
            using (Transaction transaction = new Transaction(ActiveCommandModel.Document, "Push"))
            {
                try
                {
                    transaction.Start();
                    var view = doc.GetElement(e);
                    if (view == null || view is not ViewSchedule schedule) return;
                    if (!DataLinkSchema.TryGetSchema(schedule, out var link)) return;
                    if(!DataLinkSchema.TryPush(doc, link)) throw new Exception(Base.Resources.Localizations.Messages.PushFailed);
                    DataLinkSchema.Update(schedule);
                    transaction.Commit();
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
                    return;
                }
            }
        }
        private void RemoveLink(ElementId e, Document doc)
        {
            using (Transaction transaction = new Transaction(ActiveCommandModel.Document, "Remove link"))
            {
                try
                {
                    transaction.Start();
                    var view = doc.GetElement(e);
                    if (view == null || view is not ViewSchedule schedule) return;
                    DataLinkSchema.RemoveSchema(schedule);
                    transaction.Commit();
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
                    return;
                }
            }
        }
        private bool AddLink(NewScheduleLinkVM e, Document doc)
        {
            using (Transaction transaction = new Transaction(ActiveCommandModel.Document, "Add link"))
            {
                try
                {
                    transaction.Start();
                    var view = doc.GetElement(e.Schedule.Id);
                    if (view == null || view is not ViewSchedule schedule) return false;

                    List<ElementId> categories = new();
                    if (schedule.Definition.CategoryId == null)
                        categories = new FilteredElementCollector(view.Document, view.Id).ToElements().Select(e => e.Category.Id).Distinct().ToList();
                    else categories.Add(schedule.Definition.CategoryId);

                    var model = new DataLinkModel()
                    {
                        LastSync = DateTime.UtcNow,
                        IsInstance = e.IsInstance,
                        IsRelative = !e.IsAbsolute,
                        Link = e.Path,
                        ViewId = e.Schedule.Id,
                        Categories = categories,
                    };
                    if (!DataLinkSchema.TryPush(doc, model)) throw new Exception(Base.Resources.Localizations.Messages.PushFailed);
                    DataLinkSchema.ApplySchema(schedule, model);
                    transaction.Commit();
                    return true;
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
                    return false;
                }
            }
        }
    }
}
