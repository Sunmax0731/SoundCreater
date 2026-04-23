# Torus Edison Manual

## Overview

`Torus Edison` is a Unity Editor extension for authoring reusable game-audio project data.

Current implementation scope:

- open the editor from `Tools/Torus Edison/Open Editor`
- create a new project shell
- save and load `.gats.json` project files
- render project data into deterministic offline audio buffers
- preview those buffers in the Unity Editor with Play / Stop / Rewind / Loop transport controls
- keep package samples and configuration foundations in place

Not implemented yet in this package revision:

- timeline note editing UI
- WAV export
- Undo / Redo command history

## Installation

1. Place this package under `Packages/com.example.gameaudiotool`.
2. Open the Unity project.
3. Wait for package import to complete.

## Launch

Open the tool from:

- `Tools/Torus Edison/Open Editor`

## Save And Load

- `New` creates a fresh project shell with one default track.
- `Open` reads `.gats.json` project files and rejects broken schema or unsupported format versions.
- `Save` writes the current project to disk.
- `Save As` normalizes the selected path to the `.gats.json` session extension.

The canonical session format is `.gats.json`.

## Preview Playback

- `Render Preview` builds the current project into an `AudioClip`-backed editor preview buffer.
- `Play` starts preview playback from the beginning.
- `Stop` stops playback and returns the cursor to the start.
- `Rewind` resets the cursor without rebuilding the preview buffer.
- `Loop` is stored on the project and uses `Total Bars` as the loop length while one-shot playback still includes rendered effect tails.

Config file behavior in the current foundation:

- common settings: `%LocalAppData%/GameAudioTool/config.json`
- project settings: `ProjectSettings/GameAudioToolSettings.json`
- project settings override common defaults for sample rate, channel mode, and export directory
- malformed config JSON falls back to built-in defaults so the editor can still open

## Samples

Included samples:

- `Samples~/BasicSE/basic-se.gats.json`
- `Samples~/SimpleLoop/simple-loop.gats.json`

These are intended for serializer validation and future playback/export checks.

## Current Notes

This manual reflects the current package foundation, not the full MVP in the specification documents.
As timeline, playback, export, and command systems are added, this manual should be expanded alongside the implementation.
