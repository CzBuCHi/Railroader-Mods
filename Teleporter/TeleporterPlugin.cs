using System;
using System.IO;
using HarmonyLib;
using Railroader.Modding;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Teleporter
{
    public sealed class TeleporterPlugin : IModEntryPoint, IWorldLifecycle
    {
        internal const string Identifier = "czbuchi/teleporter";
        internal const string MainWindow = "czbuchi.teleporter.main";
        internal const string EditorWindow = "czbuchi.teleporter.editor";
        internal static TeleporterPlugin? Active { get; private set; }
        internal BookmarkStore Store { get; private set; } = null!;
        internal TeleporterRuntime Runtime { get; private set; } = null!;
        internal IModContext Context { get; private set; } = null!;
        private Harmony? harmony;

        public void OnModLoad(IModContext context)
        {
            if (Active != null) return;
            Context = context;
            try
            {
                Store = new BookmarkStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Railroader", "ModSettings", "czbuchi", "teleporter.json"));
                RelayBridge.Validate();
                CameraBookmarks.Validate();
                Active = this;
                harmony = new Harmony("CzBuCHi.Teleporter.Native");
                harmony.PatchAll(typeof(TeleporterPlugin).Assembly);
                var host = new GameObject("CzBuCHi.Teleporter.Runtime");
                Object.DontDestroyOnLoad(host);
                Runtime = host.AddComponent<TeleporterRuntime>();
                Runtime.Initialize(this);
                context.Log.Info("Loaded Teleporter 3.0.1 with Relay UI.");
            }
            catch
            {
                OnModUnload();
                throw;
            }
        }

        public void OnWorldLoad() => Runtime?.SetMapLoaded(true);
        public void OnWorldUnload() => Runtime?.SetMapLoaded(false);

        public void OnModUnload()
        {
            if (Runtime != null)
            {
                Runtime.SetMapLoaded(false);
                Object.Destroy(Runtime.gameObject);
                Runtime = null!;
            }
            harmony?.UnpatchAll("CzBuCHi.Teleporter.Native");
            harmony = null;
            if (Active == this) Active = null;
        }

        internal static TeleporterPlugin RequireActive() => Active ?? throw new InvalidOperationException("Teleporter is disabled.");
    }
}
