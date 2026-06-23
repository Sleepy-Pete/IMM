# 6. Engine Integrations — Unity, Godot, StrokeReader

The same `libImmPlayer` engine is surfaced to game engines through native plugins. There are
three distinct integration products plus one shared layer:

| Product | Renders? | For | Boundary |
|---------|----------|-----|----------|
| **appImmUnity** | yes (full player) | Unity | `extern "C"` + P/Invoke |
| **appImmGodot** + GDExtension | yes (full player) | Godot 4.x | C-API + GDExtension C++ |
| **appImmStrokeReader** | **no** (data only) | any engine / open-brush-fast | `extern "C"` |
| **appImmShared** (`ImmEngineBridge`) | — | shared by Unity + Godot | C++ |

```mermaid
flowchart TD
    Play["libImmPlayer (engine)"]
    Imp["libImmImporter"]
    Bridge["appImmShared::ImmEngineBridge<br/>init · graphics · camera · lifecycle"]

    Play --> Bridge
    Bridge --> UnityC["appImmUnity (extern C)"]
    Bridge --> GodotC["appImmGodot (C-API)"]
    UnityC --> UnityCS["Unity C# (UPM package)"]
    GodotC --> GDExt["GDExtension C++ (ImmViewerNode)"]
    Imp --> Stroke["appImmStrokeReader (extern C)"]
    Stroke --> StrokeCS["C# / SharpQuill compat"]
```

---

## 6.1 appImmShared — `ImmEngineBridge`

`code/appImmShared/src/imm_engine_bridge.{h,cpp}` is the engine-agnostic core that both Unity
and Godot build on. It owns renderer creation, the player lifecycle, logging/timing, the sound
backend, and a per-camera matrix cache (up to 256 cameras).

```cpp
struct InitConfig {
    int colorSpace, antialiasing;
    const char *logFileName, *tmpFolderName;
    piRenderer::API rendererApi;   // GL / DX / GLES / Metal
    void *graphicsDevice;          // D3D device or Metal device
    bool initializeRendererOnInit; // deferred init for Android
    // … window/display/vsync/fullscreen flags
};

class ImmEngineBridge {
    bool Init(const InitConfig&);
    bool CompleteGraphicsInitialization();   // deferred (Android)
    void Shutdown();
    void GlobalWork(bool enabled, int budgetMicroseconds);
    void SetCameraMatrices(/* cameraID, stereoType, world/proj per eye */);
    bool RenderCamera(int cameraID, const ViewportInfo&, int eyeID, bool tickSound);
    Player* GetPlayer();  piRenderer* GetRenderer();  piLog* GetLog();
};
```

This is why the Unity and Godot C-APIs look almost identical — they're thin shells over the same
bridge.

---

## 6.2 Unity plugin

### Native boundary

`code/appImmUnity/src/main.cpp` exposes `extern "C"` functions and registers Unity's native
render-plugin callbacks.

```mermaid
sequenceDiagram
    autonumber
    participant U as Unity (C#)
    participant P as ImmUnityPlugin (native)
    participant B as ImmEngineBridge
    U->>P: UnityPluginLoad(IUnityInterfaces)
    U->>P: Init(colorSpace, AA, log, tmp)
    P->>B: Init(...) with Unity's graphics device
    U->>P: LoadFromFile / LoadFromMemory → docId
    loop each frame
        U->>P: GlobalWork(enabled)
        U->>P: SetMatrices(cameraID, stereo, world/proj per eye)
        U->>P: issue GL.IssuePluginEvent(GetRenderEventFunc(), eventId)
        P->>P: iOnRenderEvent(eventId) — decode camera/eye
        P->>B: RenderCamera(...)
    end
```

The render-event id **bit-packs** camera id + eye id, because Unity's plugin-event callback only
passes a single int. `iOnGraphicsDeviceEvent` handles D3D11/D3D12/GL/Metal device init/teardown.

### C# API (UPM package)

`ImmUnitySampleProject/Packages/com.immersive-foundation.imm-unity/Runtime/`:

- `ImmNativePlugin.cs` — `[DllImport("ImmUnityPlugin")]` P/Invoke wrappers (iOS uses
  `__Internal`).
- `ImmPlayerManager.cs` — singleton: `Initialize()`, `LoadDocument(path) → ImmDocument`,
  per-frame `GlobalWork()`, camera registration, and **two rendering modes** (CommandBuffer vs
  callback) auto-selected by render pipeline.
- `ImmDocument.cs` — the per-document façade. Surface area:

```mermaid
flowchart LR
    Doc["ImmDocument"]
    Doc --> Play["Playback: Pause/Resume/Restart/Continue"]
    Doc --> Nav["Chapters: SkipForward/Back · SetChapter · Get/CurrentChapter"]
    Doc --> Time["Timeline: SetTime/GetTime/GetPlayTime (µs)"]
    Doc --> Layers["Layers: GetLayerCount · GetLayerInfoByIndex<br/>SetLayerVisible/Opacity/Transform · diagnostics"]
    Doc --> Spawn["Spawn areas: get/set active, info"]
    Doc --> Info["Bounds · DocumentInfo flags · IsSequenceReady"]
```

`LayerInfo` carries id/type/parent/visibility/opacity/bounds/childcount plus paint stats
(numDrawings, numFrames, numStrokes) and names — enough to build a runtime layer panel.

---

## 6.3 appImmStrokeReader

`code/appImmStrokeReader/src/main.cpp` — a **render-free** plugin that exposes raw paint
geometry. It depends only on `libImmImporter` (not the player), which is why it builds tiny and
on every platform (incl. iOS as a static lib).

**Why separate?** It feeds **open-brush-fast** and other pipelines that want to draw IMM strokes
with their *own* renderer, and ships a **SharpQuill** compatibility mapping so stroke data lines
up with the Quill data model.

```mermaid
flowchart TD
    LoadF["StrokeReader_LoadFromFile/Memory → docId"]
    Layers["StrokeReader_GetLayerCount / GetLayerInfo / GetLayerTransform"]
    Anim["GetLayerAnimationInfo (frameRate, numFrames, repeat)<br/>GetFrameBuffer"]
    Draw["GetDrawingCount / GetDrawingIndexForChapter"]
    Strokes["GetStrokeCount / GetStrokeInfo (brush, bbox, ptcount)"]
    Pts["GetStrokePoints → StrokePoint[]{pos,color,width}"]
    Pic["GetPictureInfo / GetPicturePixelData"]
    LoadF --> Layers --> Anim --> Draw --> Strokes --> Pts
    Layers --> Pic
```

Also exposes `StrokeReader_GetBuildId()` so a downstream Unity project can detect a hot-swapped
DLL. C# binding:
`Packages/com.immersive-foundation.imm-stroke-reader/Runtime/ImmStrokeReader.cs` (+
`SharpQuillCompat.cs`). The root README documents the local DLL hot-copy workflow into a
downstream project's `Library/PackageCache`.

---

## 6.4 Godot port

GDExtension-based (Godot 4.5+), with a C backend (`appImmGodot`) over `ImmEngineBridge` and C++
bindings (`appImmGodotGDExtension`).

```mermaid
flowchart TD
    Bridge["ImmEngineBridge"] --> CAPI["appImmGodot: ImmGodot_* C-API<br/>InitEx(rendererApi) · RenderCamera<br/>BeginMetalFrame/EndMetalFrame"]
    CAPI --> Node["ImmViewerNode : Node3D<br/>full scripted API + signals"]
    CAPI --> Comp["ImmViewerCompositorEffect<br/>(Metal Forward+ production path)"]
    Node --> GD["GDScript / scenes"]
```

- **`ImmViewerNode`** (`imm_viewer_node.cpp`) — a `Node3D` exposing the whole control surface to
  GDScript: load/play/pause/chapters/timeline/layers/spawn-areas/transform/camera-registration,
  plus signals (`document_loaded`, `playback_started`, `spawn_area_changed`, …).
- **`ImmViewerCompositorEffect`** — integrates IMM into Godot's `RenderingDevice` render graph;
  the **production rendering path is macOS Metal (Forward+)**, validated with pixel-level smoke
  tests (can dump a PNG via `IMM_GODOT_VISUAL_SMOKE_PNG`).
- **Build**: SCons + godot-cpp; Windows helper `code/projects/windows/build-godot-extension.ps1`
  stages `imm_godot_extension.dll` + `ImmGodotPlugin.dll` + runtime deps into
  `addons/imm_viewer/bin/windows/...`.

### Status nuance

There are three render tiers, and they are **not equally mature**:

| Tier | Where | Maturity |
|------|-------|----------|
| Script-stub (pure GDScript) | all | validates the scripting API without any native binary |
| OpenGL Compatibility (smoke) | bootstrap | renders, not production-grade |
| Metal Forward+ (visual) | macOS | **production gate** — the CI rendering check |

Windows CI currently validates build/staging/API and the script-stub smoke, **not** native
rendering. Background and plan: `docs/unity-viewer-to-godot-port-plan.md`.

---

## 6.5 Choosing an integration

```mermaid
flowchart TD
    Q1{Need to render IMM<br/>with IMM's own engine?}
    Q1 -- "No, just stroke data" --> SR["appImmStrokeReader"]
    Q1 -- Yes --> Q2{Which engine?}
    Q2 -- Unity --> UN["appImmUnity (mature)"]
    Q2 -- Godot --> GO["appImmGodot (macOS/Metal mature;<br/>Windows API-only)"]
    Q2 -- "None / native" --> VW["appImmViewer"]
```

## 6.6 Cross-references

- The engine these wrap → [05-playback-engine.md](05-playback-engine.md)
- The stroke/layer structures exposed → [02-imm-file-format.md](02-imm-file-format.md)
- Building each plugin → [07-build-system.md](07-build-system.md)
