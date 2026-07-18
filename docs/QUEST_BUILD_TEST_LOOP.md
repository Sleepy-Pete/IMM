# Quest build & on-device test loop (Unity plugin, Windows host)

Reproducible recipe for building the IMM Unity plugin + APK, deploying to a Quest 3, and
capturing the right logs. Everything below was validated 2026-07-16 on the Windows box; adjust
paths/serials per machine.

**Why Vulkan matters:** the Meta Quest Store requires Vulkan for new submissions (GLES is
deprecated there). GLES3 is our working fallback for development; Vulkan must reach parity
before store publishing.

## Prerequisites

| Thing | Value used here |
|---|---|
| Unity | 6000.0.58f2, project `code/ImmUnitySampleProject` |
| Android SDK | `%LOCALAPPDATA%\Android\Sdk` (adb in `platform-tools/`) |
| SDK CMake + Ninja | `Sdk\cmake\3.22.1\bin` |
| NDK | 26.1.10909125 |
| Quest 3 serial | `2G0YC1ZF98028F` (**always** `adb -s <serial>` — a phantom `emulator-5562 offline` appears) |
| Headset | must be WORN during tests (app pauses ~12 s after doffing; the `com.oculus.vrpowermanager.prox_close` broadcast does NOT hold newer firmware awake) |

Git Bash gotcha: MSYS mangles `/sdcard/...` and `/data/...` args into `C:/Program Files/Git/...`.
Prefix commands with `MSYS_NO_PATHCONV=1` or wrap the whole remote command in one quoted
`adb shell "..."` string.

## 1. Native plugin (.so) rebuild — ~1 min warm

Build trees already configured (CMake-direct, NOT gradle). Order matters only when libImmCore
changed:

```bash
CMAKE="$LOCALAPPDATA/Android/Sdk/cmake/3.22.1/bin/cmake.exe"   # adjust
cd code
"$CMAKE" --build libImmCore/build          # only if libImmCore sources changed
"$CMAKE" --build libImmImporter/build      # only if importer changed
"$CMAKE" --build libImmPlayer/build        # only if player changed
"$CMAKE" --build appImmUnity/Projects/Android/app/build      # ALWAYS (links the .so)
cp appImmUnity/Projects/Android/app/build/libImmUnityPlugin.so \
   ImmUnitySampleProject/Packages/com.immersive-foundation.imm-unity/Plugins/Android/libs/arm64-v8a/
```

- The authoritative APP build tree is `appImmUnity/Projects/Android/app/build`.
  (`code/appImmUnity/android-build` is a stale tree whose re-configure FAILS - don't use.)
- Fresh configure recipe (new machine): NDK 26.1.10909125, ANDROID_ABI=arm64-v8a,
  ANDROID_PLATFORM=android-26, STL=c++_shared, 16KB page-size linker flags; build order
  libImmCore -> libImmImporter -> libImmPlayer -> app; `cp libpng16.a libpng16d.a` first
  (see docs/history + BUILDING.md).
- Windows editor DLL (for desktop tests): `msbuild code\projects\windows\imm.sln
  -t:"libImmExporter;appImmUnity" -p:Configuration=Release -p:Platform=x64`, deploy to
  `Packages/.../Plugins/x86_64/` (fully quit Unity first - it locks the DLL).

## 2. APK build — ~1-3 min warm

Two ways:

**Editor OPEN (via Unity MCP):** run C# through `script-execute`:
```csharp
ImmPlayer.Editor.BuildAutomation.BuildAndroidDebug();
```
The MCP call may time out and retry (each retry runs a full build - harmless, just wait for
`Editor.log` to go quiet). Refresh assets first if files changed outside Unity.

**Editor CLOSED (headless):**
```
Unity.exe -batchmode -quit -projectPath <proj> ^
  -executeMethod ImmPlayer.Editor.BuildAutomation.BuildAndroidDebug -logFile build.log
```

Output: `code/ImmUnitySampleProject/Builds/Android/IMMUnityTest.apk` (~130 MB dev build).

**Switching graphics API** (Vulkan testing vs GLES shipping default):
```csharp
PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
    new[]{ UnityEngine.Rendering.GraphicsDeviceType.Vulkan });        // or .OpenGLES3
```
GLES3 is the committed default; restore it (and rebuild) after Vulkan sessions.
`Assets/Editor/AndroidBootConfigPatcher.cs` injects
`xr-request-additional-vulkan-graphics-queue=1` into boot.config on every build (the native
plugin renders on Vulkan queue family 0 **index 1**; Adreno 740 exposes 4 graphics queues).

**Verify what's inside an APK** (deterministic sizes make timestamps unreliable):
```bash
unzip -p IMMUnityTest.apk lib/arm64-v8a/libImmUnityPlugin.so | grep -c "<some new log string>"
unzip -p IMMUnityTest.apk assets/bin/Data/Managed/Metadata/global-metadata.dat | grep -c "<new C# string>"
unzip -p IMMUnityTest.apk assets/bin/Data/boot.config
```

## 3. Install, launch, capture logs

```bash
ADB="$LOCALAPPDATA/Android/Sdk/platform-tools/adb.exe"
"$ADB" -s 2G0YC1ZF98028F shell am force-stop com.ImmersiveFoundation.IMMUnityTest   # ALWAYS - it stays resident
"$ADB" -s 2G0YC1ZF98028F install -r Builds/Android/IMMUnityTest.apk
"$ADB" -s 2G0YC1ZF98028F logcat -c
"$ADB" -s 2G0YC1ZF98028F shell monkey -p com.ImmersiveFoundation.IMMUnityTest -c android.intent.category.LAUNCHER 1
"$ADB" -s 2G0YC1ZF98028F logcat Unity:V ImmUnityPlugin:V piLog:V ImmRenderReporter:V DEBUG:V libc:F *:S > run.txt
```

After capturing logs, force-stop the app again before the next build/install cycle.

**Stream, never dump:** the logcat ring buffer wraps in under a second at full frame rate -
`logcat -d` after the fact loses all startup lines (renderer init, first-frame probes).
Always start the streaming capture BEFORE launching the app, as above.

**Visual debugging without wearing the headset:**
- `adb shell screencap -p /sdcard/x.png` + pull - the composited both-eye view.
- Device flag `IMM_UNITY_VK_DUMP_RTS` - dumps IMM's RAW per-eye offscreen textures (~4 s after
  start) to `files/imm_rt_eye0.png` / `imm_rt_eye1.png` (pre-composite ground truth).

**THE logging gotcha:** the native engine logs under tag **`piLog`** and renderer reports under
**`ImmRenderReporter`** - a filter of just `Unity:V ImmUnityPlugin:V *:S` silences every native
Vulkan diagnostic (cost us a full session). `DEBUG:V libc:F` adds crash tombstones.

**What healthy Vulkan looks like in the log:**
```
ImmUnityPlugin: Configuring IMM for Unity Vulkan external device (Quest)
ImmRenderReporter: Vulkan renderer submitting on dedicated second graphics queue
ImmRenderReporter: Vulkan renderer initialized with external device
Unity  : [IMM] Loaded document from memory: sample1.imm (ID: 0)
piLog  : Unity Vulkan render: camera=0 viewport=1680x1760 rendered=1 drawCalls=14 ...
```
`drawCalls>0` repeating every frame + no `Fatal signal` = rendering is executing.
Crash signature: `F/libc: Fatal signal 11 ... fault addr ...` + `DEBUG` backtrace frames.

## 4. On-device debug toggles (no rebuild needed)

- **Native flags** (all `IMM_UNITY_VK_*` toggles in main.cpp / piVulkan_Renderer.cpp):
  `adb shell setprop debug.imm.<FLAG_NAME> 1` (env vars never reach Android apps; the code
  falls back to these system properties). E.g. `debug.imm.IMM_UNITY_VK_FORCE_HOST_RENDER`,
  `debug.imm.IMM_UNITY_VK_NO_SECOND_QUEUE`, `debug.imm.IMM_UNITY_VK_HOST_BIND_ONLY`.
- **C# flags**: write flag names (one per line) to
  `/sdcard/Android/data/com.ImmersiveFoundation.IMMUnityTest/files/imm_debug_flags.txt`.
  E.g. `IMM_UNITY_VK_NO_RENDER_EVENTS` (issue no plugin events at all - vanilla-Unity test).
- Restart the app after changing either.

## 5. Vulkan validation layer on-device (exact VUIDs instead of driver SIGSEGVs)

1. Get `libVkLayer_khronos_validation.so` (arm64) from KhronosGroup/Vulkan-ValidationLayers
   releases ("android-binaries-*.zip").
2. Put it in the Unity project at `Assets/Plugins/Android/` (import platform Android/ARM64) so
   it's PACKAGED INTO THE APK. Keep it out of git; remove before shipping builds.
3. Enable/disable per run:
   `adb shell setprop debug.oculus.loadandinjectpackagedvvl.com.ImmersiveFoundation.IMMUnityTest 1`  (0 = off)
4. Read errors in unfiltered logcat: `VALIDATION`-tagged lines + Unity prints native callstacks.

**Do NOT use** the generic Android route (`settings put global enable_gpu_debug_layers 1` +
code_cache copy): on Quest it breaks the OpenXR runtime's own `xrCreateVulkanInstanceKHR`
(XR_ERROR_VALIDATION_FAILURE) and the app falls back to no-VR.

**Caveats:** VVL slows frames massively (expect a black/blank view and XR pacing complaints
while it's on - use it for log capture, not visual checks). Baseline Unity6+OpenXR+Vulkan on
Quest already emits `VUID-vkQueueSubmit-pSignalSemaphores-00067` (x~10 at startup),
`VUID-vkCreateInstance-ppEnabledExtensionNames-01388`, and finalize-related stacks even with
IMM fully disabled - don't chase those as IMM bugs.

**Attributing VUIDs (validated 2026-07-17):** run once with `IMM_UNITY_VK_NO_RENDER_EVENTS`
in the C# flag file - IMM's Vulkan never initializes, so every VUID that survives is Unity/
Meta baseline; anything that disappears is IMM's. Two gotchas: VVL suppresses each VUID
after `duplicate_message_limit` (10) reports, so "10x" means ">=10"; and the `E Unity`
validation lines carry a native backtrace a few lines ABOVE them (search up for
`libImmUnityPlugin.so` frames to confirm the call site). Current IMM debt:
`VUID-RuntimeSpirv-OpEntryPoint-08743` only (generated paint SPIR-V, needs Vulkan SDK to
regenerate - see the plan doc STATUS).

## 6. Known state / architecture notes (2026-07-16)

See `docs/history/QUEST_VULKAN_RENDER_INTEGRATION_PLAN.md` (STATUS section at top) for the
full Vulkan root-cause history. Short version: Unity's command buffer is NOT recordable at
plugin-event time on Quest (crash root cause); Unity's queue must not be touched mid-frame;
IMM therefore renders on its own second graphics queue, and content must be composited back
through Unity (offscreen RT + material blit) rather than written into the eye buffer directly.
