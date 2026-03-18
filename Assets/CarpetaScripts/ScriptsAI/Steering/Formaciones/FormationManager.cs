using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SlotAssignment
{
    public AgentNPC character;
    public int slotNumber;
}

public struct Location
{
    public Vector3 position;
    public float orientation;
}

public class FormationManager : MonoBehaviour
{
    public Agent liderNPC; 
    public FormationPattern pattern;
    private Location driftOffset;
    
    public List<SlotAssignment> slotAssignments = new List<SlotAssignment>(); // Lista de ocupantes
    public bool autoCargarAlInicio = false;

    [Header("Estados")]
    public bool enFormacionEstricta = false;
    private bool bucleWanderActivo = false; 

    [Header("Referencias de Terreno")]
    public GridManager gridManager; 

    [Header("Configuración del Ciclo")]
    public float tiempoEsperaReconstruccion = 10.0f;
    public float tiempoVagando = 7.0f;

    public bool drawGizmos = false;

    void Start() 
    {
        if (autoCargarAlInicio)
        {
            AsignarUnidadesPreestablecidas();
        }
        // Si está todo preconfigurado (no dependemos de ManageSelection), para el escenario de prueba en el que el lider y los demás están ya preestablecidos
        if (slotAssignments.Count > 0 && liderNPC != null) 
        {
            enFormacionEstricta = true;
            if (liderNPC is AgentPlayer) {
                ActivarSteeringsViaje(); 
            } else {
                // Si el líder es NPC, sí podemos lanzar el ciclo normal
                ActivarSteeringsEstrictos();
            }
        }
    }


    void Update() {
        // Si el líder actual desaparece, buscamos un sustituto
        if (liderNPC == null && slotAssignments.Count > 0) {
            ValidarLider();
        }
        if (enFormacionEstricta && liderNPC != null) {
            UpdateSlots();
        }
        
        // Control del bucle Wander (Pulsar 'S' para parar)
        if (Input.GetKeyDown(KeyCode.S)) DetenerTodo();
    }

    public void AsignarUnidadesPreestablecidas()
    {   
        slotAssignments.Clear();
        AgentNPC[] todos = Object.FindObjectsByType<AgentNPC>(FindObjectsSortMode.None);
        foreach (AgentNPC npc in todos)
        {
            AddCharacter(npc);
        }
        UpdateSlotAssignments();
        Debug.Log("Formación cargada con " + slotAssignments.Count + " unidades.");
    }

    void ActivarSteeringsEstrictos()
    {
        foreach (var sa in slotAssignments)
        {
            if (sa.character == liderNPC) continue;
            sa.character.SetModoFormacionEstricta();
        }
    }

    void ActivarSteeringsViaje()
    {
        foreach (var sa in slotAssignments)
        {
            if (sa.character == liderNPC) continue;
            sa.character.SetModoViaje();
        }
    }

    void ActivarWanderLider()
    {
        Agent npc = liderNPC;
        npc.SetModoLiderWander();
    }

    bool HaLlegado(AgentNPC npc)
    {
        float d = Vector3.Distance(npc.Position, npc.TargetFormacion.position);
        return d < 0.25f; // Ajustable
    }

    void ActualizarSteeringsNPC(AgentNPC npc, bool esLider)
    {
        if (esLider) return;
        if (HaLlegado(npc))
        {
            npc.GetComponent<LookWhereYouGoing>().enabled = false;
            npc.GetComponent<Align>().enabled = true;
        }
        else
        {
            npc.GetComponent<LookWhereYouGoing>().enabled = true;
            npc.GetComponent<Align>().enabled = false;
        }
        npc.GetComponent<Arrive>().enabled = true;
    }

    // Elige automáticamente al primer personaje de la lista como líder
    private void ValidarLider() {
        if (slotAssignments.Count > 0) {
            liderNPC = slotAssignments[0].character;
            Debug.Log("Nuevo líder asignado: " + liderNPC.name);
        }
    }

    public void DetenerTodo() {
        bucleWanderActivo = false;
        enFormacionEstricta = false;
        StopAllCoroutines();
        if (liderNPC != null) {
            liderNPC.Velocity = Vector3.zero;
            if(liderNPC.TryGetComponent<Wander>(out var w)) w.enabled = false;
        }
        Debug.Log("SISTEMA DETENIDO POR USUARIO");
    }

    // Actualiza la asignación de slots cuando cambia el número de personajes
    public void UpdateSlotAssignments() {
        if (pattern != null) {
            pattern.SetNumberOfSlots(slotAssignments.Count);
        }
        
        for (int i = 0; i < slotAssignments.Count; i++) {
            var assignment = slotAssignments[i];
            assignment.slotNumber = i + 1;
            slotAssignments[i] = assignment;
        }

        // Recalculamos el drift offset basándonos en los slots actualmente ocupados
        driftOffset = pattern.GetDriftOffset(slotAssignments);
    }

    // Añade un nuevo personaje a la formación
    public bool AddCharacter(AgentNPC character) {
        int occupiedSlots = slotAssignments.Count;

        // Comprobamos si el patrón soporta uno más
        if (pattern.SupportsSlots(occupiedSlots + 1)) {
            SlotAssignment newAssignment = new SlotAssignment {
                character = character
            };
            slotAssignments.Add(newAssignment);
            
            UpdateSlotAssignments();
            return true;
        }
        return false;
    }

    // Quita un personaje y reorganiza
    public void RemoveCharacter(AgentNPC character) {
        int index = slotAssignments.FindIndex(a => a.character == character);
        if (index != -1) {
            slotAssignments.RemoveAt(index);
            UpdateSlotAssignments();
        }
    }

    public Node BuscarVecinoCaminable(Node nodoOriginal) {
        if (nodoOriginal == null) return null;
        
        List<Node> vecinos = gridManager.GetNeighbors(nodoOriginal);
        foreach (Node n in vecinos) {
            if (n.isWalkable) return n;
        }
        return null;
    }

    // Calcula y envía los destinos a cada personaje
    public void UpdateSlots() {
        // Obtenemos el vector "Forward" y el vector "Right" del líder
        Vector3 forwardLider = liderNPC.AngleToVector(liderNPC.Orientation);
        Vector3 rightLider = liderNPC.AngleToVector(liderNPC.Orientation + 90f);

        foreach (var sa in slotAssignments) {
            if (sa.character == (Agent)liderNPC) continue;

            AgentNPC npc = sa.character;
            Location relativeLoc = pattern.GetSlotLocation(sa.slotNumber);
            
            // Calculamos la posición rotada proyectando sobre los ejes locales
            Vector3 rotatedPos = (rightLider * relativeLoc.position.x) + (forwardLider * relativeLoc.position.z);

            // Hacemos lo mismo para el Drift
            Vector3 rotatedDrift = (rightLider * driftOffset.position.x) + (forwardLider * driftOffset.position.z);

            Vector3 finalPos = liderNPC.Position + rotatedPos - rotatedDrift;
            
            if (gridManager != null) {
                Node nodoSlot = gridManager.NodeFromWorldPoint(finalPos);
                
                // Si el nodo no existe o no es caminable
                if (nodoSlot == null || !nodoSlot.isWalkable) {
                    // Buscamos el vecino caminable más cercano para no perder al NPC
                    Node vecinoValido = BuscarVecinoCaminable(nodoSlot);
                    if (vecinoValido != null) {
                        finalPos = vecinoValido.worldPosition;
                    } 
                }
            }

            float orientacion = liderNPC.Orientation + relativeLoc.orientation - driftOffset.orientation;
            npc.SetTarget(finalPos, orientacion);
            ActualizarSteeringsNPC(npc, false);
        }
    }

    public void IniciarDesplazamiento(Vector3 destino)
    {
        if (liderNPC is AgentNPC npc) 
        {
            StopAllCoroutines();
            bucleWanderActivo = false;
            
            // Se rompe la formación para ir al destino (Leader Following)
            enFormacionEstricta = false; 
            
            if(npc.TryGetComponent<Wander>(out var w)) w.enabled = false;
            
            npc.SetTarget(destino, 0);
            StartCoroutine(BucleWander());
        }
    }

    IEnumerator BucleWander()
    {
        // 1. Fase de Viaje (Solo si el líder NO es el jugador, ya que el jugador no usa TargetFormacion)
        if (liderNPC is AgentNPC) 
        {
            ActivarSteeringsViaje();
            while (Vector3.Distance(liderNPC.Position, (liderNPC as AgentNPC).TargetFormacion.position) > 2f ||
                liderNPC.Velocity.magnitude > 0.5f)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        // 2. Llegada y Reconstrucción
        enFormacionEstricta = true;
        ActivarSteeringsEstrictos();
        
        // Si el líder es NPC, lo frenamos. Si es Player, el jugador decide cuándo frenar.
        if (liderNPC is AgentNPC) liderNPC.Velocity = Vector3.zero; 

        yield return new WaitForSeconds(tiempoEsperaReconstruccion);

        // 3. Wander (SOLO si el líder es un NPC)
        // El jugador no vaga solo, así que si el líder es Player, terminamos aquí.
        if (liderNPC is AgentNPC npc)
        {
            Wander w = npc.GetComponent<Wander>();
            if (w != null)
            {
                bucleWanderActivo = true;
                while (bucleWanderActivo)
                {
                    ActivarWanderLider();
                    enFormacionEstricta = false;
                    ActivarSteeringsViaje();
                    yield return new WaitForSeconds(tiempoVagando);
                    w.enabled = false;
                    npc.Velocity = Vector3.zero;
                    if (npc.TryGetComponent<Arrive>(out var arr1))
                    {
                        npc.SetTarget(npc.Position, npc.Orientation);
                        arr1.enabled = true;
                    }
                    enFormacionEstricta = true;
                    ActivarSteeringsEstrictos();
                    yield return new WaitForSeconds(tiempoEsperaReconstruccion);
                }
            }
        }
    }


    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        if (!Application.isPlaying || liderNPC == null || pattern == null) return;

        // Dibujamos el "Forward" y "Right" del líder para validar AngleToVector
        Vector3 fwd = liderNPC.AngleToVector(liderNPC.Orientation);
        Vector3 rgt = liderNPC.AngleToVector(liderNPC.Orientation + 90f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(liderNPC.Position, fwd * 2f); // Flecha azul: Forward
        Gizmos.color = Color.red;
        Gizmos.DrawRay(liderNPC.Position, rgt * 2f); // Flecha roja: Right

        foreach (var sa in slotAssignments)
        {
            Location relLoc = pattern.GetSlotLocation(sa.slotNumber);
            
            // REPETIMOS EL CÁLCULO DE UPDATESLOTS
            Vector3 rotPos = (rgt * relLoc.position.x) + (fwd * relLoc.position.z);
            Vector3 rotDrift = (rgt * driftOffset.position.x) + (fwd * driftOffset.position.z);
            Vector3 slotWorldPos = liderNPC.Position + rotPos - rotDrift;

            // Dibujamos una esfera donde DEBERÍA estar el personaje
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(slotWorldPos, 0.3f);
            
            // Línea que une al personaje con su slot asignado
            if (sa.character != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(sa.character.Position, slotWorldPos);
            }
        }
    }
}