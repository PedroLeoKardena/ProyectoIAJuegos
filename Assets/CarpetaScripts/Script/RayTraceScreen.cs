using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTraceScreen : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Devuelve un rayo desde la cámara hasta la posición del ratón en pantalla
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit; 

        if (Physics.Raycast(ray, out hit)){
            draw(ray, hit);
        }
    }

    void draw(Ray ray, RaycastHit hit){
        //El punto de impacto no es el plano
        string str = hit.transform.gameObject.name;
        if (!( str.Equals("Plane") || str.Equals("Quad"))){
            Debug.DrawLine(ray.origin, hit.point, Color.red);
            Debug.DrawLine(hit.point, hit.point + hit.normal * 20, Color.blue);
        }
        
        changeColor(hit);
    }

    private GameObject firstThing = null;
    private GameObject secondThing = null;

    MeshRenderer m_Renderer = null;
    Color m_OriginalColor = Color.green;

    private bool firstTime = true;

    void changeColor(RaycastHit hit){
        string str = hit.transform.gameObject.name;
        if (firstTime &&! ( str.Equals("Plane") || str.Equals("Quad"))){
            firstThing = hit.transform.gameObject;
            m_Renderer = firstThing.GetComponent<MeshRenderer>();
            m_OriginalColor = m_Renderer.material.color;
            m_Renderer.material.color = Color.gray;
            firstTime = false;
            return;
        }

        if (firstThing == null) return;

        secondThing = hit.transform.gameObject;
        if (firstThing == secondThing) return;

        m_Renderer.material.color = m_OriginalColor;

        firstTime= true;
    }
}
