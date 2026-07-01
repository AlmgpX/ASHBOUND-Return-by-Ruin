# OUT NeuroSlop Beat Forge

C#/.NET 8 command line tool for making rhythm-aware FFmpeg videos from one audio file and a folder of generated images.

The tool produces FullHD MP4 by default. It uses FFprobe to read audio duration, FFmpeg to decode audio into PCM, C# to detect energy/onset peaks, and FFmpeg again to render the final video.

## Requirements

- .NET 8 SDK
- FFmpeg and FFprobe in PATH, or explicit paths passed with `--ffmpeg` and `--ffprobe`

## Basic usage

```bash
dotnet run --project tools/OUT_NeuroSlopBeatForge -- \
  --audio Content/music.mp3 \
  --images Content/generated_images \
  --out Output/video.mp4 \
  --text "ТЕНЕВОЙ ПРОГНОЗ НА СЕГОДНЯ" \
  --font-file C:/Windows/Fonts/arial.ttf \
  --shuffle
```

## Options

```text
--audio <file>        Audio file.
--images <folder>    Folder with png, jpg, jpeg, webp or bmp images.
--out <file>         Output mp4 path.
--ffmpeg <file>      FFmpeg executable. Default: ffmpeg.
--ffprobe <file>     FFprobe executable. Default: ffprobe.
--width <int>        Output width. Default: 1920.
--height <int>       Output height. Default: 1080.
--fps <int>          Output frame rate. Default: 30.
--threshold <float>  Beat sensitivity. Lower means more cuts. Default: 1.35.
--min-gap <float>    Minimum seconds between cuts. Default: 0.22.
--fallback-cut <s>   Fixed cut length if beat detection is weak. Default: 0.75.
--motion <float>     Movement overscale, from 1.0 to 1.5. Default: 1.10.
--text <text>        Draw UTF-8 text over video.
--text-file <file>   Draw text from UTF-8 file.
--font-file <file>   Font file for FFmpeg drawtext. Recommended for Cyrillic.
--font-size <int>    Text size. Default: 54.
--shuffle            Shuffle image order.
--seed <int>         Stable shuffle seed.
--duration <float>   Override audio duration in seconds.
--dry-run            Print FFmpeg command without rendering.
--keep-temp          Keep temporary concat/text files.
```

## Rhythm detection

1. FFprobe reads duration.
2. FFmpeg decodes audio to mono 8 kHz signed 16-bit PCM.
3. The tool measures RMS energy in short windows.
4. Local positive-energy peaks become image-change points.
5. If too few peaks are found, fixed cuts are used.

## Notes

For Cyrillic text on Windows, pass a real font file:

```bash
--font-file C:/Windows/Fonts/arial.ttf
```
