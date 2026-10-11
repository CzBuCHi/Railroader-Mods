namespace Tweaks;

internal sealed class RepairStopState
{
    internal string? LastTrack { get; private set; }
    internal RepairStopState(string? lastTrack) { LastTrack = string.IsNullOrEmpty(lastTrack) ? null : lastTrack; }
    internal bool Observe(bool stopped, string? track)
    {
        if (!stopped) return false;
        if (string.IsNullOrEmpty(track)) { LastTrack = null; return false; }
        if (track == LastTrack) return false;
        LastTrack = track;
        return true;
    }
}
