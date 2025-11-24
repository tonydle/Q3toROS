using UnityEngine;
using Meta.XR;

public class PassthroughCameraDisplay : MonoBehaviour
{
    [Header("Sources")]
    public PassthroughCameraAccess CameraAccess;

    [Header("Display")]
    public Renderer QuadRenderer;
    public float QuadDistance = 0.5f;

    private Texture2D m_snap;

    private void Awake()
    {
        if (CameraAccess == null)
            Debug.LogError("[PassthroughCameraDisplay] CameraAccess not set.");
        if (QuadRenderer == null)
            Debug.LogError("[PassthroughCameraDisplay] QuadRenderer not set.");

        if (!PassthroughCameraAccess.IsSupported)
        {
            Debug.LogWarning(
                "[PassthroughCameraDisplay] PassthroughCameraAccess not supported " +
                "on this device / OS version. Display will not work."
            );
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (QuadRenderer != null)
            QuadRenderer.gameObject.SetActive(false);

        // Make sure camera component is enabled so it can request permission / start
        if (CameraAccess != null && !CameraAccess.enabled)
            CameraAccess.enabled = true;
    }

    // Update is called once per frame
    private void Update()
    {
        if (CameraAccess == null || QuadRenderer == null)
            return;

        // Wait until camera is actually playing (permission granted + camera started)
        if (!CameraAccess.IsPlaying)
            return;

        // Same control: OVRInput A button
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            TakePicture();
            PlaceQuad();
        }
    }

    public void TakePicture()
    {
        QuadRenderer.gameObject.SetActive(true);

        // Use current resolution from passthrough camera
        Vector2Int res = CameraAccess.CurrentResolution;
        int width = res.x;
        int height = res.y;

        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning("[PassthroughCameraDisplay] Invalid camera resolution.");
            return;
        }

        // Grab GPU texture from passthrough camera
        Texture srcTex = CameraAccess.GetTexture();
        if (srcTex == null)
        {
            Debug.LogWarning("[PassthroughCameraDisplay] Camera texture is null.");
            return;
        }

        // Allocate / reallocate snapshot texture as needed
        if (m_snap == null || m_snap.width != width || m_snap.height != height)
        {
            m_snap = new Texture2D(width, height, TextureFormat.RGBA32, false);
        }

        // Copy GPU texture -> Texture2D using a temporary RenderTexture
        RenderTexture tempRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

        Graphics.Blit(srcTex, tempRT);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = tempRT;

        m_snap.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        m_snap.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(tempRT);

        // Assign snapshot to quad material
        QuadRenderer.material.SetTexture("_MainTex", m_snap);
    }

    public void PlaceQuad()
    {
        if (CameraAccess == null || m_snap == null)
            return;

        Transform quadTransform = QuadRenderer.transform;

        // New API: world-space camera pose
        Pose cameraPose = CameraAccess.GetCameraPose();

        // You can use either Intrinsics.Resolution or CurrentResolution
        Vector2Int resolution = CameraAccess.CurrentResolution;
        // If CurrentResolution isn't valid yet, use RequestedResolution
        if (resolution.x <= 0 || resolution.y <= 0)
            resolution = CameraAccess.RequestedResolution;


        // Position and orient quad in front of camera
        quadTransform.position = cameraPose.position + cameraPose.forward * QuadDistance;
        quadTransform.rotation = cameraPose.rotation;

        // Old code used ScreenPointToRayInCamera with pixel coords.
        // New API uses normalized viewport coords in [0,1].
        //
        // Left-middle:  (0, 0.5)
        // Right-middle: (1, 0.5)
        Vector2 viewportLeft = new Vector2(0f, 0.5f);
        Vector2 viewportRight = new Vector2(1f, 0.5f);

        Ray leftSideRayWorld = CameraAccess.ViewportPointToRay(viewportLeft);
        Ray rightSideRayWorld = CameraAccess.ViewportPointToRay(viewportRight);

        // Angle between ray directions gives horizontal FOV (space doesn't matter)
        float horizontalFOV = Vector3.Angle(leftSideRayWorld.direction, rightSideRayWorld.direction);

        // Same FOV-based scale as before
        float quadScale = 2.0f * Mathf.Tan(Mathf.Deg2Rad * horizontalFOV * 0.5f) * QuadDistance;
        float aspectRatio = (float)m_snap.width / m_snap.height;

        quadTransform.localScale = new Vector3(quadScale, quadScale / aspectRatio, 1.0f);
    }
}
