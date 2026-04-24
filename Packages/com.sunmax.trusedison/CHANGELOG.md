# Changelog

## 0.3.0

- added export duration controls for project length, fixed seconds, and auto-trim workflows
- added export tail include/cut behavior and export quality reporting for target length, output length, tail length, and normalization state
- added stereo voice spread controls with left/right detune and right-channel delay for stereo projects
- added direct grid division selection in the timeline toolbar, synchronized with snap, grid drawing, duplicate offset, and `defaultGridDivision`
- refreshed README, manuals, release notes, release helper text, BOOTH-facing copy, and validation checklist for the current release scope

## 0.2.0

- integrated preview controls and waveform confirmation into the Edit page
- added the timeline footer `+ Add Track` affordance
- added project-length controls from both the Edit page and the Settings page
- added imported `AudioClip` to 8-bit PCM WAV conversion in the Export page
- added automatic generation of a conversion `.gats.json` project alongside 8-bit WAV export
- expanded the bundled sample set to ten AudioProjects across UI, gameplay, and loop categories
- added the `Version & License` window
- fixed Settings inspector refresh after live language changes
- fixed timeline keyboard shortcut handling for duplicate, delete, undo, and redo
- fixed footer hit testing so the add-track area is no longer treated as a normal track row
- refreshed README, manuals, release notes, release helper text, and BOOTH-facing copy

## 0.1.1

- added UI localization with auto-follow and manual override for Japanese, English, and Chinese
- added diagnostic logging mode and configurable log levels for Console-based support work
- added a deterministic offline render core for waveform, noise, ADSR, delay, and track/project mixdown
- added renderer-focused editor tests for determinism, waveform coverage, tail extension, and unsafe parameter clamping
- added Unity Editor preview transport controls with Play, Pause, Stop, Rewind, loop playback, and cursor reporting
- refreshed the bundled `BasicSE` and `SimpleLoop` sample projects for playback and export checks
- added release-readiness validation notes, sample guidance, and acceptance-scenario editor tests
- refreshed README, manuals, release notes, terms, and license documents to match the current implementation
- added a UnityPackage exporter, release packaging script, and release artifact layout documentation

## 0.1.0

- created the initial Unity package structure
- implemented project/config models and JSON persistence
- added a minimal editor window shell
- added package samples and starter documentation
