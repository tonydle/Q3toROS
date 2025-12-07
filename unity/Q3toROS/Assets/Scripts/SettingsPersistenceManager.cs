using System;
using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persist simple UI settings (PlayerPrefs) so they survive app restarts on Quest.
///
/// Supported:
/// - One ROS IP address (TMP_InputField)
/// - A list of toggles (UnityEngine.UI.Toggle)
/// - A list of text values (TMP_InputField)
/// - A list of dropdown menus (Oculus.Interaction.Samples.DropDownGroup)
///
/// Usage:
/// - Attach to a GameObject in your scene.
/// - Assign fields/lists via Inspector.
/// - Optionally change the keyPrefix to maintain separate profiles.
///
/// Notes:
/// - Uses PlayerPrefs under the hood (works on Android/Quest).
/// - Saves on change; loads on Awake.
/// </summary>
public class SettingsPersistenceManager : MonoBehaviour
{
    [Header("Keys & Profile")]
    [SerializeField]
    [Tooltip("Prefix added to all PlayerPrefs keys so multiple profiles/projects don't collide.")]
    private string keyPrefix = "Q3toROS.";

    [Header("ROS")]
    [SerializeField]
    [Tooltip("TMP InputField holding the ROS IP address.")]
    private TMP_InputField rosIpInput;

    [Header("Toggles to persist")] 
    [SerializeField]
    private List<Toggle> toggles = new List<Toggle>();

    [Header("Text inputs to persist")] 
    [SerializeField]
    private List<TMP_InputField> textInputs = new List<TMP_InputField>();

    [Header("Dropdowns to persist (DropDownGroup)")] 
    [SerializeField]
    private List<DropDownGroup> dropdowns = new List<DropDownGroup>();

    // Flag to avoid saving while we apply loaded values
    private bool _isLoading;

    private void Awake()
    {
        LoadAll();
        Subscribe();
        RaiseInitialEvents();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (rosIpInput != null)
        {
            rosIpInput.onEndEdit.AddListener(OnRosIpChanged);
        }

        for (int i = 0; i < toggles.Count; i++)
        {
            int idx = i;
            if (toggles[idx] != null)
            {
                toggles[idx].onValueChanged.AddListener(v => OnToggleChanged(idx, v));
            }
        }

        for (int i = 0; i < textInputs.Count; i++)
        {
            int idx = i;
            if (textInputs[idx] != null)
            {
                textInputs[idx].onEndEdit.AddListener(v => OnTextInputChanged(idx, v));
            }
        }

        for (int i = 0; i < dropdowns.Count; i++)
        {
            int idx = i;
            var dd = dropdowns[idx];
            if (dd != null)
            {
                dd.WhenSelectionChanged ??= new UnityEngine.Events.UnityEvent<int>();
                dd.WhenSelectionChanged.AddListener(selected => OnDropdownChanged(idx, selected));
            }
        }
    }

    private void Unsubscribe()
    {
        if (rosIpInput != null)
        {
            rosIpInput.onEndEdit.RemoveListener(OnRosIpChanged);
        }

        for (int i = 0; i < toggles.Count; i++)
        {
            int idx = i;
            if (toggles[idx] != null)
            {
                toggles[idx].onValueChanged.RemoveAllListeners();
            }
        }

        for (int i = 0; i < textInputs.Count; i++)
        {
            int idx = i;
            if (textInputs[idx] != null)
            {
                textInputs[idx].onEndEdit.RemoveAllListeners();
            }
        }

        for (int i = 0; i < dropdowns.Count; i++)
        {
            int idx = i;
            var dd = dropdowns[idx];
            if (dd != null && dd.WhenSelectionChanged != null)
            {
                dd.WhenSelectionChanged.RemoveAllListeners();
            }
        }
    }

    // Public helpers you can bind to buttons if desired
    public void SaveAll()
    {
        if (rosIpInput != null)
        {
            PlayerPrefs.SetString(Key("ROSIP"), rosIpInput.text);
        }

        for (int i = 0; i < toggles.Count; i++)
        {
            var t = toggles[i];
            if (t != null)
            {
                PlayerPrefs.SetInt(Key($"Toggle.{i}"), t.isOn ? 1 : 0);
            }
        }

        for (int i = 0; i < textInputs.Count; i++)
        {
            var ti = textInputs[i];
            if (ti != null)
            {
                PlayerPrefs.SetString(Key($"Text.{i}"), ti.text);
            }
        }

        for (int i = 0; i < dropdowns.Count; i++)
        {
            var dd = dropdowns[i];
            if (dd != null)
            {
                int sel = SafeGetDropdownSelectedIndex(dd);
                PlayerPrefs.SetInt(Key($"Dropdown.{i}"), sel);
            }
        }

        PlayerPrefs.Save();
    }

    public void LoadAll()
    {
        _isLoading = true;

        if (rosIpInput != null)
        {
            var v = PlayerPrefs.GetString(Key("ROSIP"), rosIpInput.text);
            rosIpInput.SetTextWithoutNotify(v);
        }

        for (int i = 0; i < toggles.Count; i++)
        {
            var t = toggles[i];
            if (t == null) continue;
            int def = t.isOn ? 1 : 0;
            int saved = PlayerPrefs.GetInt(Key($"Toggle.{i}"), def);
            t.SetIsOnWithoutNotify(saved != 0);
        }

        for (int i = 0; i < textInputs.Count; i++)
        {
            var ti = textInputs[i];
            if (ti == null) continue;
            var v = PlayerPrefs.GetString(Key($"Text.{i}"), ti.text);
            ti.SetTextWithoutNotify(v);
        }

        for (int i = 0; i < dropdowns.Count; i++)
        {
            var dd = dropdowns[i];
            if (dd == null) continue;
            int current = SafeGetDropdownSelectedIndex(dd);
            int saved = PlayerPrefs.GetInt(Key($"Dropdown.{i}"), current);
            SafeSetDropdownSelectedIndex(dd, saved);
        }

        _isLoading = false;
    }

    public void ClearAll()
    {
        // Remove only our keys
        if (rosIpInput != null) PlayerPrefs.DeleteKey(Key("ROSIP"));
        for (int i = 0; i < toggles.Count; i++) PlayerPrefs.DeleteKey(Key($"Toggle.{i}"));
        for (int i = 0; i < textInputs.Count; i++) PlayerPrefs.DeleteKey(Key($"Text.{i}"));
        for (int i = 0; i < dropdowns.Count; i++) PlayerPrefs.DeleteKey(Key($"Dropdown.{i}"));
        PlayerPrefs.Save();
    }

    // Event handlers
    private void OnRosIpChanged(string value)
    {
        if (_isLoading) return;
        PlayerPrefs.SetString(Key("ROSIP"), value ?? string.Empty);
        PlayerPrefs.Save();
    }

    private void OnToggleChanged(int index, bool value)
    {
        if (_isLoading) return;
        PlayerPrefs.SetInt(Key($"Toggle.{index}"), value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnTextInputChanged(int index, string value)
    {
        if (_isLoading) return;
        PlayerPrefs.SetString(Key($"Text.{index}"), value ?? string.Empty);
        PlayerPrefs.Save();
    }

    private void OnDropdownChanged(int index, int selected)
    {
        if (_isLoading) return;
        PlayerPrefs.SetInt(Key($"Dropdown.{index}"), Mathf.Max(-1, selected));
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// After loading, broadcast the current values so other listeners
    /// (wired in the Inspector) get their initial callbacks.
    /// </summary>
    private void RaiseInitialEvents()
    {
        bool previousLoading = _isLoading;
        _isLoading = true;

        if (rosIpInput != null)
        {
            rosIpInput.onValueChanged.Invoke(rosIpInput.text);
        }

        for (int i = 0; i < toggles.Count; i++)
        {
            var t = toggles[i];
            if (t != null)
            {
                t.onValueChanged.Invoke(t.isOn);
            }
        }

        for (int i = 0; i < textInputs.Count; i++)
        {
            var ti = textInputs[i];
            if (ti != null)
            {
                ti.onValueChanged.Invoke(ti.text);
            }
        }

        // Dropdowns already trigger their own events via SafeSetDropdownSelectedIndex

        _isLoading = previousLoading;
    }

    // Utilities
    private string Key(string name) => string.Concat(keyPrefix, name);

    private static int SafeGetDropdownSelectedIndex(DropDownGroup dd)
    {
        // Use public getter if available
        if (dd != null)
        {
            try { return dd.SelectedIndex; }
            catch { /* fallback below */ }
        }
        return -1;
    }

    private static void SafeSetDropdownSelectedIndex(DropDownGroup dd, int index)
    {
        if (dd == null) return;
        if (index < 0) return;

        // We cannot set SelectedIndex directly (private setter).
        // Instead, toggle the corresponding Toggle in the same ToggleGroup.
        try
        {
            // Find ToggleGroup and toggles just like DropDownGroup does
            var toggleGroup = dd.GetComponentInChildren<ToggleGroup>();
            if (toggleGroup == null) return;

            Toggle[] toggles = toggleGroup.transform.GetComponentsInChildren<Toggle>()
                .Where(t => t.group == toggleGroup).ToArray();
            if (toggles == null || toggles.Length == 0) return;

            int clamped = Mathf.Clamp(index, 0, toggles.Length - 1);

            // Setting isOn triggers the dropdown's own listeners and updates header visuals
            toggles[clamped].isOn = true;
        }
        catch
        {
            // ignore if structure differs
        }
    }
}
