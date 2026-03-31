using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Separation : SteeringBehaviour
{
    [SerializeField]
    public float threshold = 5f; // Distancia mínima para considerar que un agente está demasiado cerca
    
    public float decayCoefficient = 100f; // Coeficiente de decaimiento para la fuerza de separación
    // Start is called before the first frame update
    
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

        // Si el agente actual es el líder, no calculamos separación para él
        if (agent.CompareTag("Lider")) { return steer; }

        Collider[] closeEntities = Physics.OverlapSphere(agent.Position, threshold);

        foreach (Collider col in closeEntities)
        {
            Agent neighbor = col.GetComponent<Agent>();
            if (neighbor == null || neighbor == agent) continue; // No consideramos al propio agente como vecino


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
        else {
            //Si no hay vecinos cercanos y vamos muy lentos, no aplicamos ninguna fuerza de separación
            steer.linear = Vector3.zero;
        }

        steer.angular = 0;
        return steer;
    }
}
