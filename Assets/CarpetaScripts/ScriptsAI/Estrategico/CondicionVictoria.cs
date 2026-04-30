using UnityEngine;

public class CondicionVictoria : MonoBehaviour
{
    [Header("Configuración")]
    public float tiempoNecesario = 20f;
    public float radioCaptura    = 5f;

    public float TiempoCaptura     { get; private set; } = 0f;
    public bool  VictoriaDeclarada { get; private set; } = false;

    // Cuando un bando declara victoria, ningún OTRO bando puede
    // ganar después aunque también cumpla las condiciones. 
    public static bool JuegoTerminado { get; private set; } = false;

    private ManagerEstrategico manager;

    private void Awake()
    {
        JuegoTerminado = false;
    }

    private void Start()
    {
        manager = GetComponent<ManagerEstrategico>();
    }

    private void Update()
    {
        // Si yo ya gané, si ya ganó cualquier otro bando, o si no hay manager → no compruebo.
        if (VictoriaDeclarada || JuegoTerminado || manager == null || manager.Contexto == null) return;
        if (manager.Contexto.baseEnemiga == null) return;

        if (HayUnidadPropiaCercaDeBaseEnemiga())
        {
            TiempoCaptura += Time.deltaTime;
            if (TiempoCaptura >= tiempoNecesario)
            {
                VictoriaDeclarada = true;
                JuegoTerminado    = true;   // bloquea a todos los demás bandos
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
