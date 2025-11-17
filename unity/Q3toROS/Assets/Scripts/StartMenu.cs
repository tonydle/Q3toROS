using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
namespace Unity.Robotics
{
    public class StartMenu : MonoBehaviour
    {
        [SerializeField] private TMP_InputField rosIPInputField;
        [SerializeField] private ROSTCPConnector.ROSConnection rosConnection;

        private const string RosIPPrefKey = "StartMenu_ROS_IP";
        private const string DefaultRosIP = "192.168.2.150";

        private void Awake()
        {
            string lastIP = PlayerPrefs.GetString(RosIPPrefKey, DefaultRosIP);
            rosIPInputField.text = lastIP;
            rosConnection.RosIPAddress = lastIP;
            DontDestroyOnLoad(rosConnection.gameObject);
        }

        public void ConnectToROS()
        {
            string ip = rosIPInputField.text;
            PlayerPrefs.SetString(RosIPPrefKey, ip);
            rosConnection.RosIPAddress = ip;
            rosConnection.Connect();
            Debug.Log($"Connecting to ROS at {ip}, if this fails, check the ROS IP address and make sure ros_tcp_endpoint is running.");
        }

        public void LoadScene(string sceneName)
        {
            Debug.Log($"Loading {sceneName} Scene");
            SceneManager.LoadScene(sceneName);
        }

        public void QuitProgram()
        {
            Debug.Log("Application is quitting ...");
            Application.Quit();
        }
    }
}