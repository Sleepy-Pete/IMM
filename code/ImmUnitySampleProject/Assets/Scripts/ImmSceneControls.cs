using UnityEngine;
using UnityEngine.XR;
using ImmPlayer;

// Quest-controller scene controls for reviewing IMM documents, layered on top
// of ImmFreeFly (which owns the thumbsticks). Uses only plugin APIs that exist
// today (ImmNativePlugin playback/chapter/restart) plus rig manipulation:
//
//   Right A            : pause / resume toggle
//   Right B            : restart document
//   Left  X            : previous chapter (SkipBack)
//   Left  Y            : next chapter (SkipForward)
//   Grip (one hand)    : grab the world and drag it
//   Grip (both hands)  : scale the world + yaw it about the grab midpoint
//
// World manipulation moves ImmFreeFly's origin rig, so it composes with stick
// locomotion and the XR-params matrix path exactly like flying does.
public class ImmSceneControls : MonoBehaviour
{
    const float MinWorldScale = 0.05f;
    const float MaxWorldScale = 20f;

    // First loaded document. The viewer loads exactly one (LoadFromMemory logs "ID: 0").
    const int DocId = 0;

    bool _paused;
    bool _aLatch, _bLatch, _xLatch, _yLatch, _burstLatch;

    bool _leftGripping, _rightGripping;
    Vector3 _lastLeftWorld, _lastRightWorld;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<ImmSceneControls>() != null)
            return;
        var go = new GameObject("ImmSceneControls");
        go.AddComponent<ImmSceneControls>();
        Debug.Log("[IMM_CONTROLS] bootstrap: component created");
    }

    void Update()
    {
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        HandleButtons(left, right);
        HandleGrabs(left, right);
    }

    void HandleButtons(InputDevice left, InputDevice right)
    {
        if (Pressed(right, CommonUsages.primaryButton, ref _aLatch))
        {
            // Quill-player parity: when the story is WAITING at a stop (title
            // card included), play means "continue past the stop" - the doc's
            // own state, not a chapter-number heuristic (the previous
            // GetCurrentChapter()==0 guard never matched after a skip-back).
            ImmNativePlugin.GetDocumentState(out DocumentState docState, DocId);
            if (docState.playbackState == 3 /* PlaybackState::Waiting */)
            {
                ImmNativePlugin.Continue(DocId);
                _paused = false;
                Debug.Log("[IMM_CONTROLS] continue past stop");
            }
            else
            {
                _paused = !_paused;
                if (_paused) ImmNativePlugin.Pause(DocId); else ImmNativePlugin.Resume(DocId);
                Debug.Log($"[IMM_CONTROLS] {( _paused ? "paused" : "resumed")}");
            }
        }
        if (Pressed(right, CommonUsages.secondaryButton, ref _bLatch))
        {
            ImmNativePlugin.Restart(DocId);
            _paused = false;
            Debug.Log("[IMM_CONTROLS] restart");
        }
        if (Pressed(left, CommonUsages.primaryButton, ref _xLatch))
        {
            ImmNativePlugin.SkipBack(DocId);
            Debug.Log("[IMM_CONTROLS] skip back");
        }
        if (Pressed(left, CommonUsages.secondaryButton, ref _yLatch))
        {
            ImmNativePlugin.SkipForward(DocId);
            Debug.Log("[IMM_CONTROLS] skip forward");
        }
        // Right stick CLICK = stereo-pair capture burst - press it the moment a
        // glitch is seen; 8 same-frame eye pairs land in persistentDataPath.
        // (Left stick click keeps the ImmFreeFly origin-reset role.)
        if (Pressed(right, CommonUsages.primary2DAxisClick, ref _burstLatch))
        {
            ImmPlayerManager.RequestEyeBurst();
            Debug.Log("[IMM_CONTROLS] eye burst capture triggered");
        }
    }

    static bool Pressed(InputDevice device, InputFeatureUsage<bool> usage, ref bool latch)
    {
        bool now = device.isValid && device.TryGetFeatureValue(usage, out bool v) && v;
        bool edge = now && !latch;
        latch = now;
        return edge;
    }

    void HandleGrabs(InputDevice left, InputDevice right)
    {
        Transform origin = ImmFreeFly.Origin;
        if (origin == null)
            return;

        bool leftGrip = GripHeld(left);
        bool rightGrip = GripHeld(right);
        Vector3 leftWorld = _lastLeftWorld;
        Vector3 rightWorld = _lastRightWorld;
        bool haveLeft = leftGrip && TryHandWorld(left, origin, out leftWorld);
        bool haveRight = rightGrip && TryHandWorld(right, origin, out rightWorld);

        if (haveLeft && haveRight)
        {
            if (_leftGripping && _rightGripping)
            {
                // Two-hand: scale about the midpoint + yaw with the hands' spin.
                Vector3 prevMid = (_lastLeftWorld + _lastRightWorld) * 0.5f;
                Vector3 mid = (leftWorld + rightWorld) * 0.5f;
                Vector3 prevSpan = _lastRightWorld - _lastLeftWorld;
                Vector3 span = rightWorld - leftWorld;

                float prevLen = prevSpan.magnitude;
                float len = span.magnitude;
                if (prevLen > 0.01f && len > 0.01f)
                {
                    // Growing hand span shrinks the world (pull it apart to zoom in).
                    float ratio = prevLen / len;
                    float current = origin.localScale.x;
                    float clamped = Mathf.Clamp(current * ratio, MinWorldScale, MaxWorldScale);
                    ratio = clamped / current;
                    ScaleAround(origin, mid, ratio);
                }

                float prevYaw = Mathf.Atan2(prevSpan.x, prevSpan.z) * Mathf.Rad2Deg;
                float yaw = Mathf.Atan2(span.x, span.z) * Mathf.Rad2Deg;
                origin.RotateAround(mid, Vector3.up, Mathf.DeltaAngle(yaw, prevYaw));

                origin.position += prevMid - mid;
            }
        }
        else if (haveLeft && _leftGripping && !rightGrip)
        {
            origin.position += _lastLeftWorld - leftWorld;
        }
        else if (haveRight && _rightGripping && !leftGrip)
        {
            origin.position += _lastRightWorld - rightWorld;
        }

        // Re-derive hand world positions after moving the origin so the next
        // frame's delta is measured against the new rig pose.
        if (haveLeft && TryHandWorld(left, origin, out Vector3 newLeft)) _lastLeftWorld = newLeft;
        else if (haveLeft) _lastLeftWorld = leftWorld;
        if (haveRight && TryHandWorld(right, origin, out Vector3 newRight)) _lastRightWorld = newRight;
        else if (haveRight) _lastRightWorld = rightWorld;

        _leftGripping = haveLeft;
        _rightGripping = haveRight;
    }

    static bool GripHeld(InputDevice device)
    {
        return device.isValid && device.TryGetFeatureValue(CommonUsages.gripButton, out bool g) && g;
    }

    static bool TryHandWorld(InputDevice device, Transform origin, out Vector3 world)
    {
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 tracked))
        {
            world = origin.TransformPoint(tracked);
            return true;
        }
        world = Vector3.zero;
        return false;
    }

    static void ScaleAround(Transform t, Vector3 pivot, float ratio)
    {
        t.localScale *= ratio;
        t.position = pivot + (t.position - pivot) * ratio;
    }
}
