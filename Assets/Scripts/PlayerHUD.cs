using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TMP_Text healthText;

    [Header("Ammo")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image weaponIcon;

    [Header("Ammo Settings")]
    [Tooltip("Magazine size (max bullets in one reload).")]
    [SerializeField] private int maxAmmo = 12;         // ← magazine size

    [Tooltip("Bullets currently in the magazine.")]
    [SerializeField] private int currentAmmo = 12;

    [Tooltip("Extra bullets in reserve.")]
    [SerializeField] private int reserveAmmo = 48;     // ← reserve

    [Header("Weapon Icons")]
    [SerializeField] private Sprite pistolIcon;
    [SerializeField] private Sprite rifleIcon;

    private float currentHealth = 100f;
    private float maxHealth = 100f;

    private void Start()
    {
        if (healthBar == null)
            healthBar = transform.Find("HealthPanel/HealthBar")?.GetComponent<Image>();

        if (healthText == null)
            healthText = transform.Find("HealthPanel/HealthText")?.GetComponent<TMP_Text>();

        if (ammoText == null)
            ammoText = transform.Find("AmmoPanel/AmmoText")?.GetComponent<TMP_Text>();

        if (weaponIcon == null)
            weaponIcon = transform.Find("AmmoPanel/WeaponIcon")?.GetComponent<Image>();

        // Clamp starting values
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);

        UpdateHealthUI();
        UpdateAmmoUI();
    }

    public void UpdateHealthFromNetwork(float newHealth, float newMaxHealth)
    {
        currentHealth = newHealth;
        maxHealth = newMaxHealth;
        UpdateHealthUI();
    }

    public void UpdateHealthFromNetwork(float newHealth)
    {
        currentHealth = newHealth;
        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        if (healthBar != null)
            healthBar.fillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0f;

        if (healthText != null)
            healthText.text = $"{Mathf.Ceil(currentHealth)} HP";
    }

    public void UpdateAmmoUI()
    {
        if (ammoText != null)
            ammoText.text = $"{currentAmmo} / {reserveAmmo}";   // ✅ shows reserve
    }

    public void UseAmmo(int amount)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - amount);
        UpdateAmmoUI();
    }

    public void Reload()
    {
        if (reserveAmmo <= 0)
        {
            Debug.Log("[HUD] No reserve ammo to reload.");
            return;
        }

        if (currentAmmo >= maxAmmo)
        {
            Debug.Log("[HUD] Magazine is already full.");
            return;
        }

        int ammoNeeded = maxAmmo - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

        reserveAmmo -= ammoToReload;
        currentAmmo += ammoToReload;
        UpdateAmmoUI();
        Debug.Log($"[HUD] Reloaded → {currentAmmo}/{reserveAmmo}");
    }

    public bool CanShoot() => currentAmmo > 0;

    // Optional accessors
    public int GetCurrentAmmo() => currentAmmo;
    public int GetReserveAmmo() => reserveAmmo;
    public int GetMagazineSize() => maxAmmo;
}