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
- `Samples/BasicSE.gats.json`
- `Samples/SimpleLoop.gats.json`
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

Optional parameters:

- `-UnityPath` to point at a specific Unity executable
- `-ProjectPath` to point at a specific host project
- `-OutputRoot` to change the artifact output directory
- `-SkipUnityPackage` to rebuild the zip contents without calling Unity

## Release Checklist

Before attaching the zip to GitHub Release:

1. Confirm the package version in `Packages/com.sunmax.trusedison/package.json`
2. Confirm `README`, manuals, terms, release notes, and changelog match the current implementation
3. Run the validation checklist in `Packages/com.sunmax.trusedison/Documentation~/ValidationChecklist.md`
4. Run `tools/release/build-release.ps1`
5. Confirm the unitypackage and zip were both created
6. Open the zip and verify the expected files are present
7. Keep the GitHub Release artifact and BOOTH artifact identical
8. Use `GitHubReleaseBody.ja.md` for the GitHub Release body
9. Use `BOOTHDescription.ja.md` for the BOOTH listing text
