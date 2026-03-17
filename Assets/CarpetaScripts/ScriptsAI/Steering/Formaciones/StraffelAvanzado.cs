using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StraffelAvanzado : FormationPattern
{
    // Separación entre agentes
    [Header("Separación entre agentes")]
    public float separacionX = 1.5f;
    public float separacionZ = 1.5f;

    //Esta formación será como la StraffelFormation, pero con cambios.
    //En vez de formar ws solamente, lo que haremos será añadir personajes de vanguardia y retaguardia
    //en los "huecos" de las ws.
    [Header("Separación para vanguardia y retaguardia")]
    public float separacionVanguardia = 3.5f; //3.5 metros adelante con respecto a la linea del lider (eje z)
    public float separacionRetaguardia = -7.5f; //7.5 metros atrás con respecto a la linea del lider (eje z)

    public override Location GetSlotLocation(int slotNumber) {
        // Slot 0 es el líder en el vértice (0,0)

        if (slotNumber == 0) {
            return new Location { position = Vector3.zero, orientation = 0f };
        }

        //El slot 1 será el de la retaguardia del lider.
        if (slotNumber == 1) {
            return new Location { 
                position = new Vector3(0f, 0f, separacionRetaguardia), 
                orientation = 0f 
            };
        }

        //Para hacer esta formación, lo que haremos será seguir el siguiente planteamiento.
        //Nosotros intentaremos formar una w como la de antes. Cuando en los valles (picos inferiores)
        //se coloque un agente, colocaremos a su agente de vanguardia respectivo, y cuando en el pico superior
        //se coloque un agente, colocaremos a su agente de retaguardia respectivo.
        //Es decir para formar un w completa necesitamos 12 agentes.

        //Por esto, hay que tener en cuenta que en los picos y en los valles siguen habiendo agentes.
        
        int agenteIndex = slotNumber - 2;

        //Los pares a la izquierda, los impares a la derecha
        bool esIzquierda = (agenteIndex % 2 == 0); 
        
        //Agrupamos en pares. Esto nos permite mantener la simetría entre izquierda y derecha
        int parIndex = agenteIndex / 2; //De este modo, el agente 2 y 3 forman el par 0, el agente 4 y 5 forman el par 1, etc.
        //Con esto lo que conseguimos es que se forme todo de forma simétrica

        //El rol lo determinamos teniendo en cuanta la forma de la w. Construimos ciclos de 6 pasos con los pares: 
        // 0 = ambas pendientes descendientes (por ambos lados), 1 = ambos valles,
        // 2 = ambas vanguardias frente a los valles,  3 = ambas pendientes ascientes,
        // 4 = ambos picos, 5 = ambas retaguardias frente a los picos
        int ciclo = parIndex / 6;
        int paso = parIndex % 6;
        

        //UnidadesX nos sirve para saber el eje en el que se encuentra el agente.
        int unidadesX = 0; 
        float zPos = 0f;
        float orientation = 0f;

        switch (paso){
            case 0: //Pendiente descendente
                unidadesX = 1;
                zPos = -separacionZ;
                orientation = esIzquierda ? 45f : -45f;
                break;
            case 1: //Valle
                unidadesX = 2;
                zPos = -separacionZ * 2;
                orientation = 0f;
                break;
            case 2: //Vanguardia frente al valle
                unidadesX = 2;
                zPos = separacionVanguardia;
                orientation = 0f;
                break;
            case 3: //Pendiente ascendente
                unidadesX = 3;
                zPos = -separacionZ;
                orientation = esIzquierda ? -45f : 45f;
                break;
            case 4: //Pico
                unidadesX = 4;
                zPos = 0f;
                orientation = 0f;
                break;
            case 5: //Retaguardia frente al pico
                unidadesX = 4;
                zPos = separacionRetaguardia;
                orientation = 0f;
                break;
        }

        //Calculamos la posición X en base a las unidadesX y al ciclo.
        float xPos = (unidadesX + (ciclo * 4)) * separacionX;

        // Invertimos el signo si va a la izquierda
        if (esIzquierda) xPos = -xPos;

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