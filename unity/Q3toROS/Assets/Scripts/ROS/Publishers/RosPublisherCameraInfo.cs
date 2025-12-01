using System;
using UnityEngine;
using RosMsgCameraInfo = RosMessageTypes.Sensor.CameraInfoMsg;
using RosMsgRegionOfInterest = RosMessageTypes.Sensor.RegionOfInterestMsg;

namespace Unity.Robotics
{
    /// <summary>
    /// Generic publisher for sensor_msgs/CameraInfo.
    /// This class is reusable for any camera source (passthrough, RGB, simulated, etc.).
    /// </summary>
    public class RosPublisherCameraInfo : RosPublisher<RosMsgCameraInfo>
    {
        [Header("Defaults")]
        [SerializeField] private string mFrameID = "camera";
        [SerializeField] private string mDistortionModel = "plumb_bob";

        private RosMsgCameraInfo m_message;

        protected override void Start()
        {
            base.Start();
            m_message = new RosMsgCameraInfo();
        }

        /// <summary>
        /// Publish CameraInfo given basic pinhole parameters.
        /// All extra fields (R, P, binning, ROI) are filled with reasonable defaults.
        /// </summary>
        public void Publish(
            int width,
            int height,
            double fx,
            double fy,
            double cx,
            double cy,
            string frameIdOverride = null,
            double[] distortion = null
        )
        {
            if (width <= 0 || height <= 0)
                return;

            FillHeader(frameIdOverride);

            m_message.width  = (uint)width;
            m_message.height = (uint)height;

            m_message.distortion_model = mDistortionModel;

            // Distortion: if none provided, publish zeros for 5 coeffs (common convention).
            m_message.D = distortion ?? new double[] { 0, 0, 0, 0, 0 };

            // Camera matrix K (row-major):
            // [ fx  0  cx ]
            // [ 0  fy  cy ]
            // [ 0   0   1 ]
            m_message.K = new double[9];
            m_message.K[0] = fx;  m_message.K[1] = 0.0; m_message.K[2] = cx;
            m_message.K[3] = 0.0; m_message.K[4] = fy;  m_message.K[5] = cy;
            m_message.K[6] = 0.0; m_message.K[7] = 0.0; m_message.K[8] = 1.0;

            // Rectification matrix R (identity)
            m_message.R = new double[]
            {
                1,0,0,
                0,1,0,
                0,0,1
            };

            // Projection matrix P:
            // [ fx  0  cx  0 ]
            // [ 0  fy  cy  0 ]
            // [ 0   0   1  0 ]
            m_message.P = new double[12];
            m_message.P[0]  = fx;  m_message.P[1]  = 0.0; m_message.P[2]  = cx; m_message.P[3]  = 0.0;
            m_message.P[4]  = 0.0; m_message.P[5]  = fy;  m_message.P[6]  = cy; m_message.P[7]  = 0.0;
            m_message.P[8]  = 0.0; m_message.P[9]  = 0.0; m_message.P[10] = 1.0; m_message.P[11] = 0.0;

            // No binning
            m_message.binning_x = 1;
            m_message.binning_y = 1;

            // ROI unset / full frame
            m_message.roi = new RosMsgRegionOfInterest
            {
                x_offset  = 0,
                y_offset  = 0,
                height    = 0,
                width     = 0,
                do_rectify = false
            };

            Publish(m_message);
        }

        private void FillHeader(string frameIdOverride)
        {
            m_message.header.frame_id =
                string.IsNullOrEmpty(frameIdOverride) ? mFrameID : frameIdOverride;

            var now = DateTime.UtcNow;
            var epochMs = (long)(now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            m_message.header.stamp.sec     = (int)(epochMs / 1000);
            m_message.header.stamp.nanosec = (uint)((epochMs % 1000) * 1_000_000);
        }

        // Optional helpers
        public void SetframeId(string frameId) => mFrameID = frameId;
        public void SetDistortionModel(string model) => mDistortionModel = model;
    }
}
