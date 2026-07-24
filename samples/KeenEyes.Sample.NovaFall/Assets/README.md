# NOVAFALL Assets

All sounds under `Audio/` are procedurally generated (pure-math sine/noise
synthesis, mono 22050 Hz 16-bit WAV) by a one-off dev-time script — no recorded
material. They are released as **CC0 / public domain**.

| File | Role |
|------|------|
| `music-pad.wav` | Music stem: slow sine-chord pad (always audible) |
| `music-pulse.wav` | Music stem: filtered pulse arp (fades in at Flame tier) |
| `music-lead.wav` | Music stem: triangle lead motif (fades in at Plasma tier) |
| `wind-loop.wav` | Fall wind loop, pitch-mapped to fall speed |
| `graze-ting.wav` | Near-miss ting, pitch steps up per consecutive graze |
| `tier-up-swell.wav` | Heat tier promotion swell |
| `smash-crunch.wav` | Floor Smash crunch, pitch/volume scaled by impact speed |
| `crush-rumble.wav` | Furnace proximity rumble loop |
| `death-impact.wav` | Crush death impact |

The three music stems are exactly the same length and tempo (2 bars at 100 BPM)
so they can be started in sync and cross-faded by heat tier without drifting.
