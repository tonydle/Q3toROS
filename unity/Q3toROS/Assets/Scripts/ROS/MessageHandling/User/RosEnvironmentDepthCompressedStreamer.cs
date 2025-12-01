using UnityEngine;
using Meta.XR.EnvironmentDepth;

namespace Unity.Robotics
{
    /// <summary>
    /// Publishes a visualized environment depth frame as sensor_msgs/CompressedImage,
    /// using the same texture that EnvironmentDepthManager pushes to shaders.
    ///
    /// Normalized depth texture is tone-mapped via Graphics.Blit to RGBA.
    /// </summary>
    public class RosEnvironmentDepthCompressedStreamer : MonoBehaviour
    {
        [Header("Sources")]
        public EnvironmentDepthManager DepthManager;
        public RosPublisherCompressedImage ImagePublisher;

        [Header("ROS Header")]
        public string FrameId = "quest3_depth";

        [Header("Depth texture")]
        [Tooltip("0 = left eye slice, 1 = right eye slice in the depth texture array.")]
        [Range(0, 1)] public int EyeSlice = 0;

        [Tooltip("Global shader property name that EnvironmentDepthManager writes to.")]
        public string DepthTextureProperty = "_EnvironmentDepthTexture";

        [Header("Streaming")]
        [Tooltip("Publish rate (Hz).")]
        [Range(1f, 120f)] public float PublishHz = 15f;

        private float _nextPublishTime;
        private RenderTexture _eyeTexture;

        private void Awake()
        {
            if (DepthManager == null)
                Debug.LogError("[RosEnvironmentDepthCompressedStreamer] DepthManager not set.");
            if (ImagePublisher == null)
                Debug.LogError("[RosEnvironmentDepthCompressedStreamer] ImagePublisher not set.");

            if (!EnvironmentDepthManager.IsSupported)
            {
                Debug.LogWarning("[RosEnvironmentDepthCompressedStreamer] Environment depth not supported on this device.");
            }
        }

        private void OnEnable()
        {
            if (DepthManager != null)
                DepthManager.enabled = true;

            _nextPublishTime = Time.time;
        }

        private void OnDisable()
        {
            if (DepthManager != null)
                DepthManager.enabled = false;

            if (_eyeTexture == null)
            {
                return;
            }

            _eyeTexture.Release();
            Destroy(_eyeTexture);
            _eyeTexture = null;
        }

        private void LateUpdate()
        {
            if (!DepthManager || !ImagePublisher)
                return;

            if (!DepthManager.IsDepthAvailable)
                return;

            // Rate limiting
            if (Time.time < _nextPublishTime)
                return;

            _nextPublishTime += 1f / Mathf.Max(1f, PublishHz);

            // Grab the global depth texture (2D array: stereo)
            var globalTex = Shader.GetGlobalTexture(DepthTextureProperty) as RenderTexture;
            if (!globalTex)
                return;

            var w = globalTex.width;
            var h = globalTex.height;

            // Allocate per-eye 2D RT lazily
            if (!_eyeTexture || _eyeTexture.width != w || _eyeTexture.height != h)
            {
                if (_eyeTexture)
                {
                    _eyeTexture.Release();
                    Destroy(_eyeTexture);
                }

                // Single-channel-ish; will be blitted to RGBA32 in the publisher anyway
                // TODO: using R8 and updating the publisher to handle it
                _eyeTexture = new RenderTexture(w, h, 0, RenderTextureFormat.R16);
                _eyeTexture.Create();
            }

            var slice = Mathf.Clamp(EyeSlice, 0, 1);

            // Copy one slice (eye) from the 2D array to our 2D texture
            Graphics.CopyTexture(globalTex, slice, 0, _eyeTexture, 0, 0);

            // Send it
            ImagePublisher.Publish(_eyeTexture, FrameId);
        }
    }
}
