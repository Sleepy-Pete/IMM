//
// Conformance test for the host mixer's gain policy.
//
// The composition is one expression, but it is the expression that decides
// whether a piece is audible, so it gets pinned down here rather than being
// re-derived from the UI every time someone touches it.
//
// Build + run:  code/projects/windows/run-audio-conformance.ps1
//
#include <math.h>
#include <stdio.h>

#include "../soundMix.h"

using namespace ImmPlayer;

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

static void iTestDefaults(void)
{
    printf("defaults\n");

    // A host that never touches the mixer must hear exactly what the document
    // authored - this is the single most important property here.
    iCheckNear(ComputeSoundMixGain(1.0f, false, false, false, 1.0f), 1.0f, 1e-6f,
               "Untouched mixer is unity gain");
}

static void iTestFader(void)
{
    printf("fader\n");

    iCheckNear(ComputeSoundMixGain(0.5f, false, false, false, 1.0f), 0.5f, 1e-6f,
               "Layer fader scales the gain");
    iCheckNear(ComputeSoundMixGain(0.0f, false, false, false, 1.0f), 0.0f, 1e-6f,
               "Layer fader at zero is silent");
    iCheckNear(ComputeSoundMixGain(2.0f, false, false, false, 1.0f), 2.0f, 1e-6f,
               "Layer fader can amplify above unity");
    iCheckNear(ComputeSoundMixGain(-1.0f, false, false, false, 1.0f), 0.0f, 1e-6f,
               "Negative gain is clamped, never phase inverted");
    iCheckNear(ComputeSoundMixGain(0.5f, false, false, false, -2.0f), 0.0f, 1e-6f,
               "Negative bus gain is clamped too");
}

static void iTestBus(void)
{
    printf("bus\n");

    iCheckNear(ComputeSoundMixGain(0.5f, false, false, false, 0.5f), 0.25f, 1e-6f,
               "Layer and bus faders multiply");
    iCheckNear(ComputeSoundMixGain(1.0f, false, false, false, 0.0f), 0.0f, 1e-6f,
               "A bus at zero silences its layers");
    iCheck(kNumSoundBuses >= 4, "There are enough buses to be useful");
}

static void iTestMute(void)
{
    printf("mute\n");

    iCheckNear(ComputeSoundMixGain(1.0f, true, false, false, 1.0f), 0.0f, 1e-6f,
               "Mute silences the layer");
    iCheckNear(ComputeSoundMixGain(1.0f, true, true, true, 1.0f), 0.0f, 1e-6f,
               "Mute wins over solo on the same layer");

    // Unmuting must restore exactly what was there - the fader is untouched.
    iCheckNear(ComputeSoundMixGain(0.7f, false, false, false, 1.0f), 0.7f, 1e-6f,
               "Unmuting restores the fader value");
}

static void iTestSolo(void)
{
    printf("solo\n");

    // Nothing soloed: everyone is heard.
    iCheckNear(ComputeSoundMixGain(1.0f, false, false, false, 1.0f), 1.0f, 1e-6f,
               "No solo active leaves the mix alone");

    // Something soloed elsewhere: this layer drops out.
    iCheckNear(ComputeSoundMixGain(1.0f, false, false, true, 1.0f), 0.0f, 1e-6f,
               "A solo elsewhere silences this layer");

    // The soloed layer itself is heard, still respecting its own fader and bus.
    iCheckNear(ComputeSoundMixGain(1.0f, false, true, true, 1.0f), 1.0f, 1e-6f,
               "The soloed layer is heard");
    iCheckNear(ComputeSoundMixGain(0.5f, false, true, true, 0.5f), 0.25f, 1e-6f,
               "A soloed layer still obeys its fader and bus");
}

int main(void)
{
    printf("IMM sound mixer conformance\n\n");

    iTestDefaults();
    iTestFader();
    iTestBus();
    iTestMute();
    iTestSolo();

    printf("\n%d checks, %d failures\n", gChecks, gFailures);
    return (gFailures == 0) ? 0 : 1;
}
