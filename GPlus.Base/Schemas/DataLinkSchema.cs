using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Mechanical;
using GPlus.Base.Extensions;
using GPlus.Base.Helpers;
using GPlus.Base.Models;
using System.Data;

namespace GPlus.Base.Schemas;
public static class DataLinkSchema
{
    private static Guid Id = Guid.Parse(Resources.Identifiers.DataLinkScema);
    private const string Name = "GDataLink";

    private const string Link = "";
    private const bool IsRelative = false;
    private const bool IsInstance = false;
    private const string LastSync = "";
    private static IList<ElementId> Categories = new List<ElementId>();
    public static Schema Create() => SchemaManager.CreateSchema(Id, Name, new Dictionary<string, Type> {
        { nameof(Link), Link.GetType() }, 
        { nameof(IsInstance), IsInstance.GetType() },
        { nameof(LastSync), LastSync.GetType() },
        { nameof(Categories), Categories.GetType() },
        { nameof(IsRelative), IsRelative.GetType() }
    });
    public static List<DataLinkModel> GetAll(Document doc)
    {
        List<DataLinkModel> links = new();
        var views = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Schedules)
            .Cast<ViewSchedule>()
            .ToList();

        foreach (var view in views)
        {
            if (TryGetSchema(view, out var dataLink))
                links.Add(dataLink);
        }
        return links;
    }
    public static bool TryGetSchema(ViewSchedule view, out DataLinkModel? dataLink)
    {
        dataLink = null;
        if (!SchemaManager.TryGetEntity(view, Id, out Entity? entity))
            return false;

        dataLink = new();
        dataLink.Link = entity.Get<string>(nameof(Link));
        dataLink.IsInstance = entity.Get<bool>(nameof(IsInstance));
        dataLink.LastSync= DateTime.ParseExact(entity.Get<string>(nameof(LastSync)), "dd/MM/yyyy HH:mm",null);
        dataLink.Categories = entity.Get<IList<ElementId>>(nameof(Categories)).ToList();
        dataLink.ViewId = view.Id;
        dataLink.IsRelative = entity.Get<bool>(nameof(IsRelative));


        return true;
    }
    public static void ApplySchema(ViewSchedule view, DataLinkModel dataLink)
    {

        Entity entity = SchemaManager.AssignToElement(view, Id, 
            new Dictionary<string, object> 
            { 
                { nameof(Link), dataLink.Link }, 
                { nameof(IsInstance), dataLink.IsInstance },
                { nameof(IsRelative), dataLink.IsRelative },
                { nameof(LastSync), dataLink.LastSync.ToString("dd/MM/yyyy HH:mm") },
                {nameof(Categories), dataLink.Categories },
            });
        view.SetEntity(entity);
    }
    public static void RemoveSchema(ViewSchedule view)
    {
        if (!SchemaManager.TryGetSchema(Id, out var schema))
            return;
        view.DeleteEntity(schema);
    }
    public static bool TryPush(Document doc, DataLinkModel model)
    {
        var view = doc.GetElement(model.ViewId);
        if (view == null)
            return false;
        if (view is not ViewSchedule schedule)
            return false;

        if (!TryGetRevitTable(doc, model, schedule, out DataTable? dataTable))
            return false;


        var parts = model.Link.Split('|');
        if (parts.Length != 2)
            return false;

        string filePath = parts[0];
        string sheetName = parts[1];
        if (model.IsRelative) filePath = FileHelpers.ResolvePath(filePath, view.Document);
        if (filePath == null) return false;
        return SpreadSheets.TryWriteTableToExcel(filePath, sheetName, dataTable);
    }

    public static bool TryPush(Document doc, DataLinkModel model, DataTable table, Dictionary<int, ElementId> createdElements)
    {
        var view = doc.GetElement(model.ViewId);
        if (view == null)
            return false;
        if (view is not ViewSchedule schedule)
            return false;

        var parts = model.Link.Split('|');
        if (parts.Length != 2)
            return false;

        string filePath = parts[0];
        string sheetName = parts[1];
        if (model.IsRelative) filePath = FileHelpers.ResolvePath(filePath, view.Document);
        if (filePath == null) return false;
        var result = SpreadSheets.TryAddToTableToExcel(filePath, sheetName, table, createdElements);

        Update(schedule);
        return result;
    }
    private static bool TryGetRevitTable(Document doc, DataLinkModel model, ViewSchedule schedule, out DataTable dataTable)
    {
        HashSet<Element> elements = new();
        dataTable = new DataTable();

        if (model.IsInstance)
            elements = new FilteredElementCollector(doc, model.ViewId).WhereElementIsNotElementType().ToElements().ToHashSet();
        else
        {
            var typeIds = new FilteredElementCollector(doc, model.ViewId).WhereElementIsNotElementType().Select(e => e.GetTypeId()).ToHashSet();
            elements = typeIds.Select(e => doc.GetElement(e)).ToHashSet();
        }

        List<(string Id, string Name)> parameters = new();

        var scheduleBody = schedule.GetTableData().GetSectionData(SectionType.Body);
        int numColumns = scheduleBody.NumberOfColumns;
        var fields = schedule.Definition.GetFieldCount();
        for (int col = 0; col < numColumns; col++)
        {
      
            ScheduleField field = schedule.Definition.GetField(col);
            if (field == null || field.IsCombinedParameterField || field.IsCalculatedField 
                || field.FieldType == ScheduleFieldType.Count || field.FieldType == ScheduleFieldType.HostCount || field.FieldType == ScheduleFieldType.Formula || field.FieldType == ScheduleFieldType.Percentage)
                continue;
            var columnHeading = field.ColumnHeading;
            if (parameters.Select(e => e.Name).Contains(columnHeading, StringComparer.OrdinalIgnoreCase))
            {
                var newName = doc.GetParameterName(field.ParameterId);

                if (parameters.Select(e => e.Name).Contains(newName, StringComparer.OrdinalIgnoreCase))
                {
                    columnHeading = $"{columnHeading} (1)";
                }
                else
                {
                    columnHeading = newName;
                }
            }
            parameters.Add(($"{field.FieldType}:{field.ParameterId}", columnHeading));
           
        }
        dataTable = new DataTable();
        var firstCol = new DataColumn("ElementId");
        firstCol.Caption = "ElementId";
        firstCol.ReadOnly = true;
        dataTable.Columns.Add(firstCol);

        foreach (var param in parameters)
        {
#if V2023
            DataColumn column = new DataColumn(param.Id, typeof(string));
#else
            DataColumn column = new DataColumn(param.Id, typeof(string));
#endif
            column.Caption = param.Name;
            dataTable.Columns.Add(column);
        }
        int firstElement = 0;
        foreach (Element element in elements)
        {
            try
            {
                DataRow row = dataTable.NewRow();
#if V2023
                row[0] = element.Id.IntegerValue;
#else
                row[0] = element.Id.Value;
#endif
                for (int i = 0; i < parameters.Count; i++)
                {
                    var parameterIdentification = parameters[i].Id.Split(":");
                    if (parameterIdentification.Count() != 2) continue;

                    var source = parameterIdentification.First();
#if V2023
                    if (!int.TryParse(parameterIdentification.Last(), out int id)) continue;
#else
                    if (!long.TryParse(parameterIdentification.Last(), out long id)) continue;
#endif
                    var parameterId = new ElementId(id);

                    Parameter? parameter = null;
                    var type = Enum.Parse<ScheduleFieldType>(source);
                    if (model.IsInstance && type == ScheduleFieldType.ElementType)
                        parameter = doc.GetElement(element.GetTypeId()).GetParameter(parameterId);
                    else if (type == ScheduleFieldType.ProjectInfo)
                        parameter = doc.ProjectInformation.GetParameter(parameterId);
                    else if (type == ScheduleFieldType.FromRoom)
                    {
                        if (element is FamilyInstance fi)
                        {
                            Room fromRoom = fi.FromRoom;
                            if (fromRoom != null)
                            parameter = fromRoom.GetParameter(parameterId);
                        }
                    }
                    else if (type == ScheduleFieldType.ToRoom)
                    {
                        if (element is FamilyInstance fi)
                        {
                            Room toRoom = fi.ToRoom;
                            if (toRoom != null)
                            parameter = toRoom.GetParameter(parameterId);
                        }
                    }
                    else if (type == ScheduleFieldType.Room)
                    {
                        if (element is FamilyInstance fi)
                        {
                            Room room = fi.Room;
                            if (room != null)
                            parameter = room.GetParameter(parameterId);
                        }
                    }
                    else if (type == ScheduleFieldType.Space)
                    {
                        if (element is FamilyInstance fi)
                        {
                            Space space = fi.Space;
                            if (space != null)
                                parameter = space.GetParameter(parameterId);
                        }
                    }
                    else
                        parameter = element.GetParameter(parameterId);

                    var value = parameter?.GetValue();
                    row[i + 1] = value;
                    if (firstElement == 0 
                        && (parameter == null || parameter.IsReadOnly || parameter.StorageType == StorageType.ElementId || (type != ScheduleFieldType.Instance && model.IsInstance) || (type != ScheduleFieldType.ElementType && !model.IsInstance)))
                    {
                        var column = dataTable.Columns[i + 1];
                        column.ReadOnly = true;
                    }
                }
                dataTable.Rows.Add(row);
                firstElement++;
            }
            catch { }
        }

        return true;
    }
    public static bool  AnalyzePull(Document doc, DataLinkModel model, out DataTable? data, out List<DataColumn> unMatched)
    {
        data = null;
        unMatched = new();
        var parts = model.Link.Split('|');
        if (parts.Length != 2)
            return false;

        string filePath = parts[0];
        string sheetName = parts[1];
        if (model.IsRelative) filePath = FileHelpers.ResolvePath(filePath, doc);
        if (filePath == null) return false;

        if (!SpreadSheets.TryReadSheetTable(filePath, sheetName, out data))
            return false;

        var view = doc.GetElement(model.ViewId);
        if (view == null)
            return false;
        if (view is not ViewSchedule schedule)
            return false;

        MapColumns(doc, schedule, model.IsInstance, data, out unMatched);
        return true;
    }
    public static bool TryPull(Document doc, ViewSchedule schedule, DataLinkModel model, DataTable data, out Dictionary<int, ElementId> createdEntities)
    {
        createdEntities = new();
        try
        {

            ElementId phaseId = schedule.get_Parameter(BuiltInParameter.VIEW_PHASE).AsElementId();
            PhaseArray phases = doc.Phases;
            Phase? phase = null;
            foreach (Phase p in phases)
            {
                if (p.Id == phaseId) { phase = p; break; }
            }
            for (int r = 0; r < data.Rows.Count; r++)
            {
                try
                {
                    var row = data.Rows[r];
                    var rowId = row[0].ToString().Replace("i","");
                    var action = data.Columns.Contains("Action")? row["Action"]?.ToString():"" ;
#if V2023
                    var isId = int.TryParse(rowId, out int longId);
                    var category = (BuiltInCategory)model.Categories.First().IntegerValue;
#else
                    var isId = long.TryParse(rowId, out long longId);   
                    var category = (BuiltInCategory)model.Categories.First().Value;
#endif
                    ElementId id = new ElementId(longId) ?? ElementId.InvalidElementId;

                    if (!isId ||id == ElementId.InvalidElementId || action == "created")
                    {
                        if (model.Categories.Count != 1) continue;

                        if (category == BuiltInCategory.OST_Rooms)
                        doc.CreateRoom(phase!, out id);
                        else if (category == BuiltInCategory.OST_Areas)
                        {
                            ElementId LevelId = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).ToElementIds().FirstOrDefault()!;
                            ElementId areaSchemaId = schedule.Definition.AreaSchemeId;

                            doc.CreateArea(areaSchemaId, LevelId, out id);
                        }
                        else if (category == BuiltInCategory.OST_MEPSpaces)
                            doc.CreateSpace(phase!, out id);
                        else if (category == BuiltInCategory.OST_Sheets)
                            doc.CreateSheet(phase!, out id);

                        if (id != ElementId.InvalidElementId && int.TryParse(rowId, out int rowIndex)) createdEntities.Add(rowIndex, id);

                    }
                    else if (id != ElementId.InvalidElementId && action == "deleted")
                    {
                        if (model.Categories.Count != 1) continue;
                        if (category == BuiltInCategory.OST_Rooms
                                || category == BuiltInCategory.OST_Areas
                                || category == BuiltInCategory.OST_MEPSpaces
                                || category == BuiltInCategory.OST_Sheets)
                        {
                            doc.Delete(id);
                        }
                        continue;
                    }
                    if (id == null || id == ElementId.InvalidElementId) continue;
                    Element element = doc.GetElement(id);
                    if (element == null) continue;
                    for (int c = 1; c < data.Columns.Count; c++)
                    {
                        if (data.Columns[c].ColumnName == "Action") continue; // Skip action column

                        var columnparts = data.Columns[c].ColumnName.Split(":");
                        if( Enum.TryParse(columnparts.First(), out ScheduleFieldType fieldType) == false) continue;
                        if(fieldType != ScheduleFieldType.Instance && fieldType != ScheduleFieldType.ElementType) continue;

                        if (columnparts.Length != 2) continue; // Invalid column format

#if V2023
                        if (int.TryParse(columnparts.Last(), out int column)==false) continue;
#else
                        if (long.TryParse(columnparts.Last(), out long column)==false) continue;
#endif
                        var parameterId = new ElementId(column);
                        var value = row[c];
                        var parameter = element.GetParameter(parameterId);
                        if (parameter == null || parameter.IsReadOnly || parameter.StorageType == StorageType.ElementId)
                            continue;
                        else
                            parameter.SetValue(value);
                    }
                }
                catch { }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
    public static void MapColumns(Document doc, ViewSchedule schedule, bool isInstance, DataTable? data,  out List<DataColumn> UnmatchedColumns)
    {
        int numColumns = schedule.Definition.GetFieldCount();
        List<(ElementId Id, string Name)> parameters = new();
        for (int col = 0; col < numColumns; col++)
        {
            ScheduleField field = schedule.Definition.GetField(col);
            if (field == null) continue;
            if (field.IsCombinedParameterField) continue;
            if (field.IsCalculatedField) continue;
            var name = field.ColumnHeading;
            try
            {
                name = doc.GetParameterName(field.ParameterId);
            }
            catch { }
            
            parameters.Add((field.ParameterId, name));
        }

        List<(ElementId Id, string Name)> documentParameters = schedule.Definition.GetSchedulableFields()
            .Where(e => (isInstance && e.FieldType == ScheduleFieldType.Instance) || (!isInstance && e.FieldType == ScheduleFieldType.ElementType))
            .Select(e => (e.ParameterId, e.GetName(doc)))
            .ToList();

        UnmatchedColumns = new();
        for (int i = 1; i < data.Columns.Count; i++)
        {
            try
            {
                string column = data.Columns[i].ColumnName;
                if(column == "Action") continue; // Skip action column
                ElementId parameterId = ElementId.InvalidElementId;

                var columParts = column.Split(":");
                if(columParts.Length != 2)
                {
                    UnmatchedColumns.Add(data.Columns[i]);
                    continue;
                }

#if V2023
                if(int.TryParse(columParts.Last(), out int Id))
#else
                if (!long.TryParse(columParts.Last(), out long Id))
#endif
                {

                    if (documentParameters.Any(e => e.Name == column))
                        schedule.Definition.AddField(isInstance ? ScheduleFieldType.Instance : ScheduleFieldType.ElementType, parameterId);
                    else
                        UnmatchedColumns.Add(data.Columns[i]);
                }
            }
            catch { }
        }
    }
    public static bool CompareTables(ViewSchedule view, out DataTable? rvt, out DataTable? xls)
    {
        rvt = new DataTable(); 
        xls = new DataTable();

        if (!TryGetSchema(view, out DataLinkModel? dataLink)) return false;
        if(!TryGetRevitTable(view.Document, dataLink, view, out DataTable? rvtTable)) return false;

        var parts = dataLink.Link.Split('|');
        if (parts.Length != 2)
            return false;

        string filePath = parts[0];
        string sheetName = parts[1];
        if(dataLink.IsRelative) filePath = FileHelpers.ResolvePath(filePath, view.Document);
        if (!SpreadSheets.TryReadSheetTable(filePath, sheetName, out DataTable? xlsTable))
            return false;
        CompareTables(view.Document, rvtTable, xlsTable, rvt, xls);
        return  true;

    }
    private static void CompareTables(Document doc, DataTable rvtTable, DataTable xlsTable, DataTable diffRvtTable, DataTable diffXlsTable)
    {
        const string keyCol = "ElementId";

        var rvtRowsById = rvtTable.AsEnumerable()
            .Where(row => row[keyCol] != DBNull.Value && row[keyCol] != null
#if V2023
                    && int.TryParse(row[keyCol].ToString(), out int id)
#else
                    && long.TryParse(row[keyCol].ToString(), out long id)
#endif
                    && new ElementId(id) != ElementId.InvalidElementId  )
            .GroupBy(row => row[keyCol]?.ToString())
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!, g => g.First());

        var xlsRowsById = xlsTable.AsEnumerable()
            .Where(row => row[keyCol] != DBNull.Value && row[keyCol] != null
#if V2023
                    && int.TryParse(row[keyCol].ToString(), out int id)
#else
                    && long.TryParse(row[keyCol].ToString(), out long id)
#endif
            && new ElementId(id) != ElementId.InvalidElementId)
            .GroupBy(row => row[keyCol]?.ToString())
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!, g => g.First());
       
        var newXlsRows = xlsTable.AsEnumerable()
            .Where(row => row[keyCol] == DBNull.Value || row[keyCol] == null
#if V2023
            || !(int.TryParse(row[keyCol].ToString(), out int id)
#else
            || !(long.TryParse(row[keyCol].ToString(), out long id)
#endif
            && new ElementId(id) != ElementId.InvalidElementId));
        var allIds = new HashSet<string>(rvtRowsById.Keys);
        foreach (var id in xlsRowsById.Keys)
            allIds.Add(id);

        var paramCols = new HashSet<(string ColumnName,string Caption)>(
            rvtTable.Columns.Cast<DataColumn>()
            .Concat(xlsTable.Columns.Cast<DataColumn>())
            .Where(c => c.ColumnName != keyCol)
            .Select(c=> (c.ColumnName, c.Caption)));

        // Add columns if needed
        void EnsureColumns(DataTable table)
        {
            if (!table.Columns.Contains(keyCol)) table.Columns.Add(keyCol, typeof(string));
            if (!table.Columns.Contains("Action")) table.Columns.Add("Action", typeof(string));
            foreach (var col in paramCols)
                if (!table.Columns.Contains(col.ColumnName))
                {
                    var column = new DataColumn(col.ColumnName);
                    column.Caption = col.Caption;
                    table.Columns.Add(column);
                }
        }

        EnsureColumns(diffRvtTable);
        EnsureColumns(diffXlsTable);

        string GetValue(DataRow row, string col) =>
            row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? row[col]?.ToString() : null;

        foreach (var id in allIds)
        {
            var inRvt = rvtRowsById.TryGetValue(id, out var rvtRow);
            var inXls = xlsRowsById.TryGetValue(id, out var xlsRow);

            if (inRvt && !inXls)
            {
                // RVT has an element that was deleted in XLS
                AddRow(diffRvtTable, id, "created", rvtRow, paramCols);
                var row = diffXlsTable.NewRow();
                AddRow(diffXlsTable, id, "deleted", row, paramCols);
            }
            else if (!inRvt && inXls)
            {
                // XLS has an element that was deleted in revit
                var row = diffRvtTable.NewRow();
                AddRow(diffXlsTable, id, "created", xlsRow, paramCols);
                AddRow(diffRvtTable, id, "deleted", row, paramCols);
            }
            else
            {
                // Compare both versions
                bool changed = paramCols.Any(col => {
                    var rvt = GetValue(rvtRow, col.ColumnName);
                    var xls = GetValue(xlsRow, col.ColumnName);
                    if(String.IsNullOrEmpty(rvt) && String.IsNullOrEmpty(xls))
                        return false;
                    return rvt != xls;
                });
                if (changed)
                {
                    AddRow(diffRvtTable, id, "edited", rvtRow, paramCols);
                    AddRow(diffXlsTable, id, "edited", xlsRow, paramCols);
                }
            }
        }

       
        foreach(var row in newXlsRows)
        {
            // XLS has an element that was never existed in revit
            var rvtRow = diffRvtTable.NewRow();
            AddRow(diffRvtTable, $"i{xlsTable.Rows.IndexOf(row).ToString()}", "deleted", rvtRow, paramCols);
            AddRow(diffXlsTable, $"i{xlsTable.Rows.IndexOf(row).ToString()}", "created", row, paramCols);
        }
        void AddRow(DataTable table, string id, string action, DataRow baseRow, IEnumerable<(string ColumnName, string Caption)> columns)
        {
            var newRow = table.NewRow();
            newRow[keyCol] = id;
            newRow["Action"] = action;
            foreach (var col in columns)
                newRow[col.ColumnName] = GetValue(baseRow, col.ColumnName);
            table.Rows.Add(newRow);
        }
    }
    public static void Update(ViewSchedule view)
    {
        if (!SchemaManager.TryGetEntity(view, Id, out Entity? entity))
            return;
        var date = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm");
        entity!.Set<string>(nameof(LastSync), date);
        view.SetEntity(entity);
    }
}
