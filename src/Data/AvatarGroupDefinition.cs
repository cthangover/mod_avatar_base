using System.Collections.Generic;

namespace Cthangover.Mods.Avatars
{
    /// <summary>Exactly one preset is selected per group. Higher priority groups override lower ones.</summary>
    public sealed class AvatarGroupDefinition
    {
        /// <summary>Initial selection; also the selection to request when resetting this group.</summary>
        public string Default { get; set; }

        /// <summary>Composition order, ascending; ties use ordinal group names, never command order.</summary>
        public int Priority { get; set; }

        /// <summary>Named states available to scenario authors. An empty preset can delegate to earlier groups.</summary>
        public Dictionary<string, AvatarPresetDefinition> Presets { get; set; } = new();
    }
}
