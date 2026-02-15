using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitButtonUI : MonoBehaviour
{
    [SerializeField] Image iconImage;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] TextMeshProUGUI countdownText;


    Button unitButton;

    UnitSpawner spawner;
    int unitIndex;

    void Awake()
    {
        unitButton = GetComponent<Button>();
    }

    public void Setup(UnitSpawner spawner, int unitIndex, UnitData data)
    {
        this.spawner = spawner;
        this.unitIndex = unitIndex;


        if (data != null && iconImage != null && data.icon != null)
        {
            iconImage.sprite = data.icon;
        }

        unitButton.onClick.RemoveAllListeners();
        unitButton.onClick.AddListener(() => this.spawner.EnqueueUnit(this.unitIndex));
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }
    void UpdateUI()
    {
        int count = spawner.GetQueuedCount(unitIndex);
        float remaining = spawner.GetRemainingTime(unitIndex);

        countText.text = count > 0 ? count.ToString() : "";
        countdownText.text = remaining > 0 ? remaining.ToString("F1") + "s" : "";
    }
}