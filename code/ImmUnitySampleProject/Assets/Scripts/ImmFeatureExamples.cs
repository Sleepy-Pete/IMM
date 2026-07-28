using System.IO;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.XR;

namespace ImmPlayer
{
    /// <summary>
    /// Example component showing how to use IMM runtime features from C#.
    /// </summary>
    // Runs before default-order scripts (OpenXRFlyRig etc.): the animated
    // viewpoint driver must lay down the authored rig pose BEFORE fly input
    // adds its per-frame delta, which LateUpdate then absorbs into the saved
    // rig offset. Reversed order would overwrite fly input every frame.
    [DefaultExecutionOrder(-100)]
    public class ImmFeatureExamples : MonoBehaviour
    {
        public enum LoadSource
        {
            FileSystem,       // Load from absolute path (Editor/Desktop only)
            StreamingAssets   // Load from StreamingAssets folder (works on Android)
        }

        private const string DiagPrefix = "[IMM_DIAG] ";
        private static bool IsEnvFlagEnabled(string name)
        {
            string value = System.Environment.GetEnvironmentVariable(name);
            return !string.IsNullOrEmpty(value) && value != "0";
        }

        private static bool ShouldRunPostLoadFeature(string featureName, string disableFlag)
        {
            if (IsEnvFlagEnabled("IMM_UNITY_DISABLE_FEATURE_POST_LOAD"))
            {
                Debug.Log($"{DiagPrefix}Skipping post-load {featureName}: IMM_UNITY_DISABLE_FEATURE_POST_LOAD=1");
                return false;
            }

            if (IsEnvFlagEnabled(disableFlag))
            {
                Debug.Log($"{DiagPrefix}Skipping post-load {featureName}: {disableFlag}=1");
                return false;
            }

            Debug.Log($"{DiagPrefix}Starting post-load {featureName}");
            return true;
        }

        [System.Serializable]
        public struct LayerListEntry
        {
            public int Index;
            public int Id;
            public ImmDocument.LayerType Type;
            public string Name;
            public string FullName;
        }

        [Header("Document")]
        [SerializeField] private LoadSource loadSource = LoadSource.StreamingAssets;
        [Tooltip("For FileSystem mode only - ignored when using StreamingAssets")]
        [SerializeField] private string directoryPath = "";
        [SerializeField, HideInInspector] private string selectedFileName = "";
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool autoPlay = true;
        [SerializeField] private Transform documentTransform;

        [Header("Background")]
        [SerializeField] private bool applyDocumentBackgroundColor = false;
        [SerializeField] private Camera backgroundCamera;

        [Header("Viewpoint")]
        [SerializeField] private bool applySpawnAreaToViewpoint = true;
        [SerializeField] private Transform spawnAreaTargetTransform;
        [SerializeField] private bool keepCurrentViewHeightForFloorAreas = true;
        [SerializeField] private bool constrainViewpointRotationToYawInXR = true;

        [Header("Layer Target")]
        [SerializeField, HideInInspector] private int selectedLayerIndex = -1;

        [Header("Layer Edits")]
        [SerializeField] private bool layerVisible = true;
        [SerializeField][Range(0f, 1f)] private float layerOpacity = 1.0f;
        [SerializeField] private Vector3 layerPosition = Vector3.zero;
        [SerializeField] private Vector3 layerEuler = Vector3.zero;
        [SerializeField] private float layerScale = 1.0f;

        [Header("Document Status (Read Only)")]
        public ImmDocument.DocumentStateInfo documentState;
        public ImmDocument.DocumentInfoFlags documentInfoFlags;
        public int chapterCount;
        public int currentChapter;
        public int targetChapter;
        public int layerCount;
        public int spawnAreaCount;
        public int activeSpawnAreaId;
        public int currentSpawnAreaIndex;
        public int targetSpawnAreaIndex;
        public Bounds documentBounds;

        [Header("Selected Layer Status (Read Only)")]
        public int selectedLayerId;
        public string selectedLayerName;
        public ImmDocument.LayerType selectedLayerType;
        public int selectedLayerParentId;
        public bool selectedLayerLoaded;
        public bool selectedLayerVisible;
        public float selectedLayerOpacity;
        public bool selectedLayerHasBounds;
        public Bounds selectedLayerBounds;
        public bool selectedLayerVisibilityOverrideEnabled;
        public bool selectedLayerVisibilityOverrideValue;

        public string currentFilePath;
        public ImmDocument.LayerInfo[] layers;
        public ImmDocument.SpawnAreaInfo[] spawnAreas;
        public LayerListEntry[] layerList;

        private ImmDocument _doc;
        private bool _isApplyingEdits;
        private bool _isSyncingSelection;
        private int _lastSelectedLayerIndex = int.MinValue;
        private Coroutine _visibilityDiagCoroutine;
        private Coroutine _initialSpawnAreaCoroutine;
        private Coroutine _spawnAreaApplyCoroutine;
        private Coroutine _chapterSyncCoroutine;
        private bool _isDocumentTransformDirty;
        private Vector3 _lastLayerPosition;
        private Vector3 _lastLayerEuler;
        private float _lastLayerScale = 1.0f;
        private bool _lastLayerVisible;
        private float _lastLayerOpacity;
        private int[] _spawnAreaIds = new int[0];

        // ---- Animated viewpoint driver (Quill parity, QuantumRace) ----
        // Quill can animate the viewer spot through space (transform keyframes
        // on the spawn-area layer or its parents) and re-target it mid-story
        // (MakeDefault keyframes -> needs-update signal). Mirror of
        // appImmViewer viewer.cpp GlobalWork: needs-update => hard re-anchor on
        // the authored initial spawn; animated+playing => follow the live pose
        // every frame, composed with the user's saved rig offset (captured in
        // LateUpdate so fly/snap-turn/recenter are absorbed, and the head is
        // NEVER re-compensated per frame - look/lean stays free).
        private const string ViewpointLogPrefix = "[IMM_VIEWPOINT] ";
        private bool _viewpointDriverEnabled;
        private bool _viewpointAnchored;      // a one-shot spawn apply has run
        private bool _hasRigOffset;           // offset captured since last anchor
        private Vector3 _rigOffsetInSpawnSpacePos;
        private Quaternion _rigOffsetInSpawnSpaceRot = Quaternion.identity;
        private bool _viewpointDriverActiveLogged;
        private int _viewpointFrameCounter;
        private int _loadBeatFrames;
        private float _nextLoadBeatTime;

        private static bool IsDebugFlagSet(string name)
        {
            return IsEnvFlagEnabled(name) || ReadFlagFileIntValue(name, 0) != 0;
        }

        private void Start()
        {
            Debug.Log($"{DiagPrefix}ImmFeatureExamples Start()");

            // Kill-switch: IMM_UNITY_NO_ANIMATED_VIEWPOINT restores the
            // pre-driver behavior exactly (one-shot applies, active-id skips,
            // serialized yaw constraint).
            _viewpointDriverEnabled = !IsDebugFlagSet("IMM_UNITY_NO_ANIMATED_VIEWPOINT");
            if (_viewpointDriverEnabled)
            {
                // Code default under the driver: FULL authored orientation
                // (Quill parity - QR's viewer spot banks and pitches).
                // IMM_UNITY_VIEWPOINT_YAW_ONLY is the comfort A/B.
                constrainViewpointRotationToYawInXR = IsDebugFlagSet("IMM_UNITY_VIEWPOINT_YAW_ONLY");
                Debug.Log($"{ViewpointLogPrefix}animated viewpoint driver ARMED (yawOnly={constrainViewpointRotationToYawInXR}, kill IMM_UNITY_NO_ANIMATED_VIEWPOINT)");
            }
            else
            {
                Debug.Log($"{ViewpointLogPrefix}animated viewpoint driver DISABLED by flag");
            }

            CacheLayerTransform();
            CacheLayerVisuals();
            if (loadOnStart)
            {
                LoadDocument();
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;
            if (_isApplyingEdits || _isSyncingSelection)
                return;

            if (selectedLayerIndex != _lastSelectedLayerIndex)
            {
                SyncLayerFieldsFromSelection();
                return;
            }

            if (HasLayerVisualChanged())
            {
                ApplyLayerEdits(false, applyTransform: false);
                CacheLayerVisuals();
            }

            if (HasLayerTransformChanged())
            {
                ApplyLayerEdits(false, applyVisibility: false, applyOpacity: false, applyTransform: true, logTransformChange: true);
                CacheLayerTransform();
                return;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || _doc == null)
                return;

            // Load-freeze forensics: the app renders ~0 frames during the big
            // decode (VrApi App=70s, user reads it as a crash). This heartbeat
            // decides WHERE it blocks: gaps here = main thread blocked; steady
            // beats while VrApi shows FPS=1 = render/plugin-event side.
            if (!_doc.IsSequenceReady())
            {
                _loadBeatFrames++;
                if (Time.realtimeSinceStartup >= _nextLoadBeatTime)
                {
                    Debug.Log($"[IMM_LOADBEAT] t={Time.realtimeSinceStartup:F1} frames={_loadBeatFrames} loading");
                    _nextLoadBeatTime = Time.realtimeSinceStartup + 1.0f;
                }
            }

            UpdateAnimatedViewpoint();

            if (documentTransform != null && documentTransform.hasChanged)
            {
                ApplyDocumentTransform();
                documentTransform.hasChanged = false;
            }

            if (HasLayerVisualChanged())
            {
                ApplyLayerEdits(false, applyTransform: false);
                CacheLayerVisuals();
            }

            if (HasLayerTransformChanged())
            {
                ApplyLayerEdits(false, applyVisibility: false, applyOpacity: false, applyTransform: true, logTransformChange: true);
                CacheLayerTransform();
            }

            if (_isDocumentTransformDirty)
            {
                ApplyDocumentTransform();
                _isDocumentTransformDirty = false;
            }
        }

        // Per-frame half of the animated viewpoint driver. Runs BEFORE fly
        // input (DefaultExecutionOrder): lays down authoredPose ∘ savedOffset;
        // whatever fly/snap-turn add afterwards is absorbed back into the
        // offset by LateUpdate. The user's head pose is never consulted here -
        // only one-shot re-anchors compensate for the head.
        private void UpdateAnimatedViewpoint()
        {
            if (!_viewpointDriverEnabled || _doc == null || !_doc.IsLoaded)
                return;

            // Authored hard jump: playback (or a skip) crossed a MakeDefault
            // keyframe. Re-anchor on the authored initial spawn area with the
            // full head-compensated one-shot - "skips always land in the spot".
            if (_doc.GetSpawnAreaNeedsUpdate())
            {
                _doc.ClearSpawnAreaNeedsUpdate();
                int jumpId = _doc.GetInitialSpawnAreaId();
                if (jumpId >= 0)
                {
                    _doc.SetActiveSpawnAreaId(jumpId);
                    SyncSpawnAreaSelection();
                    Debug.Log($"{ViewpointLogPrefix}authored jump -> spawn {jumpId} (needs-update consumed)");
                    ApplySpawnAreaViewpoint(jumpId);
                    activeSpawnAreaId = jumpId;
                }
                return;
            }

            if (!_viewpointAnchored || !_hasRigOffset)
                return;

            // Viewer parity: follow while not user-paused (Waiting at a stop
            // still follows; the timeline is frozen there so the pose is too).
            var playback = _doc.GetStateInfo().Playback;
            if (playback == ImmDocument.PlaybackState.Paused ||
                playback == ImmDocument.PlaybackState.PausedAndHidden)
                return;

            int active = _doc.GetActiveSpawnAreaId();
            if (active < 0)
                return;

            Transform target = ResolveSpawnAreaTargetTransform();
            if (target == null)
                return;

            Transform documentRoot = documentTransform != null ? documentTransform : transform;
            if (!_doc.TryGetSpawnAreaWorldPoseAndScale(active, documentRoot, out Pose spawnPose, out float spawnScale, out bool animated))
                return;
            if (!animated)
                return; // static spawn areas keep the one-shot behavior, zero per-frame writes

            Quaternion spawnRot = EffectiveViewpointRotation(spawnPose.rotation);
            Vector3 rigPos = spawnPose.position + spawnRot * (_rigOffsetInSpawnSpacePos * spawnScale);
            Quaternion rigRot = spawnRot * _rigOffsetInSpawnSpaceRot;
            target.localScale = Vector3.one * spawnScale;
            target.SetPositionAndRotation(rigPos, rigRot);

            if (!_viewpointDriverActiveLogged)
            {
                _viewpointDriverActiveLogged = true;
                Debug.Log($"{ViewpointLogPrefix}animated viewpoint driver ACTIVE: following spawn {active} (yawOnly={constrainViewpointRotationToYawInXR})");
            }
            _viewpointFrameCounter++;
            if ((_viewpointFrameCounter % 144) == 0) // ~2s at 72fps; piLog wraps fast, stay quiet
            {
                Debug.Log($"{ViewpointLogPrefix}anim spawn={active} pos={spawnPose.position:F2} scale={spawnScale:F3} rig={rigPos:F2}");
            }
        }

        // Capture half of the driver: express the rig's end-of-frame pose in
        // the (possibly animated) spawn area's frame. Fly input, snap turns,
        // recenters and one-shot re-anchors all funnel into this offset, so
        // the next Update's apply preserves them on top of the authored pose.
        private void LateUpdate()
        {
            if (!_viewpointDriverEnabled || !_viewpointAnchored || _doc == null || !_doc.IsLoaded)
                return;

            int active = _doc.GetActiveSpawnAreaId();
            if (active < 0)
                return;

            Transform target = ResolveSpawnAreaTargetTransform();
            if (target == null)
                return;

            Transform documentRoot = documentTransform != null ? documentTransform : transform;
            if (!_doc.TryGetSpawnAreaWorldPoseAndScale(active, documentRoot, out Pose spawnPose, out float spawnScale, out bool animated))
                return;
            if (!animated)
                return;

            Quaternion spawnRot = EffectiveViewpointRotation(spawnPose.rotation);
            Quaternion invSpawnRot = Quaternion.Inverse(spawnRot);
            _rigOffsetInSpawnSpacePos = (invSpawnRot * (target.position - spawnPose.position)) / spawnScale;
            _rigOffsetInSpawnSpaceRot = invSpawnRot * target.rotation;
            _hasRigOffset = true;
        }

        // Single source of truth for the yaw constraint: the driver and the
        // one-shot apply must agree, or the offset round-trip would fight the
        // anchor. Full authored orientation is the code default under the
        // driver (IMM_UNITY_VIEWPOINT_YAW_ONLY for the comfort A/B).
        private Quaternion EffectiveViewpointRotation(Quaternion authored)
        {
            if (constrainViewpointRotationToYawInXR && XRSettings.enabled)
                return Quaternion.Euler(0.0f, authored.eulerAngles.y, 0.0f);
            return authored;
        }

        public void LoadDocument()
        {
            StartCoroutine(LoadDocumentCoroutine());
        }

        public bool PickRandomStreamingAssetsFile()
        {
            string dirPath = Application.streamingAssetsPath;
            if (!Directory.Exists(dirPath))
            {
                Debug.LogWarning($"{DiagPrefix}StreamingAssets folder not found: {dirPath}");
                return false;
            }

            string[] immFiles = Directory.GetFiles(dirPath, "*.imm");
            if (immFiles.Length == 0)
            {
                Debug.LogWarning($"{DiagPrefix}No .imm files found in StreamingAssets: {dirPath}");
                return false;
            }

            int fileIndex = Random.Range(0, immFiles.Length);
            selectedFileName = Path.GetFileName(immFiles[fileIndex]);
            Debug.Log($"{DiagPrefix}Selected random StreamingAssets file: {selectedFileName}");
            return true;
        }

        public bool PickRandomFileSystemFile()
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                Debug.LogWarning($"{DiagPrefix}Directory path is empty");
                return false;
            }

            if (!Directory.Exists(directoryPath))
            {
                Debug.LogWarning($"{DiagPrefix}Directory not found: {directoryPath}");
                return false;
            }

            string[] immFiles = Directory.GetFiles(directoryPath, "*.imm");
            if (immFiles.Length == 0)
            {
                Debug.LogWarning($"{DiagPrefix}No .imm files found in: {directoryPath}");
                return false;
            }

            int fileIndex = Random.Range(0, immFiles.Length);
            selectedFileName = Path.GetFileName(immFiles[fileIndex]);
            Debug.Log($"{DiagPrefix}Selected random file system file: {selectedFileName}");
            return true;
        }

        public void PickRandomAndLoad()
        {
            bool picked = loadSource == LoadSource.StreamingAssets
                ? PickRandomStreamingAssetsFile()
                : PickRandomFileSystemFile();

            if (picked)
                LoadDocument();
        }

        private IEnumerator LoadDocumentCoroutine()
        {
            if (_doc != null)
            {
                ImmPlayerManager.Instance.UnloadDocument(_doc);
                _doc = null;
            }

            // Fresh document, fresh anchor state for the viewpoint driver.
            _viewpointAnchored = false;
            _hasRigOffset = false;
            _viewpointDriverActiveLogged = false;

            // Per-device document override: IMM_UNITY_DOC_FILE=<name>.imm in
            // imm_debug_flags.txt selects any StreamingAssets document without a
            // rebuild (test-loop convenience; seed for the browse feature).
            string overrideFile = ReadFlagFileStringValue("IMM_UNITY_DOC_FILE");
            if (!string.IsNullOrEmpty(overrideFile))
            {
                Debug.Log($"{DiagPrefix}Document override from flag file: {overrideFile}");
                selectedFileName = overrideFile;
            }

            if (string.IsNullOrEmpty(selectedFileName))
                yield break;

            if (loadSource == LoadSource.StreamingAssets)
            {
                yield return StartCoroutine(LoadFromStreamingAssets(selectedFileName));
            }
            else
            {
                LoadFromFileSystem();
            }

            if (_doc == null)
                yield break;

            currentFilePath = loadSource == LoadSource.StreamingAssets
                ? Path.Combine(Application.streamingAssetsPath, selectedFileName)
                : Path.Combine(directoryPath, selectedFileName);

            _isDocumentTransformDirty = true;

            if (autoPlay)
            {
                _doc.Resume();
                _doc.Show();
            }
            else
            {
                _doc.Pause();
            }

            if (ShouldRunPostLoadFeature("layer refresh", "IMM_UNITY_DISABLE_LAYER_REFRESH"))
                StartCoroutine(WaitForSequenceAndRefreshLayers());

            if (ShouldRunPostLoadFeature("initial playback state", "IMM_UNITY_DISABLE_INITIAL_PLAYBACK_STATE"))
                StartCoroutine(ApplyInitialPlaybackState());

            if (ShouldRunPostLoadFeature("initial spawn area", "IMM_UNITY_DISABLE_INITIAL_SPAWN_AREA"))
                StartCoroutine(ApplyInitialSpawnAreaViewpoint());

            if (autoPlay && ShouldRunPostLoadFeature("auto first stop", "IMM_UNITY_DISABLE_AUTO_FIRST_STOP"))
                StartCoroutine(AdvanceToFirstStopWhenReady());
        }

        private IEnumerator LoadFromStreamingAssets(string fileName)
        {
            string streamingPath = Path.Combine(Application.streamingAssetsPath, fileName);
            Debug.Log($"{DiagPrefix}Loading from StreamingAssets: {streamingPath}");

            using (UnityWebRequest request = UnityWebRequest.Get(streamingPath))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"{DiagPrefix}Failed to load from StreamingAssets: {request.error}");
                    Debug.LogError($"{DiagPrefix}  Path: {streamingPath}");
                    yield break;
                }

                byte[] data = request.downloadHandler.data;
                Debug.Log($"{DiagPrefix}Loaded {data.Length} bytes from StreamingAssets");

                // The native side rejects loads until the Android deferred renderer init has run on
                // the render thread (first plugin render event). We usually win that race only
                // because the UnityWebRequest above takes ~100 ms; anything that slows init (slow
                // device, Vulkan validation layer) made the load fail with no retry. Wait for
                // readiness explicitly.
                float readyDeadline = Time.realtimeSinceStartup + 30.0f;
                while (!ImmPlayerManager.Instance.IsReadyForDocumentLoad && Time.realtimeSinceStartup < readyDeadline)
                    yield return null;
                if (!ImmPlayerManager.Instance.IsReadyForDocumentLoad)
                {
                    Debug.LogError($"{DiagPrefix}Native renderer never became ready for document load (30s timeout)");
                    yield break;
                }

                // Load straight from the in-memory bytes (no temp file). Everything stays inside the
                // APK, and this avoids the file-path load (Player::Load(wchar_t* path)) that segfaults
                // on Android (wchar_t is 4 bytes there). The native LoadFromMemory uses the byte-buffer
                // overload instead.
                _doc = ImmPlayerManager.Instance.LoadDocumentFromMemory(data, fileName);
            }
        }

        private void LoadFromFileSystem()
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                Debug.LogError($"{DiagPrefix}Directory path is empty");
                return;
            }

            string path = Path.Combine(directoryPath, selectedFileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"{DiagPrefix}File not found: {path}");
                return;
            }

            _doc = ImmPlayerManager.Instance.LoadDocument(path);
        }

        public void UnloadDocument()
        {
            if (_doc == null)
                return;

            if (_initialSpawnAreaCoroutine != null)
            {
                StopCoroutine(_initialSpawnAreaCoroutine);
                _initialSpawnAreaCoroutine = null;
            }

            if (_spawnAreaApplyCoroutine != null)
            {
                StopCoroutine(_spawnAreaApplyCoroutine);
                _spawnAreaApplyCoroutine = null;
            }

            if (_chapterSyncCoroutine != null)
            {
                StopCoroutine(_chapterSyncCoroutine);
                _chapterSyncCoroutine = null;
            }

            ImmPlayerManager.Instance.UnloadDocument(_doc);
            _doc = null;
            currentFilePath = string.Empty;
            _viewpointAnchored = false;
            _hasRigOffset = false;
            _viewpointDriverActiveLogged = false;
        }

        public void RefreshStatus()
        {
            if (_doc == null)
                return;

            documentState = _doc.GetStateInfo();
            documentInfoFlags = _doc.GetInfoFlags();
            chapterCount = _doc.GetChapterCount();
            currentChapter = _doc.GetCurrentChapter();
            layerCount = _doc.GetLayerCount();
            spawnAreaCount = _doc.GetSpawnAreaCount();
            SyncSpawnAreaSelection();

            if (_doc.IsSequenceReady())
            {
                documentBounds = _doc.GetBoundingBox();
            }

            layers = _doc.GetLayersManaged();
            spawnAreas = _doc.GetSpawnAreas();

            // Scale forensics: a document authored at unexpected scale (or a
            // mis-applied spawn transform scale) makes stereo disparity huge and
            // unfusable in-headset. One log line answers it objectively.
            if (spawnAreas != null && spawnAreas.Length > 0)
            {
                var sa = spawnAreas[0];
                Debug.Log($"[IMM_DOC] file={selectedFileName} bounds(c={documentBounds.center:F2} s={documentBounds.size:F2}) spawnAreas={spawnAreas.Length} spawn0(pos={sa.Transform.GetPosition():F2} scale={sa.Transform.GetScale():F3} type={sa.Type})");
            }
            else
            {
                Debug.Log($"[IMM_DOC] file={selectedFileName} bounds(c={documentBounds.center:F2} s={documentBounds.size:F2}) spawnAreas=0");
            }
            RefreshLayerList();
        }

        // Re-apply the ACTIVE authored spawn viewpoint (position, yaw, scale) -
        // the recenter button snaps here: Quill documents bake their intended
        // viewing anchors as spawn areas.
        public void ReapplyActiveSpawnAreaViewpoint()
        {
            if (_doc == null)
                return;
            SyncSpawnAreaSelection();
            int id = _doc.GetActiveSpawnAreaId();
            if (id < 0 && _spawnAreaIds.Length > 0)
                id = _spawnAreaIds[0];
            if (id >= 0)
                StartSpawnAreaViewpointApply(id);
        }

        public void NextSpawnArea()
        {
            SetSpawnAreaByOffset(1);
        }

        public void PreviousSpawnArea()
        {
            SetSpawnAreaByOffset(-1);
        }

        public void JumpToSpawnArea()
        {
            SetSpawnAreaByIndex(targetSpawnAreaIndex);
        }

        public void RefreshLayerList()
        {
            if (_doc == null || !_doc.IsSequenceReady())
            {
                layerList = new LayerListEntry[0];
                return;
            }

            var layers = _doc.GetLayersManaged();
            if (layers.Length == 0)
            {
                layerList = new LayerListEntry[0];
                return;
            }

            layerList = new LayerListEntry[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                layerList[i] = new LayerListEntry
                {
                    Index = i,
                    Id = layers[i].Id,
                    Type = layers[i].Type,
                    Name = layers[i].Name,
                    FullName = layers[i].FullName
                };
            }

            if (selectedLayerIndex >= layers.Length)
            {
                selectedLayerIndex = -1;
            }

            SyncLayerFieldsFromSelection();
        }

        private IEnumerator WaitForSequenceAndRefreshLayers()
        {
            while (_doc != null && _doc.GetStateInfo().Loading != ImmDocument.LoadingState.Loaded)
                yield return null;

            if (_doc != null)
                RefreshLayerList();
        }

        private void ApplyDocumentBackgroundColor()
        {
            Camera cam = backgroundCamera != null ? backgroundCamera : Camera.main;
            if (cam == null)
                return;

            var info = ImmPlayerManager.Instance.GetPlayerInfo();
            cam.backgroundColor = info.BackgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private IEnumerator ApplyInitialPlaybackState()
        {
            if (_doc == null)
                yield break;

            // Wait until the document is fully loaded to enforce play state.
            while (_doc != null)
            {
                var state = _doc.GetStateInfo();
                if (state.Loading == ImmDocument.LoadingState.Loaded)
                    break;
                yield return null;
            }

            if (_doc == null)
                yield break;

            if (applyDocumentBackgroundColor)
                ApplyDocumentBackgroundColor();

            if (autoPlay)
            {
                _doc.Resume();
                _doc.Show();
            }
            else
            {
                _doc.Pause();
            }
        }

        private IEnumerator ApplyInitialSpawnAreaViewpoint()
        {
            if (_initialSpawnAreaCoroutine != null)
            {
                StopCoroutine(_initialSpawnAreaCoroutine);
            }

            _initialSpawnAreaCoroutine = StartCoroutine(ApplyInitialSpawnAreaViewpointRoutine());
            yield return _initialSpawnAreaCoroutine;
            _initialSpawnAreaCoroutine = null;
        }

        private IEnumerator ApplyInitialSpawnAreaViewpointRoutine()
        {
            if (_doc == null)
                yield break;

            while (_doc != null && !_doc.IsSequenceReady())
                yield return null;

            if (_doc == null)
                yield break;

            const int maxFrames = 120;
            int frames = 0;
            while (_doc != null && frames < maxFrames)
            {
                SyncSpawnAreaSelection();
                if (_spawnAreaIds.Length == 0)
                {
                    yield return null;
                    frames++;
                    continue;
                }

                int initialSpawnId = _doc.GetInitialSpawnAreaId();
                int initialIndex = System.Array.IndexOf(_spawnAreaIds, initialSpawnId);
                int index = initialIndex >= 0
                    ? initialIndex
                    : (currentSpawnAreaIndex >= 0 ? currentSpawnAreaIndex : 0);
                SetSpawnAreaByIndex(index);
                yield break;
            }
        }

        public void Play()
        {
            _doc?.Resume();
            ImmNativePlugin.GlobalWork(1);
        }

        public void Pause()
        {
            _doc?.Pause();
            ImmNativePlugin.GlobalWork(1);
        }

        public void Restart()
        {
            _doc?.Restart();
        }

        // Quill-player parity: play carries the doc from the title card to its
        // first stop on its own; without this the doc parks at the start until
        // a manual Next Chapter press (user report 2026-07-28, every run).
        private IEnumerator AdvanceToFirstStopWhenReady()
        {
            // Chapters exist only after the async decode - 221 MB QuantumRace
            // takes ~55 s, so the old 20 s deadline expired mid-decode and the
            // doc silently parked on the title card every headless run. Wait
            // for chapters with a cap that covers big documents.
            float deadline = Time.realtimeSinceStartup + 120.0f;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (_doc == null)
                    yield break;
                if (_doc.GetChapterCount() > 0 && _initialSpawnAreaCoroutine == null)
                    break;
                yield return null;
            }
            if (_doc == null || _doc.GetChapterCount() <= 1)
                yield break;
            // Headless test hook: IMM_UNITY_START_CHAPTER=N in imm_debug_flags.txt
            // jumps straight to chapter N at boot (heavy-scene captures without a
            // controller press).
            int startChapter = ReadFlagFileIntValue("IMM_UNITY_START_CHAPTER", -1);
            if (startChapter >= 0 && startChapter < _doc.GetChapterCount())
            {
                Debug.Log($"{DiagPrefix}Debug start chapter {startChapter} (flag file)");
                RequestChapterAndSync(startChapter);
                yield break;
            }
            // Only auto-advance off the title card; a user press may already
            // have moved the doc on.
            if (_doc.GetCurrentChapter() != 0)
                yield break;
            Debug.Log($"{DiagPrefix}Auto-advancing to first stop (chapter 1)");
            SkipForward();
        }

        // Reads NAME=VALUE (string) from the shared debug flag file; null when absent.
        private static string ReadFlagFileStringValue(string name)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "imm_debug_flags.txt");
                if (System.IO.File.Exists(path))
                {
                    foreach (string line in System.IO.File.ReadAllLines(path))
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith(name + "=", System.StringComparison.OrdinalIgnoreCase))
                            return trimmed.Substring(name.Length + 1).Trim();
                    }
                }
            }
            catch (System.Exception)
            {
            }
#endif
            return null;
        }

        // Reads NAME=VALUE from the shared debug flag file (persistentDataPath/
        // imm_debug_flags.txt); returns fallback when absent or malformed.
        private static int ReadFlagFileIntValue(string name, int fallback)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "imm_debug_flags.txt");
                if (System.IO.File.Exists(path))
                {
                    foreach (string line in System.IO.File.ReadAllLines(path))
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith(name + "=", System.StringComparison.OrdinalIgnoreCase) &&
                            int.TryParse(trimmed.Substring(name.Length + 1), out int value))
                            return value;
                    }
                }
            }
            catch (System.Exception)
            {
            }
#endif
            return fallback;
        }

        public void SkipForward()
        {
            if (_doc == null)
                return;

            int count = _doc.GetChapterCount();
            if (count <= 0)
            {
                _doc.SkipForward();
                return;
            }

            int current = _doc.GetCurrentChapter();
            int next = (current + 1) % count;
            RequestChapterAndSync(next);
        }

        public void SkipBack()
        {
            if (_doc == null)
                return;

            int count = _doc.GetChapterCount();
            if (count <= 0)
            {
                _doc.SkipBack();
                return;
            }

            int current = _doc.GetCurrentChapter();
            int previous = (current - 1 + count) % count;
            RequestChapterAndSync(previous);
        }

        public void JumpToChapter()
        {
            if (_doc == null)
                return;

            if (targetChapter < 0)
                targetChapter = 0;

            RequestChapterAndSync(targetChapter);
        }

        private void RequestChapterAndSync(int chapterIndex)
        {
            if (_doc == null)
                return;

            int count = _doc.GetChapterCount();
            if (count <= 0)
                return;

            int clampedChapter = Mathf.Clamp(chapterIndex, 0, count - 1);
            if (_chapterSyncCoroutine != null)
            {
                StopCoroutine(_chapterSyncCoroutine);
            }

            _chapterSyncCoroutine = StartCoroutine(ApplyChapterAndSync(clampedChapter));
        }

        private IEnumerator ApplyChapterAndSync(int chapterIndex)
        {
            if (_doc == null)
                yield break;

            _doc.SetChapter(chapterIndex);

            const int maxFrames = 10;
            int frames = 0;
            while (_doc != null && frames < maxFrames)
            {
                int current = _doc.GetCurrentChapter();
                if (current == chapterIndex)
                    break;

                yield return null;
                frames++;
            }

            if (_doc == null)
                yield break;

            currentChapter = _doc.GetCurrentChapter();
            targetChapter = currentChapter;

            SyncSpawnAreaSelection();
            // When the skip crossed an authored MakeDefault keyframe the
            // needs-update signal is pending: leave the re-anchor to the
            // viewpoint driver, which resolves the AUTHORED initial spawn id
            // (the active id here is the stale user selection - re-applying it
            // was why skips missed the authored spot on QuantumRace).
            if (_viewpointDriverEnabled && _doc.GetSpawnAreaNeedsUpdate())
            {
                Debug.Log($"{ViewpointLogPrefix}chapter sync deferring to needs-update re-anchor");
            }
            else
            {
                int activeSpawnId = _doc.GetActiveSpawnAreaId();
                if (activeSpawnId >= 0)
                {
                    StartSpawnAreaViewpointApply(activeSpawnId);
                }
            }

            _chapterSyncCoroutine = null;
        }

        public void ApplyLayerVisibility()
        {
            ApplyLayerEdits(true, applyOpacity: false, applyTransform: false);
        }

        public void ApplyLayerOpacity()
        {
            ApplyLayerEdits(true, applyVisibility: false, applyTransform: false);
        }

        public void ApplyLayerTransform()
        {
            ApplyLayerEdits(true, applyVisibility: false, applyOpacity: false, logTransformChange: true);
        }

        public void ClearLayerOverrides()
        {
            if (_doc == null || !_doc.IsSequenceReady())
                return;

            int layerId = ResolveLayerId();
            if (layerId < 0)
                return;

            LogLayerDiagnostics("before-clear", layerId);
            _doc.ClearLayerVisibilityOverride(layerId);
            _doc.ClearLayerTransformOverride(layerId);
            LogLayerDiagnostics("after-clear", layerId);
            RefreshSelectedLayerStatus();
        }

        private int ResolveLayerId()
        {
            if (_doc == null || !_doc.IsSequenceReady())
                return -1;

            if (selectedLayerIndex >= 0)
            {
                var info = _doc.GetLayerInfoManaged(selectedLayerIndex);
                if (info.HasValue)
                    return info.Value.Id;
            }

            return -1;
        }

        private void SetSpawnAreaByOffset(int offset)
        {
            if (_doc == null)
                return;

            SyncSpawnAreaSelection();
            if (_spawnAreaIds.Length == 0)
                return;

            int startIndex = currentSpawnAreaIndex >= 0 ? currentSpawnAreaIndex : 0;
            int count = _spawnAreaIds.Length;
            int targetIndex = (startIndex + offset) % count;
            if (targetIndex < 0)
                targetIndex += count;
            SetSpawnAreaByIndex(targetIndex);
        }

        private void SetSpawnAreaByIndex(int spawnAreaIndex)
        {
            if (_doc == null)
                return;

            SyncSpawnAreaSelection();
            if (_spawnAreaIds.Length == 0)
                return;

            int clampedIndex = Mathf.Clamp(spawnAreaIndex, 0, _spawnAreaIds.Length - 1);
            int spawnAreaId = _spawnAreaIds[clampedIndex];

            _doc.SetActiveSpawnAreaId(spawnAreaId);

            StartSpawnAreaViewpointApply(spawnAreaId);

            activeSpawnAreaId = spawnAreaId;
            currentSpawnAreaIndex = clampedIndex;
            targetSpawnAreaIndex = clampedIndex;
        }

        private void StartSpawnAreaViewpointApply(int spawnAreaId)
        {
            if (_spawnAreaApplyCoroutine != null)
            {
                StopCoroutine(_spawnAreaApplyCoroutine);
            }

            _spawnAreaApplyCoroutine = StartCoroutine(ApplySpawnAreaViewpointDeferred(spawnAreaId));
        }

        private IEnumerator ApplySpawnAreaViewpointDeferred(int requestedSpawnAreaId)
        {
            if (_doc == null)
                yield break;

            var info = _doc.GetSpawnAreaInfoManaged(requestedSpawnAreaId);
            int settleFrames = (info.HasValue && info.Value.Animated) ? 3 : 1;
            for (int i = 0; i < settleFrames; i++)
                yield return null;

            const int maxFrames = 10;
            int resolvedSpawnAreaId = requestedSpawnAreaId;
            for (int i = 0; i < maxFrames; i++)
            {
                if (_doc == null)
                    yield break;

                int activeSpawnAreaId = _doc.GetActiveSpawnAreaId();
                if (activeSpawnAreaId >= 0)
                {
                    resolvedSpawnAreaId = activeSpawnAreaId;
                }

                if (activeSpawnAreaId == requestedSpawnAreaId)
                {
                    break;
                }

                yield return null;
            }

            ApplySpawnAreaViewpoint(resolvedSpawnAreaId);
            _spawnAreaApplyCoroutine = null;
        }

        private void ApplySpawnAreaViewpoint(int spawnAreaId)
        {
            if (!applySpawnAreaToViewpoint || _doc == null)
                return;

            Transform target = ResolveSpawnAreaTargetTransform();
            if (target == null)
                return;

            Transform head = ResolveViewpointHeadTransform(target);
            Transform documentRoot = documentTransform != null ? documentTransform : transform;
            // The spawn transform carries the authored VIEWER scale (Quill bakes
            // its intended viewing anchors + scale into spawn areas). The rig
            // pose below must be solved WITH that scale: at rig scale s the head
            // sits at rigPos + rigRot*(s*headLocal), so placing the rig with the
            // unscaled offset flings the user (s-1)*headLocal away from the
            // authored anchor (user report: "center point is off").
            float spawnScale = 1f;
            var spawnScaleInfo = _doc.GetSpawnAreaInfoManaged(spawnAreaId);
            if (spawnScaleInfo.HasValue)
            {
                float authored = spawnScaleInfo.Value.Transform.GetScale();
                if (authored > 0.0001f)
                    spawnScale = authored;
            }

            if (_doc.TryGetSpawnAreaViewTargetPose(
                spawnAreaId,
                documentRoot,
                target,
                head,
                keepCurrentViewHeightForFloorAreas,
                out Pose targetPose))
            {
                Pose finalPose = targetPose;
                Quaternion finalRotation = targetPose.rotation;
                Vector3 headLocalPosition = target.InverseTransformPoint(head.position);
                // Zero the head's local height ONLY for floor-level spawns (the
                // anchor is a floor point; physical height stays yours, scaled).
                // For EYE-level spawns the anchor IS the intended head position:
                // keeping y lets the scaled solve land the head exactly on it.
                // Zeroing unconditionally left the head scale×headHeight (~15m at
                // 9x) above eye-level anchors - user: "felt a little high up".
                bool floorSpawn = spawnScaleInfo.HasValue &&
                                  spawnScaleInfo.Value.Type == SerializedSpawnArea.Type.FloorLevel;
                if (keepCurrentViewHeightForFloorAreas && floorSpawn)
                    headLocalPosition.y = 0.0f;
                finalRotation = EffectiveViewpointRotation(targetPose.rotation);

                // World anchor the head should land on (from the unscaled pose),
                // then re-solve the rig position with the SCALED head offset.
                Vector3 worldHeadAnchor = targetPose.position + (targetPose.rotation * headLocalPosition);
                Vector3 scaledPosition = worldHeadAnchor - (finalRotation * (headLocalPosition * spawnScale));
                finalPose = new Pose(scaledPosition, finalRotation);

                target.localScale = Vector3.one * spawnScale;
                target.SetPositionAndRotation(finalPose.position, finalPose.rotation);
                Debug.Log($"[IMM_SCALE] spawn viewpoint applied: scale={spawnScale:F3} anchor={worldHeadAnchor:F2} rigPos={finalPose.position:F2}");

                // Anchor established: the animated viewpoint driver may follow
                // from here. Offset is recaptured fresh this LateUpdate.
                _viewpointAnchored = true;
                _hasRigOffset = false;
            }
        }

        private Transform ResolveSpawnAreaTargetTransform()
        {
            if (spawnAreaTargetTransform != null)
                return spawnAreaTargetTransform;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return null;

            Transform cameraTransform = mainCamera.transform;
            return cameraTransform.parent != null ? cameraTransform.parent : cameraTransform;
        }

        private Transform ResolveViewpointHeadTransform(Transform target)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return target;
            return mainCamera.transform;
        }

        private void SyncSpawnAreaSelection()
        {
            if (_doc == null)
                return;

            _spawnAreaIds = _doc.GetSpawnAreaList();
            spawnAreaCount = _spawnAreaIds.Length;
            activeSpawnAreaId = _doc.GetActiveSpawnAreaId();
            currentSpawnAreaIndex = -1;

            for (int i = 0; i < _spawnAreaIds.Length; i++)
            {
                if (_spawnAreaIds[i] == activeSpawnAreaId)
                {
                    currentSpawnAreaIndex = i;
                    break;
                }
            }

            if (_spawnAreaIds.Length == 0)
            {
                targetSpawnAreaIndex = 0;
                return;
            }

            if (targetSpawnAreaIndex < 0 || targetSpawnAreaIndex >= _spawnAreaIds.Length)
            {
                targetSpawnAreaIndex = currentSpawnAreaIndex >= 0 ? currentSpawnAreaIndex : 0;
            }
        }

        private void ApplyDocumentTransform()
        {
            if (_doc == null || documentTransform == null)
                return;

            _doc.SetTransform(documentTransform);
        }

        private Matrix4x4 ComputeLayerLocalMatrix()
        {
            Matrix4x4 local = Matrix4x4.TRS(layerPosition, Quaternion.Euler(layerEuler), Vector3.one * layerScale);
            return local;
        }

        public void RefreshSelectedLayerStatus()
        {
            SyncLayerFieldsFromSelection();
        }

        private void SyncLayerFieldsFromSelection()
        {
            _lastSelectedLayerIndex = selectedLayerIndex;

            if (_doc == null || !_doc.IsSequenceReady())
                return;

            if (selectedLayerIndex < 0)
                return;

            var info = _doc.GetLayerInfoManaged(selectedLayerIndex);
            if (!info.HasValue)
                return;

            _isSyncingSelection = true;
            try
            {
                var li = info.Value;
                layerVisible = li.IsVisible;
                layerOpacity = li.Opacity;

                selectedLayerId = li.Id;
                selectedLayerName = li.Name;
                selectedLayerType = li.Type;
                selectedLayerParentId = li.ParentId;
                selectedLayerLoaded = li.IsLoaded;
                selectedLayerVisible = li.IsVisible;
                selectedLayerOpacity = li.Opacity;
                selectedLayerHasBounds = li.HasBounds;
                selectedLayerBounds = li.Bounds;

                var diag = _doc.GetLayerDiagnostics(li.Id);
                if (diag.HasValue)
                {
                    selectedLayerVisibilityOverrideEnabled = diag.Value.VisibilityOverrideEnabled;
                    selectedLayerVisibilityOverrideValue = diag.Value.VisibilityOverrideValue;
                }

                CacheLayerTransform();
                CacheLayerVisuals();
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void ApplyLayerEdits(bool logDiagnostics, bool applyVisibility = true, bool applyOpacity = true, bool applyTransform = true, bool logTransformChange = false)
        {
            if (_doc == null)
                return;
            if (!_doc.IsSequenceReady())
                return;

            int layerId = ResolveLayerId();
            if (layerId < 0)
                return;

            _isApplyingEdits = true;
            try
            {
                if (logDiagnostics) LogLayerDiagnostics("before", layerId);

                if (applyVisibility)
                {
                    bool ok = _doc.SetLayerVisible(layerId, layerVisible);
                    if (!ok && logDiagnostics)
                    {
                        Debug.Log($"{DiagPrefix}apply: SetLayerVisible failed for layerId={layerId}");
                    }

                    if (logDiagnostics)
                    {
                        ScheduleVisibilityDiagnostics(layerId);
                    }

                    if (!logDiagnostics)
                    {
                        Debug.Log($"{DiagPrefix}apply: layerId={layerId} visible={layerVisible} ok={ok}");
                    }
                }

                if (applyOpacity)
                {
                    bool ok = _doc.SetLayerOpacity(layerId, layerOpacity);
                    if (!logDiagnostics)
                    {
                        Debug.Log($"{DiagPrefix}apply: layerId={layerId} opacity={layerOpacity} ok={ok}");
                    }
                }

                if (applyTransform)
                {
                    bool ok = _doc.SetLayerTransform(layerId, ComputeLayerLocalMatrix());
                    CacheLayerTransform();
                    if (logTransformChange)
                    {
                        string layerSummary = "unknown";
                        if (selectedLayerIndex >= 0)
                        {
                            var info = _doc.GetLayerInfoManaged(selectedLayerIndex);
                            if (info.HasValue)
                            {
                                var li = info.Value;
                                layerSummary = $"name={li.Name} type={li.Type} loaded={li.IsLoaded} bounds={li.HasBounds} children={li.NumChildren}";
                            }
                        }

                        Debug.Log($"{DiagPrefix}transform: layerId={layerId} {layerSummary} pos={layerPosition} rot={layerEuler} scale={layerScale}");
                        LogLayerDiagnostics("after-transform", layerId);
                    }
                    else
                    {
                        Debug.Log($"{DiagPrefix}apply: layerId={layerId} transform ok={ok}");
                    }
                }

                if (logDiagnostics) LogLayerDiagnostics("after", layerId);
            }
            finally
            {
                _isApplyingEdits = false;
            }
        }

        private void LogLayerDiagnostics(string phase, int layerId)
        {
            var diag = _doc.GetLayerDiagnostics(layerId);
            if (!diag.HasValue)
            {
                Debug.Log($"{DiagPrefix}{phase}: layerId={layerId} diag unavailable");
                return;
            }

            var d = diag.Value;
            Debug.Log($"{DiagPrefix}{phase}: layerId={layerId} visKeys={d.HasVisibilityKeys} opKeys={d.HasOpacityKeys} " +
                      $"vis={d.IsVisible} op={d.Opacity} worldVis={d.IsWorldVisible} worldOp={d.WorldOpacity} parentId={d.ParentId} " +
                      $"visOverride={d.VisibilityOverrideEnabled} visOverrideValue={d.VisibilityOverrideValue} " +
                      $"transformKeys={d.HasTransformKeys} transformOverride={d.TransformOverrideEnabled}");
        }

        private void ScheduleVisibilityDiagnostics(int layerId)
        {
            if (_visibilityDiagCoroutine != null)
            {
                StopCoroutine(_visibilityDiagCoroutine);
            }

            _visibilityDiagCoroutine = StartCoroutine(LogVisibilityDiagnosticsNextFrame(layerId));
        }

        private IEnumerator LogVisibilityDiagnosticsNextFrame(int layerId)
        {
            yield return new WaitForEndOfFrame();
            LogLayerDiagnostics("post-frame", layerId);
            LogParentVisibilityChain(layerId);
            _visibilityDiagCoroutine = null;
        }

        private void LogParentVisibilityChain(int layerId)
        {
            var visited = new HashSet<int>();
            int currentId = layerId;
            int depth = 0;

            while (currentId >= 0 && depth < 32 && visited.Add(currentId))
            {
                var diag = _doc.GetLayerDiagnostics(currentId);
                if (!diag.HasValue)
                {
                    Debug.Log($"{DiagPrefix}chain[{depth}]: layerId={currentId} diag unavailable");
                    break;
                }

                var d = diag.Value;
                Debug.Log($"{DiagPrefix}chain[{depth}]: layerId={currentId} vis={d.IsVisible} worldVis={d.IsWorldVisible} parentId={d.ParentId}");

                if (d.ParentId < 0 || d.ParentId == currentId)
                    break;

                currentId = d.ParentId;
                depth++;
            }
        }

        private bool HasLayerTransformChanged()
        {
            return layerPosition != _lastLayerPosition
                || layerEuler != _lastLayerEuler
                || Mathf.Abs(layerScale - _lastLayerScale) > 0.0001f;
        }

        private void CacheLayerTransform()
        {
            _lastLayerPosition = layerPosition;
            _lastLayerEuler = layerEuler;
            _lastLayerScale = layerScale;
        }

        private bool HasLayerVisualChanged()
        {
            return layerVisible != _lastLayerVisible
                || Mathf.Abs(layerOpacity - _lastLayerOpacity) > 0.0001f;
        }

        private void CacheLayerVisuals()
        {
            _lastLayerVisible = layerVisible;
            _lastLayerOpacity = layerOpacity;
        }
    }
}
