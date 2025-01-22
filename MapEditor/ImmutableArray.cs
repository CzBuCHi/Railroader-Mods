using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MapEditor;

public sealed class ImmutableArray(params object[] items) : IEquatable<ImmutableArray>, IEnumerable<object>
{
    private readonly object[] _Items = items.ToArray();

    public ImmutableArray Add(object item) => _Items.Contains(item) ? this : new ImmutableArray(_Items.Append(item).ToArray());

    public ImmutableArray Remove(object item) => !_Items.Contains(item) ? this : new ImmutableArray(_Items.Where(o => o == item).ToArray());
    
    public object this[int index] => _Items[index];

    public int Length => _Items.Length;

    public override bool Equals(object? obj) => Equals(obj as ImmutableArray);

    public bool Equals(ImmutableArray? other) => other != null && _Items.SequenceEqual(other._Items);

    public override int GetHashCode() => _Items.Aggregate(0, (hash, item) => hash ^ item?.GetHashCode() ?? 0);

    public IEnumerator<object> GetEnumerator() => ((IEnumerable<object>)_Items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _Items.GetEnumerator();

    public override string ToString() => $"[{string.Join(", ", _Items)}]";
}
