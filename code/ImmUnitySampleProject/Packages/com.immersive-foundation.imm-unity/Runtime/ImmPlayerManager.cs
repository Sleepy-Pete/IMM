using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace ImmPlayer
{
    /// <summary>
    /// High-level manager for the IMM Player plugin
    /// Handles initialization, rendering, and document management
    /// </summary>
    public class ImmPlayerManager : MonoBehaviour
    {
        
        private const string LogPrefix = "[IMM] ";
        private static void Log(string message) => Debug.Log(LogPrefix + message);
        private static void LogWarning(string message) => Debug.LogWarning(LogPrefix + message);
        private static void LogError(string message) => Debug.LogError(LogPrefix + message);
#region Singleton

        private static ImmPlayerManager _instance;
        public static ImmPlayerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ImmPlayerManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ImmPlayerManager");
                        _instance = go.AddComponent<ImmPlayerManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Settings

        [Header("Player Settings")]
        [SerializeField] private bool useLinearColorSpace = true;
        [SerializeField] private int antialiasingLevel = 8;
        [SerializeField] private string logFileName = "imm_player_log.txt";
        [SerializeField] private Camera renderCamera = null;

        #endregion

        #region State

        private bool _isInitialized = false;
        private Dictionary<int, ImmDocument> _loadedDocuments = new Dictionary<int, ImmDocument>();
        private Dictionary<int, IntPtr> _documentMemoryPtrs = new Dictionary<int, IntPtr>(); // Track memory for async loading
        private IntPtr _renderEventFunc = IntPtr.Zero;
        private IntPtr _renderEventAndDataFunc = IntPtr.Zero;
        private readonly Dictionary<Camera, PerCameraInfo> _cameras = new Dictionary<Camera, PerCameraInfo>();
        private readonly Dictionary<Camera, float> _lastNearClipLogged = new Dictionary<Camera, float>();
        private readonly HashSet<Camera> _loggedVulkanRenderTargetSource = new HashSet<Camera>();
        private readonly HashSet<Camera> _loggedVulkanPrepareWarning = new HashSet<Camera>();
        private readonly HashSet<int> _configuredVulkanRenderEvents = new HashSet<int>();
        private const string NearDiagPrefix = "[IMMDBG_NEAR_20260208A] ";
        private const int VulkanCustomBlitEventId = 6;
        private static readonly List<UnityEngine.XR.XRDisplaySubsystem> _xrDisplaySubsystems = new List<UnityEngine.XR.XRDisplaySubsystem>();
        private bool _useCommandBufferRendering = false;
        private bool _useCameraCallbackRendering = false;
        private Coroutine _vulkanSampleEventCoroutine = null;
        private int _appleMetalEventLogCount = 0;
        private static Mesh _vulkanOverlayFixtureMesh;
        private static Material _vulkanOverlayFixtureMaterial;

        private static HashSet<string> _debugFlagFileCache;

        private static bool IsEnvFlagEnabled(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrEmpty(value))
                return value != "0";
#if UNITY_ANDROID && !UNITY_EDITOR
            // Env vars never reach an Android app process. Mirror the flags to a device
            // file instead (one flag name per line):
            //   adb shell "echo IMM_UNITY_VK_NO_RENDER_EVENTS > /sdcard/Android/data/<pkg>/files/imm_debug_flags.txt"
            if (_debugFlagFileCache == null)
            {
                _debugFlagFileCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    string path = Path.Combine(Application.persistentDataPath, "imm_debug_flags.txt");
                    if (File.Exists(path))
                    {
                        foreach (string line in File.ReadAllLines(path))
                        {
                            string flag = line.Trim();
                            if (flag.Length > 0 && !flag.StartsWith("#"))
                                _debugFlagFileCache.Add(flag);
                        }
                        Debug.Log($"[IMM_DEBUG_FLAGS] loaded {_debugFlagFileCache.Count} flags from {path}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[IMM_DEBUG_FLAGS] failed to read flag file: {e.Message}");
                }
            }
            return _debugFlagFileCache.Contains(name);
#else
            return false;
#endif
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            bool builtInPipeline = GraphicsSettings.currentRenderPipeline == null;
            bool forceCameraCallback = IsEnvFlagEnabled("IMM_UNITY_FORCE_CAMERA_CALLBACK");
            _useCommandBufferRendering = builtInPipeline && !forceCameraCallback;
            _useCameraCallbackRendering = builtInPipeline && forceCameraCallback;
            if (_useCommandBufferRendering || _useCameraCallbackRendering)
            {
                Camera.onPreCull += OnCameraPreCull;
            }
            if (_useCameraCallbackRendering)
            {
                Camera.onPostRender += OnCameraPostRender;
            }
            if (builtInPipeline && IsEnvFlagEnabled("IMM_UNITY_VK_SAMPLE_WAIT_FOR_END_OF_FRAME"))
            {
                _vulkanSampleEventCoroutine = StartCoroutine(IssueVulkanSampleEventAfterEndOfFrame());
            }
        }

        private void OnDisable()
        {
            if (_useCommandBufferRendering || _useCameraCallbackRendering)
            {
                Camera.onPreCull -= OnCameraPreCull;
            }
            if (_useCameraCallbackRendering)
            {
                Camera.onPostRender -= OnCameraPostRender;
            }
            if (_vulkanSampleEventCoroutine != null)
            {
                StopCoroutine(_vulkanSampleEventCoroutine);
                _vulkanSampleEventCoroutine = null;
            }
            CleanupCommandBuffers();
        }

        private void LateUpdate()
        {
            // Heartbeat: proves the main thread is alive when native logs go silent
            // (distinguishes an app-wide wedge from a render-thread-only stall).
            if (Time.frameCount % 72 == 0)
                Debug.Log($"[IMM_HEARTBEAT] frame={Time.frameCount} t={Time.realtimeSinceStartup:F1}s");
            foreach (var kvp in _cameras)
                MaybeDumpVulkanEyeTargets(kvp.Value);
            if (_isInitialized)
            {
                ImmNativePlugin.GlobalWork(1);
                if (IsVulkanRuntime() &&
                    _renderEventFunc != IntPtr.Zero &&
                    !IsEnvFlagEnabled("IMM_UNITY_VK_SKIP_HOST_RENDER") &&
                    IsEnvFlagEnabled("IMM_UNITY_VK_USE_PREPARE_EVENT"))
                {
                    foreach (PerCameraInfo info in _cameras.Values)
                    {
                        int prepareEventId = (info.CameraId << 8) | 0x80;
                        if (!IsEnvFlagEnabled("IMM_UNITY_VK_SKIP_MANAGED_CONFIG") && _configuredVulkanRenderEvents.Add(prepareEventId))
                        {
                            int configured = ImmNativePlugin.ConfigureVulkanRenderEvent(prepareEventId);
                            Debug.Log($"[IMM_UNITY_VK_EVENTCFG_20260611] eventId={prepareEventId} configured={configured}");
                        }
                        GL.IssuePluginEvent(_renderEventFunc, prepareEventId);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        #endregion

        #region Initialization

        public bool Initialize()
        {
            if (_isInitialized)
            {
                LogWarning("ImmPlayerManager is already initialized");
                return true;
            }

            Log("=== IMM Player Initialization Started ===");

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Metal)
            {
                LogError("IMM Player requires Metal on Apple platforms.");
                LogError($"Current Graphics API: {SystemInfo.graphicsDeviceType}");
                LogError("Switch the Apple platform Graphics API to Metal and restart Unity or rebuild the player.");
                return false;
            }
#endif

            int colorSpace = useLinearColorSpace ? 0 : 1;

            // Use Unity's temporary cache path for temporary files
            string tempFolder = Application.temporaryCachePath;
            string nativeLogPath = Path.Combine(tempFolder, logFileName);

            try
            {
#if UNITY_IOS && !UNITY_EDITOR
                ImmNativePlugin.ImmUnityRegisterRenderingPlugin();
#endif

                int result = ImmNativePlugin.Init(colorSpace, antialiasingLevel, nativeLogPath, tempFolder);

                if (result < 0)
                {
                    LogError($"Failed to initialize IMM Player. Error code: {result}");
                    LogError("Possible causes:");
                    LogError("  1. Missing DLL dependencies in Assets/Plugins/x86_64/");
                    LogError("  2. DLL platform settings incorrect (must be x86_64, Standalone + Editor)");
                    LogError("  3. Graphics API not supported (requires DirectX 11 on Windows, GLES on Android, or Metal on Apple platforms)");
                    LogError($"  4. Check native log file: {nativeLogPath}");
                    return false;
                }

                _renderEventFunc = ImmNativePlugin.GetRenderEventFunc();
                _renderEventAndDataFunc = ImmNativePlugin.GetRenderEventAndDataFunc();
                _isInitialized = true;

                Log("=== IMM Player Initialized Successfully ===");
                return true;
            }
            catch (System.DllNotFoundException ex)
            {
                LogError("=== DLL NOT FOUND ERROR ===");
                LogError($"Could not load ImmUnityPlugin.dll or one of its dependencies: {ex.Message}");
                LogError("Required DLLs in Assets/Plugins/x86_64/:");
                LogError("  - ImmUnityPlugin.dll");
                LogError("  - Audio360.dll");
                LogError("  - jpeg62.dll, libpng16.dll");
                LogError("  - ogg.dll, opus.dll, opusenc.dll");
                LogError("  - vorbis.dll, vorbisenc.dll");
                LogError("  - zlib1.dll");
                LogError("Make sure all DLLs have correct import settings!");
                return false;
            }
            catch (System.Exception ex)
            {
                LogError($"=== UNEXPECTED ERROR ===");
                LogError($"Exception during initialization: {ex.GetType().Name}");
                LogError($"Message: {ex.Message}");
                LogError($"Stack: {ex.StackTrace}");
                return false;
            }
        }

        public void Shutdown()
        {
            if (!_isInitialized)
                return;

            // Unload all documents
            foreach (var doc in _loadedDocuments.Values)
            {
                doc.Unload();
            }
            _loadedDocuments.Clear();

            // Free any remaining memory allocated for documents loaded from memory
            foreach (var memPtr in _documentMemoryPtrs.Values)
            {
                Marshal.FreeHGlobal(memPtr);
            }
            _documentMemoryPtrs.Clear();

            ImmNativePlugin.End();
            _isInitialized = false;
            CleanupCommandBuffers();

            Log("IMM Player shut down");
        }

        #endregion

        #region Document Management

        /// <summary>
        /// Load an IMM document from a file
        /// </summary>
        public ImmDocument LoadDocument(string filePath)
        {
            if (!_isInitialized)
            {
                LogError("IMM Player is not initialized");
                return null;
            }

            int docId = ImmNativePlugin.LoadFromFile(filePath);
            if (docId < 0)
            {
                LogError($"Failed to load document from: {filePath}");
                return null;
            }

            ImmDocument doc = new ImmDocument(docId, filePath);
            _loadedDocuments[docId] = doc;

            Log($"Loaded document: {filePath} (ID: {docId})");

            // Kick the native state machine once so the load command is processed promptly.
            ImmNativePlugin.GlobalWork(1);
            return doc;
        }

        /// <summary>
        /// Load an IMM document from memory
        /// </summary>
        public ImmDocument LoadDocumentFromMemory(byte[] data, string fileName)
        {
            if (!_isInitialized)
            {
                LogError("IMM Player is not initialized");
                return null;
            }

            IntPtr dataPtr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, dataPtr, data.Length);

            int docId = ImmNativePlugin.LoadFromMemory(fileName, data.Length, dataPtr);

            if (docId < 0)
            {
                // Free memory immediately on failure
                Marshal.FreeHGlobal(dataPtr);
                LogError($"Failed to load document from memory: {fileName}");
                return null;
            }

            // Track the memory pointer - native code loads asynchronously in a background thread,
            // so we must keep the memory alive until the document is unloaded
            _documentMemoryPtrs[docId] = dataPtr;

            ImmDocument doc = new ImmDocument(docId, fileName);
            _loadedDocuments[docId] = doc;

            Log($"Loaded document from memory: {fileName} (ID: {docId})");

            // Kick the native state machine once so the load command is processed promptly.
            ImmNativePlugin.GlobalWork(1);
            return doc;
        }

        /// <summary>
        /// Unload a document
        /// </summary>
        public void UnloadDocument(ImmDocument document)
        {
            if (document == null)
                return;

            int docId = document.DocumentId;

            if (_loadedDocuments.ContainsKey(docId))
            {
                _loadedDocuments.Remove(docId);
            }

            // Free any memory that was allocated for loading from memory
            if (_documentMemoryPtrs.TryGetValue(docId, out IntPtr memPtr))
            {
                _documentMemoryPtrs.Remove(docId);
                Marshal.FreeHGlobal(memPtr);
                Log($"Freed memory buffer for document {docId}");
            }

            document.Unload();
        }

        #endregion

        #region Rendering
        private class PerCameraInfo
        {
            public readonly CommandBuffer CommandBuffer = new CommandBuffer();
            public int CameraId = -1;
            public readonly float[] WorldToHead = new float[16];
            public readonly float[] HeadProj = new float[16];
            public readonly float[] WorldToLeft = new float[16];
            public readonly float[] LeftProj = new float[16];
            public readonly float[] WorldToRight = new float[16];
            public readonly float[] RightProj = new float[16];
            // Quest Vulkan: IMM renders each eye into its own offscreen texture on its
            // dedicated queue; Unity composites it back with a material blit.
            public readonly RenderTexture[] VulkanEyeTargets = new RenderTexture[2];
        }

        private Material _vulkanCompositeMaterial;

        private RenderTexture EnsureVulkanEyeTarget(PerCameraInfo info, int eye, int width, int height)
        {
            RenderTexture rt = info.VulkanEyeTargets[eye];
            if (rt != null && (rt.width != width || rt.height != height))
            {
                rt.Release();
                UnityEngine.Object.Destroy(rt);
                rt = null;
            }
            if (rt == null)
            {
                rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    name = $"IMM Vulkan Eye {eye} (cam {info.CameraId})",
                    antiAliasing = 1,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                rt.Create();
                info.VulkanEyeTargets[eye] = rt;
                Debug.Log($"[IMM_UNITY_VK_OFFSCREEN_20260716] created eye target cam={info.CameraId} eye={eye} {width}x{height}");
            }
            return rt;
        }

        private int _rtDumpCounter;

        private void MaybeDumpVulkanEyeTargets(PerCameraInfo info)
        {
            if (!IsEnvFlagEnabled("IMM_UNITY_VK_DUMP_RTS"))
                return;
            _rtDumpCounter++;
            if (_rtDumpCounter != 300)
                return;
            for (int eye = 0; eye < 2; eye++)
            {
                RenderTexture rt = info.VulkanEyeTargets[eye];
                if (rt == null)
                    continue;
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                string path = Path.Combine(Application.persistentDataPath, $"imm_rt_eye{eye}.png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Destroy(tex);
                Debug.Log($"[IMM_RT_DUMP] wrote {path}");
            }
        }

        private Material GetVulkanCompositeMaterial()
        {
            if (_vulkanCompositeMaterial == null)
            {
                Material loaded = Resources.Load<Material>("ImmVulkanComposite");
                if (loaded != null)
                {
                    _vulkanCompositeMaterial = loaded;
                }
                else
                {
                    Shader shader = Shader.Find("Unlit/Transparent");
                    if (shader != null)
                        _vulkanCompositeMaterial = new Material(shader);
                    Debug.LogWarning("[IMM_UNITY_VK_OFFSCREEN_20260716] ImmVulkanComposite material missing from Resources; Shader.Find fallback " + (_vulkanCompositeMaterial != null ? "succeeded" : "FAILED"));
                }
                if (_vulkanCompositeMaterial != null && !IsEnvFlagEnabled("IMM_UNITY_VK_NO_COMPOSITE_VFLIP"))
                {
                    // IMM renders the offscreen RT bottom-up relative to Unity's
                    // sampling convention (headset showed the scene upside down);
                    // flip V in the composite sample. Flag reverts without rebuild.
                    _vulkanCompositeMaterial.SetTextureScale("_MainTex", new Vector2(1f, -1f));
                    _vulkanCompositeMaterial.SetTextureOffset("_MainTex", new Vector2(0f, 1f));
                }
            }
            return _vulkanCompositeMaterial;
        }

        public bool UsesCommandBufferRendering => _useCommandBufferRendering;

        public bool IsReadyForDocumentLoad
        {
            get
            {
                if (!_isInitialized)
                    return false;

                return RequiresNativeDocumentLoadReady() ? ImmNativePlugin.IsReadyForDocumentLoad() != 0 : true;
            }
        }

        private static bool RequiresNativeDocumentLoadReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        private static bool IsAppleMetalRuntime()
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS
            return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal;
#else
            return false;
#endif
        }

        private static bool IsVulkanRuntime()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
            // Quest runs Vulkan; the native plugin now supports the Unity Vulkan
            // external-device overlay path on Android as well as desktop.
            return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan;
#else
            return false;
#endif
        }

        /// <summary>
        /// Authoritative per-eye view/projection straight from the XR display subsystem's
        /// render parameters. On Quest+Vulkan, Camera.GetStereoViewMatrix returned the
        /// camera's static (untracked) view - IMM content rendered head-locked. The XR
        /// render pass parameters are what the runtime actually tracks with.
        /// </summary>
        private static bool TryGetXrEyeViewProjection(Camera cam, int eye, out Matrix4x4 view, out Matrix4x4 projection)
        {
            view = Matrix4x4.identity;
            projection = Matrix4x4.identity;
            if (cam == null)
                return false;
            SubsystemManager.GetSubsystems(_xrDisplaySubsystems);
            if (_xrDisplaySubsystems.Count == 0)
                return false;
            UnityEngine.XR.XRDisplaySubsystem display = _xrDisplaySubsystems[0];
            if (display == null || !display.running)
                return false;
            int renderPassCount = display.GetRenderPassCount();
            if (renderPassCount <= 0)
                return false;
            // MultiPass: one render pass per eye, one render parameter each.
            // SinglePass: one pass with two render parameters.
            int passIndex = renderPassCount > 1 ? Mathf.Clamp(eye, 0, renderPassCount - 1) : 0;
            display.GetRenderPass(passIndex, out UnityEngine.XR.XRDisplaySubsystem.XRRenderPass renderPass);
            int parameterCount = renderPass.GetRenderParameterCount();
            if (parameterCount <= 0)
                return false;
            int parameterIndex = renderPassCount > 1 ? 0 : Mathf.Clamp(eye, 0, parameterCount - 1);
            renderPass.GetRenderParameter(cam, parameterIndex, out UnityEngine.XR.XRDisplaySubsystem.XRRenderParameter parameter);
            view = parameter.view;
            projection = parameter.projection;
            return true;
        }

        private static RenderTexture GetXrEyeRenderTexture(Camera cam, int stereoMode)
        {
            if (cam == null || !cam.stereoEnabled)
                return null;
            SubsystemManager.GetSubsystems(_xrDisplaySubsystems);
            if (_xrDisplaySubsystems.Count == 0)
                return null;
            UnityEngine.XR.XRDisplaySubsystem display = _xrDisplaySubsystems[0];
            if (display == null || !display.running)
                return null;
            int renderPassCount = display.GetRenderPassCount();
            if (renderPassCount <= 0)
                return null;
            int passIndex = 0;
            if (stereoMode == (int)StereoMode.TwoPass &&
                cam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right &&
                renderPassCount > 1)
            {
                passIndex = 1;
            }
            return display.GetRenderTextureForRenderPass(passIndex);
        }

        private static CameraEvent GetVulkanCommandBufferEvent()
        {
            string value = Environment.GetEnvironmentVariable("IMM_UNITY_VK_CAMERA_EVENT");
            if (string.Equals(value, "AfterImageEffectsOpaque", StringComparison.OrdinalIgnoreCase))
                return CameraEvent.AfterImageEffectsOpaque;
            if (string.Equals(value, "AfterForwardOpaque", StringComparison.OrdinalIgnoreCase))
                return CameraEvent.AfterForwardOpaque;
            if (string.Equals(value, "AfterSkybox", StringComparison.OrdinalIgnoreCase))
                return CameraEvent.AfterSkybox;
            if (string.Equals(value, "BeforeImageEffectsOpaque", StringComparison.OrdinalIgnoreCase))
                return CameraEvent.BeforeImageEffectsOpaque;
            if (string.Equals(value, "AfterEverything", StringComparison.OrdinalIgnoreCase))
                return CameraEvent.AfterEverything;
            if (string.Equals(value, "BeforeForwardOpaque", StringComparison.OrdinalIgnoreCase))
                return CameraEvent.BeforeForwardOpaque;

            // Unity 6 XR on Quest stops executing AfterSkybox command buffers once the
            // XR eye render path takes over (~2 frames after FOCUSED) - PreCull kept
            // issuing events but the marker never dispatched. AfterImageEffectsOpaque is
            // the hook the GLES path has always used successfully on-device.
            return CameraEvent.AfterImageEffectsOpaque;
        }

        public void SetRenderCamera(Camera camera)
        {
            renderCamera = camera;
        }

        public void ClearRenderCamera()
        {
            renderCamera = null;
        }

        private bool ShouldRenderCamera(Camera cam)
        {
            if (cam == null)
                return false;

            if (renderCamera != null && cam != renderCamera)
                return false;

            if (IsEnvFlagEnabled("IMM_UNITY_GAME_CAMERAS_ONLY") && cam.cameraType != CameraType.Game)
                return false;

            string cameraName = Environment.GetEnvironmentVariable("IMM_UNITY_CAMERA_NAME");
            if (!string.IsNullOrEmpty(cameraName) && !string.Equals(cam.name, cameraName, StringComparison.Ordinal))
                return false;

            return true;
        }

        private static bool UseRenderIntoTextureProjection(Camera cam)
        {
            if (IsEnvFlagEnabled("IMM_UNITY_FORCE_BACKBUFFER_PROJECTION"))
                return false;
            if (IsEnvFlagEnabled("IMM_UNITY_FORCE_TEXTURE_PROJECTION"))
                return true;

            // D3D11 desktop Game cameras currently render upright with the
            // backbuffer projection path. Keep the env overrides above for
            // capture/projection A/B tests and keep XR separate from this path.
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 &&
                cam != null &&
                cam.cameraType == CameraType.Game &&
                !cam.stereoEnabled)
            {
#if UNITY_6000_0_OR_NEWER
                // Unity 6's built-in pipeline routes the Game camera through an intermediate texture
                // (not the back buffer) when HDR, MSAA, a target texture, or post-processing is active.
                // Then the GPU projection must use the render-into-texture convention or IMM composites
                // Y-flipped / off-screen (black Game view). Unity 2022.3 always hit the back buffer.
                if (cam.targetTexture != null || cam.allowHDR ||
                    (cam.allowMSAA && QualitySettings.antiAliasing > 1))
                    return true;
#endif
                return false;
            }

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan &&
                cam != null &&
                cam.cameraType == CameraType.Game &&
                !cam.stereoEnabled)
                return true;

            // Unity can mark Game cameras as stereo/XR-active even when we are
            // validating the editor Game view. Do not use stereoEnabled as a
            // proxy for render-into-texture projection. SceneView is the other
            // built-in path that needs texture-style projection here.
            return cam != null && cam.cameraType == CameraType.SceneView;
        }

        private void CleanupCommandBuffers()
        {
            CameraEvent[] events =
            {
                CameraEvent.AfterImageEffectsOpaque,
                CameraEvent.BeforeForwardOpaque,
                CameraEvent.AfterForwardOpaque,
                CameraEvent.AfterSkybox,
                CameraEvent.BeforeImageEffectsOpaque,
                CameraEvent.AfterEverything
            };
            foreach (var kvp in _cameras)
            {
                if (kvp.Key)
                {
                    foreach (CameraEvent cameraEvent in events)
                    {
                        kvp.Key.RemoveCommandBuffer(cameraEvent, kvp.Value.CommandBuffer);
                    }
                }
            }
            _cameras.Clear();
            _configuredVulkanRenderEvents.Clear();
        }

        private int _preCullCount;

        private void OnCameraPreCull(Camera cam)
        {
            if (cam != null)
            {
                _preCullCount++;
                if (_preCullCount <= 12 || _preCullCount % 144 == 0)
                {
                    Matrix4x4 v = cam.worldToCameraMatrix;
                    Matrix4x4 l = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
                    Debug.Log($"[IMM_PRECULL] n={_preCullCount} cam={cam.name} stereoEnabled={cam.stereoEnabled} eye={cam.stereoActiveEye} xrActive={UnityEngine.XR.XRSettings.isDeviceActive} view=[{v.m00:F3} {v.m02:F3} {v.m03:F3}] stereoL=[{l.m00:F3} {l.m02:F3} {l.m03:F3}] eq={(v == l)}");
                }
            }
            if (!_isInitialized || _renderEventFunc == IntPtr.Zero || cam == null)
                return;
            if (!ShouldRenderCamera(cam))
                return;

            PerCameraInfo info = GetOrCreateCameraInfo(cam, _useCommandBufferRendering);

            if (IsVulkanRuntime() && IsEnvFlagEnabled("IMM_UNITY_VK_NO_RENDER_EVENTS"))
            {
                // Diagnostic: issue NO plugin events at all (Quest: every custom marker
                // callback makes Unity finalize the XR frame mid-frame). Native rendering
                // and deferred init will not run.
                info.CommandBuffer.Clear();
                return;
            }

            if (!IsReadyForDocumentLoad)
            {
                // GLES on Android needs native renderer initialization to
                // complete from Unity's render-thread plugin callback, where
                // the GL context is current. Queue this pre-load event before a
                // document exists so LoadFromFile cannot race renderer creation.
                info.CommandBuffer.Clear();
                info.CommandBuffer.IssuePluginEvent(_renderEventFunc, info.CameraId << 8);
                return;
            }

            if (!HasRenderableDocument())
                return;

            int stereoMode = ResolveStereoMode(cam);

            ConvertMatrixToArray(info.WorldToHead, cam.worldToCameraMatrix);
            bool renderIntoTexture = UseRenderIntoTextureProjection(cam);
            Matrix4x4 headProjection = cam.nonJitteredProjectionMatrix;
            ConvertMatrixToArray(info.HeadProj, GL.GetGPUProjectionMatrix(headProjection, renderIntoTexture));

            if (!_lastNearClipLogged.TryGetValue(cam, out float lastNear) || !Mathf.Approximately(lastNear, cam.nearClipPlane))
            {
                _lastNearClipLogged[cam] = cam.nearClipPlane;
                Matrix4x4 gpuProjection = GL.GetGPUProjectionMatrix(headProjection, renderIntoTexture);
                Debug.Log($"{NearDiagPrefix}cam={cam.name} type={cam.cameraType} near={cam.nearClipPlane:F5} far={cam.farClipPlane:F2} renderIntoTex={renderIntoTexture} nonJit(m22={headProjection[10]:F6},m23={headProjection[14]:F6},m32={headProjection[11]:F6}) gpu(m22={gpuProjection[10]:F6},m23={gpuProjection[14]:F6},m32={gpuProjection[11]:F6})");
            }

            if (cam.stereoEnabled)
            {
                ConvertMatrixToArray(info.WorldToLeft, cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left));
                ConvertMatrixToArray(info.LeftProj, GL.GetGPUProjectionMatrix(cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left), renderIntoTexture));
                ConvertMatrixToArray(info.WorldToRight, cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right));
                ConvertMatrixToArray(info.RightProj, GL.GetGPUProjectionMatrix(cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right), renderIntoTexture));
            }

            // Quest+Vulkan: Camera.GetStereoViewMatrix returns the camera's static view
            // (no head tracking) - IMM rendered head-locked. Pull the tracked per-eye
            // view/projection from the XR display subsystem's render parameters instead.
            bool xrMatricesApplied = false;
            if (Application.platform == RuntimePlatform.Android && IsVulkanRuntime() &&
                !IsEnvFlagEnabled("IMM_UNITY_VK_NO_XR_RENDER_PARAMS"))
            {
                if (TryGetXrEyeViewProjection(cam, 0, out Matrix4x4 leftView, out Matrix4x4 leftProj) &&
                    TryGetXrEyeViewProjection(cam, 1, out Matrix4x4 rightView, out Matrix4x4 rightProj))
                {
                    ConvertMatrixToArray(info.WorldToLeft, leftView);
                    ConvertMatrixToArray(info.LeftProj, GL.GetGPUProjectionMatrix(leftProj, renderIntoTexture));
                    ConvertMatrixToArray(info.WorldToRight, rightView);
                    ConvertMatrixToArray(info.RightProj, GL.GetGPUProjectionMatrix(rightProj, renderIntoTexture));
                    xrMatricesApplied = true;
                    if (stereoMode == (int)StereoMode.Mono)
                        stereoMode = (int)StereoMode.TwoPass;
                    if (_preCullCount <= 12 || _preCullCount % 144 == 0)
                        Debug.Log($"[IMM_XRPARAM] n={_preCullCount} L=[{leftView.m00:F3} {leftView.m02:F3} {leftView.m03:F3}] R=[{rightView.m00:F3} {rightView.m02:F3} {rightView.m03:F3}]");
                }
                else if (_preCullCount <= 12 || _preCullCount % 144 == 0)
                {
                    Debug.LogWarning($"[IMM_XRPARAM] n={_preCullCount} XR render parameters unavailable; falling back to camera stereo matrices");
                }
            }

            bool hasStereoMatrices = cam.stereoEnabled || xrMatricesApplied;
            ImmNativePlugin.SetMatrices(
                info.CameraId,
                stereoMode,
                info.WorldToHead,
                info.HeadProj,
                hasStereoMatrices ? info.WorldToLeft : null,
                hasStereoMatrices ? info.LeftProj : null,
                hasStereoMatrices ? info.WorldToRight : null,
                hasStereoMatrices ? info.RightProj : null);
            ImmNativePlugin.SetCameraViewport(info.CameraId, cam.pixelWidth, cam.pixelHeight);
            if (IsVulkanRuntime())
            {
                if (IsEnvFlagEnabled("IMM_UNITY_VK_BLUE_CANARY") && cam.clearFlags != CameraClearFlags.SolidColor)
                {
                    // Diagnostic canary: prove Unity's own Vulkan output path is visible at all.
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.0f, 0.4f, 1.0f, 1.0f);
                    Debug.Log("[IMM_UNITY_VK_CANARY_20260716] camera forced to solid blue clear");
                }

                int vulkanEye = 0;
                if (stereoMode == (int)StereoMode.TwoPass && cam.stereoEnabled && cam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right)
                    vulkanEye = 1;
                bool useOffscreenTargets = Application.platform == RuntimePlatform.Android && !IsEnvFlagEnabled("IMM_UNITY_VK_NO_OFFSCREEN_TARGET");

                if (useOffscreenTargets)
                {
                    // Quest: IMM must not write into the XR eye buffer directly - its own-queue
                    // writes race Unity's camera pass and get cleared/overwritten. Render into an
                    // offscreen texture instead; the composite blit below runs inside Unity's own
                    // pass where ordering is guaranteed.
                    // A/B lever: route BOTH eyes' native draws into eye0's RT. Distinguishes an
                    // eye1-image problem (strokes appear doubled) from an eye1-pass problem
                    // (still a single stroke set).
                    int rtEye = IsEnvFlagEnabled("IMM_UNITY_VK_EYE0_RT_BOTH_EYES") ? 0 : vulkanEye;
                    RenderTexture eyeTarget = EnsureVulkanEyeTarget(info, rtEye, cam.pixelWidth, cam.pixelHeight);
                    ImmNativePlugin.SetVulkanCameraEyeRenderBuffers(
                        info.CameraId,
                        vulkanEye,
                        eyeTarget.colorBuffer.GetNativeRenderBufferPtr(),
                        IntPtr.Zero,
                        eyeTarget.width,
                        eyeTarget.height,
                        1);
                    if (!_loggedVulkanRenderTargetSource.Contains(cam))
                    {
                        _loggedVulkanRenderTargetSource.Add(cam);
                        Debug.Log($"[IMM_UNITY_VK_RT_SRC_20260612] cam={cam.name} cameraId={info.CameraId} source=offscreenRT {eyeTarget.width}x{eyeTarget.height} pixel={cam.pixelWidth}x{cam.pixelHeight} samples=1");
                    }
                }
                else
                {
                    RenderTexture vulkanTargetTexture = cam.targetTexture != null ? cam.targetTexture : cam.activeTexture;
                    string vulkanTargetSource = vulkanTargetTexture != null ? "cameraTexture" : null;
                    if (vulkanTargetTexture == null)
                    {
                        // On Quest (XR + offscreen swapchain) Display.main resolves to a 1x1 dummy
                        // buffer - the real target is the XR eye texture for the current render pass.
                        vulkanTargetTexture = GetXrEyeRenderTexture(cam, stereoMode);
                        if (vulkanTargetTexture != null)
                            vulkanTargetSource = "xrEyePass";
                    }
                    RenderBuffer colorBuffer = vulkanTargetTexture != null ? vulkanTargetTexture.colorBuffer : Display.main.colorBuffer;
                    RenderBuffer depthBuffer = vulkanTargetTexture != null ? vulkanTargetTexture.depthBuffer : Display.main.depthBuffer;
                    int vulkanSampleCount = vulkanTargetTexture != null
                        ? Math.Max(1, vulkanTargetTexture.antiAliasing)
                        : (cam.allowMSAA ? Math.Max(1, QualitySettings.antiAliasing) : 1);
                    if (!_loggedVulkanRenderTargetSource.Contains(cam))
                    {
                        _loggedVulkanRenderTargetSource.Add(cam);
                        string source = vulkanTargetTexture != null ? $"{vulkanTargetSource} {vulkanTargetTexture.width}x{vulkanTargetTexture.height}" : "display";
                        Debug.Log($"[IMM_UNITY_VK_RT_SRC_20260612] cam={cam.name} cameraId={info.CameraId} source={source} pixel={cam.pixelWidth}x{cam.pixelHeight} samples={vulkanSampleCount}");
                    }
                    ImmNativePlugin.SetVulkanCameraRenderBuffers(
                        info.CameraId,
                        colorBuffer.GetNativeRenderBufferPtr(),
                        depthBuffer.GetNativeRenderBufferPtr(),
                        cam.pixelWidth,
                        cam.pixelHeight,
                        vulkanSampleCount);
                }
                // Offscreen path: the render event calls RenderCamera which prepares
                // internally. A managed PrepareCamera here runs on the MAIN thread and
                // races the render-thread event between its prepare and its eye render
                // (no native lock on Android) - the player's global head state can be
                // overwritten mid-eye, splitting the two eyes' views.
                if (!useOffscreenTargets)
                {
                    int prepared = IsEnvFlagEnabled("IMM_UNITY_VK_SKIP_MANAGED_PREPARE")
                        ? 0
                        : ImmNativePlugin.PrepareCamera(info.CameraId);
                    if (prepared == 0 && _loggedVulkanPrepareWarning.Add(cam))
                    {
                        Debug.LogWarning($"[IMM_UNITY_VK_PREPARE_20260612] cam={cam.name} cameraId={info.CameraId} prepared=0");
                    }
                }
            }

            if (_useCameraCallbackRendering)
                return;

            int eyeIndex = 0;
            if (stereoMode == (int)StereoMode.TwoPass && cam.stereoEnabled)
            {
                eyeIndex = cam.stereoActiveEye == Camera.MonoOrStereoscopicEye.Right ? 1 : 0;
            }

            int eventId = (info.CameraId << 8) | (eyeIndex & 0x1);
            info.CommandBuffer.Clear();
            if (IsVulkanRuntime())
            {
                if (!IsEnvFlagEnabled("IMM_UNITY_VK_SKIP_MANAGED_CONFIG") && _configuredVulkanRenderEvents.Add(eventId))
                {
                    int configured = ImmNativePlugin.ConfigureVulkanRenderEvent(eventId);
                    Debug.Log($"[IMM_UNITY_VK_EVENTCFG_20260611] eventId={eventId} configured={configured}");
                }
                bool useCustomBlit = IsEnvFlagEnabled("IMM_UNITY_VK_USE_CUSTOM_BLIT") && !IsEnvFlagEnabled("IMM_UNITY_VK_FORCE_PLAIN_EVENT");
                bool bindCameraTarget = !IsEnvFlagEnabled("IMM_UNITY_VK_SKIP_BIND_CAMERA_TARGET");
                var cameraTarget = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
                if (bindCameraTarget)
                {
                    if (IsEnvFlagEnabled("IMM_UNITY_VK_BIND_CAMERA_DEPTH_TARGET"))
                    {
                        info.CommandBuffer.SetRenderTarget(cameraTarget, new RenderTargetIdentifier(BuiltinRenderTextureType.Depth));
                    }
                    else
                    {
                        info.CommandBuffer.SetRenderTarget(cameraTarget);
                    }
                }
                if (useCustomBlit && _renderEventAndDataFunc != IntPtr.Zero)
                {
                    if (!IsEnvFlagEnabled("IMM_UNITY_VK_SKIP_MANAGED_CONFIG") && _configuredVulkanRenderEvents.Add(VulkanCustomBlitEventId))
                    {
                        int configured = ImmNativePlugin.ConfigureVulkanRenderEvent(VulkanCustomBlitEventId);
                        Debug.Log($"[IMM_UNITY_VK_EVENTCFG_20260611] eventId={VulkanCustomBlitEventId} configured={configured}");
                    }
                    info.CommandBuffer.IssuePluginCustomBlit(_renderEventAndDataFunc, (uint)eventId, cameraTarget, cameraTarget, 0, 0);
                }
                else
                {
                    info.CommandBuffer.IssuePluginEvent(_renderEventFunc, eventId);
                }
                if (Application.platform == RuntimePlatform.Android && !IsEnvFlagEnabled("IMM_UNITY_VK_NO_OFFSCREEN_TARGET"))
                {
                    // Probe: blit the LEFT RT into BOTH eyes. Right eye lights up -> the
                    // right RT was empty (native render side); still dark -> the right
                    // pass composite itself is broken (Unity side).
                    int blitEye = IsEnvFlagEnabled("IMM_UNITY_VK_BLIT_LEFT_RT_BOTH_EYES") ? 0 : (eyeIndex & 1);
                    RenderTexture eyeTarget = info.VulkanEyeTargets[blitEye];
                    Material composite = GetVulkanCompositeMaterial();
                    if (eyeTarget != null && composite != null)
                    {
                        // The plugin event above renders IMM into the offscreen texture
                        // (fence-completed on IMM's own queue before the callback returns);
                        // composite it into the eye buffer inside Unity's own pass.
                        info.CommandBuffer.Blit(eyeTarget, cameraTarget, composite);
                        if (_preCullCount <= 12)
                            Debug.Log($"[IMM_UNITY_VK_BLIT] n={_preCullCount} eye={eyeIndex} blitEye={blitEye} rt={eyeTarget.GetInstanceID()} colorPtr=0x{eyeTarget.colorBuffer.GetNativeRenderBufferPtr().ToInt64():X}");
                    }
                }
                AppendVulkanOverlayFixtureDraw(info.CommandBuffer, cam);
            }
            else
            {
#if UNITY_6000_0_OR_NEWER
                // Unity 6's render graph doesn't guarantee the camera color target is bound when the
                // plugin render event runs (Unity <=2022 left it bound implicitly). Bind it explicitly
                // so the native IMM render draws into the camera target instead of nothing (black view).
                info.CommandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
#endif
                info.CommandBuffer.IssuePluginEvent(_renderEventFunc, eventId);
            }
        }

        private static void AppendVulkanOverlayFixtureDraw(CommandBuffer commandBuffer, Camera cam)
        {
            if (!IsEnvFlagEnabled("IMM_UNITY_VK_COMMAND_BUFFER_OVERLAY_FIXTURE") || commandBuffer == null || cam == null)
                return;

            if (_vulkanOverlayFixtureMesh == null)
            {
                _vulkanOverlayFixtureMesh = new Mesh { name = "IMM Vulkan Overlay Fixture Quad" };
                _vulkanOverlayFixtureMesh.vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0.0f),
                    new Vector3( 0.5f, -0.5f, 0.0f),
                    new Vector3( 0.5f,  0.5f, 0.0f),
                    new Vector3(-0.5f,  0.5f, 0.0f)
                };
                _vulkanOverlayFixtureMesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 };
                _vulkanOverlayFixtureMesh.RecalculateBounds();
            }

            if (_vulkanOverlayFixtureMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                _vulkanOverlayFixtureMaterial = shader != null
                    ? new Material(shader)
                    : new Material(Shader.Find("Unlit/Color"));
                _vulkanOverlayFixtureMaterial.color = new Color(1.0f, 0.05f, 0.02f, 1.0f);
                _vulkanOverlayFixtureMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
                _vulkanOverlayFixtureMaterial.SetInt("_ZWrite", 0);
                _vulkanOverlayFixtureMaterial.SetInt("_Cull", (int)CullMode.Off);
            }

            commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            commandBuffer.DrawMesh(_vulkanOverlayFixtureMesh, Matrix4x4.TRS(new Vector3(0.45f, -0.05f, 0.0f), Quaternion.identity, Vector3.one * 0.55f), _vulkanOverlayFixtureMaterial);
        }

        private PerCameraInfo GetOrCreateCameraInfo(Camera cam, bool attachCommandBuffer)
        {
            if (!_cameras.TryGetValue(cam, out PerCameraInfo info))
            {
                info = new PerCameraInfo();
                info.CameraId = _cameras.Count;
                info.CommandBuffer.name = "Render IMM Content";
                _cameras[cam] = info;
                if (attachCommandBuffer)
                {
                    CameraEvent renderEvent = IsVulkanRuntime() ? GetVulkanCommandBufferEvent() : CameraEvent.AfterImageEffectsOpaque;
                    if (IsVulkanRuntime())
                    {
                        Debug.Log($"[IMM_UNITY_VK_EVENT_20260612] cam={cam.name} cameraId={info.CameraId} renderEvent={renderEvent}");
                    }
                    cam.AddCommandBuffer(renderEvent, info.CommandBuffer);
                }
            }
            return info;
        }

        private void OnCameraPostRender(Camera cam)
        {
            if (!_useCameraCallbackRendering || !_isInitialized || _renderEventFunc == IntPtr.Zero || cam == null)
                return;
            if (!ShouldRenderCamera(cam))
                return;
            if (!HasRenderableDocument())
                return;

            if (!_cameras.TryGetValue(cam, out PerCameraInfo info))
                return;

            if (IsVulkanRuntime() && IsEnvFlagEnabled("IMM_UNITY_VK_SAMPLE_WAIT_FOR_END_OF_FRAME"))
                return;

            if (IsVulkanRuntime() && IsEnvFlagEnabled("IMM_UNITY_VK_SAMPLE_EVENT1") && !IsEnvFlagEnabled("IMM_UNITY_VK_SAMPLE_WAIT_FOR_END_OF_FRAME"))
            {
                const int sampleEventId = 1;
                if (info.CameraId != 1)
                    return;
                if (!IsEnvFlagEnabled("IMM_UNITY_VK_SKIP_MANAGED_CONFIG") && _configuredVulkanRenderEvents.Add(sampleEventId))
                {
                    int configured = ImmNativePlugin.ConfigureVulkanRenderEvent(sampleEventId);
                    Debug.Log($"[IMM_UNITY_VK_SAMPLE_EVENT1_20260612] eventId={sampleEventId} camera={info.CameraId} configured={configured}");
                }
                GL.IssuePluginEvent(_renderEventFunc, sampleEventId);
                return;
            }

            int eventId = info.CameraId << 8;
            if (_appleMetalEventLogCount < 8)
            {
                Debug.Log($"[IMM_UNITY_METAL_EVENT] camera={info.CameraId} viewport={cam.pixelWidth}x{cam.pixelHeight} eventId={eventId}");
                _appleMetalEventLogCount++;
            }
            GL.IssuePluginEvent(_renderEventFunc, eventId);
        }

        private IEnumerator IssueVulkanSampleEventAfterEndOfFrame()
        {
            WaitForEndOfFrame wait = new WaitForEndOfFrame();
            while (enabled)
            {
                yield return wait;
                if (!IsVulkanRuntime() || !IsEnvFlagEnabled("IMM_UNITY_VK_SAMPLE_EVENT1") || !_isInitialized || _renderEventFunc == IntPtr.Zero)
                    continue;
                if (!HasRenderableDocument())
                    continue;

                bool hasMainCameraTarget = false;
                foreach (PerCameraInfo info in _cameras.Values)
                {
                    if (info.CameraId == 1)
                    {
                        hasMainCameraTarget = true;
                        break;
                    }
                }
                if (!hasMainCameraTarget)
                    continue;

                const int sampleEventId = 1;
                if (!IsEnvFlagEnabled("IMM_UNITY_VK_SKIP_MANAGED_CONFIG") && _configuredVulkanRenderEvents.Add(sampleEventId))
                {
                    int configured = ImmNativePlugin.ConfigureVulkanRenderEvent(sampleEventId);
                    Debug.Log($"[IMM_UNITY_VK_SAMPLE_WFE_20260612] eventId={sampleEventId} configured={configured}");
                }
                GL.IssuePluginEvent(_renderEventFunc, sampleEventId);
            }
        }

        private bool HasRenderableDocument()
        {
            foreach (ImmDocument doc in _loadedDocuments.Values)
            {
                if (doc != null && doc.IsSequenceReady())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Set camera matrices for rendering
        /// </summary>
        public void SetCameraMatrices(
            int cameraId,
            Camera camera,
            StereoMode stereoMode = StereoMode.Mono)
        {
            if (!_isInitialized)
                return;

            Matrix4x4 worldToCamera = camera.worldToCameraMatrix;
            bool renderIntoTexture = UseRenderIntoTextureProjection(camera);
            Matrix4x4 projection = GL.GetGPUProjectionMatrix(camera.nonJitteredProjectionMatrix, renderIntoTexture);

            float[] world2head = MatrixToFloatArray(worldToCamera);
            float[] prjHead = MatrixToFloatArray(projection);

            int stereoType = (int)stereoMode;

            ImmNativePlugin.SetMatrices(
                cameraId,
                stereoType,
                world2head,
                prjHead,
                null, null, null, null);
            ImmNativePlugin.SetCameraViewport(cameraId, camera.pixelWidth, camera.pixelHeight);
        }

        /// <summary>
        /// Set camera matrices for stereo rendering
        /// </summary>
        public void SetStereoCameraMatrices(
            int cameraId,
            Matrix4x4 world2head,
            Matrix4x4 projectionHead,
            Matrix4x4 world2leftEye,
            Matrix4x4 projectionLeft,
            Matrix4x4 world2rightEye,
            Matrix4x4 projectionRight,
            StereoMode stereoMode)
        {
            if (!_isInitialized)
                return;

            ImmNativePlugin.SetMatrices(
                cameraId,
                (int)stereoMode,
                MatrixToFloatArray(world2head),
                MatrixToFloatArray(projectionHead),
                MatrixToFloatArray(world2leftEye),
                MatrixToFloatArray(projectionLeft),
                MatrixToFloatArray(world2rightEye),
                MatrixToFloatArray(projectionRight));
        }

        /// <summary>
        /// Issue a render event for a camera
        /// </summary>
        public void IssueRenderEvent(int cameraId)
        {
            if (_renderEventFunc != IntPtr.Zero)
            {
                int eventId = cameraId << 8;
                GL.IssuePluginEvent(_renderEventFunc, eventId);
            }
        }

        #endregion

        #region Utility

        public struct PlayerInfoManaged
        {
            public Color BackgroundColor;
        }

        public PlayerInfoManaged GetPlayerInfo()
        {
            PlayerInfo info;
            ImmNativePlugin.GetPlayerInfo(out info);
            return new PlayerInfoManaged
            {
                BackgroundColor = new Color(
                    info.backgroundColor.red,
                    info.backgroundColor.green,
                    info.backgroundColor.blue)
            };
        }

        private static float[] MatrixToFloatArray(Matrix4x4 matrix)
        {
            float[] result = new float[16];
            for (int i = 0; i < 16; i++)
            {
                result[i] = matrix[i];
            }
            return result;
        }

        private static void ConvertMatrixToArray(float[] dst, Matrix4x4 matrix)
        {
            for (int i = 0; i < 16; i++)
            {
                dst[i] = matrix[i];
            }
        }

        private static int ResolveStereoMode(Camera cam)
        {
            if (!cam.stereoEnabled)
                return (int)StereoMode.Mono;

            // Keep XRSettings optional so non-XR Unity package builds compile without XR package symbols.
            Type xrSettingsType = Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule");
            object mode = xrSettingsType?.GetProperty("stereoRenderingMode")?.GetValue(null);
            string modeName = mode?.ToString();

            if (modeName == "SinglePass")
                return (int)StereoMode.SinglePass;

            // The native plugin doesn't support instanced single-pass; force two-pass.
            return (int)StereoMode.TwoPass;
        }

        #endregion

        #region Enums

        public enum StereoMode
        {
            Mono = 0,
            TwoPass = 1,
            SinglePass = 2
        }

        #endregion
    }
}
