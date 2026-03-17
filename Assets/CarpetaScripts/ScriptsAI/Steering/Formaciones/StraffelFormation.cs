using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StraffelFormation : FormationPattern
{
    // Separación entre agentes
    public float separacionX = 1.5f;
    public float separacionZ = 1.5f;

    public override Location GetSlotLocation(int slotNumber) {
        // Slot 0 es el líder en el vértice (0,0)
        if (slotNumber == 0) {
            return new Location { position = Vector3.zero, orientation = 0f };
        }

        //Para hacer esta formación, lo que haremos será seguir el siguiente planteamiento.
        //Teniendo en cuenta que es una formación en formas de v o w, que si tenemos 5 agentes,
        //solo se formará una v, y si tenemos 9 agentes se podrá ver la primera w, lo que haremos es en sí
        //calcular la columna del agente en base al número de slot con respecto al lider.
        //Luego en base al numero de la columna, se calculará la fila

        // Para el resto:
        // Los impares van a la izquierda, los pares a la derecha
        bool esIzquierda = (slotNumber % 2 != 0);
        
        // Calculamos en qué "columna" está (1, 2, 3...)
        int columna  = (slotNumber + 1) / 2;

        float xPos = esIzquierda ? -columna * separacionX : columna * separacionX;


        //Ahora calculamos la profundidad.
        //Para ello hacemos uso de un PingPong para que vaya aumentando y luego disminuyendo, creando así la forma de W.
        float profundidad = Mathf.PingPong(columna, 2);
        float zPos = -profundidad * separacionZ;

        //En esta formación todos miran hacia adelante, los que están en la profundidad media.
        //Estos miraran en diagonal
        float orientation = 0f;
        if (profundidad == 1) {
            orientation = esIzquierda ? 45f : -45f;
        }
        

        return new Location { 
            position = new Vector3(xPos, 0, zPos), 
            orientation = orientation 
        };
    }

    public override Location GetDriftOffset(List<SlotAssignment> slotAssignments) {
        return new Location { position = Vector3.zero, orientation = 0f };
    }

    public override bool SupportsSlots(int slotCount) {
        return slotCount > 0;
    }
}