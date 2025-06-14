using MapEditor.Behaviours;
using MapEditor.MapState;
using MapEditor.Utility;
using Serilog;
using UnityEngine;

namespace MapEditor.Features.TelegraphPoles;

public sealed class TelegraphPoleModeHandler(int telegraphPoleId, MoveableObjectMode mode) : IMoveableObjectHandler
{
    public GameObject         GameObject => TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId).gameObject;
    public MoveableObjectMode Mode       { get; } = mode;

    public Vector3    StartPosition { get; private set; }
    public Quaternion StartRotation { get; private set; }

    public void OnStart() {
        var telegraphPole = TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId);
        StartPosition = telegraphPole.transform.localPosition;
        StartRotation = telegraphPole.transform.localRotation;
    }

    public void OnUpdate(Vector3? translation, Quaternion? rotation) {
        var telegraphPole = TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId);

        if (translation.HasValue) {
            telegraphPole.transform.localPosition = StartPosition + translation.Value;
        }

        if (rotation.HasValue) {
            Log.Information("Rotation: " + telegraphPole.transform.localRotation + " -> " + StartRotation * rotation.Value);
            telegraphPole.transform.localRotation = StartRotation * rotation.Value;
        }
    }

    public IStateStep OnComplete(Vector3? translation, Quaternion? rotation) {
        var node          = TelegraphPoleUtility.Graph.NodeForId(telegraphPoleId)!;
        var telegraphPole = TelegraphPoleUtility.GetTelegraphPole(telegraphPoleId);

        UnityHelpers.CallOnNextFrame(() => MoveableObject.Create(this));

        Vector3? newRotation = null;
        if (rotation != null) {
            var parentRotation       = telegraphPole.transform.parent.rotation;
            var graphRotation        = TelegraphPoleUtility.Graph.transform.rotation;
            var desiredLocalRotation = StartRotation * rotation.Value;
            var nodeRotation         = Quaternion.Inverse(graphRotation) * parentRotation * desiredLocalRotation;
            newRotation = nodeRotation.eulerAngles;
        }

        return new TelegraphPoleUpdate(telegraphPoleId) {
            OriginalPosition = translation != null ? node.position : null,
            OriginalRotation = rotation != null ? node.eulerAngles : null,
            Position = translation != null ? node.position + translation.Value : null,
            Rotation = newRotation
        };
    }
}
