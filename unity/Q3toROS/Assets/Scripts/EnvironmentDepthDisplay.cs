using UnityEngine;
using Meta.XR.EnvironmentDepth;

public class EnvironmentDepthDisplay : MonoBehaviour
{
    [Header("Sources")]
    public EnvironmentDepthManager DepthManager;

    [Header("Display")]
    public Renderer QuadRenderer;
    [Tooltip("Camera transform (e.g., center eye). If null, uses Camera.main.")]
    public Transform CameraTransform;
    [Tooltip("Distance in front of the camera to place the quad.")]
    public float QuadDistance = 0.5f;
    [Tooltip("0 = left eye, 1 = right eye slice in the depth texture array.")]
    [Range(0, 1)] public int EyeSlice = 0;

    [Header("Shader property names")]
    [Tooltip("Global shader property that EnvironmentDepthManager writes to.")]
    public string DepthTextureProperty = "_EnvironmentDepthTexture";

    private RenderTexture _eyeTexture;

    private void Awake()
    {
        if (DepthManager == null)
            Debug.LogError("[EnvironmentDepthDisplay] DepthManager not set.");
        if (QuadRenderer == null)
            Debug.LogError("[EnvironmentDepthDisplay] QuadRenderer not set.");

        if (!EnvironmentDepthManager.IsSupported)
        {
            Debug.LogWarning("[EnvironmentDepthDisplay] Environment depth not supported on this device.");
        }
    }

    private void OnEnable()
    {
        // Just make sure depth manager is enabled so it starts requesting depth
        if (DepthManager != null)
            DepthManager.enabled = true;
    }

    private void OnDisable()
    {
        if (DepthManager != null)
            DepthManager.enabled = false;

        if (_eyeTexture != null)
        {
            _eyeTexture.Release();
            Destroy(_eyeTexture);
            _eyeTexture = null;
        }
    }

    private void Start()
    {
        if (QuadRenderer != null)
            QuadRenderer.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (DepthManager == null || QuadRenderer == null)
            return;

        // Wait until depth is actually available
        if (!DepthManager.IsDepthAvailable)
            return;

        // Grab global depth texture set by EnvironmentDepthManager
        var globalTex = Shader.GetGlobalTexture(DepthTextureProperty) as RenderTexture;
        if (globalTex == null)
            return;

        // Depth texture is a 2D array (stereo), we’ll copy one slice into a 2D RT
        int w = globalTex.width;
        int h = globalTex.height;

        if (_eyeTexture == null || _eyeTexture.width != w || _eyeTexture.height != h)
        {
            if (_eyeTexture != null)
            {
                _eyeTexture.Release();
                Destroy(_eyeTexture);
            }

            // Use a single-channel-ish format; Unity will still display it via mainTexture
            _eyeTexture = new RenderTexture(w, h, 0, RenderTextureFormat.R16);
            _eyeTexture.Create();
        }

        // Copy from 2D array slice -> 2D texture (GPU only, no CPU readback)
        int slice = Mathf.Clamp(EyeSlice, 0, 1);
        Graphics.CopyTexture(globalTex, slice, 0, _eyeTexture, 0, 0);

        // Show on quad
        if (!QuadRenderer.gameObject.activeSelf)
            QuadRenderer.gameObject.SetActive(true);

        QuadRenderer.material.mainTexture = _eyeTexture;

        // Position quad in front of camera (like your passthrough display)
        Transform camT = CameraTransform;
        Camera cam = null;

        if (camT == null && Camera.main != null)
        {
            camT = Camera.main.transform;
            cam = Camera.main;
        }
        else if (CameraTransform != null)
        {
            cam = CameraTransform.GetComponent<Camera>();
            if (cam == null && Camera.main != null)
                cam = Camera.main;
        }

        if (camT == null || cam == null)
            return;

        Transform quadTransform = QuadRenderer.transform;

        quadTransform.position = camT.position + camT.forward * QuadDistance;
        quadTransform.rotation = camT.rotation;

        // Roughly match camera FOV so it fills the view
        float height = 2f * QuadDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * cam.aspect;
        quadTransform.localScale = new Vector3(width, height, 1f);
    }
}
