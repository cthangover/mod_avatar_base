using System.Collections.Generic;
using System.Text.Json;

namespace Cthangover.Mods.Avatars
{
    /// <summary>A named state contributes renderer-specific target bindings, not incremental changes.</summary>
    public sealed class AvatarPresetDefinition
    {
        /// <summary>
        /// Opaque binding values: a layers renderer may read image IDs, a Live2D renderer numbers.
        /// A key is replaced as a whole; objects are not recursively merged and null is a literal value.
        /// </summary>
        public Dictionary<string, JsonElement> Values { get; set; } = new();
    }
}
