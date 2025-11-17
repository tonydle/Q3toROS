using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMarkerMsg = RosMessageTypes.Visualization.MarkerMsg;
using RosHeader = RosMessageTypes.Std.HeaderMsg;
using RosColorRGBA = RosMessageTypes.Std.ColorRGBAMsg;
using RosPoint = RosMessageTypes.Geometry.PointMsg;

namespace Unity.Robotics
{
    public class RosSubscriberVisualizationMarker : RosSubscriber<RosMarkerMsg>
    {
        private RosMarkerMsg _latestMarker;
        private bool _messageAvailable = false;

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
            if (NewMessageAvailable())
            {
                _latestMarker = GetLatestMessage();
                _messageAvailable = true;
            }
        }

        public bool IsAvailable()
        {
            return _messageAvailable;
        }

        public void MarkMessageAsRead()
        {
            _messageAvailable = false;
        }

        public RosHeader GetHeader()
        {
            return _latestMarker.header;
        }

        public string GetFrameId()
        {
            return _latestMarker.header.frame_id;
        }

        public string GetNamespace()
        {
            return _latestMarker.ns;
        }

        public int GetId()
        {
            return _latestMarker.id;
        }

        public int GetMarkerType()
        {
            return _latestMarker.type;
        }

        public int GetAction()
        {
            return _latestMarker.action;
        }

        public Pose GetPose()
        {
            return new Pose(
                _latestMarker.pose.position.From<FLU>(),
                _latestMarker.pose.orientation.From<FLU>()
            );
        }

        public Vector3 GetScale()
        {
            Vector3 scale = _latestMarker.scale.From<FLU>();
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return scale;
        }

        public Color GetColor()
        {
            var color = _latestMarker.color;
            return new Color(color.r, color.g, color.b, color.a);
        }

        public float GetLifetime()
        {
            return (float)_latestMarker.lifetime.sec + _latestMarker.lifetime.nanosec * 1e-9f;
        }

        public bool IsFrameLocked()
        {
            return _latestMarker.frame_locked;
        }

        public Vector3[] GetPoints()
        {
            var points = _latestMarker.points;
            Vector3[] unityPoints = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                unityPoints[i] = points[i].From<FLU>();
            }
            return unityPoints;
        }

        public Color[] GetColors()
        {
            var colors = _latestMarker.colors;
            Color[] unityColors = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                unityColors[i] = new Color(color.r, color.g, color.b, color.a);
            }
            return unityColors;
        }

        public string GetText()
        {
            return _latestMarker.text;
        }

        public string GetMeshResource()
        {
            return _latestMarker.mesh_resource;
        }

        public bool UseEmbeddedMaterials()
        {
            return _latestMarker.mesh_use_embedded_materials;
        }
    }
}
