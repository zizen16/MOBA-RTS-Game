using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public abstract class BaseUnit : MonoBehaviour, IMovable, ISelectable, IDamageable
{
    public bool isEnemyUnit = false;
    //[SerializeField] public UnitData unitData;

    [SerializeField] GameObject selectionIndicator;
    [SerializeField] Canvas healthBarCanvas;
    [SerializeField] Slider healthBarSlider;
    [SerializeField] Image healthBarFill;
    [SerializeField] Vector3 healthBarOffset = new Vector3(0, 3, 0);

    public bool hidehealthBarWhenFull;
    //setting
    [Header("Unit Stats")]
    public float maxHealth = 500f;
    float currentHealth;
    public float maxSpeed;
    float currentSpeed;
    public float maxLevel;
    float currentLevel;
    public float maxExp;
    float currentExp;

    
    Camera mainCamera;
    bool isSelected;
    protected NavMeshAgent agent;

    void Awake()
    {
        mainCamera = Camera.main;
        agent = GetComponent<NavMeshAgent>();
    }
    void Start()
    {
        //maxHealth = unitData.maxHealth;
        currentHealth = maxHealth;
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
    public void MoveTo(Vector3 position)
    {
        agent.SetDestination(position);
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
            Collider collider = GetComponent<Collider>();
            collider.enabled = false;
            Destroy(gameObject);
        }
    }
    void OnDestroy()
    {
        SelectionManager.selectedObj.Remove(this);
        if (!isEnemyUnit)
        {
            //PlayerManager.Instance.UnregisterUnit(unitData);
        }
    }
}