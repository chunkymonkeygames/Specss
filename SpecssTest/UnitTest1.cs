using Specss;

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

            var enc = new BinaryCoder(schema);
            enc.SetField("req", 5);
            enc.SetField("nreq", 3);
            enc.SetField("req4", (uint)3);
            enc.SetField("req5", 3.5f);
            enc.SetField("req2", "test string");
            enc.SetField("repeated", new int[] { 2, 3, 5, 3 });
            enc.SetField("long", 99392231L);
            // avoid setting nreq2 to make sure unrequired fields work
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
            Console.WriteLine("Tests completed successfully");
        }
    }
}