using System.Collections.Generic;
using JetBrains.Annotations;

namespace MapMod.TelegraphPoles;

[PublicAPI]
public sealed class TelegraphPoles
{
    public string                             Handler { get; set; } = null!;
    public Dictionary<int, TelegraphPoleData> Nodes   { get; }      = new();
}
