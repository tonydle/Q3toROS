using UnityEngine;
using System.Collections.Generic;

namespace Unity.Robotics
{
    public class CubeVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform referenceTransform; // Optional reference transform
        [SerializeField] private Material cubeMaterial; // Material to apply to cubes
        [SerializeField] private RosSubscriberVisualizationMarker markerSubscriber;

        // Dictionary to track cubes by their ID
        private Dictionary<int, GameObject> markerCubes = new Dictionary<int, GameObject>();

        void Update()
        {
            if (markerSubscriber.IsAvailable())
            {
                if(markerSubscriber.GetMarkerType() != RosMessageTypes.Visualization.MarkerMsg.CUBE)
                {
                    return;
                }

                // Handle the marker based on its action
                switch (markerSubscriber.GetAction())
                {
                    case 0: // ADD
                    case 1: // MODIFY
                        HandleAddOrModify();
                        break;
                    case 2: // DELETE
                        HandleDelete(markerSubscriber.GetId());
                        break;
                }

                // Mark the message as read
                markerSubscriber.MarkMessageAsRead();
            }
        }

        private void HandleAddOrModify()
        {
            int markerId = markerSubscriber.GetId();

            // Check if the marker with this ID already exists
            if (markerCubes.ContainsKey(markerId))
            {
                // Modify the existing cube
                UpdateCube(markerId);
            }
            else
            {
                // Create a new cube
                CreateCube(markerId);
            }
        }

        private void HandleDelete(int id)
        {
            // Check if the cube exists
            if (markerCubes.ContainsKey(id))
            {
                // Destroy the cube and remove it from the dictionary
                Destroy(markerCubes[id]);
                markerCubes.Remove(id);
            }
        }

        private void CreateCube(int markerId)
        {
            // Create a new GameObject for the cube
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Use reference transform if assigned, otherwise find the frame object
            if (referenceTransform == null)
            {
                var frameId = markerSubscriber.GetFrameId();
                var frameObject = GameObject.Find(frameId);
                if (frameObject == null)
                {
                    Debug.LogWarning($"Frame '{frameId}' not found in the scene.");
                    return;
                }
                referenceTransform = frameObject.transform;
            }

            // Set the cube's position, rotation, and scale
            cube.transform.localScale = markerSubscriber.GetScale();
            cube.transform.SetParent(referenceTransform);
            cube.transform.localPosition = markerSubscriber.GetPose().position;
            cube.transform.localRotation = markerSubscriber.GetPose().rotation;
        
            // Set the cube's color (including alpha)
            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeMaterial != null)
            {
                cubeRenderer.material = new Material(cubeMaterial);
            }
            cubeRenderer.material.color = markerSubscriber.GetColor();

            // Add the cube to the dictionary
            markerCubes[markerId] = cube;
        }

        private void UpdateCube(int markerId)
        {
            // Retrieve the existing cube
            GameObject cube = markerCubes[markerId];

            // Update the cube's position, rotation, and scale
            cube.transform.localPosition = markerSubscriber.GetPose().position;
            cube.transform.localRotation = markerSubscriber.GetPose().rotation;

            // Update the cube's color (including alpha)
            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            cubeRenderer.material.color = markerSubscriber.GetColor();
        }
    }
}