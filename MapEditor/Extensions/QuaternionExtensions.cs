using UnityEngine;

namespace MapEditor.Extensions;

public static class QuaternionExtensions
{
    public static Quaternion Clone(this Quaternion quaternion) {
        return new Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }
}