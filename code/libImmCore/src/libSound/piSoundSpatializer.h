//
// Platform independent evaluation of the IMM/Quill spatial audio model.
//
// This is the ONE place where "what the author meant" (LayerSound's type,
// attenuation curve, directional cone/frustum and world transform) becomes
// gains, delays and filter coefficients. Every backend that does its own
// mixing must go through here, so that Quest, Apple, web and any platform we
// add later agree with each other by construction instead of by luck.
//
// Nothing in this file touches an audio device, an OS API or a third party
// SDK - it is pure math over the values the player already computes in
// LayerRendererSound::GlobalWork. That is deliberate: a new platform should
// only ever have to write a device shim, never a spatializer.
//
// The attenuation and modifier math is a port of the behaviour of the Windows
// Audio360 backend (piSoundEngineAudioSDKBackend.cpp) so that content authored
// and checked against the Windows viewer keeps sounding the same everywhere.
//
#pragma once

#include "../libBasics/piTypes.h"
#include "../libBasics/piVecTypes.h"

namespace ImmCore
{

    class piSoundSpatializer
    {
    public:

        // Mirrors piSoundEngine::AttenuationType / LayerSound::AttenuationType.
        enum class Attenuation
        {
            None = 0,
            Linear = 1,
            Logarithmic = 2,
            InverseSquare = 3
        };

        // Mirrors piSoundEngine::ModifierType / LayerSound::ModifierType.
        enum class Modifier
        {
            None = 0,
            Cone = 1,
            Frustum = 2
        };

        // Channel layout of a 4 channel first order bed.
        //   AmbiX = ACN ordering (W,Y,Z,X) with SN3D normalization. This is what
        //           Audio360 called AMBIX_4 and is the default for IMM content.
        //   FuMa  = legacy ordering (W,X,Y,Z) with W attenuated by 1/sqrt(2).
        enum class Ambisonic
        {
            AmbiX = 0,
            FuMa = 1
        };

        // The head, in world space. IMM/GL convention: the viewer faces -Z,
        // +Y is up, +X is right.
        struct Listener
        {
            vec3d mPosition;
            vec3d mForward;
            vec3d mUp;
            vec3d mRight;

            void SetIdentity(void);
            void SetFromTransform(const trans3d & listenerToWorld);
        };

        // One emitter, in world space, with the authored parameters as stored
        // in the .imm. Distances are expected to be already scaled by the
        // layer's world scale (the player does this).
        struct Voice
        {
            vec3d       mPosition;
            vec3d       mForward;
            vec3d       mUp;

            Attenuation mAttenuation;
            double      mAttenuationMin;
            double      mAttenuationMax;

            Modifier    mModifier;
            double      mConeAngleInner;      // radians
            double      mConeAngleBand;       // radians
            double      mConeAttenOut;
            double      mFrustumAngleInnerX;  // radians
            double      mFrustumAngleInnerY;  // radians
            double      mFrustumAngleBand;    // radians
            double      mFrustumAttenOut;

            void SetDefaults(void);
        };

        // Per-ear rendering parameters for one positional emitter.
        //   mGain   includes distance attenuation and the constant power pan.
        //   mDelay  is the interaural time difference, in samples.
        //   mCutoff is a one pole lowpass blend factor (head shadowing);
        //           1 means bypass, smaller values are duller.
        struct Binaural
        {
            float mGain[2];
            float mDelay[2];
            float mCutoff[2];
        };

        // Distance attenuation only. Audio360 semantics: full gain at or inside
        // the minimum distance, silent at or beyond the maximum distance.
        static float ComputeAttenuation(const Voice & voice, const Listener & listener);

        // Directional cone / frustum gain. This is what piSoundEngine::
        // ComputeModifiers must return - the player folds it into the voice
        // volume itself, so a mixer must NOT apply it a second time.
        static float ComputeModifier(const Voice & voice, const Listener & listener);

        // Full positional render parameters. Includes ComputeAttenuation but
        // deliberately NOT ComputeModifier, for the reason above.
        static void ComputeBinaural(const Voice & voice, const Listener & listener,
                                    int sampleRate, Binaural * result);

        // Reduces a binaural result to a single gain plus a stereo pan, for
        // output stages that only offer those two controls - AVAudioPlayer on
        // Apple, a StereoPannerNode on the web. Interaural delay and head
        // shadowing are lost; distance and lateral position survive.
        //
        // `gain` comes back as the distance attenuation and `pan` as -1..1,
        // because the pan law here is constant power: gainL^2 + gainR^2 is the
        // attenuation squared, and gainR^2 - gainL^2 is the attenuation
        // squared times the pan.
        static void ReduceToStereoPan(const Binaural & binaural, float * gain, float * pan);

        // Decode weights for a first order bed, honouring both the listener's
        // orientation and the bed layer's own orientation.
        // result[ear][channel], channel indices follow `convention`.
        static void ComputeAmbisonicDecode(const Voice & voice, const Listener & listener,
                                           Ambisonic convention, float result[2][4]);

        // y += coefficient * (x - y). Returns 1 (bypass) for cutoffs at or
        // above Nyquist.
        static float OnePoleCoefficient(float cutoffHz, int sampleRate);

        // Longest interaural delay a mixer must be able to buffer.
        static int MaxDelaySamples(int sampleRate);

        static const float kHeadRadius;    // meters
        static const float kSpeedOfSound;  // meters/second
    };

} // namespace ImmCore
