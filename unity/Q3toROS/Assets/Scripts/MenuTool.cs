using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
namespace Unity.Robotics
{
    public class MenuTool : MonoBehaviour
    {
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