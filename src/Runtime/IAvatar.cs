using System.Collections.Generic;

namespace Cthangover.Mods.Avatars
{
    /// <summary>
    /// One independent logical avatar instance. Named presets are the public control surface;
    /// rendering and transitions belong to an attached renderer, not to dialog or character data.
    /// </summary>
    public interface IAvatar
    {
        /// <summary>The resource whose defaults and bindings were snapshotted for this instance.</summary>
        string AvatarId { get; }

        /// <summary>
        /// Atomically replaces selections for specified groups and retains all other selections.
        /// Invalid names throw ArgumentException without changing the instance. Null is not a reset.
        /// </summary>
        void ApplyPresets(IReadOnlyDictionary<string, string> changes);

        /// <summary>Captures a detached, JSON-serializable logical state for persistence.</summary>
        AvatarState CaptureState();

        /// <summary>Composes a complete immutable renderer target from baseline and current presets.</summary>
        AvatarFrame Resolve();
    }
}
