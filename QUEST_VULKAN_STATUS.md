# Quest Vulkan Status — Wins and Next Work

_Updated 2026-07-28. Branch `vr-main`, verified in-headset on Quest (Adreno 740) with
`TheArtofChange.imm` (103 MB streaming document), `TheQuantumRace.imm` (221 MB) and
the sample forest scene._

## ⏭ Start here next session (2026-07-28 end of day)

Everything below is pushed (`origin/vr-main`). Both apps are force-stopped on the
headset; the device is ready to launch either one.

**The one open defect: two stroke layers missing near the end of QuantumRace.**
Ruled out already, with telemetry rather than argument — don't re-chase these:
size culling (zero drops), layer and chunk frustum culling (zero drops),
`RENDER_BUDGET` (compiled out), far-plane clipping (fixed at 20000), and per-eye
submission (counts symmetric, `pic=2 p360=1` in both eyes). So the layers never
enter the draw list at all.

**Two instruments are loaded and waiting — neither needs new code:**

1. **`[IMM_NODRAW]` telemetry**, live in the installed build. Reach the moment in
   our player and the log names the layer *and the reason*: not visible, not
   potentially visible (timeline window), opacity 0, layer not loaded, or drawing
   not loaded. No controller press needed.
2. **The reference-player A/B is already set up.** `TheQuantumRace.imm` is pushed
   to `/sdcard/Android/data/org.linuxfoundation.imm.player/files/IMM/` and loads
   cleanly there (79 chapters). Watch the same moment in the GLES3 native player:
   strokes present there ⇒ the loss is in our Unity/Vulkan integration; strokes
   missing there too ⇒ shared player/importer code or the document. That prebuilt
   APK predates our telemetry, so this pass is visual; building `appImmViewer`
   from source with `[IMM_NODRAW]` compiled in would make it mechanical.

**Also open (lower priority):** whether the 1.5-frame viewpoint prediction adds
overshoot on top of correct authored motion — test by feel with
`IMM_UNITY_NO_VIEWPOINT_PREDICT`, not by adding machinery. And the stutter fix
itself belongs in Quill: `Maincam/SecondaryAnim` (36/59 stepped) is the
highest-leverage layer to re-key.

**Device state:** `2111_confirmstate` build installed and stopped, flag file =
`IMM_UNITY_DOC_FILE=TheQuantumRace.imm` + `IMM_UNITY_VK_ENABLE_BURST=1`.
WiFi adb `192.168.1.220:5555` is the reliable channel (USB drops mid-session);
re-arm with `adb -s <usb-serial> tcpip 5555` from any USB window.

## Where things stand

The Unity + Vulkan on-device path now plays a full streaming IMM document at 72 fps
stereo with correct stereo separation, real depth ordering, live 6-DoF tracking, the
authored 9.024× viewer scale and spawn viewpoints, floor-anchored tracking origin,
alpha compositing over Unity scene content, and a world that stays planted in space
during head motion. All of the above is user-verified in-headset. The renderer started
this arc at ~9 fps with an upside-down, head-locked, single-eye-glitching image.

## Verified wins

Each entry: symptom → root cause → fix (commit).

### Performance
- **~9 fps → 72 fps stereo.** Per-draw `record→submit→vkWaitForFences` (~40 blocking
  round-trips per eye) → eye-frame command-buffer batching with per-draw descriptor
  sets, in-pass clears (CLEAR render-pass variant, no barrier), the shader-read
  transition riding the batch submit, and pipelined end-of-eye submits (fence wait
  deferred to ring-slot reuse). (`41b6be1`, `b6c5995`, `012f635`)
- **Streaming uploads no longer drain the pipeline.** Every fenced upload/clear/
  transition helper used to wait out ALL in-flight eye submissions. Per-texture
  last-use stamps + per-slot acquisition stamps now let helpers wait only the slots
  that can reference their texture — freshly created textures (every streamed chunk)
  wait nothing. (`7ae596a`)

### Correctness
- **Upside-down / behind-you / flat image.** Stale composite V-flip calibration over
  an already display-oriented render + reversed-Z mismatch → texture-convention
  projection parity by design + external reverse-Z (GREATER, clear 0.0). (`b6c5995`)
- **Frame-over-frame "feedback" trails.** The eager batch-open landed before the
  external-frame state assignments, so the clear-eligibility check compared the
  previous eye's target and the clear never ran. One-line ordering fix, found via the
  BATCHTRACE instrument in a single headless run. (`012f635`)
- **Left eye intermittently rendering from the wrong position.** Display-state
  uniforms uploaded into the one shared persistent buffer while the previous eye's
  GPU work was still reading it (exposed by pipelined submits) → resource separation:
  dedicated utility command buffer + fence, batch ring(3) with per-slot descriptor
  pools and transient-uniform partitions, eager batch open for uniform isolation.
  (`012f635`)
- **Chapter-skip destroy races and stalls.** Outgoing chapter resources were
  destroyed while pipelined eyes still referenced them; the first guard (wait at
  destroy) stalled streaming frames → deferred destruction queue keyed on ring
  acquisition stamps. (`012f635`)

### Comfort — the big one
- **"World locked to my head" / intro logo chasing head position.** The triple-
  buffered eye RTs made Unity's composite sample the eye image rendered one frame
  earlier, so every composited IMM pixel carried a one-frame-old pose that timewarp
  then dragged against head motion. Single-buffer same-frame reads cure it completely
  (user: "very stable, no more fighting positioning") — currently enabled by the
  `IMM_UNITY_VK_NO_DOUBLE_BUFFER` device flag; making it the code default requires
  the semaphore bridge below. Also cleaned up the heavy-scene look-around feel.
  Ruled out along the way: frame rate (72 fps, zero stale frames during the symptom),
  tracking (live pose transitions), Floor origin (kill-switch A/B), compositor depth
  submission (inert — see PTW note).
- **Not-grounded feel.** Unity path never set a tracking origin; now requests
  `TrackingOriginModeFlags.Floor` (native-viewer LOCAL_FLOOR parity). The spawn
  anchor-solve is origin-agnostic for eye-level anchors and becomes *correct* for
  floor-type anchors (Device origin reported height ≈ 0). (`9af9852`)

### Authoring fidelity & UX
- **Authored 9.024× viewer scale + spawn viewpoints** applied at load, chapter snaps,
  and recenter; anchor-solve places the head exactly on the authored anchor at scale.
  Controls: sticks fly, L-click recenter-to-spawn, grips grab/scale, A/B pause/restart,
  X/Y chapters. (`a5592fe`)
- **Play carries the doc to its first stop.** The document used to park on the title
  card until a manual chapter press; it now auto-advances (same path as the Y button)
  once chapters and the initial spawn are ready. (`9af9852`)
- **Unity content composites through empty IMM pixels** (white-cube test passes) with
  the alpha composite (`IMM_UNITY_VK_NO_OPAQUE_COMPOSITE`; shipping default TBD —
  opaque saves a dst read but only suits docs with full 360 coverage).

## Measured facts driving the next work

- **IMM_PERF record split** (heavy scene, ~40 draws/eye): `drawRec≈0.2ms`,
  `descAlloc≈0.1ms`, `gap≈4.2–5.4ms` — 95% of eye record time is player-side
  per-draw work *between* record calls (~115 µs/draw), duplicated per eye. The
  deepest scenes (65–74 draws) overrun the 13.9 ms frame budget from this alone.
- **Queue architecture:** on Android all IMM work deliberately submits on a dedicated
  second graphics queue (touching Unity's queue mid-frame triggers
  `FinalizeFrameForExternalPresent` → black view). Cross-queue ordering to Unity's
  composite is currently CPU-timing-safe, not GPU-ordered.
- **Compositor PTW:** VrApi runs `Type=PTWFromStereo` (depth estimated from the
  stereo pair). App depth submission is enabled (`Depth16Bit`) but inert because the
  camera renders into our offscreen RT — the XR swapchain depth is never written.
  Becomes real when host-depth interleave lands.
- **Log tags:** C# = `Unity`; renderer = `ImmRenderReporter`; bridge =
  `ImmUnityPlugin`; player = `piLog` (~180 lines/s — wraps the ring in under a
  minute; always stream, never rely on post-hoc dumps). VrApi 1 Hz lines are the OVR
  metrics feed.

## Completed since first writing (same day, 2026-07-28 afternoon)

- **Semaphore bridge shipped** (`0dae826`): the composite blit is GPU-ordered against
  the second-queue eye render via a wait-only submission on Unity's queue;
  single-buffer same-frame eye RTs are the code default (`94515a2`), triple-buffer
  is opt-in for A/B. User-verified planted world on pure defaults.
- **MSAA 4×** (`284508a`): transient tile-memory attachments, in-pass resolve into
  Unity's 1× image, stroke A2C via the player's blend state. User: "looks great";
  GPU peaks 68%, fps profile unchanged.
- **Native FFR** (`fa0a54a`): FFR-2 fragment-density map riding the MSAA pass.
  Quality visually unchanged (the correct FFR outcome); GPU no worse (65% peak).
- **Auto first-stop** at load verified working; A-button play-from-title variant
  parked (its `GetCurrentChapter()==0` title detection doesn't match the native
  chapter model after a skip-back — fix with chapter-state logging).
- **Gap verdict:** upl/lock/api brackets prove the renderer costs 0.30 ms of the
  ~4.5 ms per-eye gap; the rest is the player's own loop → simpleperf next.
- **Gap KILLED (`519f1a4`):** simpleperf named the cost — `iUnloadNotInTimeline`'s
  full-tree scan per camera per frame plus per-call getenv checks. Throttling the
  scan to ~2×/s and caching the flags took the measured gap from 4.2-4.7 ms to
  **0.19 ms per eye** at matched draw counts (headless A/B via the new
  `IMM_UNITY_START_CHAPTER=N` test hook, `1db66b6`). Total IMM render-thread cost
  is now ~0.5 ms/eye — the heavy-scene fps sags were this, twice a frame.

## Completed in the finish-line pass (2026-07-28 evening)

- **Color-space audit closed, no change:** Linear end-to-end (Unity project → C# →
  bridge → player), matching the native viewer's Vulkan contract; sRGB storage with
  linear math is the correct round trip.
- **Play-at-a-stop** (`134c1bb`): A reads the doc's own `PlaybackState`; `Waiting`
  → `Continue()` (the Quill verb, every stop including the title card).
- **Host-depth interleave complete** (`191c6b9` supply + `a5be810` prime): Unity's
  XR depth reaches the plugin, and a fullscreen depth-only prime draw lays it into
  the transient 4x depth at batch open — strokes depth-test against Unity geometry
  with MSAA and FFR intact. Smoked on device with all fingerprints simultaneously.
  Opt-in `IMM_UNITY_VK_HOST_DEPTH` until the one-press occlusion eyeball; kill
  `IMM_UNITY_VK_NO_DEPTH_PRIME` falls back to 1x attach.
- **Ship hygiene** (`9cf9ca8` + native pass): alpha composite is the code default
  (opaque opt-in for full-360 docs), the stray 26 MB validation layer no longer
  ships in the APK, frame-begin/pose logging sampled, Android graphics API verified
  Vulkan-only, and the device debug flag file is EMPTY — every verified behavior is
  the code default.

## TheQuantumRace.imm — second document, state as of 2026-07-28 night

The user's second document (221 MB, authored scale 0.839x — smaller-than-life, the
opposite regime from Art of Change's 9x) is selectable via the new
`IMM_UNITY_DOC_FILE=<name>.imm` flag-file override (`c51c997`).

- **First contact found and killed the renderer's last legacy draw path**: the
  fullscreen picture quad (360 backdrops) re-began the eye pass with no clear
  values — fatal on the always-CLEAR MSAA target (driver null-deref in
  `vkCmdBeginRenderPass`). Fixed by batching the quad like every other picture
  draw (`c51c997`); headlessly verified — QR loads (~62 s decode), plays, chapters
  advance, 90 s+ past the old crash point. Art of Change regression-checked clean
  on the same build. **The batch architecture now covers every draw path.**
- **In-headset session did not complete**: after several rapid doc-switch
  launch/stop cycles, the OS degraded — launcher intents parked on
  `ClearActivity`, one app start froze pre-boot (zero log output, `App=0.00ms`)
  under system memory pressure (cached-process sweeps, a VrShell service crash),
  and the headset needed a manual power-cycle. No evidence of an app bug beyond
  the (fixed) quad crash; the freeze pattern points at OS state worn down by the
  day's ~30 launch cycles plus three 221 MB loads.

**Next session opener:** fresh-boot headset → single clean Quantum Race launch
(`IMM_UNITY_DOC_FILE=TheQuantumRace.imm`, direct `am start` — launcher-intent
wedges bypassed) → the in-headset pass that tonight's freeze pre-empted.

## Animated viewpoint driver (Quill parity) — 2026-07-28, headless-verified

QuantumRace's viewer spot is a first-class animated camera: ONE spawn area whose
layer transform (position, orientation, scale) is keyframed through the whole
piece. The Unity side previously sampled it once per chapter and flattened
rotation to yaw; now it follows it every frame, mirroring appImmViewer's
GlobalWork loop (`viewer.cpp:286-309`).

- **Native bridge** (`appImmUnity/src/main.cpp`): exported
  `GetSpawnAreaNeedsUpdate`/`SetSpawnAreaNeedsUpdate` (the MakeDefault-keyframe
  jump signal, previously unsurfaced) and added `GetSpawnAreaPose` — a
  pose-only, allocation-free query safe at 72 Hz (live evaluated transform +
  animated/floor/locomotion flags). Also fixed a per-call `piws2str` malloc
  leak in `GetSpawnAreaInfo` (static buffer; the C# marshaller copies during
  the call).
- **Driver** (`ImmFeatureExamples`): `[DefaultExecutionOrder(-100)]`; Update
  lays down `authoredPose ∘ savedOffset` (scale-solved), LateUpdate re-captures
  the offset in spawn space — fly input, snap turns and recenters are absorbed
  automatically, and the head is NEVER re-compensated per frame (look/lean
  stays free; only one-shot re-anchors solve against the head). Needs-update →
  hard re-anchor on `GetInitialSpawnAreaId` (the authored jump target; the old
  chapter-sync path re-applied the stale ACTIVE id). Follows while Playing OR
  Waiting; paused = hold. Static spawn areas: zero per-frame writes (AoC
  unchanged).
- **Full authored orientation is the code default** (pitch/roll included —
  QR's camera banks); `IMM_UNITY_VIEWPOINT_YAW_ONLY` is the comfort A/B, kill
  `IMM_UNITY_NO_ANIMATED_VIEWPOINT` restores pre-driver behavior exactly.
  Fingerprints: `[IMM_VIEWPOINT] ... ARMED` at boot, `ACTIVE: following spawn N`
  on first follow, sampled `anim` line ~2 s.
- **Headless verify on QR** (1716 animvp build): driver follows the authored
  camera through 100s-of-meters travel and a **1000x live scale range**
  (0.012 → 13.5) at 72 fps where content allows; rig tracks the anchor within
  head-solve tolerance; memory FLAT (~1485 MB free) over the soak = leak fix +
  72 Hz polling clean; zero crashes/exceptions.
- **Auto-first-stop deadline fix**: chapters appear only after the async decode
  (~55 s for QR's 221 MB); the old 20 s deadline expired mid-decode and every
  headless QR run parked on the title. Now 120 s.
- **Android native build repaired** (was latently broken; any fresh CMake
  configure failed): guarded the IMPORTED-target declarations in
  libImmImporter/libImmPlayer CMakeLists (`if(NOT TARGET ...)` — superbuild vs
  standalone), scoped `-std=c++17`/`-frtti` to CXX (vorbis C sources
  hard-error), added `appImmShared/imm_engine_bridge.cpp` to the plugin
  sources, fixed stale `ImmCore/ImmImporter/ImmPlayer` link names to the real
  `libImm*` targets. The committed android-build ninja graph had been masking
  all of this since the target rename.
- **QR fps note**: heavy travel segments sag (34/24/28 fps, Stale 40-58,
  headless soak) with App CPU only 3-8 ms — the known deep-scene content cost,
  pre-existing and unrelated to the driver (title scene holds 72/72 with the
  driver following). Extreme-scale segments (0.012-0.075) are fill-rate
  suspects for the quality roadmap (eyeBufferScale knob).

**Donned verdict (same day): "viewer position was doing well this round" — the
driver works in-headset.** Three follow-ups from that ride:

1. **Load freeze — ROOT-CAUSED AND FIXED (`2a9a1e4`, device-verified)**: the
   SPU stage (opus decode of every sound layer, 40-60 s for QR's 221 MB) ran
   synchronously inside the loading state machine, which Unity drives under
   `Player::GlobalWork`'s mutex on the MAIN thread — zero frames submitted
   for the whole decode (LOADBEAT: no beats; VrApi `App=70s, FPS=1/72`).
   Moved onto the existing detached loader thread, sequential after the CPU
   stage. Verified: 72/72 fps, Stale=0 through the full 53 s decode, then
   GPU-stage → 79 chapters → auto-advance → anchor → driver ACTIVE, all
   within 200 ms. A loading indicator is now buildable (the app renders
   during load). Follow-on fix in the same commit: the initial spawn anchor
   waited 120 FRAMES for spawn areas (which exist only at LoadingComplete) —
   now a realtime deadline, or the viewpoint driver never engaged.
   Headless-workflow lesson: the device sleeps ~90 s after a doffed launch
   and freezes everything mid-load — arm the prox_close keep-alive loop for
   headless runs.
2. **Strokes vanishing at distance / "render distance" (root-caused + fixed,
   eyeball pending)**: scene camera was `near=0.3, far=1000` while QR's
   authored travel passes 1100 m — everything beyond 1 km clipped. Set to the
   native viewer contract `0.01/20000`.
3. **Left-eye glitches at travel speed**: streaming-churn race class under
   QR's fast authored travel — needs a BATCHTRACE-style evidence session, no
   blind fix.

Device: `1725_farplane_loadbeat` build installed+stopped (all of the above).

## Picture path repaired (`c3b26b2`) + pose prediction (`b87c608`) — 2026-07-28 late

Pete's first full QR ride found the skybox scenes collapsing to 23 fps in BOTH
eyes (Stale 40-58, GPU and CPU idle — TimeWarp dragging stale frames read as
"tracking broken") and the title PNG "glued to my eyes". Four defects, one path:

- **Pipeline thrash**: the picture pipeline kept ONE cached variant keyed on
  render pass; the batched eye targets cycle SIX passes (2 eyes x 3 ring
  slots), so `vkCreateGraphicsPipelines` ran on every picture draw (~7 ms;
  the api bracket measured 14-24 ms/eye). Now on the paint path's variant
  cache — worst api after the fix: **0.52 ms**, 129 skybox-seconds at 72 fps.
- **2D quad in NDC**: the Vulkan 2D picture VS emitted raw NDC positions —
  every 2D picture rendered fullscreen and head-locked. Now world-placed
  (aspect-scaled quad through `mLayerToViewer` + eye projection, GLES parity).
- **Wrong eye**: both picture VSes hardcoded `mEye[0]`; right eye rendered
  pictures with the left eye's projection. Now `pass.mID` like the paint VS.
- **Aspect never arrived**: `SetShaderConstant4F` is a GL-only stub on Vulkan;
  the aspect now rides the slot-9 buffer, bound as descriptor binding 9.

User verdict: *"skybox and png fixes are in and they work and there is better
tracking."* Shader generator gains an NDK-glslc fallback (no glslangValidator
needed).

**Pose prediction** (`b87c608`): the native spawn pose is evaluated a frame
behind, so the rig trailed the authored camera at travel speed. One-frame
extrapolation (slerp + scale-rate, teleport guard; capture mirrors the APPLIED
pose so prediction never leaks into the user offset). Kill
`IMM_UNITY_NO_VIEWPOINT_PREDICT`. Also `culled=`/`trisCulled=` counters in the
sampled render line — the evidence channel for the open missing-strokes hunt.

## Load time, culling exonerated, and the stepped-key finding — 2026-07-28 night

**Load: 53 s → 30 s.** The SPU pass decoded QuantumRace's FOUR ~22 MB opus
tracks serially on the loader thread. Each `AddSound` decodes through its own
`AMediaCodec` and the Android engine's sound array is already mutex-protected,
so they now decode on up to four workers (`ae77576`, worker cap `edc2974`) —
wall time is the longest single track. Combined with the freeze fix, the load
is now a live, tracked, 72 fps world for ~30 s instead of a minute-long
apparent hang. Non-Android platforms keep the serial path.

**Missing strokes: culling is exonerated, twice over.** The layer screen-size
cull (`f < 0.005`, tuned for room-scale documents) was widened to 0.001 with
naming telemetry, and the layer + chunk frustum culls got a kill-switch
(`IMM_UNITY_NO_FRUSTUM_CULL`) and their own telemetry. Result across full
rides with content visibly missing: **zero size culls, zero layer-frustum
culls**. Neither mechanism is dropping the content.

**The render log had been eye-blind all along.** The sampler used an even
period (60) while eyes alternate, so it always landed on the SAME eye — every
render line ever captured was eye 0, and per-eye asymmetry was structurally
invisible. Odd period + explicit `eye=` field (`67030e5`). First per-eye data:
counts are **symmetric** (`pic=2 p360=1` both eyes, paint within 1-2), so the
missing content is *not* a per-eye submission failure — the layers never enter
the draw list at all. That points at the silent early-outs, which now name
themselves (`[IMM_NODRAW]`, `883525d`): not visible, not potentially visible
(timeline window), zero opacity, layer not loaded, drawing not loaded.

**Burst capture caught the picture defect on film.** One frame, same instant:
black sky in one eye, white sky in the other; and a separate frame pure white
in both. The white had a cause — the scene camera cleared with Unity's default
**skybox**, so anywhere IMM content is absent the compositor shows Unity's
bright sky. Now clears to transparent black (`67030e5`), matching the native
viewer: missing content reads black instead of flashing white. Diagnosis
clarity, not the root cause.

**Stepped animation keys — user's diagnosis, confirmed, and deliberately NOT
"fixed".** The `[IMM_KEYS]` dump walks the spawn area's parent chain (the
spawn layer itself carries no keys — the motion lives on the groups above it):

    Maincam (spawn)   0 keys
    Cam              44 keys ->  17 stepped, 27 linear
    SecondaryAnim    59 keys ->  36 stepped, 10 linear, 13 eased
    Maincam (group) 3911 keys ->  73 stepped, 3810 linear, 28 eased

A stepped key (`InterpolationType::None`) holds its pose and snaps at the next
key, so travel flows where keys are linear and steps where they are not —
matching "fine in some points of the film, a problem in others" — and the
vehicles step for the same reason, since they are driven by the same authored
keys. Forcing interpolation (`IMM_SMOOTH_STEPPED_KEYS`) was tried and
**reverted** (`e3e2fb5`): it flattens the edit's intended holds and cuts into
slides. **The stepping is authoring, not a defect.** The telemetry stays
(`5d1a03e`). Easing is NOT flattened — Smoothstep/EaseIn/EaseOut are applied
to `t` upstream; only `None` steps.

**Open, in priority order:**

1. **Two main stroke layers missing near the film's end.** Culling ruled out;
   per-eye submission ruled out. `[IMM_NODRAW]` will name the layers and the
   reason on the next ride that reaches them.
2. **Motion still not perfect in fast/nested-layer travel.** Authored stepping
   is expected; the open question is whether the 1.5-frame viewpoint
   prediction now *adds* overshoot on top of correct authored motion. The
   honest test is `IMM_UNITY_NO_VIEWPOINT_PREDICT` (feel it with no
   viewer-side interference), not more machinery.
3. Loading indicator — now buildable, since the app renders throughout load.

## Reference players and source Quill projects (2026-07-28 night)

**Two reference implementations are installed on the test headset** — better
ground truth than any third-party app:

- **`org.linuxfoundation.imm.player`** — the native IMM player. No Unity:
  `libappImmPlayer.so` + `libvrapi.so`, ships `sample1.imm`, and links
  **`libEGL`/`libGLESv3`** (no Vulkan). Built from the same source tree we are
  porting, so it emits the **same `piLog` tags** — its log is directly
  comparable to ours, line for line. Side-loading QuantumRace into it splits
  the missing-layer question in half: if it draws the layers, the loss is in
  our Unity/Vulkan integration; if it drops them too, the loss is in shared
  player/importer logic or the document.
- **`com.facebook.arvr.quillplayer`** (Meta, 2.0.141) — visual ground truth
  for whether a layer is supposed to appear at all.

**Both reference players share a stack that differs from ours on every axis.**
Verified by reading their linked libraries:

| | Meta Quill player | IMM reference player | **Our port** |
|---|---|---|---|
| Engine | native C++ | native C++ | **Unity** |
| VR API | VrApi | VrApi | **OpenXR** |
| Graphics | **GLES3** | **GLES3** | **Vulkan** |

`libquillplayer.so` and `libDemoQuillPlayer.so` link `libGLESv3`/`libEGL`/
`libvrapi` with no Vulkan and no Unity; the IMM reference player is the same.
So neither working reference exercises the code paths we are building. That is
useful in both directions: it explains why we meet problems they never hit,
and it means a defect reproduced in the reference player is in shared
player/importer code, while one that only we see is in our integration. This
does not argue against Vulkan (see [[vulkan-quest-standards-audit]] — the
Store does not require it), but it does mean the reference players are the
fastest way to split "ours" from "shared" for any new defect.

**Reference-player A/B is set up and works.** The native player loads the
newest `.imm` from its own external files dir (`OvrApp.cpp:2237-2265`):

    adb push TheQuantumRace.imm \
      /sdcard/Android/data/org.linuxfoundation.imm.player/files/IMM/
    adb shell monkey -p org.linuxfoundation.imm.player \
      -c android.intent.category.LAUNCHER 1

It loaded QuantumRace cleanly — 79 chapters, same document, same `piLog`
tags — so the same film can now be watched in both renderers and compared
scene for scene. First numbers from that run, ours against the reference:

| | Reference (GLES3, native) | Ours (Vulkan, Unity) |
|---|---|---|
| CPU load | 4247 ms | ~700 ms (loads from memory) |
| Audio decode | 65814 ms (serial) | ~28000 ms (4 workers) |
| GPU load | 2 ms | 0 ms |

**Our port loads QuantumRace 2.3x faster than the reference implementation**
— the parallel opus decode is a genuine improvement over upstream, not just
parity. Caveat: the prebuilt reference APK predates our `[IMM_NODRAW]` /
`[IMM_KEYS]` telemetry, so the missing-layer comparison is currently visual.
Building `appImmViewer` from source with that telemetry would make the
comparison mechanical, and is the natural follow-up if the visual pass is
ambiguous.

**The Quill player is also the UI parity spec.** A user screenshot
(`com.facebook.arvr.quillplayer-20260727-210116.jpg`, Soda Island Chapter 1)
documents Meta's exact control layout for the outstanding UI backlog item:
playback bar (skip-back / previous / pause / play / volume / HD), *Return to
Browse*, *Reset View*, *Free Fly*, *Show / Hide Panel*, *Continue*, and
"Press Thumbstick to Play Next", plus a browse panel with title, studio, date
and description.

**Source Quill project inspected** (`soda-island-episode-three`,
`Masterfile_ABC_v43_O_ms_df`: `Quill.json` 50 MB + `Quill.qbin` 1.86 GB):

- `"DefaultViewpoint": "Root/Masterfile/Maincam/SecondaryAnim/Cam/Maincam"` —
  **the same camera chain QuantumRace's key dump reported**, so this rig is a
  studio template and findings transfer between documents.
- `"Framerate": 24` — documents are authored at 24 fps while we present at 72.
- Key interpolation across the whole project: **Linear 57056, None (stepped)
  8457 (~12.6%), Smoothstep 2432, EaseIn 614, EaseOut 401.** Stepped keys are
  a normal, heavy part of the authoring idiom — further support for honoring
  them rather than smoothing them.
- Layer types: Paint 1259, Group 1010, Picture 12 (2D 8, 360_Equirect_Mono 6),
  Sound 4 (Positional 3, Flat 1), Camera 1, Viewpoint 1.
- Layer schema: `Animation { Duration, Timeline, StartOffset, MaxRepeatCount,
  Keys { Visibility, Transform, Opacity } }`. **Visibility keys are always
  `Interpolation: None`** (never interpolated, matching the player). So a
  layer's presence is decided by visibility keys + timeline window + opacity —
  precisely the three silent early-outs `[IMM_NODRAW]` now names, which makes
  the telemetry directly interpretable against the authored data.

## Stutter is authored — the specific layers to re-key (2026-07-28 night)

The user built a timeline viewer and independently spotted it: around 6:56,
subgroups that follow the camera transform carry fully stepped keyframes.
`tools/quill_layer_map.ps1` corroborates and names them. **Every fully-stepped
transform layer in the project is a Group** (33 of them) — which matters,
because a group carries the transform its children ride, so one stepped group
steps everything under it.

Highest leverage first, inside the camera rig:

| Layer | Xf keys | Stepped | Note |
|---|---|---|---|
| `Maincam/SecondaryAnim` | 59 | **36** | the rig everything hangs off — dominant source |
| `Maincam/SecondaryAnim/Cam` | 44 | **17** | |
| `Maincam/SecondaryAnim/GarlicCockpit6N1Nn` | 31 | **31** | fully stepped, from 8:27 |
| `Maincam/SecondaryAnim/Racersempty1` (+`/Abstract/Gar`, `/Abstract/Rainbow`) | 3-4 | 1 each | **live 6:50** — the racers |
| `Maincam/SecondaryAnim/Foldercars` (+`/Abstract/NewGroup14`, `/NewGroup16`) | 3-6 | 1 each | **live 6:50** |

The racer groups at 6:50 match the reported moment and the "vehicles stutter"
symptom exactly. Note they each carry only ~1 stepped key of their own — they
inherit most of the stepping from `SecondaryAnim` above them, so re-keying the
parent rig is the single highest-value authoring change.

**Fix belongs in Quill, not the player.** Interpolating stepped keys at
playback was tried and reverted (`e3e2fb5`) because it flattens intended holds
and cuts. Not every stepped key is a mistake — the ones worth changing are
those sitting in the middle of continuous camera motion.

## Debug flags (device: `imm_debug_flags.txt`, one per line, `NAME` or `NAME=VALUE`)

Every entry is pushed into the native process environment at boot (`d3217e2`),
so raw-`getenv` toggles across the player and renderer libs work from this file
— before that bridge existed they were silently dead on Android.

| Flag | Effect |
|---|---|
| `IMM_UNITY_DOC_FILE=<name>.imm` | Pick any StreamingAssets document |
| `IMM_UNITY_VK_ENABLE_BURST` | Arm right-stick 8-frame stereo burst capture |
| `IMM_UNITY_START_CHAPTER=N` | Boot straight into chapter N (headless captures) |
| `IMM_UNITY_NO_ANIMATED_VIEWPOINT` | Disable the animated viewpoint driver |
| `IMM_UNITY_NO_VIEWPOINT_PREDICT` | Disable pose prediction (raw authored pose) |
| `IMM_UNITY_PREDICT_FRAMES=<f>` | Prediction lead, default 1.5 |
| `IMM_UNITY_SMOOTH_VIEWPOINT=<hz>` | Critically-damped viewpoint follow |
| `IMM_UNITY_VIEWPOINT_YAW_ONLY` | Drop authored pitch/roll (comfort A/B) |
| `IMM_UNITY_NO_FRUSTUM_CULL` | Disable layer + chunk frustum culling |
| `IMM_UNITY_NO_SIZE_CULL` | Disable screen-size layer culling |

## Next work

1. **Missing stroke layers in QuantumRace** — the one open defect. Both
   instruments are already loaded; see "Start here next session" at the top.
2. **Viewpoint prediction sanity check** — does 1.5-frame prediction add
   overshoot on top of authored motion? Feel it with
   `IMM_UNITY_NO_VIEWPOINT_PREDICT`.
3. **Loading indicator** — now buildable, since the app renders throughout the
   ~28 s load instead of freezing.
4. **UI panel/volume/browse** (Meta Quill player parity) — the remaining
   product-scale feature. The Quill player screenshot documents the target
   layout; the doc-override flag is the seed for browse.
5. Parked one-press checks: A-at-stop continues; host-depth occlusion with
   `IMM_UNITY_VK_HOST_DEPTH` (white cube vs strokes) → flip default-on if it
   passes; confirm the deepest scenes hold 72.
6. Workflow docs (WiFi-adb + stream + VrApi capture recipes) as they stabilize.

## Open bugs (parked, non-blocking)

- One-off full pose freeze (run 2 on 2026-07-28): camera rotation frozen for minutes
  while the session stayed focused at 72 fps; position showed only sensor jitter ×9.
  Not reproduced since. If it recurs: XR input subsystem wedge — instrument
  `XRInputSubsystem` state transitions.
- Cached-process reaper kills the app if launched while doffed without keep-alive
  broadcasts (`cch+N CEM` in ActivityManager). Launch worn, or arm the 3 s keep-alive
  loop.
