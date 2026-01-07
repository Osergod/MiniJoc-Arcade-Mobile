using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CambiarEscena(string nombreEscena)
    {
        SceneManager.LoadScene(nombreEscena);
    }

    public void MostrarPanel(GameObject panel)
    {
        panel.SetActive(true);
    }
    
    public void OcultarPanel(GameObject panel)
    {
        panel.SetActive(false);
    }

    public void Close()
    {
        Application.Quit();
    }

    /*public void TogleBackgroundMusic()
    {
        GeneralManager.Instance.ToggleMusica();
    }*/
}
