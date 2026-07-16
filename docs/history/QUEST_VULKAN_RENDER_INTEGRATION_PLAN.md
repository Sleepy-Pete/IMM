# Quest on-device: GLES ships; Vulkan draw crash root-caused (validation layer), own-queue architecture in progress

## STATUS 2026-07-16 (evening): VULKAN RENDERS CONTINUOUSLY ON QUEST - offscreen composite architecture working

The full architecture landed and runs sustained (2+ minutes, 72 fps, serial 9500+ IMM frames,
~38 draw calls/eye, zero crashes):

1. **IMM renders each eye into its own offscreen RenderTexture** on IMM's **dedicated second
   Vulkan graphics queue** (never touching Unity's queue or command buffers mid-frame), fully
   fence-completed inside the plugin-event callback.
2. **Unity composites the RT itself** (`CommandBuffer.Blit` with `Resources/ImmVulkanComposite`
   material, Unlit/Transparent fileID 10750) inside its own camera pass - ordering vs clears is
   Unity's problem again, as it should be.
3. **THE final event bug: `CameraEvent.AfterSkybox` command buffers stop being executed by
   Unity 6 XR on Quest** ~2 frames after the session reaches FOCUSED (PreCull kept issuing at
   144 Hz with the buffer attached - dispatch simply stopped; app healthy at 72 fps). This is
   what made every earlier run "stop after 3-4 frames". Fix: attach at
   **`CameraEvent.AfterImageEffectsOpaque`** - the hook the GLES path always used.
4. Diagnostics added along the way: `[IMM_PRECULL]` gate dump, `[IMM_HEARTBEAT]` main-thread
   pulse, `Unity Vulkan frame begin/stage` serials, `IMM_UNITY_VK_BLUE_CANARY` (device-flag-file
   toggled solid-blue camera clear), C# device flag file
   (`.../files/imm_debug_flags.txt`), `IMM_UNITY_VK_NO_RENDER_EVENTS`, `IMM_UNITY_VK_NO_OFFSCREEN_TARGET`.

**Known polish TODOs (visual correctness):** possible Y-flip of the composited image; no depth
buffer in the offscreen path yet (stroke sorting artifacts) - add IMM-owned cached depth image;
alpha semantics of the composite (IMM clear alpha vs Unlit/Transparent); MSAA; perf (per-draw
fence waits -> batch one command buffer per frame); cross-queue sync is currently correct only
because submits fence-complete inside the callback.

---

## STATUS 2026-07-16 (afternoon) - earlier context

The SIGSEGV in vkCmdDrawIndexed is fully root-caused with the Khronos validation layer running
ON the Quest (packaged in the APK + `adb shell setprop
debug.oculus.loadandinjectpackagedvvl.com.ImmersiveFoundation.IMMUnityTest 1`; the Android
GraphicsEnvironment injection route breaks Meta's `xrCreateVulkanInstanceKHR` - don't use it):

1. **Host-render-pass path is fundamentally broken on Quest**: at plugin-event time Unity's
   `CommandRecordingState` reports a command buffer that is NOT in the recording state
   (`VUID-vkCmd*-commandBuffer-recording` for every recorded command, plus
   `VUID-vkCmdDrawIndexed-renderpass`). Recording into it is illegal; the Adreno driver
   survives the binds and null-derefs (fault 0x4dc) on the first vkCmdDrawIndexed.
   `EnsureInsideRenderPass` cannot fix an un-begun command buffer. -> Android now defaults to
   the external-image path (`IMM_UNITY_VK_FORCE_HOST_RENDER` opts back in for experiments).
2. **The `source=display` fallback resolves to a 1x1 dummy buffer** on Quest
   (`xr-use-vulkan-offscreen-swapchain-no-main-display-buffer=1`). C# now passes the real XR
   eye texture via `XRDisplaySubsystem.GetRenderTextureForRenderPass` (per eye, MultiPass) -
   `source=xrEyePass 1680x1760` confirmed; AccessRenderBufferTexture resolves it fine, draws
   execute (14 draw calls: 13 paint + 360 picture), NO crash.
3. **Any non-passive interaction with Unity's Vulkan frame breaks XR pacing -> black view**:
   requesting queue access (AccessQueue) - and even dispatching an event whose config asks for
   EnsureInside/ModifiesCommandBuffersState - makes Unity run
   `XRDisplaySubsystem::GfxThread::AfterRendering -> GfxDeviceVK::FinalizeFrameForExternalPresent`
   MID-FRAME (validation: Unity's own frame semaphore double-signaled,
   `VUID-vkQueueSubmit-pSignalSemaphores-00067` x10; events stall after ~4 frames; headset
   shows black - user-confirmed).
4. **Adopted architecture (in progress): IMM renders on its OWN VkQueue.** Adreno 740 family 0
   has 4 queues (`adb shell cmd gpu vkjson`); `Assets/Editor/AndroidBootConfigPatcher.cs` sets
   `xr-request-additional-vulkan-graphics-queue=1` so Unity creates 2; the renderer grabs
   queue index 1 on Android ("Vulkan renderer submitting on dedicated second graphics queue" -
   VVL-clean) and ALL IMM submits go there (also fixes the pre-existing main-thread
   PrepareCamera/upload races; every record->submit->wait helper now takes a recursive
   submitMutex). Android Vulkan events are configured as PURE PASSIVE markers
   (DontCare + flags=0) and the callback only OBSERVES images
   (kUnityVulkanResourceAccess_ObserveOnly, no EnsureOutsideRenderPass, no AccessQueue).
5. **Load/init race fixed** (`ImmFeatureExamples.LoadFromStreamingAssets` now waits for
   `IsReadyForDocumentLoad`): the load used to win by ~8ms of luck; anything slowing init
   (validation layer) made it fail with no retry.

**Next milestone test** (build pending at interruption): passive-marker + own-queue run.
Expected: no finalize errors, no event stall, Unity scene stays visible. IMM content drawn
directly into the eye image will likely be INVISIBLE (Unity's later pass clears/overdraws it -
execution-order): that's expected and OK. The visibility step after that: render IMM into
per-eye offscreen RenderTextures (IMM queue) and let Unity composite them with a normal
material/CommandBuffer draw (cross-queue sync coarse at first: 1-frame-stale sampling, then
semaphore/double-buffer).

**Current device/tooling state**: VVL packaged inside the APK (Assets/Plugins/Android/
libVkLayer_khronos_validation.so, keep UNCOMMITTED, remove for shipping); logcat filter must
include `piLog:V ImmRenderReporter:V`; all `IMM_UNITY_VK_*` toggles work on-device via
`adb shell setprop debug.imm.<NAME> 1`; headset must be worn (app pauses ~12s doffed;
prox_close broadcast does NOT hold this firmware awake).

---

# (Historical) Quest on-device: GLES ships; Vulkan overlay wired and inits, draw handoff is the remaining task

**Date:** 2026-07-16
**Shipping status:** **Quest renders in VR via OpenGLES3** (commit e132fa6, user-confirmed). That
is the working, committed default.

**Vulkan status (commit 85149ff, WIP):** the Android Vulkan overlay is now wired and, on Quest,
the Vulkan external-device renderer **initializes successfully**, `sample1.imm` loads, and stereo
render events fire — but it **crashes in `vkCmdDrawIndexed`** at the first real draw. Vulkan code
is dormant unless the project selects Vulkan, so GLES is unaffected. See "Vulkan on-device result"
below.

---

## Vulkan on-device result (2026-07-16, commit 85149ff)

Switched Android gfx → Vulkan and deployed. logcat sequence:
- `Configuring IMM for Unity Vulkan external device (Quest)` ✅
- `AndroidCompleteInit ... (api=Vulkan)` → `Vulkan renderer initialized in deferred init - SUCCESS` ✅
  (the hard part — IMM's Vulkan renderer accepts Unity's external VkDevice/queue on Quest)
- `[IMM] Loaded document from memory: sample1.imm (ID: 0)` ✅
- `[IMM_UNITY_VK_RT_SRC ...] source=display pixel=1680x1760 samples=1` — C# `SetVulkanCameraRenderBuffers` ran ✅
- `[IMM_UNITY_VK_EVENTCFG ...] eventId=0/1 configured=1` ✅ — `ConfigureVulkanRenderEvent` OK both eyes
- `iOnRenderEvent event_id=0/1` ✅ — render events fire
- **SIGSEGV, fault 0x4dc**, backtrace:
  ```
  #01 vkCmdDrawIndexed            (vulkan.adreno.so)
  #03 piRendererVulkan::DrawPrimitiveIndexed
  #04 piRenderMesh::Render
  #05 LayerRendererPicture::DisplayRender      ← 360 background picture
  #06 Player::RenderStereoMultiPass
  #07 ImmEngineBridge::RenderPreparedCamera
  ```

**Root cause:** IMM's `piRendererVulkan` (built for the standalone Android VR viewer) **owns its own
command buffers, render pass, and framebuffer**. As a Unity *overlay* it must instead record its
draws into **Unity's** command buffer, obtained at the plugin event via
`IUnityGraphicsVulkan::CommandRecordingState` / `AccessQueue` (the desktop overlay path does this).
On Quest the draw reaches `vkCmdDrawIndexed` with an invalid/foreign command-buffer + render-pass
state → null deref in the Adreno driver. Also note `source=display` — for XR the render target is
the OpenXR eye swapchain image, not `Display.main`; the overlay needs Unity's active eye render
buffer (`AccessRenderBufferTexture`), not the display buffer.

**Next step (the real remaining work):** make `piRendererVulkan` record into Unity's supplied
command buffer / render pass when running as an external-device overlay, instead of its own —
specifically for stereo MultiPass. Study how the desktop `iUnityVulkanQueueRenderCallback` /
`iRenderUnityVulkanCameraInHostRenderPass` bridge IMM's draws onto Unity's command buffer, and
extend that to the Quest eye-buffer path. Until then, ship GLES3.

**Quick de-risk experiments to try first:** (1) `IMM_UNITY_VK_SKIP_HOST_RENDER=1` env to isolate
whether the crash is the host-render-pass path vs the external-image path; (2) confirm whether
`AccessRenderBufferTexture` returns a valid image for the XR eye buffer (the `source=display`
fallback suggests it may not); (3) add a null-guard on the command-recording state in the native
render callback so an invalid state skips the draw instead of crashing (turns crash → blank, safer
for iteration).

---

## 2026-07-16 (second session): crash bisected on-device — draw-state specific, validation layer staged

**Tooling unblocked (big):**
- `piLog` **does** reach logcat on Android (tag `piLog`; `ImmRenderReporter` for renderer reports).
  The earlier capture filter `Unity:V ImmUnityPlugin:V *:S` silenced every native Vulkan diagnostic.
  Correct filter: `adb logcat Unity:V ImmUnityPlugin:V piLog:V ImmRenderReporter:V DEBUG:V *:S`.
- Env-var debug toggles were dead on Android (no env reaches an app process). All `IMM_UNITY_VK_*`
  flags (native side) now also read Android system properties:
  `adb shell setprop debug.imm.<FLAG_NAME> 1` (helpers in `main.cpp` and `piVulkan_Renderer.cpp`).
- New renderer knob `IMM_UNITY_VK_HOST_BIND_ONLY` records all binds but skips the draw.

**Changes landed (dormant unless gfx=Vulkan):**
- `iRenderUnityVulkanCameraInHostRenderPass` calls `EnsureInsideRenderPass()` before
  `CommandRecordingState` (kill-switch `IMM_UNITY_VK_NO_ENSURE_INSIDE`).
- Host frames now always declare a depth attachment on the pipeline (spec-safe both ways;
  kill-switch `IMM_UNITY_VK_NO_HOST_DEPTH_ATTACHMENT`) — the Quest `source=display` fallback
  reports no depth RenderBuffer while Unity's eye pass owns depth.
- `iSubmitPictureDraw` refuses draws whose vertex/index `VkBuffer` backing is null (the paint path
  already guarded this) and one-shot-logs every handle right before the first host draw.
- HOST_RT diagnostics re-fire whenever Unity's render pass handle changes (old 24-line cap
  exhausted before the interesting XR transition).

**On-device results (Quest 3, Adreno 740, all handles logged):**
- `IMM_UNITY_VK_DEBUG_HOST_CLEAR_ONLY=1` → `vkCmdClearAttachments` into Unity's command buffer
  runs crash-free frame after frame → **the recording state / render pass instance is valid.**
- Full draw still SIGSEGVs (fault `0x4dc`, `vkCmdDrawIndexed+52`) on the **first** indexed draw,
  with cmd/pipeline/renderPass/framebuffer/vb/ib/descriptor-set all verified non-null, pipeline
  freshly created against Unity's actual render pass (subpass 0, 1 sample, depth-stencil present).
  So the null the driver dereferences is inside draw-only state — not the pass, not the handles.

**Next step (staged, needs headset worn):** the Khronos validation layer 1.4.350.1 is installed
on-device (`code_cache/libVkLayer_khronos_validation.so` + `enable_gpu_debug_layers` settings for
this package). One run that reaches the draw will emit the exact VUID. Blocked at session end:
headset doffed — validation slows startup past the ~12 s proximity pause window and the
`com.oculus.vrpowermanager.prox_close` broadcast didn't hold the device awake.
To disable the layer afterwards: `adb shell settings put global enable_gpu_debug_layers 0`.

---

## What works on-device (verified via adb logcat, 2026-07-16)

- Headless batchmode build succeeds: `Unity.exe -batchmode -quit -executeMethod
  ImmPlayer.Editor.BuildAutomation.BuildAndroidDebug` → 78 MB APK. (Interactive MCP builds
  are unreliable — BuildPlayer blocks Unity's main thread and the MCP call times out; use
  batchmode.)
- `adb -s 2G0YC1ZF98028F install -r` + launch: app runs, package
  `com.ImmersiveFoundation.IMMUnityTest`.
- OpenXR: session UNKNOWN→IDLE→READY→SYNCHRONIZED→VISIBLE→**FOCUSED**, stereo swapchain
  1680×1760×2. XR presentation is healthy.
- IMM C# init OK; `sample1.imm` read from the APK (`jar:...!/assets/sample1.imm`, 5831101 bytes).
- Native render event fires: `I/ImmUnityPlugin: iOnRenderEvent called, event_id=0`.
- App pauses ~12 s after launch when the headset comes off (proximity sensor) — normal;
  keep the headset worn during tests.

## Root cause of the blank scene (exact)

logcat:
```
E/ImmUnityPlugin: Failed to initialize GLES renderer in deferred init
E/ImmUnityPlugin: Deferred init failed - rendering disabled
E/ImmUnityPlugin: LoadFromMemory blocked until Android deferred renderer init completes
E/Unity: [IMM] Failed to load document from memory: sample1.imm
```
`code/appImmUnity/src/main.cpp:1393` hardcodes `config.rendererApi = piRenderer::API::GLES`
for Android, unconditionally — but the app runs **Vulkan** (ProjectSettings Android gfx =
Vulkan, Meta-recommended for Quest). The deferred init (`AndroidCompleteInit`,
`main.cpp:264`) then tries to create a **GLES** renderer with no GLES context → fails →
rendering disabled → the memory load is blocked → blank.

## Why: the entire Unity↔Vulkan overlay is `#if defined(WINDOWS)`

The Vulkan integration was built and validated on Windows desktop (the
`experiment/unity-vulkan-overlay-from-feature` line) and never enabled for Android. All of
these blocks in `code/appImmUnity/src/main.cpp` are Windows-gated:
- Device-event Vulkan branch that grabs `IUnityGraphicsVulkan` + instance (lines ~314–329).
- `iMakeUnityVulkanEventConfig`, `iConfigureUnityVulkanEvent` (~392–436).
- `iUnityVulkanQueueRenderCallback`, render-buffer access, `iOnRenderEventAndData` (~523–898).
- Vulkan branch of `iOnRenderEvent` (~942–968).
- `ConfigureVulkanRenderEvent` export — `#else` returns 0 on Android (~1265–1278).
- Config selection that builds `piVulkanExternalDevice` from Unity's Vulkan instance and
  selects `API::Vulkan` (~1398–1420) — **the exact template Android needs**.

The Android render path only ever handled GLES.

## The plan (next session — needs headset worn + visual confirmation)

Port the Windows Vulkan overlay to Android in `main.cpp`. It's well-templated; the risk is
the Android **deferred-init** timing (renderer is created on the render thread via
`AndroidCompleteInit`, not at plugin Init) and the external-device handoff.

1. **Un-gate** the six Vulkan blocks above: `#if defined(WINDOWS)` →
   `#if defined(WINDOWS) || defined(__ANDROID__) || defined(ANDROID)`. Confirm
   `IUnityGraphicsVulkan.h` symbols compile for arm64 (they are cross-platform Unity plugin
   headers).
2. **Device event** (`iOnGraphicsDeviceEvent`, Android branch ~330): add a
   `kUnityGfxRendererVulkan` case mirroring Windows ~314–329 (fetch `IUnityGraphicsVulkan`,
   `Instance()`, `ConfigureEvent`s, set `mDevice`).
3. **Config** (`main.cpp:1393`): when `UnityAPI.mRenderer == kUnityGfxRendererVulkan`, build
   `piVulkanExternalDevice` from `mVulkanInstance` and set `rendererApi = Vulkan` +
   `graphicsDevice = &unityVulkanDevice` (mirror Windows ~1398–1420) while keeping
   `initializeRendererOnInit = false` (Android defers to the render thread).
4. **Deferred init** (`AndroidCompleteInit`, ~264): ensure `CompleteGraphicsInitialization()`
   creates the **Vulkan** renderer with the stored external device (verify
   `ImmEngineBridge::CreateRenderer` / `CompleteGraphicsInitialization` in
   `code/appImmShared/src/imm_engine_bridge.cpp` pass `mConfig.graphicsDevice` to
   `piRenderer::Create(API::Vulkan)` on the deferred path). The bridge Vulkan reverse-Z /
   projection config already exists (`imm_engine_bridge.cpp` ~586–601).
5. Rebuild `.so` (fast, incremental — see below), redeploy, launch **with headset on**, read
   logcat. **Objective success signals** (no visual needed): `Vulkan renderer initialized
   with external device`, non-zero draw calls, no validation/`Could not load` errors. Then a
   headset glance confirms the forest.

### Fallback to evaluate if the Vulkan port stalls
Switch Android gfx API → OpenGLES3 (ProjectSettings Android `m_APIs: 15000000` → `0b000000`)
and retry: the GLES path is already wired on Android and our new load fixes (below) may make
it render where it was previously black. **Risk:** Unity 6 OpenXR on Quest may be Vulkan-only;
logcat will show immediately if the XR session refuses GLES.

## Fixes already landed this session (committed + pushed, tip 64e9d2b)

Two bugs that broke **all** memory-backed loads (the only load path on Android — StreamingAssets
live inside the APK — and Unity's path on every platform):
1. **Use-after-free** in `LoadFromMemory`: aliased the managed buffer in a stack-local array,
   queued its address into the async loader → crash in `memcpy` (two Unity 6 editor crashes,
   symbolized). Now heap-copies; the document owns/frees it.
2. **Stream cursor**: `piIStreamArray` uses the array length as its read cursor; filling with
   `SetLength(size)` parked it at EOF → importer read 0 bytes → silent load failure. Copy now
   leaves length 0; `ImportFromMemory` rewinds defensively.
Validated in the Unity 6 desktop editor (D3D11): sample1.imm imports (42 ms), renders (Game
View screenshot). Also strip of the temporary U6DIAG diagnostic; Windows Standalone gfx pinned
to D3D11 (Android stays Vulkan).

## Rebuild reference (both plugins, from merged source)

- **Windows DLL:** `msbuild A:\Github\IMM2\IMM\code\projects\windows\imm.sln
  -t:"libImmExporter;appImmUnity" -p:Configuration=Release -p:Platform=x64` (post-build
  auto-copies to the package unless Unity holds the DLL lock — fully quit Unity to deploy).
- **Android arm64 Vulkan .so:** CMake-direct + NDK 26.1.10909125 + SDK cmake 3.22.1/Ninja,
  build order libImmCore → libImmImporter → libImmPlayer → `code/appImmUnity/Projects/Android/app`;
  `cp libpng16.a libpng16d.a` first; 16 KB page-size linker flags; deploy `.so` to
  `Packages/com.immersive-foundation.imm-unity/Plugins/Android/libs/arm64-v8a/`.
- **APK:** `Unity.exe -batchmode -quit -projectPath <proj> -executeMethod
  ImmPlayer.Editor.BuildAutomation.BuildAndroidDebug -logFile <log>` (Unity must be closed).
- **Deploy/test:** `adb -s 2G0YC1ZF98028F install -r Builds/Android/IMMUnityTest.apk`;
  launch via monkey; `adb -s 2G0YC1ZF98028F logcat Unity:V ImmUnityPlugin:V *:S`. Note a
  phantom `emulator-5562 offline` entry appears intermittently — always target the Quest by
  serial `2G0YC1ZF98028F`.
