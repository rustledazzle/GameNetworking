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
    [SerializeField] private int maxAmmo = 48;
    [SerializeField] private int currentAmmo = 12;
    [SerializeField] private int reserveAmmo = 60;

    [Header("Weapon Icons")]
    [SerializeField] private Sprite pistolIcon;
    [SerializeField] private Sprite rifleIcon;

    private float currentHealth = 100f;
    private float maxHealth = 100f;

    private void Start()
    {
        // Auto-find references if not assigned
        if (healthBar == null)
            healthBar = transform.Find("HealthPanel/HealthBar")?.GetComponent<Image>();

        if (healthText == null)
            healthText = transform.Find("HealthPanel/HealthText")?.GetComponent<TMP_Text>();

        if (ammoText == null)
            ammoText = transform.Find("AmmoPanel/AmmoText")?.GetComponent<TMP_Text>();

        if (weaponIcon == null)
            weaponIcon = transform.Find("AmmoPanel/WeaponIcon")?.GetComponent<Image>();

        UpdateHealthUI();
        UpdateAmmoUI();
    }

    private void Update()
    {
        // DEBUG: Test health with H key
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10);
        }

        // DEBUG: Test ammo with R key (reload)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reload();
        }
    }

    public void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)} HP";
        }
    }

    public void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{currentAmmo} / {maxAmmo}";
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Debug.Log("Player died!");
            RespawnPlayer();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUI();
    }

    public void UseAmmo(int amount)
    {
        currentAmmo = Mathf.Max(0, currentAmmo - amount);
        UpdateAmmoUI();

        if (currentAmmo <= 0 && reserveAmmo > 0)
        {
            Debug.Log("Out of ammo! Press R to reload.");
        }
        else if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo! No reserve ammo left.");
        }
    }

    public void Reload()
    {
        if (reserveAmmo <= 0)
        {
            Debug.Log("No reserve ammo!");
            return;
        }
        if (currentAmmo >= maxAmmo)
        {
            Debug.Log("Magazine is full!");
            return;
        }

        int ammoNeeded = maxAmmo - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);

        reserveAmmo -= ammoToReload;
        currentAmmo += ammoToReload;

        UpdateAmmoUI();
        Debug.Log($"Reloaded! Current: {currentAmmo}, Reserve: {reserveAmmo}");
    }

    public void SetWeaponPistol()
    {
        if (weaponIcon != null && pistolIcon != null)
        {
            weaponIcon.sprite = pistolIcon;
        }
        maxAmmo = 48;
        UpdateAmmoUI();
    }

    public void SetWeaponRifle()
    {
        if (weaponIcon != null && rifleIcon != null)
        {
            weaponIcon.sprite = rifleIcon;
        }
        maxAmmo = 30;
        UpdateAmmoUI();
    }

    private void RespawnPlayer()
    {
        currentHealth = maxHealth;
        currentAmmo = 12;
        reserveAmmo = 60;
        UpdateHealthUI();
        UpdateAmmoUI();

        // Position reset is handled by HealthSystem or SpawnManager
        Debug.Log("Player respawned!");
    }

    // Public getters
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
    public bool CanShoot() => currentAmmo > 0;
}