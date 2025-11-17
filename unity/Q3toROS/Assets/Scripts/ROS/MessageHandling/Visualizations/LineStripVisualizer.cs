using UnityEngine;
using System.Collections.Generic;

namespace Unity.Robotics
{
    public class LineStripVisualizer : MonoBehaviour
    {
        [SerializeField] private Transform referenceTransform; // Optional reference transform
        [SerializeField] private Material lineMaterial; // Material to apply to the line
        [SerializeField] private RosSubscriberVisualizationMarker markerSubscriber;

        private LineRenderer lineRenderer;

        void Start()
        {
            // Set up the LineRenderer component
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            if (lineMaterial != null)
            {
                lineRenderer.material = new Material(lineMaterial);
            }
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
            lineRenderer.positionCount = 0;  // Initially no points in the line
            lineRenderer.useWorldSpace = false;  // This allows for local transformations relative to a reference frame
        }

        void Update()
        {
            if (markerSubscriber.IsAvailable())
            {
                if (markerSubscriber.GetMarkerType() != RosMessageTypes.Visualization.MarkerMsg.LINE_STRIP)
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
                        HandleDelete();
                        break;
                }

                // Mark the message as read
                markerSubscriber.MarkMessageAsRead();
            }
        }

        private void HandleAddOrModify()
        {
            // Get the points from the LINE_STRIP marker
            Vector3[] points = markerSubscriber.GetPoints();
            Color color = markerSubscriber.GetColor();

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

            // Set the line renderer's color
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.material.color = color;

            // Set the line's points
            lineRenderer.positionCount = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                // Set each point relative to the reference transform
                lineRenderer.SetPosition(i, referenceTransform.TransformPoint(points[i]));
            }
        }

        private void HandleDelete()
        {
            // Clear the line strip by setting the position count to zero
            lineRenderer.positionCount = 0;
        }
    }
}
