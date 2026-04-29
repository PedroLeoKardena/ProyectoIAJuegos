using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Mapa de influencia táctico. Calcula periódicamente la proyección de fuerza
// militar de cada bando sobre el grid y expone una API de consulta.
// Debug: Azul = influencia aliada, Rojo = influencia enemiga, Magenta = zona contestada.
//
// Implementa IMapaTactico para que los consumidores (ComportamientoTactico,
// Minimapa, ...) trabajen contra el contrato y no contra esta clase
// concreta.  Se auto-registra en ServicioMapaTactico al activarse.
public class InfluenceMap : MonoBehaviour, IMapaTactico
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
    [SerializeField] private float I0_InfanteriaPesada = 50f;
    [SerializeField] private float I0_Velites           = 30f;
    [SerializeField] private float I0_Exploradores      = 15f;

    [Header("Debug")]
    [SerializeField] private bool debugMode         = false;
    [SerializeField] private bool showNumericValues = false;
    [SerializeField] private int debugFontSize      = 8;

    private float[,] allied;
    private float[,] enemy;
    private int width;
    private int height;
    private Node[,] nodeCache;

    // Inicializa el singleton. Solo asigna la referencia al gridManager; el grid aún no está listo.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    // Registro/desregistro en el servicio para que los consumidores resuelvan el mapa por interfaz.
    private void OnEnable()  { ServicioMapaTactico.Registrar(this); }
    private void OnDisable() { ServicioMapaTactico.Quitar(this); }

    // Reserva arrays, cachea nodos del grid (ya inicializado por GridManager.Awake) y arranca el refresco.
    private void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("[InfluenceMap] gridManager es null. Asigna el componente en el Inspector.");
            enabled = false;
            return;
        }

        width  = gridManager.width;
        height = gridManager.height;
        allied = new float[width, height];
        enemy  = new float[width, height];

        nodeCache = new Node[width, height];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                nodeCache[x, z] = gridManager.NodeFromWorldPoint(gridManager.GridToWorld(x, z));

        InvokeRepeating(nameof(RefreshMap), 0f, refreshInterval);
    }

    // ===== IMapaTactico + ACCESORES (apartado e y desacoplamiento) =====
    // El bloque siguiente es la implementación EXPLÍCITA del contrato IMapaTactico
    // y el resto son métodos auxiliares que reutilizamos internamente.

    public string Nombre => "Influencia";

    // Ancho del grid (columnas).
    public int Width  => width;
    // Alto del grid (filas).
    public int Height => height;

    // Influencia aliada/enemiga por celda. Validan bounds para cumplir el contrato de IMapaTactico.
    public float ValorAliadoEn(int x, int z)
        => (allied != null && x >= 0 && z >= 0 && x < width && z < height) ? allied[x, z] : 0f;

    public float ValorEnemigoEn(int x, int z)
        => (enemy  != null && x >= 0 && z >= 0 && x < width && z < height) ? enemy[x, z]  : 0f;

    // Devuelve si una celda es transitable (para que el minimapa pinte de un color base las no transitables).
    public bool IsWalkable(int x, int z)
        => nodeCache != null && x >= 0 && z >= 0 && x < width && z < height
           && nodeCache[x, z] != null && nodeCache[x, z].isWalkable;

    // Máximo aliado / enemigo sobre todo el mapa (para normalizar intensidades en el minimapa).
    public float MaxAliado()
    {
        if (allied == null) return 0f;
        float max = 0f;
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                if (allied[x, z] > max) max = allied[x, z];
        return max;
    }

    public float MaxEnemigo()
    {
        if (enemy == null) return 0f;
        float max = 0f;
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                if (enemy[x, z] > max) max = enemy[x, z];
        return max;
    }

    // Implementación de IMapaTactico.Control: balance neto en una posición del mundo.
    // Reutiliza el GetControl(Vector3) original del compañero (delega en él).
    public float Control(Vector3 worldPos) => GetControl(worldPos);

    // Devuelve la influencia aliada acumulada en el nodo dado. Devuelve 0 si es null o fuera del grid.
    public float GetAlliedInfluence(Node node)
    {
        if (node == null || allied == null || node.x < 0 || node.z < 0 || node.x >= width || node.z >= height) return 0f;
        return allied[node.x, node.z];
    }

    // Devuelve la influencia enemiga acumulada en el nodo dado. Devuelve 0 si es null o fuera del grid.
    public float GetEnemyInfluence(Node node)
    {
        if (node == null || enemy == null || node.x < 0 || node.z < 0 || node.x >= width || node.z >= height) return 0f;
        return enemy[node.x, node.z];
    }

    // Devuelve el balance de control (aliado - enemigo) en el nodo dado.
    public float GetControl(Node node) => GetAlliedInfluence(node) - GetEnemyInfluence(node);

    // Devuelve la influencia aliada en la posición del mundo dada.
    public float GetAlliedInfluence(Vector3 worldPos)
    {
        if (gridManager == null) return 0f;
        return GetAlliedInfluence(gridManager.NodeFromWorldPoint(worldPos));
    }

    // Devuelve la influencia enemiga en la posición del mundo dada.
    public float GetEnemyInfluence(Vector3 worldPos)
    {
        if (gridManager == null) return 0f;
        return GetEnemyInfluence(gridManager.NodeFromWorldPoint(worldPos));
    }

    // Devuelve el balance de control en la posición del mundo dada.
    public float GetControl(Vector3 worldPos)
    {
        if (gridManager == null) return 0f;
        return GetControl(gridManager.NodeFromWorldPoint(worldPos));
    }

    // Devuelve I₀ según el UnitType del agente. Usa I0_Exploradores como fallback.
    private float GetI0(Agent unit)
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

    // Acumula la influencia de un grupo de agentes sobre el array target.
    // Fórmula: I_d = I₀ / √(1 + d), con d = distancia euclídea en unidades de mundo.
    private void ProcessUnits(Agent[] units, float[,] target)
    {
        foreach (Agent unit in units)
        {
            Node origin = gridManager.NodeFromWorldPoint(unit.Position);
            if (origin == null) continue;

            float i0 = GetI0(unit);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    Node candidate = nodeCache[x, z];
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
        if (allied == null || enemy == null) return;

        System.Array.Clear(allied, 0, allied.Length);
        System.Array.Clear(enemy,  0, enemy.Length);

        Agent[] allAgents = FindObjectsByType<Agent>(FindObjectsSortMode.None);
        ProcessUnits(System.Array.FindAll(allAgents, a => a.faction == Faction.Aliado),  allied);
        ProcessUnits(System.Array.FindAll(allAgents, a => a.faction == Faction.Enemigo), enemy);
    }

    // Visualiza la influencia de ambos bandos sobre el grid en el Scene View.
    // Azul = aliados (players), Rojo = enemigos (NPCs), Magenta = zona contestada.
    private void OnDrawGizmos()
    {
        if (!debugMode || !Application.isPlaying || allied == null || nodeCache == null || gridManager == null) return;

        // Normalizar cada bando por separado para que ambos sean visibles independientemente.
        float maxAllied = 0.001f;
        float maxEnemy  = 0.001f;
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {
                if (allied[x, z] > maxAllied) maxAllied = allied[x, z];
                if (enemy[x, z]  > maxEnemy)  maxEnemy  = enemy[x, z];
            }

        float cs = gridManager.cellSize;

#if UNITY_EDITOR
        GUIStyle debugStyle = null;
        if (showNumericValues)
        {
            debugStyle = new GUIStyle();
            debugStyle.fontSize = debugFontSize;
            debugStyle.normal.textColor = Color.white;
            debugStyle.alignment = TextAnchor.MiddleCenter;
        }
#endif

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Node node = nodeCache[x, z];
                if (node == null || !node.isWalkable) continue;

                float alliedNorm = Mathf.Clamp01(allied[x, z] / maxAllied);
                float enemyNorm  = Mathf.Clamp01(enemy[x, z]  / maxEnemy);
                float alpha = Mathf.Max(alliedNorm, enemyNorm);
                if (alpha < 0.01f) continue;

                // Canal rojo = enemigos, canal azul = aliados; magenta donde ambos coinciden.
                Color col = new Color(enemyNorm, 0f, alliedNorm, Mathf.Lerp(0.7f, 0.9f, alpha));

                Gizmos.color = col;
                Gizmos.DrawCube(node.worldPosition, new Vector3(cs, 0.2f, cs) * 0.95f);

#if UNITY_EDITOR
                if (showNumericValues && debugStyle != null)
                {
                    float control = allied[x, z] - enemy[x, z];
                    UnityEditor.Handles.Label(node.worldPosition + Vector3.up * 0.2f, control.ToString("F1"), debugStyle);
                }
#endif
            }
        }
    }
}
