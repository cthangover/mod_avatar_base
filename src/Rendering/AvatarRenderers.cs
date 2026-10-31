using System;
using System.Collections.Generic;
using Cthangover.Core.Mods.Consumers;

namespace Cthangover.Mods.Avatars
{
    /// <summary>
    /// Mod-owned factory registry using normal assembly load/unload notifications. No dependency on
    /// a concrete backend; duplicate IDs are errors rather than load-order-dependent replacements.
    /// </summary>
    public sealed class AvatarRenderers : ModTypeConsumer<IAvatarRendererFactory>
    {
        private static readonly Dictionary<string, IAvatarRendererFactory> factories = new(StringComparer.Ordinal);
        /// <inheritdoc/>
        public override string Id => "avatar_renderers";

        /// <summary>Creates the selected backend and applies the initial target; caller owns the result.</summary>
        public static IAvatarRenderer Create(IAvatar avatar)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            var target = avatar.Resolve();
            if (!factories.TryGetValue(target.Renderer, out var factory))
                throw new KeyNotFoundException($"Avatar renderer '{target.Renderer}' is not loaded.");
            var renderer = factory.Create() ?? throw new InvalidOperationException($"Avatar renderer '{target.Renderer}' returned null.");
            try { renderer.Apply(target); return renderer; }
            catch { renderer.Dispose(); throw; }
        }

        /// <inheritdoc/>
        protected override void Add(IAvatarRendererFactory instance, string modId)
        {
            if (string.IsNullOrWhiteSpace(instance.Id)) throw new ArgumentException($"Mod '{modId}' supplied an empty avatar renderer Id.");
            if (!factories.TryAdd(instance.Id, instance))
                throw new InvalidOperationException($"Duplicate avatar renderer '{instance.Id}' from '{modId}'.");
        }

        /// <inheritdoc/>
        protected override void Remove(IAvatarRendererFactory instance, string modId)
        {
            if (factories.TryGetValue(instance.Id, out var current) && ReferenceEquals(current, instance))
                factories.Remove(instance.Id);
        }
    }
}
