using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEditor;
using UnityEngine.InputSystem;

public class BuildingPlacementManager : MonoBehaviour
{
    public static BuildingPlacementManager Instance;

    [Header("Placement elements")]
    public LayerMask placementLayerMask;
    [SerializeField] GameObject buildingButtonPrefab;
    [SerializeField] Transform uiParentTransform;

    [Space]
    [Header("Building Placement Offsets")]
    
    public LayerMask buildingLayer;
    [SerializeField] Material validPlacementMaterial;
    [SerializeField] Material invalidPlacementMaterial;
    Renderer[] previewRenderers;

    Camera mainCamera;
    GameObject buildingPreviewInstance;
    BuildingData currentBuildingData;
    Builder selectedBuilder;

    public bool IsPlacing => buildingPreviewInstance != null;

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
        mainCamera = Camera.main;
    }
    void Update()
    {
        if (buildingPreviewInstance != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementLayerMask))
            {
                buildingPreviewInstance.transform.position = CalculateGroundPosition(hit.point, buildingPreviewInstance);
                UpdatePreviewMaterial();
            }
        }
    }

    public void ShowBuildingUIBuilder(Builder builder)
    {
        selectedBuilder = builder;
        uiParentTransform.gameObject.SetActive(true);
        PopulateBuildableBuildingsBuilder(builder);
    }
    void PopulateBuildableBuildingsBuilder(Builder builder)
    {
        foreach (Transform child in uiParentTransform)
        {
            Destroy(child.gameObject);
        }
        foreach (BuildingData data in builder.buildableBuildingData)
        {
            GameObject btn = Instantiate(buildingButtonPrefab, uiParentTransform);
            btn.GetComponentInChildren<Image>().sprite = data.icon;
            btn.GetComponentInChildren<TextMeshProUGUI>().text = data.buildingName;

            bool canBuild = PlayerManager.Instance.CanBuild(data);
            btn.GetComponent<Button>().interactable = canBuild;
            btn.GetComponent<Button>().onClick.AddListener(() => StartPlacement(data));
        }
    }
    void StartPlacement(BuildingData data)
    {
        if (selectedBuilder != null)
        {
            CancelPlacement();
            currentBuildingData = data;
            buildingPreviewInstance = Instantiate(currentBuildingData.constructionPrefab);
            previewRenderers = buildingPreviewInstance.GetComponentsInChildren<Renderer>();
            selectedBuilder.isPlacing = true;
        }
    }
    public void ConfirmPlacement()
    {
        if (buildingPreviewInstance == null || currentBuildingData == null || selectedBuilder == null) return;
        if (IsOverlapping())
        {
            Debug.Log("Invalid placement location.");
            return;
        }
        Vector3 buildLocation = buildingPreviewInstance.transform.position;
        //assign a builder
        selectedBuilder.AssignBuildingTask(buildLocation, currentBuildingData, buildingPreviewInstance);

        buildingPreviewInstance = null;
        currentBuildingData = null;
    }
    public void HideAllUI()
    {
        CancelPlacement();
        uiParentTransform.gameObject.SetActive(false);
        selectedBuilder = null;
    }
    public void CancelPlacement()
    {
        if (buildingPreviewInstance != null)
        {
            Destroy(buildingPreviewInstance);
            buildingPreviewInstance = null;
        }
        if (selectedBuilder != null)
        {
            selectedBuilder.isPlacing = false;
        }
        currentBuildingData = null;
    }
    public Vector3 CalculateGroundPosition(Vector3 position, GameObject obj)
    {
        Vector3 rayStart = position + Vector3.up * 100f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 200f, placementLayerMask))
        {
            float heightOffset = CalculcateHeaghtOffset(obj);
            return new Vector3(hit.point.x, hit.point.y + heightOffset, hit.point.z);
        }
        return position;
    }
    public float CalculcateHeaghtOffset(GameObject prefab)
    {
        BoxCollider boxCollider = prefab.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            Vector3 scale = prefab.transform.localScale;
            float colliderBottom = (boxCollider.center.y - boxCollider.size.y / 2) * scale.y;
            return -colliderBottom;
        }
        return 0f;
    }

    bool IsOverlapping()
    {
        BoxCollider collider = buildingPreviewInstance.GetComponent<BoxCollider>();
        if (collider == null) return false;

        Vector3 center = buildingPreviewInstance.transform.TransformPoint(collider.center);
        Vector3 halfsize = Vector3.Scale(collider.size * 0.475f, buildingPreviewInstance.transform.lossyScale);

        Collider[] overlaps = Physics.OverlapBox(center, halfsize, buildingPreviewInstance.transform.rotation, buildingLayer);

        foreach (Collider col in overlaps)
        {
            if (col.gameObject != buildingPreviewInstance
            && !col.transform.IsChildOf(buildingPreviewInstance.transform))
            {
                return true;
            }
        }

        return false;
    }
    void UpdatePreviewMaterial()
    {
        Material mat = IsOverlapping() ? invalidPlacementMaterial : validPlacementMaterial;

        foreach (Renderer rend in previewRenderers)
        {
            rend.material = mat;
        }
    }
}
