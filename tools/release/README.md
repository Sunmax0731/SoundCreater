# Torus Edison Release Packaging

## Goal

This folder defines the repeatable flow for building the release artifacts that will later be attached to GitHub Release and mirrored to BOOTH.

## Fixed Artifact Names

The current release flow uses these names:

- unitypackage: `TorusEdison-<version>.unitypackage`
- release zip: `TorusEdison-<version>-release.zip`
- staging folder: `ReleaseBuilds/TorusEdison-<version>/`

## Release Zip Contents

The release zip is expected to contain:

- `README.md`
- `Manual.ja.md`
- `Manual.md`
- `TermsOfUse.md`
- `ReleaseNotes.md`
- `ValidationChecklist.md`
- `CHANGELOG.md`
- `LICENSE.md`
- `Samples/` containing every bundled `.gats.json` sample from `Packages/com.sunmax.trusedison/Samples~/`
- `TorusEdison-<version>.unitypackage`

## Publishing Text

Use these files as the source text when publishing:

- `GitHubReleaseBody.ja.md`
- `BOOTHDescription.ja.md`

## Build Command

Run from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools\release\build-release.ps1
```

`build-release.ps1` runs the repository-standard EditMode validation gate before creating release artifacts.

Optional parameters:

- `-UnityPath` to point at a specific Unity executable
- `-ProjectPath` to point at a specific host project
- `-OutputRoot` to change the artifact output directory
- `-SkipUnityPackage` to skip UnityPackage export; EditMode tests still call Unity unless `-SkipEditModeTests` is also set
- `-SkipEditModeTests` to skip the automated EditMode release gate for a packaging-only dry run

To run only the automated validation gate:

```powershell
powershell -ExecutionPolicy Bypass -File tools\validation\run-editmode-tests.ps1
```

The validation script does not pass `-quit` to Unity. The Unity Test Framework command-line runner exits the editor after the test run, and the script fails if the result XML is missing, empty, contains zero executed tests, or reports failures.

## Release Checklist

Before attaching the zip to GitHub Release:

1. Confirm the package version in `Packages/com.sunmax.trusedison/package.json`
2. Confirm `Editor/Utilities/GameAudioToolInfo.cs`, sample `.gats.json`, and document examples use the same release version
3. Confirm `README`, manuals, terms, release notes, changelog, and buyer-facing copy match the current implementation
4. Run `tools/release/build-release.ps1`; do not skip EditMode tests for release artifacts
5. Run the manual checks in `Packages/com.sunmax.trusedison/Documentation~/ValidationChecklist.md`
6. Confirm the unitypackage and zip were both created
7. Open the zip and verify the expected files are present
8. Keep the GitHub Release artifact and BOOTH artifact identical
9. Use `GitHubReleaseBody.ja.md` for the GitHub Release body
10. Use `BOOTHDescription.ja.md` for the BOOTH listing text
