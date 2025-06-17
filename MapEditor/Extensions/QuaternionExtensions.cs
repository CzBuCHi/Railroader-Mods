using UnityEngine;

namespace MapEditor.Extensions;

public static class QuaternionExtensions
{
    public static Quaternion Clone(this Quaternion quaternion) => new(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
}
