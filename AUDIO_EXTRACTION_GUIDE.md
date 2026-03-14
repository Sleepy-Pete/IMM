# IMM Audio Extraction and Conversion Guide

## Overview

This guide explains how audio works in IMM files and how to extract and convert audio data.

## How Audio Works in IMM Files

### Audio Storage

Audio in IMM files is stored as binary data in one of three formats:
- **WAV** - Uncompressed PCM audio
- **OGG** - Ogg Vorbis compressed audio  
- **OPUS** - Opus compressed audio (most common for spatial audio)

The audio data is embedded directly in the IMM file's binary stream and loaded on-demand during playback.

### Audio Playback Architecture

The IMM Unity plugin uses **Facebook's Audio360 SDK** (TBE - Two Big Ears) for spatial audio:

```
IMM File → piWav (memory) → Audio360 SDK → Audio Device
```

**Key Point:** Audio playback **bypasses Unity's AudioSource system entirely**. The plugin creates its own audio engine that directly interfaces with the system audio device.

### 3D Audio Positioning

Each audio layer in an IMM file has:

1. **Transform Information**
   - Position (x, y, z) in world space
   - Direction vector (where the sound is pointing)
   - Up vector (orientation)

2. **Spatial Type**
   - `Flat (Headlocked)` - Traditional stereo, follows listener's head
   - `Ambisonic (360)` - 360-degree spatial audio
   - `Positional (3D)` - 3D positioned with distance attenuation

3. **Attenuation Settings** (for Positional audio)
   - Type: None, Linear, or Logarithmic
   - Min distance (full volume)
   - Max distance (silent)

4. **Audio Properties**
   - Gain (amplification)
   - Volume (0.0 to 1.0)
   - Looping (true/false)
   - Sample rate, channels, bit depth

## Extracting Audio Information

### Using the ImmAudioExtractor Tool

I've created a tool at `code/tools/ImmAudioExtractor/` that can extract audio information and export audio files.

#### Build the Tool

```bash
# From IMM repository root
mkdir build
cd build
cmake ..
cmake --build . --target ImmAudioExtractor
```

#### Extract Audio Information

```bash
# Display all audio layer information
./ImmAudioExtractor info myfile.imm
```

**Example Output:**
```
Found 2 audio layer(s)

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

=== Audio Layer: BirdSound ===
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

#### Export Audio Files

```bash
# Export all audio as WAV files
./ImmAudioExtractor export myfile.imm ./audio_output wav

# Export as OPUS files
./ImmAudioExtractor export myfile.imm ./audio_output opus

# Export as OGG files
./ImmAudioExtractor export myfile.imm ./audio_output ogg
```

This creates one audio file per audio layer, named after the layer name.

### Using the Python Wrapper

For easier integration, use the Python wrapper:

```bash
# Display audio info
python code/tools/ImmAudioExtractor/imm_audio_info.py info myfile.imm

# Get JSON output
python code/tools/ImmAudioExtractor/imm_audio_info.py info myfile.imm --json

# Export audio
python code/tools/ImmAudioExtractor/imm_audio_info.py export myfile.imm ./output wav
```

## Converting Audio Formats

### Method 1: Using ImmAudioExtractor

The simplest way to convert audio:

```bash
# Extract as WAV (uncompressed)
./ImmAudioExtractor export original.imm ./temp wav

# Then use external tools like ffmpeg to convert
ffmpeg -i temp/audio.wav -c:a libopus -b:a 128k output.opus
```

### Method 2: Programmatic Conversion

Use the IMM SDK directly in your code:

```cpp
#include "libImmCore/src/libWave/piWave.h"
#include "libImmCore/src/libWave/formats/piWaveWAV.h"
#include "libImmCore/src/libWave/formats/piWaveOGG.h"
#include "libImmCore/src/libWave/formats/piWaveOPUS.h"

// Load audio from disk
piWav wav;
ReadWAVFromDisk(&wav, L"input.wav");

// Convert to different formats
WriteOGGToFile(L"output.ogg", &wav);
WriteOPUSToFile(L"output.opus", &wav, 128000); // 128kbps bitrate
WriteWAVToDisk(L"output.wav", &wav);

// Clean up
wav.Deinit();
```

### Method 3: Re-authoring IMM Files

To change the audio codec in an IMM file:

1. Extract audio using ImmAudioExtractor
2. Convert audio using external tools
3. Re-import into your IMM authoring tool (e.g., Quill)
4. Export new IMM file with desired audio format

## Understanding Audio Position Data

### Coordinate System

IMM uses a **right-handed coordinate system**:
- +X = Right
- +Y = Up  
- +Z = Forward (towards viewer)

### Extracting Position Programmatically

```cpp
// Get the layer's world transform
trans3d transform = layer->GetTransformToWorld();

// Extract position
vec3d position = transform.mTranslation;

// Extract direction (forward vector)
vec3d direction = normalize((transform * vec4d(0, 0, -1, 0)).xyz());

// Extract up vector
vec3d up = normalize((transform * vec4d(0, 1, 0, 0)).xyz());
```

### Position in Unity

If you want to use these positions in Unity:

```csharp
// IMM position to Unity position
Vector3 unityPos = new Vector3(
    (float)immPos.x,
    (float)immPos.y,
    (float)immPos.z
);

// IMM direction to Unity direction  
Vector3 unityDir = new Vector3(
    (float)immDir.x,
    (float)immDir.y,
    (float)immDir.z
);
```

## Common Use Cases

### 1. Analyzing Spatial Audio Layout

```bash
# Get JSON output for analysis
python imm_audio_info.py info scene.imm --json > audio_layout.json

# Process with jq or other tools
cat audio_layout.json | jq '.[] | select(.spatial_type == "Positional (3D)")'
```

### 2. Batch Converting Audio

```bash
# Extract all audio as WAV
./ImmAudioExtractor export scene.imm ./wav_output wav

# Convert all to MP3 with ffmpeg
for f in wav_output/*.wav; do
    ffmpeg -i "$f" -c:a libmp3lame -b:a 192k "${f%.wav}.mp3"
done
```

### 3. Creating Audio Visualization

Use the extracted position data to create visualizations:

```python
import json
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D

# Load audio info
with open('audio_layout.json') as f:
    audio_data = json.load(f)

# Plot 3D positions
fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')

for audio in audio_data:
    pos = audio['position']
    ax.scatter(pos['x'], pos['y'], pos['z'], label=audio['layer_name'])

ax.set_xlabel('X')
ax.set_ylabel('Y')
ax.set_zlabel('Z')
plt.legend()
plt.show()
```

## Technical Reference

### Audio Data Flow

```
1. IMM File (binary)
   ↓
2. fromImmersiveLayerSound.cpp reads audio data
   ↓
3. Stored in LayerSound::mSound (piWav structure)
   ↓
4. LayerRendererSound::LoadInSPU() loads to sound engine
   ↓
5. Audio360 SDK creates AudioObject
   ↓
6. Direct audio device output
```

### Key Files

- `code/libImmImporter/src/fromImmersive/fromImmersiveLayerSound.cpp` - Reads audio from IMM
- `code/libImmPlayer/src/layerRenderers/layerRendererSound/layerRendererSound.cpp` - Audio playback
- `code/libImmCore/src/libSound/windows/piSoundEngineAudioSDKBackend.cpp` - Audio360 integration
- `code/libImmCore/src/libWave/` - Audio format conversion utilities

## Troubleshooting

### Audio not found in IMM file
- Check that the IMM was exported with audio included
- Some authoring tools may strip audio for smaller file sizes

### Position data seems incorrect
- Remember IMM uses right-handed coordinates
- Position is in world space after applying layer hierarchy transforms
- Check parent layer transforms if audio is in a group

### Exported audio is silent
- Check the gain and volume values
- Verify the audio data isn't corrupted in the IMM file
- Try exporting as WAV first to rule out codec issues

## See Also

- `code/tools/ImmAudioExtractor/README.md` - Detailed tool documentation
- Audio360 SDK documentation in `thirdparty/audio360-sdk/`
- IMM file format specification (if available)

