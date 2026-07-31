# Local browser access

- Chrome is installed and may be launched directly whenever local browser access is useful.
- An unavailable in-app browser connection does not mean that a local browser is unavailable; use the project's Chrome/Playwright harness or launch visible Chrome instead.
- Never describe browser or visual verification as unavailable solely because the in-app automation backend is unavailable.
- Prefer visible Chrome when the user wants to watch browser testing.

# Git synchronization

- Always pull before pushing because CI may have committed replacement binaries upstream.
- CI replacement-binary commits are always safe to merge.
- Avoid stashing when a pull can safely merge without it. If a stash or autostash is necessary, inventory the dirty files first and verify that the same contents are restored immediately afterward; never leave the user's changes stranded in a stash.

# Sensitive information

This fork is public. Everything committed here is published, and rewriting
history after a push does not un-publish it — so the check belongs *before* the
commit, not before the push.

**Never commit:**

- **Studio or client content.** `.imm` documents, Quill project folders, `.qbin`
  payloads, source art, audio stems, video. The only documents in the repository
  are the upstream `sample1.imm` fixtures. Keep working documents outside the
  repo or under an ignored path, and verify with `git check-ignore -v <path>`.
- **Logs and capture output.** Device logs, `logcat` dumps, screen captures,
  profiler traces, build logs, session transcripts. These belong in the untracked
  `captures/` directory beside the repo, never inside it.
- **Named clients, collaborators, or projects that are not ours to name.** Refer
  to a role instead — "the author", "the reference project". A film's title may
  appear where it is genuinely load-bearing for a bug report; the names of the
  people who made it may not.
- **Machine and network identity.** Local absolute paths, usernames, LAN
  addresses, device serials, API keys. Take the headset address from
  `IMM_HEADSET_SERIAL` rather than writing it into a script (see `tools/`).

**Keep, deliberately:** the technical narrative — root causes, the wrong turns,
the measurements, the tool that cracked it. That record is the point of these
documents, and sanitizing it into vagueness costs more than it protects. Strip
the identifying detail, keep the engineering.

**Asset filenames** sit on the line. Keep one when the defect is about that
specific asset (`littledome3.png` is 16:9 in a 2:1 mapping). Drop it when it only
reveals how a client's project is organised (audio stem names, folder layouts).

Before committing documentation or tooling, scan the staged diff:

```sh
git diff --cached | grep -niE "[A-Z]:\\\\|192\.168|10\.[0-9]+\.[0-9]+\.|/Users/|\.imm\b|\.qbin\b"
```

# Critical blockers and goal continuation

- If continued progress depends on an important user approval, decision, or other blocker, never mention it only once in a live update and then continue as though it was communicated.
- Either stop work and make the blocker plus the exact response needed the terminal response, or continue useful work while repeating the blocker and exact response needed on every line of every user-visible update until the user answers, so it cannot scroll past unnoticed.
- Treat the blocked part of the goal as paused until the user explicitly answers. Do not claim that the user failed to answer a request that appeared only in an earlier, potentially scrolled-off update.
