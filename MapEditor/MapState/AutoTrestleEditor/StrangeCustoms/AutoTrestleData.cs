using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StrangeCustoms;
using UnityEngine;

namespace MapEditor.MapState.AutoTrestleEditor.StrangeCustoms;

// copy of StrangeCustoms.AutoTrestleBuilder.AutoTrestleData
[JsonConverter(typeof(AutoTrestleDataJsonConverter))]
public class AutoTrestleData
{
    public SerializedSplinePoint[]          Points    { get; set; } = null!;
    public AutoTrestle.AutoTrestle.EndStyle HeadStyle { get; set; }
    public AutoTrestle.AutoTrestle.EndStyle TailStyle { get; set; }
}

// copy of StrangeCustoms.AutoTrestleBuilder.SerializedSplinePoint
public class SerializedSplinePoint {
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
}

// workaround: JObject tries to serialize more than just x,y,z from Vector3 class -> reference loop exception
public class AutoTrestleDataJsonConverter : JsonConverter<AutoTrestleData>
{
    public override void WriteJson(JsonWriter writer, AutoTrestleData? value, JsonSerializer serializer) {
        if (value == null) {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();

        // Serialize Points array
        writer.WritePropertyName("Points");
        writer.WriteStartArray();
        foreach (var point in value.Points) {
            writer.WriteStartObject();

            // Serialize Position without 'normalized' property
            writer.WritePropertyName("Position");
            WriteVector3(writer, point.Position);

            // Serialize Rotation without 'normalized' property
            writer.WritePropertyName("Rotation");
            WriteVector3(writer, point.Rotation);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        // Serialize other properties (HeadStyle and TailStyle)
        writer.WritePropertyName("HeadStyle");
        serializer.Serialize(writer, value.HeadStyle.ToString());

        writer.WritePropertyName("TailStyle");
        serializer.Serialize(writer, value.TailStyle.ToString());

        writer.WriteEndObject();
    }

    private static void WriteVector3(JsonWriter writer, Vector3 vector) {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(vector.x);
        writer.WritePropertyName("y");
        writer.WriteValue(vector.y);
        writer.WritePropertyName("z");
        writer.WriteValue(vector.z);
        writer.WriteEndObject();
    }

    public override AutoTrestleData? ReadJson(JsonReader reader, Type objectType, AutoTrestleData? existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType == JsonToken.Null) {
            return null;
        }

        var obj  = JObject.Load(reader);
        var data = new AutoTrestleData();

        // Deserialize Points array
        var pointsArray = obj["Points"]!.ToArray();
        var points      = new SerializedSplinePoint[pointsArray.Length];
        for (var i = 0; i < pointsArray.Length; i++) {
            var pointObj = pointsArray[i];
            points[i] = new SerializedSplinePoint {
                Position = ReadVector3(pointObj["Position"]!),
                Rotation = ReadVector3(pointObj["Rotation"]!)
            };
        }

        data.Points = points;

        // Deserialize HeadStyle and TailStyle
        data.HeadStyle = obj["HeadStyle"]!.ToObject<AutoTrestle.AutoTrestle.EndStyle>(serializer);
        data.TailStyle = obj["TailStyle"]!.ToObject<AutoTrestle.AutoTrestle.EndStyle>(serializer);

        return data;
    }

    private static Vector3 ReadVector3(JToken token) => new((float)token["x"]!, (float)token["y"]!, (float)token["z"]!);
}
