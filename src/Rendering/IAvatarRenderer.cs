using System;
using Godot;

namespace Cthangover.Mods.Avatars
{
    /// <summary>
    /// Optional presentation adapter implemented by a rendering mod. The caller attaches View to
    /// its UI container and disposes the adapter before that container is freed. The adapter owns
    /// its nodes and animations, but must not dispose borrowed cache-owned textures.
    /// </summary>
    public interface IAvatarRenderer : IDisposable
    {
        /// <summary>Renderer-owned visual root, created on the Godot main thread.</summary>
        Control View { get; }

        /// <summary>
        /// Applies a complete compatible target. Seconds must be finite and nonnegative; zero snaps.
        /// New targets interrupt previous transitions from the currently displayed state. Discrete
        /// renderers may snap even for positive duration. Missing bindings must not retain stale values.
        /// </summary>
        void Apply(AvatarFrame target, double transitionSeconds = 0);
    }
}
