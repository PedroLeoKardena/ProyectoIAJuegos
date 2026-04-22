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
        // En un juego final aquí habría animaciones, partículas, etc.
        Destroy(gameObject);
    }
}
