using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace ImmPlayer
{
    /// <summary>
    /// Starts XR only for scenes that explicitly include this component.
    /// Keep project-wide XR auto-start disabled so desktop scenes do not enter
    /// OpenXR MultiPass just because the project has an XR loader configured.
    /// </summary>
    public sealed class XrSceneBootstrap : MonoBehaviour
    {
        private bool _startedSubsystems;
        private bool _initializedLoader;

        private IEnumerator Start()
        {
            XRManagerSettings manager = XRGeneralSettings.Instance?.Manager;
            if (manager == null)
            {
                Debug.LogError("[IMM_XR_SCENE_BOOTSTRAP] XR manager settings are missing.");
                yield break;
            }

            if (manager.activeLoader == null)
            {
                yield return manager.InitializeLoader();
                _initializedLoader = manager.activeLoader != null;
            }

            if (manager.activeLoader == null)
            {
                Debug.LogError("[IMM_XR_SCENE_BOOTSTRAP] XR loader initialization failed.");
                yield break;
            }

            manager.StartSubsystems();
            _startedSubsystems = true;

            if (!FlagFileHas("IMM_UNITY_NO_FLOOR_ORIGIN"))
                yield return SetFloorTrackingOrigin();
        }

        /// <summary>
        /// Native-viewer parity: appImmViewer sets vrapi tracking space LOCAL_FLOOR,
        /// so authored floor anchors sit on the physical floor and the runtime knows
        /// the real floor plane. Unity's default is Device (eye-level at startup),
        /// which leaves floor-typed spawns guessing the user's height.
        /// Kill-switch: IMM_UNITY_NO_FLOOR_ORIGIN in imm_debug_flags.txt.
        /// </summary>
        private IEnumerator SetFloorTrackingOrigin()
        {
            var inputSubsystems = new List<XRInputSubsystem>();
            float deadline = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < deadline)
            {
                SubsystemManager.GetSubsystems(inputSubsystems);
                foreach (XRInputSubsystem input in inputSubsystems)
                {
                    if (!input.running)
                        continue;
                    if (input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor))
                    {
                        Debug.Log($"[IMM_XR_ORIGIN] tracking origin Floor requested, now={input.GetTrackingOriginMode()}");
                        yield break;
                    }
                }
                yield return null;
            }
            Debug.LogWarning("[IMM_XR_ORIGIN] Floor tracking origin not accepted within 10s; staying on device default");
        }

        private static bool FlagFileHas(string name)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "imm_debug_flags.txt");
                if (System.IO.File.Exists(path))
                    foreach (string line in System.IO.File.ReadAllLines(path))
                        if (string.Equals(line.Trim(), name, System.StringComparison.OrdinalIgnoreCase))
                            return true;
            }
            catch (System.Exception)
            {
            }
#endif
            return false;
        }

        private void OnDisable()
        {
            XRManagerSettings manager = XRGeneralSettings.Instance?.Manager;
            if (manager == null)
                return;

            if (_startedSubsystems)
            {
                manager.StopSubsystems();
                _startedSubsystems = false;
            }

            if (_initializedLoader)
            {
                manager.DeinitializeLoader();
                _initializedLoader = false;
            }
        }
    }
}
