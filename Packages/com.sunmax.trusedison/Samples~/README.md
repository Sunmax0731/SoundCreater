# Torus Edison Sample Projects

This package now ships with ten bundled sample projects.

`Create Samples` copies all of them into `myAudioProjects/` in the Unity host project.
The File page still provides quick-load buttons for `Basic SE` and `Simple Loop`, while the rest can be opened from the copied sample folder.

## Core Validation Samples

### Basic SE

File:

- `BasicSE/basic-se.gats.json`

Purpose:

- confirm that one-shot preview playback is audible
- confirm that short effect rendering produces a non-silent export
- confirm that serializer load succeeds without fallback warnings

Expected sound:

- a short impact-style sound with a secondary hit and light delay tail

### Simple Loop

File:

- `SimpleLoop/simple-loop.gats.json`

Purpose:

- confirm loop playback over multiple bars
- confirm multi-track render and export behavior
- confirm that lead and chord content survive save, load, preview, and export

Expected sound:

- a 4-bar loop with repeating lead notes and supporting chord tones

## UI Sound Variations

### UI Click

File:

- `UIClick/ui-click.gats.json`

Expected sound:

- a very short, bright menu click

### UI Confirm

File:

- `UIConfirm/ui-confirm.gats.json`

Expected sound:

- a short upward confirmation chime with a light sparkle layer

### UI Cancel

File:

- `UICancel/ui-cancel.gats.json`

Expected sound:

- a short downward cancel sound with a slightly rough edge

### Coin Pickup

File:

- `CoinPickup/coin-pickup.gats.json`

Expected sound:

- a bright pickup chime with a small bell-like tail

## Gameplay Effect Variations

### Power Up Rise

File:

- `PowerUpRise/power-up-rise.gats.json`

Expected sound:

- a short rising effect suited for unlock or power-up moments

### Laser Shot

File:

- `LaserShot/laser-shot.gats.json`

Expected sound:

- a sharp retro laser burst

### Explosion Burst

File:

- `ExplosionBurst/explosion-burst.gats.json`

Expected sound:

- a layered burst with low body and noisy impact

## Loop Variation

### Alarm Loop

File:

- `AlarmLoop/alarm-loop.gats.json`

Expected sound:

- a repeating two-tone warning loop across two bars

## Recommended Verification Flow

1. Open `Tools/Torus Edison/Open Editor`
2. Click `Create Samples`
3. Load `Basic SE`
4. Render preview and play it
5. Export it as WAV
6. Load `Simple Loop`
7. Render preview and play it with loop enabled
8. Open the local sample folder and try several of the additional sample projects

For the full acceptance checklist, see:

- `Documentation~/ValidationChecklist.md`
