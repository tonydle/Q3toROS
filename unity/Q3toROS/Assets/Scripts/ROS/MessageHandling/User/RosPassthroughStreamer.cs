using UnityEngine;
using Meta.XR; 

namespace Unity.Robotics
{
    /// <summary>
    /// Toggleable passthrough image streamer using PassthroughCameraAccess.
    /// Press the chosen button/key to start/stop publishing sensor_msgs/CompressedImage.
    /// </summary>
    public class RosPassthroughStreamer : MonoBehaviour
    {
        [Header("Sources")]
        public PassthroughCameraAccess CameraAccess;
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
            if (CameraAccess == null)
                Debug.LogError("[RosPassthroughStreamer] CameraAccess not set.");

            if (!PassthroughCameraAccess.IsSupported)
            {
                Debug.LogWarning(
                    "[RosPassthroughStreamer] PassthroughCameraAccess is not supported " +
                    "on this device / OS version. Streaming will be disabled."
                );
            }
        }

        private void OnEnable()
        {
            // Make sure the camera component is active so it can request permission
            if (CameraAccess != null && !CameraAccess.enabled)
                CameraAccess.enabled = true;
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
            if (!_isPublishing || ImagePublisher == null || CameraAccess == null)
                return;

            // Wait until camera is actually playing
            // (permission granted + camera started)
            if (!CameraAccess.IsPlaying)
                return;

            // Rate limiting
            if (Time.time < _nextPublishTime)
                return;

            _nextPublishTime += 1f / Mathf.Max(1f, PublishHz);

            // Get GPU texture from passthrough camera
            Texture tex = CameraAccess.GetTexture();
            if (tex == null || CameraAccess.CurrentResolution.x <= 16)
                return; // still not ready / invalid

            // If RosPublisherCompressedImage.Publish takes Texture, this compiles as-is.
            // If it currently takes WebCamTexture, change its signature to Texture.
            ImagePublisher.Publish(tex, FrameId);

            // If you extend your publisher to take timestamps later, you could do:
            // ImagePublisher.Publish(tex, FrameId, CameraAccess.Timestamp);
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
