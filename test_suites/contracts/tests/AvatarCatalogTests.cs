using System.Collections.Generic;
using Cthangover.Core.Mods.Testing;
using Cthangover.Mods.Avatars;

namespace Cthangover.Mods.Avatars.Contracts
{
    /// <summary>Exercises real Kind registration, mod entity resolution and independent catalog instances.</summary>
    public sealed class AvatarCatalogTests
    {
        [ModTest("avatar_catalog/resolved_entity_creates_independent_instances")]
        public static void Resource(ModTestContext ctx)
        {
            var first = AvatarCatalog.Create("avatar_contracts/sample");
            var second = AvatarCatalog.Create("avatar_contracts/sample");
            first.ApplyPresets(new Dictionary<string, string> { ["eyes_state"] = "closed" });
            ModAssert.CloseTo(0, first.Resolve().Values["eye"].GetDouble());
            ModAssert.CloseTo(1, second.Resolve().Values["eye"].GetDouble());
            var restored = AvatarCatalog.Create("avatar_contracts/sample", first.CaptureState());
            ModAssert.Equal("closed", restored.CaptureState().Presets["eyes_state"]);
        }
    }
}
