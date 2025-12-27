using UnityEngine;
using UnityEngine.UI;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }
    
    [Header("UI")]
    public Text coinCountText;
    
    private int totalCoins = 0;
    
    void Awake()
    {
        // Singleton simple
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Cargar monedas guardadas
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        UpdateUI();
    }
    
    public void AddCoin(int amount = 1)
    {
        totalCoins += amount;
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        UpdateUI();
        
        // Efecto opcional
        Debug.Log($"¡Moneda recolectada! Total: {totalCoins}");
    }
    
    void UpdateUI()
    {
        if (coinCountText != null)
        {
            coinCountText.text = totalCoins.ToString();
        }
    }
    
    public int GetCoinCount()
    {
        return totalCoins;
    }
    
    public void ResetCoins()
    {
        totalCoins = 0;
        PlayerPrefs.SetInt("TotalCoins", 0);
        UpdateUI();
    }
}