using UnityEngine;

namespace Teleporter.Extensions;

public static class QuaternionExtensions {
    public static Vector ToVector(this Quaternion quaternion) {
        return quaternion.eulerAngles.ToVector();
    }
}
