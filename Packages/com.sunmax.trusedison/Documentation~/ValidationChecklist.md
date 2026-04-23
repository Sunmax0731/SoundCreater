# Torus Edison Validation Checklist

## Purpose

Use this checklist before merge, release packaging, and BOOTH upload work.

This document defines:

- the automated checks that should pass
- the manual checks that still need a live Unity Editor
- the expected use of bundled samples
- the currently known limitations

## Automated Checks

Recommended minimum automated checks:

1. Unity EditMode tests
2. JSON load and save round-trip tests
3. Renderer determinism and output-shape tests
4. Undo / Redo tests
5. WAV export tests
6. Bundled sample load / render / export tests

Current automated coverage in this package includes:

- serializer tests under `Tests/Editor/GameAudioProjectSerializerTests.cs`
- render tests under `Tests/Editor/GameAudioProjectRendererTests.cs`
- undo / redo tests under `Tests/Editor/GameAudioCommandHistoryTests.cs`
- export tests under `Tests/Editor/GameAudioExportUtilityTests.cs`
- acceptance-scenario tests under `Tests/Editor/GameAudioAcceptanceScenarioTests.cs`

## Bundled Samples

Release validation should use both bundled samples.

### Basic SE

Path:

- `Samples~/BasicSE/basic-se.gats.json`

Expected use:

- quick one-shot preview confirmation
- short WAV export confirmation
- non-silent render buffer confirmation

Expected sound:

- a short impact-like sound with a small secondary tail

### Simple Loop

Path:

- `Samples~/SimpleLoop/simple-loop.gats.json`

Expected use:

- loop playback confirmation
- multi-track mix confirmation
- longer WAV export confirmation

Expected sound:

- a 4-bar loop with lead notes across all bars and supporting chord tones

## Manual Checks In Unity

These checks still need a live Unity Editor session.

### 1. Open And Load

1. Open the Unity host project.
2. Open `Tools/Torus Edison/Open Editor`.
3. Load `Basic SE`.
4. Load `Simple Loop`.

Expected:

- both samples load without error dialogs
- Foundation Status does not show schema-breaking errors

### 2. Preview Playback

1. Load `Basic SE`
2. Click `Render Preview`
3. Click `Play`
4. Confirm the sound is audible
5. Click `Stop`

Then:

1. Load `Simple Loop`
2. Confirm `Loop` is enabled
3. Click `Render Preview`
4. Click `Play`
5. Click `Pause`
6. Click `Play` again to resume
7. Click `Rewind`
8. Click `Play`

Expected:

- preview is audible
- pause resumes from the held position
- rewind returns playback to the start
- loop playback continues over the 4-bar sample

### 3. Timeline Editing

1. Go to the `Edit` page
2. Create a note on an empty lane
3. Move the note
4. Resize the note
5. Duplicate the note with `Ctrl+D`
6. Delete one note
7. Undo and redo the changes

Expected:

- note creation, move, resize, duplicate, delete all work
- selection state remains consistent
- undo / redo returns the project to the expected state

### 4. Inspector Editing

1. Select a note and change pitch or velocity
2. Select a track and change volume or pan
3. Change project BPM or Total Bars
4. Re-render preview and confirm the change is reflected

Expected:

- inspector values update the active selection
- changes affect playback after render
- invalid values are clamped safely

### 5. WAV Export

1. Go to the `Export` page
2. Export `Basic SE`
3. Export `Simple Loop`
4. Confirm both files are written
5. If exporting under `Assets/`, confirm the file appears in the Project window

Expected:

- export succeeds without exception
- exported files are valid `.wav`
- exported files are non-empty
- `Assets/` export refresh behavior is correct

## Known Limitations

- Live mouse interaction feel still requires manual confirmation in Unity.
- Export behavior under `Assets/` should still be spot-checked in a real editor session.
- In the current validation environment, Unity batch `-runTests -testResults ...` may exit with code `0` without emitting the expected XML file.
- End-user generated files such as local projects, exports, logs, and temp files are intentionally ignored by git.

## Release Gate

Before calling the package release-ready, confirm all of the following:

- automated EditMode tests pass
- both bundled samples load successfully
- both bundled samples preview successfully
- both bundled samples export successfully
- timeline and inspector editing still behave correctly
- README, manual, terms, and release notes reflect the current implementation
