using System;
using UnityEngine;
using RosMsgCompressedImage = RosMessageTypes.Sensor.CompressedImageMsg;

namespace Unity.Robotics
{
    /// <summary>
    /// Publishes sensor_msgs/CompressedImage from a Texture2D.
    /// Works with passthrough snapshots, WebCamTexture, RenderTextures, etc.
    ///
    /// For testing, can repeatedly publish a specified Texture2D at a set rate.
    /// </summary>
    public class RosPublisherCompressedImage : RosPublisher<RosMsgCompressedImage>
    {
        [Header("Image source & header")]
        [SerializeField] private string m_frameID = "camera";
        [Tooltip("jpeg or png")]
        [SerializeField] private string m_format = "jpeg";

        [Header("JPEG options (if format==jpeg)")]
        [Range(1, 100)]
        [SerializeField] private int m_jpegQuality = 80;

        // Reusable buffer for texture readback
        private Texture2D m_scratchReadable;

        // ---------- TEST / INSPECTOR STREAMING ----------
        [Header("Test Streaming (Inspector)")]
        [Tooltip("If true, publishes the Test Texture at PublishHz automatically.")]
        [SerializeField] private bool m_publishTestTexture = false;

        [Tooltip("Texture to publish repeatedly for testing.")]
        [SerializeField] private Texture2D m_testTexture;

        [Tooltip("Header.frame_id to use when publishing the Test Texture.")]
        [SerializeField] private string m_testFrameId = "camera";

        [Tooltip("Publish rate for test streaming (Hz).")]
        [Range(1f, 120f)]
        [SerializeField] private float m_publishHz = 10f;

        private float m_nextPublishTime;
        private byte[] m_cachedBytes;
        private bool m_cacheDirty = true;

        private RosMsgCompressedImage m_message;

        protected override void Start()
        {
            base.Start();
            m_message = new RosMsgCompressedImage();
            m_nextPublishTime = Time.time;
            RebuildCacheIfNeeded();
        }

        private void Update()
        {
            if (!m_publishTestTexture || m_testTexture == null) return;

            if (Time.time >= m_nextPublishTime)
            {
                m_nextPublishTime += 1f / Mathf.Max(1f, m_publishHz);

                // Use cached bytes (fast); refresh cache only when texture/params change
                RebuildCacheIfNeeded();
                if (m_cachedBytes != null && m_cachedBytes.Length > 0)
                {
                    FillHeader(m_testFrameId);
                    m_message.format = m_format.ToLowerInvariant();
                    m_message.data = m_cachedBytes;
                    Publish(m_message);
                }
            }
        }

        private void MarkCacheDirty()
        {
            m_cacheDirty = true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Any inspector change that affects encoding should mark cache dirty
            m_cacheDirty = true;
        }
#endif

        private void RebuildCacheIfNeeded()
        {
            if (!m_cacheDirty || m_testTexture == null) return;

            var readable = EnsureReadable(m_testTexture, ref m_scratchReadable);

            m_cachedBytes = m_format.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? readable.EncodeToPNG()
                : readable.EncodeToJPG(Mathf.Clamp(m_jpegQuality, 1, 100));

            m_cacheDirty = false;
        }

        /// <summary>Publish a Texture2D directly</summary>
        public void Publish(Texture2D tex, string frameIdOverride = null)
        {
            if (!tex) return;

            var readable = EnsureReadable(tex, ref m_scratchReadable);
            var bytes = m_format.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? readable.EncodeToPNG()
                : readable.EncodeToJPG(Mathf.Clamp(m_jpegQuality, 1, 100));

            FillHeader(frameIdOverride);
            m_message.format = m_format.ToLowerInvariant();
            m_message.data = bytes;

            Publish(m_message);
        }

        /// <summary>Publish any Texture. Will be copied to a readable Texture2D.</summary>
        public void Publish(Texture tex, string frameIdOverride = null)
        {
            if (tex == null) return;

            var readable = BlitToReadable(tex, ref m_scratchReadable);

            var bytes = m_format.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? readable.EncodeToPNG()
                : readable.EncodeToJPG(Mathf.Clamp(m_jpegQuality, 1, 100));

            FillHeader(frameIdOverride);
            m_message.format = m_format.ToLowerInvariant();
            m_message.data = bytes;

            Publish(m_message);
        }

        /// <summary>Publish from an active WebCamTexture.</summary>
        public void Publish(WebCamTexture webcamTex, string frameIdOverride = null)
        {
            if (webcamTex == null || !webcamTex.isPlaying || webcamTex.width <= 16) return;

            // Re-allocate scratch to match webcam size
            if (m_scratchReadable == null || m_scratchReadable.width != webcamTex.width || m_scratchReadable.height != webcamTex.height)
                m_scratchReadable = new Texture2D(webcamTex.width, webcamTex.height, TextureFormat.RGBA32, false);

            m_scratchReadable.SetPixels32(webcamTex.GetPixels32());
            m_scratchReadable.Apply(false, false);

            var bytes = m_format.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? m_scratchReadable.EncodeToPNG()
                : m_scratchReadable.EncodeToJPG(Mathf.Clamp(m_jpegQuality, 1, 100));

            FillHeader(frameIdOverride);
            m_message.format = m_format.ToLowerInvariant();
            m_message.data = bytes;

            Publish(m_message);
        }

        private void FillHeader(string frameIdOverride)
        {
            m_message.header.frame_id = string.IsNullOrEmpty(frameIdOverride) ? m_frameID : frameIdOverride;

            var now = DateTime.UtcNow;
            var epochMs = (long)(now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            m_message.header.stamp.sec = (int)(epochMs / 1000);
            m_message.header.stamp.nanosec = (uint)(epochMs % 1000 * 1_000_000);
        }

        /// <summary>Ensure the given Texture2D is readable; if not, makes a readable copy into scratch.</summary>
        private static Texture2D EnsureReadable(Texture2D src, ref Texture2D scratch)
        {
            return src.isReadable ? src : BlitToReadable(src, ref scratch);
        }

        /// <summary>Copies any Texture to a readable RGBA32 Texture2D via RenderTexture.</summary>
        private static Texture2D BlitToReadable(Texture src, ref Texture2D scratch)
        {
            int w = src.width, h = src.height;

            if (!scratch || scratch.width != w || scratch.height != h || scratch.format != TextureFormat.RGBA32)
                scratch = new Texture2D(w, h, TextureFormat.RGBA32, false);

            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;

            try
            {
                Graphics.Blit(src, rt);
                RenderTexture.active = rt;
                scratch.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                scratch.Apply(false, false);
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
            return scratch;
        }

        // -------- Setters (also mark cache dirty) --------
        public void SetFrameId(string frameId) { m_frameID = frameId; MarkCacheDirty(); }
        public void SetFormat(string fmt) { m_format = string.IsNullOrEmpty(fmt) ? "jpeg" : fmt.ToLowerInvariant(); MarkCacheDirty(); }
        public void SetJpegQuality(int q) { m_jpegQuality = Mathf.Clamp(q, 1, 100); MarkCacheDirty(); }

        // Test controls
        public void SetTestTexture(Texture2D tex) { m_testTexture = tex; MarkCacheDirty(); }
        public void SetPublishTestTexture(bool enable) { m_publishTestTexture = enable; }
        public void StartTestPublishing() { m_publishTestTexture = true; m_nextPublishTime = Time.time; }
        public void StopTestPublishing() { m_publishTestTexture = false; }
        public void SetPublishHz(float hz) { m_publishHz = Mathf.Clamp(hz, 1f, 120f); }
    }
}
