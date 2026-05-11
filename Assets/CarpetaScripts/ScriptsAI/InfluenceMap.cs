using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Mapa de influencia táctico basado en inundación (Dijkstra / Map Flooding, Millington cap. 5).
// Calcula periódicamente la proyección de fuerza militar de cada bando expandiendo
// la influencia nodo a nodo a través del grafo del grid, respetando obstáculos.
// Debug: Azul = influencia aliada, Rojo = influencia enemiga, Magenta = zona contestada.
//
// Implementa IMapaTactico para que los consumidores (ComportamientoTactico,
// Minimapa, ...) trabajen contra el contrato y no contra esta clase concreta.
// Se auto-registra en ServicioMapaTactico al activarse.
public class InfluenceMap : MonoBehaviour, IMapaTactico
{
    // Registro de un nodo en la lista abierta del algoritmo de inundación.
    private struct LocationRecord
    {
        public Node  node;
        public float influence; // Fuerza actual en este nodo
    }

    // Heap máximo ordenado por influencia descendente. Evita el coste O(n²) de ordenar una lista.
    private sealed class MaxHeap
    {
        private readonly List<LocationRecord> data = new List<LocationRecord>();

        public int Count => data.Count;

        // Inserta un registro y restaura la propiedad de heap.
        public void Push(LocationRecord r)
        {
            data.Add(r);
            BubbleUp(data.Count - 1);
        }

        // Extrae y devuelve el registro de mayor influencia.
        public LocationRecord Pop()
        {
            LocationRecord top = data[0];
            int last = data.Count - 1;
            data[0] = data[last];
            data.RemoveAt(last);
            if (data.Count > 0) SiftDown(0);
            return top;
        }

        public void Clear() => data.Clear();

        private void BubbleUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (data[parent].influence >= data[i].influence) break;
                Swap(i, parent);
                i = parent;
            }
        }

        private void SiftDown(int i)
        {
            int n = data.Count;
            while (true)
            {
                int largest = i;
                int left    = 2 * i + 1;
                int right   = 2 * i + 2;
                if (left  < n && data[left].influence  > data[largest].influence) largest = left;
                if (right < n && data[right].influence > data[largest].influence) largest = right;
                if (largest == i) break;
                Swap(i, largest);
                i = largest;
            }
        }

        private void Swap(int a, int b)
        {
            LocationRecord tmp = data[a];
            data[a] = data[b];
            data[b] = tmp;
        }
    }

    // Instancia única accesible globalmente.
    public static InfluenceMap Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private GridManager gridManager;

    [Header("Actualización")]
    [Tooltip("Segundos entre refrescos del mapa (0.5 – 2 recomendado).")]
    [SerializeField] private float refreshInterval = 1f;

    [Header("Influencia – Inundación")]
    [Tooltip("Coste de influencia por cada paso de nodo.")]
    [SerializeField] private float decayAmount = 5f;
    [Tooltip("Influencia mínima para seguir propagando. Ramas por debajo se cierran.")]
    [SerializeField] private float influenceThreshold = 0.5f;

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

    // Array de nodos cerrados reutilizado entre refrescos para evitar allocations.
    private bool[,] closed;

    // Heap compartido entre las dos llamadas a FloodFill por refresco.
    private readonly MaxHeap heap = new MaxHeap();

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
        closed = new bool[width, height];

        nodeCache = new Node[width, height];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                nodeCache[x, z] = gridManager.NodeFromWorldPoint(gridManager.GridToWorld(x, z));

        InvokeRepeating(nameof(RefreshMap), 0f, refreshInterval);
    }

    // ===== IMapaTactico + ACCESORES =====

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
    public float Control(Vector3 worldPos) => GetControl(worldPos);

    // Devuelve la influencia aliada acumulada en el nodo dado.
    public float GetAlliedInfluence(Node node)
    {
        if (node == null || allied == null || node.x < 0 || node.z < 0 || node.x >= width || node.z >= height) return 0f;
        return allied[node.x, node.z];
    }

    // Devuelve la influencia enemiga acumulada en el nodo dado.
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

    // Algoritmo de inundación lineal (Millington, Map Flooding / Dijkstra).
    // I_vecino = I_actual - decayAmount/terreno. Regla "highest strength wins".
    private void FloodFill(Agent[] units, float[,] target)
    {
        System.Array.Clear(closed, 0, closed.Length);
        heap.Clear();

        // Inicialización de la frontera: una entrada por unidad con su I₀.
        foreach (Agent unit in units)
        {
            Node origin = gridManager.NodeFromWorldPoint(unit.Position);
            if (origin == null || !origin.isWalkable) continue;

            float i0 = GetI0(unit);

            // Solo encolamos si mejoramos la influencia ya registrada en el origen.
            if (i0 <= target[origin.x, origin.z]) continue;

            target[origin.x, origin.z] = i0;
            heap.Push(new LocationRecord { node = origin, influence = i0 });
        }

        // Expansión por prioridad descendente de influencia.
        while (heap.Count > 0)
        {
            LocationRecord current = heap.Pop();
            Node u = current.node;

            // Lazy deletion: si ya fue cerrado por un camino de mayor influencia, se descarta.
            if (closed[u.x, u.z]) continue;
            closed[u.x, u.z] = true;

            foreach (Node v in gridManager.GetNeighbors(u))
            {
                if (v == null || !v.isWalkable || closed[v.x, v.z]) continue;

                // El terreno modifica el coste del paso: bosque (0.8) cuesta más, llanura (1.2) menos.
                // stepCost = decayAmount / terrainMult → la influencia nunca puede crecer entre nodos.
                float terrainMult  = GetTerrainMultiplier(v.terrainTag);
                float newInfluence = current.influence - decayAmount / terrainMult;

                // Cierre de rama: influencia por debajo del umbral o inferior a la ya registrada.
                if (newInfluence < influenceThreshold) continue;
                if (newInfluence <= target[v.x, v.z]) continue;

                target[v.x, v.z] = newInfluence;
                heap.Push(new LocationRecord { node = v, influence = newInfluence });
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
        FloodFill(System.Array.FindAll(allAgents, a => a.faction == Faction.Aliado),  allied);
        FloodFill(System.Array.FindAll(allAgents, a => a.faction == Faction.Enemigo), enemy);
    }

    // Visualiza la influencia de ambos bandos sobre el grid en el Scene View.
    // Azul = aliados, Rojo = enemigos, Magenta = zona contestada.
    private void OnDrawGizmos()
    {
        if (!debugMode || !Application.isPlaying || allied == null || nodeCache == null || gridManager == null) return;

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
