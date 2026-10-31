using System;
using System.Collections.Generic;
using System.Text.Json;
using Cthangover.Core.Mods.Testing;

namespace Cthangover.Mods.Avatars.Tests
{
    /// <summary>Resource serialization preserves opaque renderer values and rejects malformed preset groups.</summary>
    public sealed class AvatarDefinitionTests
    {
        [ModTest("avatar/definition_json_roundtrip_preserves_renderer_data")]
        public static void RoundTrip(ModTestContext ctx)
        {
            var original = AvatarInstanceTests.Definition();
            original.Groups["eyes_state"].Presets["opened"].Values["layer"] = JsonSerializer.SerializeToElement("eyes/opened");
            original.BaseValues["options"] = JsonSerializer.SerializeToElement(new { enabled = true, offset = new[] { 1, 2 } });
            original.BaseValues["hidden"] = JsonSerializer.SerializeToElement<object>(null);
            var copy = JsonSerializer.Deserialize<AvatarDefinition>(JsonSerializer.Serialize(original));
            copy.Validate();
            ModAssert.Equal(original.ID, copy.ID); ModAssert.Equal(original.Renderer, copy.Renderer);
            ModAssert.Equal(100, copy.Groups["eyes_state"].Priority);
            ModAssert.Equal("auto", copy.Groups["eyes_state"].Default);
            ModAssert.Equal("eyes/opened", copy.Groups["eyes_state"].Presets["opened"].Values["layer"].GetString());
            ModAssert.True(copy.BaseValues["options"].GetProperty("enabled").GetBoolean());
            ModAssert.Equal(2, copy.BaseValues["options"].GetProperty("offset")[1].GetInt32());
            ModAssert.True(copy.BaseValues["hidden"].ValueKind == JsonValueKind.Null);
            ModAssert.CloseTo(0.3, copy.Groups["emotion"].Presets["angry"].Values["eye"].GetDouble());
        }

        [ModTest("avatar/invalid_definitions_are_rejected_before_instantiation")]
        public static void Invalid(ModTestContext ctx)
        {
            Reject(new AvatarDefinition());
            var d = AvatarInstanceTests.Definition(); d.Renderer = " "; Reject(d);
            d = AvatarInstanceTests.Definition(); d.Groups = null; Reject(d);
            d = AvatarInstanceTests.Definition(); d.BaseValues = null; Reject(d);
            d = AvatarInstanceTests.Definition(); d.Groups["emotion"].Default = "missing"; Reject(d);
            d = AvatarInstanceTests.Definition(); d.Groups["emotion"].Presets = null; Reject(d);
            d = AvatarInstanceTests.Definition(); d.Groups["emotion"] = null; Reject(d);
            d = AvatarInstanceTests.Definition(); d.Groups["emotion"].Presets["neutral"] = null; Reject(d);
            d = AvatarInstanceTests.Definition(); d.Groups["emotion"].Presets["neutral"].Values = null; Reject(d);
            d = AvatarInstanceTests.Definition(); d.BaseValues["bad"] = default; Reject(d);
            d = AvatarInstanceTests.Definition(); d.Groups[" "] = d.Groups["emotion"]; Reject(d);
            d = AvatarInstanceTests.Definition(); d.Groups["emotion"].Presets[" "] = new(); Reject(d);
            d = AvatarInstanceTests.Definition(); d.BaseValues[""] = JsonSerializer.SerializeToElement(1); Reject(d);
            var nullRejected = false;
            try { _ = new AvatarInstance(null); } catch (ArgumentNullException) { nullRejected = true; }
            ModAssert.True(nullRejected);
        }

        [ModTest("avatar/catalog_unknown_id_is_diagnosed")]
        public static void Missing(ModTestContext ctx)
        {
            var rejected = false;
            try { _ = AvatarCatalog.Create("avatar_tests/nonexistent"); } catch (KeyNotFoundException) { rejected = true; }
            ModAssert.True(rejected);
        }

        private static void Reject(AvatarDefinition definition)
        {
            var rejected = false;
            try { definition.Validate(); } catch (ArgumentException) { rejected = true; }
            ModAssert.True(rejected, "malformed definition is rejected");
        }
    }
}
