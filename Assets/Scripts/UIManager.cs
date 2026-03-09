using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{

    public GameObject NewDataPanel;
    public string TheGame = "Scene_Play";
    public GameObject continueButton;
    public GameObject settingsPanel;
    public GameObject creditsPanel;

    // BG music and SFX
    public AudioSource bgMusicSource;
    public AudioSource[] sfxSources;

    // Set BG music volume
    public void SetBGMusicVolume(float volume)
    {
        if (bgMusicSource != null)
            bgMusicSource.volume = volume;
    }

    // Set SFX volume for all
    public void SetAllSFXVolume(float volume)
    {
        if (sfxSources != null)
        {
            foreach (var sfx in sfxSources)
            {
                if (sfx != null)
                    sfx.volume = volume;
            }
        }
    }

    // Conditional SFX play
    public void PlaySFXIf(int index, bool condition)
    {
        if (condition && sfxSources != null && index >= 0 && index < sfxSources.Length)
        {
            sfxSources[index]?.Play();
        }
    }


    void Start()
{
    if (!PlayerPrefs.HasKey("SaveData"))
    {
        continueButton.SetActive(false);
    }
}
	public void StartGame()
	{	 

        // Check if save data exists
        if (PlayerPrefs.HasKey("SaveData"))
        {
            // Show confirmation panel
            NewDataPanel.SetActive(true);
        }
        else
        {
            Debug.Log("Start Game pressed");
            // No save data → start game immediately
            SceneManager.LoadScene(TheGame);
        }
	}
    public void ConfirmNewGame()
    {
        // Delete old save
        PlayerPrefs.DeleteKey("SaveData");

        // Start new game
        SceneManager.LoadScene(TheGame);
    }

    public void CancelNewGame()
    {
        // Hide panel if player cancels
        NewDataPanel.SetActive(false);
    }

	public void ContinueGame()
	{
		Debug.Log("Continue Game pressed");

        // Check if saved data exists
        if (PlayerPrefs.HasKey("SaveData"))
        {
            SceneManager.LoadScene(TheGame);
        }
        else
        {
            Debug.Log("No saved game found.");
        }
	}

	public void OpenSettings()
    {
        Debug.Log("Settings pressed");
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

	public void ShowCredits()
    {
        Debug.Log("Credits pressed");
        creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        creditsPanel.SetActive(false);
    }

	public void ExitGame()
	{
		Debug.Log("Exit pressed");
		Application.Quit();
	}


    // Music and SFX Methods
    
}
