using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Manages the SDK settings menu functionality, including showing, hiding,
/// and positioning the menu. It also handles the associated audio feedback
/// for menu interactions.
/// </summary>
public class ISDKSettingsMenuInteractionManager : MonoBehaviour
{
    /// <summary>
    /// The Parent Object of the Menu
    /// </summary>
    [Tooltip("The parent object of the menu")]
    [Header("Place the grabbable parent object here")]
    [SerializeField]
    private GameObject m_menuParent;

    /// <summary>
    /// The audio to play when showing the menu panel
    /// </summary>
    [Tooltip("The audio to play when showing the menu panel")]
    [Header("Place the menu open audio here")]
    [SerializeField]
    private AudioSource m_showMenuAudio;

    /// <summary>
    /// The audio to play when hiding the menu panel
    /// </summary>
    [Tooltip("The audio to play when hiding the menu panel")]
    [Header("Place the menu hide audio here")]
    [SerializeField]
    private AudioSource m_hideMenuAudio;

    /// <summary>
    /// The location the menu should be spawning at
    /// </summary>
    [Tooltip("The location the menu should be spawning at")]
    [Header("The location the menu should be spawning at")]
    [SerializeField]
    private GameObject m_spawnPoint;

    private bool m_started = false;
    protected virtual void Start()
    {
        this.BeginStart(ref m_started);
        this.AssertField(m_menuParent, nameof(m_menuParent));
        this.AssertField(m_showMenuAudio, nameof(m_showMenuAudio));
        this.AssertField(m_hideMenuAudio, nameof(m_hideMenuAudio));
        this.AssertField(m_spawnPoint, nameof(m_spawnPoint));

        this.EndStart(ref m_started);
    }

    /// <summary>
    /// Show/hide the menu.
    /// </summary>

    public void ToggleMenu()
    {
        if (m_menuParent.activeSelf)
        {
            m_hideMenuAudio.Play();
            m_menuParent.SetActive(false);
        }
        else
        {
            m_showMenuAudio.Play();
            m_menuParent.transform.position = m_spawnPoint.transform.position;
            m_menuParent.transform.rotation = m_spawnPoint.transform.rotation;
            m_menuParent.SetActive(true);
        }
    }

    #region Injects
    public void InjectAllMenuItems(GameObject parent,
        AudioSource show, AudioSource hide, GameObject spawnpoint)
    {
        InjectMenuParent(parent);
        InjectShowAudio(show);
        InjectHideAudio(hide);
        InjectSpawnPoint(spawnpoint);
    }

    private void InjectMenuParent(GameObject parent)
    {
        m_menuParent = parent;
    }

    private void InjectShowAudio(AudioSource show)
    {
        m_showMenuAudio = show;
    }

    private void InjectHideAudio(AudioSource hide)
    {
        m_hideMenuAudio = hide;
    }

    private void InjectSpawnPoint(GameObject spawnpoint)
    {
        m_spawnPoint = spawnpoint;
    }
    #endregion
}