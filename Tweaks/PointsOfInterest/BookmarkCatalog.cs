using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tweaks.PointsOfInterest
{
    internal static class BookmarkCatalog
    {
        internal sealed class Entry
        {
            internal string Group { get; }
            internal string Name { get; }
            internal string Id { get; }
            internal Entry(string group, string name)
            {
                Group = group; Name = name;
                Id = Convert.ToBase64String(Encoding.UTF8.GetBytes(group)) + ":" +
                    Convert.ToBase64String(Encoding.UTF8.GetBytes(name));
            }
        }

        internal static List<Entry> Create(Settings settings) => settings.Groups.SelectMany(group =>
            group.Locations.Keys.OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .Select(name => new Entry(group.Name, name))).ToList();
    }
}
