using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotacionAleatoria : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public float velocity = 4f;

    public float deltaTime = 0.25f;
    public float time = 0f;

    //Control orientación aleatoria
    public float MaxAngle = 360f;
    private float angle = 0f;
    private float newAngle = 0f;
    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;

        if (time >= deltaTime){// Si se alcanzó el intervalo temporal ...
            newAngle = (Random.value - Random.value) * MaxAngle; // cambiar el ángulo de orientación.
            time = 0; // Vuelta a empezar
        }

        // Linearly interpolates between a and b by t. Especial para ángulos
        angle = Mathf.LerpAngle(angle, newAngle, Time.deltaTime); //Cambio suave

        // The rotation as Euler angles in degrees. Rotación para esa orientación.
        transform.eulerAngles = new Vector3(0, angle, 0);

        // Movimiento lineal
        transform.position += transform.forward * velocity * Time.deltaTime;
    }

}
