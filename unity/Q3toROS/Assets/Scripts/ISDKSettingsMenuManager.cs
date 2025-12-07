using Oculus.Interaction;
using UnityEngine;

/// <summary>
/// Manages the SDK settings menu functionality, including showing, hiding,
/// and positioning the menu. It also handles the associated audio feedback
/// for menu interactions.
/// </summary>
public class ISDKSettingsMenuManager : MonoBehaviour
{
    /// <summary>
    /// The Parent Object of the Menu
    /// </summary>
    [Tooltip("The parent object of the menu")]
    [Header("Place the grabbable parent object here")]
    [SerializeField]
    private GameObject menuParent;

    /// <summary>
    /// The audio to play when showing the menu panel
    /// </summary>
    [Tooltip("The audio to play when showing the menu panel")]
    [Header("Place the menu open audio here")]
    [SerializeField]
    private AudioSource showMenuAudio;

    /// <summary>
    /// The audio to play when hiding the menu panel
    /// </summary>
    [Tooltip("The audio to play when hiding the menu panel")]
    [Header("Place the menu hide audio here")]
    [SerializeField]
    private AudioSource hideMenuAudio;

    /// <summary>
    /// The location the menu should be spawning at
    /// </summary>
    [Tooltip("The location the menu should be spawning at")]
    [Header("The location the menu should be spawning at")]
    [SerializeField]
    private GameObject spawnPoint;

    private bool m_started = false;
    protected virtual void Start()
    {
        this.BeginStart(ref m_started);
        this.AssertField(menuParent, nameof(menuParent));
        this.AssertField(this.showMenuAudio, nameof(showMenuAudio));
        this.AssertField(this.hideMenuAudio, nameof(hideMenuAudio));
        this.AssertField(this.spawnPoint, nameof(spawnPoint));

        this.EndStart(ref m_started);
    }

    /// <summary>
    /// Show/hide the menu.
    /// </summary>

    public void ToggleMenu()
    {
        if (menuParent.activeSelf)
        {
            hideMenuAudio.Play();
            menuParent.SetActive(false);
        }
        else
        {
            showMenuAudio.Play();
            menuParent.transform.position = spawnPoint.transform.position;
            menuParent.transform.rotation = spawnPoint.transform.rotation;
            menuParent.SetActive(true);
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
        menuParent = parent;
    }

    private void InjectShowAudio(AudioSource show)
    {
        showMenuAudio = show;
    }

    private void InjectHideAudio(AudioSource hide)
    {
        hideMenuAudio = hide;
    }

    private void InjectSpawnPoint(GameObject spawnpoint)
    {
        spawnPoint = spawnpoint;
    }
    #endregion
}