using UnityEngine;
using PassthroughCameraSamples;

public class PassthroughCameraDisplay : MonoBehaviour
{
    public WebCamTextureManager WebcamManager;
    public Renderer QuadRenderer;
    public float QuadDistance = 0.5f;

    private Texture2D m_snap;

    // Start is called before the first frame update
    private void Start()
    {
        QuadRenderer.gameObject.SetActive(false);

    }

    // Update is called once per frame
    private void Update()
    {
        if (WebcamManager.WebCamTexture != null)
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                TakePicture();
                PlaceQuad();
            }
        }
    }

    public void TakePicture()
    {
        QuadRenderer.gameObject.SetActive(true);

        var width = WebcamManager.WebCamTexture.width;
        var height = WebcamManager.WebCamTexture.height;

        if (m_snap == null)
        {
            m_snap = new Texture2D(width, height);
        }

        var pix = WebcamManager.WebCamTexture.GetPixels32();
        m_snap.SetPixels32(pix);
        m_snap.Apply();

        QuadRenderer.material.SetTexture("_MainTex", m_snap);
    }

    public void PlaceQuad()
    {
        var quadTransform = QuadRenderer.transform;

        var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);

        var resolution = PassthroughCameraUtils.GetCameraIntrinsics(PassthroughCameraEye.Left).Resolution;

        quadTransform.position = cameraPose.position + cameraPose.forward * QuadDistance;
        quadTransform.rotation = cameraPose.rotation;

        var leftSideRay = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, new Vector2Int(0, resolution.y / 2));
        var rightSideRay = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, new Vector2Int(resolution.x, resolution.y / 2));

        var horizontalFOV = Vector3.Angle(leftSideRay.direction, rightSideRay.direction);

        var quadScale = 2.0f * Mathf.Tan(Mathf.Deg2Rad * horizontalFOV / 2.0f) * QuadDistance;
        var aspectRatio = (float)m_snap.width / m_snap.height;

        quadTransform.localScale = new Vector3(quadScale, quadScale / aspectRatio, 1.0f);
    }
}
