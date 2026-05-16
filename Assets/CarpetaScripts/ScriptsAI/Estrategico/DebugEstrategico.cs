using UnityEngine;

public class DebugEstrategico : MonoBehaviour
{
    private bool activo = false;
    private bool tacticoActivo = true;
    private ManagerEstrategico[] managers;
    private CondicionVictoria[]  condiciones;
    private Material _glMat;

    private TacticalWaypoint[]         _waypoints;
    private AgentNPC[]                 _npcs;
    private AStarPathfinderInfluence[] _pathfinders;

    public void ToggleActivo() { activo = !activo; }
    public bool EsActivo        => activo;
    public bool EsTacticoActivo => tacticoActivo;

    private void Start()
    {
        managers      = FindObjectsByType<ManagerEstrategico>(FindObjectsSortMode.None);
        condiciones   = FindObjectsByType<CondicionVictoria>(FindObjectsSortMode.None);
        _waypoints    = FindObjectsByType<TacticalWaypoint>(FindObjectsSortMode.None);
        _npcs         = FindObjectsByType<AgentNPC>(FindObjectsSortMode.None);
        _pathfinders  = FindObjectsByType<AStarPathfinderInfluence>(FindObjectsSortMode.None);
    }

    private float _tiempoRefrescoCaché = 0f;
    private const float INTERVALO_REFRESCO = 3f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) activo = !activo;
        if (Input.GetKeyDown(KeyCode.T))  ToggleTacticalPathfinding();

        _tiempoRefrescoCaché += Time.deltaTime;
        if (_tiempoRefrescoCaché >= INTERVALO_REFRESCO)
        {
            _tiempoRefrescoCaché = 0f;
            _npcs        = FindObjectsByType<AgentNPC>(FindObjectsSortMode.None);
            _pathfinders = FindObjectsByType<AStarPathfinderInfluence>(FindObjectsSortMode.None);
        }
    }

    public void ToggleTacticalPathfinding()
    {
        tacticoActivo = !tacticoActivo;
        foreach (var pf in FindObjectsByType<AStarPathfinderInfluence>(FindObjectsSortMode.None))
        {
            pf.useTacticalPathfinding = tacticoActivo;
            pf.ComputePath();
        }
        Debug.Log($"[Debug] Pathfinding táctico: {(tacticoActivo ? "ON" : "OFF")}");
    }

    // GL rendering — visible en Game view sin necesitar el botón Gizmos
    private void OnRenderObject()
    {
        if (!activo || !Application.isPlaying) return;

        EnsureMaterial();
        _glMat.SetPass(0);

        GL.PushMatrix();
        GL.Begin(GL.LINES);

        // Waypoints: haz vertical de 10 unidades + cruz en la cima
        foreach (var wp in _waypoints)
        {
            Color c = wp.role switch
            {
                WaypointRole.Base when wp.faction == Faction.Aliado => Color.green,
                WaypointRole.Base                                   => Color.red,
                WaypointRole.PasoEstrategico                       => Color.yellow,
                _                                                   => Color.cyan
            };
            GL.Color(c);
            Vector3 bot = wp.transform.position;
            Vector3 top = bot + Vector3.up * 10f;
            GL.Vertex(bot); GL.Vertex(top);
            GL.Vertex(top + Vector3.left * 1.5f);  GL.Vertex(top + Vector3.right * 1.5f);
            GL.Vertex(top + Vector3.forward * 1.5f); GL.Vertex(top + Vector3.back * 1.5f);
        }

        // Destinos estratégicos comentados temporalmente para no confundir con caminos A*
        // foreach (var npc in FindObjectsByType<AgentNPC>(FindObjectsSortMode.None))
        // {
        //     if (npc.faction == Faction.Neutro) continue;
        //     var ct = npc.GetComponent<ComportamientoTactico>();
        //     if (ct == null || ct.destinoEstrategico == null) continue;
        //     GL.Color(Color.yellow);
        //     GL.Vertex(npc.Position + Vector3.up * 0.5f);
        //     GL.Vertex(ct.destinoEstrategico.position + Vector3.up * 0.5f);
        // }

        // Caminos de pathfinding: magenta=táctico, cian=distancia mínima
        foreach (var pf in _pathfinders)
        {
            if (pf == null || pf.CurrentPath?.nodes == null) continue;
            var npc = pf.GetComponent<AgentNPC>();
            if (npc == null || npc.faction == Faction.Neutro) continue;

            GL.Color(pf.useTacticalPathfinding ? Color.magenta : Color.cyan);
            for (int i = 0; i < pf.CurrentPath.nodes.Length - 1; i++)
            {
                GL.Vertex(pf.CurrentPath.nodes[i]     + Vector3.up * 0.3f);
                GL.Vertex(pf.CurrentPath.nodes[i + 1] + Vector3.up * 0.3f);
            }
        }

        GL.End();
        GL.PopMatrix();
    }

    private void EnsureMaterial()
    {
        if (_glMat != null) return;
        _glMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        _glMat.hideFlags = HideFlags.HideAndDontSave;
        _glMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _glMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _glMat.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
        _glMat.SetInt("_ZWrite",   0);
    }

    private void OnGUI()
    {
        if (!activo) return;

        GUIStyle caja = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.UpperLeft };
        caja.normal.textColor = Color.white;

        const float ALTURA_CAJA = 80f;
        int n = managers != null ? managers.Length : 0;
        float y = Screen.height - 10f - n * ALTURA_CAJA - 40f;

        foreach (var mgr in managers)
        {
            if (mgr == null || mgr.Contexto == null) continue;

            string bando     = mgr.faction == Faction.Aliado ? "ALIADOS" : "ENEMIGOS";
            string modo      = mgr.Contexto.modo.ToString().ToUpper();
            string inf       = $"{mgr.Contexto.influenciaPropia:F0} vs {mgr.Contexto.influenciaEnemiga:F0}";
            string guerraTxt = mgr.Contexto.guerraTotal ? " [GUERRA TOTAL]" : "";

            int total = 0, enCombate = 0;
            foreach (var npc in _npcs)
            {
                if (npc == null || npc.faction != mgr.faction) continue;
                total++;
                var ct = npc.GetComponent<ComportamientoTactico>();
                if (ct != null &&
                    (ct.estadoActual == ComportamientoTactico.EstadoTactico.Persiguiendo ||
                     ct.estadoActual == ComportamientoTactico.EstadoTactico.ManteniendoDistancia))
                    enCombate++;
            }

            float tiempoCaptura = 0f;
            foreach (var cv in condiciones)
            {
                if (cv != null && cv.GetComponent<ManagerEstrategico>() == mgr)
                    tiempoCaptura = cv.TiempoCaptura;
            }

            float tiempoNec = mgr.GetComponent<CondicionVictoria>()?.tiempoNecesario ?? 20f;

            string texto =
                $"[{bando}]{guerraTxt} Modo: {modo} | Influencia: {inf}\n" +
                $"Unidades: {total} ({enCombate} combate, {total - enCombate} otras)\n" +
                $"Victoria: {tiempoCaptura:F1}s / {tiempoNec:F0}s";

            GUI.Box(new Rect(10, y, 390, 72), texto, caja);
            y += 80f;
        }

        string pfEstado = tacticoActivo ? "ACTIVO  [T] para desactivar" : "INACTIVO  [T] para activar";
        GUI.Box(new Rect(10, y, 390, 28), $"Pathfinding táctico: {pfEstado}", caja);
    }
}
