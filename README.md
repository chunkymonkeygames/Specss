# Specss


## Usage

```csharp
var schema = new Schema("product");
// field name, field type, required
schema.AddField(new Field("price", SpecssFieldType.Float32, true));
schema.AddField(new Field("sales", SpecssFieldType.Int32, false));
schema.AddField(new Field("name", SpecssFieldType.UtfEightString, true));

var enc = new BinaryCoder(schema);
enc.SetField("price", 3.50f);
enc.SetField("sales", 300);
enc.SetField("name", "test string");
enc.Encode();
var output = enc.GetOutputBytes();


// transmit output online or whatever

var dec = new BinaryCoder(schema);
var obj = dec.Decode(new MemoryStream(output));
obj["price"] // 3.50f
obj["sales"] // 300
// ... you get it

```