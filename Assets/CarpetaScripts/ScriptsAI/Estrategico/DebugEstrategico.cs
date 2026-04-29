using UnityEngine;

public class DebugEstrategico : MonoBehaviour
{
    private bool activo = false;
    private ManagerEstrategico[] managers;
    private CondicionVictoria[]  condiciones;

    private void Start()
    {
        managers    = FindObjectsByType<ManagerEstrategico>(FindObjectsSortMode.None);
        condiciones = FindObjectsByType<CondicionVictoria>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            activo = !activo;
    }

    private void OnGUI()
    {
        if (!activo) return;

        GUIStyle caja = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.UpperLeft };
        caja.normal.textColor = Color.white;

        float y = 10f;
        foreach (var mgr in managers)
        {
            if (mgr == null || mgr.Contexto == null) continue;

            string bando     = mgr.faction == Faction.Aliado ? "ALIADOS" : "ENEMIGOS";
            string modo      = mgr.Contexto.modo.ToString().ToUpper();
            string inf       = $"{mgr.Contexto.influenciaPropia:F0} vs {mgr.Contexto.influenciaEnemiga:F0}";
            string guerraTxt = mgr.Contexto.guerraTotal ? " [GUERRA TOTAL]" : "";

            int total = 0, enCombate = 0;
            foreach (var npc in FindObjectsByType<AgentNPC>(FindObjectsSortMode.None))
            {
                if (npc.faction != mgr.faction) continue;
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
    }

    private void OnDrawGizmos()
    {
        if (!activo || !Application.isPlaying) return;

        foreach (var wp in FindObjectsByType<TacticalWaypoint>(FindObjectsSortMode.None))
        {
            Gizmos.color = wp.role switch
            {
                WaypointRole.Base when wp.faction == Faction.Aliado => Color.green,
                WaypointRole.Base                                   => Color.red,
                WaypointRole.PasoEstrategico                       => Color.yellow,
                _                                                   => Color.cyan
            };
            Gizmos.DrawWireSphere(wp.transform.position, 2f);
        }

        foreach (var npc in FindObjectsByType<AgentNPC>(FindObjectsSortMode.None))
        {
            var ct = npc.GetComponent<ComportamientoTactico>();
            if (ct == null || ct.destinoEstrategico == null) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(npc.Position, ct.destinoEstrategico.position);
        }
    }
}
