# Torus Edison Manual

Japanese version: [Manual.ja.md](Manual.ja.md)

Related documents:

- [TermsOfUse.md](TermsOfUse.md)
- [ReleaseNotes.md](ReleaseNotes.md)
- [ValidationChecklist.md](ValidationChecklist.md)

## Overview

`Torus Edison` is a Unity Editor extension for creating, editing, previewing, saving, and exporting reusable game-audio project data.

Current implementation scope:

- file workflows through `New`, `Open`, `Save`, and `Save As`
- `.gats.json` project persistence
- timeline note editing
- note, track, and project inspector editing
- preview rendering and transport controls
- WAV export
- undo and redo
- bundled sample projects for validation

## Supported Environment

- Unity `6000.0` or newer
- Unity Editor workflows on Windows
- offline use

## Launch

Open the tool from:

- `Tools/Torus Edison/Open Editor`

## Workspace Pages

The current editor window is split into four top tabs.

### File

Use this page for:

- checking the current project path and basic status
- creating a new project
- opening or saving `.gats.json` files
- creating local sample copies
- loading bundled `Basic SE` and `Simple Loop` samples

### Edit

Use this page for:

- preview transport and render controls while editing
- rendered preview waveform display
- project lengths up to `128 bars`
- creating notes on the timeline
- moving notes across beats and tracks
- resizing notes
- duplicating and deleting selected notes
- editing note, track, and project values from the inspector

Current shortcuts:

- `Render Preview`, `Play`, `Pause`, `Stop`, `Rewind`, `Loop`
- `?` opens a modal help window for timeline gestures and shortcuts
- `Ctrl+D` duplicate selected notes
- `Delete` remove selected notes
- `Ctrl+Z` undo
- `Ctrl+Y` redo

### Export

Use this page for WAV export.

Available controls:

- `Export WAV`
- `Open Export Folder`
- common default folder
- project override folder
- auto refresh toggle for `Assets/` exports

### Settings

Use this page for:

- project-level values such as BPM, total bars, sample rate, and channel mode
- UI language mode
- diagnostic mode and log level
- current foundation diagnostics
- validation warnings from loaded project data

## Save And Load

- `New` creates a fresh project shell.
- `Open` reads `.gats.json` project files and rejects broken schema or unsupported format versions.
- `Save` writes the current project to disk.
- `Save As` normalizes the selected path to the `.gats.json` session extension.

The canonical session format is `.gats.json`.

## Editing Notes

Current editing support includes:

- note creation by dragging on an empty lane
- note move by dragging an existing note
- note resize by dragging note edges
- multi-selection aware note changes
- inspector-driven edits for pitch, velocity, and related values
- undo and redo for major edit operations

## Preview Playback

Preview rendering builds an offline audio buffer from the current project and plays it back through the Unity Editor preview path.

Current implementation includes:

- waveform and white-noise rendering
- ADSR and delay support
- track and project mixdown
- preview waveform display
- play, pause, stop, rewind, and loop transport

## WAV Export

Current WAV export behavior:

- exports 16-bit PCM `.wav`
- supports `48000` and `44100` Hz
- supports mono and stereo output
- sanitizes file names
- creates export folders if needed
- refreshes `AssetDatabase` when exporting under `Assets/`

## Configuration Files

Current configuration files:

- common settings: `%LocalAppData%/GameAudioTool/config.json`
- project settings: `ProjectSettings/GameAudioToolSettings.json`

Project settings override common defaults for sample rate, channel mode, export directory, and auto-refresh behavior.

## Samples

Bundled samples:

- `Samples~/BasicSE/basic-se.gats.json`
- `Samples~/SimpleLoop/simple-loop.gats.json`

Sample notes and validation steps:

- [Samples~/README.md](../Samples~/README.md)
- [ValidationChecklist.md](ValidationChecklist.md)

## Known Limitations

- live mouse interaction feel still needs real-editor confirmation
- export behavior under `Assets/` should still be spot-checked in Unity
- UI localization currently supports Japanese, English, and Chinese only
- diagnostic logging is intended for local support work through the Unity Console

## Current Notes

This manual reflects the current implementation in the repository. It is intended to stay aligned with the package rather than the original MVP planning state.
