# Session Close — 2026-07-30 → 2026-07-31

_Branch `vr-main`, 18 commits (`72bfc77..d1a5453`), **none pushed** by decision.
Device: Quest 3, build `IMMUnityTest_20260731_picdepthwrite.apk` (02:12) installed,
app stopped, flag file reset to `IMM_UNITY_DOC_FILE=TheQuantumRace.imm`.
Verification: Pete in-headset, collaborative A/B — the mode that solved what five
rounds of headless instrumentation could not._

## Fixed and verified this session

| # | Defect | Root cause | Fix | Verified |
|---|--------|-----------|-----|----------|
| 1 | Quest audio flat / one-channel | Spatial model existed only inside Audio360 (Windows); Android had fourteen empty stubs | Portable spatializer above the platform boundary + Quest backend (`46d4202`), host mixer (`d1dbc6f`) | **Heard**: "audio worked great" — positional sources track, image holds on head turn |
| 2 | Pause runs on in the background; play jumps ahead | `Pause` stopped every timeline; `Resume` rebased only world-visible ones — off-screen scene timelines absorbed the whole pause | Pause records exactly what it stopped; Resume rebases that set (`290969e`) | Fixed + committed; passive confirm next don |
| 3 | QuantumRace "404" from StreamingAssets | **Stale install** — the installed APK (186 MB) never contained the file. The packaging/size-limit theories were measured against a locally built APK | Install a current build; `unzip -l` on the *installed* APK is now a standing precheck | Loads clean: 79 chapters, ~22 s |
| 4 | `shield.png1` stopped loading | Same-format `Convert` returned failure (only cross-format pairs implemented); 07-30's instrumentation made that bool fatal | Same format = no-op success (`c25256d`) | Device log clean |
| 5 | **All dark pictures invisible** (360sky, floor, dome…) | **Colour-space test inverted** in the hand-written Vulkan picture shaders: `ColorSpace {Linear=0, Gamma=1}`, shaders applied `pow(rgb, 2.2)` when `COLOR_SPACE == 0`. We run Linear ⇒ every picture gamma-crushed in proportion to its darkness (360sky 29/255 → 2/255) | Invert the test (`4ea51cb`) | **Seen**: "the skybox came in" |
| 6 | **Dome a faint hollow ball; floor washed/see-through** | **Pictures never wrote depth** — `depthWriteEnable` hardcoded 0 in the Vulkan picture pipeline (GLES writes it). A picture could not occlude itself: the dome resolved by triangle order, back hemisphere over front | Write follows test, like paint (`42989e7`) | **Seen**: "that's it! it's all working — the dome and the floor" |
| 7 | Latent: picture descriptor set never bound blue noise; pool undersized | Binding 7 undeclared/unwritten; pool requested 3 uniform buffers for a 4-buffer layout | Declare + write + size correctly (`f9d3b8a`) | Not the visible bug, but real; fixed |

**The picture saga was two stacked defects (#5 + #6).** Fixing either alone still
looks broken, which is much of why it survived four sessions.

## Explained, not bugs

- **"Popup image" during debug modes** — `skybreak1/2.png`: luminance ~245 but
  authored ~fully transparent (alpha ≈ 0 on 96 % of pixels). Forced-opaque debug
  modes made them appear; normal alpha returns them to authored near-invisibility.
- **Dome looks stretched** — `littledome3.png` is 16:9 (3840×2160) typed
  `Image360EquirectMono`, whose mapping assumes 2:1. Authoring conversation with
  the author, not code.
- **Floor sits beneath the painted ground** — authored layering; the depth-on
  composite (paint in front, floor beneath/beyond) is the intended look
  (Pete's "#2" verdict on the A/B frames).

## The tool that cracked it: the picture debug ladder

`IMM_UNITY_VK_PIC_DEBUG=<n>` — fragment-shader specialization constant, inert at 0,
**kept in the build**. Each mode isolates one stage; Pete answers each in seconds
from inside the headset:

| n | Shows | Question it answers |
|---|-------|--------------------|
| 1 | solid magenta | does the draw produce fragments at all? |
| 2 | texture RGB, alpha forced 1 | does sampling work? |
| 3 | sampled alpha as greyscale | is `texel.a` the killer? |
| 4 | `layer.mOpacity` as greyscale | is the uniform the killer? |
| 5 | green = front face, red = back face | winding / self-occlusion / flip |

Five rounds of CPU-side counters ([IMM_PICFMT], [IMM_PICUP], [IMM_PICCULL],
[IMM_PICPLACE], [IMM_VKTEX]) all *honestly reported success* — the defects lived
past the last stage any CPU counter can see. The ladder + Pete's eyes closed in
minutes what the counters could not close in a day. All the counters stay too:
they are what proved which stages were *not* guilty.

## Tooling added

- `tools/headset-run.ps1` — precheck (incl. **installed-APK content listing** — the
  check that would have prevented the "404" session) → flags (always read back) →
  capture armed before launch → keep-alive → observe → force-stop always → parsed
  verdict.
- `tools/headset-screencap.ps1` — framebuffer grabs at chosen offsets.
- `IMM_UNITY_AUTO_CONTINUE[=dwellMs]` — presses through authored stops (same
  `Continue()` verb as the A button); `[IMM_TEST]` markers.
- Chapter→time map extracted from skip logs (`chapter 49 = 7:23.1`,
  `74 = 9:12.6`) — with `IMM_UNITY_START_CHAPTER` turns a 12-minute soak into a
  90-second probe.
- `[IMM_AUDIO]` layer-type line — spatialization routing is one grep
  (`type=Positional|Ambisonic|Flat`), never a guess.
- `IMM_UNITY_VK_PIC2D_NO_DEPTH` — diagnostic only, default off.

## Wrong turns, kept honest

Four theories died on device evidence before the real causes: texture allocation,
scale-dependent clip range, host-data malloc, blue-noise descriptor (real bug,
wrong culprit). Two premature closes: "allocation confirmed" (retracted same
hour) and the ~1 am session close calling the dome/floor authored — **Pete's
pushback ("flipped and not rendered double sided") was correct** and led straight
to #6. Working rules that came out of it:

1. When Pete says "not rendering properly", the observation outranks the closure
   narrative.
2. Verify against the **installed** APK, never the local build.
3. Compounding weirdness (reference crashes + mangled splash + dead controls) =
   the Horizon OS shell wedge → **reboot first**, then diagnose.
4. Never auto-install from a background job while the headset is in use —
   archive, install on Pete's word.
5. `EditorApplication.delayCall` builds silently die in an unfocused editor —
   synchronous `script-execute` whose MCP timeout is the *success* signal.
6. ASCII scans cannot find `L"..."` literals — check wide encodings before
   declaring a symbol absent.

## Open items (small, none blocking)

- Colour-accuracy pass vs the native viewer — the colour fix changes every
  picture's tone (Pete: "we can do this comparison later").
- `ocean.png` not seen at 10:00+ — likely authored `Bg` occlusion; casual recheck.
- `desert1.JPG` is the only picture that never emits a `LoadAsset` line.
- GLES reference player crashed twice at launch (pre-reboot OS; retry sometime).
- `Player::Resume(id)` sets `mTarget = cmdId` not `id` — latent, dormant with a
  single document; one-line fix waiting.
- Pause fix passive verify: pause mid-film, wait, resume — no jump.
- macOS/iOS audio backends compile-checked only — need a Mac.
- Push `vr-main` (18 commits) when Pete says so.

## Next session opener

Read `QUEST_VULKAN_STATUS.md` "Start here" (kept current). Device carries the
02:12 build with every fix and the debug ladder inert. One full-film don with
audio attention = the closing verification of the whole arc; then push.
