using System;
using System.Collections.Generic;
using System.Linq;
using CarInspectorTweaks.Data;
using CarInspectorTweaks.Events;
using CarInspectorTweaks.Features;
using CzBuCHi.Shared.UI;
using GalaSoft.MvvmLight.Messaging;
using Game.Messages;
using Model;
using Serilog;
using UI.Builder;
using UI.Common;
using UnityEngine;
using ILogger = Serilog.ILogger;

namespace CarInspectorTweaks.UI;

public sealed class TimeNotificationWindow : ProgrammaticWindowBase
{
    public override Window.Sizing          Sizing          => Window.Sizing.Resizable(new Vector2Int(500, 250));
    public override Window.Position        DefaultPosition => Window.Position.CenterRight;
    public static   TimeNotificationWindow Shared          => WindowManager.Shared!.GetWindow<TimeNotificationWindow>()!;

    private readonly ILogger _Logger = Log.ForContext<TimeNotificationWindow>()!;

    private Messenger _Messenger = null!;

    public override void Awake() {
        base.Awake();
        Window.Title = "Time notifications Window";

        _Messenger = Messenger.Default!;
        _Messenger.Register(this, new Action<AddCars>(_ => GetLocomotives()));
        _Messenger.Register(this, new Action<RemoveCars>(_ => GetLocomotives()));
    }

    private void GetLocomotives () {
        _Logger.Information("Resolving locomotives ...");

        _Locomotives = TrainController.Shared!.Cars!.OfType<BaseLocomotive>().ToList();
        _LocomotiveNames = _Locomotives.Select(o => o.DisplayName).ToList();
    }

    private readonly UIState<string>                      _Selected        = new("");
    private readonly SortedList<string, TimeNotification> _Notifications   = [];
    private          List<BaseLocomotive>                 _Locomotives     = [];
    private          List<string>                         _LocomotiveNames = [];

    private string GetNewNotificationId() {
        string id;
        do {
            id = Guid.NewGuid().ToString("N").Substring(0, 8);
        } while (_Notifications.ContainsKey(id));
        return id;
    }

    protected override void Build(UIPanelBuilder builder) {
        if (_Selected.Value == "") {
            TimeNotificationsManager.FillSortedList(_Notifications);
            _Selected.Value = _Notifications.Count > 0 ? _Notifications.First().Key : "";
            GetLocomotives();
        }

        builder.RebuildOnEvent<TimeNotificationWindowRefresh>();

        builder.ButtonStrip(strip => {
            strip.AddButton("Create new", () => {
                var notification = new TimeNotification {
                    LocomotiveId = _Locomotives[0].id,
                    Identifier = GetNewNotificationId(),
                    Hour = 8,
                    Minute = 0,
                    Message = "It's 8 o'clock"
                };
                _Notifications.Add(notification.Identifier, notification);
                _Selected.Value = notification.Identifier;
                builder.Rebuild();
            });
        });

        var groups = _Notifications
                     .Select(o => new UIPanelBuilder.ListItem<TimeNotification>(o.Key, o.Value, "", o.Value.ToString()))
                     .ToList();

        builder.AddListDetail(groups, _Selected, BuildDetail);
    }

    private string? _Identifier;
    private string  _Message            = "";
    private int     _SelectedLocomotive;

    private readonly List<string> _Hours   = Enumerable.Range(0, 24).Select(o => o.ToString()).ToList();
    private readonly List<string> _Minutes = Enumerable.Range(0, 60).Select(o => o.ToString()).ToList();

    private int _Hour;
    private int _Minute;

    private void BuildDetail(UIPanelBuilder builder, TimeNotification? value) {
        _Logger.Information("BuildDetail: " + _Notifications.Count + " | " + _Selected.Value);
        if (value == null) {
            var message = _Notifications.Count > 0
                ? "Select notification to view details ..."
                : "Saved notifications will be shown here ...";
            builder.AddLabel(message);
            return;
        }

        if (_Selected.Value != _Identifier) {
            _SelectedLocomotive = _Locomotives.FindIndex(o => o.id == value.LocomotiveId);
            _Identifier = _Selected.Value;
            _Hour = value.Hour;
            _Minute = value.Minute;
            _Message = value.Message;
        }
        
        builder.AddField("Locomotive", builder.AddDropdown(_LocomotiveNames, _SelectedLocomotive, o => _SelectedLocomotive = o))
               .Tooltip("Locomotive", "Select locomotive, that will trigger notification.");

        builder.AddField("Time", builder.HStack(stack => {
            stack.AddDropdown(_Hours, _Hour, o => _Hour = o).FlexibleWidth();
            stack.AddDropdown(_Minutes, _Minute, o => _Minute = o).FlexibleWidth();
        }))
        .Tooltip("Time of day", "Enter time of dat for notification to occur.");

        builder.AddField("Message", builder.AddInputField(_Message, o => _Message = o))
               .Tooltip("Message", "Enter message that will be shown.");

        builder.ButtonStrip(strip => {
            strip.AddButton("Save changes", () => {
                value.Hour = _Hour;
                value.Minute = _Minute;
                value.Message = _Message;
                value.LocomotiveId = _Locomotives[_SelectedLocomotive].id;
                TimeNotificationsManager.SaveSortedList(_Notifications);
                CarInspectorTweaksPlugin.SendEvent(new TimeNotificationWindowRefresh());
            });

            strip.AddButton("Delete", () => {
                var index = _Notifications.IndexOfKey(_Selected.Value);
                if (index != -1 && index > 0) {
                    _Selected.Value = _Notifications.Keys[index - 1];
                }

                _Notifications.Remove(value.Identifier);
                TimeNotificationsManager.SaveSortedList(_Notifications);
                CarInspectorTweaksPlugin.SendEvent(new TimeNotificationWindowRefresh());
            });
        });
    }
}
