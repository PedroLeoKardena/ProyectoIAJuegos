using UnityEngine;

// MonoBehaviour que configura y ejecuta el algoritmo A* en la escena de Unity.
// Gestiona referencias al grid, selección de heurística y toggle del coste táctico.
// Solo calcula y expone el Path; el seguimiento del camino lo gestiona PathFollowingSinOffset.
public class AStarPathfinder : MonoBehaviour
{
    [Header("Configuración A*")]
    [SerializeField] private GridManager grid;
    [Tooltip("Transform del destino final del agente.")]
    [SerializeField] private Transform objetivo;
    [SerializeField] private HeuristicType heuristicType = HeuristicType.Euclidean;

    [Tooltip("Activa el peso táctico del Influence Map en el cálculo del coste.")]
    [SerializeField] private bool useTacticalCost = false;

    [Tooltip("Tecla para recalcular el camino en tiempo de ejecución (demo al profesor).")]
    [SerializeField] private KeyCode recomputeKey = KeyCode.Space;

    // Controla la visibilidad de los Gizmos de depuración (usado en OnDrawGizmos).
    [Header("Debug")]
    [SerializeField] private bool drawDebug = true;

    // Delegado de coste asignable externamente para el Influence Map futuro.
    // Si es null cuando useTacticalCost == true, se usa el coste base (distancia euclídea).
    public AStarAlgorithm.CostProvider costProvider;

    // Último camino calculado. Null si no hay camino o aún no se ha calculado.
    public Path CurrentPath { get; private set; }

    private void Start()
    {
        // Depende de que GridManager.Awake() haya ejecutado primero (seguro en escena estática).
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
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
            Debug.LogWarning("[AStarPathfinder] Falta grid u objetivo en el Inspector.");
            return null;
        }

        Node start  = grid.NodeFromWorldPoint(transform.position);
        Node target = grid.NodeFromWorldPoint(objetivo.position);

        AStarAlgorithm.CostProvider provider = BuildCostProvider();
        CurrentPath = AStarAlgorithm.FindPath(start, target, grid, provider, heuristicType);

        if (CurrentPath == null)
            Debug.LogWarning($"[AStarPathfinder] No se encontró camino. Start=({start?.x},{start?.z}) Target=({target?.x},{target?.z})");
        else
            Debug.Log($"[AStarPathfinder] Camino encontrado: {CurrentPath.nodes.Length} waypoints. Táctico={useTacticalCost}");

        return CurrentPath;
    }

    // Construye el CostProvider adecuado según el estado del toggle táctico.
    private AStarAlgorithm.CostProvider BuildCostProvider()
    {
        if (useTacticalCost && costProvider != null)
            return costProvider;

        // Coste base: distancia euclídea entre centros de celda.
        return (from, to) => Vector3.Distance(from.worldPosition, to.worldPosition);
    }
}
