using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Free-fly review controls on the Quest thumbsticks. Moves a rig ABOVE the
// tracked camera (XR tracking keeps overwriting the camera's own transform, so
// locomotion must move a parent), letting the scene be inspected from any
// angle while head tracking stays live.
//
//   Left stick   : fly along gaze (y) / strafe (x)
//   Right stick  : world up-down (y) / smooth yaw around the viewer (x)
//   Stick click  : reset the rig to the physical origin
//
// The rig's world->local matrix is published to ImmPlayerManager so the
// Vulkan XR-params matrix path can fold locomotion into the views it hands
// the native renderer (gated there by IMM_UNITY_VK_FREEFLY_COMPOSE).
public class ImmFreeFly : MonoBehaviour
{
    const float MoveSpeed = 2.5f;      // m/s at full stick
    const float VerticalSpeed = 2.0f;  // m/s
    const float YawSpeed = 120f;       // deg/s
    const float Deadzone = 0.15f;

    Transform _origin;
    Camera _cam;
    bool _resetLatch;

    // The locomotion rig above the tracked camera, shared with ImmSceneControls
    // (grip-grab world manipulation). Null until the camera is found.
    public static Transform Origin { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<ImmFreeFly>() != null)
            return;
        var go = new GameObject("ImmFreeFly");
        go.AddComponent<ImmFreeFly>();
        Debug.Log("[IMM_FREEFLY] bootstrap: component created");
    }

    void Update()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null)
                return;
        }
        if (_origin == null)
        {
            // Parent the tracked camera under a rig at identity so its world
            // pose is unchanged at creation and locomotion is purely additive.
            _origin = _cam.transform.parent;
            if (_origin == null)
            {
                var rig = new GameObject("ImmFreeFlyOrigin");
                rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                _cam.transform.SetParent(rig.transform, true);
                _origin = rig.transform;
                Debug.Log("[IMM_FREEFLY] camera reparented under ImmFreeFlyOrigin");
            }
        }

        Vector2 left = ReadStick(XRNode.LeftHand, out bool leftClick);
        Vector2 right = ReadStick(XRNode.RightHand, out bool rightClick);
        _ = rightClick; // right stick click belongs to ImmSceneControls' capture burst

        if (leftClick)
        {
            if (!_resetLatch)
            {
                _resetLatch = true;
                RecenterToView();
            }
        }
        else
        {
            _resetLatch = false;
        }

        float dt = Time.deltaTime;
        Transform head = _cam.transform;

        if (Mathf.Abs(left.y) > Deadzone)
            _origin.position += head.forward * (left.y * MoveSpeed * dt);
        if (Mathf.Abs(left.x) > Deadzone)
            _origin.position += head.right * (left.x * MoveSpeed * dt);
        if (Mathf.Abs(right.y) > Deadzone)
            _origin.position += Vector3.up * (right.y * VerticalSpeed * dt);
        if (Mathf.Abs(right.x) > Deadzone)
            _origin.RotateAround(head.position, Vector3.up, right.x * YawSpeed * dt);

        ImmPlayer.ImmPlayerManager.ExternalWorldToTracking = _origin.worldToLocalMatrix;
        Origin = _origin;
    }

    // Left stick click: RECENTER. Preferred behavior snaps back to the ACTIVE
    // authored Quill spawn viewpoint (position, yaw, and authored scale) - the
    // anchors baked into the document. Falls back to re-fronting the content to
    // the current gaze when no document/spawn machinery is present.
    void RecenterToView()
    {
        var features = FindFirstObjectByType<ImmPlayer.ImmFeatureExamples>();
        if (features != null)
        {
            features.ReapplyActiveSpawnAreaViewpoint();
            Debug.Log("[IMM_FREEFLY] recentered to authored spawn viewpoint");
            return;
        }
        Transform head = _cam.transform;
        float yaw = head.localEulerAngles.y;
        Quaternion r = Quaternion.Euler(0f, -yaw, 0f);
        float s = _origin.localScale.x;
        Vector3 p = head.localPosition;
        Vector3 flat = new Vector3(p.x, 0f, p.z);
        _origin.rotation = r;
        _origin.position = -(r * flat) * s;
        Debug.Log("[IMM_FREEFLY] recentered to current view (no document)");
    }

    static readonly List<InputDevice> _devices = new List<InputDevice>();

    static Vector2 ReadStick(XRNode node, out bool click)
    {
        click = false;
        InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return Vector2.zero;
        device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out click);
        return device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 v) ? v : Vector2.zero;
    }
}
