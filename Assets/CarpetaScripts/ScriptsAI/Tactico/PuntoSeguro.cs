using UnityEngine;

public class PuntoSeguro : MonoBehaviour
{
    [Tooltip("Cantidad de vida recuperada por segundo")]
    public float curacionPorSegundo = 10f;

    [Tooltip("Velocidad máxima (magnitud) permitida para considerar que la unidad está inmóvil")]
    public float umbralInmovil = 0.1f;

    private void OnTriggerStay(Collider other)
    {
        // 1. Verificar si el objeto que entra tiene un sistema de salud
        SistemaSalud salud = other.GetComponent<SistemaSalud>();
        
        if (salud != null && salud.vidaActual < salud.vidaMaxima)
        {
            // 2. Comprobar si está inmóvil
            // Intentamos obtener el componente Bodi para leer su Speed
            Bodi bodi = other.GetComponent<Bodi>();
            
            bool estaInmovil = false;

            if (bodi != null)
            {
                // Si su velocidad es menor que el umbral, consideramos que está quieto
                if (bodi.Speed <= umbralInmovil)
                {
                    estaInmovil = true;
                }
            }
            else 
            {
                // Si no tiene componente Bodi, miramos su Rigidbody estándar si tiene
                Rigidbody rb = other.GetComponent<Rigidbody>();
                if (rb != null && rb.velocity.magnitude <= umbralInmovil)
                {
                    estaInmovil = true;
                }
                else if (rb == null)
                {
                    // Si no tiene ni Bodi ni Rigidbody, le curamos siempre por defecto
                    estaInmovil = true;
                }
            }

            // 3. Aplicar curación si corresponde
            if (estaInmovil)
            {
                // Curamos un poquito cada frame (curacionPorSegundo * Time.deltaTime)
                salud.Curar(curacionPorSegundo * Time.deltaTime);
            }
        }
    }
}
