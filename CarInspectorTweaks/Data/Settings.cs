namespace CarInspectorTweaks.Data;

public class Settings
{
    public bool               WhistleButtons           { get; set; }
    public bool               FastStrongMan            { get; set; }
    public bool               CopyRepairDestination    { get; set; }
    public bool               ShowCarSpeed             { get; set; }
    public bool               BleedAll                 { get; set; }
    public bool               UpdateCarCustomizeWindow { get; set; }
    public bool               ConsistManage            { get; set; }
    public float              OilThreshold             { get; set; }
    public bool               ShowCarOil               { get; set; }
    public int                YardMaxSpeed             { get; set; } = 15;
    public bool               SetCarInspectorHeight    { get; set; }
    public int                CarInspectorHeight       { get; set; } = 500;
    public bool               CopyCrew                 { get; set; }
    public bool               AutoOpenConsistWindow    { get; set; }
    public bool               FollowAndSelect          { get; set; }
    public bool               TimeNotificationsEnabled { get; set; }
    public TimeNotification[] TimeNotifications        { get; set; } = [];
}