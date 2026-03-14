// IMM Audio Extractor Tool
// Extracts audio information and audio files from IMM files
// Can export audio to different formats (WAV, OGG, OPUS)

#include <stdio.h>
#include <string>
#include <vector>

#include "libImmCore/src/libBasics/piLog.h"
#include "libImmCore/src/libBasics/piFile.h"
#include "libImmCore/src/libBasics/piString.h"
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

struct AudioInfo
{
    std::string layerName;
    vec3d position;
    vec3d direction;
    vec3d up;
    LayerSound::Type spatialType;
    LayerSound::AttenuationType attenuationType;
    float attenuationMin;
    float attenuationMax;
    float gain;
    float volume;
    bool isLooping;
    int numChannels;
    int sampleRate;
    int bits;
    uint64_t numSamples;
    double duration;
};

class AudioExtractor
{
public:
    AudioExtractor() : mLog()
    {
        mLog.Init(L"ImmAudioExtractor.log", LT_MESSAGE, false);
    }

    ~AudioExtractor()
    {
        mLog.End();
    }

    bool ExtractAudioInfo(const wchar_t* immFilePath, std::vector<AudioInfo>& audioInfos)
    {
        Sequence sequence;
        if (!ImportFromDisk(&sequence, &mLog, immFilePath, Drawing::ColorSpace::Linear, Drawing::PaintRenderingTechnique::Static))
        {
            mLog.Printf(LT_ERROR, L"Failed to load IMM file: %s", immFilePath);
            return false;
        }

        mLog.Printf(LT_MESSAGE, L"Successfully loaded IMM file");
        
        // Traverse all layers and find sound layers
        TraverseLayersForAudio(&sequence, sequence.GetRootLayer(), audioInfos);

        mLog.Printf(LT_MESSAGE, L"Found %d audio layers", (int)audioInfos.size());
        return true;
    }

    bool ExportAudio(const wchar_t* immFilePath, const wchar_t* outputFolder, const char* format)
    {
        Sequence sequence;
        if (!ImportFromDisk(&sequence, &mLog, immFilePath, Drawing::ColorSpace::Linear, Drawing::PaintRenderingTechnique::Static))
        {
            mLog.Printf(LT_ERROR, L"Failed to load IMM file: %s", immFilePath);
            return false;
        }

        mLog.Printf(LT_MESSAGE, L"Exporting audio from IMM file...");
        
        int exportCount = 0;
        ExportLayerAudio(&sequence, sequence.GetRootLayer(), outputFolder, format, exportCount);

        mLog.Printf(LT_MESSAGE, L"Exported %d audio files", exportCount);
        return exportCount > 0;
    }

private:
    piLog mLog;

    void TraverseLayersForAudio(Sequence* sequence, Layer* layer, std::vector<AudioInfo>& audioInfos)
    {
        if (!layer) return;

        if (layer->GetType() == Layer::Type::Sound && layer->GetLoaded())
        {
            LayerSound* soundLayer = (LayerSound*)layer->GetImplementation();
            AudioInfo info = ExtractAudioInfoFromLayer(layer, soundLayer);
            audioInfos.push_back(info);
        }

        // Recursively process children
        const int numChildren = layer->GetNumChildren();
        for (int i = 0; i < numChildren; i++)
        {
            TraverseLayersForAudio(sequence, layer->GetChild(i), audioInfos);
        }
    }

    AudioInfo ExtractAudioInfoFromLayer(Layer* layer, LayerSound* soundLayer)
    {
        AudioInfo info;
        
        // Layer name
        piString name = layer->GetName();
        info.layerName = piws2str(name.GetS());

        // Transform (position, direction, up)
        trans3d transform = layer->GetTransformToWorld();
        info.position = transform.mTranslation;
        info.direction = normalize((transform * vec4d(0, 0, -1, 0)).xyz());
        info.up = normalize((transform * vec4d(0, 1, 0, 0)).xyz());

        // Spatial properties
        info.spatialType = soundLayer->GetType();
        
        LayerSound::AttenuationParameters atten;
        soundLayer->GetAttenuation(&atten);
        info.attenuationType = atten.mType;
        info.attenuationMin = atten.mMin;
        info.attenuationMax = atten.mMax;

        // Audio properties
        info.gain = soundLayer->GetGain();
        info.volume = soundLayer->GetVolume();
        info.isLooping = soundLayer->GetLooping();

        // Wave data
        const piWav* wav = soundLayer->GetSound();
        info.numChannels = wav->mNumChannels;
        info.sampleRate = wav->mRate;
        info.bits = wav->mBits;
        info.numSamples = wav->mNumSamples;
        info.duration = (double)wav->mNumSamples / (double)wav->mRate;

        return info;
    }

    void ExportLayerAudio(Sequence* sequence, Layer* layer, const wchar_t* outputFolder, const char* format, int& exportCount)
    {
        if (!layer) return;

        if (layer->GetType() == Layer::Type::Sound && layer->GetLoaded())
        {
            LayerSound* soundLayer = (LayerSound*)layer->GetImplementation();
            const piWav* wav = soundLayer->GetSound();

            // Build output filename
            piString outputPath;
            outputPath.InitCopyW(outputFolder);
            outputPath.AppendW(L"/");
            outputPath.Append(layer->GetName());
            outputPath.AppendS(".");
            outputPath.AppendS(format);

            bool success = false;
            if (strcmp(format, "wav") == 0)
            {
                success = WriteWAVToDisk(outputPath.GetS(), wav);
            }
            else if (strcmp(format, "ogg") == 0)
            {
                success = WriteOGGToFile(outputPath.GetS(), wav);
            }
            else if (strcmp(format, "opus") == 0)
            {
                success = WriteOPUSToFile(outputPath.GetS(), wav, 128000); // 128kbps bitrate
            }

            if (success)
            {
                mLog.Printf(LT_MESSAGE, L"Exported: %s", outputPath.GetS());
                exportCount++;
            }
            else
            {
                mLog.Printf(LT_ERROR, L"Failed to export: %s", outputPath.GetS());
            }

            outputPath.End();
        }

        // Recursively process children
        const int numChildren = layer->GetNumChildren();
        for (int i = 0; i < numChildren; i++)
        {
            ExportLayerAudio(sequence, layer->GetChild(i), outputFolder, format, exportCount);
        }
    }
};

void PrintAudioInfo(const AudioInfo& info)
{
    printf("\n=== Audio Layer: %s ===\n", info.layerName.c_str());
    printf("Position: (%.3f, %.3f, %.3f)\n", info.position.x, info.position.y, info.position.z);
    printf("Direction: (%.3f, %.3f, %.3f)\n", info.direction.x, info.direction.y, info.direction.z);
    printf("Up: (%.3f, %.3f, %.3f)\n", info.up.x, info.up.y, info.up.z);

    const char* spatialTypes[] = { "Flat (Headlocked)", "Ambisonic (360)", "Positional (3D)" };
    printf("Spatial Type: %s\n", spatialTypes[(int)info.spatialType]);

    if (info.spatialType == LayerSound::Type::Positional)
    {
        const char* attenTypes[] = { "None", "Linear", "Logarithmic" };
        printf("Attenuation: %s (Min: %.2f, Max: %.2f)\n",
               attenTypes[(int)info.attenuationType],
               info.attenuationMin,
               info.attenuationMax);
    }

    printf("Gain: %.2f\n", info.gain);
    printf("Volume: %.2f\n", info.volume);
    printf("Looping: %s\n", info.isLooping ? "Yes" : "No");
    printf("Channels: %d\n", info.numChannels);
    printf("Sample Rate: %d Hz\n", info.sampleRate);
    printf("Bit Depth: %d bits\n", info.bits);
    printf("Duration: %.2f seconds\n", info.duration);
}

void PrintUsage()
{
    printf("IMM Audio Extractor Tool\n");
    printf("========================\n\n");
    printf("Usage:\n");
    printf("  ImmAudioExtractor info <imm_file>\n");
    printf("      - Display information about all audio layers in the IMM file\n\n");
    printf("  ImmAudioExtractor export <imm_file> <output_folder> <format>\n");
    printf("      - Export all audio from IMM file to specified format\n");
    printf("      - Supported formats: wav, ogg, opus\n\n");
    printf("Examples:\n");
    printf("  ImmAudioExtractor info myfile.imm\n");
    printf("  ImmAudioExtractor export myfile.imm ./audio_output wav\n");
    printf("  ImmAudioExtractor export myfile.imm ./audio_output opus\n");
}

int main(int argc, char* argv[])
{
    if (argc < 3)
    {
        PrintUsage();
        return 1;
    }

    std::string command = argv[1];
    const wchar_t* immFile = pistr2ws(argv[2]);

    AudioExtractor extractor;

    if (command == "info")
    {
        std::vector<AudioInfo> audioInfos;
        if (!extractor.ExtractAudioInfo(immFile, audioInfos))
        {
            printf("Failed to extract audio information\n");
            return 1;
        }

        printf("\nFound %d audio layer(s)\n", (int)audioInfos.size());
        for (const auto& info : audioInfos)
        {
            PrintAudioInfo(info);
        }
    }
    else if (command == "export")
    {
        if (argc < 5)
        {
            printf("Error: export command requires output folder and format\n");
            PrintUsage();
            return 1;
        }

        const wchar_t* outputFolder = pistr2ws(argv[3]);
        const char* format = argv[4];

        if (strcmp(format, "wav") != 0 && strcmp(format, "ogg") != 0 && strcmp(format, "opus") != 0)
        {
            printf("Error: Unsupported format '%s'. Use wav, ogg, or opus\n", format);
            return 1;
        }

        if (!extractor.ExportAudio(immFile, outputFolder, format))
        {
            printf("Failed to export audio\n");
            return 1;
        }

        printf("\nAudio export completed successfully!\n");
    }
    else
    {
        printf("Error: Unknown command '%s'\n", command.c_str());
        PrintUsage();
        return 1;
    }

    return 0;
}

