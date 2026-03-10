using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerSelection : MonoBehaviour
{
    public LayerMask unitLayer; // Para detectar los personajes en la selección
    public List<AgentNPC> selectedUnits = new List<AgentNPC>(); // Lista de unidades seleccionadas
    public FormationManager formationManager;

    private Vector3 startMousePos; // Posición inicial del clic para arrastrar selección

    void Update()
    {
        // Selección con clic izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            startMousePos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Si fue un clic y no un arrastre, seleccionar unidad individual
            if (Vector3.Distance(startMousePos, Input.mousePosition) < 4f)
            {
                SelectUnit();
            }
            else
            {
                SelectMultipleUnits();
            }
        }

        //Selecciona el punto para que los seleccionados vayan
        if (Input.GetMouseButtonDown(1)) 
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (formationManager != null && formationManager.slotAssignments.Count > 0)
                {
                    formationManager.IniciarDesplazamiento(hit.point);
                }
                else
                {
                    Debug.Log("Moviendo sin formación.");
                    // Si no hay formación (unidades sueltas), movimiento normal en rejilla
                    MoveSelectedUnitsToPoint(hit.point);
                }
            }
        }

        // Deseleccionar todas las unidades con la tecla "Esc"
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectAllUnits();
        }
    }

    void SelectUnit()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, unitLayer))
        {
            AgentNPC unit = hit.collider.GetComponent<AgentNPC>();
            if (unit != null)
            {
                bool holdsShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                
                if (!holdsShift) DeselectAllUnits();

                if (selectedUnits.Contains(unit))
                {
                    if (holdsShift) selectedUnits.Remove(unit);
                }
                else
                {
                    selectedUnits.Add(unit);
                }
            }
        }
        else if (!Input.GetKey(KeyCode.LeftShift))
        {
            DeselectAllUnits(); // Clic al vacío deselecciona
        }
    }

    void SelectMultipleUnits()
    {
        if (!Input.GetKey(KeyCode.LeftShift)) DeselectAllUnits();

        foreach (var unit in FindObjectsOfType<AgentNPC>())
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
            if (IsWithinSelectionBounds(screenPos))
            {
                if (!selectedUnits.Contains(unit)) selectedUnits.Add(unit);
            }
        }
    }

    bool IsWithinSelectionBounds(Vector3 screenPos)
    {
        float minX = Mathf.Min(startMousePos.x, Input.mousePosition.x);
        float maxX = Mathf.Max(startMousePos.x, Input.mousePosition.x);
        float minY = Mathf.Min(startMousePos.y, Input.mousePosition.y);
        float maxY = Mathf.Max(startMousePos.y, Input.mousePosition.y);

        return screenPos.x > minX && screenPos.x < maxX && screenPos.y > minY && screenPos.y < maxY;
    }

    public void DeselectAllUnits()
    {
        foreach(var unit in selectedUnits) unit.tag = "NPC";
        selectedUnits.Clear();
    }

    public List<AgentNPC> getSelectedUnits()
    {
        return this.selectedUnits;
    }

    public void FormarSeleccionados()
    {
        if (formationManager == null || selectedUnits.Count == 0) return;

        formationManager.slotAssignments.Clear();
        formationManager.liderNPC = null;

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            AgentNPC agente = selectedUnits[i];
            if (agente != null)
            {
                if (i == 0) {
                    agente.tag = "Lider"; // El primero seleccionado es el líder
                } else {
                    agente.tag = "NPC";   // El resto son seguidores
                }
                // Añadimos a la formación
                formationManager.AddCharacter(agente);
                
                // ACTIVAMOS los componentes de movimiento
                if (agente.TryGetComponent<Arrive>(out var arr)) arr.enabled = true;
                if (agente.TryGetComponent<Align>(out var aln)) aln.enabled = false;
                if (agente.TryGetComponent<Separation>(out var sep)) sep.enabled = true;
                if (agente.TryGetComponent<Face>(out var face)) face.enabled = false;
                if (agente.TryGetComponent<WallAvoidance>(out var wall)) wall.enabled = true;

                // Reseteamos los targets manuales para que obedezcan al FormationManager
                if (arr != null) arr.target = null;
                if (aln != null) aln.target = null;
            }
        }
        
        formationManager.enFormacionEstricta = true;
        formationManager.UpdateSlotAssignments();
        formationManager.Invoke("AlternarSteeringsRotacionEstricta", 0.1f);
        Debug.Log("Agentes activados y en formación.");
    }

    public void MoveSelectedUnitsToPoint(Vector3 destination)
    {
        if (selectedUnits.Count == 0) return;

        float spacing = 2.0f; // Espacio entre unidades para que no choquen
        
        int unitsPerRow = Mathf.CeilToInt(Mathf.Sqrt(selectedUnits.Count));
        int totalRows = Mathf.CeilToInt((float)selectedUnits.Count / unitsPerRow);

        // Calculamos el offset para que el 'destination' sea el CENTRO del grupo
        float offsetX = (unitsPerRow - 1) * spacing / 2f;
        float offsetZ = (totalRows - 1) * spacing / 2f;

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            int row = i / unitsPerRow;
            int col = i % unitsPerRow;

            // Posición relativa al centro
            Vector3 posRelativa = new Vector3(col * spacing - offsetX, 0, -row * spacing + offsetZ);
            Vector3 finalPosition = destination + posRelativa;

            selectedUnits[i].tag = "NPC"; // Al no haber formación oficial, todos son NPCs (para Separation)

            // Activamos comportamientos básicos
            if (selectedUnits[i].TryGetComponent<Arrive>(out var arr)) arr.enabled = true;
            if (selectedUnits[i].TryGetComponent<Separation>(out var sep)) sep.enabled = true;
            if (selectedUnits[i].TryGetComponent<Face>(out var face)) face.enabled = true;
            if (selectedUnits[i].TryGetComponent<WallAvoidance>(out var wall)) wall.enabled = true;

            if (formationManager.gridManager != null) {
                Node nodoSlot = formationManager.gridManager.NodeFromWorldPoint(finalPosition);
                if (nodoSlot == null || !nodoSlot.isWalkable) {
                    Node vecinoValido = formationManager.BuscarVecinoCaminable(nodoSlot);
                    if (vecinoValido != null) {
                        finalPosition = vecinoValido.worldPosition;
                    } 
                }
            }

            // Asignar destino
            selectedUnits[i].SetTarget(finalPosition, 0);
        }
    }

}
