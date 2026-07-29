# Consuming the IMM Unity packages in other projects

Two UPM packages live in this repo and are developed here:

| Package | Path | Depends on |
|---|---|---|
| `com.immersive-foundation.imm-unity` | `code/ImmUnitySampleProject/Packages/com.immersive-foundation.imm-unity` | `imm-stroke-reader` |
| `com.immersive-foundation.imm-stroke-reader` | `code/ImmUnitySampleProject/Packages/com.immersive-foundation.imm-stroke-reader` | `com.unity.nuget.newtonsoft-json` |

Both ship prebuilt native binaries (Android arm64, iOS, macOS, Windows x64), so a
consuming project needs no C++ toolchain.

**UPM does not resolve transitive git dependencies.** Whichever mode you use, add
*both* IMM packages explicitly. Newtonsoft comes from Unity's own registry and
resolves normally.

---

## Mode 0 — `.unitypackage` (easiest for someone else; no manifest editing)

A single self-contained file. The consumer drags it into an open Unity project
(or *Assets → Import Package → Custom Package*) and everything lands ready to
use — both packages, all native binaries for Android arm64, Windows x64, macOS
and iOS, with their import settings intact.

Because the exported paths keep their `Packages/...` prefix, the import creates
**embedded packages** rather than loose files in `Assets/`. Assembly
definitions, plugin platform settings and package identity all survive, and
uninstalling is deleting the two folders under `Packages/`.

Build it:

```powershell
tools/build_unitypackage.ps1            # batch mode; Editor must be closed
tools/build_unitypackage.ps1 -SyncNative  # rebuild the .so first
```

or, with the Editor open, use the menu item **IMM → Export Unity Package
(release artifact)**. Output lands in `code/ImmUnitySampleProject/dist/`
(gitignored — attach it to a GitHub Release rather than committing it).

**The one manual step for the consumer:** add Newtonsoft Json, which SharpQuill's
reader/writer genuinely require. *Package Manager → + → Add package by name →*
`com.unity.nuget.newtonsoft-json`. It is deliberately not bundled: shipping a
second copy collides with any project that already has it.

Suggested release note for consumers:

> **Install**
> 1. Import `IMM-Unity-<version>.unitypackage` into your project.
> 2. Package Manager → *Add package by name* → `com.unity.nuget.newtonsoft-json`.
>
> Requires Unity 2021.3+. Native plugins included for Android arm64 (Quest),
> Windows x64, macOS and iOS — no C++ toolchain needed.

## Mode 1 — local path (use this while iterating)

Best while the plugin is changing daily: the consuming project references the
package *in place*, so a rebuild here is visible there immediately with no
publish, commit, or version bump.

In the consuming project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.immersive-foundation.imm-unity": "file:A:/Github/IMM2/IMM/code/ImmUnitySampleProject/Packages/com.immersive-foundation.imm-unity",
    "com.immersive-foundation.imm-stroke-reader": "file:A:/Github/IMM2/IMM/code/ImmUnitySampleProject/Packages/com.immersive-foundation.imm-stroke-reader",
    "com.unity.nuget.newtonsoft-json": "3.2.1"
  }
}
```

Relative paths (`file:../../IMM/code/...`) work too and survive moving the pair
of repos together. Machine-specific either way — fine for local work, not for
sharing.

## Mode 2 — git URL + tag (use this to share)

Portable across machines and collaborators. Pin a tag so consumers upgrade
deliberately:

```json
{
  "dependencies": {
    "com.immersive-foundation.imm-unity": "https://github.com/Sleepy-Pete/IMM.git?path=code/ImmUnitySampleProject/Packages/com.immersive-foundation.imm-unity#imm-unity-v0.2.0",
    "com.immersive-foundation.imm-stroke-reader": "https://github.com/Sleepy-Pete/IMM.git?path=code/ImmUnitySampleProject/Packages/com.immersive-foundation.imm-stroke-reader#imm-unity-v0.2.0",
    "com.unity.nuget.newtonsoft-json": "3.2.1"
  }
}
```

`?path=` is relative to the repo root; `#` takes a tag or branch. Use `#vr-main`
to track the branch instead of pinning — convenient, but consumers then move
whenever this branch does.

**Caveat:** UPM clones the whole repo to resolve a git dependency, and this repo
is ~275 MB (mostly rebuilt native binaries in history — the Android `.so` alone
has 39 revisions). Resolution is slow but works. See "Reducing clone cost" below.

## Mode 3 — tarball (use this for a frozen handoff)

```powershell
cd code/ImmUnitySampleProject/Packages/com.immersive-foundation.imm-unity
npm pack      # -> com.immersive-foundation.imm-unity-0.2.0.tgz
```

Consumer references the file:

```json
"com.immersive-foundation.imm-unity": "file:../Packages/com.immersive-foundation.imm-unity-0.2.0.tgz"
```

Smallest download and fully reproducible — no repo clone, no network at resolve
time. Best for sending a specific build to someone outside the project.

---

## Release workflow

```powershell
# 1. build the native plugin and copy it into the package
tools/sync_native_to_package.ps1

# 2. bump "version" in the package's package.json and add a CHANGELOG entry

# 3. tag and push
git add -A; git commit -m "imm-unity 0.2.0"
git tag imm-unity-v0.2.0
git push origin vr-main --tags

# 4. build the drop-in artifact and attach it to the release
tools/build_unitypackage.ps1            # Editor must be closed for batch mode
gh release create imm-unity-v0.2.0 `
  code/ImmUnitySampleProject/dist/IMM-Unity-0.2.0.unitypackage `
  --title "IMM Unity 0.2.0" --notes-file <notes>
```

(`gh` is not installed on this machine yet — until it is, create the release
and upload the `.unitypackage` through the GitHub web UI.)

That covers all four consumption modes at once: tag for git URLs, artifact for
drag-and-drop, and the committed package for local-path users.

Consumers on a pinned tag then move by editing their `manifest.json`; consumers
tracking `#vr-main` pick it up on their next resolve (Unity caches git packages —
`Packages/packages-lock.json` pins the resolved commit, so delete that entry or
use *Packages → Resolve* to force an update).

Version discipline while pre-1.0: bump the minor for additive API, the patch for
fixes and rebuilt binaries.

## Reducing clone cost (when it starts to hurt)

The repo is large mainly because every rebuilt native binary is a new blob in
history. Options, cheapest first:

1. **Ship binaries as release artifacts** instead of committing them — the
   package stays source-only and a build step or a release `.tgz` supplies the
   natives. Biggest win, some workflow change.
2. **Git LFS for `Plugins/**`** (`git lfs migrate import --include="*.so,*.a,*.dll,*.bundle/**"`).
   Rewrites history, so every existing clone must be re-cloned — coordinate before doing it.
3. **Split the packages into their own repo** and consume this one as the
   development home. Cleanest long-term, most work now.

None of these are urgent at ~275 MB; revisit if resolve times become annoying.
