# Tweaks 2.0.23

Requires RTM.RailForge. Mod ID: CzBuCHi.Tweaks.

Selected CarInspectorTweaks features: sleep until the next interchange, whole-MPH car speed, oil level, Shift-follow-and-select, 6-MPH manual pushing, copy repair destination, copy crew, and bleed all.

The locomotive HUD adds control-mode icons, Shift+G waypoint selection from any mode, optional consist length in meters (Show train length in meters in settings), and conditional fix-air, release-handbrakes, and oil/hotbox warning icons.

Choose / test whistles opens a single-choice checkbox list with one-second previews. OK confirms; Cancel discards the choice. Manual whistle and bell changes require customization permission and a repair track. The optional five-game-minute installation uses a temporary waybill; disabling its settings toggle makes manual changes immediate.

The repair-shop prankster always waits five game minutes and keeps its work hidden. Manual confirmation overrides it for that visit. Optional lettering changes use the adjacent tender or the locomotive itself, as appropriate.

PrankTexts.txt contains editable English phrases: one physical line per choice, literal \n for a line break within a sign, maximum 100 characters. Blank lines are ignored; comments have no special meaning. Edit prank texts is available in settings while lettering changes are enabled.

Close the game and extract Mods/CzBuCHi.Tweaks into its game directory. Preserve your customized PrankTexts.txt when updating.

Build from the repository root with dotnet publish Tweaks/Tweaks.csproj -c Release. The ZIP is created in the repository bin directory. The release package includes the detailed guide at docs/Tweaks.md.
