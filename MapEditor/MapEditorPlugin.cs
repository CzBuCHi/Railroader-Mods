using System;
using System.IO;
using System.Reflection;
using System.Text;
using CzBuCHi.Shared.Harmony;
using CzBuCHi.Shared.UI;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Helpers;
using JetBrains.Annotations;
using KeyValue.Runtime;
using MapEditor.Behaviours;
using MapEditor.UI;
using MapEditor.Utility;
using Newtonsoft.Json;
using Railloader;
using RollingStock.Controls;
using Serilog;
using StrangeCustoms.Tracks;
using Track;
using UI.Builder;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Object = UnityEngine.Object;

namespace MapEditor;

[PublicAPI]
public sealed class MapEditorPlugin(IModdingContext context, IUIHelper uiHelper) : SingletonPluginBase<MapEditorPlugin>, IModTabHandler
{
    private const string PluginIdentifier = "CzBuCHi.MapEditor";

    public IModdingContext Context  => context;
    public IUIHelper       UiHelper => uiHelper;

    public override void OnEnable() {
        var harmony = new HarmonyLib.Harmony(PluginIdentifier);
        harmony.PatchAll();

        Messenger.Default.Register(this, new Action<MapDidLoadEvent>(OnMapDidLoad));
        Messenger.Default.Register(this, new Action<MapDidUnloadEvent>(OnMapDidUnload));

        ProgrammaticWindowCreatorPatches.RegisterWindow<EditorWindow>();
        ProgrammaticWindowCreatorPatches.RegisterWindow<MilestonesWindow>();
        ProgrammaticWindowCreatorPatches.RegisterWindow<SceneWindow>();
    }

    public override void OnDisable() {
        var harmony = new HarmonyLib.Harmony(PluginIdentifier);
        harmony.UnpatchAll();

        Messenger.Default.Unregister(this);
    }

    private void OnMapDidLoad(MapDidLoadEvent @event) {
        TopRightArea.AddButton("MapEditor.icon.png", "Map Editor", 9, EditorWindow.Toggle);

        UnityHelpers.Initialize();
    }

    private void OnMapDidUnload(MapDidUnloadEvent @event) {
        EditorState.Reset();
        UnityHelpers.Destroy();
    }

    public void ModTabDidOpen(UIPanelBuilder builder) {
        builder.AddButton("Milestones Manager", MilestonesWindow.Toggle);
        builder.AddButton("Scene Viewer", SceneWindow.Toggle);
        builder.AddButton("Export Scenery",  Exporter.ExportScenery);
        //builder.AddButton("Testing", Testing);
    }

    public void ModTabDidClose() {
    }

    //private static Vector3? _NodeStartPosition;
    //private static void Select(GameObject gameObject)  {

    //    Log.Information("Select: " + gameObject);

    //    //var moveableObject = gameObject.AddComponent<MoveableObject>();
    //    //moveableObject.OnDragStart = () => {
    //    //    Log.Information("OnDragStart");
    //    //    _NodeStartPosition = gameObject.transform.localPosition;
    //    //};
    //    //moveableObject.OnDrag = dragOffset => {
    //    //    Log.Information("OnDrag: " + dragOffset);
    //    //    gameObject.transform.localPosition = _NodeStartPosition!.Value + dragOffset;
    //    //};
    //    //moveableObject.OnDragEnd = dragOffset => {
    //    //    Log.Information("OnDragEnd: " + dragOffset);
    //    //    _NodeStartPosition = null;
    //    //    Object.Destroy(moveableObject);
    //    //};
    //}

    //public static void Testing() {
        
    //    WorldTransformer.TryGetShared(out var worldTransformer);        
    //    var original = GetDeep(worldTransformer.gameObject, "Large Scenery/Whittier/Water Column");


    //    // vytvorit TrackMarkerPlacer  : neco jako ConsistPlacer akorat bude nastavovat pozici TrackMarker
        
    //    // napozicovat clone tka, aby jeho tackmarkr byl podle ^

        

    //    var dummy = new GameObject();
    //    dummy.SetActive(false);
        
    //    var clone = Object.Instantiate(original, original.transform.position + new Vector3(5, 0, 0), original.transform.rotation, dummy.transform);

    //    var globalKeyValueObject = clone.GetComponent<GlobalKeyValueObject>();
    //    globalKeyValueObject.globalObjectId += "_clone";
                
    //    var trackMarker = clone.GetComponent<TrackMarker>();
    //    trackMarker.id += "_clone";


        
        
    //    // tohle volat v update
    //    //trackMarker.Location = ConsistPlacer.HitLocation();
        


    //    clone.transform.SetParent(original.transform.parent);
        
    //    Object.Destroy(dummy);
        
       
    //    Exporter.Export(original);
    //    Exporter.Export(clone);
    //    //Select(clone);
    //}
    
    //private static GameObject? GetDeep(GameObject root, string path) {
    //    var go = root;
    //    foreach (var part in path.Split('/')) {
    //        go = GetChildByName(go, part);
    //        if (go == null) {
    //            return null;
    //        }
    //    }

    //    return go;

    //    GameObject? GetChildByName(GameObject parent, string name) {
    //        for (var i = 0; i < parent.transform.childCount; i++) {
    //            var child = parent.transform.GetChild(i);
    //            if (child.name == name) {
    //                return child.gameObject;
    //            }
    //        }

    //        return null;
    //    }
    //}

    public static PatchEditor? PatchEditor { get; set; }
}