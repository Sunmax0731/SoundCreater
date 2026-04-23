# Torus Edison Sample Projects

This package currently ships with two bundled sample projects.

## Basic SE

File:

- `BasicSE/basic-se.gats.json`

Purpose:

- confirm that one-shot preview playback is audible
- confirm that short effect rendering produces a non-silent export
- confirm that serializer load succeeds without fallback warnings

Expected sound:

- a short impact-style sound with a secondary hit and light delay tail

## Simple Loop

File:

- `SimpleLoop/simple-loop.gats.json`

Purpose:

- confirm loop playback over multiple bars
- confirm multi-track render and export behavior
- confirm that lead and chord content survive save, load, preview, and export

Expected sound:

- a 4-bar loop with repeating lead notes and supporting chord tones

## Recommended Verification Flow

1. Open `Tools/Torus Edison/Open Editor`
2. Load `Basic SE`
3. Render preview and play it
4. Export it as WAV
5. Load `Simple Loop`
6. Render preview and play it with loop enabled
7. Export it as WAV

For the full acceptance checklist, see:

- `Documentation~/ValidationChecklist.md`

