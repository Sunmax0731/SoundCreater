# Torus Edison Manual

## Overview

`Torus Edison` is a Unity Editor extension for authoring reusable game-audio project data.

Current implementation scope:

- open the editor from `Tools/Torus Edison/Open Editor`
- create a new project shell
- save and load `.gats.json` project files
- keep package samples and configuration foundations in place

Not implemented yet in this package revision:

- timeline note editing UI
- audio preview playback
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
- `Open` reads `.json` project files and applies validation/fallback rules.
- `Save` writes the current project to disk.
- `Save As` stores the project under a new file path.

The canonical session format is `.gats.json`.

## Samples

Included samples:

- `Samples~/BasicSE/basic-se.gats.json`
- `Samples~/SimpleLoop/simple-loop.gats.json`

These are intended for serializer validation and future playback/export checks.

## Current Notes

This manual reflects the current package foundation, not the full MVP in the specification documents.
As timeline, playback, export, and command systems are added, this manual should be expanded alongside the implementation.
