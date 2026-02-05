using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropuestaOrdenarIrAUnLugar : MonoBehaviour
{
    public GameObject markSpecial;

    public Transform target;

    private List<GameObject> markedObjects = new List<GameObject>();

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
                        foreach (GameObject obj in markedObjects){
                            SeekAceleration seekScript = obj.GetComponent<SeekAceleration>();
                            if (seekScript != null)
                            {
                                // Le asignamos el puntero como objetivo
                                seekScript.target = target;
                            }
                        }
                    }
                }
                else if (hit.transform.tag == "NPC"){
                    //Aqui no hace falta hacer la diferenciación entre clickar en marca o en NPC, ya que las marcas
                    //generadas son hijas de los NPCs y por tanto no tienen tag "NPC"
                    mark(hit.transform.gameObject);
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

            marker.transform.localPosition = Vector3.up * 1; //Cambiamos la posicion relativa
            marker.name = "Mark"; //Cambiamos el nombre del objeto

            markedObjects.Add(thing); // Añadimos el objeto a la lista de marcados
        }else{// If there is no reference then
            Destroy(marker); // Destroy the marker
            
            markedObjects.Remove(thing); // Quitamos el objeto de la lista de marcados
        }
    }
}
