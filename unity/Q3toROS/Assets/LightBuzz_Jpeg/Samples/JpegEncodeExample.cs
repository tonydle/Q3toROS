using LightBuzz.Jpeg;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JpegEncodeExample : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private RawImage imageSource;

    [Header("JPEG Quality")]
    [SerializeField] private Text textQuality;
    [SerializeField] private Slider sliderQuality;

    [Header("Results")]
    [SerializeField] private RawImage imageResult;
    [SerializeField] private Text textResult;

    private Texture2D texture;
    private int quality;

    private void Start()
    {
        texture = imageSource.texture as Texture2D;
        quality = (int)sliderQuality.value;

        textQuality.text = "Quality: " + quality + "%";
        imageResult.texture = new Texture2D(1, 1);
    }

    public void SliderQuality_ValueChanged(float arg0)
    {
        quality = (int)arg0;

        textQuality.text = "Quality: " + quality + "%";
    }

    public void ButtonUnity_Click()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        byte[] data = texture.EncodeToJPG(quality);

        sw.Stop();
        
        (imageResult.texture as Texture2D).LoadImage(data);
        textResult.text = "Result for Unity3D: <color='red'>" + sw.ElapsedMilliseconds + " milliseconds</color>";
    }

    public void ButtonLightBuzz_Click()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        byte[] data = texture.EncodeToJPGFast(quality);

        sw.Stop();

        (imageResult.texture as Texture2D).LoadImage(data);
        textResult.text = "Result for LightBuzz: <color='green'>" + sw.ElapsedMilliseconds + " milliseconds</color>";
    }

    public void ButtonOpenDecodeExample_Click()
    {
        SceneManager.LoadScene("JpegDecodeExample");
    }
}
