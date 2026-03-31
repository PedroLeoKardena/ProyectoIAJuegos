using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public LayerMask unitLayer; // Para detectar los personajes en la selección
    private List<AgentNPC> selectedUnits = new List<AgentNPC>(); // Lista de unidades seleccionadas
    public FormationManager formationManager;
    public Texture2D selectionTexture;

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

        // Formar las unidades seleccionadas con la tecla "F"
        if (Input.GetKeyDown("f"))
        {
            FormarSeleccionados();
        }
    }

    // Funciones genéricas para activar solo lo que interesa
    void SetSteeringsViaje(AgentNPC npc)
    {
        npc.SetModoViaje();
    }

    void SetSteeringsFormacionEstricta(AgentNPC npc)
    {
        npc.SetModoFormacionEstricta();
    }

    void SetSteeringsLiderWander(AgentNPC npc)
    {
        npc.SetModoLiderWander();
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
                
                if (!holdsShift)
                {
                    DeselectAllUnits();
                    selectedUnits.Add(unit);
                    unit.SetSelected(true);
                } 
                else
                {
                    if (selectedUnits.Contains(unit))
                    {
                        selectedUnits.Remove(unit);
                        unit.SetSelected(false);
                         
                    }
                    else
                    {
                        selectedUnits.Add(unit);
                        unit.SetSelected(true);
                    }
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

        foreach (var unit in Object.FindObjectsByType<AgentNPC>(FindObjectsSortMode.None))
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
            if (IsWithinSelectionBounds(screenPos))
            {
                if (!selectedUnits.Contains(unit))
                {
                    selectedUnits.Add(unit);
                    unit.SetSelected(true);
                } 
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
        foreach(var unit in selectedUnits) 
        {
            unit.tag = "NPC";
            unit.SetSelected(false);
        }
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
                    formationManager.liderNPC = agente;
                } else {
                    agente.tag = "NPC";   // El resto son seguidores
                    // Añadimos a la formación
                    formationManager.AddCharacter(agente);
                }
                
                // ACTIVAMOS los componentes de movimiento
                SetSteeringsFormacionEstricta(agente);
            }
        }
        
        formationManager.enFormacionEstricta = true;
        formationManager.UpdateSlotAssignments();
    }

    public void MoveSelectedUnitsToPoint(Vector3 destination)
    {
        if (selectedUnits.Count == 0) return;

        foreach(var npc in selectedUnits)
        {
            npc.tag = "NPC";
            SetSteeringsViaje(npc);
            npc.SetTarget(destination, 0);
        }
    }

    
    // Dibujar selección
    void OnGUI()
    {
        if (Input.GetMouseButton(0) && Vector3.Distance(startMousePos, Input.mousePosition) > 4f)
        {
            // Creamos el rectángulo basado en la posición del ratón
            var rect = GetScreenRect(startMousePos, Input.mousePosition);
            
            // Dibujamos el fondo semitransparente
            GUI.color = new Color(1f, 1f, 1f, 0.2f);
            GUI.DrawTexture(rect, selectionTexture);
            
        }
    }

    Rect GetScreenRect(Vector3 screenPos1, Vector3 screenPos2)
    {
        screenPos1.y = Screen.height - screenPos1.y;
        screenPos2.y = Screen.height - screenPos2.y;
        var topLeft = Vector3.Min(screenPos1, screenPos2);
        var bottomRight = Vector3.Max(screenPos1, screenPos2);
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }



}
