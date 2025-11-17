using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosPoseStamped = RosMessageTypes.Geometry.PoseStampedMsg;

namespace Unity.Robotics
{
    public class RosSubscriberPoseStamped : RosSubscriber<RosPoseStamped>
    {
        private Pose _latestPose = new Pose();
        private string _frame_id = "";
        private bool _messageAvailable = false;
        private RosPoseStamped _poseStamped;

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
            if (NewMessageAvailable())
            {
                _poseStamped = GetLatestMessage();
                _latestPose.position = _poseStamped.pose.position.From<FLU>();
                _latestPose.rotation = _poseStamped.pose.orientation.From<FLU>();
                _frame_id = _poseStamped.header.frame_id;
                _messageAvailable = true;
            }
        }

        public bool IsAvailable()
        {
            return _messageAvailable;
        }

        public string GetFrameId()
        {
            return _frame_id;
        }

        public Pose GetLatestPose()
        {
            if(_messageAvailable) _messageAvailable = false;
            return _latestPose;
        }

        public Vector3 GetLatestPosition()
        {
            if(_messageAvailable) _messageAvailable = false;
            return _latestPose.position;
        }

        public Quaternion GetLatestRotation()
        {
            if(_messageAvailable) _messageAvailable = false;
            return _latestPose.rotation;
        }
    }
}
