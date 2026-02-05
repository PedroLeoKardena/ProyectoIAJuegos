using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnMouse : MonoBehaviour
{
    public Renderer rend;
    // Start is called before the first frame update
    void Start()
    {
        rend = GetComponent<Renderer>();   
    }
    void OnMouseEnter()
    {
        //Cuando ponemos el ratón encima el color pasa a rojo
        rend.material.color = new Color(1, 0, 0);
    }

    void OnMouseOver()
    {
        //Conforme se mantiene encima el color va cambiando de rojo a azul
        rend.material.color += new Color(-.5f, 0, .5f) * Time.deltaTime; 
    }

    void OnMouseExit()
    {
        //Cuando sacamos el ratón de encima vuelve a blanco
        rend.material.color = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
