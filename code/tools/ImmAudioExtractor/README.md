# IMM Audio Extractor Tool

A command-line utility for extracting audio information and audio files from IMM (Immersive Media) files.

## Overview

The IMM Audio Extractor allows you to:
1. **Extract detailed audio metadata** - Get information about all audio layers including 3D position, spatial properties, and audio format details
2. **Export audio files** - Convert and export audio from IMM files to standard formats (WAV, OGG, OPUS)

## Features

### Audio Information Extraction

For each audio layer in an IMM file, you can extract:

- **Layer name** - The name of the audio layer
- **3D Position** - World-space coordinates (x, y, z)
- **Direction & Up vectors** - Orientation of the audio source
- **Spatial Type**:
  - `Flat (Headlocked)` - Traditional stereo audio that follows the listener
  - `Ambisonic (360)` - 360-degree spatial audio
  - `Positional (3D)` - 3D positioned audio with distance attenuation
- **Attenuation Settings** (for Positional audio):
  - Type: None, Linear, or Logarithmic
  - Min/Max distance values
- **Audio Properties**:
  - Gain and Volume levels
  - Looping status
  - Number of channels (mono, stereo, etc.)
  - Sample rate (Hz)
  - Bit depth
  - Duration (seconds)

### Audio Export

Export audio from IMM files to standard formats:
- **WAV** - Uncompressed PCM audio
- **OGG** - Ogg Vorbis compressed audio
- **OPUS** - Opus compressed audio (128kbps bitrate)

## Building

### Prerequisites
- CMake 3.10 or higher
- C++17 compatible compiler
- IMM SDK libraries (libImmCore, libImmImporter)

### Build Instructions

```bash
# From the IMM repository root
mkdir build
cd build
cmake ..
cmake --build . --target ImmAudioExtractor
```

The executable will be built in the `build/code/tools/ImmAudioExtractor/` directory.

## Usage

### Display Audio Information

```bash
ImmAudioExtractor info <imm_file>
```

**Example:**
```bash
ImmAudioExtractor info myfile.imm
```

**Output:**
```
Found 3 audio layer(s)

=== Audio Layer: BackgroundMusic ===
Position: (0.000, 0.000, 0.000)
Direction: (0.000, 0.000, -1.000)
Up: (0.000, 1.000, 0.000)
Spatial Type: Flat (Headlocked)
Gain: 1.00
Volume: 0.80
Looping: Yes
Channels: 2
Sample Rate: 48000 Hz
Bit Depth: 16 bits
Duration: 120.50 seconds

=== Audio Layer: BirdChirping ===
Position: (5.230, 2.100, -3.450)
Direction: (0.000, 0.000, -1.000)
Up: (0.000, 1.000, 0.000)
Spatial Type: Positional (3D)
Attenuation: Linear (Min: 1.00, Max: 10.00)
Gain: 1.00
Volume: 0.60
Looping: Yes
Channels: 1
Sample Rate: 44100 Hz
Bit Depth: 16 bits
Duration: 5.20 seconds
```

### Export Audio Files

```bash
ImmAudioExtractor export <imm_file> <output_folder> <format>
```

**Supported formats:** `wav`, `ogg`, `opus`

**Examples:**
```bash
# Export all audio as WAV files
ImmAudioExtractor export myfile.imm ./audio_output wav

# Export all audio as OPUS files
ImmAudioExtractor export myfile.imm ./audio_output opus

# Export all audio as OGG files
ImmAudioExtractor export myfile.imm ./audio_output ogg
```

The tool will create one audio file per audio layer, named after the layer name.

## How Audio Works in IMM Files

### Audio Storage Format

Audio in IMM files is stored in one of three compressed formats:
- **WAV** - Uncompressed PCM
- **OGG** - Ogg Vorbis compression
- **OPUS** - Opus compression (most common for spatial audio)

### Audio Playback Architecture

The IMM Unity plugin uses **Facebook's Audio360 SDK** (TBE - Two Big Ears) for spatial audio playback:

1. **Audio is read** from the IMM binary file stream
2. **Stored in memory** as `piWav` structures
3. **Passed to Audio360 SDK** which creates `AudioObject` instances
4. **Audio360 SDK outputs directly** to the system audio device

**Important:** Audio playback **bypasses Unity's AudioSource system entirely**. The plugin uses its own independent audio engine for better spatial audio support and performance.

### 3D Audio Positioning

For `Positional (3D)` audio:
- Position is extracted from the layer's world transform
- Direction and up vectors define the audio source orientation
- Attenuation controls how volume decreases with distance
- Modifiers can add directional cones or frustums for focused audio

## Converting Audio Formats

### To Convert Audio in IMM Files

If you want to change the audio codec used in an IMM file:

1. **Extract the audio** using this tool:
   ```bash
   ImmAudioExtractor export original.imm ./temp_audio wav
   ```

2. **Convert the audio** using external tools (ffmpeg, Audacity, etc.)

3. **Re-import into your IMM authoring tool** (e.g., Quill) with the new audio files

### Programmatic Conversion

You can also use the IMM SDK directly to convert audio:

```cpp
#include "libImmCore/src/libWave/piWave.h"
#include "libImmCore/src/libWave/formats/piWaveWAV.h"
#include "libImmCore/src/libWave/formats/piWaveOPUS.h"

// Load WAV
piWav wav;
ReadWAVFromDisk(&wav, L"input.wav");

// Convert to OPUS
WriteOPUSToFile(L"output.opus", &wav, 128000); // 128kbps

// Clean up
wav.Deinit();
```

## Technical Details

### Audio Data Structure

Each audio layer contains:
- `piWav` structure with PCM audio data or compressed blob
- Spatial properties (type, attenuation, modifiers)
- Transform matrix for 3D positioning
- Playback properties (looping, gain, volume)

### Coordinate System

- IMM uses a right-handed coordinate system
- Position is in world space (after applying layer hierarchy transforms)
- Direction vector points in the -Z direction by default
- Up vector points in the +Y direction by default

## Troubleshooting

### "Failed to load IMM file"
- Ensure the IMM file path is correct
- Check that the file is a valid IMM format
- Verify you have read permissions

### "Failed to export audio"
- Ensure the output folder exists
- Check write permissions for the output folder
- Verify the audio format is supported (wav, ogg, opus)

### Missing audio layers
- Some IMM files may have audio layers that aren't loaded by default
- Check the IMM file was exported with audio included

## License

This tool is part of the IMM SDK. See the main repository LICENSE file for details.

