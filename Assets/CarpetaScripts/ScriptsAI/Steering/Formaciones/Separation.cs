using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Separation : SteeringBehaviour
{

    public float threshold = 5f; // Distancia mínima para considerar que un agente está demasiado cerca
    public float decayCoefficient = 100f; // Coeficiente de decaimiento para la fuerza de separación
    // Start is called before the first frame update
    public List<Agent> neighbors; // Lista de agentes vecinos que se deben considerar para la separación
    
    void Start()
    {
        this.nameSteering = "Separation";
        this.target = null; // Separation no necesita un target específico
    }

    // Update is called once per frame
    public override Steering GetSteering(Agent agent)
    {
        Steering steer = new Steering();

        foreach (Agent neighbor in neighbors)
        {
            if (neighbor == agent) continue; // No consideramos al propio agente como vecino


            //Comprobamos si el vecino está dentro del umbral de separación
            //Calculamos la dirección desde el vecino hacia el agente.
            Vector3 direction = neighbor.Position - agent.Position;
            float distance = direction.magnitude;

            if (distance < threshold){
                //Calculamos la fuerza de repulsión/separación
                float strength = Mathf.Min(decayCoefficient / (distance * distance), agent.MaxAcceleration);
                
                //Añadimos la aceleracion de separación al steering
                steer.linear -= direction.normalized * strength; 
            }
        }

        return steer;
    }
}
