using UnityEngine;

[RequireComponent(typeof(TraductorTactico))]
[RequireComponent(typeof(CondicionVictoria))]
public class ManagerEstrategico : MonoBehaviour
{
    [Header("Configuración")]
    public Faction faction;
    public float intervaloActualizacion = 1.5f;

    [Header("Teclas — solo bando Aliado")]
    public KeyCode teclaDefensivo    = KeyCode.Alpha1;
    public KeyCode teclaOfensivo     = KeyCode.Alpha2;
    public KeyCode teclaGuerraTotal  = KeyCode.G;

    public ContextoGrupo Contexto { get; private set; }

    private TraductorTactico traductor;

    private void Start()
    {
        Contexto  = new ContextoGrupo();
        traductor = GetComponent<TraductorTactico>();

        Faction factionEnemiga = faction == Faction.Aliado ? Faction.Enemigo : Faction.Aliado;

        TacticalWaypoint[] waypoints = FindObjectsByType<TacticalWaypoint>(FindObjectsSortMode.None);
        foreach (var wp in waypoints)
        {
            if (wp.faction == faction)
            {
                if (wp.role == WaypointRole.Base)          Contexto.basePropia   = wp.transform;
                if (wp.role == WaypointRole.PuntoRetirada) Contexto.puntoDefensa = wp.transform;
            }
            else if (wp.faction == factionEnemiga)
            {
                if (wp.role == WaypointRole.Base)        Contexto.baseEnemiga    = wp.transform;
                if (wp.role == WaypointRole.PuntoAtaque) Contexto.objetivoAtaque = wp.transform;
            }
        }

        if (Contexto.objetivoAtaque == null) Contexto.objetivoAtaque = Contexto.baseEnemiga;
        if (Contexto.puntoDefensa   == null) Contexto.puntoDefensa   = Contexto.basePropia;

        if (traductor != null)
            traductor.InicializarConContexto(Contexto);

        InvokeRepeating(nameof(ActualizarEstrategia), 0.1f, intervaloActualizacion);
    }

    private void Update()
    {
        if (faction != Faction.Aliado || Contexto.guerraTotal) return;

        if (Input.GetKeyDown(teclaDefensivo))
        {
            Contexto.modo = ModoEstrategico.Defensivo;
            Debug.Log("[Aliados] Modo: DEFENSIVO");
        }
        else if (Input.GetKeyDown(teclaOfensivo))
        {
            Contexto.modo = ModoEstrategico.Ofensivo;
            Debug.Log("[Aliados] Modo: OFENSIVO");
        }
        else if (Input.GetKeyDown(teclaGuerraTotal))
        {
            ActivarGuerraTotal();
        }
    }

    private void ActualizarEstrategia()
    {
        ActualizarInfluencia();

        if (Contexto.guerraTotal)
        {
            Contexto.modo = ModoEstrategico.GuerraTotal;
            return;
        }

        if (faction == Faction.Enemigo && Contexto.basePropia != null)
        {
            float amenazaEnBase = InfluenceMap.Instance != null
                ? InfluenceMap.Instance.GetAlliedInfluence(Contexto.basePropia.position)
                : 0f;
            float propiaEnBase = InfluenceMap.Instance != null
                ? InfluenceMap.Instance.GetEnemyInfluence(Contexto.basePropia.position)
                : 0f;
            Contexto.modo = amenazaEnBase > propiaEnBase
                ? ModoEstrategico.Defensivo
                : ModoEstrategico.Ofensivo;
        }
    }

    private void ActualizarInfluencia()
    {
        if (InfluenceMap.Instance == null || Contexto.basePropia == null) return;

        Vector3 centro = Contexto.baseEnemiga != null
            ? (Contexto.basePropia.position + Contexto.baseEnemiga.position) / 2f
            : Contexto.basePropia.position;

        if (faction == Faction.Aliado)
        {
            Contexto.influenciaPropia  = InfluenceMap.Instance.GetAlliedInfluence(centro);
            Contexto.influenciaEnemiga = InfluenceMap.Instance.GetEnemyInfluence(centro);
        }
        else
        {
            Contexto.influenciaPropia  = InfluenceMap.Instance.GetEnemyInfluence(centro);
            Contexto.influenciaEnemiga = InfluenceMap.Instance.GetAlliedInfluence(centro);
        }
    }

    public void DeclararVictoria()
    {
        Debug.Log($"[VICTORIA] Bando {faction} ha ganado.");
        foreach (var m in FindObjectsByType<ManagerEstrategico>(FindObjectsSortMode.None))
        {
            m.Contexto.guerraTotal = false;
            m.Contexto.modo = ModoEstrategico.Defensivo;
        }
    }

    private void ActivarGuerraTotal()
    {
        foreach (var m in FindObjectsByType<ManagerEstrategico>(FindObjectsSortMode.None))
        {
            m.Contexto.guerraTotal = true;
            m.Contexto.modo = ModoEstrategico.GuerraTotal;
        }
        Debug.Log("[GUERRA TOTAL] ¡Modo Guerra Total activado!");
    }
}
