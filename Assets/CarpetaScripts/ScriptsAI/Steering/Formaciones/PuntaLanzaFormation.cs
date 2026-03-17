using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuntaLanzaFormation : FormationPattern
{
    // Separación entre agentes
    public float separacionX = 1.5f;
    public float separacionZ = 1.5f;

    public override Location GetSlotLocation(int slotNumber) {
        // Líder (Vértice)
        if (slotNumber == 0) {
            return new Location { position = Vector3.zero, orientation = 0f };
        }

        // Lógica para los laterales (V)
        int slotParaCalculoV = slotNumber;
        if (slotNumber > 5) slotParaCalculoV = slotNumber - 1; 

        int fila = (slotParaCalculoV + 1) / 2;
        bool esIzquierda = (slotParaCalculoV % 2 != 0);

        // Slot 5 (Retaguardia central)
        if (slotNumber == 5) {
            // Lo bajamos un nivel más que la fila 2 (donde están el 3 y el 4)
            return new Location { 
                position = new Vector3(0, 0, -2 * separacionZ - 1.0f), 
                orientation = 180f 
            };
        }

        // Posición para el resto de la V
        float xPos = esIzquierda ? -fila * separacionX : fila * separacionX;
        float zPos = -fila * separacionZ;
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