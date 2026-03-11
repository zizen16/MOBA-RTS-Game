using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{

    [Header("Panels")]
    public GameObject NewDataPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    public GameObject continueButton;

    [Header("Settings")]
    public string TheGame = "Scene_Play";

    void Start()
    {
        if (!PlayerPrefs.HasKey("SaveData"))
        {
            continueButton.SetActive(false);
        }
    }

    // --- Audio UI Bridges ---

    public void OnMusicSliderChanged(float value)
    {
        // Value from slider (0.0001 to 1) converted to Decibels for the Mixer
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20;
        SoundManager.Instance.ChangeGroupVolume("MusicVol", dB);
    }

    public void OnSFXSliderChanged(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20;
        SoundManager.Instance.ChangeGroupVolume("SFXVol", dB);
    }

    public void PlayButtonHoverSound(AudioClip clip)
    {
        SoundManager.Instance.PlaySFX(clip);
    }

    // --- Scene Management ---

    public void StartGame()
    {    
        if (PlayerPrefs.HasKey("SaveData"))
            NewDataPanel.SetActive(true);
        else
            SceneManager.LoadScene(TheGame);
    }

    public void ConfirmNewGame()
    {
        PlayerPrefs.DeleteKey("SaveData");
        SceneManager.LoadScene(TheGame);
    }

    public void CancelNewGame() => NewDataPanel.SetActive(false);

    public void ContinueGame()
    {
        if (PlayerPrefs.HasKey("SaveData"))
            SceneManager.LoadScene(TheGame);
    }

    public void OpenSettings() => settingsPanel.SetActive(true);
    public void CloseSettings() => settingsPanel.SetActive(false);
    public void ShowCredits() => creditsPanel.SetActive(true);
    public void CloseCredits() => creditsPanel.SetActive(false);
    public void ExitGame() => Application.Quit();
}
    

