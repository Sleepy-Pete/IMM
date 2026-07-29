# Changelog

All notable changes to `com.immersive-foundation.imm-unity`.

Consumption and release instructions: `PACKAGING.md` at the repo root.

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
