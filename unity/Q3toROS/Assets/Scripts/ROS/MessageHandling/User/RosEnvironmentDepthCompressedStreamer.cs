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
        public EnvironmentDepthManager depthManager;
        public RosPublisherCompressedImage imagePublisher;

        [Header("ROS Header")]
        public string frameId = "quest3_depth";
        
        [Header("Depth texture")]
        [Tooltip("0 = left eye slice, 1 = right eye slice in the depth texture array.")]
        [Range(0, 1)] public int eyeSlice;

        [Tooltip("Global shader property name that EnvironmentDepthManager writes to.")]
        public string depthTextureProperty = "_EnvironmentDepthTexture";
        
        [Header("Streaming")]
        [Tooltip("Publish rate (Hz).")]
        [Range(1f, 120f)] public float publishHz = 15f;
        
        public string PublishHzString
        {
            get => publishHz.ToString();
            set => publishHz = float.Parse(value);
        }

        private float m_nextPublishTime;
        private RenderTexture m_eyeTexture;

        private void Awake()
        {
            if (depthManager == null)
                Debug.LogError("[RosEnvironmentDepthCompressedStreamer] DepthManager not set.");
            if (imagePublisher == null)
                Debug.LogError("[RosEnvironmentDepthCompressedStreamer] ImagePublisher not set.");

            if (!EnvironmentDepthManager.IsSupported)
            {
                Debug.LogWarning("[RosEnvironmentDepthCompressedStreamer] Environment depth not supported on this device.");
            }
        }

        private void OnEnable()
        {
            if (depthManager != null)
                depthManager.enabled = true;

            m_nextPublishTime = Time.time;
        }

        private void OnDisable()
        {
            if (depthManager != null)
                depthManager.enabled = false;

            if (m_eyeTexture == null)
            {
                return;
            }

            m_eyeTexture.Release();
            Destroy(m_eyeTexture);
            m_eyeTexture = null;
        }

        private void LateUpdate()
        {
            if (!depthManager || !imagePublisher)
                return;

            if (!depthManager.IsDepthAvailable)
                return;

            // Rate limiting
            if (Time.time < m_nextPublishTime)
                return;

            m_nextPublishTime += 1f / Mathf.Max(1f, publishHz);

            // Grab the global depth texture (2D array: stereo)
            var globalTex = Shader.GetGlobalTexture(depthTextureProperty) as RenderTexture;
            if (!globalTex)
                return;

            var w = globalTex.width;
            var h = globalTex.height;

            // Allocate per-eye 2D RT lazily
            if (!m_eyeTexture || m_eyeTexture.width != w || m_eyeTexture.height != h)
            {
                if (m_eyeTexture)
                {
                    m_eyeTexture.Release();
                    Destroy(m_eyeTexture);
                }

                // Single-channel-ish; will be blitted to RGBA32 in the publisher anyway
                // TODO: using R8 and updating the publisher to handle it
                m_eyeTexture = new RenderTexture(w, h, 0, RenderTextureFormat.R16);
                m_eyeTexture.Create();
            }

            var slice = Mathf.Clamp(eyeSlice, 0, 1);

            // Copy one slice (eye) from the 2D array to our 2D texture
            Graphics.CopyTexture(globalTex, slice, 0, m_eyeTexture, 0, 0);

            // Send it
            imagePublisher.Publish(m_eyeTexture, frameId);
        }
    }
}
