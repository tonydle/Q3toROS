using UnityEngine;
using System.Collections.Generic;

namespace Unity.Robotics
{
    public class CubeListVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform referenceTransform; // Optional reference transform
        [SerializeField] private Material cubeMaterial; // Material to apply to cubes
        [SerializeField] private RosSubscriberVisualizationMarker markerSubscriber;

        // List to track the current cubes
        private List<GameObject> markerCubes = new List<GameObject>();

        void Update()
        {
            if (markerSubscriber.IsAvailable())
            {
                if(markerSubscriber.GetMarkerType() != RosMessageTypes.Visualization.MarkerMsg.CUBE_LIST)
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
                        HandleDeleteAll();
                        break;
                }

                // Mark the message as read
                markerSubscriber.MarkMessageAsRead();
            }
        }

        private void HandleAddOrModify()
        {
            // Delete all existing cubes
            HandleDeleteAll();

            // Get the points and colors from the CUBE_LIST marker
            Vector3[] points = markerSubscriber.GetPoints();
            Color[] colors = markerSubscriber.GetColors();

            for (int i = 0; i < points.Length; i++)
            {
                // Create a new cube for each point
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
                cube.transform.position = referenceTransform.TransformPoint(points[i]);
                cube.transform.rotation = referenceTransform.rotation;
                cube.transform.localScale = markerSubscriber.GetScale();

                // Set the cube's color (including alpha)
                Renderer cubeRenderer = cube.GetComponent<Renderer>();
                if (cubeMaterial != null)
                {
                    cubeRenderer.material = new Material(cubeMaterial);
                }
                cubeRenderer.material.color = colors[i];

                // Add the cube to the list
                markerCubes.Add(cube);
            }
        }

        private void HandleDeleteAll()
        {
            // Destroy all existing cubes and clear the list
            foreach (var cube in markerCubes)
            {
                Destroy(cube);
            }
            markerCubes.Clear();
        }
    }
}
