using System.Linq;
using Model;
using Newtonsoft.Json;

namespace CarInspectorTweaks.Data;

public sealed class TimeNotification
{
    public  string LocomotiveId { get; set; } = null!;
    public  string Identifier   { get; set; } = null!;
    public  string Message      { get; set; } = null!;
    public  string Time         { get; set; } = null!;

    private int _Hour;

    [JsonIgnore]
    public int Hour {
        get => _Hour;
        set => UpdateTime(value, _Minute);
    }

    private int _Minute;

    [JsonIgnore]
    public int Minute {
        get => _Minute;
        set => UpdateTime(_Hour, value);
    }

    [JsonIgnore]
    public float Hours { get; private set; }

    private void UpdateTime(int hour, int minute) {
        _Hour = hour;
        _Minute = minute; 
        Time = $"{Hour:D2}:{Minute:D2}";
        Hours = Hour + Minute / 60f;
    }

    public override string ToString() {
        var locomotive     = TrainController.Shared!.Cars!.OfType<BaseLocomotive>().FirstOrDefault(o => o.id == LocomotiveId);
        var locomotiveName = locomotive?.DisplayName ?? "Invalid locomotive id";
        return $"{locomotiveName} at {Time}: '{Message}'";
    }
}
