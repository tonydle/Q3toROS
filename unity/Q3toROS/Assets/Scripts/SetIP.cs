using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Robotics
{
    public class SetIP : MonoBehaviour
    {
        [SerializeReference] private ROSTCPConnector.ROSConnection _rosConnection;
        public string _defaultRosIP = "192.168.0.10";

        private void Start()
        {
            string ip = PlayerPrefs.GetString("ROSIP", _defaultRosIP);
            _rosConnection.RosIPAddress = ip;
            _rosConnection.Connect();
        }
    }
}