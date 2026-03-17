using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormacionTrianguloEqui : FormationPattern
{
    // Separación entre agentes
    public float separacion = 2f;

    public override Location GetSlotLocation(int slotNumber) {
        // Slot 0 es el líder en el vértice (0,0)
        if (slotNumber == 0) {
            return new Location { position = Vector3.zero, orientation = 0f };
        }

        int totalAgentes = numberOfSlots + 1;
        bool esTrianguloCompleto = (totalAgentes % 3 == 0);

        //Esta formación es especial, ya que hacemos lo siguiente: si el numero de slots es
        //divisible entre 3, entonces se forma el triangulo completo.
        //Si no, se forman solo dos lados del triangulo, haciendo una V inversa, con el lider en el vertice.

        float xPos = 0f;
        float zPos = 0f;
        float orientation = 0f;

        //Debug.Log("Numero de Personajes: " + numberOfSlots);
        if(esTrianguloCompleto){

            //Podemos formar un triangulo.
            int k = totalAgentes / 3; // Número de filas completas
            int agentesEnV = 2 * k; // Agentes que forman la V (los primeros 2/3)

            //Constantes para formar un triangulo de forma equiangular
            float sin30 = 0.5f;
            float cos30 = 0.8660254f;
            
            //Los 2 primeros tercios de los slots forman la v inversa del triangulo.
            if(slotNumber <= agentesEnV){
                bool esIzquierda = (slotNumber % 2 != 0);
                int fila = (slotNumber + 1) / 2;

                xPos = (esIzquierda ? -sin30 : sin30) * fila * separacion;
                zPos = -cos30 * fila * separacion;
                orientation = esIzquierda ? 45f : -45f;
            }
            else{
                //Son los agentes de la base del triangulo,
                //Indice dentro de la base
                int indiceBase = slotNumber - (2 * k); 
                int filaBase = k;

                //Empezamos desde la esquina izquierda y vamos sumando X hacia la derecha
                xPos = (-sin30 * k * separacion) + (indiceBase * separacion);
                zPos = -cos30 * k * separacion;

                //Los ponemos mirando hacia atrás.
                orientation = 180f;
            }
            
        }else{
            //Formamo la V inversa
            // Los impares van a la izquierda, los pares a la derecha
            bool esIzquierda = (slotNumber % 2 != 0);
            // Calculamos en qué "nivel" de la V está (1, 2, 3...)
            // Usamos slotNumber + 1 / 2 para que los pares e impares compartan fila
            int fila = (slotNumber + 1) / 2;

            xPos = esIzquierda ? -fila * separacion : fila * separacion;
            zPos = -fila * separacion;

            // Orientación: los de la izquierda miran un poco hacia afuera (45°), 
            // los de la derecha hacia el otro lado (-45°)
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