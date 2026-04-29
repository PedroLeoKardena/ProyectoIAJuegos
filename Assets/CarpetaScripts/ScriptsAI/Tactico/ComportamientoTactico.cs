using UnityEngine;
using TMPro; // Necesario para TextMeshPro

[RequireComponent(typeof(TerrainSpeedModifier))]
public class ComportamientoTactico : MonoBehaviour
{
    public enum EstadoTactico { Reposo, Explorando, Persiguiendo, Huyendo, ManteniendoDistancia }

    [Header("Configuración Táctica")]
    public EstadoTactico estadoActual = EstadoTactico.Reposo;
    [Tooltip("Distancia a la que detectan a un enemigo")]
    public float radioVision = 15f;
    [Tooltip("Distancia a partir de la cual los Velites o Exploradores consideran al enemigo una amenaza inminente")]
    public float radioPeligro = 7f; 
    [Tooltip("Distancia de ataque Cuerpo a Cuerpo")]
    public float radioAtaque = 2.5f;
    public string tagEnemigo = "Enemigo";

    [Header("Patrulla (Apartado J)")]
    [Tooltip("Si se activa, en vez de Reposar o Explorar libremente, seguirá las rutas asignadas al script PathFollowing.")]
    public bool esPatrulla = false;

    [Header("Combate (Apartado C)")]
    public float danoCuerpoCuerpo = 25f;
    public float cooldownAtaque = 1.5f;
    private float tiempoUltimoAtaque = 0f;
    [Tooltip("El prefab esférico del proyectil. Si está vacío, se autocreará una esfera al disparar.")]
    public GameObject prefabProyectil;

    private UnitType tipoUnidad;
    private TextMeshPro textoBocadillo;

    [Header("Color de Bocadillo Frecuente")]
    public Color colorTexto = Color.white;

    // Referencias exactas a steerings (evitando ambigüedades por herencia de Unity)
    private Arrive arrive;
    private Wander wander;
    private Flee flee;
    private Component alignExacto; // Lo guardamos como Component para apagarlo sin casteos conflictivos
    private Component lookExacto;
    private PathFollowingSinOffset patrolSteering;

    // --- Contexto estratégico (asignado por TraductorTactico) ---
    [HideInInspector] public ContextoGrupo contextoGrupo;
    [HideInInspector] public Transform destinoEstrategico;
    private AgentNPC npc;

    private void Start()
    {
        npc = GetComponent<AgentNPC>();
        tipoUnidad = GetComponent<TerrainSpeedModifier>().unitType;

        // Búsqueda nativa de Unity (100% segura para evitar que se queden en null)
        arrive = GetComponent<Arrive>();
        wander = GetComponent<Wander>();
        flee = GetComponent<Flee>();
        patrolSteering = GetComponent<PathFollowingSinOffset>();

        // Rescate manual exclusivo para los que comparten la herencia "Align"
        foreach (var c in GetComponents<SteeringBehaviour>())
        {
            if (c is Align && !(c is Wander) && !(c is Face) && !(c is LookWhereYouGoing)) 
            {
                alignExacto = c; // Es el Align puro
            }
            else if (c is LookWhereYouGoing)
            {
                lookExacto = c;
            }
        }

        // Obligamos a apagar las cosas antes del primer Frame
        SetModoReposo();
        CrearBocadilloVisual();
    }

    private void Update()
    {
        Transform enemigo = BuscarEnemigoCercano();

        // Ejecutar FSM (Máquina de Estados) dependiendo del tipo
        switch (tipoUnidad)
        {
            case UnitType.InfanteriaPesada:
                EjecutarLogicaInfanteria(enemigo);
                break;
            case UnitType.Velites:
                EjecutarLogicaVelite(enemigo);
                break;
            case UnitType.Exploradores:
                EjecutarLogicaExplorador(enemigo);
                break;
        }

        // Si no hemos encontrado enemigo, mostramos visualmente la búsqueda alternativa
        if (enemigo == null && textoBocadillo != null)
        {
            GameObject objByName = GameObject.Find("Enemigo");
            GameObject[] objsByTag = GameObject.FindGameObjectsWithTag("Enemigo");
            textoBocadillo.text += $"\n<size=2>(Dbg: {objsByTag.Length} tags, Nombre={(objByName != null ? "Si" : "No")})</size>";
        }
        else if (enemigo != null && textoBocadillo != null)
        {
            float distStr = Vector3.Distance(transform.position, enemigo.position);
            textoBocadillo.text += $"\n<size=2>(Debug: Enemigo a {distStr:F1}m)</size>";
        }

        // Mantener el texto mirando siempre a cámara
        if (Camera.main != null && textoBocadillo != null)
        {
            textoBocadillo.transform.rotation = Camera.main.transform.rotation;
        }
    }

    // ==========================================
    // LÓGICAS TÁCTICAS ESPECIALIZADAS (Punto A)
    // ==========================================

    private void EjecutarLogicaInfanteria(Transform animadorEnemigo)
    {
        // La Infantería Pesada es lenta pero implacable. Va directa al combate cuerpo a cuerpo.
        if (animadorEnemigo != null)
        {
            float dist = Vector3.Distance(transform.position, animadorEnemigo.position);
            if (dist <= radioVision)
            {
                if (dist <= radioAtaque)
                {
                    CambiarEstado(EstadoTactico.Persiguiendo, "¡TOMA ESTO!");
                    SetModoReposo(); // Frena para pegar
                    
                    // Lógica de Daño Matemático (Apartado C - FAD/FTA/FTD)
                    if (Time.time >= tiempoUltimoAtaque + cooldownAtaque)
                    {
                        SistemaSalud saludEnemigo = animadorEnemigo.GetComponent<SistemaSalud>();
                        if (saludEnemigo != null) 
                        {
                            float danoCalculado = CalculadoraCombate.CalcularDaño(this.gameObject, animadorEnemigo.gameObject);
                            saludEnemigo.RecibirDano(danoCalculado);
                        }
                        tiempoUltimoAtaque = Time.time;
                    }
                }
                else
                {
                    CambiarEstado(EstadoTactico.Persiguiendo, "¡Por la gloria!");
                    SetModoAtaque();
                    if (arrive != null)
                    {
                        arrive.target = animadorEnemigo.GetComponent<Agent>();
                    }
                }
                return;
            }
        }

        // Si no hay enemigo, descansan o patrullan
        LanzarModoBase("Esperando ordenes...");
    }

    private void EjecutarLogicaVelite(Transform enemigo)
    {
        // Los Velites (escaramuzadores) atacan a distancia. Si está muy cerca huyen, si está a media distancia atacan, si nada, patrullan.
        if (enemigo != null)
        {
            float dist = Vector3.Distance(transform.position, enemigo.position);
            if (dist < radioPeligro)
            {
                CambiarEstado(EstadoTactico.Huyendo, "¡Retirada tactica!");
                SetModoHuir();
                if (flee != null)
                {
                    // FIX MÁGICO PARA EL FLEE DE TU CÓDIGO
                    flee.panicDistance = 9999f; // Tu script Flee para en seco si pasa de 5 metros. Anulamos el límite inyectando distancia virtual.
                    flee.target = enemigo.GetComponent<Agent>();
                }
                return;
            }
            else if (dist <= radioVision)
            {
                CambiarEstado(EstadoTactico.ManteniendoDistancia, "¡Lanzando jabalinas!");
                
                SetModoReposo(); // Se frena para disparar
                
                // Rotar físicamente al velite para que mire al enemigo (porque apagamos sus steerings para frenarlo)
                Vector3 destDir = (enemigo.position - transform.position).normalized;
                destDir.y = 0;
                if (destDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(destDir);

                // Lógica de Atacar a Distancia
                if (Time.time >= tiempoUltimoAtaque + cooldownAtaque)
                {
                    DispararProyectil(enemigo.position);
                    tiempoUltimoAtaque = Time.time;
                }
                return;
            }
        }

        LanzarModoBase("Buscando blancos...");
    }

    private void EjecutarLogicaExplorador(Transform enemigo)
    {
        // Exploradores: rápidos, detectan enemigos de lejos pero no combaten.
        if (enemigo != null)
        {
            float dist = Vector3.Distance(transform.position, enemigo.position);
            // Sus sentidos agudizados le hacen siempre huir
            if (dist <= radioVision)
            {
                CambiarEstado(EstadoTactico.Huyendo, "¡Enemigo avistado! Volviendo...");
                SetModoHuir();
                if (flee != null)
                {
                    flee.panicDistance = 9999f; // Forzamos retirada indefinida hasta que la FSM lo saque de aquí
                    flee.target = enemigo.GetComponent<Agent>();
                }
                return;
            }
        }

        LanzarModoBase("Cartografiando el mapa...");
    }

    // ==========================================
    // SISTEMA DE PATRULLA Y COMBATE (Apartados J y C)
    // ==========================================

    private void LanzarModoBase(string gritoReposo)
    {
        if (destinoEstrategico != null && npc != null)
        {
            float dist = Vector3.Distance(transform.position, destinoEstrategico.position);
            if (dist > 2f)
            {
                CambiarEstado(EstadoTactico.Explorando, "Ejecutando orden...");
                npc.SetTarget(destinoEstrategico.position, npc.Orientation);

                // arrive.target = null → Arrive usará TargetFormacion automáticamente
                if (arrive != null) { arrive.target = null; arrive.enabled = true; }
                Activar((MonoBehaviour)lookExacto, true);
                if (TryGetComponent<WallAvoidance>(out var wall)) wall.enabled = true;
                Activar(wander, false);
                Activar(flee, false);
                Activar((MonoBehaviour)alignExacto, false);
                return;
            }
        }

        if (esPatrulla && patrolSteering != null)
        {
            CambiarEstado(EstadoTactico.Explorando, "Patrullando ruta...");
            SetModoPatrulla();
        }
        else
        {
            if (tipoUnidad == UnitType.InfanteriaPesada)
            {
                CambiarEstado(EstadoTactico.Reposo, gritoReposo);
                SetModoReposo();
            }
            else
            {
                CambiarEstado(EstadoTactico.Explorando, gritoReposo);
                SetModoExplorar();
            }
        }
    }

    private void DispararProyectil(Vector3 posicionObjetivo)
    {
        // Elevamos ligeramente para que salga del pecho/mano y no del suelo
        Vector3 puntoDisparo = transform.position + Vector3.up * 1f; 
        
        GameObject proyectilAct;
        if (prefabProyectil == null)
        {
            // Creamos un proyectil de la nada como recurso visual provisional
            proyectilAct = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proyectilAct.transform.position = puntoDisparo;
            proyectilAct.transform.localScale = Vector3.one * 0.3f;
            proyectilAct.AddComponent<Rigidbody>();
            
            SphereCollider sc = proyectilAct.GetComponent<SphereCollider>();
            sc.isTrigger = true; // Para que cuele la función OnTriggerEnter
            
            // Le pintamos de un color llamativo
            Renderer r = proyectilAct.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = Color.yellow;
            
            Proyectil p = proyectilAct.AddComponent<Proyectil>();
            p.tagEnemigo = this.tagEnemigo;
            p.disparador = this.gameObject; // Autoria para el cálculo matemático
        }
        else
        {
            proyectilAct = Instantiate(prefabProyectil, puntoDisparo, Quaternion.identity);
            
            Proyectil p = proyectilAct.GetComponent<Proyectil>();
            if (p != null) p.disparador = this.gameObject;
        }

        // Orientarlo hacia el enemigo para que vuele recto
        proyectilAct.transform.LookAt(posicionObjetivo + Vector3.up * 1f);
    }

    // ==========================================
    // MÉTODOS DE APOYO
    // ==========================================

    private void CambiarEstado(EstadoTactico nuevoEstado, string textoGrito)
    {
        if (estadoActual != nuevoEstado)
        {
            estadoActual = nuevoEstado;
        }
        // Actualizamos siempre por si queremos animarlo el texto
        if (textoBocadillo != null) textoBocadillo.text = textoGrito;
    }

    private Transform BuscarEnemigoCercano()
    {
        GameObject[] enemigos = GameObject.FindGameObjectsWithTag(tagEnemigo);
        
        Transform enemigoCercano = null;
        float distanciaMinima = Mathf.Infinity;

        // Primero evaluamos todo lo que tenga la Tag "Enemigo"
        foreach (GameObject obj in enemigos)
        {
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < distanciaMinima)
            {
                distanciaMinima = dist;
                enemigoCercano = obj.transform;
            }
        }

        return enemigoCercano;
    }

    private void Activar(MonoBehaviour script, bool estado)
    {
        if (script != null) script.enabled = estado;
    }

    private void SetModoPatrulla()
    {
        Activar(wander, false);
        Activar(arrive, false);
        Activar(flee, false);
        
        Activar(patrolSteering, true);
        Activar((MonoBehaviour)lookExacto, true);
        Activar((MonoBehaviour)alignExacto, false);
        
        if (TryGetComponent<WallAvoidance>(out var wall)) wall.enabled = true;
    }

    private void SetModoExplorar()
    {
        Activar(arrive, false);
        Activar(flee, false);
        Activar((MonoBehaviour)lookExacto, false);
        Activar((MonoBehaviour)alignExacto, false);

        Activar(wander, true);
        
        if (TryGetComponent<WallAvoidance>(out var wall)) wall.enabled = true;
    }

    private void SetModoAtaque()
    {
        Activar(wander, false);
        Activar(flee, false);
        Activar((MonoBehaviour)alignExacto, false);
        Activar(patrolSteering, false);

        Activar(arrive, true);
        Activar((MonoBehaviour)lookExacto, true);
        if (TryGetComponent<WallAvoidance>(out var wall)) wall.enabled = true;
    }

    private void SetModoHuir()
    {
        Activar(wander, false);
        Activar(arrive, false);
        Activar((MonoBehaviour)alignExacto, false);
        Activar(patrolSteering, false);

        Activar(flee, true);
        Activar((MonoBehaviour)lookExacto, true);
        if (TryGetComponent<WallAvoidance>(out var wall)) wall.enabled = true;
    }

    private void SetModoReposo()
    {
        Activar(arrive, false);
        Activar(wander, false);
        Activar(flee, false);
        Activar(patrolSteering, false);
        
        Activar((MonoBehaviour)lookExacto, false); // Apagamos LookWhere para que frene
        Activar((MonoBehaviour)alignExacto, false);
    }

    private void CrearBocadilloVisual()
    {
        GameObject textGO = new GameObject("Bocadillo_TMPro");
        textGO.transform.SetParent(this.transform);
        textGO.transform.localPosition = new Vector3(0, 3.5f, 0); // Flotando encima (ajusta la 'y' a la altura de tus cápsulas o personajes)
        
        textoBocadillo = textGO.AddComponent<TextMeshPro>();
        textoBocadillo.alignment = TextAlignmentOptions.Center;
        textoBocadillo.fontSize = 5; // Ajusta si el texto es muy grande
        textoBocadillo.color = colorTexto;
        textoBocadillo.fontStyle = FontStyles.Bold;
        
        // Poner la capa visual por delante, para que resalte.
        textoBocadillo.sortingOrder = 99;
    }
}
