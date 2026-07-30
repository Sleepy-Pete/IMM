//
// Conformance test for the platform independent spatializer.
//
// This is the guard that keeps every backend honest. The spatializer is pure
// math with no device, so it can be built and run on any host - which means a
// new platform port can be checked against the same numbers the Windows viewer
// was mixed on, before anyone puts a headset on.
//
// Build + run:  code/projects/windows/run-spatializer-conformance.ps1
//
#include <math.h>
#include <stdio.h>

#include "../piSoundSpatializer.h"

using namespace ImmCore;

static int gFailures = 0;
static int gChecks = 0;

static void iCheck(bool condition, const char *what)
{
    gChecks++;
    if (!condition)
    {
        gFailures++;
        printf("  FAIL  %s\n", what);
    }
}

static void iCheckNear(float value, float expected, float tolerance, const char *what)
{
    gChecks++;
    if (!(fabsf(value - expected) <= tolerance))
    {
        gFailures++;
        printf("  FAIL  %s (got %.6f, expected %.6f +/- %.6f)\n", what, value, expected, tolerance);
    }
}

static piSoundSpatializer::Listener iListenerAtOrigin(void)
{
    piSoundSpatializer::Listener l;
    l.SetIdentity();
    return l;
}

// A voice sitting `distance` meters straight ahead of an origin listener.
static piSoundSpatializer::Voice iVoiceAhead(double distance)
{
    piSoundSpatializer::Voice v;
    v.SetDefaults();
    v.mPosition = vec3d(0.0, 0.0, -distance);
    return v;
}

static void iTestAttenuation(void)
{
    printf("attenuation\n");
    const piSoundSpatializer::Listener listener = iListenerAtOrigin();

    {
        piSoundSpatializer::Voice v = iVoiceAhead(1000.0);
        v.mAttenuation = piSoundSpatializer::Attenuation::None;
        iCheckNear(piSoundSpatializer::ComputeAttenuation(v, listener), 1.0f, 1e-6f,
                   "None is unattenuated at any distance");
    }

    {
        piSoundSpatializer::Voice v = iVoiceAhead(1.0);
        v.mAttenuation = piSoundSpatializer::Attenuation::Linear;
        v.mAttenuationMin = 1.0;
        v.mAttenuationMax = 11.0;
        iCheckNear(piSoundSpatializer::ComputeAttenuation(v, listener), 1.0f, 1e-6f,
                   "Linear is full gain at the minimum distance");

        v.mPosition = vec3d(0.0, 0.0, -6.0);
        iCheckNear(piSoundSpatializer::ComputeAttenuation(v, listener), 0.5f, 1e-5f,
                   "Linear is half gain at the midpoint");

        v.mPosition = vec3d(0.0, 0.0, -11.0);
        iCheckNear(piSoundSpatializer::ComputeAttenuation(v, listener), 0.0f, 1e-6f,
                   "Linear mutes at the maximum distance");

        v.mPosition = vec3d(0.0, 0.0, -500.0);
        iCheckNear(piSoundSpatializer::ComputeAttenuation(v, listener), 0.0f, 1e-6f,
                   "Linear stays muted beyond the maximum distance");
    }

    {
        // Audio360 parity: the logarithmic curve is built to land 30 dB down
        // at the maximum distance (factor = 5 / log2(max/min)).
        piSoundSpatializer::Voice v = iVoiceAhead(1.0);
        v.mAttenuation = piSoundSpatializer::Attenuation::Logarithmic;
        v.mAttenuationMin = 1.0;
        v.mAttenuationMax = 32.0;

        iCheckNear(piSoundSpatializer::ComputeAttenuation(v, listener), 1.0f, 1e-6f,
                   "Logarithmic is full gain at the minimum distance");

        v.mPosition = vec3d(0.0, 0.0, -31.999);
        const float nearMax = piSoundSpatializer::ComputeAttenuation(v, listener);
        const float decibels = 20.0f * log10f(nearMax);
        iCheckNear(decibels, -30.0f, 0.5f, "Logarithmic is ~30 dB down just inside the maximum");

        v.mPosition = vec3d(0.0, 0.0, -32.0);
        iCheckNear(piSoundSpatializer::ComputeAttenuation(v, listener), 0.0f, 1e-6f,
                   "Logarithmic mutes at the maximum distance");
    }

    {
        // Monotonically decreasing, which the mixer relies on for smoothing.
        piSoundSpatializer::Voice v = iVoiceAhead(1.0);
        v.mAttenuation = piSoundSpatializer::Attenuation::Logarithmic;
        v.mAttenuationMin = 1.0;
        v.mAttenuationMax = 50.0;
        float previous = 2.0f;
        bool monotonic = true;
        for (int i = 1; i < 50; i++)
        {
            v.mPosition = vec3d(0.0, 0.0, -double(i));
            const float g = piSoundSpatializer::ComputeAttenuation(v, listener);
            if (g > previous + 1e-6f) monotonic = false;
            previous = g;
        }
        iCheck(monotonic, "Logarithmic never increases with distance");
    }
}

static void iTestConeModifier(void)
{
    printf("cone modifier\n");
    const piSoundSpatializer::Listener listener = iListenerAtOrigin();

    // An emitter 5 m ahead of the listener, facing back at them (+Z).
    piSoundSpatializer::Voice v;
    v.SetDefaults();
    v.mPosition = vec3d(0.0, 0.0, -5.0);
    v.mForward = vec3d(0.0, 0.0, 1.0);
    v.mUp = vec3d(0.0, 1.0, 0.0);
    v.mModifier = piSoundSpatializer::Modifier::Cone;
    v.mConeAngleInner = 0.3;
    v.mConeAngleBand = 0.4;
    v.mConeAttenOut = 0.1;

    iCheckNear(piSoundSpatializer::ComputeModifier(v, listener), 1.0f, 1e-5f,
               "Cone is unattenuated on axis");

    // Rotate the emitter to face away from the listener.
    v.mForward = vec3d(0.0, 0.0, -1.0);
    iCheckNear(piSoundSpatializer::ComputeModifier(v, listener), 0.1f, 1e-5f,
               "Cone falls to attenOut when facing away");

    // Just outside the inner angle but inside the band: strictly between.
    v.mForward = normalize(vec3d(sin(0.5), 0.0, cos(0.5)));
    const float partial = piSoundSpatializer::ComputeModifier(v, listener);
    iCheck(partial > 0.1f && partial < 1.0f, "Cone interpolates across the band");

    // attenOut is a floor, never a gain above unity.
    v.mConeAttenOut = 0.0;
    v.mForward = vec3d(0.0, 0.0, -1.0);
    iCheckNear(piSoundSpatializer::ComputeModifier(v, listener), 0.0f, 1e-5f,
               "Cone reaches silence when attenOut is zero");

    piSoundSpatializer::Voice none = v;
    none.mModifier = piSoundSpatializer::Modifier::None;
    iCheckNear(piSoundSpatializer::ComputeModifier(none, listener), 1.0f, 1e-6f,
               "No modifier is unity");
}

static void iTestFrustumModifier(void)
{
    printf("frustum modifier\n");
    const piSoundSpatializer::Listener listener = iListenerAtOrigin();

    piSoundSpatializer::Voice v;
    v.SetDefaults();
    v.mPosition = vec3d(0.0, 0.0, -5.0);
    v.mForward = vec3d(0.0, 0.0, 1.0);  // facing the listener
    v.mUp = vec3d(0.0, 1.0, 0.0);
    v.mModifier = piSoundSpatializer::Modifier::Frustum;
    v.mFrustumAngleInnerX = 0.5;
    v.mFrustumAngleInnerY = 0.5;
    v.mFrustumAngleBand = 0.2;
    v.mFrustumAttenOut = 0.25;

    iCheckNear(piSoundSpatializer::ComputeModifier(v, listener), 1.0f, 1e-5f,
               "Frustum is unattenuated on axis");

    // Behind the emitter: the Windows backend returns hard silence and ignores
    // attenOut. Parity is deliberate.
    v.mForward = vec3d(0.0, 0.0, -1.0);
    iCheckNear(piSoundSpatializer::ComputeModifier(v, listener), 0.0f, 1e-6f,
               "Frustum is silent behind the emitter (Audio360 parity)");

    // Well outside the frustum but still in front of it.
    v.mForward = vec3d(0.0, 0.0, 1.0);
    v.mPosition = vec3d(30.0, 0.0, -5.0);
    iCheckNear(piSoundSpatializer::ComputeModifier(v, listener), 0.25f, 1e-4f,
               "Frustum falls to attenOut outside the band");
}

static void iTestBinaural(void)
{
    printf("binaural\n");
    const int rate = 48000;
    const piSoundSpatializer::Listener listener = iListenerAtOrigin();
    piSoundSpatializer::Binaural b;

    {
        piSoundSpatializer::Voice v = iVoiceAhead(3.0);
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &b);
        iCheckNear(b.mGain[0], b.mGain[1], 1e-5f, "Source ahead is centred");
        iCheckNear(b.mDelay[0], 0.0f, 1e-5f, "Source ahead has no left delay");
        iCheckNear(b.mDelay[1], 0.0f, 1e-5f, "Source ahead has no right delay");
        iCheckNear(b.mCutoff[0], b.mCutoff[1], 1e-6f, "Source ahead is unshadowed");
        iCheckNear(b.mGain[0] * b.mGain[0] + b.mGain[1] * b.mGain[1], 1.0f, 1e-4f,
                   "Constant power with no attenuation");
    }

    {
        // Straight out to the listener's right.
        piSoundSpatializer::Voice v;
        v.SetDefaults();
        v.mPosition = vec3d(3.0, 0.0, 0.0);
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &b);
        iCheck(b.mGain[1] > b.mGain[0], "Source on the right is louder in the right ear");
        iCheck(b.mDelay[0] > b.mDelay[1], "Source on the right reaches the left ear later");
        iCheck(b.mCutoff[0] < b.mCutoff[1], "Source on the right shadows the left ear");
        iCheck(b.mDelay[0] <= float(piSoundSpatializer::MaxDelaySamples(rate)),
               "Interaural delay fits the mixer's delay line");
        iCheckNear(b.mGain[0] * b.mGain[0] + b.mGain[1] * b.mGain[1], 1.0f, 1e-4f,
                   "Panning preserves power");
    }

    {
        // Mirrored to the left: same magnitudes, swapped ears.
        piSoundSpatializer::Voice v;
        v.SetDefaults();
        v.mPosition = vec3d(-3.0, 0.0, 0.0);
        piSoundSpatializer::Binaural left;
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &left);
        v.mPosition = vec3d(3.0, 0.0, 0.0);
        piSoundSpatializer::Binaural right;
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &right);
        iCheckNear(left.mGain[0], right.mGain[1], 1e-5f, "Left/right panning is symmetric");
        iCheckNear(left.mDelay[1], right.mDelay[0], 1e-5f, "Left/right delay is symmetric");
    }

    {
        // Inside the head, panning stops meaning anything and the interaural
        // delay would flip on sub-millimetre movement. The imbalance must
        // collapse towards centre relative to the same direction far away.
        piSoundSpatializer::Voice v;
        v.SetDefaults();

        v.mPosition = vec3d(3.0, 0.0, 0.0);
        piSoundSpatializer::Binaural far;
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &far);
        const float farImbalance = fabsf(far.mGain[1] - far.mGain[0]);

        v.mPosition = vec3d(0.02, 0.0, 0.0);
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &b);
        const float nearImbalance = fabsf(b.mGain[1] - b.mGain[0]);

        iCheck(nearImbalance < farImbalance * 0.15f,
               "Near field collapses towards centre");
        iCheck(b.mDelay[0] < far.mDelay[0] * 0.15f,
               "Near field collapses the interaural delay too");

        // And it must be continuous - no jump at the near field boundary.
        v.mPosition = vec3d(0.249, 0.0, 0.0);
        piSoundSpatializer::Binaural inside;
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &inside);
        v.mPosition = vec3d(0.251, 0.0, 0.0);
        piSoundSpatializer::Binaural outside;
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &outside);
        iCheckNear(inside.mGain[1], outside.mGain[1], 1e-3f,
                   "Near field boundary is continuous");
    }

    {
        // Attenuation must scale the pair, not the balance.
        piSoundSpatializer::Voice v;
        v.SetDefaults();
        v.mPosition = vec3d(3.0, 0.0, 0.0);
        v.mAttenuation = piSoundSpatializer::Attenuation::Linear;
        v.mAttenuationMin = 1.0;
        v.mAttenuationMax = 5.0;
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &b);
        const float expected = 0.5f;  // (5-3)/(5-1)
        const float power = b.mGain[0] * b.mGain[0] + b.mGain[1] * b.mGain[1];
        iCheckNear(sqrtf(power), expected, 1e-4f, "Distance attenuation scales both ears");
    }

    {
        // A source behind the listener still lateralises correctly.
        piSoundSpatializer::Voice v;
        v.SetDefaults();
        v.mPosition = vec3d(2.0, 0.0, 2.0);  // behind and to the right
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &b);
        iCheck(b.mGain[1] > b.mGain[0], "Source behind-right is still louder on the right");
    }
}

// The reduction used by output stages that only have a gain and a pan
// (AVAudioPlayer on Apple). Tested here because those backends cannot be built
// or run on every host, but this math is the part that can actually be wrong.
static void iTestStereoPanReduction(void)
{
    printf("stereo pan reduction\n");
    const int rate = 48000;
    const piSoundSpatializer::Listener listener = iListenerAtOrigin();
    piSoundSpatializer::Binaural b;
    float gain = -1.0f;
    float pan = -2.0f;

    {
        piSoundSpatializer::Voice v = iVoiceAhead(3.0);
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &b);
        piSoundSpatializer::ReduceToStereoPan(b, &gain, &pan);
        iCheckNear(pan, 0.0f, 1e-5f, "Source ahead reduces to centre pan");
        iCheckNear(gain, 1.0f, 1e-5f, "Unattenuated source reduces to unity gain");
    }

    {
        piSoundSpatializer::Voice v;
        v.SetDefaults();
        v.mPosition = vec3d(3.0, 0.0, 0.0);
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &b);
        piSoundSpatializer::ReduceToStereoPan(b, &gain, &pan);
        iCheck(pan > 0.5f, "Source on the right reduces to a right-hand pan");
        iCheckNear(gain, 1.0f, 1e-5f, "Panning alone does not change the gain");

        v.mPosition = vec3d(-3.0, 0.0, 0.0);
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &b);
        float mirroredGain = 0.0f;
        float mirroredPan = 0.0f;
        piSoundSpatializer::ReduceToStereoPan(b, &mirroredGain, &mirroredPan);
        iCheckNear(mirroredPan, -pan, 1e-5f, "Reduction is symmetric left to right");
        iCheckNear(mirroredGain, gain, 1e-5f, "Mirroring does not change the gain");
    }

    {
        // Distance must land in the gain, and must NOT wash out the pan - that
        // was the failure mode of deriving pan from the raw ear gains.
        piSoundSpatializer::Voice near;
        near.SetDefaults();
        near.mPosition = vec3d(3.0, 0.0, 0.0);
        near.mAttenuation = piSoundSpatializer::Attenuation::Linear;
        near.mAttenuationMin = 1.0;
        near.mAttenuationMax = 11.0;
        piSoundSpatializer::ComputeBinaural(near, listener, rate, &b);
        float nearGain = 0.0f, nearPan = 0.0f;
        piSoundSpatializer::ReduceToStereoPan(b, &nearGain, &nearPan);

        piSoundSpatializer::Voice far = near;
        far.mPosition = vec3d(9.0, 0.0, 0.0);
        piSoundSpatializer::ComputeBinaural(far, listener, rate, &b);
        float farGain = 0.0f, farPan = 0.0f;
        piSoundSpatializer::ReduceToStereoPan(b, &farGain, &farPan);

        iCheck(farGain < nearGain, "Distance shows up in the gain");
        iCheckNear(farGain, 0.2f, 1e-4f, "Reduced gain matches the attenuation curve");
        iCheckNear(farPan, nearPan, 1e-5f, "Distance does not wash out the pan");
    }

    {
        // Beyond the maximum distance everything is silent, and the pan must
        // not divide by zero on the way there.
        piSoundSpatializer::Voice v;
        v.SetDefaults();
        v.mPosition = vec3d(50.0, 0.0, 0.0);
        v.mAttenuation = piSoundSpatializer::Attenuation::Linear;
        v.mAttenuationMin = 1.0;
        v.mAttenuationMax = 11.0;
        piSoundSpatializer::ComputeBinaural(v, listener, rate, &b);
        piSoundSpatializer::ReduceToStereoPan(b, &gain, &pan);
        iCheckNear(gain, 0.0f, 1e-6f, "Muted source reduces to zero gain");
        iCheck(pan >= -1.0f && pan <= 1.0f, "Muted source still yields a finite pan");
    }
}

static void iTestAmbisonic(void)
{
    printf("ambisonic\n");
    piSoundSpatializer::Voice bed;
    bed.SetDefaults();

    piSoundSpatializer::Listener listener = iListenerAtOrigin();
    float weights[2][4];

    // Encode a mono source into first order AmbiX (ACN/SN3D) at a world
    // direction, then decode and compare the ears.
    struct Encode
    {
        static void AmbiX(const vec3d & worldDirection, float out[4])
        {
            const vec3d d = normalize(worldDirection);
            const float aX = float(-d.z);  // ambisonic X is front
            const float aY = float(-d.x);  // ambisonic Y is left
            const float aZ = float(d.y);   // ambisonic Z is up
            out[0] = 1.0f;   // W
            out[1] = aY;     // Y
            out[2] = aZ;     // Z
            out[3] = aX;     // X
        }
    };

    struct Decode
    {
        static void Ears(const float channels[4], const float weights[2][4], float * l, float * r)
        {
            *l = 0.0f;
            *r = 0.0f;
            for (int c = 0; c < 4; c++)
            {
                *l += channels[c] * weights[0][c];
                *r += channels[c] * weights[1][c];
            }
        }
    };

    piSoundSpatializer::ComputeAmbisonicDecode(bed, listener, piSoundSpatializer::Ambisonic::AmbiX, weights);

    {
        float channels[4];
        Encode::AmbiX(vec3d(0.0, 0.0, -1.0), channels);  // straight ahead
        float l, r;
        Decode::Ears(channels, weights, &l, &r);
        iCheckNear(l, r, 1e-5f, "Bed content ahead decodes centred");
        iCheck(l > 0.0f, "Bed content ahead is audible");
    }

    {
        float channels[4];
        Encode::AmbiX(vec3d(-1.0, 0.0, 0.0), channels);  // to the listener's left
        float l, r;
        Decode::Ears(channels, weights, &l, &r);
        iCheck(l > r, "Bed content on the left decodes louder on the left");
    }

    {
        // Turn the listener 90 degrees to the right; content that was on the
        // left should swing round to the front and re-centre.
        piSoundSpatializer::Listener turned;
        turned.SetIdentity();
        turned.mForward = vec3d(-1.0, 0.0, 0.0);
        turned.mRight = vec3d(0.0, 0.0, -1.0);
        turned.mUp = vec3d(0.0, 1.0, 0.0);

        float turnedWeights[2][4];
        piSoundSpatializer::ComputeAmbisonicDecode(bed, turned, piSoundSpatializer::Ambisonic::AmbiX, turnedWeights);

        float channels[4];
        Encode::AmbiX(vec3d(-1.0, 0.0, 0.0), channels);
        float l, r;
        Decode::Ears(channels, turnedWeights, &l, &r);
        iCheckNear(l, r, 1e-5f, "Bed follows the head: left content centres when you turn to it");
    }

    {
        // The bed's own orientation must matter too: rotating the layer 180
        // degrees should move front content behind the listener.
        piSoundSpatializer::Voice rotated;
        rotated.SetDefaults();
        rotated.mForward = vec3d(0.0, 0.0, 1.0);

        float rotatedWeights[2][4];
        piSoundSpatializer::ComputeAmbisonicDecode(rotated, listener, piSoundSpatializer::Ambisonic::AmbiX, rotatedWeights);

        float channels[4];
        Encode::AmbiX(vec3d(0.0, 0.0, -1.0), channels);
        float straightL, straightR, rotatedL, rotatedR;
        Decode::Ears(channels, weights, &straightL, &straightR);
        Decode::Ears(channels, rotatedWeights, &rotatedL, &rotatedR);
        iCheck(rotatedL < straightL, "Rotating the bed layer moves its content");
    }
}

static void iTestListenerTransform(void)
{
    printf("listener transform\n");
    piSoundSpatializer::Listener l;
    const trans3d identity = trans3d(quatd(0.0, 0.0, 0.0, 1.0), 1.0, flip3::N, vec3d(0.0, 0.0, 0.0));
    l.SetFromTransform(identity);

    iCheckNear(float(l.mForward.z), -1.0f, 1e-6f, "Identity listener faces -Z");
    iCheckNear(float(l.mUp.y), 1.0f, 1e-6f, "Identity listener is Y up");
    iCheckNear(float(l.mRight.x), 1.0f, 1e-6f, "Identity listener has +X right");

    const trans3d moved = trans3d(quatd(0.0, 0.0, 0.0, 1.0), 1.0, flip3::N, vec3d(1.0, 2.0, 3.0));
    l.SetFromTransform(moved);
    iCheckNear(float(l.mPosition.y), 2.0f, 1e-6f, "Listener picks up translation");
    iCheckNear(float(l.mForward.z), -1.0f, 1e-6f, "Translation does not rotate the listener");
}

static void iTestFilterHelpers(void)
{
    printf("filter helpers\n");
    iCheckNear(piSoundSpatializer::OnePoleCoefficient(30000.0f, 48000), 1.0f, 1e-6f,
               "Cutoff above Nyquist bypasses the filter");
    const float c = piSoundSpatializer::OnePoleCoefficient(1500.0f, 48000);
    iCheck(c > 0.0f && c < 1.0f, "Shadow cutoff is a usable one pole coefficient");
    iCheck(piSoundSpatializer::MaxDelaySamples(48000) >= 32, "Delay line covers the interaural delay at 48k");
    iCheck(piSoundSpatializer::MaxDelaySamples(24000) < piSoundSpatializer::MaxDelaySamples(48000),
           "Delay line scales with the sample rate");
}

int main(void)
{
    printf("IMM spatializer conformance\n\n");

    iTestAttenuation();
    iTestConeModifier();
    iTestFrustumModifier();
    iTestBinaural();
    iTestStereoPanReduction();
    iTestAmbisonic();
    iTestListenerTransform();
    iTestFilterHelpers();

    printf("\n%d checks, %d failures\n", gChecks, gFailures);
    return (gFailures == 0) ? 0 : 1;
}
