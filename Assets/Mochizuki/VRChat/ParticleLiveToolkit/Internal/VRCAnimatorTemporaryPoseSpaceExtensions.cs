using VRC.SDK3.Avatars.Components;

namespace Mochizuki.VRChat.ParticleLiveToolkit.Internal
{
    // ReSharper disable once InconsistentNaming
    internal static class VRCAnimatorTemporaryPoseSpaceExtensions
    {
        public static void ApplyTo(this VRCAnimatorTemporaryPoseSpace source, VRCAnimatorTemporaryPoseSpace dest)
        {
            dest.ApplySettings = source.ApplySettings;
            dest.delayTime = source.delayTime;
            dest.enterPoseSpace = source.enterPoseSpace;
            dest.fixedDelay = source.fixedDelay;
            dest.debugString = source.debugString;
            dest.name = source.name;
            dest.hideFlags = source.hideFlags;
        }
    }
}