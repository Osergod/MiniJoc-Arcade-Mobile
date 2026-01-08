using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelOpciones;
    
    [Header("Controles de Audio")]
    public Slider sliderVolumenMusica;
    public Slider sliderVolumenSFX;
    public Toggle toggleMusica;
    public Toggle toggleSFX;

    [Header("Tienda")]
    public TMP_Text numCoins;
    
    void Start()
    {
        CargarConfigAudio();
    }
    
    void CargarConfigAudio()
    {
        if (GeneralManager.Instance != null)
        {
            // Configurar sliders
            if (sliderVolumenMusica != null)
            {
                sliderVolumenMusica.value = GeneralManager.Instance.volumenMusica;
                sliderVolumenMusica.onValueChanged.AddListener(CambiarVolumenMusica);
            }
            
            if (sliderVolumenSFX != null)
            {
                sliderVolumenSFX.value = GeneralManager.Instance.volumenSFX;
                sliderVolumenSFX.onValueChanged.AddListener(CambiarVolumenSFX);
            }
            
            // Configurar toggles
            if (toggleMusica != null)
            {
                toggleMusica.isOn = GeneralManager.Instance.musicaActiva;
                toggleMusica.onValueChanged.AddListener(ToggleMusica);
            }
            
            if (toggleSFX != null)
            {
                toggleSFX.isOn = GeneralManager.Instance.sfxActivados;
                toggleSFX.onValueChanged.AddListener(ToggleSonidosSFX);
            }
        }
    }

    public void CambiarEscena(string nombreEscena)
    {
        SceneManager.LoadScene(nombreEscena);
    }

    public void MostrarPanel(GameObject panel)
    {
        panel.SetActive(true);
        if (panel == panelOpciones)
        {
            CargarConfigAudio();
        }
    }
    
    public void OcultarPanel(GameObject panel)
    {
        panel.SetActive(false);
    }

    public void Close()
    {
        Application.Quit();
    }
    
    // ========== MÉTODOS PARA CONTROLES DE AUDIO ==========
    
    public void CambiarVolumenMusica(float volumen)
    {
        if (GeneralManager.Instance != null)
        {
            GeneralManager.Instance.volumenMusica = volumen;
            GeneralManager.Instance.AplicarConfigAudio(); // Esto ya guarda automáticamente
        }
    }
    
    public void CambiarVolumenSFX(float volumen)
    {
        if (GeneralManager.Instance != null)
        {
            GeneralManager.Instance.volumenSFX = volumen;
            GeneralManager.Instance.AplicarConfigAudio(); // Esto ya guarda automáticamente
        }
    }
    
    public void ToggleMusica(bool activado)
    {
        if (GeneralManager.Instance != null)
        {
            GeneralManager.Instance.musicaActiva = activado;
        
            // Aplicar cambio de estado play/pause
            if (GeneralManager.Instance.musicaActual != null)
            {
                if (activado)
                {
                    if (!GeneralManager.Instance.musicaActual.isPlaying)
                        GeneralManager.Instance.musicaActual.Play();
                }
                else
                {
                    GeneralManager.Instance.musicaActual.Pause();
                }
            }
        
            // Guardar cambios
            GeneralManager.Instance.GuardarCambios();
        }
    }
    
    public void ToggleSonidosSFX(bool activado)
    {
        if (GeneralManager.Instance != null)
        {
            GeneralManager.Instance.sfxActivados = activado;
            GeneralManager.Instance.AplicarConfigAudio(); // Esto ya guarda automáticamente
        }
    }
}