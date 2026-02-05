using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public float velocity = 2f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Leer el teclado
        Vector3 newDirection = new Vector3 (
            Input.GetAxis ("Horizontal"),
            0,
            Input.GetAxis ("Vertical"));
        // Mirar en la dirección del vector leído.
        transform.LookAt (transform.position + newDirection);
        // Avanzar de acuerdo a la velocidad establecida
        transform.position +=
            newDirection * velocity * Time.deltaTime;
    }
}
