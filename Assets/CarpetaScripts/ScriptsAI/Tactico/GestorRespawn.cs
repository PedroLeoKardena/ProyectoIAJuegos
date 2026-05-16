using System.Collections;
using UnityEngine;

public class GestorRespawn : MonoBehaviour
{
    public static GestorRespawn Instancia;

    [Tooltip("Tiempo en segundos antes de que la unidad destruida reaparezca en la base")]
    public float tiempoRespawn = 5f;

    [Tooltip("Punto/Objeto en el mapa que actúa como base para las unidades de los NPCs (Aliados/Neutrales)")]
    public Transform baseAliada;
    
    [Tooltip("Punto/Objeto en el mapa que actúa como base para los Enemigos")]
    public Transform baseEnemiga;

    private void Awake()
    {
        // Implementación sencilla del patrón Singleton para poder llamarlo desde cualquier lado
        if (Instancia == null) Instancia = this;
        else Destroy(gameObject);
    }

    public void ProgramarRespawn(GameObject unidad, Vector3 puntoMuerte)
    {
        StartCoroutine(RutinaRespawn(unidad, puntoMuerte));
    }

    private IEnumerator RutinaRespawn(GameObject unidad, Vector3 puntoMuerte)
    {
        // 1. Apagamos la unidad en lugar de destruirla para poder reciclarla (Object Pooling básico)
        unidad.SetActive(false);

        // 2. Esperamos el tiempo definido para agilizar el juego
        yield return new WaitForSeconds(tiempoRespawn);

        // 3. Determinar a qué base debe ir según su Tag
        Transform baseDestino = baseAliada;
        if (unidad.CompareTag("Enemigo") && baseEnemiga != null)
        {
            baseDestino = baseEnemiga;
        }

        // Si hay una base definida, se mueve ahí; si no, reaparece donde murió por si acaso.
        Vector3 posAparicion = baseDestino != null ? baseDestino.position : puntoMuerte;

        // 4. Revivir y reposicionar
        unidad.transform.position = posAparicion;
        
        SistemaSalud salud = unidad.GetComponent<SistemaSalud>();
        if (salud != null) salud.vidaActual = salud.vidaMaxima;

        // Restauramos su escala por si se quedó encogida debido al daño visual
        unidad.transform.localScale = Vector3.one; 

        // Encendemos la unidad de nuevo
        unidad.SetActive(true);

        // 5. Ordenarle que acuda al lugar en el que fue destruida (Waypoint destino)
        AgentNPC agent = unidad.GetComponent<AgentNPC>();
        if (agent != null)
        {
            // Forzamos modo viaje y le mandamos al punto donde murió
            agent.SetModoViaje();
            agent.SetTarget(puntoMuerte, 0);
        }
    }
}
