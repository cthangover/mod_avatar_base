using System.Collections.Generic;

namespace Cthangover.Mods.Avatars
{
    /// <summary>
    /// Serializable logical state only: no nodes, textures or interpolation progress.
    /// Capture returns a detached copy; restoring a partial map fills missing groups from defaults.
    /// </summary>
    public sealed class AvatarState
    {
        /// <summary>Definition identity prevents accidentally restoring another avatar's selections.</summary>
        public string AvatarId { get; set; }

        /// <summary>Selected preset per group. Unknown groups or presets are rejected on restore.</summary>
        public Dictionary<string, string> Presets { get; set; } = new();
    }
}
