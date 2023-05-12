

namespace Specss
{
    public enum SpecssFieldType
    {
        Omitted = 0,
        Int32 = 1,
        UInt32 = 2,
        Float32 = 3,
        UtfEightString = 4,
        RawBytesString = 5,
        Array = 6,
        Dict = 7
    }

    public struct Field
    {
        public Field(String name, SpecssFieldType type, bool isRequired)
        {
            Name = name;
            Type = type;
            required = isRequired;
        }
        public readonly string Name { get; }
        public readonly SpecssFieldType Type { get; }
        public readonly bool required;
        internal uint index = 0;
    }

    public class Schema
    {
        private readonly string name;
        private readonly List<Field> fields;
        private readonly Dictionary<string, Field> fieldNames;
        private readonly Dictionary<int, Field> fieldIDs;

        private uint index = 1;


        public Field[] GetFields()
        {
            return fields.ToArray();
        }

        public bool HasField(string name)
        {
            return fieldNames.ContainsKey(name);
        }

        public bool HasField(int id)
        {
            return fieldIDs.ContainsKey(id);
        }

        public Field GetField(string name)
        {
            return fieldNames[name];
        }

        public Field GetField(int id)
        {
            return fieldIDs[id];
        }


        public Schema(String name)
        {
            this.name = name;
            this.fields = new List<Field>();
            this.fieldNames = new Dictionary<string, Field>();
            this.fieldIDs = new Dictionary<int, Field>();
        }

        public String GetName()
        {
            return name;
        }

        public void AddField(Field field)
        {
            field.index = index++;
            fieldNames[field.Name] = field;
            fieldIDs[(int)field.index] = field;
            fields.Add(field);
        }
    }
}