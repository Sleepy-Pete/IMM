# Audio Status — Spatialization and the Host Mixer

_Updated 2026-07-30. Branch `vr-main`. Nothing committed yet — the whole change set is in the
working tree. Architecture contract: [`docs/architecture/08-audio.md`](docs/architecture/08-audio.md)._

## ⏭ Start here next session

**Everything below builds and passes its tests, but NONE of it has been heard on a
device.** The next step is a listening A/B on Quest, not more code:

1. Push a build with `TheQuantumRace.imm` and ride a segment with positional sound.
2. Toggle `IMM_AUDIO_NO_SPATIAL=1` in `imm_debug_flags.txt` and ride the same segment.
3. The question to answer: **do positional layers land where the drawing puts them, and does
   the image hold still when you turn your head?**

If a bed sounds inside-out or rotates the wrong way, try `IMM_AUDIO_AMBISONIC_FUMA=1` — the
channel convention is the one assumption in this work that could not be verified from the
content (see §"Assumptions worth checking" below).

## What changed, and why

The spatial model was implemented exactly once — inside Audio360, a discontinued proprietary
Windows binary — and re-stubbed on every other platform. Android and macOS each had the same
fourteen empty function bodies, written independently. That is why nothing carried.

The fix was to move the model **above** the platform boundary.

| # | Change | Files |
|---|--------|-------|
| 1 | **Portable spatializer.** Attenuation curves, cone/frustum modifiers, constant-power pan + Woodworth ITD + head-shadow filtering, first-order ambisonic decode, and a gain+pan reduction for weak output stages. Pure math — no device, no SDK, no OS. | `libImmCore/src/libSound/piSoundSpatializer.{h,cpp}` |
| 2 | **Quest spatializes.** The fourteen no-ops are implemented; the mixer runs a per-voice delay line and per-ear shadow filter, interpolating parameters across each callback block. | `libImmCore/src/libSound/android/piSoundEngineAndroid.cpp` |
| 3 | **Apple is no longer silent.** The bridge selected `API::Null` on anything that was not Windows or Android, so Unity builds for macOS/iOS had *no audio at all*. | `appImmShared/src/imm_engine_bridge.cpp` |
| 4 | **Apple spatializes** as far as `AVAudioPlayer` allows — exact attenuation and cone/frustum, plus lateral position. | `libImmCore/src/libSound/macos/piSoundEngineAVFoundation.mm` |
| 5 | **Host lifecycle.** `PauseAllSounds`/`ResumeAllSounds` exported and wired to `OnApplicationPause`/`OnApplicationFocus`. | `appImmUnity/src/main.cpp`, `ImmPlayerManager.cs` |
| 6 | **Host mixer.** Per-layer fader/mute/solo plus 8 buses, with a C# surface. | `libImmPlayer/src/soundMix.h`, `player.{h,cpp}`, `ImmDocument.cs` |
| 7 | **Conformance tests + docs.** | `run-audio-conformance.ps1`, `docs/architecture/08-audio.md` |

## Parity with Audio360 is deliberate

Windows keeps Audio360 — it works and content was mixed against it. The attenuation and
modifier math in the portable spatializer is a **port of that backend's behaviour**, so
everything else agrees with the reference rather than drifting from it:

- Logarithmic attenuation reproduces `factor = 5 / log2(max/min)`, landing 30 dB down at the
  maximum distance, with `maxDistanceMute` semantics.
- The cone uses angular-space smoothstep, not cosine space.
- **The frustum returns hard silence behind the emitter and ignores `attenOut`.** That is a
  quirk. It is reproduced on purpose and commented in both places. Do not "fix" it in one
  backend only — that is precisely how platforms drift apart.

## Verification evidence

| Check | Result |
|---|---|
| `run-audio-conformance.ps1` — spatializer suite | **59 checks, 0 failures** |
| `run-audio-conformance.ps1` — mixer suite | **16 checks, 0 failures** |
| Quest `libImmUnityPlugin.so` (arm64-v8a, NDK 24, CMake) | builds + links, 0 errors |
| New plugin exports present in the `.so` | 13/13 confirmed via `llvm-nm` |
| Windows `ImmUnityPlugin.dll` + `libImmCore`/`libImmImporter`/`libImmPlayer` | builds clean, Audio360 path untouched |
| Android backend under `-Wall -Wextra` | no warnings from new code |
| **macOS / iOS** | **not compiled — needs a mac** |
| **On-device listening** | **not done** |

Two build-entry-point traps, both pre-existing and neither a code signal:

- The gradle project at `code/appImmUnity/Projects/Android` **fails at configuration** (AGP does
  not resolve). Use `build_android.bat`, or CMake directly with NDK `24.0.8215888` and SDK
  CMake `3.22.1`.
- `projects/windows/build-unity-plugins.ps1` compiles fine but **fails at its post-build copy**
  when the Unity Editor holds `opus.dll` open. Run msbuild directly with
  `/t:appImmUnity:Rebuild /p:PostBuildEventUseInBuild=false` to verify the code.

## Assumptions worth checking on device

1. **Ambisonic channel convention.** 4- and 9-channel beds are decoded as **AmbiX (ACN/SN3D)**,
   which is what Audio360 called `AMBIX_4`. If a bed sounds inside-out or rotates the wrong
   way, `IMM_AUDIO_AMBISONIC_FUMA=1` switches the ordering. 10/11-channel beds are Facebook's
   **TBE** layout, which is *not* AmbiX; those deliberately fall back to flat playback using
   their head-locked stereo pair (the last two channels) rather than being mis-decoded.
2. **The near-field collapse radius** (0.25 m) and the **pan depth cap** (0.92) are judgement
   calls, tuned to avoid an emitter swinging wildly as it passes through the head and to avoid
   a hard pan reading as a fault. Both are single constants in `piSoundSpatializer.cpp`.
3. **The listener is one frame stale.** `Player::GlobalWork` pushes `SetListener` *after* the
   layer traversal, so `ComputeModifiers` sees the previous frame's head pose. Pre-existing,
   harmless at walking speed, possibly audible at QuantumRace travel speed.

## Killswitches

| Flag | Effect |
|------|--------|
| `IMM_AUDIO_NO_SPATIAL` | flat, unspatialized playback — the previous behaviour (Quest and Apple) |
| `IMM_AUDIO_AMBISONIC_FUMA` | decode 4-channel beds as FuMa instead of AmbiX (Quest) |

Set through `imm_debug_flags.txt`; the C# side pushes every entry into the process environment
at boot. **Cat the flag file after pushing** — a silent typo reads as "the change did nothing".

## What is still missing

1. **No HRIR convolution.** Pan + ITD + head shadow is a large improvement over centred mono,
   but it is not true binaural and does not resolve front from back. The MIT-licensed HRIRs and
   FIR filter in `facebookincubator/Audio360` drop in behind `ComputeBinaural` without changing
   a single caller — that is the layering paying off.
2. **Apple is gain + pan only**, because `AVAudioPlayer` offers nothing else. Moving its output
   stage to `AVAudioEngine` + `AVAudioEnvironmentNode`, or to PHASE, buys HRTF, ambisonic and
   room acoustics — and covers macOS, iOS, tvOS and visionOS in one adapter.
3. **No host handoff.** IMM audio still cannot reach Unity's AudioMixer, the Meta XR Audio
   spatializer, or visionOS's OS-level spatializer. The host mixer added here balances IMM's
   own voices; it does **not** put them inside the host's mix graph. On visionOS a
   self-managed output stream is actively wrong — it fights the system renderer — so this is
   the prerequisite for any visionOS attempt.
4. **Device parameters are hardcoded** at 48 kHz / 512 frames, with no query of the device's
   native rate or optimal burst size.

## Licensing note

Audio360 stays on Windows by decision. Its status is worth recording: Meta ended support on
2022-05-16 and removed the downloads; the vendored copy came from a third-party archive and
carries **no LICENSE or EULA** in `thirdparty/audio360-sdk/`, and no entry in
`THIRD_PARTY_LICENSES.txt`. Shipping an app that uses it is the historically intended use;
**redistributing the SDK itself** — which this repo does, including inside the published UPM
package at `Plugins/x86_64/Audio360.dll` — is the part with no documented grant. The portable
spatializer is what eventually makes dropping it possible.
