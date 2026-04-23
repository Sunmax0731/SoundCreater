# SoundCreater

`SoundCreater` is the repository name.

The public-facing tool name is `Torus Edison`.

`Torus Edison` is a Unity Editor extension for building reusable game-audio project data, preview workflows, and release artifacts for later GitHub Release / BOOTH distribution.

This repository now also contains the Unity host project used to validate the embedded package locally.

## Naming

- Repository: `SoundCreater`
- Tool name: `Torus Edison`

The tool name is a wordplay on Thomas Edison, reframed as a sound-invention tool.

## Current State

Current foundation in this repository:

- Unity package skeleton under `Packages/com.example.gameaudiotool`
- domain models for project, track, note, voice, ADSR, effect, and config
- `.gats.json` save/load foundation with validation
- common/project config serializers
- deterministic offline audio rendering core for waveform, noise, ADSR, delay, and mixdown
- editor preview playback with Play / Stop / Rewind / Loop transport controls
- sample session files and starter release documentation

Not implemented yet:

- timeline editing UI
- WAV export
- Undo / Redo command history

## Open The Tool

Open the repository root as a Unity project, wait for package import to complete, then open:

- `Tools/Torus Edison/Open Editor`

## Repository Layout

- `Packages/com.example.gameaudiotool`
  Main Unity package under development.
- `Assets`, `Packages/manifest.json`, `ProjectSettings`
  Unity host project files used for local validation of the embedded package.
- `game-audio-tool-docs/game-audio-tool-docs`
  Requirements, specification, skill, and agent guidance used as implementation references.

## Planning Docs

Reference documents:

- [requirements-definition-v0.3.md](game-audio-tool-docs/game-audio-tool-docs/requirements-definition-v0.3.md)
- [specification-v0.1.md](game-audio-tool-docs/game-audio-tool-docs/specification-v0.1.md)
- [Skill.md](game-audio-tool-docs/game-audio-tool-docs/Skill.md)
- [Agents.md](game-audio-tool-docs/game-audio-tool-docs/Agents.md)

## Release Direction

The repository is intended to ship:

- GitHub Release assets
- a BOOTH listing using the public tool name `Torus Edison`

Those release tasks are tracked in GitHub Issues.
