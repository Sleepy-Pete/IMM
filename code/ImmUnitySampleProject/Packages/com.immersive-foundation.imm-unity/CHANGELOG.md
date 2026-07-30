# Changelog

All notable changes to `com.immersive-foundation.imm-unity`.

Consumption and release instructions: `PACKAGING.md` at the repo root.

## [Unreleased]

Additive API; no breaking changes to existing entry points.

### Added
- **Host audio mixer.** `ImmDocument.GetSoundLayers()` returns every sound layer with its
  current mixer state, and `SetSoundLayerVolume/Mute/Solo/Bus` plus the static
  `ImmDocument.SetSoundBusVolume(bus, volume)` drive it. The mixer **multiplies** the
  authored volume rather than replacing it, so the document's timeline keeps animating
  underneath a fader. Mute wins over solo; while anything is soloed everything else is
  silent; solo is scoped to the document; bus faders are global to the player.
  Note that sound layers are deliberately exempt from the visibility gate, so
  `SetLayerVisible(false)` does **not** silence a layer — that is what mute is for.
- `ImmNativePlugin.PauseAllSounds()` / `ResumeAllSounds()`, called automatically by
  `ImmPlayerManager` on `OnApplicationPause` and `OnApplicationFocus`. The native engine
  owns its own audio output device, so Unity's own `AudioListener` pause never reached
  these voices — a doffed headset or a backgrounded app kept playing the piece to nobody.

### Fixed
- **Audio was completely silent in Unity builds for macOS and iOS.** The engine bridge
  selected the null sound backend on any platform that was not Windows or Android; Apple
  targets now select the AVFoundation backend, which already existed and is what the
  native macOS viewer uses.
- **Quest audio had no spatialization at all.** Every positional/listener/attenuation entry
  point in the Android backend was an empty function body, and multichannel content was
  played by dumping channels 0 and 1 to left and right. Positional layers are now placed
  with distance attenuation, directional cone/frustum, panning, interaural delay and head
  shadowing; first-order ambisonic beds are decoded and follow the head. Distance
  attenuation and the directional modifiers match the Windows Audio360 reference by
  construction. `IMM_AUDIO_NO_SPATIAL=1` restores the previous behaviour.
- macOS/iOS gained the same model as far as `AVAudioPlayer` can express it: exact distance
  attenuation and cone/frustum, plus lateral position. No interaural delay, head shadowing
  or ambisonic on that backend yet.

## [0.2.0] — 2026-07-28

Additive API; no breaking changes to existing entry points.

### Added
- `GetSpawnAreaPose(docId, spawnAreaId, out SpawnAreaPose)` — allocation-free
  spawn-area query safe to poll every frame. Returns the live evaluated
  transform (animated viewpoint layers move it continuously) plus
  animated / floor-level / locomotion flags. Use this rather than
  `GetSpawnAreaInfo` on a per-frame path.
- `GetSpawnAreaNeedsUpdate(docId)` / `SetSpawnAreaNeedsUpdate(docId, state)` —
  the timeline-driven spawn-area change signal raised by Quill `MakeDefault`
  keyframes. Consume it to re-anchor the viewer on the authored viewpoint.
- `SetRuntimeFlag(name, value)` — mirrors an entry into the native process
  environment. `ImmPlayerManager` now pushes every `imm_debug_flags.txt` entry
  through it at boot, so `getenv`-gated toggles across the player and renderer
  work on Android, where the process environment is otherwise empty.
- `ImmDocument.TryGetSpawnAreaWorldPoseAndScale(...)` — live spawn pose and
  scale converted to Unity space and composed with the document root.
- `ImmDocument.GetSpawnAreaNeedsUpdate()` / `ClearSpawnAreaNeedsUpdate()`.

### Fixed
- `GetSpawnAreaInfo` leaked a heap buffer on every call (the name string was
  allocated and never freed). Long-running or per-frame callers leaked steadily.
- Audio decode no longer blocks the host's main thread. The SPU stage ran
  synchronously inside the loading state machine, which Unity drives from
  `GlobalWork`, so a large document froze the application for the whole decode
  (~57 s for a 221 MB document, zero frames submitted). It now runs on the
  loader thread; the app renders throughout loading.
- Android audio decode runs up to four opus tracks in parallel, cutting a 221 MB
  document's load from ~53 s to ~28 s.
- Vulkan picture rendering: pipelines are cached per render-pass variant
  (previously rebuilt on every picture draw, ~7 ms each), 2D pictures are placed
  in the world instead of emitting raw NDC (they rendered fullscreen and
  head-locked), and both picture shaders select the eye via `PassState` instead
  of a hardcoded index.

### Changed
- Declared samples removed from `package.json` — the referenced `Samples~`
  folder does not exist, so Package Manager showed a sample that could not be
  imported. Example scripts live in the sample project in the repo.

## [0.1.0]

Initial package: IMM document loading and playback runtime with native plugins
for Android arm64, Windows x64, macOS and iOS.
