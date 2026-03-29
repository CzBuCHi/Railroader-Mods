using HarmonyLib;

namespace Teleporter.Extensions
{
    internal static class CameraSelectorExtensions
    {
        #region SelectCamera

        private delegate bool GetTimeStringDelegate(CameraSelector cameraSelector, CameraSelector.CameraIdentifier cameraIdentifier);

        private static readonly GetTimeStringDelegate _SelectCamera = AccessTools.MethodDelegate<GetTimeStringDelegate>(AccessTools.Method(typeof(CameraSelector), "SelectCamera")!)!;

        public static bool SelectCamera(this CameraSelector cameraSelector, CameraSelector.CameraIdentifier cameraIdentifier) => _SelectCamera(cameraSelector, cameraIdentifier);

        #endregion
    }
}
