//
// See piSoundSpatializer.h. Pure math, no platform dependencies.
//
#include <math.h>

#include "piSoundSpatializer.h"

namespace ImmCore
{

    const float piSoundSpatializer::kHeadRadius = 0.0875f;
    const float piSoundSpatializer::kSpeedOfSound = 343.0f;

    static const double kPi = 3.14159265358979323846;

    // Below this distance an emitter is effectively inside the head: panning
    // stops meaning anything and the interaural delay would flip sign on tiny
    // movements, so we fade back to centred.
    static const double kNearFieldRadius = 0.25;

    // A hard pan to a silent ear sounds like a fault rather than a position,
    // and it makes the head shadow filter audible as a gate. Cap the pan.
    static const float kMaxPanDepth = 0.92f;

    static const float kShadowCutoffOpen = 20000.0f;
    static const float kShadowCutoffShut = 1500.0f;

    static inline float iClamp01(float x)
    {
        return (x < 0.0f) ? 0.0f : ((x > 1.0f) ? 1.0f : x);
    }

    static inline float iSmoothstep(float a, float b, float x)
    {
        if (b <= a) return (x < a) ? 0.0f : 1.0f;
        const float t = iClamp01((x - a) / (b - a));
        return t * t * (3.0f - 2.0f * t);
    }

    static inline vec3d iNegate(const vec3d & v)
    {
        return vec3d(-v.x, -v.y, -v.z);
    }

    // Orthonormal basis of an emitter. IMM/GL convention: a layer faces -Z and
    // +Y is up, so the basis Z axis is the reverse of the facing direction.
    static void iVoiceBasis(const piSoundSpatializer::Voice & voice,
                            vec3d * xAxis, vec3d * yAxis, vec3d * zAxis)
    {
        vec3d z = normalizeSafe(iNegate(voice.mForward));
        if (lengthSquared(z) < 0.5) z = vec3d(0.0, 0.0, 1.0);

        vec3d up = normalizeSafe(voice.mUp);
        if (lengthSquared(up) < 0.5) up = vec3d(0.0, 1.0, 0.0);

        vec3d x = cross(up, z);
        if (lengthSquared(x) < 1e-12)
        {
            // Up is parallel to the facing direction - pick any perpendicular.
            up = (::fabs(z.y) < 0.9) ? vec3d(0.0, 1.0, 0.0) : vec3d(1.0, 0.0, 0.0);
            x = cross(up, z);
        }
        x = normalize(x);

        *xAxis = x;
        *yAxis = cross(z, x);
        *zAxis = z;
    }

    void piSoundSpatializer::Listener::SetIdentity(void)
    {
        mPosition = vec3d(0.0, 0.0, 0.0);
        mForward = vec3d(0.0, 0.0, -1.0);
        mUp = vec3d(0.0, 1.0, 0.0);
        mRight = vec3d(1.0, 0.0, 0.0);
    }

    void piSoundSpatializer::Listener::SetFromTransform(const trans3d & listenerToWorld)
    {
        mPosition = listenerToWorld.mTranslation;
        mForward = normalizeSafe((listenerToWorld * vec4d(0.0, 0.0, -1.0, 0.0)).xyz());
        mUp = normalizeSafe((listenerToWorld * vec4d(0.0, 1.0, 0.0, 0.0)).xyz());
        mRight = normalizeSafe((listenerToWorld * vec4d(1.0, 0.0, 0.0, 0.0)).xyz());

        if (lengthSquared(mForward) < 0.5) mForward = vec3d(0.0, 0.0, -1.0);
        if (lengthSquared(mUp) < 0.5) mUp = vec3d(0.0, 1.0, 0.0);
        if (lengthSquared(mRight) < 0.5) mRight = vec3d(1.0, 0.0, 0.0);
    }

    void piSoundSpatializer::Voice::SetDefaults(void)
    {
        mPosition = vec3d(0.0, 0.0, 0.0);
        mForward = vec3d(0.0, 0.0, -1.0);
        mUp = vec3d(0.0, 1.0, 0.0);
        mAttenuation = Attenuation::None;
        mAttenuationMin = 1.0;
        mAttenuationMax = 100.0;
        mModifier = Modifier::None;
        mConeAngleInner = 0.0;
        mConeAngleBand = 0.0;
        mConeAttenOut = 0.0;
        mFrustumAngleInnerX = 0.0;
        mFrustumAngleInnerY = 0.0;
        mFrustumAngleBand = 0.0;
        mFrustumAttenOut = 0.0;
    }

    float piSoundSpatializer::ComputeAttenuation(const Voice & voice, const Listener & listener)
    {
        if (voice.mAttenuation == Attenuation::None)
            return 1.0f;

        const double dmin = (voice.mAttenuationMin > 0.0) ? voice.mAttenuationMin : 0.001;
        const double dmax = (voice.mAttenuationMax > dmin) ? voice.mAttenuationMax : (dmin + 0.001);
        const double d = length(listener.mPosition - voice.mPosition);

        if (d <= dmin) return 1.0f;
        // Audio360 is configured with maxDistanceMute = true by the Windows
        // backend, so the curve ends in silence rather than a floor.
        if (d >= dmax) return 0.0f;

        if (voice.mAttenuation == Attenuation::Linear)
        {
            return float((dmax - d) / (dmax - dmin));
        }

        if (voice.mAttenuation == Attenuation::Logarithmic)
        {
            // Audio360's "factor" is 6 dB of attenuation per doubling of
            // distance at 1.0. The Windows backend picks the factor so the
            // curve lands 30 dB down at the maximum distance; the +0.001 guard
            // is reproduced so the two agree exactly.
            const double span = ::log(dmax / (dmin + 0.001)) / ::log(2.0);
            const double factor = (span > 1e-6) ? (5.0 / span) : 5.0;
            return float(::pow(dmin / d, factor));
        }

        // InverseSquare: amplitude falls as 1/distance (power as 1/distance^2).
        return float(dmin / d);
    }

    float piSoundSpatializer::ComputeModifier(const Voice & voice, const Listener & listener)
    {
        if (voice.mModifier == Modifier::None)
            return 1.0f;

        vec3d xAxis, yAxis, zAxis;
        iVoiceBasis(voice, &xAxis, &yAxis, &zAxis);

        // The listener, expressed in the emitter's own frame.
        const vec3d delta = listener.mPosition - voice.mPosition;
        const vec3d p = vec3d(dot(delta, xAxis), dot(delta, yAxis), dot(delta, zAxis));

        if (voice.mModifier == Modifier::Cone)
        {
            const vec3d pn = normalizeSafe(p);
            // 1 when the listener sits straight down the emitter's -Z axis.
            const float ff = iClamp01(float(-pn.z));

            // Angular space rather than cosine space - it feels much more
            // linear as you move into the cone. Same choice as the Windows
            // backend.
            const float fa = float(voice.mConeAngleInner);
            const float fb = float(voice.mConeAngleInner + voice.mConeAngleBand);
            const float atten = 1.0f - iSmoothstep(fa, fb, ::acosf(ff));
            const float attenOut = float(voice.mConeAttenOut);
            return iClamp01(atten) * (1.0f - attenOut) + attenOut;
        }

        // Frustum. Flip Z so that +Z points along the emitter's facing
        // direction, then project the listener onto the Z=1 plane.
        const vec3d s = vec3d(p.x, p.y, -p.z);

        // NOTE: behind the emitter the Windows backend returns hard silence and
        // ignores mFrustumAttenOut. That is reproduced on purpose - content is
        // mixed against it. Do not "fix" this in one backend only.
        if (s.z < 0.0) return 0.0f;
        if (s.z < 1e-9) return 0.0f;

        const float px = float(s.x / s.z);
        const float py = float(s.y / s.z);

        const float innerX = ::tanf(float(voice.mFrustumAngleInnerX));
        const float innerY = ::tanf(float(voice.mFrustumAngleInnerY));
        const float outerX = ::tanf(float(voice.mFrustumAngleInnerX + voice.mFrustumAngleBand));
        const float outerY = ::tanf(float(voice.mFrustumAngleInnerY + voice.mFrustumAngleBand));

        const float attX = iSmoothstep(0.0f, outerX - innerX, ::fabsf(px) - innerX);
        const float attY = iSmoothstep(0.0f, outerY - innerY, ::fabsf(py) - innerY);
        const float att = 1.0f - ((attX > attY) ? attX : attY);

        // Squared for a more perceptual gradient, as on Windows.
        const float attenOut = float(voice.mFrustumAttenOut);
        return iClamp01(att * att) * (1.0f - attenOut) + attenOut;
    }

    float piSoundSpatializer::OnePoleCoefficient(float cutoffHz, int sampleRate)
    {
        if (sampleRate <= 0) return 1.0f;
        const float nyquist = 0.5f * float(sampleRate);
        if (cutoffHz >= nyquist) return 1.0f;
        if (cutoffHz <= 0.0f) return 0.0f;
        const float c = 1.0f - ::expf(-2.0f * float(kPi) * cutoffHz / float(sampleRate));
        return iClamp01(c);
    }

    int piSoundSpatializer::MaxDelaySamples(int sampleRate)
    {
        // Woodworth's formula peaks at (r/c) * (pi/2 + 1) seconds.
        const float seconds = (kHeadRadius / kSpeedOfSound) * (float(kPi) * 0.5f + 1.0f);
        const int samples = int(::ceilf(seconds * float(sampleRate))) + 2;
        return (samples < 4) ? 4 : samples;
    }

    void piSoundSpatializer::ComputeBinaural(const Voice & voice, const Listener & listener,
                                             int sampleRate, Binaural * result)
    {
        if (result == nullptr) return;

        const float attenuation = ComputeAttenuation(voice, listener);

        const vec3d delta = voice.mPosition - listener.mPosition;
        const double distance = length(delta);

        // Fade the whole spatial treatment out as the emitter enters the head.
        double proximity = distance / kNearFieldRadius;
        if (proximity > 1.0) proximity = 1.0;
        if (proximity < 0.0) proximity = 0.0;

        double lateral = 0.0;
        if (distance > 1e-6)
        {
            const vec3d u = vec3d(delta.x / distance, delta.y / distance, delta.z / distance);
            // Component along the listener's right axis. Using this rather than
            // an azimuth angle keeps sources behind the head lateralised
            // correctly; front/back is not resolved without real HRIRs, which
            // is a known limitation of this renderer.
            lateral = dot(u, listener.mRight);
        }

        float pan = float(lateral * proximity) * kMaxPanDepth;
        if (pan < -1.0f) pan = -1.0f;
        if (pan > 1.0f) pan = 1.0f;

        // Constant power pan: gainL^2 + gainR^2 == 1.
        const float gainL = ::sqrtf(0.5f * (1.0f - pan));
        const float gainR = ::sqrtf(0.5f * (1.0f + pan));

        // Woodworth interaural time difference, applied to the far ear.
        const float theta = ::asinf(pan);
        float itdSeconds = (kHeadRadius / kSpeedOfSound) * (::fabsf(theta) + ::fabsf(::sinf(theta)));
        float itdSamples = itdSeconds * float(sampleRate);
        const float maxDelay = float(MaxDelaySamples(sampleRate) - 2);
        if (itdSamples > maxDelay) itdSamples = maxDelay;
        if (itdSamples < 0.0f) itdSamples = 0.0f;

        // Head shadowing on the far ear only.
        const float shadow = ::fabsf(pan);
        const float shadowCutoff = kShadowCutoffOpen + (kShadowCutoffShut - kShadowCutoffOpen) * shadow;
        const float openCoefficient = OnePoleCoefficient(kShadowCutoffOpen, sampleRate);
        const float shadowCoefficient = OnePoleCoefficient(shadowCutoff, sampleRate);

        result->mGain[0] = attenuation * gainL;
        result->mGain[1] = attenuation * gainR;

        if (pan >= 0.0f)
        {
            // Source is to the right: the left ear is far.
            result->mDelay[0] = itdSamples;
            result->mDelay[1] = 0.0f;
            result->mCutoff[0] = shadowCoefficient;
            result->mCutoff[1] = openCoefficient;
        }
        else
        {
            result->mDelay[0] = 0.0f;
            result->mDelay[1] = itdSamples;
            result->mCutoff[0] = openCoefficient;
            result->mCutoff[1] = shadowCoefficient;
        }
    }

    void piSoundSpatializer::ReduceToStereoPan(const Binaural & binaural, float * gain, float * pan)
    {
        const float left = binaural.mGain[0] * binaural.mGain[0];
        const float right = binaural.mGain[1] * binaural.mGain[1];
        const float power = left + right;

        if (gain != nullptr)
            *gain = ::sqrtf(power);

        if (pan != nullptr)
        {
            float p = (power > 1e-12f) ? ((right - left) / power) : 0.0f;
            if (p < -1.0f) p = -1.0f;
            if (p > 1.0f) p = 1.0f;
            *pan = p;
        }
    }

    void piSoundSpatializer::ComputeAmbisonicDecode(const Voice & voice, const Listener & listener,
                                                    Ambisonic convention, float result[2][4])
    {
        if (result == nullptr) return;

        // Two virtual cardioids, splayed either side of where the listener is
        // facing. 60 degrees keeps a solid front image while still swinging
        // convincingly when the head turns.
        const double splay = 60.0 * kPi / 180.0;
        const double cs = ::cos(splay);
        const double sn = ::sin(splay);

        const vec3d forward = listener.mForward;
        const vec3d right = listener.mRight;

        const vec3d earDirection[2] =
        {
            normalizeSafe(vec3d(forward.x * cs - right.x * sn,
                                forward.y * cs - right.y * sn,
                                forward.z * cs - right.z * sn)),
            normalizeSafe(vec3d(forward.x * cs + right.x * sn,
                                forward.y * cs + right.y * sn,
                                forward.z * cs + right.z * sn))
        };

        // The bed carries its own orientation, so express the virtual mics in
        // the bed's frame before decoding.
        vec3d xAxis, yAxis, zAxis;
        iVoiceBasis(voice, &xAxis, &yAxis, &zAxis);

        for (int ear = 0; ear < 2; ear++)
        {
            const vec3d d = earDirection[ear];
            const vec3d local = vec3d(dot(d, xAxis), dot(d, yAxis), dot(d, zAxis));

            // GL (x right, y up, z back) -> ambisonic (X front, Y left, Z up).
            const float aX = float(-local.z);
            const float aY = float(-local.x);
            const float aZ = float(local.y);

            if (convention == Ambisonic::AmbiX)
            {
                // ACN ordering: W, Y, Z, X. Cardioid = 0.5 * (W + dot(d, XYZ)).
                result[ear][0] = 0.5f;
                result[ear][1] = 0.5f * aY;
                result[ear][2] = 0.5f * aZ;
                result[ear][3] = 0.5f * aX;
            }
            else
            {
                // FuMa ordering: W, X, Y, Z, with W stored 1/sqrt(2) down.
                result[ear][0] = 0.5f * 1.41421356f;
                result[ear][1] = 0.5f * aX;
                result[ear][2] = 0.5f * aY;
                result[ear][3] = 0.5f * aZ;
            }
        }
    }

} // namespace ImmCore
