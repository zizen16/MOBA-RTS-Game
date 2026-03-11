using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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

        // --- Your existing mechanics (Unchanged) ---
        var buttonComp = btn.GetComponent<UnitButtonUI>();
        if (buttonComp != null)
        {
            buttonComp.Setup(selectedSpawner, i, data);
        }
        else
        {
            Image image = btn.GetComponentInChildren<Image>();
            if (image != null) image.sprite = data.icon;

            TextMeshProUGUI txt = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = data.unitName;

            Button button = btn.GetComponent<Button>();
            int index = i;
            if (button != null)
            {
                button.onClick.AddListener(() => selectedSpawner.EnqueueUnit(index));
            }
        }

        // --- Animation Add-on (New) ---
        // 1. Prepare button
        btn.transform.localScale = Vector3.zero;
        CanvasGroup canvasGroup = btn.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // 2. Play animation with delay based on index
        // This ensures they appear in the order of the grid
        btn.transform.DOScale(1f, 0.5f)
           .SetDelay(i * 0.1f)
           .SetEase(Ease.OutBack);

        canvasGroup.DOFade(1f, 0.5f)
           .SetDelay(i * 0.1f);
    }
    }
    public void HideSpawnerUI()
    {
        spawnerUIPanel.SetActive(false);
        selectedSpawner = null;
    }

        public void ForceCompleteUIAnimations()
    {
        // The 'true' argument tells DOTween to jump to the end 
        // of the animation instead of just stopping in the middle.
        DOTween.Kill(uiParentTransform, true);
    }
}
