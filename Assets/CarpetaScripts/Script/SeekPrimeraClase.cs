using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeekPrimeraClase : MonoBehaviour
{
    public Transform target;
    public float velocity = 2f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 newDirection = target.position - transform.position;

        transform.LookAt(transform.position+newDirection);

        transform.position += newDirection*velocity*Time.deltaTime;
    }

    private void OnDrawGizmos (){
        //El gizmo es una linea que se dibuja en la escena para ver direcciones o posiciones
        
        //Origen de la linea
        Vector3 from = transform.position;

        //Destino de la linea
        Vector3 to =  transform.localPosition + (target.position - transform.position) * velocity;

        // Elevación para no tocar el suelo
        Vector3 elevation = new Vector3 (0,1,0);
        from += elevation;
        to += elevation;

        Gizmos.color = Color.red;

        Gizmos.DrawLine (from, to);
    }
}
