# Handoff — Vulkan Merge into Unity 6 Branch

**Date:** 2026-06-24
**Status:** ✅ **PROMOTED** — `icosa-mirror-version` fast-forwarded to `c19ccd3` (Path A step 2 done, 2026-06-24; Unity was closed). Awaiting Unity Editor + Quest tests (your part).
**Constraint honored:** Local only — **nothing pushed**. Old tip `62f56e1` preserved at branch `safety/pre-vulkan-promote-2026-06-24`.

---

## TL;DR

The Vulkan work from `icosa-mirror/experiment/unity-vulkan-overlay-from-feature` has been
merged into our Unity 6 line, **both plugins rebuilt from the merged source**, validated as
far as possible without the Editor, and committed to an isolated worktree branch. The only
remaining steps require Unity/headset — they're yours.

---

## Git state (verified)

| Branch | Commit | Meaning |
|---|---|---|
| `icosa-mirror-version` | `c19ccd3` | **Your current branch — now carries the merge** (FF'd 2026-06-24). Main tree at `A:\Github\IMM2\IMM`. |
| `safety/pre-vulkan-promote-2026-06-24` | `62f56e1` | Pre-promotion tip. `git reset --hard 62f56e1` to back out. |
| `vulkan-merge-wip` | `c19ccd3` | Same commit as your branch now. Worktree `A:\Github\IMM2\IMM-vulkan-merge` (redundant; remove after tests pass). |

Worktrees:
- `A:/Github/IMM2/IMM`               → `icosa-mirror-version` (your tree, keep Editor here)
- `A:/Github/IMM2/IMM-vulkan-merge`  → `vulkan-merge-wip` (the merge work)

Commit chain on `vulkan-merge-wip`: merge `9610a46` (Vulkan into Unity 6, 9 conflicts resolved)
→ `c19ccd3` (both plugins rebuilt from the merged source).

---

## What was done

### 1. Merge (commit `9610a46`)
- 9 conflicts, all resolved. **Unity 6 kept** throughout; Vulkan brought in.
- All 3 of our Unity-6 fixes re-applied on top of their Vulkan-complete files:
  - **Reverse-Z depth** (`imm_engine_bridge.cpp`): `DepthBuffer::Linear10` for DX, `Linear01`
    otherwise. (Their branch independently converged on the same fix + added Vulkan/Metal
    zero-to-one clip depth — took theirs.)
  - **`UseRenderIntoTextureProjection`** (`ImmPlayerManager.cs`): Unity-6 `targetTexture/HDR/MSAA`
    branch returns `true`.
  - **Render-event `SetRenderTarget`** (`ImmPlayerManager.cs`): `BuiltinRenderTextureType.CameraTarget`
    set before `IssuePluginEvent` on the non-Vulkan path.
- `manifest.json` resolved to Unity 6 package versions; removed the duplicate MCP package
  (kept `com.ivanmurzak.unity.mcp` 0.82.1).

### 2. Plugins rebuilt from merged source (commit `c19ccd3`)

| Plugin | Path (in worktree) | Validation |
|---|---|---|
| **Windows DLL** (D3D11) | `Packages/com.immersive-foundation.imm-unity/Runtime/Plugins/x86_64/ImmUnityPlugin.dll` | 4.5 MB, MSVC x64, compiles + links. Vulkan code Android-gated. |
| **Android Vulkan `.so`** | `Packages/com.immersive-foundation.imm-unity/Runtime/Plugins/Android/libs/arm64-v8a/libImmUnityPlugin.so` | 11.3 MB, ELF64 AArch64, 16 KB-aligned, exports `ConfigureVulkanRenderEvent` / `LoadFromMemory` / `LoadFromFile` / `UnityPluginLoad`. |

Native build chain (NDK 26.1.10909125, CMake + Ninja, arm64-v8a, android-26, `c++_shared`,
16 KB page alignment): **libImmCore (8,248-line Vulkan renderer) → libImmImporter → libImmPlayer
→ appImmUnity** built standalone in dependency order (the self-contained `add_subdirectory`
path conflicts — each lib re-imports its deps).

### 3. Self-tests run (everything possible without the Editor)
Compile ✓ · link/symbol resolution ✓ · ARM64 arch ✓ · plugin exports ✓ · 16 KB page alignment ✓.

---

## Earlier fixes folded in (context)

- **Unity 6 black render** → reverse-Z depth (`Linear10` for DX). Confirmed rendering.
- **Quest crash** (Unity logo → skybox → black): `Player::Load(wchar_t*)` SIGSEGV via the
  StreamingAssets → temp-file → path-load route. Fixed in
  `Assets/Scripts/ImmFeatureExamples.cs` by switching to
  `ImmPlayerManager.Instance.LoadDocumentFromMemory(data, fileName)` — in-memory load, keeps
  the `.imm` inside the APK, avoids the crashing path-load.

---

## Remaining steps — YOUR PART (needs Unity / headset)

### Path A (recommended): promote to your branch, then test
1. ~~**Close your current Unity**~~ ✅ Unity was closed.
2. ~~**fast-forward `icosa-mirror-version` to `c19ccd3`**~~ ✅ **Done 2026-06-24** (local only, no push).
3. **Reopen** the project in Unity 6 (expect a big package reimport). Run `/mcp` to reconnect *only* if you want me to drive/inspect the Editor.
4. ~~**set Android graphics API → Vulkan**~~ ✅ **Already Vulkan** in `ProjectSettings.asset` (`AndroidPlayer m_APIs: 15000000`, came in with the merge). No action needed.
5. **Desktop Editor test** (D3D11) — confirms the merge still renders the forest. ← **next**
6. **Quest Build And Run** — exercises the new Vulkan `.so` on-device. ← then this

> Note: `WindowsStandaloneSupport` is also set to Vulkan in these settings; the Editor test is unaffected (Editor = D3D11), but a Windows *standalone build* would need its API switched back to D3D11 (Windows plugin is D3D11-only; Vulkan is Android-gated).

### Path B: test the worktree first, promote later
- Open `A:\Github\IMM2\IMM-vulkan-merge\code\ImmUnitySampleProject` directly in Unity 6 and test
  there. Promote to `icosa-mirror-version` afterward.

### Cleanup (after promoting)
```
git worktree remove A:/Github/IMM2/IMM-vulkan-merge
```

---

## Rebuild reference (if plugins need rebuilding)

- **Android `.so`:** NDK 26.1.10909125. Build libs standalone in order
  (libImmCore → Importer → Player → appImmUnity importing them). App spec:
  `code/appImmUnity/Projects/Android/app/CMakeLists.txt` (authoritative — links android/log/EGL/
  GLESv3/z/OpenSLES/mediandk; **no** Audio360, **no** explicit libvulkan).
  Flags: `ANDROID_ABI=arm64-v8a`, `ANDROID_PLATFORM=android-26`, `ANDROID_STL=c++_shared`,
  `-Wl,-z,max-page-size=16384 -Wl,-z,common-page-size=16384`.
  Gotcha: `app/CMakeLists.txt` imports `libpng16d.a` (debug name) — a Release build produces
  `libpng16.a`; copy it: `cp libpng16.a libpng16d.a`.
  Note: `code/appImmUnity/CMakeLists.txt` + `build_android.bat` are the **outdated** self-contained
  path (main.cpp only, NDK 24, wrong target names) — **not used**.
- **Windows DLL:** MSBuild/VS2022. Build `/t:"libImmExporter;appImmUnity"` (libImmExporter first,
  or you get LNK1181 missing `libImmExporter.lib`).

---

## Why Unity Editor can't mute IMM audio (answered earlier)

IMM uses Facebook Audio360/FBA spatial audio, which bypasses Unity's audio mixer entirely — so
Unity's mute/volume don't touch it. (On the Android plugin the backend is OpenSLES.)
