using UnityEngine;

namespace Teleporter.Extensions;

public static class VectorExtensions {
    public static Vector3 ToVector3(this Vector vector) {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }

    public static Quaternion ToQuaternion(this Vector vector) {
        return Quaternion.Euler(vector.X, vector.Y, vector.Z);
    }

    public static Vector ToVector(this Vector3 vector) {
        return new Vector(vector.x, vector.y, vector.z);
    }
}