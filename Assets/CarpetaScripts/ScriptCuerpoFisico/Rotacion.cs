using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotacion : MonoBehaviour
{
    public float valor = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Ejecucion 1: con esta configuración, si ponemos angulo desde [0,179] -> Y crece normal.
        //Si ponemos angulo desde [180,360] -> Y crece desde -180 hasta 0
        //transform.rotation = Quaternion.AngleAxis(valor, Vector3.up);

        //Ejecucion 2
        //Igual que la ejecucion 1.
        transform.rotation = Quaternion.Euler(0, valor, 0);
    
        //Ejercicio 3
        //Igual que las anteriores
        //transform.rotation = new Quaternion(); //Quaternion.identity;
        //transform.Rotate(Vector3.up, valor);
    }
}
