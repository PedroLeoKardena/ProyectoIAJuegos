using UnityEngine;

// MonoBehaviour que implementa A* con coste táctico basado en el Mapa de Influencia (Millington, cap. 5).
// Fórmula: C = D + w_i × T_i, donde D es el coste base de terreno (distancia/velocidad),
// w_i es la sensibilidad de la unidad al peligro y T_i la influencia enemiga media en la arista.

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

    [Header("Multiplicadores Estratégicos")]
    [Tooltip("Escala de w_i en modo Ofensivo. 0.3 = 30% del valor base; las unidades ignoran parcialmente el peligro y toman rutas directas.")]
    [SerializeField] private float multiplicadorOfensivo = 0.3f;
    [Tooltip("Escala de w_i en modo Defensivo. 2.5 = rutas muy evasivas, rodeando zonas de influencia enemiga aunque la ruta sea más larga.")]
    [SerializeField] private float multiplicadorDefensivo = 2.5f;

    [Tooltip("Tecla para recalcular el camino en tiempo de ejecución (demo al profesor).")]
    [SerializeField] private KeyCode recomputeKey = KeyCode.Space;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = false;

    // Último camino calculado. Null si no hay camino o aún no se ha calculado.
    public Path CurrentPath { get; private set; }

    // Escalonado: cada instancia computa en un frame distinto para evitar picos de CPU.
    private static int _totalInstancias = 0;
    private int  _miIndice;
    private bool _recomputePending = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetContador() { _totalInstancias = 0; }

    public void SetObjetivo(Transform nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;
        if (isActiveAndEnabled) RequestRecompute();
    }
    public void RequestRecompute() { _recomputePending = true; }
    public Transform Objetivo => objetivo;

    private TerrainSpeedModifier _terrainMod;
    private ComportamientoTactico _comportamiento;
    private ModoEstrategico? _ultimoModo;

    private System.Collections.IEnumerator Start()
    {
        // Índice secuencial único: NPC 0 computa en frame 0, NPC 1 en frame 1, etc.
        _miIndice = _totalInstancias++;
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        _terrainMod     = GetComponent<TerrainSpeedModifier>();
        _comportamiento = GetComponent<ComportamientoTactico>();
        yield return null;
        if (objetivo != null) ComputePath();
        _ultimoModo = _comportamiento?.contextoGrupo?.modo;
    }

    private void Update()
    {
        if (Input.GetKeyDown(recomputeKey))
            _recomputePending = true;

        ModoEstrategico? modoActual = _comportamiento?.contextoGrupo?.modo;
        if (modoActual != _ultimoModo)
        {
            _ultimoModo = modoActual;
            if (objetivo != null) _recomputePending = true;
        }

        // Procesa el recompute solo en el frame que le corresponde a esta instancia.
        // Con N instancias: NPC 0 computa en frames 0,N,2N,... NPC 1 en frames 1,N+1,2N+1,...
        int spread = Mathf.Max(_totalInstancias, 1);
        if (_recomputePending && Time.frameCount % spread == _miIndice % spread)
        {
            _recomputePending = false;
            if (objetivo != null) ComputePath();
        }
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
        {
            float sensBase  = GetSensitivity(_terrainMod.unitType);
            float mult      = GetMultiplicadorEstrategico();
            string modo     = _comportamiento?.contextoGrupo?.modo.ToString() ?? "N/A";
            Debug.Log($"[AStarPathfinderInfluence] Camino encontrado: {CurrentPath.nodes.Length} waypoints. " +
                      $"Táctico={useTacticalPathfinding} | Unidad={_terrainMod.unitType} | " +
                      $"Modo={modo} | SensBase={sensBase:F2} | Mult={mult:F2} | SensEfectiva={sensBase * mult:F2}");
        }

        return CurrentPath;
    }

    // Construye el CostProvider adecuado según el toggle táctico y el modo estratégico activo.
    // Modo normal: coste = distancia / velocidad en ese terreno.
    // Modo táctico: coste = D + (w_i × mult_estrategico) × T_i.
    //   Ofensivo → mult=0.2: sensibilidad mínima, rutas directas "camorristas".
    //   Defensivo → mult=2.5: sensibilidad máxima, rutas evasivas aunque más largas.
    //   GuerraTotal → mult=0: sin penalización táctica, camino más corto absoluto.
    private AStarAlgorithm.CostProvider BuildCostProvider()
    {
        UnitType unitType    = _terrainMod.unitType;
        float    sensitivity = GetSensitivity(unitType) * GetMultiplicadorEstrategico();

        if (useTacticalPathfinding && InfluenceMap.Instance != null)
        {
            return (from, to) =>
            {
                // D: coste base de terreno (tiempo de viaje = distancia / velocidad)
                float baseTerrainCost = UnityEngine.Vector3.Distance(from.worldPosition, to.worldPosition)
                                        / TerrainSpeedModifier.GetSpeed(unitType, to.terrainTag);

                // T_i: promedio de influencia enemiga entre nodo origen y destino, normalizado a [0,1].
                // La normalización es necesaria porque I₀ varía por tipo de unidad (15–50),
                // de lo contrario w_i escalaría valores crudos y los pesos perderían su significado.
                float maxEnemy      = InfluenceMap.Instance.MaxEnemigo();
                float influenceFrom = InfluenceMap.Instance.GetEnemyInfluence(from);
                float influenceTo   = InfluenceMap.Instance.GetEnemyInfluence(to);
                float avgInfluence  = (maxEnemy > 0f)
                                      ? (influenceFrom + influenceTo) * 0.5f / maxEnemy
                                      : 0f;

                // Penalización táctica: w_i × T_i  (T_i ∈ [0,1])
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

    // Devuelve el multiplicador estratégico que escala w_i según el modo de combate activo.
    // GuerraTotal=0 (ruta más corta sin evasión), Ofensivo=0.2 (ignora parcialmente el peligro),
    // Defensivo=2.5 (evasión máxima, rodea zonas de influencia enemiga).
    // Si no hay ContextoGrupo asignado, devuelve 1 (comportamiento neutro).
    private float GetMultiplicadorEstrategico()
    {
        if (_comportamiento == null || _comportamiento.contextoGrupo == null)
            return 1f;

        return _comportamiento.contextoGrupo.modo switch
        {
            ModoEstrategico.GuerraTotal => 0f,
            ModoEstrategico.Ofensivo    => multiplicadorOfensivo,
            ModoEstrategico.Defensivo   => multiplicadorDefensivo,
            _                           => 1f
        };
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
