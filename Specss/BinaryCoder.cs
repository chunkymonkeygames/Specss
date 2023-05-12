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
        public static Dictionary<SpecssFieldType, object> TypeToByte;
        public static Dictionary<SpecssFieldType, object> ByteToType;

        static BinaryEncodeUtil()
        {
#pragma warning disable CS8974 // Converting method group to non-delegate type
            TypeToByte = new Dictionary<SpecssFieldType, object> {
                { SpecssFieldType.UInt32, UIntToBytes },
                { SpecssFieldType.Int32, IntToBytes },
                { SpecssFieldType.Float32, FloatToBytes },
                { SpecssFieldType.UtfEightString, UTF8ToBytes },
                { SpecssFieldType.RawBytesString, RawStringToBytes },
            };

            ByteToType = new Dictionary<SpecssFieldType, object> {
                { SpecssFieldType.UInt32, BytesToUInt },
                { SpecssFieldType.Int32, BytesToInt },
                { SpecssFieldType.Float32, BytesToFloat },
                { SpecssFieldType.UtfEightString, BytesToUTF8 },
                { SpecssFieldType.RawBytesString, BytesToRawString },
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
                    outputMemoryStream.WriteByte((byte)f.Type); // send type
                    dynamic writer = BinaryEncodeUtil.TypeToByte[f.Type];
                    writer(Convert.ChangeType(data[f.Name], writer.GetType().GetMethod("Invoke").GetParameters()[0].ParameterType), outputMemoryStream);
                }
                else
                    outputMemoryStream.WriteByte((byte)SpecssFieldType.Omitted); // Ommitted type shows optional value 
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
                    if ((SpecssFieldType)type == SpecssFieldType.Omitted)
                        if (schema.GetField((int)fieldID).required)
                            throw new InvalidDataException("field " + schema.GetField((int)fieldID).Name + " is required but not included");
                        else
                        {
                            dictoutput.Add(schema.GetField((int)fieldID).Name, null);
                            continue;
                        }
                    var field = schema.GetField((int)fieldID);
                    if (!BinaryEncodeUtil.ByteToType.ContainsKey((SpecssFieldType)type))
                        throw new InvalidDataException("Unknown type on field " + field.Name);
                    dynamic decoder = BinaryEncodeUtil.ByteToType[field.Type];
                    object data = decoder(inputMemoryStream);
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
