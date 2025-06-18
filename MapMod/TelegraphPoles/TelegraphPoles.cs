using System.Collections.Generic;
using JetBrains.Annotations;

namespace MapMod.TelegraphPoles;

[PublicAPI]
public sealed class TelegraphPoles
{
    public string Handler { get; } = typeof(TelegraphPoleBuilder).FullName!;
    public Dictionary<int, TelegraphPoleData> Nodes   { get; }      = new();
}
