//
// Host-side mixing policy for IMM sound layers.
//
// The .imm carries what the author intended (gain, animated volume, spatial
// placement). This is the separate, host-owned layer on top of that: a fader,
// mute and solo per sound layer plus a small set of buses, so an application
// can turn the narration down and keep the score up without touching the
// document.
//
// The composition itself lives here as a pure function with no dependencies so
// it can be unit tested - see libImmCore/src/libSound/tests. Everything else
// about the mixer is bookkeeping around this one expression.
//
#pragma once

namespace ImmPlayer
{
    // Number of mix buses. Small and fixed: this is a utility mixer for
    // balancing a piece, not a DAW.
    static const int kNumSoundBuses = 8;

    // Composes the host mixer's contribution to one sound layer's gain. The
    // result multiplies the authored volume - it never replaces it.
    //
    // Precedence, matching what people expect from a mixing desk:
    //   - mute always wins, including on a soloed layer;
    //   - while anything is soloed, everything not soloed is silent.
    inline float ComputeSoundMixGain(float layerVolume, bool muted, bool soloed,
                                     bool anySoloActive, float busVolume)
    {
        if (muted) return 0.0f;
        if (anySoloActive && !soloed) return 0.0f;

        float gain = layerVolume * busVolume;
        if (gain < 0.0f) gain = 0.0f;
        return gain;
    }
}
