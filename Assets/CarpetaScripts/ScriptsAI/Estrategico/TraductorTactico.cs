using System.Collections.Generic;
using UnityEngine;

public class TraductorTactico : MonoBehaviour
{
    public float intervaloTraduccion = 1.5f;

    private ContextoGrupo contexto;
    private Faction faction;
    private List<AgentNPC> unidades = new List<AgentNPC>();

    public void InicializarConContexto(ContextoGrupo ctx)
    {
        contexto = ctx;
        faction  = GetComponent<ManagerEstrategico>().faction;

        AgentNPC[] todos = FindObjectsByType<AgentNPC>(FindObjectsSortMode.None);
        foreach (var npc in todos)
        {
            if (npc.faction != faction) continue;
            unidades.Add(npc);

            var ct = npc.GetComponent<ComportamientoTactico>();
            if (ct != null) ct.contextoGrupo = contexto;
        }

        InvokeRepeating(nameof(Traducir), intervaloTraduccion, intervaloTraduccion);
    }

    private void Traducir()
    {
        if (contexto == null) return;

        unidades.RemoveAll(n => n == null);

        foreach (var npc in unidades)
        {
            var ct = npc.GetComponent<ComportamientoTactico>();
            if (ct == null) continue;

            if (ct.estadoActual == ComportamientoTactico.EstadoTactico.Persiguiendo ||
                ct.estadoActual == ComportamientoTactico.EstadoTactico.ManteniendoDistancia)
                continue;

            ct.destinoEstrategico = ObtenerDestinoParaUnidad(npc);
        }
    }

    private Transform ObtenerDestinoParaUnidad(AgentNPC npc)
    {
        var tsm = npc.GetComponent<TerrainSpeedModifier>();
        UnitType tipo = tsm != null ? tsm.unitType : UnitType.InfanteriaPesada;

        return contexto.modo switch
        {
            ModoEstrategico.Defensivo   => ObtenerWaypointDefensivo(tipo),
            ModoEstrategico.Ofensivo    => ObtenerWaypointOfensivo(tipo),
            ModoEstrategico.GuerraTotal => contexto.objetivoAtaque,
            _                           => contexto.puntoDefensa
        };
    }

    private Transform ObtenerWaypointDefensivo(UnitType tipo)
    {
        return tipo switch
        {
            UnitType.Exploradores => ObtenerPasoEstrategicoCercano() ?? contexto.puntoDefensa,
            _                     => contexto.puntoDefensa
        };
    }

    private Transform ObtenerWaypointOfensivo(UnitType tipo)
    {
        return contexto.objetivoAtaque;
    }

    private Transform ObtenerPasoEstrategicoCercano()
    {
        if (contexto.basePropia == null) return null;

        TacticalWaypoint[] todos = FindObjectsByType<TacticalWaypoint>(FindObjectsSortMode.None);
        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (var wp in todos)
        {
            if (wp.role != WaypointRole.PasoEstrategico) continue;
            float d = Vector3.Distance(wp.transform.position, contexto.basePropia.position);
            if (d < minDist) { minDist = d; closest = wp.transform; }
        }
        return closest;
    }
}
