using UnityEngine;
using PassthroughCameraSamples; // only for your Webcam manager type; remove if not needed

namespace Unity.Robotics
{
    /// <summary>
    /// Toggleable passthrough/WebCam image streamer.
    /// Press the chosen button/key to start/stop publishing sensor_msgs/CompressedImage.
    /// </summary>
    public class RosPassthroughStreamer : MonoBehaviour
    {
        [Header("Sources")]
        public WebCamTextureManager WebcamManager;
        public RosPublisherCompressedImage ImagePublisher;

        [Header("ROS Header")]
        public string FrameId = "quest3_passthrough";

        [Header("Streaming")]
        [Tooltip("Target publish rate (Hz).")]
        [Range(1f, 120f)] public float PublishHz = 15f;
        public bool StartOnAwake = false;

        [Header("Controls")]
        [Tooltip("Use OVRInput A button to toggle (if Oculus present).")]
        public bool UseOVRInputA = true;
        [Tooltip("Keyboard fallback to toggle.")]
        public KeyCode ToggleKey = KeyCode.P;

        private bool _isPublishing;
        private float _nextPublishTime;

        private void Awake()
        {
            if (ImagePublisher == null)
                Debug.LogError("[RosPassthroughStreamer] ImagePublisher not set.");
            if (WebcamManager == null)
                Debug.LogError("[RosPassthroughStreamer] WebcamManager not set.");
        }

        private void Start()
        {
            SetPublishing(StartOnAwake);
            _nextPublishTime = Time.time;
        }

        private void Update()
        {
            // --- Toggle controls ---
#if OCULUS_INTEGRATION_PRESENT
            if (UseOVRInputA && OVRInput.GetDown(OVRInput.Button.One))
                TogglePublishing();
#endif
            if (Input.GetKeyDown(ToggleKey))
                TogglePublishing();

            // --- Publish loop ---
            if (!_isPublishing || ImagePublisher == null || WebcamManager == null)
                return;

            var wct = WebcamManager.WebCamTexture;
            if (wct == null || !wct.isPlaying || wct.width <= 16) // not yet ready/started
                return;

            if (Time.time >= _nextPublishTime)
            {
                _nextPublishTime += 1f / Mathf.Max(1f, PublishHz);
                // Use the publisher's fast WebCamTexture path
                ImagePublisher.Publish(wct, FrameId);
            }
        }

        public void TogglePublishing() => SetPublishing(!_isPublishing);

        public void SetPublishing(bool enable)
        {
            _isPublishing = enable;
            if (_isPublishing)
            {
                _nextPublishTime = Time.time;
                Debug.Log("[RosPassthroughStreamer] Publishing: ON");
            }
            else
            {
                Debug.Log("[RosPassthroughStreamer] Publishing: OFF");
            }
        }

        // Optional: external controls (e.g., UI buttons)
        public void StartPublishing() => SetPublishing(true);
        public void StopPublishing()  => SetPublishing(false);
    }
}
