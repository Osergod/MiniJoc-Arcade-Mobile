using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralManager : MonoBehaviour
{
    public static GeneralManager Instance;
    
    [Header("Configuración de Audio")]
    public bool musicaActiva = true;
    public bool sfxActivados = true;
    public float volumenMusica = 1f;
    public float volumenSFX = 1f;
    
    // Referencia a la música ACTUAL
    public AudioSource musicaActual;
    private AudioManager audioManagerActual;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnEscenaCargada;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnEscenaCargada(Scene escena, LoadSceneMode modo)
    {
        ConfigurarNuevoAudioManager();
    }
    
    void ConfigurarNuevoAudioManager()
    {
        // Buscar AudioManager en la escena nueva
        AudioManager nuevoAM = FindObjectOfType<AudioManager>();
        
        if (nuevoAM != null)
        {
            audioManagerActual = nuevoAM;
            
            // Configurar la música de fondo
            if (nuevoAM.BackgroundMusic != null)
            {
                musicaActual = nuevoAM.BackgroundMusic;
                musicaActual.volume = volumenMusica;
                
                if (musicaActiva && !musicaActual.isPlaying)
                {
                    musicaActual.Play();
                }
                else if (!musicaActiva && musicaActual.isPlaying)
                {
                    musicaActual.Pause();
                }
            }
            
            // Aplicar configuración de SFX a todos los efectos
            AplicarConfigSFX();
        }
    }
    
    // Método que aplica configuración SOLO a efectos de sonido
    public void AplicarConfigSFX()
    {
        // Buscar TODOS los AudioSource en la escena
        AudioSource[] todosLosAudioSources = FindObjectsOfType<AudioSource>();
        
        foreach (AudioSource audioSource in todosLosAudioSources)
        {
            // Aplicar solo a efectos de sonido (NO a la música de fondo)
            if (audioSource != musicaActual && audioSource != null)
            {
                audioSource.volume = volumenSFX;
                audioSource.mute = !sfxActivados;
            }
        }
    }
    
    // Método que aplica TODA la configuración de audio
    public void AplicarConfigAudio()
    {
        // 1. Configurar música
        if (musicaActual != null)
        {
            musicaActual.volume = volumenMusica;
            
            if (musicaActiva && !musicaActual.isPlaying)
            {
                musicaActual.UnPause();
            }
            else if (!musicaActiva && musicaActual.isPlaying)
            {
                musicaActual.Pause();
            }
        }
        
        // 2. Configurar TODOS los efectos de sonido
        AplicarConfigSFX();
    }
    
    public void ToggleMusica()
    {
        musicaActiva = !musicaActiva;
        
        if (musicaActual != null)
        {
            if (musicaActiva)
            {
                if (!musicaActual.isPlaying)
                {
                    musicaActual.Play();
                }
            }
            else
            {
                musicaActual.Pause();
            }
        }
    }
}