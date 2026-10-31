using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Cthangover.Mods.Avatars
{
    /// <summary>Detached target snapshot. Renderers interpolate toward it or switch discrete assets.</summary>
    public sealed class AvatarFrame
    {
        /// <summary>Identity of the avatar producing this frame.</summary>
        public string AvatarId { get; }
        /// <summary>Renderer identifier from the definition; consumers must check compatibility.</summary>
        public string Renderer { get; }
        /// <summary>All active preset choices, including defaults.</summary>
        public IReadOnlyDictionary<string, string> Presets { get; }
        /// <summary>Complete composed targets. An absent binding is absent, not left over from a prior frame.</summary>
        public IReadOnlyDictionary<string, JsonElement> Values { get; }

        internal AvatarFrame(string id, string renderer, Dictionary<string, string> presets,
            Dictionary<string, JsonElement> values)
        {
            AvatarId = id;
            Renderer = renderer;
            Presets = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(presets));
            var copy = new Dictionary<string, JsonElement>();
            foreach (var value in values) copy.Add(value.Key, value.Value.Clone());
            Values = new ReadOnlyDictionary<string, JsonElement>(copy);
        }
    }
}
