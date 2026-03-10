using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuntaLanzaFormation : FormationPattern
{
    // Separación entre agentes
    public float separacionX = 1.5f;
    public float separacionZ = 1.5f;

    public override Location GetSlotLocation(int slotNumber) {
        // Slot 0 es el líder en el vértice (0,0)
        if (slotNumber == 0) {
            return new Location { position = Vector3.zero, orientation = 0f };
        }

        // Slot 5 es un caso especial: Retaguardia (siempre al final del eje central)
        // Para que sea escalable, lo movemos al final de la columna central según el tamaño
        if (slotNumber == 5) {
            // Lo ponemos un poco más atrás de la última fila lateral actual
            return new Location { position = new Vector3(0, 0, -5f), orientation = 180f };
        }

        // Para el resto (escalable en V):
        // Los impares van a la izquierda, los pares a la derecha
        bool esIzquierda = (slotNumber % 2 != 0);
        
        // Calculamos en qué "nivel" de la V está (1, 2, 3...)
        // Usamos slotNumber + 1 / 2 para que los pares e impares compartan fila
        int fila = (slotNumber + 1) / 2;

        float xPos = esIzquierda ? -fila * separacionX : fila * separacionX;
        float zPos = -fila * separacionZ;

        // Orientación: los de la izquierda miran un poco hacia afuera (45°), 
        // los de la derecha hacia el otro lado (-45°)
        float orientation = esIzquierda ? 45f : -45f;

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