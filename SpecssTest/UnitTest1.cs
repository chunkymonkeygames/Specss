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
            schema.AddField(new Field("req", SpecssFieldType.Int32, true));
            schema.AddField(new Field("nreq", SpecssFieldType.Int32, false));
            schema.AddField(new Field("req4", SpecssFieldType.UInt32, true));
            schema.AddField(new Field("req5", SpecssFieldType.Float32, true));
            schema.AddField(new Field("req2", SpecssFieldType.UtfEightString, true));
            schema.AddField(new Field("nreq2", SpecssFieldType.UtfEightString, false));

            var enc = new BinaryCoder(schema);
            enc.SetField("req", 5);
            enc.SetField("nreq", 3);
            enc.SetField("req4", (uint)3);
            enc.SetField("req5", 3.5f);
            enc.SetField("req2", "test string");
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

        }
    }
}