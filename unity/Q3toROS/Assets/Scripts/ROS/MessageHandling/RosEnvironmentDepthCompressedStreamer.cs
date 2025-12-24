using UnityEngine;
using Meta.XR.EnvironmentDepth;

namespace Unity.Robotics
{
    /// <summary>
    /// Publishes a visualized environment depth frame as sensor_msgs/CompressedImage,
    /// using the same texture that EnvironmentDepthManager pushes to shaders.
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
        [Range(0, 1)] public int EyeSlice;

        [Tooltip("Global shader property name that EnvironmentDepthManager writes to.")]
        public string DepthTextureProperty = "_EnvironmentDepthTexture";

        [Header("Streaming")]
        [Tooltip("Publish rate (Hz).")]
        [Range(1f, 120f)] public float PublishHz = 15f;

        // String version for UI binding
        public string PublishHzString
        {
            get => PublishHz.ToString();
            set => PublishHz = float.Parse(value);
        }

        private float m_nextPublishTime;
        private RenderTexture m_eyeTexture;

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

            m_nextPublishTime = Time.time;
        }

        private void OnDisable()
        {
            if (DepthManager != null)
                DepthManager.enabled = false;

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
            if (!DepthManager || !ImagePublisher)
                return;

            if (!DepthManager.IsDepthAvailable)
                return;

            // Rate limiting
            if (Time.time < m_nextPublishTime)
                return;

            m_nextPublishTime += 1f / Mathf.Max(1f, PublishHz);

            // Grab the global depth texture (2D array: stereo)
            var globalTex = Shader.GetGlobalTexture(DepthTextureProperty) as RenderTexture;
            if (!globalTex)
                return;

            var w = globalTex.width;
            var h = globalTex.height;

            // Allocate per-eye texture
            if (!m_eyeTexture || m_eyeTexture.width != w || m_eyeTexture.height != h)
            {
                if (m_eyeTexture)
                {
                    m_eyeTexture.Release();
                    Destroy(m_eyeTexture);
                }

                // Single-channel R16 texture
                m_eyeTexture = new RenderTexture(w, h, 0, RenderTextureFormat.R16);
                _ = m_eyeTexture.Create();
            }

            var slice = Mathf.Clamp(EyeSlice, 0, 1);

            // Copy one slice (eye) from the 2D array to our 2D texture
            Graphics.CopyTexture(globalTex, slice, 0, m_eyeTexture, 0, 0);

            // Send it
            ImagePublisher.Publish(m_eyeTexture, FrameId);
        }
    }
}
