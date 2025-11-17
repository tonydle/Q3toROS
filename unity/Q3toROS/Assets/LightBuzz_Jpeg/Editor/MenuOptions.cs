using UnityEditor;
using UnityEngine;

public class MenuOptions : MonoBehaviour
{
    [MenuItem("LightBuzz/Super-Fast JPEG/Support")]
    static void SupportContact()
    {
        Application.OpenURL(EditorConstants.SUPPORT_URL);
    }

    [MenuItem("LightBuzz/Super-Fast JPEG/Rate")]
    static void RateAsset()
    {
        Application.OpenURL(EditorConstants.ASSET_STORE_URL);
    }

    [MenuItem("LightBuzz/Super-Fast JPEG/Documentation")]
    static void ReadDocumentation()
    {
        Application.OpenURL(EditorConstants.ONLINE_DOCUMENTATION_URL);
    }
}
