using Specss;
using System.Threading.Tasks.Dataflow;

namespace SpecssTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var schema = new Schema("test");
            schema.AddField(new Field("req", new SpecssFieldType(SpecssFieldTypeNum.Int32, false), true));
            schema.AddField(new Field("nreq", new SpecssFieldType(SpecssFieldTypeNum.Int32, false), false));
            schema.AddField(new Field("req4", new SpecssFieldType(SpecssFieldTypeNum.UInt32, false), true));
            schema.AddField(new Field("req5", new SpecssFieldType(SpecssFieldTypeNum.Float32, false), true));
            schema.AddField(new Field("req2", new SpecssFieldType(SpecssFieldTypeNum.UtfEightString, false), true));
            schema.AddField(new Field("nreq2", new SpecssFieldType(SpecssFieldTypeNum.UtfEightString, false), false));
            schema.AddField(new Field("repeated", new SpecssFieldType(SpecssFieldTypeNum.Int32, true), true));
            schema.AddField(new Field("long", new SpecssFieldType(SpecssFieldTypeNum.Long, false), true));
            schema.AddField(new Field("bytes", new SpecssFieldType(SpecssFieldTypeNum.RawBytesString, false), false));
            schema.AddField(new Field("sub", new SpecssFieldType(SpecssFieldTypeNum.RawBytesString, true), false));
            schema.AddField(new Field("sub2", new SpecssFieldType(SpecssFieldTypeNum.RawBytesString, true), false));

            var enc = new BinaryCoder(schema);
            enc.SetField("req", 5);
            enc.SetField("nreq", 3);
            enc.SetField("req4", (uint)3);
            enc.SetField("req5", 3.5f);
            enc.SetField("req2", "test string");
            enc.SetField("repeated", new int[] { 2, 3, 5, 3 });
            enc.SetField("long", 99392231L);
            enc.SetField("bytes", new byte[] { 10, 10, 11, 12 });
            //var sub = new byte[][] { new byte[] { 0,1,2,3  }, new byte[] { 0,1,2,3 } };
            var sub = new byte[][] { new byte[] {12, 1, 2, 3 }};
            enc.SetField("sub", sub);
            enc.Encode();
            var output = enc.GetOutputBytes();
            var dec = new BinaryCoder(schema);
            var obj = dec.Decode(new MemoryStream(output));
            Assert.AreEqual(5, obj["req"]);
            Assert.AreEqual(3, obj["nreq"]);
            Assert.AreEqual("test string", obj["req2"]);
            Assert.AreEqual(null, obj["nreq2"]);
            Assert.AreEqual((uint)3, obj["req4"]);
            Assert.AreEqual(3.5f, obj["req5"]);
            Assert.AreEqual(99392231L, obj["long"]);
            CollectionAssert.AreEqual(new int[] { 2, 3, 5, 3 }, (int[])obj["repeated"]);
            CollectionAssert.AreEqual(new byte[] { 10, 10, 11, 12 }, (byte[])obj["bytes"]);
            Console.WriteLine("Tests completed successfully");
        }
    }
}