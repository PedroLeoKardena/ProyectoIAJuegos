using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class Proyectil : MonoBehaviour
{
    public float velocidad = 15f;
    public float dano = 25f; // Solo se usará si no hay disparador registrado
    public float tiempoVida = 5f;

    public string tagEnemigo = "Enemigo"; // Quién recibe el daño
    public GameObject disparador; // Quien disparó la flecha para la Calculadora de Combate

    private void Start()
    {
        // El proyectil vuela hacia el frente constantemente
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.velocity = transform.forward * velocidad;

        // Limpieza automática por si se pierde en el infinito
        Destroy(gameObject, tiempoVida);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Revisamos si chocamos contra nuestro objetivo
        if (other.CompareTag(tagEnemigo) || other.CompareTag("NPC"))
        {
            // Ignorar auto-colisiones
            if (disparador != null && other.gameObject == disparador) return;

            SistemaSalud salud = other.GetComponent<SistemaSalud>();
            if (salud != null)
            {
                float danoCalculado = dano;
                if (disparador != null)
                {
                    // Lógica avanzada del PDF
                    danoCalculado = CalculadoraCombate.CalcularDaño(disparador, other.gameObject);
                }

                salud.RecibirDano(danoCalculado);
                
                // Efecto opcional de impacto ("hit") iría aquí
                Destroy(gameObject); // Destruir el proyectil al chocar
            }
        }
        else if (other.CompareTag("Untagged"))
        {
            // Podría ser un muro u obstáculo
            Destroy(gameObject);
        }
    }
}
