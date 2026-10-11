using System;
using TurntableControl;

static void Check(bool condition, string name)
{
    if (!condition)
        throw new Exception(name);
}

Check(new RotationGoal(350, 10).Direction == 1, "shortest direction through zero clockwise");
Check(new RotationGoal(10, 350).Direction == -1, "shortest direction through zero counterclockwise");
Check(new RotationGoal(0, 180).Direction == 1, "deterministic half turn");
Check(new RotationGoal(0, 0).ReachedDuringStep(0, 0), "already lined");
Check(new RotationGoal(359.9f, 0).ReachedDuringStep(359.9f, 0.1f), "arrival across 360 clockwise");
Check(new RotationGoal(0.1f, 0).ReachedDuringStep(0.1f, 359.9f), "arrival across 360 counterclockwise");
Check(!new RotationGoal(0, 20).ReachedDuringStep(0, 0), "blocked movement is not arrival");
Check(!new RotationGoal(0, 20).ReachedDuringStep(0, 359.8f), "inertia away from new target is not arrival");
Check(!new RotationGoal(40, 20).ReachedDuringStep(40, 40.2f), "opposite retarget direction is not arrival");

var scenarios = 0;
foreach (var dt in new[] { 0.02f, 0.05f })
foreach (var initialSpeed in new[] { -10f, 0f, 10f })
for (var start = 0; start < 360; start += 17)
for (var target = 0; target < 360; target += 19)
{
    var goal = new RotationGoal(start, target);
    var angle = (float)start;
    var speed = initialSpeed;
    var arrived = false;
    for (var frame = 0; frame < 3000; frame++)
    {
        speed += (goal.Direction * 10f - speed) * dt;
        var before = angle;
        if (Math.Abs(speed) > 0.5f)
            angle = RotationGoal.Normalize(angle + speed * dt);
        if (!goal.ReachedDuringStep(before, angle))
            continue;
        var correction = Math.Abs(RotationGoal.SignedDelta(angle, goal.TargetAngle));
        Check(correction <= 10f * dt + RotationGoal.ArrivalTolerance + 0.0001f,
            "arrival correction must be within one native step");
        angle = goal.TargetAngle;
        arrived = true;
        break;
    }
    Check(arrived, "native full-lever rotation must reach target");
    Check(Math.Abs(RotationGoal.SignedDelta(angle, target)) <= 0.001f, "exact final angle");
    scenarios++;
}
Console.WriteLine("PASS: 9 edge cases and " + scenarios + " native-speed rotation scenarios.");
