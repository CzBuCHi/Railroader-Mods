using System;
using MapEditor.Behaviours;
using MapEditor.Extensions;
using MapEditor.MapState.AutoTrestleEditor.StrangeCustoms;
using MapEditor.MapState.TrackSegmentEditor;
using Serilog;
using Track;
using UI.Builder;
using UnityEngine;

namespace MapEditor.UI.Controls;

public static class TrackSegmentEditor
{
    private static string?                          _GroupId;
    private static int?                             _Priority;
    private static int?                             _SpeedLimit;
    private static TrackClass?                      _TrackClass;
    private static TrackSegment.Style?              _Style;
    private static bool?                            _Trestle;
    private static AutoTrestle.AutoTrestle.EndStyle _TrestleHead;
    private static AutoTrestle.AutoTrestle.EndStyle _TrestleTail;

    public static void Build(UIPanelBuilder builder, TrackSegment trackSegment) {
        builder.AddField("Id", builder.AddInputField(trackSegment.id, _ => { })).Disable(true);
        builder.AddField("Operation", builder.ButtonStrip(strip => {
            strip.AddButtonSelectable("None", MoveableObject.ActiveMode == MoveableObjectMode.None, SetOperation(strip, MoveableObjectMode.None));
            strip.AddButtonSelectable("Move", MoveableObject.ActiveMode == MoveableObjectMode.Move, SetOperation(strip, MoveableObjectMode.Move));
            strip.AddButtonSelectable("Rotate", MoveableObject.ActiveMode == MoveableObjectMode.Rotate, SetOperation(strip, MoveableObjectMode.Rotate));
        }));

        AddField(builder, "Group ID", _GroupId != null && _GroupId != trackSegment.groupId,
            builder.AddInputField(_GroupId ?? trackSegment.groupId ?? "", UpdateGroupId)
        );
        AddField(builder, "Priority", _Priority != null && _Priority != trackSegment.priority,
            builder.AddSliderQuantized(
                () => _Priority ?? trackSegment.priority,
                () => (_Priority ?? trackSegment.priority).ToString("0"),
                o => _Priority = (int)o, 1, -2, 2, UpdatePriority
            )!
        );
        AddField(builder, "Speed Limit", _SpeedLimit != null && _SpeedLimit != trackSegment.speedLimit,
            builder.AddSliderQuantized(
                () => _SpeedLimit ?? trackSegment.speedLimit,
                () => (_SpeedLimit ?? trackSegment.speedLimit).ToString("0"),
                o => _SpeedLimit = (int)o, 5, 0, 45, UpdateSpeedLimit
            )!
        );
        AddField(builder, "Track Class", _TrackClass != null && _TrackClass != trackSegment.trackClass,
            builder.AddEnumDropdown(_TrackClass ?? trackSegment.trackClass, UpdateTrackClass)
        );
        AddField(builder, "Track Style", _Style != null && _Style != trackSegment.style,
            builder.AddEnumDropdown(_Style ?? trackSegment.style, UpdateStyle)
        );

        if ((_Style ?? trackSegment.style) == TrackSegment.Style.Bridge) {
            var data = MapEditorPlugin.PatchEditor!.GetAutoTrestle(trackSegment);
            AddField(builder, "Trestle", _Trestle != null && _Trestle != (data != null),
                builder.AddToggle(() => _Trestle ?? data != null, UpdateTrestle)!
            );
            if (_Trestle == true || data != null) {
                AddField(builder, "Trestle Head Style", _TrestleHead != data?.HeadStyle,
                    builder.AddEnumDropdown(_TrestleHead, UpdateTrestleHead)
                );
                AddField(builder, "Trestle Tail Style", _TrestleTail != data?.TailStyle,
                    builder.AddEnumDropdown(_TrestleTail, UpdateTrestleTail)
                );
            }
        }

        builder.AddSection("Operations", section => {
            section.ButtonStrip(strip => {
                strip.AddButton("Update properties", UpdateSegmentProperties);
                strip.AddButton("Remove", () => TrackSegmentUtility.Remove(trackSegment));
                strip.AddButton("Inject Node", () => TrackSegmentUtility.InjectNode(trackSegment));
                strip.AddButton("Straighten", () => TrackSegmentUtility.Straighten(trackSegment));
            });
        });

        return;

        Action SetOperation(UIPanelBuilder strip, MoveableObjectMode mode) => () => {
            MoveableObject.Create(new TrackSegmentMoveHandler(trackSegment, mode));
            strip.Rebuild();
        };

        void UpdateGroupId(string value) {
            Log.Information("UpdateGroupId: " + value);
            _GroupId = value;
            builder.Rebuild();
        }

        void UpdatePriority(float value) {
            Log.Information("UpdatePriority: " + value);
            _Priority = (int)value;
            builder.Rebuild();
        }

        void UpdateSpeedLimit(float value) {
            Log.Information("UpdateSpeedLimit: " + value);
            _SpeedLimit = (int)value;
            builder.Rebuild();
        }

        void UpdateTrackClass(TrackClass value) {
            Log.Information("UpdateTrackClass: " + value);
            _TrackClass = value;
            builder.Rebuild();
        }

        void UpdateStyle(TrackSegment.Style value) {
            Log.Information("UpdateStyle: " + value);
            _Style = value;
            builder.Rebuild();
        }

        void UpdateTrestle(bool value) {
            Log.Information("UpdateTrestle: " + value);
            _Trestle = value;
            builder.Rebuild();
        }

        void UpdateTrestleHead(AutoTrestle.AutoTrestle.EndStyle value) {
            Log.Information("UpdateTrestleHead: " + value);
            _TrestleHead = value;
            builder.Rebuild();
        }

        void UpdateTrestleTail(AutoTrestle.AutoTrestle.EndStyle value) {
            Log.Information("UpdateGroupId: " + value);
            _TrestleTail = value;
            builder.Rebuild();
        }

        void UpdateSegmentProperties() {
            TrackSegmentUtility.UpdateSegment(trackSegment, _GroupId, _Priority, _SpeedLimit, _TrackClass, _Style, _Trestle, _TrestleHead, _TrestleTail);
            _GroupId = null;
            _Priority = null;
            _SpeedLimit = null;
            _TrackClass = null;
            _Style = null;
            _Trestle = null;
            builder.Rebuild();
        }
    }

    private static void AddField(UIPanelBuilder builder, string label, bool changed, RectTransform control) => builder.AddField(changed ? label.Color("FFFFFF")! : label, control);
}
