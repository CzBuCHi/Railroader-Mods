using JetBrains.Annotations;
using UnityEngine;

namespace MapMod.TelegraphPoles;

[PublicAPI]
public sealed class TelegraphPoleData
{
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public int     Tag      { get; set; }
}