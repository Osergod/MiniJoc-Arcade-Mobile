using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralManager : MonoBehaviour
{
    public static GeneralManager Instance;
    public bool musicaActiva = true;
    
    // Referencia a la música ACTUAL
    public AudioSource musicaActual;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnEscenaCargada; // ¡IMPORTANTE!
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Esto se ejecuta CADA VEZ que se carga una escena
    void OnEscenaCargada(Scene escena, LoadSceneMode modo)
    {
        // Buscar la música en la escena nueva
        AudioManager nuevoAM = FindObjectOfType<AudioManager>();
        
        if (nuevoAM != null && nuevoAM.BackgroundMusic != null)
        {
            musicaActual = nuevoAM.BackgroundMusic;
            
            // Aplicar el estado guardado
            if (musicaActiva)
            {
                musicaActual.Play();
            }
            else
            {
                musicaActual.Pause();
            }
        }
    }
    
    public void ToggleMusica()
    {
        musicaActiva = !musicaActiva;
        
        if (musicaActual != null)
        {
            if (musicaActiva)
            {
                musicaActual.Play();
            }
            else
            {
                musicaActual.Pause();
            }
        }
    }
}