using System;
using System.Linq;
using AutoTrestle;
using Newtonsoft.Json.Linq;
using StrangeCustoms;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapEditor.Features.AutoTrestles;

// modified version of StrangeCustoms.AutoTrestleBuilder
internal class AutoTrestleBuilder : ISplineyBuilder
{
    private AutoTrestleProfile? _Profile;

    public GameObject BuildSpliney(string id, Transform parentTransform, JObject data) {
        var autoTrestleData = data.ToObject<AutoTrestleData>()!;
        return BuildAutoTrestle(id, parentTransform, autoTrestleData);
    }

    public GameObject BuildAutoTrestle(string id, Transform parentTransform, AutoTrestleData autoTrestleData) {
        if (_Profile == null) {
            _Profile = Object.FindObjectOfType<AutoTrestle.AutoTrestle>()!.profile!;
        }

        if (autoTrestleData?.Points == null || autoTrestleData.Points.Length == 0) {
            throw new ArgumentException("No points supplied");
        }

        var gameObject = new GameObject(id);
        gameObject.SetActive(false);
        gameObject.transform.SetParent(parentTransform, false);

        var center = autoTrestleData.Points.Aggregate(Vector3.zero, (a, b) => a + b.Position) / autoTrestleData.Points.Length;
        gameObject.transform.localPosition = center;

        var autoTrestle = gameObject.AddComponent<AutoTrestle.AutoTrestle>();
        autoTrestle.controlPoints = autoTrestleData.Points.Select(s => new AutoTrestle.AutoTrestle.ControlPoint {
            position = s.Position - center,
            rotation = Quaternion.Euler(s.Rotation)
        }).ToList();

        autoTrestle.headStyle = autoTrestleData.HeadStyle;
        autoTrestle.tailStyle = autoTrestleData.TailStyle;
        autoTrestle.profile = _Profile;
        gameObject.SetActive(true);
        return gameObject;
    }

    public void RemoveTrestle(string id, Transform parentTransform) {
        var trestle = parentTransform.GetComponentsInChildren<AutoTrestle.AutoTrestle>()?.FirstOrDefault(o => o.name == id);
        if (trestle != null) {
            Object.Destroy(trestle.gameObject);
        }
    }
}
