using UnityEngine;
using TMPro;

public class SistemaSalud : MonoBehaviour
{
    public float vidaMaxima = 100f;
    public float vidaActual;

    [Header("Matemáticas de Combate Universitario")]
    [Tooltip("Calidad de la unidad (ej. 100 para élites, 70 para básicos)")]
    public float calidad = 100f;
    [Tooltip("Constante de impacto para regular el daño base")]
    public float constanteImpacto = 20f;
    
    private void Start()
    {
        vidaActual = vidaMaxima;
    }

    public void Curar(float cantidad)
    {
        vidaActual += cantidad;
        if (vidaActual > vidaMaxima) vidaActual = vidaMaxima;
    }

    public void RecibirDano(float cantidad)
    {
        vidaActual -= cantidad;
        
        // Efecto visual rápido: encogerse un pelín o parpadear
        transform.localScale *= 0.95f; 
        
        if (vidaActual <= 0)
        {
            Morir();
        }
    }

    private void Morir()
    {
        // Si hemos creado un Gestor de Respawn en la escena, le cedemos la gestión de la muerte
        if (GestorRespawn.Instancia != null)
        {
            GestorRespawn.Instancia.ProgramarRespawn(this.gameObject, transform.position);
            
            // Forzamos deselección si estamos usando SelectionManager
            SelectionManager sm = Object.FindFirstObjectByType<SelectionManager>();
            if (sm != null)
            {
                AgentNPC ag = GetComponent<AgentNPC>();
                if (ag != null && sm.getSelectedUnits().Contains(ag)) {
                    sm.getSelectedUnits().Remove(ag);
                    ag.SetSelected(false);
                    ag.tag = "NPC";
                }
            }
        }
        else
        {
            // Fallback: Si no hay base, destruimos normalmente.
            Destroy(gameObject);
        }
    }
}
