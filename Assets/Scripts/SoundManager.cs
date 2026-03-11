using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{

     public void ChangeGroupVolume(string parameterName, float volumeInDb)
    {
        // parameterName would be "MusicVol" or "SFXVol"
        mainMixer.SetFloat(parameterName, volumeInDb);
    }
    // Singleton instance
    public static SoundManager Instance;

    [Header("References")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioMixer mainMixer;

    private void Awake()
    {
        // Ensure only one instance exists (Singleton Pattern)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Plays a one-shot sound effect.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        sfxSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Changes the background music.
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    /// <summary>
    /// Controls volume via Mixer (value should be -80 to 20).
    /// </summary>
    public void ChangeMasterVolume(float volume)
    {
        mainMixer.SetFloat("MasterVol", volume);
    }

   
}
