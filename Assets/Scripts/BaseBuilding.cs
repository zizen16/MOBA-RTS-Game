using UnityEngine;
using UnityEngine.UI;

public class BaseBuilding : MonoBehaviour, ISelectable, IDamageable
{
    public bool isAIOwned = false;
    public BuildingData buildingData;
    [SerializeField] GameObject selectionIndicator;
    [SerializeField] Canvas healthBarCanvas;
    [SerializeField] Slider healthBarSlider;
    [SerializeField] Image healthBarFill;
    [SerializeField] Vector3 healthBarOffset = new Vector3(0, 3, 0);

    public bool hidehealthBarWhenFull;
    //setting
    public float maxHealth = 500f;
    float currentHealth;
    Camera mainCamera;
    bool isSelected;

    void Awake()
    {
        mainCamera = Camera.main;
        maxHealth = buildingData.maxHealth;
        currentHealth = maxHealth;
        
    }
    void Start()
    {
        if (isAIOwned)
        {
            print("Building owned AI"  + buildingData.buildingName);
            AIManager.Instance.RegisterAIBuilding(this);
        }
        else
        {
            PlayerManager.Instance.RegisterBuilding(buildingData);
            PlayerManager.Instance.RegisterBaseBuilding(this);
        }
    }
    void LateUpdate()
    {
        UpdatehealthBar();
        UpdateHealthBarTransform();
    }
    void UpdatehealthBar()
    {
        healthBarSlider.value = currentHealth / maxHealth;
        UpdatehealthBarVisibility();
    }
    void UpdatehealthBarVisibility()
    {
        float currentHealthPercent = currentHealth / maxHealth;
        bool shouldShow;
        if (hidehealthBarWhenFull && currentHealthPercent >= 1)
        {
            shouldShow = false;
        }
        else
        {
            shouldShow = true;
        }
        healthBarCanvas.gameObject.SetActive(shouldShow);

    }
    void UpdateHealthBarTransform()
    {
        healthBarCanvas.transform.position = transform.position + healthBarOffset;
        healthBarCanvas.transform.rotation = mainCamera.transform.rotation;
    }
    public void Select()
    {
        if (isSelected)
        {
            return;
        }
        isSelected = true;
        selectionIndicator.SetActive(isSelected);
    }

    public void Deselect()
    {
        if (!isSelected)
        {
            return;
        }
        isSelected = false;
        selectionIndicator.SetActive(isSelected);
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }
    void OnDestroy()
    {
        SelectionManager.selectedObj.Remove(this);
        if (isAIOwned)
        {
            AIManager.Instance.UnregisterAIBuilding(this);
        }
        else
        {
            PlayerManager.Instance.UnregisterBuilding(buildingData);
            PlayerManager.Instance.UnregisterBaseBuilding(this);
        }
    }
}
