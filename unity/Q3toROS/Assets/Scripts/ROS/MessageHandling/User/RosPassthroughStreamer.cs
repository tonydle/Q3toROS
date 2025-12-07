using System.Globalization;
using UnityEngine;
using Meta.XR;

namespace Unity.Robotics
{
    /// <summary>
    /// Passthrough image streamer using PassthroughCameraAccess.
    /// Publishes sensor_msgs/CompressedImage and optionally sensor_msgs/CameraInfo.
    /// </summary>
    public class RosPassthroughStreamer : MonoBehaviour
    {
        [Header("Sources")]
        public PassthroughCameraAccess cameraAccess;
        public RosPublisherCompressedImage imagePublisher;
        public RosPublisherCameraInfo cameraInfoPublisher; 

        [Header("ROS Header")]
        public string frameId = "quest3_passthrough";

        [Header("Streaming")]
        [Tooltip("Target publish rate (Hz).")]
        [Range(1f, 120f)] public float publishHz = 15f;

        public string PublishHzString
        {
            get => publishHz.ToString();
            set => publishHz = float.Parse(value);
        }

        private int m_resolutionOption;
        public int ResolutionOption
        {
            get => m_resolutionOption;
            set
            {
                cameraAccess.enabled = false;
                switch (value)
                {
                    case 0:
                        cameraAccess.RequestedResolution = new Vector2Int(320, 240);
                        break;
                    case 1:
                        cameraAccess.RequestedResolution = new Vector2Int(640, 480);
                        break;
                    case 2:
                        cameraAccess.RequestedResolution = new Vector2Int(800, 600);
                        break;
                    case 3:
                        cameraAccess.RequestedResolution = new Vector2Int(1280, 960);
                        break;
                    default:
                        Debug.LogError($"Invalid resolution option {value}");
                        break;
                    
                }
                m_resolutionOption = value;
                cameraAccess.enabled = true;
            }
        }

        [Tooltip("If true, publish CameraInfo together with CompressedImage.")]
        public bool publishCameraInfo = true;

        [Tooltip("If true, send CameraInfo only once after camera starts.")]
        public bool cameraInfoOnce;

        private float m_nextPublishTime;
        private bool m_cameraInfoSent;
        

        private void Awake()
        {
            if (!imagePublisher)
                Debug.LogError("[RosPassthroughStreamer] ImagePublisher not set.");
            if (!cameraAccess)
                Debug.LogError("[RosPassthroughStreamer] CameraAccess not set.");
            if (publishCameraInfo && !cameraInfoPublisher)
                Debug.LogWarning("[RosPassthroughStreamer] CameraInfoPublisher not set.");

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
            if (cameraAccess && !cameraAccess.enabled)
                cameraAccess.enabled = true;
        }

        private void Start()
        {
            m_nextPublishTime = Time.time;
            m_cameraInfoSent = false;
        }

        private void Update()
        {
            if (!imagePublisher || !cameraAccess)
                return;

            // Wait until the camera is actually playing
            if (!cameraAccess.IsPlaying)
                return;

            // Rate limiting
            if (Time.time < m_nextPublishTime)
                return;

            m_nextPublishTime += 1f / Mathf.Max(1f, publishHz);

            // Get GPU texture from the passthrough camera
            var tex = cameraAccess.GetTexture();
            if (!tex || cameraAccess.CurrentResolution.x <= 16)
                return; // still not ready / invalid

            // Publish image
            imagePublisher.Publish(tex, frameId);

            // Publish CameraInfo (derived from PassthroughCameraAccess intrinsics)
            if (publishCameraInfo && cameraInfoPublisher)
            {
                if (!cameraInfoOnce || !m_cameraInfoSent)
                {
                    PublishCameraInfoFromIntrinsics();
                    m_cameraInfoSent = true;
                }
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void PublishCameraInfoFromIntrinsics()
        {
            // Resolution: prefer CurrentResolution; fallback to intrinsics sensor resolution
            var res = cameraAccess.CurrentResolution;
            var intr = cameraAccess.Intrinsics;

            if (res.x <= 0 || res.y <= 0)
                res = intr.SensorResolution;

            if (res.x <= 0 || res.y <= 0)
            {
                Debug.LogWarning("[RosPassthroughStreamer] Invalid resolution for CameraInfo.");
                return;
            }

            // Intrinsics: fx/fy, cx/cy from Meta's intrinsics
            var focal = intr.FocalLength;      // Vector2 (fx, fy)
            var pp    = intr.PrincipalPoint;   // Vector2 (cx, cy)

            double fx = focal.x;
            double fy = focal.y;
            double cx = pp.x;
            double cy = pp.y;

            // Distortion unknown
            cameraInfoPublisher.Publish(
                res.x,
                res.y,
                fx,
                fy,
                cx,
                cy,
                frameId,
                null  // or new double[5]{k1,k2,t1,t2,k3} if we have real values
            );
        }
    }
}
