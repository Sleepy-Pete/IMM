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

        if (leftClick || rightClick)
        {
            if (!_resetLatch)
            {
                _resetLatch = true;
                _origin.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Debug.Log("[IMM_FREEFLY] origin reset");
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
