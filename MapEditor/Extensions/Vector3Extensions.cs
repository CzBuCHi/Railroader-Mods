using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MapEditor.Extensions;

public static class Vector3Extensions
{
    public static Vector3 Clone(this Vector3 vector3) {
        return new Vector3(vector3.x, vector3.y, vector3.z);
    }

    public static JObject ToJObject(this Vector3 vector3)
    {
        return new JObject
        {
            { "x", vector3.x },
            { "y", vector3.y },
            { "z", vector3.z },
        };
    }
}