namespace Tweaks;

public sealed class Settings
{
    public bool SleepUntilNextInterchange { get; set; } = true;
    public bool ShowCarSpeed { get; set; } = true;
    public bool ShowCarOil { get; set; } = true;
    public bool FollowAndSelect { get; set; } = true;
    public bool FastStrongMan { get; set; } = true;
    public bool CopyCrew { get; set; } = true;
    public bool CopyRepairDestination { get; set; } = true;
    public bool ConsistActionButtons { get; set; } = true;
    public float OilWarningThreshold { get; set; } = 0.5f;
    public bool WaypointShortcutAllModes { get; set; } = true;
    public bool ControlModeButtons { get; set; } = true;
    public bool RepairSoundButtons { get; set; } = true;
    public bool ManualSoundInstallationDelay { get; set; } = true;
    public bool PrankLettering { get; set; } = true;
    public bool RepairSoundPrank { get; set; } = true;
    public bool BleedAll { get; set; } = true;
}
