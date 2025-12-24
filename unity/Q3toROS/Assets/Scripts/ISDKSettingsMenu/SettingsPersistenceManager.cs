using System.Collections.Generic;
using System.Linq;
using Oculus.Interaction.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persist simple UI settings (PlayerPrefs) so they remain the same across app restarts
///
/// Supported:
/// - A list of toggles (UnityEngine.UI.Toggle)
/// - A list of text values (TMP_InputField)
/// - A list of dropdown menus (ISDK) (Oculus.Interaction.Samples.DropDownGroup)
///
/// Usage:
/// - Attach to a GameObject
/// - Change the keyPrefix to maintain separate profiles.
/// - Assign fields/lists via Inspector.
///
/// Notes:
/// - Uses PlayerPrefs under the hood.
/// - Saves on change; loads on Awake.
/// </summary>
public class SettingsPersistenceManager : MonoBehaviour
{
    [Header("Keys & Profile")]
    [SerializeField]
    [Tooltip("Prefix added to all PlayerPrefs keys so multiple profiles/projects don't collide.")]
    private string m_keyPrefix = "Q3toROS.";

    [Header("Toggles to persist (UnityEngine.UI.Toggle)")]
    [SerializeField]
    private List<Toggle> m_toggles = new();

    [Header("Text inputs to persist (TMP_InputField)")]
    [SerializeField]
    private List<TMP_InputField> m_textInputs = new();

    [Header("Dropdowns to persist (ISDK DropDownGroup)")]
    [SerializeField]
    private List<DropDownGroup> m_dropdowns = new();

    // Flag to avoid saving while loading
    private bool m_isLoading;

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
        for (var i = 0; i < m_toggles.Count; i++)
        {
            var idx = i;
            if (m_toggles[idx] != null)
            {
                m_toggles[idx].onValueChanged.AddListener(v => OnToggleChanged(idx, v));
            }
        }

        for (var i = 0; i < m_textInputs.Count; i++)
        {
            var idx = i;
            if (m_textInputs[idx] != null)
            {
                m_textInputs[idx].onEndEdit.AddListener(v => OnTextInputChanged(idx, v));
            }
        }

        for (var i = 0; i < m_dropdowns.Count; i++)
        {
            var idx = i;
            var dd = m_dropdowns[idx];
            if (dd != null)
            {
                dd.WhenSelectionChanged ??= new UnityEngine.Events.UnityEvent<int>();
                dd.WhenSelectionChanged.AddListener(selected => OnDropdownChanged(idx, selected));
            }
        }
    }

    private void Unsubscribe()
    {
        for (var i = 0; i < m_toggles.Count; i++)
        {
            var idx = i;
            if (m_toggles[idx] != null)
            {
                m_toggles[idx].onValueChanged.RemoveAllListeners();
            }
        }

        for (var i = 0; i < m_textInputs.Count; i++)
        {
            var idx = i;
            if (m_textInputs[idx] != null)
            {
                m_textInputs[idx].onEndEdit.RemoveAllListeners();
            }
        }

        for (var i = 0; i < m_dropdowns.Count; i++)
        {
            var idx = i;
            var dd = m_dropdowns[idx];
            if (dd != null && dd.WhenSelectionChanged != null)
            {
                dd.WhenSelectionChanged.RemoveAllListeners();
            }
        }
    }

    // Public helpers in case needed
    public void SaveAll()
    {
        for (var i = 0; i < m_toggles.Count; i++)
        {
            var t = m_toggles[i];
            if (t != null)
            {
                PlayerPrefs.SetInt(Key($"Toggle.{i}"), t.isOn ? 1 : 0);
            }
        }

        for (var i = 0; i < m_textInputs.Count; i++)
        {
            var ti = m_textInputs[i];
            if (ti != null)
            {
                PlayerPrefs.SetString(Key($"Text.{i}"), ti.text);
            }
        }

        for (var i = 0; i < m_dropdowns.Count; i++)
        {
            var dd = m_dropdowns[i];
            if (dd != null)
            {
                var sel = SafeGetDropdownSelectedIndex(dd);
                PlayerPrefs.SetInt(Key($"Dropdown.{i}"), sel);
            }
        }

        PlayerPrefs.Save();
    }

    public void LoadAll()
    {
        m_isLoading = true;

        for (var i = 0; i < m_toggles.Count; i++)
        {
            var t = m_toggles[i];
            if (t == null) continue;
            var def = t.isOn ? 1 : 0;
            var saved = PlayerPrefs.GetInt(Key($"Toggle.{i}"), def);
            t.SetIsOnWithoutNotify(saved != 0);
        }

        for (var i = 0; i < m_textInputs.Count; i++)
        {
            var ti = m_textInputs[i];
            if (ti == null) continue;
            var v = PlayerPrefs.GetString(Key($"Text.{i}"), ti.text);
            ti.SetTextWithoutNotify(v);
        }

        for (var i = 0; i < m_dropdowns.Count; i++)
        {
            var dd = m_dropdowns[i];
            if (dd == null) continue;
            var current = SafeGetDropdownSelectedIndex(dd);
            var saved = PlayerPrefs.GetInt(Key($"Dropdown.{i}"), current);
            SafeSetDropdownSelectedIndex(dd, saved);
        }

        m_isLoading = false;
    }

    public void ClearAll()
    {
        // Remove all saved keys
        for (var i = 0; i < m_toggles.Count; i++) PlayerPrefs.DeleteKey(Key($"Toggle.{i}"));
        for (var i = 0; i < m_textInputs.Count; i++) PlayerPrefs.DeleteKey(Key($"Text.{i}"));
        for (var i = 0; i < m_dropdowns.Count; i++) PlayerPrefs.DeleteKey(Key($"Dropdown.{i}"));
        PlayerPrefs.Save();
    }

    // Event handlers
    private void OnToggleChanged(int index, bool value)
    {
        if (m_isLoading) return;
        PlayerPrefs.SetInt(Key($"Toggle.{index}"), value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnTextInputChanged(int index, string value)
    {
        if (m_isLoading) return;
        PlayerPrefs.SetString(Key($"Text.{index}"), value ?? string.Empty);
        PlayerPrefs.Save();
    }

    private void OnDropdownChanged(int index, int selected)
    {
        if (m_isLoading) return;
        PlayerPrefs.SetInt(Key($"Dropdown.{index}"), Mathf.Max(-1, selected));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// After loading, broadcast the current values so other listeners
    /// (wired in the Inspector) get their initial callbacks.
    /// </summary>
    private void RaiseInitialEvents()
    {
        var previousLoading = m_isLoading;
        m_isLoading = true;

        for (var i = 0; i < m_toggles.Count; i++)
        {
            var t = m_toggles[i];
            if (t != null)
            {
                t.onValueChanged.Invoke(t.isOn);
            }
        }

        for (var i = 0; i < m_textInputs.Count; i++)
        {
            var ti = m_textInputs[i];
            if (ti != null)
            {
                ti.onValueChanged.Invoke(ti.text);
            }
        }

        // Dropdowns already trigger their own events via SafeSetDropdownSelectedIndex

        m_isLoading = previousLoading;
    }

    // Utilities
    private string Key(string name) => string.Concat(m_keyPrefix, name);

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

        try
        {
            // Find ToggleGroup and toggles just like DropDownGroup does
            var toggleGroup = dd.GetComponentInChildren<ToggleGroup>();
            if (toggleGroup == null) return;

            var toggles = toggleGroup.transform.GetComponentsInChildren<Toggle>()
                .Where(t => t.group == toggleGroup).ToArray();
            if (toggles == null || toggles.Length == 0) return;

            var clamped = Mathf.Clamp(index, 0, toggles.Length - 1);

            // Setting isOn triggers the dropdown's own listeners and updates header visuals
            toggles[clamped].isOn = true;
        }
        catch
        {
            // ignore if structure differs
        }
    }
}
