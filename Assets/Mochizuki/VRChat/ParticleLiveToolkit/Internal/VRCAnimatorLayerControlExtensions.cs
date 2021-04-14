using VRC.SDK3.Avatars.Components;

namespace Mochizuki.VRChat.ParticleLiveToolkit.Internal
{
    // ReSharper disable once InconsistentNaming
    internal static class VRCAnimatorLayerControlExtensions
    {
        public static void ApplyTo(this VRCAnimatorLayerControl source, VRCAnimatorLayerControl dest)
        {
            dest.layer = source.layer;
            dest.ApplySettings = source.ApplySettings;
            dest.blendDuration = source.blendDuration;
            dest.debugString = source.debugString;
            dest.goalWeight = source.goalWeight;
            dest.name = source.name;
            dest.playable = source.playable;
            dest.hideFlags = source.hideFlags;
        }
    }
}