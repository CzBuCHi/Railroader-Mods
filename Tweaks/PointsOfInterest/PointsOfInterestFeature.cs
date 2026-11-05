using System;
using Tweaks.PointsOfInterest.Input;
using Railroader.Modding;
using Tweaks.PointsOfInterest.Bookmarks;
using Tweaks.PointsOfInterest.Camera;
using Tweaks.PointsOfInterest.UI;
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
                Store = new BookmarkStore(global::Tweaks.Configuration.Settings.FilePath);
                RelayBridge.Validate();
                CameraBookmarks.Validate();
                Active = this;
                var host = new GameObject("CzBuCHi.Tweaks.PointsOfInterest.Runtime");
                Object.DontDestroyOnLoad(host);
                Runtime = host.AddComponent<PointsOfInterestRuntime>();
                Runtime.Initialize(this);
                PointsOfInterestBinding.Enable(Store.Current);
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
            PointsOfInterestBinding.Disable();
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
