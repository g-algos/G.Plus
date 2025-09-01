using Autodesk.Revit.DB.ExtensibleStorage;
using GPlus.Base.Extensions;
using GPlus.Base.Models;
using System.Text.Json;
using Color = Autodesk.Revit.DB.Color;

namespace GPlus.Base.Schemas
{
    public class LocationSchema
    {
      
        private LocationSchema() { }
        //Use to get
        public LocationSchema(ProjectInfo project, Entity entity, bool refresh = false)
        {
            _entity = entity;
            Schema schema = entity.Schema;
            Id = schema.GUID;
            Name = entity.Get<string>(nameof(Name));
            Categories = entity.Get<IList<ElementId>>(nameof(Categories));
            Parameter = entity.Get<ElementId>(nameof(Parameter));
            IncludeLinks = entity.Get<bool>(nameof(IncludeLinks));
            ByValue = entity.Get<bool>(nameof(ByValue));
            Step = entity.Get<int>(nameof(Step));
            Valid = project.Document.ValidParameter(Parameter);
            var items = entity.Get<IList<string>>(nameof(Items));
            Items = items?.Select(e => JsonSerializer.Deserialize<LocalizationItemModel>(e))?.ToList() ?? new();
            if (refresh) Generate(project.Document);
            _entity.Set<IList<string>>(nameof(Items), _items);
            project.SetEntity(_entity);
        }

        //Use to create
        public LocationSchema(ProjectInfo project, string name, List<ElementId> categories, ElementId parameter, bool byValue, bool includeLinks, int step)
        {
            Name = name;
            Categories = categories;
            Parameter = parameter;
            IncludeLinks = includeLinks;
            ByValue = byValue;
            Step = step;
            Valid = project.Document.ValidParameter(parameter);
            Schema schema = SchemaManager.CreateSchema(Id, name.Replace(" ","_"), new Dictionary<string, Type>
                {
                    { nameof(Categories), Categories.GetType() },
                    { nameof(Parameter), Parameter.GetType() },
                    { nameof(Name), Name.GetType() },
                    { nameof(Items), _items.GetType() },
                    { nameof(IncludeLinks), IncludeLinks.GetType() },
                    { nameof(Valid), Valid.GetType() },
                    { nameof(ByValue), ByValue.GetType() },
                    { nameof(Step), Step.GetType() }
                });

            Generate(project.Document);
            _entity = SchemaManager.AssignToElement(project, Id, new Dictionary<string, object>
                {
                    { nameof(Categories), Categories },
                    { nameof(Parameter), Parameter },
                    { nameof(Name), Name },
                    { nameof(Items), _items },
                    { nameof(IncludeLinks), IncludeLinks },
                    { nameof(Valid), Valid },
                    { nameof(ByValue), ByValue },
                    { nameof(Step), Step }
                });
            project.SetEntity(_entity);
        }
        public Guid Id { get; private set; } = Guid.NewGuid();
        private string _name;
        public string Name {
            get
            { return _name; }
            set
            {
                if (value.Any(c => !char.IsLetter(c) && c != '_' && c != '-' && c != ' ')) throw new Exception(Resources.Localizations.Messages.OnlyStrings);
                _name = value;
            }
        
        } 
        public IList<ElementId> Categories { get; private set; }
        public ElementId Parameter { get; private set; }
        public bool Valid { get; private set; }
        public int Step { get; set; }
        public List<LocalizationItemModel> Items
        {
            get
            {
                return _items.Select(e => JsonSerializer.Deserialize<LocalizationItemModel>(e)).ToList();
            }
            private set
            {
                _items = value.Select(e => JsonSerializer.Serialize(e)).ToList();
            }
        }

        private IList<string> _items = new List<string>();
        public bool IncludeLinks { get; private set; }
        public bool ByValue { get; private set; }

        private Entity _entity;
        public Entity Entity
        {
            get => _entity;
        }
        public bool Equals(LocationSchema? other) => Id == other?.Id;
        private void Generate(Document doc)
        {
            //TODO Create schema
        
            var alreadyExists = Items.Any() ? new HashSet<string>(Items.Select(i => i.Value)) : new();
            if (!ByValue)
            {
                alreadyExists = alreadyExists.SelectMany(alreadyExists => alreadyExists.Split(':'))
                                             .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            HashSet<string> allValues = new HashSet<string>();
            HashSet<string> values = new HashSet<string>();
            List<Document> docSet = new List<Document>();

            if (IncludeLinks)
            {
                docSet = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_RvtLinks)
                    .WhereElementIsNotElementType()
                    .Select(e=> (e as RevitLinkInstance).GetLinkDocument())
                    .ToList();
            }
            docSet.Add(doc);
            foreach (var document in docSet)
            {
                var collector = new FilteredElementCollector(document);
                foreach (var category in Categories)
                {
                    foreach (var v in collector
                        .OfCategoryId(category)
                        .ToElements()
                        .Select(e => e.GetParameter(Parameter)?.GetValue()?.ToString())
                        .Where(v => v != null))
                    {
                        allValues.Add(v!);
                        if(alreadyExists.Contains(v!))
                            continue;
                        values.Add(v!);
                    }
                }
            }

            var fillPatern = new FilteredElementCollector(ActiveCommandModel.Document)
                                .OfClass(typeof(FillPatternElement))
                                .Cast<FillPatternElement>()
                                .FirstOrDefault(e => e.GetFillPattern().IsSolidFill);
            Random c = new Random();

            if (ByValue)
            {
                foreach (var value in values)
                {
                    Color color = new Color((byte)(c.Next(0, 256) * 0.3 + 109), (byte)(c.Next(0, 256) * 0.3 + 109), (byte)(c.Next(0, 256) * 0.3 + 109));
#if V2023
                    var patternId = fillPatern.Id.IntegerValue;
#else
                    var patternId = fillPatern.Id.Value;
#endif
                    var item = new LocalizationItemModel(value, color, patternId);
                    _items.Add(JsonSerializer.Serialize(item));
                }
            }
            else
            {
                List<double> doubleValues = allValues.Select(e=> double.Parse(e)).ToList();
                var min =Math.Min(0, doubleValues.Min());
                var max = doubleValues.Max();

                var ranges = new List<(double Start, double End)>();

                for (double start = min; start < max; start += Step)
                {
                    double end = Math.Min(start + Step, max);
                    ranges.Add((start, end));
                }

                foreach (var value in ranges)
                {
                    Color color = new Color((byte)(c.Next(0, 256) * 0.3 + 109), (byte)(c.Next(0, 256) * 0.3 + 109), (byte)(c.Next(0, 256) * 0.3 + 109));
#if V2023
                    var patternId = fillPatern.Id.IntegerValue;
#else
                    var patternId = fillPatern.Id.Value;
#endif
                    var item = new LocalizationItemModel($@"{value.Start}:{value.End}", color, patternId);
                    _items.Add(JsonSerializer.Serialize(item));
                }
            }

            if (Items.Any())
            {
                foreach (var item in Items)
                {
                    if (!allValues.Contains(item.Value))
                        _items.Remove(JsonSerializer.Serialize(item));
                }
            }
        }
        public void EditField(ProjectInfo project, string field, object value)
        {
            if (field == nameof(Items))
            {
                if (value is not List<LocalizationItemModel> item)
                    return;
                Items = value as List<LocalizationItemModel>??Items;
                value = _items;
            }
            SchemaManager.EditFieldsValues(_entity, project, new Dictionary<string, object>() { { field, value } });
        }
        public static LocalizationModel? GetModel(Guid Id, ProjectInfo project)
        {
            if (!SchemaManager.TryGetSchema(Id, out var schema))
                return null;
            if (!SchemaManager.TryGetEntity(project, Id, out var entity))
                return null;

           var values = SchemaManager.GetFieldsValues(entity);

            return new LocalizationModel()
            {
                Id = Id,
                ByValue = (bool)values[nameof(ByValue)],
                IncludeElementsFromLinks = (bool)values[nameof(IncludeLinks)],
                Categories = ((IList<ElementId>)values[nameof(Categories)]).ToHashSet<ElementId>(),
                Valid = project.Document.ValidParameter((ElementId)values[nameof(Parameter)]),
                Items = ((IList <string>)values[nameof(Items)]).Select(e => JsonSerializer.Deserialize<LocalizationItemModel>(e)).ToHashSet<LocalizationItemModel>(),
                Name = (string)values[nameof(Name)],
                Parameter = (ElementId)values[nameof(Parameter)],
                Step = (int?)values[nameof(Step)]
            };
        }

    }
}
