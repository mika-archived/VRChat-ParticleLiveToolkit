using UnityEditor.Animations;

namespace Mochizuki.VRChat.ParticleLiveToolkit.Internal
{
    internal static class AnimatorControllerLayerExtensions
    {
        public static AnimatorControllerLayer CloneDeep(this AnimatorControllerLayer source)
        {
            return new AnimatorControllerLayer
            {
                stateMachine = source.stateMachine.CloneDeep(),
                avatarMask = source.avatarMask,
                blendingMode = source.blendingMode,
                defaultWeight = source.defaultWeight,
                iKPass = source.iKPass,
                name = source.name,
                syncedLayerAffectsTiming = source.syncedLayerAffectsTiming,
                syncedLayerIndex = source.syncedLayerIndex
            };
        }
    }
}