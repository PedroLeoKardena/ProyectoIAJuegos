using UnityEngine;

public class CondicionVictoria : MonoBehaviour
{
    [Header("Configuración")]
    public float tiempoNecesario = 20f;
    public float radioCaptura    = 5f;

    public float TiempoCaptura     { get; private set; } = 0f;
    public bool  VictoriaDeclarada { get; private set; } = false;

    private ManagerEstrategico manager;

    private void Start()
    {
        manager = GetComponent<ManagerEstrategico>();
    }

    private void Update()
    {
        if (VictoriaDeclarada || manager == null || manager.Contexto == null) return;
        if (manager.Contexto.baseEnemiga == null) return;

        if (HayUnidadPropiaCercaDeBaseEnemiga())
        {
            TiempoCaptura += Time.deltaTime;
            if (TiempoCaptura >= tiempoNecesario)
            {
                VictoriaDeclarada = true;
                manager.DeclararVictoria();
            }
        }
        else
        {
            TiempoCaptura = 0f;
        }
    }

    private bool HayUnidadPropiaCercaDeBaseEnemiga()
    {
        Vector3 posBase      = manager.Contexto.baseEnemiga.position;
        Faction factionPropia = manager.faction;

        foreach (var npc in FindObjectsByType<AgentNPC>(FindObjectsSortMode.None))
        {
            if (npc.faction != factionPropia) continue;
            if (Vector3.Distance(npc.Position, posBase) <= radioCaptura)
                return true;
        }
        return false;
    }
}
