using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cthangover.Mods.Avatars
{
    /// <summary>
    /// Renderer-independent implementation. Owns a definition snapshot and recomposes full targets
    /// so leaving a preset also removes its contributions. No shared entity data is ever mutated.
    /// Instances are intended for their owning UI thread; they do not synchronize concurrent writes.
    /// </summary>
    public sealed class AvatarInstance : IAvatar
    {
        private readonly AvatarDefinition definition;
        private readonly Dictionary<string, string> selections = new(StringComparer.Ordinal);

        /// <summary>Creates defaults, optionally overlaid with validated saved selections.</summary>
        public AvatarInstance(AvatarDefinition definition, AvatarState state = null)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            definition.Validate();
            this.definition = JsonSerializer.Deserialize<AvatarDefinition>(JsonSerializer.Serialize(definition));
            foreach (var group in this.definition.Groups) selections.Add(group.Key, group.Value.Default);
            if (state != null)
            {
                if (!string.Equals(state.AvatarId, AvatarId, StringComparison.Ordinal))
                    throw new ArgumentException($"Saved avatar '{state.AvatarId}' does not match '{AvatarId}'.");
                ApplyPresets(state.Presets);
            }
        }

        /// <inheritdoc/>
        public string AvatarId => definition.ID;

        /// <inheritdoc/>
        public void ApplyPresets(IReadOnlyDictionary<string, string> changes)
        {
            if (changes == null) throw new ArgumentNullException(nameof(changes));
            // Snapshot the caller's map before validation; commit only after the entire patch is valid.
            var pending = new Dictionary<string, string>(changes, StringComparer.Ordinal);
            foreach (var change in pending)
            {
                if (!definition.Groups.TryGetValue(change.Key, out var group))
                    throw new ArgumentException($"Avatar '{AvatarId}': unknown group '{change.Key}'.");
                if (change.Value == null || !group.Presets.ContainsKey(change.Value))
                    throw new ArgumentException($"Avatar '{AvatarId}', group '{change.Key}': unknown preset '{change.Value}'.");
            }
            foreach (var change in pending) selections[change.Key] = change.Value;
        }

        /// <inheritdoc/>
        public AvatarState CaptureState() => new()
        {
            AvatarId = AvatarId,
            Presets = new Dictionary<string, string>(selections, StringComparer.Ordinal)
        };

        /// <inheritdoc/>
        public AvatarFrame Resolve()
        {
            var values = new Dictionary<string, JsonElement>(definition.BaseValues, StringComparer.Ordinal);
            foreach (var group in definition.Groups.OrderBy(Priority).ThenBy(Name, StringComparer.Ordinal))
                foreach (var value in group.Value.Presets[selections[group.Key]].Values)
                    values[value.Key] = value.Value;
            return new AvatarFrame(AvatarId, definition.Renderer, selections, values);
        }

        private static int Priority(KeyValuePair<string, AvatarGroupDefinition> group) => group.Value.Priority;
        private static string Name(KeyValuePair<string, AvatarGroupDefinition> group) => group.Key;
    }
}
