using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Separation : SteeringBehaviour
{
    [SerializeField]
    public float threshold = 5f; // Distancia mínima para considerar que un agente está demasiado cerca
    
    public float decayCoefficient = 100f; // Coeficiente de decaimiento para la fuerza de separación
    // Start is called before the first frame update

    [SerializeField]
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
        Vector3 totalSeparationForce = Vector3.zero;
        int count = 0; // Contador de vecinos cercanos

        foreach (Agent neighbor in neighbors)
        {
            if (agent == neighbor || neighbor == null) continue; // No consideramos al propio agente como vecino


            //Comprobamos si el vecino está dentro del umbral de separación
            //Calculamos la dirección desde el vecino hacia el agente.
            Vector3 direction = neighbor.Position - agent.Position;
            float distance = direction.magnitude;

            if (distance < threshold){
                //Calculamos la fuerza de repulsión/separación
                float strength = Mathf.Min(decayCoefficient / (distance * distance), agent.MaxAcceleration);
                //Hay un vecino demasiado cerca, incrementamos el contador
                count++;
                //Añadimos la aceleracion de separación al steering
                totalSeparationForce -= direction.normalized * strength; 
            }
        }

        if (count > 0){
            //Si hay vecinos cercanos, aplicamos la fuerza de separación promedio
            steer.linear = totalSeparationForce / count;
        }
        else if(agent.Speed > 0.1f) {
            float timeToBrake = 0.2f; 
            steer.linear = -agent.Velocity / timeToBrake;
            
            if (steer.linear.magnitude > agent.MaxAcceleration)
            {
                steer.linear = steer.linear.normalized * agent.MaxAcceleration;
            }
        }
        else {
            //Si no hay vecinos cercanos y vamos muy lentos, no aplicamos ninguna fuerza de separación
            agent.Velocity = Vector3.zero; // Frenado total (trampa física opcional)
            steer.linear = Vector3.zero;
        }

        steer.angular = 0;
        return steer;
    }
}
