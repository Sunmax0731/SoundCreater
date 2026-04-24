# Torus Edison Manual

Japanese version: [Manual.ja.md](Manual.ja.md)

Related documents:

- [TermsOfUse.md](TermsOfUse.md)
- [ReleaseNotes.md](ReleaseNotes.md)
- [ValidationChecklist.md](ValidationChecklist.md)

## Overview

`Torus Edison` is a Unity Editor extension for sketching short game sound effects and loopable audio ideas directly inside a Unity project.
It stores project data as `.gats.json` files and supports editing, previewing, and exporting without leaving the Unity Editor.

Current implementation scope:

- `New`, `Open`, `Save`, and `Save As`
- `.gats.json` persistence
- timeline note editing
- note, track, and project inspector editing
- preview rendering and transport controls
- 16-bit WAV export
- 8-bit WAV conversion for imported `AudioClip` assets
- undo and redo
- bundled sample projects for validation

## Supported Environment

- Unity `6000.0` or newer
- Unity Editor workflows on Windows
- offline use

## Launch

Open the tool from:

- `Tools/Torus Edison/Open Editor`
- `Tools/Torus Edison/Version & License`

## Workspace Pages

The current editor window is split into four top tabs.

### File

Use this page for:

- checking the current project path and status
- creating a new project
- opening and saving `.gats.json` files
- creating local sample copies
- quick-loading bundled `Basic SE` and `Simple Loop` samples
- copying the full ten-project sample set with `Create Samples`

### Edit

Use this page for editing and preview work in one place.

- preview rendering and transport controls
- rendered preview waveform display
- project lengths up to `128 bars`
- creating notes on the timeline
- moving notes across beats and tracks
- resizing notes
- duplicating and deleting selected notes
- adding tracks from the footer `+ Add Track` button
- editing note, track, and project values from the inspector
- opening the timeline help window from `?`

Current shortcuts:

- `Ctrl+N` new project
- `Ctrl+O` open project
- `Ctrl+S` save project
- `Ctrl+Shift+S` save as
- `Space` play or pause preview
- `Ctrl+D` duplicate selected notes
- `Delete` remove selected notes
- `Ctrl+Z` undo
- `Ctrl+Y` redo

### Export

Use this page for WAV export and imported `AudioClip` conversion.

Available controls:

- `Export WAV`
- imported `AudioClip` to 8-bit WAV conversion
- automatic generation of a conversion-focused `.gats.json` project next to the converted WAV
- `Open Export Folder`
- common default folder
- project override folder
- auto refresh toggle for `Assets/` exports

### Settings

Use this page for project-level and tool-level settings.

- BPM
- Total Bars
- current project Sample Rate
- current project Channel Mode
- new-project Sample Rate Override
- new-project Channel Mode Override
- UI language mode
- startup guide display toggle
- remember-last-project toggle and last remembered project path
- diagnostic mode and log level
- current foundation diagnostics
- validation warnings from loaded project data

## Save And Load

- `New` creates a fresh project shell.
- `Open` reads `.gats.json` project files and rejects broken schema or unsupported format versions.
- `Save` writes the current project to disk.
- `Save As` normalizes the selected path to the `.gats.json` session extension.

The canonical session format is `.gats.json`.
When remember-last-project is enabled, Torus Edison restores the last saved or opened `.gats.json` file the next time the editor window starts. If the remembered file no longer exists, the stored path is cleared and a new project is created instead.

## Timeline Editing

Current editing support includes:

- note creation by dragging on an empty lane
- note move by dragging an existing note
- note resize by dragging note edges
- multi-selection aware note changes
- inspector-driven edits for pitch, velocity, and related values
- track creation from the timeline footer
- project length edits from the Edit page `Bars` field
- undo and redo for major edit operations

## Preview Playback

Preview rendering builds an offline audio buffer from the current project and plays it back through the Unity Editor preview path.

Current implementation includes:

- waveform and white-noise rendering
- ADSR and delay support
- track and project mixdown
- preview waveform display
- `Render Preview`, `Play`, `Pause`, `Stop`, `Rewind`, and `Loop`

## WAV Export

Current WAV export behavior:

- exports 16-bit PCM `.wav`
- supports `48000` and `44100` Hz
- supports mono and stereo output
- sanitizes file names
- creates export folders if needed
- refreshes `AssetDatabase` when exporting under `Assets/`
- reports export quality after each export: output peak, source peak, project length, output length, tail length, normalize status, and the delta from the previous export in the session
- warns when the rendered buffer is silent, very quiet, or at clipping risk
- can optionally normalize the exported WAV with a configurable headroom value; this is an export-only option and does not modify the project or Undo history

## 8-bit WAV Conversion

The Export page also includes a conversion flow for imported `AudioClip` assets.

- select a source `AudioClip` that already exists in the Unity project
- choose the output name
- choose the target sample rate
- choose `Preserve Source`, `Mono`, or `Stereo`
- export as 8-bit PCM `.wav`
- automatically generate a conversion `.gats.json` project in the same folder

This feature converts audio that you already brought into Unity. It does not download or extract audio from YouTube or other external services.

## Version And License Window

Use `Tools/Torus Edison/Version & License` to check the current package version, the license entry point, and the release source.

## Configuration Files

Current configuration files:

- common settings: `%LocalAppData%/GameAudioTool/config.json`
- project settings: `ProjectSettings/GameAudioToolSettings.json`

Common settings store the startup guide flag, remember-last-project flag, last remembered project path, default export directory, language, diagnostics, and baseline audio defaults.
Project settings override common defaults for sample rate, channel mode, export directory, and auto-refresh behavior.
The sample rate and channel mode overrides are used when `New` creates a project. Existing `.gats.json` files keep the sample rate and channel mode stored in the project file.

## Project File Compatibility

`.gats.json` loading accepts `formatVersion` values in the supported major line, currently `1.x.x`.

Compatibility rules:

- `formatVersion`, the `project` object, and `project.name` are required
- different major versions are rejected
- unknown fields are ignored
- known fields are type-checked when present
- missing optional fields use domain defaults and warnings when the fallback changes project behavior
- `defaultVoice` and delay settings can be omitted in older `1.x` files and are rebuilt from current defaults

## Samples

Bundled samples:

- `BasicSE/basic-se.gats.json`
- `SimpleLoop/simple-loop.gats.json`
- `UIClick/ui-click.gats.json`
- `UIConfirm/ui-confirm.gats.json`
- `UICancel/ui-cancel.gats.json`
- `CoinPickup/coin-pickup.gats.json`
- `PowerUpRise/power-up-rise.gats.json`
- `LaserShot/laser-shot.gats.json`
- `ExplosionBurst/explosion-burst.gats.json`
- `AlarmLoop/alarm-loop.gats.json`

Sample notes and validation steps:

- [Samples~/README.md](../Samples~/README.md)
- [ValidationChecklist.md](ValidationChecklist.md)

## Known Limitations

- live mouse interaction feel still benefits from confirmation in a real Unity Editor session
- export behavior under `Assets/` should still be spot-checked in Unity
- UI localization currently supports Japanese, English, and Chinese only
- diagnostic logging is intended for local support work through the Unity Console

## Current Notes

This manual reflects the current implementation in the repository and is intended to stay aligned with the package as it evolves.
