# Reclatch

A lightweight screen recorder for Windows.

No scene graph, no streaming stack, no watermark, no account. Reclatch does one thing:
it records your screen and hands you the file.

## Status

Planning. There is nothing to install yet.

## Scope for v1

**Capture** — full screen, a single window, or a selected region. Multiple monitors with
an explicit monitor picker. Cursor shown or hidden, with optional click highlighting.
Selectable frame rate and output scale.

**Audio** — system audio via WASAPI loopback and microphone input, each toggled
independently, each with a level meter. Optionally written as separate tracks so the
recording stays editable.

**Encoding** — hardware encoding through NVENC, QuickSync or AMF, with a software
fallback when none is available. H.264 in MP4 by default, targeted by quality or bitrate.

**Control** — global hotkeys for start, stop and pause. A live status readout showing
elapsed time, file size and dropped frames. Countdown before recording and minimise to
tray. Recordings survive a crash: the file on disk stays playable.

**Housekeeping** — free space check before and during a recording, a filename template,
persisted settings, and a jump to the finished file when recording ends.

## Not in v1

Webcam overlay, scene and source compositing, live streaming, in-game overlay,
annotation, GIF export, scheduled recording, replay buffer.

## License

MIT
