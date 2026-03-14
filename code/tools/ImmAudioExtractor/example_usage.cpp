// Example: How to programmatically extract audio information from IMM files
// This demonstrates using the IMM SDK directly without the command-line tool

#include <stdio.h>
#include "libImmCore/src/libBasics/piLog.h"
#include "libImmCore/src/libWave/piWave.h"
#include "libImmCore/src/libWave/formats/piWaveWAV.h"
#include "libImmCore/src/libWave/formats/piWaveOGG.h"
#include "libImmCore/src/libWave/formats/piWaveOPUS.h"
#include "libImmImporter/src/fromImmersive/fromImmersive.h"
#include "libImmImporter/src/document/sequence.h"
#include "libImmImporter/src/document/layer.h"
#include "libImmImporter/src/document/layerSound.h"

using namespace ImmCore;
using namespace ImmImporter;

// Example 1: Extract audio information
void ExtractAudioInfo(const wchar_t* immFilePath)
{
    piLog log;
    log.Init(L"example.log", LT_MESSAGE, false);
    
    Sequence sequence;
    if (!ImportFromDisk(&sequence, &log, immFilePath, Drawing::ColorSpace::Linear, Drawing::PaintRenderingTechnique::Static))
    {
        printf("Failed to load IMM file\n");
        log.End();
        return;
    }
    
    printf("Successfully loaded IMM file\n\n");
    
    // Traverse all layers
    Layer* rootLayer = sequence.GetRootLayer();
    TraverseAndPrintAudio(rootLayer);
    
    log.End();
}

void TraverseAndPrintAudio(Layer* layer)
{
    if (!layer) return;
    
    // Check if this is a sound layer
    if (layer->GetType() == Layer::Type::Sound && layer->GetLoaded())
    {
        LayerSound* soundLayer = (LayerSound*)layer->GetImplementation();
        
        // Get layer name
        piString name = layer->GetName();
        printf("=== Audio Layer: %s ===\n", piws2str(name.GetS()));
        
        // Get 3D position and orientation
        trans3d transform = layer->GetTransformToWorld();
        vec3d position = transform.mTranslation;
        vec3d direction = normalize((transform * vec4d(0, 0, -1, 0)).xyz());
        vec3d up = normalize((transform * vec4d(0, 1, 0, 0)).xyz());
        
        printf("Position: (%.3f, %.3f, %.3f)\n", position.x, position.y, position.z);
        printf("Direction: (%.3f, %.3f, %.3f)\n", direction.x, direction.y, direction.z);
        printf("Up: (%.3f, %.3f, %.3f)\n", up.x, up.y, up.z);
        
        // Get spatial type
        LayerSound::Type spatialType = soundLayer->GetType();
        const char* typeNames[] = { "Flat (Headlocked)", "Ambisonic (360)", "Positional (3D)" };
        printf("Spatial Type: %s\n", typeNames[(int)spatialType]);
        
        // Get attenuation (for positional audio)
        if (spatialType == LayerSound::Type::Positional)
        {
            LayerSound::AttenuationParameters atten;
            soundLayer->GetAttenuation(&atten);
            
            const char* attenTypes[] = { "None", "Linear", "Logarithmic" };
            printf("Attenuation: %s (Min: %.2f, Max: %.2f)\n", 
                   attenTypes[(int)atten.mType], atten.mMin, atten.mMax);
        }
        
        // Get audio properties
        printf("Gain: %.2f\n", soundLayer->GetGain());
        printf("Volume: %.2f\n", soundLayer->GetVolume());
        printf("Looping: %s\n", soundLayer->GetLooping() ? "Yes" : "No");
        
        // Get wave data
        const piWav* wav = soundLayer->GetSound();
        printf("Channels: %d\n", wav->mNumChannels);
        printf("Sample Rate: %d Hz\n", wav->mRate);
        printf("Bit Depth: %d bits\n", wav->mBits);
        printf("Duration: %.2f seconds\n", (double)wav->mNumSamples / (double)wav->mRate);
        printf("\n");
    }
    
    // Recursively process children
    const int numChildren = layer->GetNumChildren();
    for (int i = 0; i < numChildren; i++)
    {
        TraverseAndPrintAudio(layer->GetChild(i));
    }
}

// Example 2: Export audio to different formats
void ExportAudioFromIMM(const wchar_t* immFilePath, const wchar_t* outputFolder)
{
    piLog log;
    log.Init(L"example.log", LT_MESSAGE, false);
    
    Sequence sequence;
    if (!ImportFromDisk(&sequence, &log, immFilePath, Drawing::ColorSpace::Linear, Drawing::PaintRenderingTechnique::Static))
    {
        printf("Failed to load IMM file\n");
        log.End();
        return;
    }
    
    Layer* rootLayer = sequence.GetRootLayer();
    ExportLayerAudio(rootLayer, outputFolder);
    
    log.End();
}

void ExportLayerAudio(Layer* layer, const wchar_t* outputFolder)
{
    if (!layer) return;
    
    if (layer->GetType() == Layer::Type::Sound && layer->GetLoaded())
    {
        LayerSound* soundLayer = (LayerSound*)layer->GetImplementation();
        const piWav* wav = soundLayer->GetSound();
        
        // Build output paths
        piString basePath;
        basePath.InitCopyW(outputFolder);
        basePath.AppendW(L"/");
        basePath.Append(layer->GetName());
        
        // Export as WAV
        piString wavPath;
        wavPath.InitCopyS(basePath.GetS());
        wavPath.AppendS(".wav");
        if (WriteWAVToDisk(wavPath.GetS(), wav))
        {
            printf("Exported WAV: %s\n", piws2str(wavPath.GetS()));
        }
        wavPath.End();
        
        // Export as OGG
        piString oggPath;
        oggPath.InitCopyS(basePath.GetS());
        oggPath.AppendS(".ogg");
        if (WriteOGGToFile(oggPath.GetS(), wav))
        {
            printf("Exported OGG: %s\n", piws2str(oggPath.GetS()));
        }
        oggPath.End();
        
        // Export as OPUS (128kbps)
        piString opusPath;
        opusPath.InitCopyS(basePath.GetS());
        opusPath.AppendS(".opus");
        if (WriteOPUSToFile(opusPath.GetS(), wav, 128000))
        {
            printf("Exported OPUS: %s\n", piws2str(opusPath.GetS()));
        }
        opusPath.End();
        
        basePath.End();
    }
    
    // Recursively process children
    const int numChildren = layer->GetNumChildren();
    for (int i = 0; i < numChildren; i++)
    {
        ExportLayerAudio(layer->GetChild(i), outputFolder);
    }
}

// Example 3: Convert audio format
void ConvertAudioFormat(const wchar_t* inputPath, const wchar_t* outputPath, const char* format)
{
    piWav wav;
    
    // Load audio (supports WAV, OGG)
    if (!wav.ReadFromDisk(inputPath))
    {
        printf("Failed to load audio file\n");
        return;
    }
    
    printf("Loaded audio: %d channels, %d Hz, %d bits\n", 
           wav.mNumChannels, wav.mRate, wav.mBits);
    
    // Convert to desired format
    bool success = false;
    if (strcmp(format, "wav") == 0)
    {
        success = WriteWAVToDisk(outputPath, &wav);
    }
    else if (strcmp(format, "ogg") == 0)
    {
        success = WriteOGGToFile(outputPath, &wav);
    }
    else if (strcmp(format, "opus") == 0)
    {
        success = WriteOPUSToFile(outputPath, &wav, 128000); // 128kbps
    }
    
    if (success)
    {
        printf("Successfully converted to %s\n", format);
    }
    else
    {
        printf("Failed to convert audio\n");
    }
    
    wav.Deinit();
}

int main()
{
    printf("IMM Audio Extraction Examples\n");
    printf("=============================\n\n");
    
    // Example 1: Extract and print audio info
    printf("Example 1: Extract audio information\n");
    ExtractAudioInfo(L"example.imm");
    
    // Example 2: Export all audio from IMM
    printf("\nExample 2: Export audio files\n");
    ExportAudioFromIMM(L"example.imm", L"./audio_output");
    
    // Example 3: Convert a single audio file
    printf("\nExample 3: Convert audio format\n");
    ConvertAudioFormat(L"input.wav", L"output.opus", "opus");
    
    return 0;
}

