using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private GeneralManager GeneralManager;

    public AudioSource BackgroundMusic;
    public AudioSource ButtonSound;
    public AudioSource CoinSound;
    
    void Start()
    {
        if (GeneralManager.Instance != null)
        {
            GeneralManager.Instance.musicaActual = BackgroundMusic;
        }
    }
    
    void Update()
    {
        
    }

    // Método para hacer sonar los botons al ser pulsados
    public void SoundButton()
    {
        ButtonSound.Play();
    }

    // Método para hacer sonar los diamantes al ser recogidos
    public void SoundCoin()
    {
        CoinSound.Play();
    }
}