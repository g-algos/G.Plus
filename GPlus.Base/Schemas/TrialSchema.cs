using Autodesk.Revit.DB.ExtensibleStorage;

namespace GPlus.Base.Schemas
{
    public static class TrialSchemaCollection
    {
        private static Guid Id = Guid.Parse("24491067-E28D-4C7A-B084-22D01D79495C");
        private const string Name = "GTrialCollection";

        private static Entity Entity = new Entity(Guid.Parse("B25A0647-6C22-4E1A-8F2A-34AF38567271"));
        private static IList<Entity> Entities = new List<Entity>(){ new Entity(Guid.Parse("B25A0647-6C22-4E1A-8F2A-34AF38567271")) };

        public static void Create()
        {
            SchemaManager.CreateSchema(Id, Name, 
                new Dictionary<string, Type> { 
                    { nameof(Entities), Entities.GetType() },
                    { nameof(Entity), Entity.GetType() }
                }, 
                Guid.Parse("B25A0647-6C22-4E1A-8F2A-34AF38567271"));
        }
        public static Entity Assign(Document doc, Entity entity)
        {
            Entity = entity;
            Entities = new List<Entity>() { entity };
           return  SchemaManager.AssignToElement(doc.ProjectInformation, Id, new Dictionary<string, object>()
            {
               { nameof(Entity), Entity },
               { nameof(Entities), Entities }
            });
        }
    }

    public static class TrialSchema
    {
        private static Guid Id = Guid.Parse("B25A0647-6C22-4E1A-8F2A-34AF38567271");
        private const string Name = "GTrial";

        private static string Value = "Test";

        public static void Create()
        {
            SchemaManager.CreateSchema(Id, Name,
                new Dictionary<string, Type> {
                    { nameof(Value), Value.GetType() }
                });
        }
        public static Entity CreateEntity()
        {
            Entity entity = new Entity(Id);
            entity.Set<string>(nameof(Value), Value);
            return entity;
        }
    }
}
