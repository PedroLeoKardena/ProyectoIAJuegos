using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// =============================================================================
//  Mapa táctico de Visibilidad / Exposición.
// -----------------------------------------------------------------------------
//  Para cada celda transitable, lanzamos N rayos en ángulos fijos (cada
//  360/N grados) desde la altura de un personaje. Cada rayo se dispara con una longitud máxima
//  (rangoMaximo) y mide cuánto consigue avanzar antes de chocar con un
//  obstáculo.  La VISIBILIDAD de la celda se define como la LONGITUD MEDIA
//  de los N rayos, normalizada por rangoMaximo (queda en [0, 1]):
//      0 = celda muy cubierta (todos los rayos chocan rápido)
//      1 = celda muy expuesta (todos los rayos llegan al límite sin chocar)
//
//  Es un mapa ESTÁTICO (sólo depende de la geometría de obstáculos): se
//  calcula una sola vez al iniciar la escena.  Si los obstáculos se mueven
//  en runtime se puede llamar a Recalcular() manualmente.
//
//  Implementa IMapaTactico para que el Minimapa y otros consumidores lo
//  traten como cualquier otro mapa táctico.  Como la visibilidad es de
//  un solo canal (no es "aliado vs enemigo"), expongo el mismo valor en
//  ValorAliadoEn y ValorEnemigoEn.
// =============================================================================
public class VisibilityMap : MonoBehaviour, IMapaTactico
{
    [Header("Referencias")]
    [SerializeField] private GridManager gridManager;
    [Tooltip("LayerMask de obstáculos contra los que el rayo de LOS choca.")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Cálculo")]
    [Tooltip("Número de rayos disparados desde cada celda. Más = más preciso pero más caro.")]
    [Range(4, 32)]
    [SerializeField] private int numRayos = 12;
    [Tooltip("Longitud máxima de cada rayo. Mayor = la visibilidad alcanza más lejos.")]
    [SerializeField] private float rangoMaximo = 30f;
    [Tooltip("Altura de los 'ojos' del personaje sobre el centro de la celda.")]
    [SerializeField] private float alturaOjos = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // ----- Datos del mapa (cache) -----
    private float[,] visibilidad;     // ~[0, 1]: 0 = cubierto, 1 = totalmente expuesto.
    private int width, height;
    private Node[,] nodeCache;
    private float maxValor = 1f;      // ya normalizado en [0,1], no debería cambiar mucho.

    // ----- IMapaTactico -----
    public string Nombre => "Visibilidad";
    public int Width  => width;
    public int Height => height;

    public bool IsWalkable(int x, int z)
        => nodeCache != null && x >= 0 && z >= 0 && x < width && z < height
           && nodeCache[x, z] != null && nodeCache[x, z].isWalkable;

    // Visibilidad es mono-canal: el valor "aliado" y "enemigo" coinciden y
    // representan lo mismo (la exposición de la celda).
    public float ValorAliadoEn (int x, int z) => Get(x, z);
    public float ValorEnemigoEn(int x, int z) => Get(x, z);
    public float MaxAliado () => maxValor;
    public float MaxEnemigo() => maxValor;

    // Control = 0 (no aplica el concepto de balance entre bandos).
    public float Control(Vector3 worldPos) => 0f;

    private float Get(int x, int z)
        => (visibilidad != null && x >= 0 && z >= 0 && x < width && z < height)
           ? visibilidad[x, z] : 0f;

    // ----- Ciclo de vida -----
    private void OnEnable()  { ServicioMapaTactico.Registrar(this); }
    private void OnDisable() { ServicioMapaTactico.Quitar(this); }

    private void Awake()
    {
        if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
    }

    private void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError("[VisibilityMap] gridManager es null. Asigna el componente en el Inspector.");
            enabled = false;
            return;
        }

        Recalcular();
    }

    // ----- Cálculo principal -----
    public void Recalcular()
    {
        width  = gridManager.width;
        height = gridManager.height;
        visibilidad = new float[width, height];
        nodeCache   = new Node[width, height];

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                nodeCache[x, z] = gridManager.NodeFromWorldPoint(gridManager.GridToWorld(x, z));

        // Pre-cálculo de las direcciones (vectores horizontales, equiespaciadas).
        Vector3[] direcciones = new Vector3[numRayos];
        for (int i = 0; i < numRayos; i++)
        {
            float angRad = i * (Mathf.PI * 2f / numRayos);
            direcciones[i] = new Vector3(Mathf.Cos(angRad), 0f, Mathf.Sin(angRad));
        }

        // Para cada celda transitable, lanzamos los rayos y calculamos la longitud media.
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Node n = nodeCache[x, z];
                if (n == null || !n.isWalkable) { visibilidad[x, z] = 0f; continue; }

                Vector3 origen = n.worldPosition + Vector3.up * alturaOjos;
                float sumaLong = 0f;

                for (int i = 0; i < numRayos; i++)
                {
                    if (Physics.Raycast(origen, direcciones[i], out RaycastHit hit, rangoMaximo, obstacleLayer))
                    {
                        sumaLong += hit.distance;
                    }
                    else
                    {
                        sumaLong += rangoMaximo;  // sin obstáculo en rango → llegamos al límite
                    }
                }

                // Promedio normalizado al rango máximo. ~1 = celda expuesta total.
                visibilidad[x, z] = (sumaLong / numRayos) / rangoMaximo;
            }
        }
        maxValor = 1f;

        Debug.Log($"[VisibilityMap] Calculado mapa {width}×{height} con {numRayos} rayos.");
    }

    // ----- Debug visual en Scene View -----
    private void OnDrawGizmos()
    {
        if (!debugMode || !Application.isPlaying || visibilidad == null || nodeCache == null) return;

        float cs = gridManager.cellSize;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Node n = nodeCache[x, z];
                if (n == null || !n.isWalkable) continue;

                float v = visibilidad[x, z] / maxValor;
                Gizmos.color = new Color(v, v, 0f, 0.5f);   // amarillo: oscuro = oculto, claro = expuesto
                Gizmos.DrawCube(n.worldPosition, new Vector3(cs, 0.2f, cs) * 0.95f);
            }
        }
    }
}
