# Documentation

The repository root holds only the entry points a newcomer needs — `README.md`,
`BUILDING.md`, `PACKAGING.md`, `CONTRIBUTING.md`. Everything else lives here.

## Where to look

| Folder | What is in it |
|---|---|
| [`status/`](status/) | **Living documents.** The current state of a work stream, kept up to date. Start here to pick up where a session left off. |
| [`architecture/`](architecture/) | How the system is built: file format, core libraries, playback engine, engine integrations, build system, audio. Numbered for reading in order — see its [README](architecture/README.md). |
| [`plans/`](plans/) | Forward-looking designs and contracts. A plan describes work that is proposed or in progress; once it ships, the outcome moves into `status/` or `architecture/`. |
| [`history/`](history/) | Superseded notes, past session records, and completed fix reports. Kept for the reasoning, not as current guidance — **do not treat anything here as describing the code today.** |
| [`android/`](android/) | Android build, deployment, and log-collection guides. |

## The status documents

| Document | Covers |
|---|---|
| [`status/QUEST_VULKAN_STATUS.md`](status/QUEST_VULKAN_STATUS.md) | The Quest Vulkan renderer — what is verified on device, the flag table, and next work. Its top section is the session opener. |
| [`status/AUDIO_STATUS.md`](status/AUDIO_STATUS.md) | Spatial audio: the portable spatializer, host mixer, and platform backends. |
| [`status/QUILL_UNITY_STATUS.md`](status/QUILL_UNITY_STATUS.md) | The direct Quill-project importer and timeline player for Unity. |

## Conventions

- A status document's opening section is always the "start here" block; keep it
  current, and let the detail below it accumulate as history.
- Prefer correcting a document in place over adding a newer one that contradicts
  it. When a conclusion is overturned, say so explicitly and keep the wrong turn
  described — the route to a diagnosis is usually worth more than the diagnosis.
- No studio-sensitive material in any document. See the "Sensitive information"
  section of [`../AGENTS.md`](../AGENTS.md) for what that means in practice.
