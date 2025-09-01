using Autodesk.Revit.DB.ExtensibleStorage;
using FieldBuilder = Autodesk.Revit.DB.ExtensibleStorage.FieldBuilder;

namespace GPlus.Base.Schemas
{
    internal static class SchemaManager
    {
        internal static bool TryGetSchema(Guid schemaGuid, out Schema? schema)
        {
            // Lookup the schema by GUID
            schema = Schema.Lookup(schemaGuid);
            return schema != null;
        }
        internal static Schema CreateSchema(Guid schemaGuid, string schemaName, Dictionary<string, Type> fields, Guid? subschema = null)
        {
            try
            {
                // Check if the schema already exists
                if (TryGetSchema(schemaGuid, out Schema? schema))
                    return schema!;
                // Start creating the schema builder
                SchemaBuilder schemaBuilder = new SchemaBuilder(schemaGuid);
                schemaBuilder.SetSchemaName(schemaName);
                schemaBuilder.SetReadAccessLevel(AccessLevel.Application);
                schemaBuilder.SetWriteAccessLevel(AccessLevel.Application);
                schemaBuilder.SetVendorId("etc-tec");
                schemaBuilder.SetApplicationGUID(Guid.Parse("75FA216D-AAB3-4E8E-88A3-C312610C3F05"));
    
                // Add fields to the schema

                foreach (var field in fields)
                {
                    if (field.Value.GetInterfaces().Any(e => (e.IsGenericType && e.GetGenericTypeDefinition() == typeof(IList<>)) || e.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>))))
                    {
                        FieldBuilder fieldBuilder =  schemaBuilder.AddArrayField(field.Key, field.Value.GetGenericArguments()[0]);
                        if (field.Value.GetGenericArguments()[0] == typeof(Entity))
                        {
                            fieldBuilder.SetSubSchemaGUID(subschema!.Value);
                        }
                    }
                    else
                    {
                        FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField(field.Key, field.Value);
                        if (field.Value == typeof(Entity))
                        {
                            fieldBuilder.SetSubSchemaGUID(subschema!.Value);
                        }
                    }

                }

                // Finalize and return the schema
                return schemaBuilder.Finish();
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        internal static bool TryGetEntity(Element element, Guid schemaGuid, out Entity? entity)
        {
            // Lookup the schema
            if (TryGetSchema(schemaGuid, out Schema? schema))
            {
                // Get the entity from the element
                entity = element.GetEntity(schema);
                return entity.IsValid();
            }
            entity = null;
            return false;
        }
        internal static Entity AssignToElement(Element element, Guid schemaGuid, Dictionary<string, object> fieldValues)
        {
            // Lookup the schema
            if (!TryGetSchema(schemaGuid, out Schema? schema))
                throw new InvalidOperationException($"Schema with GUID {schemaGuid} does not exist.");

            Entity entity = SetEntity(element, fieldValues, schema);
            return entity;
        }
        internal static Entity AssignToElement(Element element, Schema schema, Dictionary<string, object> fieldValues)
        {
            Entity entity = SetEntity(element, fieldValues, schema);
            return entity;
        }
        private static Entity SetEntity(Element element, Dictionary<string, object> fieldValues, Schema? schema)
        {
            // Create or get the entity
            Entity entity = new Entity(schema);

            // Assign values to the schema fields
            foreach (var field in fieldValues)
            {
                SetField(entity, field);
            }
            element.SetEntity(entity);
            return entity;
        }
        internal static Dictionary<string, object> GetFieldsValues(Element element, Guid schemaGuid)
        {
            if (!TryGetSchema(schemaGuid, out Schema? schema))
                throw new InvalidOperationException($"Schema with GUID {schemaGuid} does not exist.");

            if(!TryGetEntity(element, schemaGuid, out Entity? entity))
                throw new InvalidOperationException($"The element does not have a valid entity for schema '{schema!.SchemaName}'.");

            Dictionary<string, object> fieldValues = GetFieldsValues(entity!, schema!);

            return fieldValues;
        }
        internal static Dictionary<string, object> GetFieldsValues(Entity entity)
        {
            Schema schema = entity.Schema;
            Dictionary<string, object> fieldValues = GetFieldsValues(entity, schema);

            return fieldValues;
        }
        private static Dictionary<string, object> GetFieldsValues(Entity entity, Schema schema)
        {
            // Retrieve values for all fields in the schema
            Dictionary<string, object> fieldValues = new Dictionary<string, object>();
            foreach (Field field in schema.ListFields())
            {
                object value = GetField(entity, field);
                fieldValues[field.FieldName] = value;
            }

            return fieldValues;
        }
        internal static void EditFieldsValues(Entity entity, Element element, Dictionary<string, object> updatedFieldValues)
        {
            Schema schema = entity.Schema;
            // Update the fields in the entity
            foreach (var updatedField in updatedFieldValues)
            {
                Field schemaField = schema.GetField(updatedField.Key);
                if (schemaField == null)
                    return;
                // Update the field value in the entity
                SetField(entity, updatedField);
            }

            // Reassign the updated entity back to the element
            element.SetEntity(entity);
        }
        internal static void RemoveFromElement(Element element, Guid schemaGuid)
        {
            if (TryGetSchema(schemaGuid, out Schema? schema)!)
                throw new InvalidOperationException($"Schema with GUID {schemaGuid} does not exist.");
            element.DeleteEntity(schema);
        }
        private static void SetField (Entity entity, KeyValuePair<string,object> field)
        {
            switch (field.Value)
            {
                case bool b:
                    entity.Set(field.Key, b);
                    break;
                case byte bt:
                    entity.Set(field.Key, bt);
                    break;
                case short s:
                    entity.Set(field.Key, s);
                    break;
                case int i:
                    entity.Set(field.Key, i);
                    break;
                case float f:
                    entity.Set(field.Key, f);
                    break;
                case double d:
                    entity.Set(field.Key, d);
                    break;
                case ElementId id:
                    entity.Set(field.Key, id);
                    break;
                case Guid g:
                    entity.Set(field.Key, g);
                    break;
                case string str:
                    entity.Set(field.Key, str);
                    break;
                case XYZ xyz:
                    entity.Set(field.Key, xyz);
                    break;
                case UV uv:
                    entity.Set(field.Key, uv);
                    break;
                case Entity ent:
                    entity.Set(field.Key, ent);
                    break;

                // Arrays (IList<T>)    
                case IList<bool> b:
                    entity.Set(field.Key, b);
                    break;
                case IList<byte> bt:
                    entity.Set(field.Key, bt);
                    break;
                case IList<short> s:
                    entity.Set(field.Key, s);
                    break;
                case IList<int> i:
                    entity.Set(field.Key, i);
                    break;
                case IList<float> f:
                    entity.Set(field.Key, f);
                    break;
                case IList<double> d:
                    entity.Set(field.Key, d);
                    break;
                case IList<ElementId> id:
                    entity.Set(field.Key, id);
                    break;
                case IList<Guid> g:
                    entity.Set(field.Key, g);
                    break;
                case IList<string> str:
                    entity.Set(field.Key, str);
                    break;
                case IList<XYZ> xyz:
                    entity.Set(field.Key, xyz);
                    break;
                case IList<UV> uv:
                    entity.Set(field.Key, uv);
                    break;
                case IList<Entity> ent:
                    entity.Set(field.Key, ent);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported field type: {field.Value?.GetType().FullName}");
            }

        }
        private static object GetField(Entity entity, Field field)
        {
            object value;

            Type t = field.ValueType;

            if (field.ContainerType == ContainerType.Simple)
            {
                if (t == typeof(bool))
                    return entity.Get<bool>(field.FieldName);
                else if (t == typeof(byte))
                    return entity.Get<byte>(field.FieldName);
                else if (t == typeof(short))
                    return entity.Get<short>(field.FieldName);
                else if (t == typeof(int))
                    return entity.Get<int>(field.FieldName);
                else if (t == typeof(float))
                    return entity.Get<float>(field.FieldName);
                else if (t == typeof(double))
                    return entity.Get<double>(field.FieldName);
                else if (t == typeof(ElementId))
                    return entity.Get<ElementId>(field.FieldName);
                else if (t == typeof(Guid))
                    return entity.Get<Guid>(field.FieldName);
                else if (t == typeof(string))
                    return entity.Get<string>(field.FieldName);
                else if (t == typeof(XYZ))
                    return entity.Get<XYZ>(field.FieldName);
                else if (t == typeof(UV))
                    return entity.Get<UV>(field.FieldName);
                else if (t == typeof(Entity))
                    return entity.Get<Entity>(field.FieldName);
            }

            else if (field.ContainerType == ContainerType.Array)
            {
                if (t == typeof(bool))
                    return entity.Get<IList<bool>>(field.FieldName);
                else if (t == typeof(byte))
                    return entity.Get<IList<byte>>(field.FieldName);
                else if (t == typeof(short))
                    return entity.Get<IList<short>>(field.FieldName);
                else if (t == typeof(int))
                    return entity.Get<IList<int>>(field.FieldName);
                else if (t == typeof(float))
                    return entity.Get<IList<float>>(field.FieldName);
                else if (t == typeof(double))
                    return entity.Get<IList<double>>(field.FieldName);
                else if (t == typeof(ElementId))
                    return entity.Get<IList<ElementId>>(field.FieldName);
                else if (t == typeof(Guid))
                    return entity.Get<IList<Guid>>(field.FieldName);
                else if (t == typeof(string))
                    return entity.Get<IList<string>>(field.FieldName);
                else if (t == typeof(XYZ))
                    return entity.Get<IList<XYZ>>(field.FieldName);
                else if (t == typeof(UV))
                    return entity.Get<IList<UV>>(field.FieldName);
                else if (t == typeof(Entity))
                    return entity.Get<IList<Entity>>(field.FieldName);
            }
            throw new InvalidOperationException($"Unsupported field type: {t.FullName}");

        }
    }
}
