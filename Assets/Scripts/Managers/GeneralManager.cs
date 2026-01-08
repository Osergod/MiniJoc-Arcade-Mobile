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
            
            // CARGAR CONFIGURACIÓN GUARDADA
            CargarConfiguracion();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        GuardarConfiguracion();
    }
    
    void OnApplicationQuit()
    {
        GuardarConfiguracion();
    }
    
    void OnEscenaCargada(Scene escena, LoadSceneMode modo)
    {
        ConfigurarNuevoAudioManager();
    }
    
    // ========== GUARDAR Y CARGAR ==========
    
    void GuardarConfiguracion()
    {
        PlayerPrefs.SetInt("MusicaActiva", musicaActiva ? 1 : 0);
        PlayerPrefs.SetInt("SFXActivados", sfxActivados ? 1 : 0);
        PlayerPrefs.SetFloat("VolumenMusica", volumenMusica);
        PlayerPrefs.SetFloat("VolumenSFX", volumenSFX);
        
        PlayerPrefs.Save();
    }
    
    void CargarConfiguracion()
    {
        musicaActiva = PlayerPrefs.GetInt("MusicaActiva", 1) == 1;
        sfxActivados = PlayerPrefs.GetInt("SFXActivados", 1) == 1;
        volumenMusica = PlayerPrefs.GetFloat("VolumenMusica", 0.7f);
        volumenSFX = PlayerPrefs.GetFloat("VolumenSFX", 0.8f);
    }
    
    public void GuardarCambios()
    {
        GuardarConfiguracion();
    }
    
    public void ResetearAjustes()
    {
        musicaActiva = true;
        sfxActivados = true;
        volumenMusica = 0.7f;
        volumenSFX = 0.8f;
        
        AplicarConfigAudio();
        GuardarConfiguracion();
    }
    
    // ========== CONFIGURACIÓN DE AUDIO ==========
    
    void ConfigurarNuevoAudioManager()
    {
        AudioManager nuevoAM = FindObjectOfType<AudioManager>();
        
        if (nuevoAM != null)
        {
            audioManagerActual = nuevoAM;
            
            if (nuevoAM.BackgroundMusic != null)
            {
                musicaActual = nuevoAM.BackgroundMusic;
                // Solo aplicar configuración, NO tocar Play/Pause aquí
                musicaActual.volume = volumenMusica;
            }
            
            // Configurar música según estado guardado
            if (musicaActual != null)
            {
                if (musicaActiva)
                {
                    if (!musicaActual.isPlaying)
                    {
                        musicaActual.Play(); // Solo play si no está sonando
                    }
                }
                else
                {
                    musicaActual.Pause();
                }
            }
            
            // Configurar SFX
            AplicarConfigSFX();
        }
    }
    
    public void AplicarConfigSFX()
    {
        AudioSource[] todosLosAudioSources = FindObjectsOfType<AudioSource>();
        
        foreach (AudioSource audioSource in todosLosAudioSources)
        {
            if (audioSource != musicaActual && audioSource != null)
            {
                audioSource.volume = volumenSFX;
                audioSource.mute = !sfxActivados;
            }
        }
    }
    
    public void AplicarConfigAudio()
    {
        // 1. Solo cambiar volumen de música (NO estado play/pause)
        if (musicaActual != null)
        {
            musicaActual.volume = volumenMusica;
            // NO tocar Play/Pause aquí, solo volumen
        }
        
        // 2. Configurar SFX
        AplicarConfigSFX();
        
        // 3. Guardar cambios
        GuardarConfiguracion();
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
        
        GuardarConfiguracion();
    }
}