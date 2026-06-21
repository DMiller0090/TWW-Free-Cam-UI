# TWW Free Cam UI

A Windows tool for scripting sequenced free-camera movements in **The Legend of Zelda: The Wind Waker** and **Twilight Princess** running under the [Dolphin](https://dolphin-emu.org/) emulator.

You build a timeline of camera "shots" — each with a start/end camera position, a start/end focus point, a duration (in frames), and an interpolation curve. The tool attaches to a running Dolphin process and writes the camera and focus coordinates into game memory each frame, producing smooth, repeatable camera moves.

## Use with TAS playbacks

This tool is especially useful for adding custom free-cam shots over **TAS (tool-assisted speedrun) playbacks**. Because the camera sequence runs on its own timer alongside the movie, it is **not perfectly frame-deterministic** — desyncs between the script and the playback are common. In practice it still works well, particularly when combined with **video splicing** to stitch together clean takes.

## Features

- Timeline of camera shots with per-row:
  - Start/end camera position (X, Y, Z)
  - Start/end focus position (X, Y, Z)
  - Duration in **frames** or **speed** (the two are kept in sync automatically from the move distance)
  - Interpolation type: **Linear**, **Ease In/Out**, or **Cubic Bezier** (with tunable control points)
  - Optional **actor lock** — focus values are treated as offsets from a selected in-game actor's coordinates
- Save and load scripts as JSON (see the [`examples/`](examples) folder)
- Live attach to a running Dolphin process

## Supported games / versions

Memory editors are included for:

- **The Wind Waker** — JP
- **Twilight Princess** — USA (Eng), JP, and PAL

## Requirements

- Windows
- [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
- Dolphin emulator running one of the supported game versions

## Building

```sh
dotnet build TWW-Free-Cam-UI.sln -c Release
```

Or open `TWW-Free-Cam-UI.sln` in Visual Studio 2022+ and build (the project targets `net9.0-windows` with WPF).

## Usage

1. Launch your supported game in Dolphin.
2. Run TWW Free Cam UI and attach to the Dolphin process.
3. Build a camera script row by row (or load one of the example JSON scripts).
4. Run the sequence. For TAS work, start playback and the camera script together, then splice as needed to clean up any desyncs.

## Disclaimer

This is a community tool for emulator-based content creation and TAS production. It reads and writes the memory of a running Dolphin process; addresses are version-specific, so use the matching game version.
