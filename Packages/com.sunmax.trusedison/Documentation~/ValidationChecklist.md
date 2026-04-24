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
- config serializer tests under `Tests/Editor/GameAudioConfigSerializerTests.cs`
- render tests under `Tests/Editor/GameAudioProjectRendererTests.cs`
- undo / redo tests under `Tests/Editor/GameAudioCommandHistoryTests.cs`
- export tests under `Tests/Editor/GameAudioExportUtilityTests.cs`
- diagnostic logger tests under `Tests/Editor/GameAudioDiagnosticLoggerTests.cs`
- acceptance-scenario tests under `Tests/Editor/GameAudioAcceptanceScenarioTests.cs`

JSON compatibility coverage must include:

- same-major `1.x.x` project files that omit optional fields
- fallback warnings for rebuilt optional voice/effect state
- rejection of unsupported major versions
- rejection of known optional fields when the field is present with the wrong JSON type

Run the repository-standard automated check from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools\validation\run-editmode-tests.ps1
```

This script starts Unity in batchmode, waits for the spawned Unity process to exit, and fails if:

- Unity returns a non-zero exit code
- the EditMode result XML is missing or empty
- no tests were executed
- the result XML reports failed tests

Do not add `-quit` to ad-hoc `-runTests` commands. The Unity Test Framework command-line runner exits the editor after the test run has finished, and `-quit` can terminate before result XML is written.

## Bundled Samples

The package now includes ten bundled sample projects.
Release validation should at minimum use the two core samples below.

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

Keyboard shortcut check:

- `Space` starts preview playback when the editor canvas has focus
- `Space` pauses playback while preview is playing
- `Space` does not trigger preview playback while editing a text or numeric field

### 3. Timeline Editing

1. Go to the `Edit` page
2. Create a note on an empty lane
3. Move the note
4. Resize the note
5. Add a track with `+ Add Track`
6. Duplicate the note with `Ctrl+D`
7. Delete one note
8. Undo and redo the changes

Expected:

- note creation, move, resize, duplicate, delete, and track add all work
- selection state remains consistent
- undo / redo returns the project to the expected state

### 4. File Shortcuts

1. Focus the editor canvas outside input fields.
2. Press `Ctrl+S`.
3. Press `Ctrl+Shift+S`.
4. Press `Ctrl+O`.
5. Press `Ctrl+N`.
6. Repeat while a text or numeric field is focused.

Expected:

- file shortcuts invoke the same actions as the toolbar buttons when the editor canvas has focus
- text and numeric field editing is not interrupted by global shortcuts

### 5. Inspector Editing

1. Select a note and change pitch or velocity
2. Select a track and change volume or pan
3. Change project BPM or Total Bars
4. Re-render preview and confirm the change is reflected

Expected:

- inspector values update the active selection
- changes affect playback after render
- invalid values are clamped safely

### 6. WAV Export

1. Go to the `Export` page
2. Export `Basic SE`
3. Export `Simple Loop`
4. Confirm both files are written
5. If exporting under `Assets/`, confirm the file appears in the Project window
6. Confirm Export Quality shows peak, project duration, output duration, and tail duration
7. Confirm a no-note or muted project shows a silent-buffer warning
8. Confirm an over-gained project shows clipping risk when Normalize Export is off
9. Enable Normalize Export with headroom and confirm the quality line reports normalize gain and a lower output peak

Expected:

- export succeeds without exception
- exported files are valid `.wav`
- exported files are non-empty
- `Assets/` export refresh behavior is correct
- quality warnings make silent, low peak, and clipping-risk exports visible before distribution
- normalize remains an export option and does not alter the project or Undo history

### 7. Localization And Diagnostics

1. Go to the `Settings` page
2. Change UI language mode between `Auto`, `Japanese`, `English`, and `Chinese`
3. Confirm major labels and buttons update immediately
4. Confirm the Settings inspector remains visible after each language change
5. Enable diagnostic mode
6. Raise log level to `Verbose`
7. Load a sample or render preview
8. Confirm diagnostic output appears in the Unity Console

Expected:

- language changes apply without reopening the window
- the Settings page remains visible after each change
- Auto follows the editor or system language fallback
- diagnostic logs respect the selected log level
- disabling diagnostic mode suppresses routine info logs

## Known Limitations

- Live mouse interaction feel still requires manual confirmation in Unity.
- Export behavior under `Assets/` should still be spot-checked in a real editor session.
- `-testResults` XML should be inspected only after the spawned Unity process exits; use `tools\validation\run-editmode-tests.ps1` for the standard flow.
- End-user generated files such as local projects, exports, logs, and temp files are intentionally ignored by git.

## Release Gate

Before calling the package release-ready, confirm all of the following:

- automated EditMode tests pass
- both bundled samples load successfully
- both bundled samples preview successfully
- both bundled samples export successfully
- timeline and inspector editing still behave correctly
- README, manual, terms, and release notes reflect the current implementation
