using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RemindRate
{
    private const string RemindDatePref = EditorConstants.ASSET_PREFIX + "remind_date";
    private const long Duration = TimeSpan.TicksPerDay * 7L;

    static RemindRate()
    {
        ShowReminder();
    }

    private static void ShowReminder()
    {
        string setting = EditorPrefs.GetString(RemindDatePref);
        long ticks;
        long.TryParse(setting, out ticks);

        DateTime dateReminded = DateTime.FromBinary(ticks);

        if ((DateTime.Now - dateReminded).Ticks > Duration)
        {
            int option = EditorUtility.DisplayDialogComplex
            (
                EditorConstants.ASSET_NAME + " - Rate Asset",
                "If you enjoy using " + EditorConstants.ASSET_NAME + ", please take some time to leave us a review. Giving us 5 stars will allow our team to keep maintining the plugin and adding more cool features!",
                "❤ LOVE this asset ❤",
                "Need help!",
                "Don't ask again"
            );

            switch (option)
            {
                case 0: // Leave review
                    Application.OpenURL(EditorConstants.ASSET_STORE_URL);
                    EditorPrefs.SetString(RemindDatePref, DateTime.MaxValue.Ticks.ToString());
                    break;
                case 1: // Contact support
                    Application.OpenURL(EditorConstants.SUPPORT_URL);
                    EditorPrefs.SetString(RemindDatePref, DateTime.Now.Ticks.ToString());
                    break;
                case 2: // Don't ask again
                    EditorPrefs.SetString(RemindDatePref, DateTime.MaxValue.Ticks.ToString());
                    break;
                default:
                    break;
            }
        }
    }
}
