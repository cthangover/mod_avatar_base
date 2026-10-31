using System;
using System.Collections.Generic;
using System.Text.Json;
using Cthangover.Core.Mods.Testing;

namespace Cthangover.Mods.Avatars.Tests
{
    /// <summary>Public preset contract: independent instances, atomic changes and deterministic full targets.</summary>
    public sealed class AvatarInstanceTests
    {
        [ModTest("avatar/defaults_partial_update_and_removed_contributions")]
        public static void Selections(ModTestContext ctx)
        {
            IAvatar avatar = new AvatarInstance(Definition());
            ModAssert.Equal("test/avatar", avatar.AvatarId);
            ModAssert.Equal("neutral", avatar.CaptureState().Presets["emotion"]);
            ModAssert.Equal("test", avatar.Resolve().Renderer);
            ModAssert.CloseTo(1, avatar.Resolve().Values["eye"].GetDouble());
            avatar.ApplyPresets(new Dictionary<string, string> { ["emotion"] = "angry" });
            ModAssert.CloseTo(0.3, avatar.Resolve().Values["eye"].GetDouble());
            ModAssert.Equal("red", avatar.Resolve().Values["tint"].GetString());
            avatar.ApplyPresets(new Dictionary<string, string> { ["eyes_state"] = "opened" });
            ModAssert.Equal("angry", avatar.CaptureState().Presets["emotion"], "unmentioned groups retained");
            ModAssert.CloseTo(1, avatar.Resolve().Values["eye"].GetDouble(), what: "explicit eyes override emotion");
            avatar.ApplyPresets(new Dictionary<string, string> { ["emotion"] = "neutral", ["eyes_state"] = "auto" });
            ModAssert.False(avatar.Resolve().Values.ContainsKey("tint"), "leaving a preset removes its contributions");
            ModAssert.CloseTo(1, avatar.Resolve().Values["eye"].GetDouble(), what: "baseline restored");
        }

        [ModTest("avatar/composition_is_independent_of_command_order")]
        public static void Order(ModTestContext ctx)
        {
            var a = new AvatarInstance(Definition()); var b = new AvatarInstance(Definition());
            a.ApplyPresets(new Dictionary<string, string> { ["emotion"] = "angry" });
            a.ApplyPresets(new Dictionary<string, string> { ["eyes_state"] = "opened" });
            b.ApplyPresets(new Dictionary<string, string> { ["eyes_state"] = "opened" });
            b.ApplyPresets(new Dictionary<string, string> { ["emotion"] = "angry" });
            ModAssert.CloseTo(a.Resolve().Values["eye"].GetDouble(), b.Resolve().Values["eye"].GetDouble());
            var tied = Definition(); tied.Groups["emotion"].Priority = 100;
            var tie = new AvatarInstance(tied);
            tie.ApplyPresets(new Dictionary<string, string> { ["eyes_state"] = "opened", ["emotion"] = "angry" });
            ModAssert.CloseTo(1, tie.Resolve().Values["eye"].GetDouble(), what: "ordinal group name breaks equal priorities");
        }

        [ModTest("avatar/invalid_patch_is_atomic_and_names_are_case_sensitive")]
        public static void Atomic(ModTestContext ctx)
        {
            IAvatar avatar = new AvatarInstance(Definition());
            Reject(avatar, new Dictionary<string, string> { ["emotion"] = "angry", ["eyes_state"] = "missing" });
            Reject(avatar, new Dictionary<string, string> { ["Emotion"] = "angry" });
            Reject(avatar, new Dictionary<string, string> { ["emotion"] = "Angry" });
            Reject(avatar, new Dictionary<string, string> { ["emotion"] = null });
            Reject(avatar, null);
            ModAssert.Equal("neutral", avatar.CaptureState().Presets["emotion"]);
            ModAssert.CloseTo(1, avatar.Resolve().Values["eye"].GetDouble());
        }

        [ModTest("avatar/definition_state_and_frame_do_not_alias_instances")]
        public static void Isolation(ModTestContext ctx)
        {
            var definition = Definition();
            IAvatar a = new AvatarInstance(definition); IAvatar b = new AvatarInstance(definition);
            var frame = a.Resolve(); var state = a.CaptureState();
            definition.Groups["emotion"].Presets.Clear(); definition.BaseValues.Clear();
            state.Presets["emotion"] = "missing";
            a.ApplyPresets(new Dictionary<string, string> { ["emotion"] = "angry" });
            ModAssert.Equal("neutral", b.CaptureState().Presets["emotion"]);
            ModAssert.Equal("neutral", frame.Presets["emotion"]);
            ModAssert.CloseTo(1, frame.Values["eye"].GetDouble());
            ModAssert.CloseTo(0.3, a.Resolve().Values["eye"].GetDouble());
            var readOnly = false;
            try { ((IDictionary<string, string>)frame.Presets).Add("new", "value"); }
            catch (NotSupportedException) { readOnly = true; }
            ModAssert.True(readOnly, "render target selections cannot be mutated");
        }

        [ModTest("avatar/state_json_roundtrip_and_restore_validation")]
        public static void Persistence(ModTestContext ctx)
        {
            var original = new AvatarInstance(Definition());
            original.ApplyPresets(new Dictionary<string, string> { ["emotion"] = "angry", ["eyes_state"] = "opened" });
            var saved = JsonSerializer.Deserialize<AvatarState>(JsonSerializer.Serialize(original.CaptureState()));
            var restored = new AvatarInstance(Definition(), saved);
            ModAssert.Equal(original.AvatarId, restored.AvatarId);
            ModAssert.Equal("angry", restored.CaptureState().Presets["emotion"]);
            ModAssert.Equal("opened", restored.CaptureState().Presets["eyes_state"]);
            ModAssert.CloseTo(1, restored.Resolve().Values["eye"].GetDouble());
            saved.Presets.Clear();
            ModAssert.Equal("angry", restored.CaptureState().Presets["emotion"], "restored state is copied");
            var partial = new AvatarState { AvatarId = "test/avatar", Presets = new() { ["emotion"] = "angry" } };
            ModAssert.Equal("auto", new AvatarInstance(Definition(), partial).CaptureState().Presets["eyes_state"]);
            RejectState(new AvatarState { AvatarId = "other" });
            RejectState(new AvatarState { AvatarId = "test/avatar", Presets = new() { ["emotion"] = "missing" } });
            RejectState(new AvatarState { AvatarId = "test/avatar", Presets = null });
        }

        internal static AvatarDefinition Definition() => JsonSerializer.Deserialize<AvatarDefinition>(
            "{\"Id\":\"test/avatar\",\"Renderer\":\"test\",\"BaseValues\":{\"eye\":1},\"Groups\":{" +
            "\"emotion\":{\"Default\":\"neutral\",\"Presets\":{\"neutral\":{},\"angry\":{\"Values\":{\"eye\":0.3,\"tint\":\"red\"}}}}," +
            "\"eyes_state\":{\"Default\":\"auto\",\"Priority\":100,\"Presets\":{\"auto\":{},\"opened\":{\"Values\":{\"eye\":1}}}}}}");

        private static void Reject(IAvatar avatar, IReadOnlyDictionary<string, string> changes)
        {
            var rejected = false;
            try { avatar.ApplyPresets(changes); } catch (ArgumentException) { rejected = true; }
            ModAssert.True(rejected, "invalid changes are rejected");
        }
        private static void RejectState(AvatarState state)
        {
            var rejected = false;
            try { _ = new AvatarInstance(Definition(), state); } catch (ArgumentException) { rejected = true; }
            ModAssert.True(rejected, "invalid restored state is rejected");
        }
    }
}
