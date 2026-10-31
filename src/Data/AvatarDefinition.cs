using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cthangover.Core.Factories;

namespace Cthangover.Mods.Avatars
{
    /// <summary>
    /// Mod entity describing an avatar, not a party character. Presets contain opaque renderer
    /// bindings; the base never interprets a binding as a texture, a numeric parameter or a motion.
    /// Entity resolver inheritance and patches apply before an instance snapshots this definition.
    /// </summary>
    [EntityKind("avatar")]
    public sealed class AvatarDefinition : IIdentifiable
    {
        /// <summary>Entity identity used by content references and saved instance state.</summary>
        [JsonPropertyName("Id")]
        public string ID { get; set; }

        /// <summary>Renderer identifier; naming it does not load or require that renderer.</summary>
        public string Renderer { get; set; }

        /// <summary>Baseline bindings restored on every composition, before selected presets.</summary>
        public Dictionary<string, JsonElement> BaseValues { get; set; } = new();

        /// <summary>Author-defined groups, such as eyes_state, emotion or pose; keys are case-sensitive.</summary>
        public Dictionary<string, AvatarGroupDefinition> Groups { get; set; } = new();

        /// <summary>Rejects incomplete definitions before any instance or renderer can consume them.</summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ID)) throw new ArgumentException("Avatar Id is required.");
            if (string.IsNullOrWhiteSpace(Renderer)) throw new ArgumentException($"Avatar '{ID}': Renderer is required.");
            ValidateValues(BaseValues, "BaseValues");
            if (Groups == null) throw new ArgumentException($"Avatar '{ID}': Groups cannot be null.");
            foreach (var entry in Groups)
            {
                var group = entry.Value;
                if (string.IsNullOrWhiteSpace(entry.Key) || group == null)
                    throw new ArgumentException($"Avatar '{ID}': group names and definitions are required.");
                if (group.Presets == null || group.Presets.Count == 0 ||
                    string.IsNullOrWhiteSpace(group.Default) || !group.Presets.ContainsKey(group.Default))
                    throw new ArgumentException($"Avatar '{ID}', group '{entry.Key}': Default must name an existing preset.");
                foreach (var preset in group.Presets)
                {
                    if (string.IsNullOrWhiteSpace(preset.Key) || preset.Value == null)
                        throw new ArgumentException($"Avatar '{ID}', group '{entry.Key}': preset names and definitions are required.");
                    ValidateValues(preset.Value.Values, entry.Key + "/" + preset.Key);
                }
            }
        }

        private void ValidateValues(Dictionary<string, JsonElement> values, string path)
        {
            if (values == null) throw new ArgumentException($"Avatar '{ID}', {path}: values cannot be null.");
            foreach (var value in values)
                if (string.IsNullOrWhiteSpace(value.Key) || value.Value.ValueKind == JsonValueKind.Undefined)
                    throw new ArgumentException($"Avatar '{ID}', {path}: binding names and JSON values are required.");
        }
    }
}
