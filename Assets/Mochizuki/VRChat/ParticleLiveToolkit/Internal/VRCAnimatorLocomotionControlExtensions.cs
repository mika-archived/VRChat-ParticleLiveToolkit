using VRC.SDK3.Avatars.Components;

namespace Mochizuki.VRChat.ParticleLiveToolkit.Internal
{
    // ReSharper disable once InconsistentNaming
    internal static class VRCAnimatorLocomotionControlExtensions
    {
        public static void ApplyTo(this VRCAnimatorLocomotionControl source, VRCAnimatorLocomotionControl dest)
        {
            dest.ApplySettings = source.ApplySettings;
            dest.debugString = source.debugString;
            dest.disableLocomotion = source.disableLocomotion;
            dest.hideFlags = source.hideFlags;
            dest.name = source.name;
        }
    }
}