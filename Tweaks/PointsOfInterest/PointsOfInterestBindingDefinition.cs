using Newtonsoft.Json;

namespace Tweaks.PointsOfInterest
{
    internal static class PointsOfInterestBindingDefinition
    {
        internal const string ActionId = "dfcb5b13-7737-444a-91a5-3a83814f9f49";
        internal const string MapName = "CzBuCHi.Tweaks";
        internal const string ActionName = "Points of interest";
        internal static string Json => JsonConvert.SerializeObject(new { maps = new[] { new
            {
                name = MapName,
                id = "4ea03cb5-4416-4b6c-b328-194cc3cf5ad1",
                actions = new[] { new { name = ActionName, type = "Button", id = ActionId, expectedControlType = "Button" } },
                bindings = new[]
                {
                    new { name = "Modifiers", id = "ef92e3a2-47c9-49ea-9e51-f6cbaecbd58c", path = "Modifiers", action = ActionName, isComposite = true, isPartOfComposite = false },
                    new { name = "modifier1", id = "aa6bd064-495e-4aba-a1e1-6bb97602ac85", path = "<Keyboard>/alt", action = ActionName, isComposite = false, isPartOfComposite = true },
                    new { name = "binding", id = "d6c4ac5a-22b4-484a-b471-b6763e98d3e8", path = "<Keyboard>/t", action = ActionName, isComposite = false, isPartOfComposite = true }
                }
            } } });
    }
}
