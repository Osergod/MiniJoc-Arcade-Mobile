using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }
    
    [Header("UI")]
    public TMP_Text coinCountTextHUD;
    public TMP_Text coinCountTextStats;
    public TMP_Text coinCountTextShop;
    
    public int totalCoins = 0;
    public int parcialCoins = 0;
    
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
        parcialCoins = 0;
        UpdateUI();

        coinCountTextShop.text = totalCoins.ToString();
    }
    
    public void AddCoin(int amount = 1)
    {
        totalCoins += amount;
        parcialCoins += amount;
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();
        UpdateUI();
        
        // Efecto opcional
        Debug.Log($"¡Moneda recolectada! Total: {totalCoins}");
    }
    
    void UpdateUI()
    {
        if (coinCountTextHUD != null)
            coinCountTextHUD.text = parcialCoins.ToString();

        if (coinCountTextStats != null)
            coinCountTextStats.text = parcialCoins.ToString();
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