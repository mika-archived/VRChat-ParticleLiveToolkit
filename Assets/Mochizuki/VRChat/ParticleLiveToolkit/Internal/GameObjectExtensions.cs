using System.Collections.Generic;

using UnityEngine;

namespace Mochizuki.VRChat.ParticleLiveToolkit.Internal
{
    internal static class GameObjectExtensions
    {
        public static string GetRelativePathFor(this GameObject obj, GameObject child)
        {
            var paths = new List<string>();
            var current = child.transform;

            while (current != null && current != obj.transform)
            {
                paths.Add(current.name);
                current = current.parent;
            }

            paths.Reverse();
            return string.Join("/", paths);
        }
    }
}