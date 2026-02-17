using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    public LayerMask selectableLayer;
    public LayerMask groundLayer;
    public LayerMask enemyLayer;

    public GameObject targetMarker;
    public float targetMarkerTime = 0.5f;
    public GameObject targetEnemy;

    public RectTransform selectionBoxUI;
    public float dragThreshold = 5f;

    public static List<ISelectable> selectedObj = new List<ISelectable>();

    Vector2 dragStartPosition;
    bool isDragging;
    bool isSelecting;
    bool isInAttackMode;// if right click should attack enemies

    Camera cam;
    Worker activeWorker;
    Hero activeHero;

    void Awake()
    {
        cam = Camera.main;
    }
    void Update()
    {
        HandleLeftMouseButton();
        if (Mouse.current.rightButton.wasPressedThisFrame) HandleRightMouseButton();
    }

    void HandleLeftMouseButton()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || selectionBoxUI == null) return;
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            if (isInAttackMode)
            {
                HandleAttackModeClick();
                return;
            }
            if (TryHandleResourceClick())
            {
                Debug.Log("Resources Clicked!");
                return;
            }
            if (activeWorker != null && activeWorker.isPlacing)
            {
                ConfirmBuildingAndDeselectWorker();
                return;
            }
            StartDragSelection();
        }
        else if (mouse.leftButton.isPressed && isSelecting)
        {
            UpdateDragSelection();
        }
        else if (mouse.leftButton.wasReleasedThisFrame && isSelecting)
        {
            FinishSelection();
        }

    }
    void StartDragSelection()
    {
        isSelecting = true;
        dragStartPosition = Mouse.current.position.ReadValue();
        selectionBoxUI.sizeDelta = Vector2.zero;
        selectionBoxUI.gameObject.SetActive(true);
        isDragging = false;
    }
    void UpdateDragSelection()
    {
        Vector2 currentMousePos = Mouse.current.position.ReadValue();
        Vector2 dragDelta = currentMousePos - dragStartPosition;
        if (Mathf.Abs(dragDelta.x) > dragThreshold || Mathf.Abs(dragDelta.y) > dragThreshold)
        {
            isDragging = true;
        }
        UpdateSelectionBoxUI(currentMousePos); ;
    }
    void FinishSelection()
    {
        DeselectAll();
        if (isDragging)
        {
            SelectUnitsInBox();
        }
        else
        {
            SelectObjectUnderCursor();// single click na unit
        }
        //UpdateUI();-----------------------------------------------------------------------
        selectionBoxUI.gameObject.SetActive(false);
        isSelecting = false;
    }
    void UpdateSelectionBoxUI(Vector2 currentMousePos)
    {
        float width = currentMousePos.x - dragStartPosition.x;
        float heigth = currentMousePos.y - dragStartPosition.y;

        selectionBoxUI.anchoredPosition = dragStartPosition + new Vector2(width / 2, heigth / 2);
        selectionBoxUI.sizeDelta = new Vector2(Mathf.Abs(width), Mathf.Abs(heigth));
    }
    void SelectUnitsInBox()
    {
        Bounds boxBounds = GetSelectionBoxBounds();

        Collider[] colliders = Physics.OverlapSphere(cam.transform.position, 1000f, selectableLayer);

        foreach (Collider col in colliders)
        {
            Vector2 screenPos = cam.WorldToScreenPoint(col.transform.position);
            if (boxBounds.Contains(screenPos))
            {
                BaseUnit obj = col.GetComponent<BaseUnit>();
                if (obj != null && !selectedObj.Contains(obj))
                {
                    selectedObj.Add(obj);
                    obj.Select();
                }
            }
        }

    }
    void SelectObjectUnderCursor()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, selectableLayer))
        {
            ISelectable obj = hit.collider.GetComponent<ISelectable>();
            if (obj != null)
            {
                selectedObj.Add(obj);
                obj.Select();
            }
            UnitSpawner spawner = hit.collider.GetComponent<UnitSpawner>();
            if (spawner != null)
            {
                SpawnerUIManager.Instance.ShowSpawner(spawner);
            }
        }
    }
    Bounds GetSelectionBoxBounds()
    {
        return new Bounds(selectionBoxUI.anchoredPosition, selectionBoxUI.sizeDelta);
    }
    void HandleRightMouseButton()
    {
        // If placing a building preview, cancel it
        /*if (BuildingPlacementManager.Instance.IsPlacing)
        {
            BuildingPlacementManager.Instance.CancelPlacement();
            return;
        }*/

        List<BaseUnit> movableUnits = new List<BaseUnit>();
        
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, enemyLayer))
        {
            Debug.Log("Enemy");
            foreach (var obj in selectedObj)
            {
                if (obj is CombatUnit combatUnit)
                {
                    combatUnit.StartMoving();
                    if(!movableUnits.Contains(combatUnit))
                    {
                        movableUnits.Add(combatUnit);
                    }
                }
                else if (obj is Hero hero)
                {
                    hero.StartMoving();
                    hero.currentTarget = hit.collider.gameObject;
                    hero.state = CombatState.Chasing;
                    if(!movableUnits.Contains(hero))
                    {
                        movableUnits.Add(hero);
                    }
                }
            }
        }
        else if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            Debug.Log("GROUDN");
            foreach (var obj in selectedObj)
            {
                if (obj is Worker worker)
                {
                    if (worker.CanBeMoved())
                    {
                        if(obj is Builder builder && builder.isPlacing)
                        {
                            builder.CancelConstruction();
                        }
                        movableUnits.Add(worker);
                    }
                }
                else if (obj is CombatUnit combatUnit)
                {
                    combatUnit.StartMoving();
                    if(!movableUnits.Contains(combatUnit))
                    {
                        movableUnits.Add(combatUnit);
                    }
                }
                else if (obj is Hero hero)
                {
                    hero.StartMoving();
                    if(!movableUnits.Contains(hero))
                    {
                        movableUnits.Add(hero);
                    }
                }
            }
        }
        
        

        foreach (BaseUnit unit in movableUnits)
        {
            if (unit != null)
            {
                unit.MoveTo(hit.point);
            }
            else
            {
                selectedObj.Remove(unit);
            }
            targetMarker.SetActive(true);
            targetMarker.transform.position = hit.point;
            StopAllCoroutines();
            StartCoroutine(HideTargetMarker());
        }
        UpdateUI();
    }
    void DeselectAll()
    {
        foreach (ISelectable obj in selectedObj)
        {
            obj.Deselect();
        }
        selectedObj.Clear();
        //BuildingPlacementManager.Instance.HideAllUI();-------------------------------------------------
        //SpawnerUIManager.Instance.HideSpawnerUI();
    }
    IEnumerator HideTargetMarker()
    {
        yield return new WaitForSeconds(targetMarkerTime);
        targetMarker.SetActive(false);
    }
    void UpdateUI()
    {
        activeWorker = null;
        foreach (ISelectable obj in selectedObj)
        {
            if (obj is Builder worker)
            {
                if (worker.currentState == Worker.WorkerState.Idle)
                {
                    BuildingPlacementManager.Instance.ShowBuildingUIBuilder(worker);
                    activeWorker = worker;
                    return;
                }
            }
            /*else if (obj is Hero hero)
            {
                if (hero.currentState == Hero.HeroState.Idle)
                {
                    BuildingPlacementManager.Instance.ShowBuildingUIHero(hero);
                    activeHero = hero;
                    return;
                }
            }*/
        }
        //BuildingPlacementManager.Instance.HideAllUI();-------------------------------------------------------
    }
    void ConfirmBuildingAndDeselectWorker()
    {
        BuildingPlacementManager.Instance.ConfirmPlacement();
        if (activeWorker != null)
        {
            selectedObj.Remove(activeWorker);
            activeWorker.Deselect();
        }
        //UpdateUI();--------------------------------------------------------
    }
    public void EnterAttackMode()
    {
        isInAttackMode = true;
        Debug.Log("Entered Attack Mode");
    }
    void HandleAttackModeClick()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, enemyLayer))
        {
            GameObject enemyTarget = hit.collider.gameObject;
            foreach (var obj in selectedObj)
            {
                if (obj is ICombatUnit combatUnit)
                {
                    combatUnit.ForceAttackTarget(enemyTarget);
                    Debug.Log("Attacking Target: " + enemyTarget.name);
                }
            }
            ShowTargetMarker(enemyTarget.transform.position);
        }
        else if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            Vector3 destination = hit.point;
            foreach (var obj in selectedObj)
            {
                if (obj is ICombatUnit combatUnit)
                {
                    combatUnit.AttackMove(destination);
                }
            }
            ShowTargetMarker(destination);
        }
        isInAttackMode = false;
        Debug.Log("Exited Attack Mode");
    }
    bool TryHandleResourceClick()
    {
        List<Worker> selectedWorkers = new List<Worker>();
        foreach (var obj in selectedObj)
        {
            if (obj is Worker worker && worker.CanBeMoved())
            {
                selectedWorkers.Add(worker);
            }
        }
        if (selectedWorkers.Count == 0)
        {
            return false;
        }
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            GoldResource resource = hit.collider.GetComponent<GoldResource>();
            if (resource != null && resource.HasGold())
            {
                // Assign all selected workers to gather from this resource
                foreach (Looter worker in selectedWorkers)
                {
                    worker.AssignGatheringTask(resource);
                }

                // Show target marker at resource location
                ShowTargetMarker(resource.transform.position);
                Debug.Log("Assigned workers to gather gold from: " + resource.gameObject.name);

                // Deselect all workers after assigning gathering task
                DeselectAllWorkers();

                return true;
            }
        }
        return false;
    }
    void DeselectAllWorkers()
    {
        List<ISelectable> workersToRemove = new List<ISelectable>();

        foreach (var obj in selectedObj)
        {
            if (obj is Worker worker)
            {
                worker.Deselect();
                workersToRemove.Add(obj);
            }
        }

        foreach (var worker in workersToRemove)
        {
            selectedObj.Remove(worker);
        }

        //UpdateUI();------------------------------------------------------
    }
    void ShowTargetMarker(Vector3 position)
    {
        targetMarker.SetActive(true);
        targetMarker.transform.position = position;
        StopAllCoroutines();
        StartCoroutine(HideTargetMarker());
    }
}