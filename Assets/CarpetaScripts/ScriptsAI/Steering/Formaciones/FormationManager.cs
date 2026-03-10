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
    public AgentNPC liderNPC; 
    public FormationPattern pattern;
    private Location driftOffset;
    [HideInInspector]
    public List<SlotAssignment> slotAssignments = new List<SlotAssignment>(); // Lista de ocupantes

    [Header("Estados")]
    public bool enFormacionEstricta = false;
    private bool bucleWanderActivo = false; 

    [Header("Referencias de Terreno")]
    public GridManager gridManager; 

    [Header("Configuración del Ciclo")]
    public float tiempoEsperaReconstruccion = 10.0f;
    public float tiempoVagando = 7.0f;


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
        for (int i = 0; i < slotAssignments.Count; i++) {
            var assignment = slotAssignments[i];
            assignment.slotNumber = i;
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
            if (sa.character == liderNPC) continue;

            Location relativeLoc = pattern.GetSlotLocation(sa.slotNumber);
            
            // Calculamos la posición rotada proyectando sobre los ejes locales
            Vector3 rotatedPos = (rightLider * relativeLoc.position.x) + (forwardLider * relativeLoc.position.z);

            // Hacemos lo mismo para el Drift
            Vector3 rotatedDrift = (rightLider * driftOffset.position.x) + (forwardLider * driftOffset.position.z);

            Vector3 finalTargetPos = liderNPC.Position + rotatedPos - rotatedDrift;
            
            if (gridManager != null) {
                Node nodoSlot = gridManager.NodeFromWorldPoint(finalTargetPos);
                
                // Si el nodo no existe o no es caminable
                if (nodoSlot == null || !nodoSlot.isWalkable) {
                    // Buscamos el vecino caminable más cercano para no perder al NPC
                    Node vecinoValido = BuscarVecinoCaminable(nodoSlot);
                    if (vecinoValido != null) {
                        finalTargetPos = vecinoValido.worldPosition;
                    } 
                }
            }

            float finalTargetOri;
            if (!enFormacionEstricta) {
                // Durante el trayecto: Mirar hacia donde se mueven (orientación = velocidad)
                finalTargetOri = sa.character.Orientation;
            } else {
                // En formación estricta: Orientación específica del slot
                finalTargetOri = liderNPC.Orientation + relativeLoc.orientation - driftOffset.orientation;
            }

            // Aplicamos el target final al Agente
            sa.character.SetTarget(finalTargetPos, finalTargetOri);
        }
    }

    public void IniciarDesplazamiento(Vector3 destino)
    {
        StopAllCoroutines();
        bucleWanderActivo = false;
        
        // Se rompe la formación para ir al destino (Leader Following)
        enFormacionEstricta = false; 
        
        if(liderNPC.TryGetComponent<Wander>(out var w)) w.enabled = false;
        
        liderNPC.SetTarget(destino, 0);
        StartCoroutine(BucleWander());
    }

    private void AlternarSteeringsRotacion(bool modoEstricto)
    {
        foreach (var sa in slotAssignments)
        {
            if (sa.character == liderNPC) continue;

            // Buscamos los componentes en el seguidor
            Face faceComp = sa.character.GetComponent<Face>();
            Align alignComp = sa.character.GetComponent<Align>();

            if (modoEstricto)
            {
                // En llegada/parada: Queremos mirar al ángulo del slot
                if (faceComp != null) faceComp.enabled = false;
                if (alignComp != null) alignComp.enabled = true;
            }
            else
            {
                // En viaje/wander: Queremos mirar hacia donde caminamos
                if (faceComp != null) faceComp.enabled = true;
                if (alignComp != null) alignComp.enabled = false;
            }
        }
    }

    IEnumerator BucleWander()
    {
        // 1. VIAJE (Leader Following)
        AlternarSteeringsRotacion(false); // Mirar al frente (Face ON)
        while (Vector3.Distance(liderNPC.Position, liderNPC.TargetFormacion.position) > 2.0f || 
            liderNPC.Velocity.magnitude > 0.5f) {
            yield return new WaitForSeconds(0.5f);
        }

        // 2. LLEGADA Y RECONSTRUCCIÓN
        enFormacionEstricta = true; 
        AlternarSteeringsRotacion(true); // Rotar al slot (Align ON)
        liderNPC.Velocity = Vector3.zero;
        if (liderNPC.TryGetComponent<Arrive>(out var arrInicial)) {
            liderNPC.SetTarget(liderNPC.Position, liderNPC.Orientation);
        }
        yield return new WaitForSeconds(tiempoEsperaReconstruccion);

        // 3. WANDER
        Wander wanderComp = liderNPC.GetComponent<Wander>();
        if (wanderComp != null) {
            bucleWanderActivo = true;
            while (bucleWanderActivo) 
            {
                // --- FASE 1: VAGAR ---
                Debug.Log("Iniciando Wander...");
                
                // Apagamos el Arrive para que no tire de él hacia atrás
                if (liderNPC.TryGetComponent<Arrive>(out var arr)) arr.enabled = false;
                
                wanderComp.enabled = true;
                enFormacionEstricta = false; // Seguidores en Leader Following
                AlternarSteeringsRotacion(false); // Seguidores vuelven a mirar al frente

                yield return new WaitForSeconds(tiempoVagando);

                // --- FASE 2: PARAR ---
                Debug.Log("Parada y Reconstrucción...");
                wanderComp.enabled = false;
                liderNPC.Velocity = Vector3.zero;
                
                // IMPORTANTE: Actualizamos el Arrive para que su destino sea LA POSICIÓN ACTUAL
                if (arr != null) 
                {
                    liderNPC.SetTarget(liderNPC.Position, liderNPC.Orientation);
                    arr.enabled = true; // Ahora el Arrive le ordena quedarse quieto AQUÍ
                }
                
                enFormacionEstricta = true; // Los seguidores reconstruyen en la nueva zona
                AlternarSteeringsRotacion(true); // Seguidores rotan a posición fija
                yield return new WaitForSeconds(tiempoEsperaReconstruccion);
            }
        }
    }


    private void OnDrawGizmos()
    {
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
            // Nota: Asegúrate de que driftOffset esté actualizado antes de dibujar
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