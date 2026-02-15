using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpawnerUIManager : MonoBehaviour
{
    public static SpawnerUIManager Instance;

    public GameObject spawnerUIPanel;
    public Transform uiParentTransform;
    public GameObject unitButtonPrefab;

    UnitSpawner selectedSpawner;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowSpawner(UnitSpawner spawner)
    {
        spawnerUIPanel.SetActive(true);
        selectedSpawner = spawner;
        PopulateUnitButtons();

    }
    void PopulateUnitButtons()
    {
        foreach (Transform child in uiParentTransform)
        {
            Destroy(child.gameObject);
        }

        if (selectedSpawner == null)
        {
            return;
        }
        for (int i = 0; i < selectedSpawner.spawnableUnits.Length; i++)
        {
            UnitData data = selectedSpawner.spawnableUnits[i];
            GameObject btn = Instantiate(unitButtonPrefab, uiParentTransform);

            var buttonComp = btn.GetComponent<UnitButtonUI>();
            if (buttonComp != null)
            {

                buttonComp.Setup(selectedSpawner, i, data);
            }
            else
            {
                Image image = btn.GetComponentInChildren<Image>();
                if (image != null)
                {
                    image.sprite = data.icon;
                }
                TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = data.unitName;
                }
                Button button = btn.GetComponent<Button>();
                int index = i;
                if (button != null)
                {
                    button.onClick.AddListener(() => selectedSpawner.EnqueueUnit(index));
                }
            }
        }
    }
    public void HideSpawnerUI()
    {
        spawnerUIPanel.SetActive(false);
        selectedSpawner = null;
    }
}
