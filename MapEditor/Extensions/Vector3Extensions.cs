using UnityEngine;

namespace MapEditor.Extensions;

public static class Vector3Extensions
{
    public static Vector3 Clone(this Vector3 vector3) => new(vector3.x, vector3.y, vector3.z);
}
