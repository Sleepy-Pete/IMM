# Quest Vulkan Status — Wins and Next Work

_Updated 2026-07-28. Branch `vr-main`, verified in-headset on Quest (Adreno 740) with
`TheArtofChange.imm` (103 MB streaming document) and the sample forest scene._

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

## Next work (priority order)

1. **simpleperf the player loop.** The ~4 ms/eye of player-side CPU (duplicated per
   eye) is the 72-sustained store-gate blocker. Record the render thread in a heavy
   scene, read hot symbols, fix (upload-once-per-frame / dirty tracking are the
   leading candidates).
2. **Color-space audit** (Gamma-vs-Linear decision) and **host-depth interleave**
   (activates real compositor PTW depth — depth submission is already enabled and
   waiting — plus proper Unity-object occlusion).
3. **Play-from-title guard fix** (parked by user; diagnosis captured above).
4. **UI panel/volume/browse** (Meta Quill player parity).
5. **Ship hygiene.** Strip probes/BATCHTRACE/piLog spam, remove the validation-layer
   .so, composite default decision, GLES→Vulkan manifest default after soak, push
   `vr-main`, document the WiFi-adb + stream + VrApi workflow.

## Open bugs (parked, non-blocking)

- One-off full pose freeze (run 2 on 2026-07-28): camera rotation frozen for minutes
  while the session stayed focused at 72 fps; position showed only sensor jitter ×9.
  Not reproduced since. If it recurs: XR input subsystem wedge — instrument
  `XRInputSubsystem` state transitions.
- Cached-process reaper kills the app if launched while doffed without keep-alive
  broadcasts (`cch+N CEM` in ActivityManager). Launch worn, or arm the 3 s keep-alive
  loop.
