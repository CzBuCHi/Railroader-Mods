using System;
using System.IO;
using Railroader.Modding;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tweaks.PointsOfInterest
{
    internal sealed class PointsOfInterestFeature
    {
        internal const string Identifier = "czbuchi/tweaks";
        internal const string MainWindow = "czbuchi.tweaks.poi.main";
        internal const string EditorWindow = "czbuchi.tweaks.poi.editor";
        internal static PointsOfInterestFeature? Active { get; private set; }
        internal BookmarkStore Store { get; private set; } = null!;
        internal PointsOfInterestRuntime Runtime { get; private set; } = null!;
        internal IModContext Context { get; private set; } = null!;

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
                var host = new GameObject("CzBuCHi.Tweaks.PointsOfInterest.Runtime");
                Object.DontDestroyOnLoad(host);
                Runtime = host.AddComponent<PointsOfInterestRuntime>();
                Runtime.Initialize(this);
                context.Log.Info("Loaded Points of interest with Relay UI.");
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
            if (Active == this) Active = null;
        }

        internal static PointsOfInterestFeature RequireActive() => Active ?? throw new InvalidOperationException("Points of interest is disabled.");
    }
}
