using System;
using UnityEngine;
using RosMsgCompressedImage = RosMessageTypes.Sensor.CompressedImageMsg;

namespace Unity.Robotics
{
    /// <summary>
    /// Publishes sensor_msgs/CompressedImage from a Texture2D.
    /// Works with passthrough snapshots, WebCamTexture, RenderTextures, etc.
    ///
    /// Test mode: set a Texture2D in the inspector and stream it at a fixed rate.
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

        [Header("Processing")]
        [Tooltip("Flip vertically after readback (useful for GL→ROS coord differences)")]
        [SerializeField] private bool m_flipVertical = false;

        // Reusable buffer to avoid GC churn when making a readable copy
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

        [Tooltip("If true, re-encode every frame (uses more CPU). If false, cache bytes and only update timestamps.")]
        [SerializeField] private bool m_reencodeEveryFrame = false;

        private float m_nextPublishTime;
        private byte[] m_cachedBytes;
        private int m_cachedW, m_cachedH;
        private string m_cachedFormat;
        private bool m_cacheDirty = true;

        protected override void Start()
        {
            base.Start();
            _message = new RosMsgCompressedImage();
            m_nextPublishTime = Time.time;
            RebuildCacheIfNeeded();
        }

        private void Update()
        {
            if (!m_publishTestTexture || m_testTexture == null) return;

            if (Time.time >= m_nextPublishTime)
            {
                m_nextPublishTime += 1f / Mathf.Max(1f, m_publishHz);

                if (m_reencodeEveryFrame)
                {
                    // Re-encode each time (simple but CPU-heavy)
                    Publish(m_testTexture, m_testFrameId);
                }
                else
                {
                    // Use cached bytes (fast); refresh cache only when texture/params change
                    RebuildCacheIfNeeded();
                    if (m_cachedBytes != null && m_cachedBytes.Length > 0)
                    {
                        FillHeader(m_testFrameId);
                        _message.format = m_format.ToLowerInvariant();
                        _message.data = m_cachedBytes;
                        Publish(_message);
                    }
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
            if (m_flipVertical) FlipInPlaceVertical(readable);

            m_cachedBytes = m_format.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? readable.EncodeToPNG()
                : readable.EncodeToJPG(Mathf.Clamp(m_jpegQuality, 1, 100));

            m_cachedW = readable.width;
            m_cachedH = readable.height;
            m_cachedFormat = m_format.ToLowerInvariant();
            m_cacheDirty = false;
        }

        /// <summary>Publish a Texture2D directly (must be readable or we'll copy it).</summary>
        public void Publish(Texture2D tex, string frameIdOverride = null)
        {
            if (!tex) return;

            var readable = EnsureReadable(tex, ref m_scratchReadable);
            if (m_flipVertical) FlipInPlaceVertical(readable);

            var bytes = m_format.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? readable.EncodeToPNG()
                : readable.EncodeToJPG(Mathf.Clamp(m_jpegQuality, 1, 100));

            FillHeader(frameIdOverride);
            _message.format = m_format.ToLowerInvariant();
            _message.data = bytes;

            Publish(_message);
        }

        /// <summary>Publish any Texture. Will be copied to a readable Texture2D.</summary>
        public void Publish(Texture tex, string frameIdOverride = null)
        {
            if (tex == null) return;

            var readable = BlitToReadable(tex, ref m_scratchReadable);
            if (m_flipVertical) FlipInPlaceVertical(readable);

            var bytes = m_format.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? readable.EncodeToPNG()
                : readable.EncodeToJPG(Mathf.Clamp(m_jpegQuality, 1, 100));

            FillHeader(frameIdOverride);
            _message.format = m_format.ToLowerInvariant();
            _message.data = bytes;

            Publish(_message);
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

            if (m_flipVertical) FlipInPlaceVertical(m_scratchReadable);

            var bytes = m_format.Equals("png", StringComparison.OrdinalIgnoreCase)
                ? m_scratchReadable.EncodeToPNG()
                : m_scratchReadable.EncodeToJPG(Mathf.Clamp(m_jpegQuality, 1, 100));

            FillHeader(frameIdOverride);
            _message.format = m_format.ToLowerInvariant();
            _message.data = bytes;

            Publish(_message);
        }

        private void FillHeader(string frameIdOverride)
        {
            _message.header.frame_id = string.IsNullOrEmpty(frameIdOverride) ? m_frameID : frameIdOverride;

            var now = DateTime.UtcNow;
            var epochMs = (long)(now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            _message.header.stamp.sec = (int)(epochMs / 1000);
            _message.header.stamp.nanosec = (uint)((epochMs % 1000) * 1_000_000);
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

            // Detect if source is single-channel (like R16 depth RT)
            var isSingleChannel = false;
            if (src is RenderTexture srcRT && srcRT.format == RenderTextureFormat.R16)
                isSingleChannel = true;

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

                // Grayscale fix for single-channel sources
                if (isSingleChannel)
                {
                    var pixels = scratch.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        var c = pixels[i];
                        var v = c.r;
                        pixels[i] = new Color32(v, v, v, 255);
                    }

                    scratch.SetPixels32(pixels);
                    scratch.Apply(false, false);
                }
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
            return scratch;
        }

        /// <summary>Vertical flip of Texture2D pixels (RGBA32 path).</summary>
        private static void FlipInPlaceVertical(Texture2D tex)
        {
            var w = tex.width;
            var h = tex.height;
            var pixels = tex.GetPixels32();

            for (int yTop = 0, yBot = h - 1; yTop < yBot; yTop++, yBot--)
            {
                var rowTop = yTop * w;
                var rowBot = yBot * w;
                for (var x = 0; x < w; x++)
                {
                    (pixels[rowTop + x], pixels[rowBot + x]) = (pixels[rowBot + x], pixels[rowTop + x]);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
        }

        // -------- Optional setters (mark cache dirty where relevant) --------
        public void SetFrameId(string frameId) { m_frameID = frameId; MarkCacheDirty(); }
        public void SetFormat(string fmt) { m_format = string.IsNullOrEmpty(fmt) ? "jpeg" : fmt.ToLowerInvariant(); MarkCacheDirty(); }
        public void SetJpegQuality(int q) { m_jpegQuality = Mathf.Clamp(q, 1, 100); MarkCacheDirty(); }
        public void SetFlipVertical(bool flip) { m_flipVertical = flip; MarkCacheDirty(); }

        // Test controls
        public void SetTestTexture(Texture2D tex) { m_testTexture = tex; MarkCacheDirty(); }
        public void SetPublishTestTexture(bool enable) { m_publishTestTexture = enable; }
        public void StartTestPublishing() { m_publishTestTexture = true; m_nextPublishTime = Time.time; }
        public void StopTestPublishing() { m_publishTestTexture = false; }
        public void SetPublishHz(float hz) { m_publishHz = Mathf.Clamp(hz, 1f, 120f); }
    }
}
