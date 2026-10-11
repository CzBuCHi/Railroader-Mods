using System;

namespace TurntableControl;

// Independent of Unity so wraparound and arrival can be tested without the game.
internal sealed class RotationGoal
{
    internal const float ArrivalTolerance = 0.001f;

    internal float TargetAngle { get; }
    internal int Direction { get; }

    internal RotationGoal(float currentAngle, float targetAngle)
    {
        TargetAngle = Normalize(targetAngle);
        Direction = SignedDelta(currentAngle, TargetAngle) >= 0f ? 1 : -1;
    }

    internal float DistanceFrom(float angle)
    {
        return Normalize(Direction * (TargetAngle - angle));
    }

    internal bool ReachedDuringStep(float before, float after)
    {
        var gap = DistanceFrom(before);
        if (gap <= ArrivalTolerance)
            return true;

        // Motion away from the goal (native inertia after retargeting) is not arrival.
        var travel = Direction * SignedDelta(before, after);
        return travel > 0f && travel + ArrivalTolerance >= gap;
    }

    internal static float Normalize(float angle)
    {
        if (float.IsNaN(angle) || float.IsInfinity(angle))
            throw new ArgumentOutOfRangeException(nameof(angle));
        return (angle % 360f + 360f) % 360f;
    }

    internal static float SignedDelta(float from, float to)
    {
        var delta = Normalize(to - from);
        return delta > 180f ? delta - 360f : delta;
    }
}
