using System.Collections.Generic;
using Cthangover.Core.Mods.Resolvers;

namespace Cthangover.Mods.Avatars
{
    /// <summary>Uses the normal entity resolver, including folder/ZIP mods, inheritance and patches.</summary>
    public static class AvatarCatalog
    {
        /// <summary>Creates independent state from the currently resolved resource; unknown IDs throw.</summary>
        public static IAvatar Create(string id, AvatarState state = null)
        {
            if (string.IsNullOrWhiteSpace(id) || !Entities.Collect<AvatarDefinition>().TryGetValue(id, out var definition))
                throw new KeyNotFoundException($"Avatar definition '{id}' was not found.");
            return new AvatarInstance(definition, state);
        }
    }
}
