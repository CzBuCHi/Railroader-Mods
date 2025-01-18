using System;
using System.Linq.Expressions;
using System.Reflection;
using Cameras;

namespace Teleporter.Extensions;

public static class StrategyCameraControllerExtensions {
    private static readonly Func<StrategyCameraController, float>   _GetDistance;
    private static readonly Action<StrategyCameraController, float> _SetDistance;
    private static readonly Action<StrategyCameraController, float> _SetAngleX;

    static StrategyCameraControllerExtensions() {
        var distanceField = typeof(StrategyCameraController).GetField("_distance", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var angleXField   = typeof(StrategyCameraController).GetField("_angleX", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var controllerParam = Expression.Parameter(typeof(StrategyCameraController));
        var distanceGetter = Expression.Lambda<Func<StrategyCameraController, float>>(
            Expression.Field(controllerParam, distanceField),
            controllerParam
        );
        _GetDistance = distanceGetter.Compile();

        var distanceValue = Expression.Parameter(typeof(float));
        var distanceSetter = Expression.Lambda<Action<StrategyCameraController, float>>(
            Expression.Assign(Expression.Field(controllerParam, distanceField), distanceValue),
            controllerParam, distanceValue
        );
        _SetDistance = distanceSetter.Compile();

        var angleXValue = Expression.Parameter(typeof(float));
        var angleXSetter = Expression.Lambda<Action<StrategyCameraController, float>>(
            Expression.Assign(Expression.Field(controllerParam, angleXField), angleXValue),
            controllerParam, angleXValue
        );
        _SetAngleX = angleXSetter.Compile();
    }

    public static float GetDistance(this StrategyCameraController controller) => _GetDistance(controller);

    public static void SetDistance(this StrategyCameraController controller, float value) => _SetDistance(controller, value);

    public static void SetAngleX(this StrategyCameraController controller, float value) => _SetAngleX(controller, value);
}
