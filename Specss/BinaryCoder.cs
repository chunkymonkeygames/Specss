using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Specss
{
   

    public static class BinaryEncodeUtil
    {
        public static Dictionary<SpecssFieldTypeNum, object> TypeToByte;
        public static Dictionary<SpecssFieldTypeNum, object> ByteToType;

        static BinaryEncodeUtil()
        {
#pragma warning disable CS8974 // Converting method group to non-delegate type
            TypeToByte = new Dictionary<SpecssFieldTypeNum, object> {
                { SpecssFieldTypeNum.UInt32, UIntToBytes },
                { SpecssFieldTypeNum.Int32, IntToBytes },
                { SpecssFieldTypeNum.Float32, FloatToBytes },
                { SpecssFieldTypeNum.UtfEightString, UTF8ToBytes },
                { SpecssFieldTypeNum.RawBytesString, RawStringToBytes },
            };

            ByteToType = new Dictionary<SpecssFieldTypeNum, object> {
                { SpecssFieldTypeNum.UInt32, BytesToUInt },
                { SpecssFieldTypeNum.Int32, BytesToInt },
                { SpecssFieldTypeNum.Float32, BytesToFloat },
                { SpecssFieldTypeNum.UtfEightString, BytesToUTF8 },
                { SpecssFieldTypeNum.RawBytesString, BytesToRawString },
            };
#pragma warning restore CS8974 // Converting method group to non-delegate type
        }

        // no IPAddress HostToNetworkOrder for floats
        public static byte[] HostToNetworkOrder(float host)
        {
            byte[] bytes = BitConverter.GetBytes(host);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return bytes;
        }

        public static float NetworkToHostOrder(byte[] bytes)
        {

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            return BitConverter.ToSingle(bytes, 0);
        }


        public static void UIntToBytes(uint i, MemoryStream s)
        {
            s.Write(BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder((int)i)),0,4);
        }

        public static uint BytesToUInt(MemoryStream s)
        {
            var i = new byte[4];
            s.Read(i, 0, 4);
            return (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(i));
        }

        public static void IntToBytes(int i, MemoryStream s)
        {
            s.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(i)), 0, 4);
        }

        public static int BytesToInt(MemoryStream s)
        {
            var i = new byte[4];
            s.Read(i, 0, 4);
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(i));
        }

        public static void FloatToBytes(float i, MemoryStream s)
        {
            s.Write(HostToNetworkOrder(i), 0, 4);
        }

        public static float BytesToFloat(MemoryStream s)
        {
            var i = new byte[4];
            s.Read(i, 0, 4);
            return NetworkToHostOrder(i);
        }

        public static void UTF8ToBytes(string i, MemoryStream s)
        {
            // write length first
            var strBytes = Encoding.UTF8.GetBytes(i);
            UIntToBytes((uint)strBytes.Length, s);
            s.Write(strBytes, 0, strBytes.Length);
        }

        public static string BytesToUTF8(MemoryStream s)
        {
            // read length
            var len = BytesToUInt(s);
            var i = new byte[len];
            s.Read(i, 0, (int)len);
            return Encoding.UTF8.GetString(i);
        }

        public static void RawStringToBytes(byte[] i, MemoryStream s)
        {
            UIntToBytes((uint)i.Length, s);
            s.Write(i, 0, i.Length);
        }

        public static byte[] BytesToRawString(MemoryStream s)
        {
            // read length
            var len = BytesToUInt(s);
            var i = new byte[len];
            s.Read(i, 0, (int)len);
            return i;
        }



        public static void EncodeField(Field field, MemoryStream ms, object data)
        {
            if (data is Array && data.GetType().GetElementType()! != typeof(byte)) // basically, byte arrays are an acceptable field value, so make sure this isnt one of those
            {
                if (!field.FieldType.Repeated)
                    throw new InvalidDataException("Provided an array but type is not repeatable " + field.Name + "/" + data.ToString() + "/" + data.GetType().ToString());
                IntToBytes(((Array)data).Length, ms);
                foreach (object f in (Array)data) {
                    EncodeField(field, ms, f);
                }
            }
            else
            {
                if (field.FieldType.Repeated)
                    IntToBytes(1, ms);
                dynamic writer = TypeToByte[field.FieldType.Type];
                writer(Convert.ChangeType(data, writer.GetType().GetMethod("Invoke").GetParameters()[0].ParameterType), ms);
            }
        }

        private static T[] ChangeArrType<T>(object[] arr)
        {
            return Array.ConvertAll(arr, (item) => (T)item);
        }

        public static object DecodeField(Field field, MemoryStream ms)
        {
            object data;
            if (!ByteToType.ContainsKey(field.FieldType.Type))
                throw new InvalidDataException("Unknown type on field " + field.Name);
            if (field.FieldType.Repeated)
            {
                var length = BytesToInt(ms);
                if (length == 1)
                {
                    dynamic decoder = ByteToType[field.FieldType.Type];
                    data = decoder(ms);
                } else
                {
                    var arr = new object[length];
                    for (int i = 0; i < length; i++)
                    {
                        arr[i] = DecodeField(field, ms);
                    }
                    Type T = arr[0].GetType();
                    var method = typeof(BinaryEncodeUtil).GetMethod(nameof(ChangeArrType), BindingFlags.NonPublic | BindingFlags.Static, new Type[] { typeof(object[]) })!;
                    var gen = method.MakeGenericMethod(T);
                    data = gen.Invoke(null, new object[] { arr })!;
                }
            } else
            {
                dynamic decoder = ByteToType[field.FieldType.Type];
                data = decoder(ms);
            }
            return data;
        }
    }

    public class BinaryCoder
    {
        private readonly Schema schema;
        private readonly Dictionary<string, object> data;
        private MemoryStream? outputMemoryStream;
        
        public BinaryCoder(Schema schema)
        {
            this.schema = schema;
            this.data = new Dictionary<string, object>();
        }

        public Schema GetSchema()
        {
            return schema;
        }

        public void SetField(String fieldName, object value)
        {
            if (!schema.HasField(fieldName))
            {
                throw new ArgumentException("Not a valid field");
            }
            data[fieldName] = value;
        }

        public void Encode()
        {
            outputMemoryStream = new MemoryStream();
            foreach (Field f in schema.GetFields())
            {

                BinaryEncodeUtil.UIntToBytes(f.index, outputMemoryStream);

                if (!data.ContainsKey(f.Name) && f.required)
                    throw new InvalidOperationException("Required field " + f.Name + " is not present");
                // format will always be
                // INDEX (put above)
                // TYPE
                // DATA
                if (data.ContainsKey(f.Name))
                {
                    outputMemoryStream.WriteByte((byte)f.FieldType.Type); // send type
                    BinaryEncodeUtil.EncodeField(f, outputMemoryStream, data[f.Name]);
                }
                else
                    outputMemoryStream.WriteByte((byte)SpecssFieldTypeNum.Omitted); // Ommitted type shows optional value 
            }
            BinaryEncodeUtil.UIntToBytes(9999, outputMemoryStream);
        }

        public IDictionary<string, object> Decode(MemoryStream inputMemoryStream)
        {
            dynamic output = new ExpandoObject();
            IDictionary<string, object> dictoutput = output;
            try
            {

                while (true)
                {
                    var fieldID = BinaryEncodeUtil.BytesToUInt(inputMemoryStream);
                    if (fieldID == 9999)
                    {
                        break;
                    }
                    if (!schema.HasField((int)fieldID))
                        throw new InvalidDataException("Unknown field " + fieldID);
                    var type = inputMemoryStream.ReadByte();
                    if ((SpecssFieldTypeNum)type == SpecssFieldTypeNum.Omitted)
                        if (schema.GetField((int)fieldID).required)
                            throw new InvalidDataException("field " + schema.GetField((int)fieldID).Name + " is required but not included");
                        else
                        {
                            dictoutput.Add(schema.GetField((int)fieldID).Name, null);
                            continue;
                        }
                    var field = schema.GetField((int)fieldID);
                    Console.WriteLine("reading field " + field.Name);
                    if ((int)field.FieldType.Type != type)
                        throw new InvalidDataException("Schema does not match");
                    var data = BinaryEncodeUtil.DecodeField(field, inputMemoryStream);
                    dictoutput[field.Name] = data;
                }
                foreach (Field f in schema.GetFields())
                {
                    if (!dictoutput.ContainsKey(f.Name))
                    {
                        throw new InvalidDataException("Field " + f.Name + " was not included in data");
                    }
                }
            } catch (Exception e) 
            {
                throw new IOException("Could not decode", e);
            }
            return dictoutput;
        }

        public byte[] GetOutputBytes()
        {
            return outputMemoryStream!.ToArray();
        }
    }

}
