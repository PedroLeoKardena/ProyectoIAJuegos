using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Mapa de influencia táctico. Calcula periódicamente la proyección de fuerza
// militar de cada bando sobre el grid y expone una API de consulta.
public class InfluenceMap : MonoBehaviour
{
    // Instancia única accesible globalmente.
    public static InfluenceMap Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private GridManager gridManager;

    [Header("Actualización")]
    [Tooltip("Segundos entre refrescos del mapa (0.5 – 2 recomendado).")]
    [SerializeField] private float refreshInterval = 1f;

    [Header("Influencia")]
    [Tooltip("Radio máximo de efecto por unidad en unidades de mundo.")]
    [SerializeField] private float influenceRadius = 10f;

    [Header("Modificadores de Terreno")]
    [SerializeField] private float bosqueMultiplier  = 0.8f;
    [SerializeField] private float llanuraMultiplier = 1.2f;
    [SerializeField] private float caminoMultiplier  = 1.0f;

    [Header("Potencia Base (I₀) por Tipo de Unidad")]
    [SerializeField] private float I0_InfanteriaPesada = 15f;
    [SerializeField] private float I0_Velites           = 8f;
    [SerializeField] private float I0_Exploradores      = 5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode         = false;
    [SerializeField] private bool showNumericValues = false;

    private float[,] _allied;
    private float[,] _enemy;
    private int _width;
    private int _height;
    private Node[,] _nodeCache;

    // Inicializa el singleton. Solo asigna la referencia al gridManager; el grid aún no está listo.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    // Reserva arrays, cachea nodos del grid (ya inicializado por GridManager.Awake) y arranca el refresco.
    private void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("[InfluenceMap] gridManager es null. Asigna el componente en el Inspector.");
            enabled = false;
            return;
        }

        _width  = gridManager.width;
        _height = gridManager.height;
        _allied = new float[_width, _height];
        _enemy  = new float[_width, _height];

        _nodeCache = new Node[_width, _height];
        for (int x = 0; x < _width; x++)
            for (int z = 0; z < _height; z++)
                _nodeCache[x, z] = gridManager.NodeFromWorldPoint(gridManager.GridToWorld(x, z));

        InvokeRepeating(nameof(RefreshMap), 0f, refreshInterval);
    }

    // Devuelve I₀ según el UnitType del GameObject. Usa I0_Exploradores como fallback.
    private float GetI0(GameObject unit)
    {
        TerrainSpeedModifier tsm = unit.GetComponent<TerrainSpeedModifier>();
        if (tsm == null) return I0_Exploradores;
        switch (tsm.unitType)
        {
            case UnitType.InfanteriaPesada: return I0_InfanteriaPesada;
            case UnitType.Velites:          return I0_Velites;
            case UnitType.Exploradores:     return I0_Exploradores;
            default:                        return I0_Exploradores;
        }
    }

    // Devuelve el multiplicador de terreno para el terrainTag del nodo candidato.
    private float GetTerrainMultiplier(string terrainTag)
    {
        switch (terrainTag)
        {
            case "Bosque":  return bosqueMultiplier;
            case "Llanura": return llanuraMultiplier;
            case "Camino":  return caminoMultiplier;
            default:        return 1f;
        }
    }

    // Acumula la influencia de un grupo de unidades sobre el array target.
    // Fórmula: I_d = I₀ / √(1 + d), con d = distancia euclídea en unidades de mundo.
    private void ProcessUnits(GameObject[] units, float[,] target)
    {
        foreach (GameObject unit in units)
        {
            Node origin = gridManager.NodeFromWorldPoint(unit.transform.position);
            if (origin == null) continue;

            float i0 = GetI0(unit);

            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _height; z++)
                {
                    Node candidate = _nodeCache[x, z];
                    if (candidate == null) continue;

                    float d = Vector3.Distance(origin.worldPosition, candidate.worldPosition);
                    if (d > influenceRadius) continue;

                    float influence = i0 / Mathf.Sqrt(1f + d);
                    influence *= GetTerrainMultiplier(candidate.terrainTag);
                    target[x, z] += influence;
                }
            }
        }
    }

    // Recalcula toda la influencia del mapa. Llamado periódicamente, nunca en Update.
    private void RefreshMap()
    {
        System.Array.Clear(_allied, 0, _allied.Length);
        System.Array.Clear(_enemy,  0, _enemy.Length);

        ProcessUnits(GameObject.FindGameObjectsWithTag("Aliado"), _allied);
        ProcessUnits(GameObject.FindGameObjectsWithTag("Enemigo"), _enemy);
    }
}
