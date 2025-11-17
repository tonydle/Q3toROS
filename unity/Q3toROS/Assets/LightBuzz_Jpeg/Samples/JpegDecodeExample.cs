using LightBuzz.Jpeg;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JpegDecodeExample : MonoBehaviour
{
    [Header("Results")]
    [SerializeField] private string path;
    [SerializeField] private RawImage imageResult;
    [SerializeField] private Text textResult;

    private JpegDecoder jpegDecoder;

    private void Awake()
    {
        jpegDecoder = new JpegDecoder();
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(path))
        {
            path = Path.Combine(Application.dataPath, "LightBuzz_Jpeg", "Resources", "LightBuzz", "Example_Compressed.jpg");
        }
    }

    public void ButtonLightBuzz_Click()
    {
        byte[] jpgBytes = File.ReadAllBytes(path);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        byte[] rawBytes = jpegDecoder.Decode(jpgBytes, PixelFormat.RGBA, Flag.NONE, out int width, out int height);

        sw.Stop();

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(rawBytes);
        texture.Apply();

        imageResult.texture = texture;
        textResult.text = "Result for LightBuzz: <color='green'>" + sw.ElapsedMilliseconds + " milliseconds</color>";
    }

    public void ButtonOpenEncodeExample_Click()
    {
        SceneManager.LoadScene("JpegEncodeExample");
    }
}