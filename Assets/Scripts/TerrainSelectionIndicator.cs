using UnityEngine;

#if CREST_OCEAN
using Crest;
#endif

public class TerrainSelectionIndicator : MonoBehaviour
{
    [Header("Line Renderer")]
    [SerializeField]
    private LineRenderer circleRenderer;

    [Header("Circle Settings")]
    [SerializeField]
    private float radius = 1.5f;

    [SerializeField]
    private int segments = 24;

    [SerializeField]
    private float heightOffset = 0.05f;

    [Header("Performance")]
    [SerializeField]
    private LayerMask terrainLayer;

    [SerializeField]
    private float updateInterval = 0.1f;

    [SerializeField]
    private float movementThreshold = 0.1f;

    [Header("Visual Settings")]
    [SerializeField]
    private float lineWidth = 0.1f;

    [SerializeField]
    private Color circleColor = Color.green;

    [Header("Crest Ocean Support")]
    [SerializeField]
    private bool useCrestOcean = true;

    [SerializeField]
    private bool useHighestSurface = true;

    private Vector3[] points;
    private Vector3 lastPosition;
    private float nextUpdateTime;
    private Transform unitTransform;
    private bool isActive;

#if CREST_OCEAN
    private SampleHeightHelper[] oceanSamplers;
    private bool isOverWater;
#endif

    private void Awake()
    {
        unitTransform = transform.parent;
        if (unitTransform == null)
        {
            unitTransform = transform;
        }

        points = new Vector3[segments + 1];

#if CREST_OCEAN
        if (useCrestOcean)
        {
            oceanSamplers = new SampleHeightHelper[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                oceanSamplers[i] = new SampleHeightHelper();
            }
        }
#endif

        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        if (circleRenderer == null)
        {
            circleRenderer = GetComponent<LineRenderer>();

            if (circleRenderer == null)
            {
                Debug.LogError($"TerrainSelectionIndicator on {gameObject.name}: No LineRenderer assigned or found!");
                return;
            }
        }

        circleRenderer.positionCount = segments + 1;
        circleRenderer.loop = true;
        circleRenderer.useWorldSpace = true;
        circleRenderer.startWidth = lineWidth;
        circleRenderer.endWidth = lineWidth;

        if (circleRenderer.material != null)
        {
            circleRenderer.startColor = circleColor;
            circleRenderer.endColor = circleColor;
        }

        circleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        circleRenderer.receiveShadows = false;
    }

    private void OnEnable()
    {
        isActive = true;
        nextUpdateTime = 0f;

#if CREST_OCEAN
        if (useCrestOcean && oceanSamplers != null && OceanRenderer.Instance != null)
        {
            InitializeCrestQueries();
        }
#endif

        UpdateIndicatorImmediate();
        lastPosition = GetUnitPosition();
    }

    private void OnDisable()
    {
        isActive = false;
    }

    private void Update()
    {
        if (!isActive || circleRenderer == null)
        {
            return;
        }

#if CREST_OCEAN
        bool crestAvailable = useCrestOcean && oceanSamplers != null && OceanRenderer.Instance != null;

        if (crestAvailable)
        {
            InitializeCrestQueries();
        }
#endif

        Vector3 currentPosition = GetUnitPosition();
        bool hasMoved = Vector3.SqrMagnitude(currentPosition - lastPosition) > movementThreshold * movementThreshold;
        bool timeToUpdate = Time.time >= nextUpdateTime;

#if CREST_OCEAN
        bool needsUpdate = hasMoved || timeToUpdate || (crestAvailable && isOverWater);
#else
        bool needsUpdate = hasMoved || timeToUpdate;
#endif

        if (needsUpdate)
        {
            UpdateIndicatorImmediate();
            lastPosition = currentPosition;
            nextUpdateTime = Time.time + updateInterval;
        }
    }

#if CREST_OCEAN
    private void InitializeCrestQueries()
    {
        Vector3 center = GetUnitPosition();

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
            Vector3 worldPos = center + offset;

            oceanSamplers[i].Init(worldPos, 0f, true);
        }
    }
#endif

    private Vector3 GetUnitPosition()
    {
        return unitTransform != null ? unitTransform.position : transform.position;
    }

    public void UpdateIndicatorImmediate()
    {
        if (circleRenderer == null)
        {
            return;
        }

        Vector3 center = GetUnitPosition();

#if CREST_OCEAN
        bool anyPointOverWater = false;
#endif

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            Vector3 worldPos = center + offset;
            float finalHeight = center.y;

            Vector3 rayStart = worldPos + Vector3.up * 50f;
            bool hasTerrainHit = Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, terrainLayer);
            float terrainHeight = hasTerrainHit ? hit.point.y : float.MinValue;

            float oceanHeight = float.MinValue;
#if CREST_OCEAN
            if (useCrestOcean && oceanSamplers != null && OceanRenderer.Instance != null)
            {
                if (oceanSamplers[i].Sample(out float sampledHeight))
                {
                    oceanHeight = sampledHeight;
                }
            }
#endif

            if (useHighestSurface)
            {
                if (hasTerrainHit && oceanHeight > float.MinValue)
                {
                    finalHeight = Mathf.Max(terrainHeight, oceanHeight);
#if CREST_OCEAN
                    if (oceanHeight >= terrainHeight)
                    {
                        anyPointOverWater = true;
                    }
#endif
                }
                else if (hasTerrainHit)
                {
                    finalHeight = terrainHeight;
                }
                else if (oceanHeight > float.MinValue)
                {
                    finalHeight = oceanHeight;
#if CREST_OCEAN
                    anyPointOverWater = true;
#endif
                }
            }
            else
            {
                if (oceanHeight > float.MinValue)
                {
                    finalHeight = oceanHeight;
#if CREST_OCEAN
                    anyPointOverWater = true;
#endif
                }
                else if (hasTerrainHit)
                {
                    finalHeight = terrainHeight;
                }
            }

            points[i] = new Vector3(worldPos.x, finalHeight + heightOffset, worldPos.z);
        }

#if CREST_OCEAN
        isOverWater = anyPointOverWater;
#endif

        circleRenderer.SetPositions(points);
    }

    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        if (isActive)
        {
            UpdateIndicatorImmediate();
        }
    }

    public void SetColor(Color newColor)
    {
        circleColor = newColor;

        if (circleRenderer != null)
        {
            circleRenderer.startColor = newColor;
            circleRenderer.endColor = newColor;

            if (circleRenderer.material != null)
            {
                circleRenderer.material.color = newColor;
            }
        }
    }

    public void SetLineWidth(float newWidth)
    {
        lineWidth = newWidth;

        if (circleRenderer != null)
        {
            circleRenderer.startWidth = newWidth;
            circleRenderer.endWidth = newWidth;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (circleRenderer != null && Application.isPlaying && points != null && unitTransform != null)
        {
            SetupLineRenderer();
            UpdateIndicatorImmediate();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = circleColor;
        Vector3 center = transform.parent != null ? transform.parent.position : transform.position;

        Vector3 previousPoint = center + new Vector3(radius, 0, 0);
        for (int i = 1; i <= 32; i++)
        {
            float angle = (float)i / 32 * Mathf.PI * 2f;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previousPoint, newPoint);
            previousPoint = newPoint;
        }
    }
#endif
}
