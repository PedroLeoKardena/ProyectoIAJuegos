using UnityEngine;

// MonoBehaviour que implementa A* con coste táctico basado en el Mapa de Influencia (Millington, cap. 5).
// Fórmula: C = D + w_i × T_i, donde D es el coste base de terreno (distancia/velocidad),
// w_i es la sensibilidad de la unidad al peligro y T_i la influencia enemiga media en la arista.
// Permite al profesor comparar la ruta normal frente a la táctica mediante el toggle useTacticalPathfinding.
[RequireComponent(typeof(TerrainSpeedModifier))]
public class AStarPathfinderInfluence : UnityEngine.MonoBehaviour
{
    [Header("Configuración A*")]
    [SerializeField] private GridManager grid;
    [Tooltip("Transform del destino final del agente.")]
    [SerializeField] private Transform objetivo;
    [SerializeField] private HeuristicType heuristicType = HeuristicType.Euclidean;

    [Header("Pathfinding Táctico")]
    [Tooltip("Activa la penalización táctica. Desactivado = distancia mínima. Activado = evita zonas de influencia enemiga.")]
    [SerializeField] public bool useTacticalPathfinding = true;

    [Header("Pesos de Sensibilidad por Tipo de Unidad (w_i)")]
    [Tooltip("Exploradores: peso alto. Evitan activamente el peligro.")]
    [SerializeField] private float weightExploradores = 2.0f;
    [Tooltip("Infantería Pesada: peso bajo. Son más resistentes y menos evasivos.")]
    [SerializeField] private float weightInfanteriaPesada = 0.3f;
    [Tooltip("Velites / Infantería Ligera: peso moderado.")]
    [SerializeField] private float weightVelites = 1.0f;

    [Tooltip("Tecla para recalcular el camino en tiempo de ejecución (demo al profesor).")]
    [SerializeField] private KeyCode recomputeKey = KeyCode.Space;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;

    // Último camino calculado. Null si no hay camino o aún no se ha calculado.
    public Path CurrentPath { get; private set; }

    private TerrainSpeedModifier _terrainMod;

    private void Start()
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        _terrainMod = GetComponent<TerrainSpeedModifier>();
        ComputePath();
    }

    private void Update()
    {
        // Permite recalcular el camino en tiempo real durante la demo.
        if (Input.GetKeyDown(recomputeKey))
            ComputePath();
    }

    // Calcula el camino desde la posición actual hasta el objetivo configurado.
    // Devuelve el Path resultante y lo almacena en CurrentPath.
    public Path ComputePath()
    {
        if (grid == null || objetivo == null)
        {
            Debug.LogWarning("[AStarPathfinderInfluence] Falta grid u objetivo en el Inspector.");
            return null;
        }

        Node start  = grid.NodeFromWorldPoint(transform.position);
        Node target = grid.NodeFromWorldPoint(objetivo.position);

        AStarAlgorithm.CostProvider provider = BuildCostProvider();
        float hScale = 1f / TerrainSpeedModifier.GetMaxSpeed(_terrainMod.unitType);
        CurrentPath = AStarAlgorithm.FindPath(start, target, grid, provider, heuristicType, hScale);

        if (CurrentPath == null)
            Debug.LogWarning($"[AStarPathfinderInfluence] No se encontró camino. Start=({start?.x},{start?.z}) Target=({target?.x},{target?.z})");
        else
            Debug.Log($"[AStarPathfinderInfluence] Camino encontrado: {CurrentPath.nodes.Length} waypoints. " +
                      $"Táctico={useTacticalPathfinding} | Unidad={_terrainMod.unitType} | Sensibilidad={GetSensitivity(_terrainMod.unitType):F2}");

        return CurrentPath;
    }

    // Construye el CostProvider adecuado según el toggle táctico.
    // Modo normal: coste = distancia / velocidad en ese terreno.
    // Modo táctico: coste = D + w_i × T_i, usando la influencia enemiga media de la arista.
    private AStarAlgorithm.CostProvider BuildCostProvider()
    {
        UnitType unitType   = _terrainMod.unitType;
        float    sensitivity = GetSensitivity(unitType);

        if (useTacticalPathfinding && InfluenceMap.Instance != null)
        {
            return (from, to) =>
            {
                // D: coste base de terreno (tiempo de viaje = distancia / velocidad)
                float baseTerrainCost = UnityEngine.Vector3.Distance(from.worldPosition, to.worldPosition)
                                        / TerrainSpeedModifier.GetSpeed(unitType, to.terrainTag);

                // T_i: promedio de influencia enemiga entre nodo origen y destino
                // (conversión nodo → arista, Millington cap. 5)
                float influenceFrom = InfluenceMap.Instance.GetEnemyInfluence(from);
                float influenceTo   = InfluenceMap.Instance.GetEnemyInfluence(to);
                float avgInfluence  = (influenceFrom + influenceTo) * 0.5f;

                // Penalización táctica: w_i × T_i
                float tacticalPenalty = sensitivity * avgInfluence;
                float total           = baseTerrainCost + tacticalPenalty;

                // Garantía de no negatividad: preserva la admisibilidad del A*
                if (total < 0f)
                {
                    Debug.LogWarning($"[AStarPathfinderInfluence] Coste negativo ({total:F3}) en nodo ({to.x},{to.z}). Forzado a 0.");
                    return 0f;
                }

                return total;
            };
        }

        // Modo sin táctica: solo coste de terreno (ruta de distancia mínima)
        return (from, to) =>
            UnityEngine.Vector3.Distance(from.worldPosition, to.worldPosition)
            / TerrainSpeedModifier.GetSpeed(unitType, to.terrainTag);
    }

    // Devuelve el peso de sensibilidad táctica (w_i) según el tipo de unidad.
    // Exploradores alto, Infantería Pesada bajo, Velites moderado.
    private float GetSensitivity(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Exploradores:     return weightExploradores;
            case UnitType.InfanteriaPesada: return weightInfanteriaPesada;
            case UnitType.Velites:          return weightVelites;
            default:                        return weightVelites;
        }
    }

#if UNITY_EDITOR
    // Dibuja el camino calculado en la vista Scene.
    // Magenta = modo táctico activo, Cian = modo normal (para comparación visual).
    private void OnDrawGizmos()
    {
        if (!drawDebug) return;

        float radius = (grid != null) ? grid.cellSize * 0.4f : 0.5f;

        if (objetivo != null)
        {
            Gizmos.color = UnityEngine.Color.red;
            Gizmos.DrawSphere(objetivo.position, radius);
        }

        if (!Application.isPlaying || grid == null) return;

        Node startNode = grid.NodeFromWorldPoint(transform.position);
        if (startNode != null)
        {
            Gizmos.color = UnityEngine.Color.green;
            Gizmos.DrawSphere(startNode.worldPosition, radius);
        }

        if (CurrentPath == null || CurrentPath.nodes == null) return;

        // Magenta = táctico (ruta curvada), cian = normal (distancia mínima)
        UnityEngine.Color waypointColor = useTacticalPathfinding ? UnityEngine.Color.magenta : UnityEngine.Color.yellow;
        UnityEngine.Color lineColor     = useTacticalPathfinding ? new UnityEngine.Color(1f, 0f, 1f, 0.7f) : UnityEngine.Color.cyan;

        for (int i = 0; i < CurrentPath.nodes.Length; i++)
        {
            Gizmos.color = waypointColor;
            Gizmos.DrawSphere(CurrentPath.nodes[i], grid.cellSize * 0.25f);

            if (i < CurrentPath.nodes.Length - 1)
            {
                Gizmos.color = lineColor;
                Gizmos.DrawLine(CurrentPath.nodes[i], CurrentPath.nodes[i + 1]);
            }
        }
    }
#endif
}
