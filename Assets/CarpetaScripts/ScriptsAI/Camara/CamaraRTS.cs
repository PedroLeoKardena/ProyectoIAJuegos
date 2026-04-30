using UnityEngine;

// =============================================================================
//  CamaraRTS — cámara estilo Real-Time Strategy.
// -----------------------------------------------------------------------------
//  - Pan con WASD y flechas (usa los ejes "Horizontal"/"Vertical" de Unity, que
//    por defecto incluyen ambos juegos de teclas).
//  - Aceleración con Shift mantenido.
//  - Zoom in/out con la rueda del ratón (mueve la cámara a lo largo de su
//    propio "forward", con clamp en altura para no atravesar el suelo ni
//    salirse al cielo).
//  - R / Home: reset a la pose inicial.
//  - Clamp horizontal opcional al BoundingBox del mapa para que la cámara no
//    se aleje del terreno y la pierdas.
// =============================================================================
public class CamaraRTS : MonoBehaviour
{
    [Header("Pan (WASD + flechas)")]
    [Tooltip("Velocidad base de desplazamiento, en unidades de mundo por segundo.")]
    public float panSpeed = 30f;
    [Tooltip("Velocidad mientras mantienes Shift.")]
    public float panSpeedRapida = 70f;

    [Header("Zoom (rueda del ratón)")]
    [Tooltip("Cuánto avanza/retrocede la cámara por unidad de scroll.")]
    public float zoomSpeed = 80f;
    [Tooltip("Altura mínima (Y) a la que la cámara no se acerca más.")]
    public float alturaMin = 8f;
    [Tooltip("Altura máxima (Y) a la que la cámara no se aleja más.")]
    public float alturaMax = 80f;

    [Header("Reset")]
    [Tooltip("Tecla principal para resetear posición y rotación a las iniciales.")]
    public KeyCode teclaReset = KeyCode.R;

    [Header("Clamp al mapa")]
    [Tooltip("Si está activo, la cámara nunca podrá ver fuera del rectángulo definido en mapBounds.")]
    public bool limitarAlMapa = true;
    [Tooltip("Si está activo, el clamp tiene en cuenta el ÁREA VISIBLE de la cámara según el zoom.\n" +
             "Cuando es false, solo se limita la POSICIÓN (modo simple, más rápido pero deja ver bordes).")]
    public bool clampPorAreaVisible = true;
    [Tooltip("Centro y tamaño del rectángulo del mapa (XZ). Solo se usan X y Z; Y se ignora.\n" +
             "Para tu Escenario_General: center=(30,0,0), size=(240,0,80).")]
    public Bounds mapBounds = new Bounds(new Vector3(30f, 0f, 0f), new Vector3(240f, 0f, 80f));

    // Pose inicial guardada en Start para el reset.
    private Vector3    posInicial;
    private Quaternion rotInicial;
    private Camera     camCache;

    private void Start()
    {
        posInicial = transform.position;
        rotInicial = transform.rotation;
        camCache   = GetComponent<Camera>();
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool rapido = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float velocidad = rapido ? panSpeedRapida : panSpeed;

        Vector3 delta = (Vector3.right * h + Vector3.forward * v) * velocidad * Time.deltaTime;
        transform.position += delta;

        // Zoom con la rueda del ratón.
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            transform.position += transform.forward * scroll * zoomSpeed;
        }

        // Limitar la altura para que el zoom no atraviese el suelo ni se vaya al espacio.
        Vector3 p = transform.position;
        p.y = Mathf.Clamp(p.y, alturaMin, alturaMax);
        transform.position = p;

        // Limitar XZ al rectángulo del mapa.
        if (limitarAlMapa)
        {
            if (clampPorAreaVisible && camCache != null) ClampPorAreaVisible();
            else                                          ClampPorPosicion();
        }

        // Reset.
        if (Input.GetKeyDown(teclaReset) || Input.GetKeyDown(KeyCode.Home))
        {
            transform.position = posInicial;
            transform.rotation = rotInicial;
        }
    }

    // ----------------------------------------------------------------------
    //  Clamp simple: solo limita la POSICIÓN del transform a mapBounds.
    //  Es rápido, pero permite ver bordes/vacío cuando la cámara está
    //  pegada al borde y haces zoom-out.
    // ----------------------------------------------------------------------
    private void ClampPorPosicion()
    {
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, mapBounds.min.x, mapBounds.max.x);
        p.z = Mathf.Clamp(p.z, mapBounds.min.z, mapBounds.max.z);
        transform.position = p;
    }

    // ----------------------------------------------------------------------
    //  Clamp avanzado: calcula los 4 puntos donde el frustum de la cámara
    //  toca el plano del suelo (Y=0), saca el rectángulo (AABB) que esos
    //  4 puntos abarcan en XZ, y desplaza la cámara lo justo para que ese
    //  rectángulo quede dentro de mapBounds. Si la cámara ve más mapa del
    //  que hay (zoom out extremo), la centra automáticamente.
    //
    //  Funciona tanto para cámaras cenitales como inclinadas, y se adapta
    //  al zoom: cuanto más alto, más se restringe el desplazamiento lateral.
    // ----------------------------------------------------------------------
    private void ClampPorAreaVisible()
    {
        Plane suelo = new Plane(Vector3.up, Vector3.zero);

        // Lanzamos rayos por las 4 esquinas del viewport e intersectamos con el suelo.
        Vector2[] esquinas = {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 1f), new Vector2(1f, 1f)
        };
        Vector3 visMin = new Vector3( Mathf.Infinity, 0f,  Mathf.Infinity);
        Vector3 visMax = new Vector3(-Mathf.Infinity, 0f, -Mathf.Infinity);

        for (int i = 0; i < 4; i++)
        {
            Ray r = camCache.ViewportPointToRay(new Vector3(esquinas[i].x, esquinas[i].y, 0f));
            if (!suelo.Raycast(r, out float t)) return; // raro, ignora este frame
            Vector3 hit = r.GetPoint(t);
            if (hit.x < visMin.x) visMin.x = hit.x;
            if (hit.z < visMin.z) visMin.z = hit.z;
            if (hit.x > visMax.x) visMax.x = hit.x;
            if (hit.z > visMax.z) visMax.z = hit.z;
        }

        Vector3 shift = Vector3.zero;
        float visW = visMax.x - visMin.x;
        float visH = visMax.z - visMin.z;

        // Eje X
        if (visW >= mapBounds.size.x)
        {
            // El área visible es más ancha que el mapa: centrar.
            float centroVis = (visMin.x + visMax.x) * 0.5f;
            shift.x = mapBounds.center.x - centroVis;
        }
        else if (visMin.x < mapBounds.min.x) shift.x = mapBounds.min.x - visMin.x;
        else if (visMax.x > mapBounds.max.x) shift.x = mapBounds.max.x - visMax.x;

        // Eje Z
        if (visH >= mapBounds.size.z)
        {
            float centroVis = (visMin.z + visMax.z) * 0.5f;
            shift.z = mapBounds.center.z - centroVis;
        }
        else if (visMin.z < mapBounds.min.z) shift.z = mapBounds.min.z - visMin.z;
        else if (visMax.z > mapBounds.max.z) shift.z = mapBounds.max.z - visMax.z;

        if (shift.sqrMagnitude > 0f) transform.position += shift;
    }

    // Auto-ayuda en el editor: dibuja el rectángulo de límite del mapa.
    private void OnDrawGizmosSelected()
    {
        if (!limitarAlMapa) return;
        Gizmos.color = new Color(1f, 0.7f, 0.1f, 0.6f);
        Vector3 c = mapBounds.center;
        Vector3 s = new Vector3(mapBounds.size.x, 0.1f, mapBounds.size.z);
        Gizmos.DrawWireCube(new Vector3(c.x, posInicial.y, c.z), s);
    }
}
