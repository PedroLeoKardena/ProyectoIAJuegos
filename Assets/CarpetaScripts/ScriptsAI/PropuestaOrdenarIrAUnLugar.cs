using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PropuestaOrdenarIrAUnLugar : MonoBehaviour
{

    /*
    IMPORTANTE: Este script solo funcionará con aquellos objetos (Prefabs, Personajes, etc...) que 
    tengan un collider con el que detectar el click.
    */
    public GameObject markSpecial;

    public Transform target;

    [Header("Interfaz")]
    [Tooltip("Botón (UI) limpiar selección")]
    public GameObject botonLimpiarSeleccion;


    private List<GameObject> markedObjects = new List<GameObject>();


    void Start()
    {
        // Nos aseguramos de que el botón empiece oculto
        if (botonLimpiarSeleccion != null) botonLimpiarSeleccion.SetActive(false);


        //Nos aseguramos de que el target tenga un componente Agent para que los NPCs puedan ir a él
        if (target != null && target.GetComponent<Agent>() == null)
        {
            target.gameObject.AddComponent<Agent>();
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1")){
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit)) {
                if (hit.transform.tag == "Terrain"){
                    //Código para ordenar que todos los objetos marcados vayan a la posición clicada
                    if (target != null){
                        target.position = hit.point;
                        
                        Agent targetAgent = target.GetComponent<Agent>();

                        foreach (GameObject obj in markedObjects){
                            
                            Arrive arriveScript = obj.GetComponent<Arrive>();
                            if (arriveScript != null)
                            {
                                // Le asignamos el puntero como objetivo
                                arriveScript.target = targetAgent;
                            }
                            
                        }
                    }
                }
                else{
                    
                    //Lo que hacemos es comprobar de manera intrínseca si el objeto clicado tiene un componente AgentNPC, y si es así, lo marcamos (o desmarcamos)
                    AgentNPC agenteTocado = hit.transform.GetComponentInParent<AgentNPC>();

                    if (agenteTocado != null)
                    {
                        mark(agenteTocado.gameObject);
                    }
                }
                
            }
        }

        // Actualizamos el estado del botón de limpiar selección.
        
        if (botonLimpiarSeleccion != null)
        {
            bool haySeleccionados = markedObjects.Count > 0;
            botonLimpiarSeleccion.SetActive(haySeleccionados);

            if(haySeleccionados){
                bool todosParados = true;
                foreach (GameObject obj in markedObjects)
                {
                    Agent agente = obj.GetComponent<Agent>();                    
                    if (agente != null && agente.Speed > 0.01f) // Si el agente tiene una velocidad significativa, consideramos que no está parado
                    {
                        todosParados = false;
                        break;
                    }
                }

                Button btnComponente = botonLimpiarSeleccion.GetComponent<Button>();
                if (btnComponente != null)
                {
                    btnComponente.interactable = todosParados;
                }
            }
        }
        
    }

    private void mark(GameObject thing){
        GameObject marker = null;

        // If there is a child in the object called Mark
        if (thing.transform.Find("Mark") != null){
            marker = thing.transform.Find("Mark").gameObject; 
        }

        //Si no hay un hijo llamado Mark creamos uno
        if (marker == null){
            //Creamos instancia de un objeto Mark
            marker = Instantiate (markSpecial, thing.transform);

            marker.transform.localPosition = Vector3.up * 2.5f; //Cambiamos la posicion relativa
            marker.name = "Mark"; //Cambiamos el nombre del objeto

            if(markedObjects.Count == 0){
                thing.tag = "Lider"; // Si es el primer objeto marcado, le asignamos el tag de líder
            }
            
            markedObjects.Add(thing); // Añadimos el objeto a la lista de marcados
        }else{// If there is no reference then
            Destroy(marker); // Destroy the marker

            if (thing.CompareTag("Lider")) {
                thing.tag = "NPC";
            }

            //Eliminamos tambien el target del Script Arrive para que deje de ir al target
            Arrive arriveScript = thing.GetComponent<Arrive>();
            if (arriveScript != null)
            {
                arriveScript.target = null;
            }

            markedObjects.Remove(thing); // Quitamos el objeto de la lista de marcados

        }

        
    }
    
    public void LimpiarSeleccion()
    {
        
        foreach (GameObject obj in markedObjects)
        {   
            if (obj != null)
            {
                // Destruimos la marca
                Transform marker = obj.transform.Find("Mark");
                if (marker != null) Destroy(marker.gameObject);

                // Si alguno era el líder, vuelve a ser NPC
                if (obj.CompareTag("Lider")) obj.tag = "NPC";

                //Eliminamos tambien el target del Script Arrive para que deje de ir al target.
                //En cambio, lo que hacemos es frenar su movimiento asignando el target a la posicion actual del NPC, así alejan infinitamente al NPC del target y por tanto se queda quieto. Si asignamos el target a null, el script Arrive no hace nada y el NPC sigue con su último target asignado.
                Arrive arriveScript = obj.GetComponent<Arrive>();
                if (arriveScript != null)
                {
                    arriveScript.target = null;
                }
            }
        }
        
        // Actualizamos el estado del botón de limpiar selección
        if (botonLimpiarSeleccion != null) botonLimpiarSeleccion.SetActive(false);
        
        // Vaciamos la lista de golpe
        markedObjects.Clear();
    }
}
