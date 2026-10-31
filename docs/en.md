# Avatar Engine foundation

`avatar_base` owns renderer-independent avatar definitions and preset state. It has no
runtime dependency on dialogs, party characters, UI engine mods or Live2D. A visual novel,
a character preview or another UI can use the same logical instance.

This first version implements resource loading, validated preset selections, deterministic
composition and serializable state. `IAvatarRenderer` defines the presentation boundary;
The `avatar_layers` mod implements the `layers` renderer and is an optional dependency of content
that uses it. Live2D adaptation, scenario actions, a visual editor and automatic save-system
integration are not implemented yet. Existing `first=` / `second=` dialog portraits are unaffected.

## Resource format

Content mods declare `depends: ["avatar_base>=1"]` (or `avatar_layers>=1` for layered images) and supply normal entity files, for example
`avatars/marao.json`. `Kind: avatar` is registered by `AvatarDefinition`. Definitions use the
standard entity resolver: folder/ZIP providers, overrides, `Extends` and patches work through
that pipeline; the catalog does not parse files or maintain a second resource cache.

```json
{
  "Kind": "avatar",
  "Items": [{
    "Id": "my_game/marao",
    "Renderer": "layers",
    "BaseValues": {
      "layers/body": "marao/body.png",
      "layers/eyes": "marao/eyes/opened.png"
    },
    "Groups": {
      "emotion": {
        "Default": "neutral",
        "Priority": 0,
        "Presets": {
          "neutral": {},
          "angry": {"Values": {"layers/eyes": "marao/eyes/narrowed.png"}}
        }
      },
      "eyes_state": {
        "Default": "auto",
        "Priority": 100,
        "Presets": {
          "auto": {},
          "opened": {"Values": {"layers/eyes": "marao/eyes/opened.png"}},
          "closed": {"Values": {"layers/eyes": "marao/eyes/closed.png"}}
        }
      }
    }
  }]
}
```

Group and preset names are author-defined, case-sensitive and not a fixed engine vocabulary.
Each group has one selected preset; `Default` must name an existing preset. Definitions with
no groups are valid static avatars. `auto` is an ordinary empty preset here, not a reserved word:
it contributes nothing, leaving the baseline and earlier groups in control.

`Values` is a dictionary of opaque JSON targets. The layers adapter reads PNG paths;
a Live2D adapter can read numeric values such as `parameters/ParamEyeLOpen: 1`. The base does
not invent a universal numeric range, interpret binding names or load assets. Binding value
types, required bindings, compatibility of poses/layers and asset existence are the adapter's
responsibility. Values can also be objects or arrays for an adapter's structured configuration.

## Applying presets

```csharp
IAvatar avatar = AvatarCatalog.Create("my_game/marao");
avatar.ApplyPresets(new Dictionary<string, string> {
    ["emotion"] = "angry",
    ["eyes_state"] = "opened"
});
AvatarFrame target = avatar.Resolve();
using IAvatarRenderer renderer = AvatarRenderers.Create(avatar);
// Attach renderer.View to the host. After later changes:
renderer.Apply(target, transitionSeconds: 0.15);
AvatarState saved = avatar.CaptureState();
IAvatar restored = AvatarCatalog.Create(saved.AvatarId, saved);
```

`ApplyPresets` is atomic: every supplied group/preset is checked before changing any selection.
Unmentioned groups retain their choices. Null, misspellings and unknown presets throw
`ArgumentException`; there is no silent fallback. `AvatarCatalog.Create` throws
`KeyNotFoundException` for an unknown avatar. Select the group's default explicitly to reset it.

Every `Resolve` starts from `BaseValues` and applies all selected presets by ascending group
`Priority`, then ordinal group name. Later contributions replace earlier values for the same
binding. The order of scenario commands and JSON properties never determines precedence.
An individual JSON binding is replaced whole, not deep merged; null is a literal target value.
Thus explicit `eyes_state` at priority 100 overrides the eye contribution of `emotion` at 0.
Selecting `auto` delegates control back to the emotion/baseline on the next composition.

The output is a complete immutable snapshot, not a delta. Leaving a preset removes its
contributions; adapters must reset bindings absent from a later target rather than retaining
stale values. Define adapter-required reset targets in `BaseValues`. There is no automatic
cross-group compatibility solver in this foundation.

## Ownership and persistence

An `AvatarInstance` validates and snapshots its definition. It never changes cached entity DTOs.
Two instances of the same definition have independent selections. Frames and captured states
are detached; editing the returned save DTO cannot alter a live instance. Active instances keep
their definition snapshot after resource changes; the owning host recreates them on reload.

`AvatarState` stores the avatar ID and selected preset names. JSON round-trip it in the owning
subsystem's save data. Missing groups on restore use defaults; a mismatched avatar ID or unknown
selection is rejected. Content migrations must explicitly handle removed/renamed presets.
There is no global avatar roster or automatic registration with the game's save system.

`IAvatarRenderer` exposes a renderer-owned `Control` and accepts complete targets with an optional
transition duration. Rendering mods implement it and decide how presets become images or model
parameters. They must validate compatible targets and finite nonnegative durations. Zero snaps;
a discrete adapter may also snap for positive duration. A new animated target interrupts the
previous transition from the currently displayed state. The host attaches `View` to its UI and
disposes the adapter before freeing that container. The adapter frees owned nodes/animations,
never shared cache textures. All visual operations run on the Godot main thread.

The base owns no Godot objects or update loop. Pure logical instances do not need disposal.
`AvatarRenderers` discovers public, parameterless `IAvatarRendererFactory` implementations via
`ModTypeConsumer`, including normal unload notifications. Factories declare a unique case-sensitive
renderer ID and create an owned adapter. Duplicate IDs are rejected. `AvatarRenderers.Create(avatar)`
resolves the backend and applies the initial state, disposing the adapter if initialization fails.
The host explicitly applies subsequent `avatar.Resolve()` targets; there is no hidden subscription.
The host must dispose active adapters before their mod/scene unloads. Missing backends throw
`KeyNotFoundException`, making a missing content dependency visible.

## Verification

Tests use the engine's runtime mod compiler, not a separate project referencing mod sources:

```text
godot --headless --path . -- --frontend --with-mods=avatar_base --run-tests --mod=avatar_base
godot --headless --path . -- --frontend --with-mods=avatar_base --itest=avatar_contracts --run-tests --mod=avatar_base__contracts
```

In this workspace run these through the shared Windows job bridge. The opt-in `contracts` test
suite supplies a sample `Kind: avatar` resource and checks the real catalog and independent
instances. Production mods contain no sample character or fake renderer implementation.
