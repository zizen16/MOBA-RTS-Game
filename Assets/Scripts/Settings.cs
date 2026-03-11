using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [Header("Mixer Reference")]
    [SerializeField] private AudioMixer mainMixer;

    [Header("UI Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        // Load saved values or use default (0.75f)
        float savedMusic = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVol", 0.75f);

        // Set Slider visuals
        if (musicSlider) musicSlider.value = savedMusic;
        if (sfxSlider) sfxSlider.value = savedSFX;

        // Apply to Mixer
        SetMusicVolume(savedMusic);
        SetSFXVolume(savedSFX);
    }

    public void SetMusicVolume(float value)
    {
        // Convert 0-1 slider value to -80 to 20 Decibels
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20;
        mainMixer.SetFloat("MusicVol", dB);
        PlayerPrefs.SetFloat("MusicVol", value);
    }

    public void SetSFXVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20;
        mainMixer.SetFloat("SFXVol", dB);
        PlayerPrefs.SetFloat("SFXVol", value);
    }

    [Header("Pause Menu")]
    [SerializeField] private GameObject pausePanel;
    public void PauseGame(bool isPaused)
    {
        Time.timeScale = isPaused ? 0f : 1f;
        pausePanel.SetActive(isPaused);

        // Tell the SoundManager to dim the music when paused
        if (isPaused)
            SoundManager.Instance.ChangeGroupVolume("MusicVol", -20f); // Make it quieter
        else
            SoundManager.Instance.ChangeGroupVolume("MusicVol", 0f);   // Return to normal
    }



    
}
