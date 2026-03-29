using UnityEngine;

namespace Teleporter.Extensions
{
    internal static class QuaternionExtensions
    {
        public static Vector ToVector(this Quaternion quaternion) => quaternion.eulerAngles.ToVector();
    }
}
