using UnityEngine;

// =============================================================================
//  Selección de objetivo basada en Mapa Táctico.
// -----------------------------------------------------------------------------
//  La integración es opcional: basta con añadir este componente a un NPC para 
//  que su ComportamientoTactico empiece a consultar el mapa de influencia al 
//  elegir objetivo. Si NO se añade, el NPC elige "el más cercano".
//
//  Toggle de depuración (apartado k):
//    La tecla I alterna usarInfluencia en runtime.
// =============================================================================
[DisallowMultipleComponent]
public class SelectorObjetivoInfluencia : MonoBehaviour
{
    [Header("Decisión basada en Mapa Táctico")]
    [Tooltip("Si está activo, la elección de objetivo combina distancia y control del mapa táctico.\n" +
             "Si está desactivado, simplemente elige al enemigo más cercano (modo de comparación para defensa).")]
    public bool usarInfluencia = true;

    [Tooltip("Peso del término de cercanía. Cuanto mayor, más se prioriza el enemigo cercano.")]
    public float pesoDistancia = 1.0f;

    [Tooltip("Peso del término de control aliado en la celda del enemigo. Positivo: prefiere enemigos en zona dominada por su bando.")]
    public float pesoControl = 0.6f;

    [Header("Depuración (apartado k)")]
    [Tooltip("Tecla para alternar 'usarInfluencia' en tiempo de ejecución.")]
    public KeyCode teclaToggle = KeyCode.I;

    // ----- Referencias cacheadas en Start -----
    private Agent        miAgente;     // Para conocer Faction propia.
    private IMapaTactico mapaTactico;  // Resuelto vía servicio locator (interfaz, no concreción).

    private void Start()
    {
        miAgente    = GetComponent<Agent>();
        mapaTactico = ServicioMapaTactico.MapaPrimario;
    }

    private void Update()
    {
        if (Input.GetKeyDown(teclaToggle))
        {
            usarInfluencia = !usarInfluencia;
            Debug.Log($"[SelectorObjetivoInfluencia] usarInfluencia = {usarInfluencia}");
        }
    }

    // -----------------------------------------------------------------------
    //  ComportamientoTactico llama a este método si el componente
    //  está presente en el GameObject.
    //
    //  Devuelve el Transform del enemigo elegido o null si no hay candidatos
    //  dentro del radio de visión.
    // -----------------------------------------------------------------------
    public Transform SeleccionarObjetivo(Vector3 origen, float radioVision, string tagEnemigo)
    {
        if (string.IsNullOrEmpty(tagEnemigo)) tagEnemigo = "Enemigo";

        GameObject objByName  = GameObject.Find(tagEnemigo);
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag(tagEnemigo);

        Transform mejor = null;
        float mejorScore = -Mathf.Infinity;
        float distanciaMinima = Mathf.Infinity; // respaldo cuando la influencia esté desactivada

        bool aplicaInfluencia = usarInfluencia && mapaTactico != null;
        float refControl = 1f;
        if (aplicaInfluencia)
            refControl = Mathf.Max(0.001f, Mathf.Max(mapaTactico.MaxAliado(), mapaTactico.MaxEnemigo()));

        foreach (GameObject obj in enemigos)
        {
            if (obj == null) continue;
            float dist = Vector3.Distance(origen, obj.transform.position);

            // Solo evaluamos objetivos dentro del radio de visión: fuera de él
            // ni siquiera "los vemos", así que no entran en la decisión.
            if (dist > radioVision) continue;

            // ---------- Modo "más cercano" (influencia desactivada) ----------
            if (!aplicaInfluencia)
            {
                if (dist < distanciaMinima)
                {
                    distanciaMinima = dist;
                    mejor = obj.transform;
                }
                continue;
            }

            // ---------- Modo con influencia (apartado f activado) ----------
            float termDistancia = 1f - Mathf.Clamp01(dist / radioVision);
            float controlEnEnemigo = mapaTactico.Control(obj.transform.position) / refControl; // ~[-1, 1]

            // El mapa devuelve aliado-enemigo desde el punto de vista del bando "Aliado".
            // Si nuestra unidad es Faction.Enemigo, invertimos el signo para que el
            // término represente "control del bando propio" coherentemente.
            if (miAgente != null && miAgente.faction == Faction.Enemigo)
                controlEnEnemigo = -controlEnEnemigo;

            float score = pesoDistancia * termDistancia + pesoControl * controlEnEnemigo;

            if (score > mejorScore)
            {
                mejorScore = score;
                mejor = obj.transform;
            }
        }

        // Fallback por nombre exacto si no había ninguno con tag dentro del radio.
        if (mejor == null && objByName != null)
        {
            float distName = Vector3.Distance(origen, objByName.transform.position);
            if (distName <= radioVision) mejor = objByName.transform;
        }

        return mejor;
    }
}
