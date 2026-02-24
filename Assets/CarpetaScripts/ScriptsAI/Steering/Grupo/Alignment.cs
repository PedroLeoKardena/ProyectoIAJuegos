using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Alignment : SteeringBehaviour
{
    [SerializeField]
    public List<Agent> neighbors;

    [SerializeField]
    public float threshold = 5f;


    void Start()
    {
        this.nameSteering = "Alignment";
        this.target = null; // Alignment no necesita un target específico
    }

    public override Steering GetSteering(Agent agent){
        Steering steer = new Steering();
        
        float averageHeading = 0;

        int count = 0;

        foreach (Agent neighbor in neighbors){
            if (agent == neighbor || neighbor == null) continue;

            Vector3 direction = neighbor.Position - agent.Position;
            float distance = direction.magnitude;

            if (distance <= threshold)
            {
                averageHeading += neighbor.Heading(); 
                count++;
            }
        }

        if (count > 0){
            averageHeading /= count;

            float desiredRotation = averageHeading - agent.Heading();
            
            float timeToTarget = 0.1f; // Tiempo para alcanzar la orientación deseada
            float targetRotationVelocity = desiredRotation / timeToTarget;
            
            // Limitar la aceleración angular al máximo permitido por el agente

            targetRotationVelocity = Mathf.Clamp(targetRotationVelocity, -agent.MaxAngularAcc, agent.MaxAngularAcc);

            steer.angular = (targetRotationVelocity - agent.Rotation) / timeToTarget;

        }else{
            steer.angular = 0;
        }

        steer.linear = Vector3.zero;
        return steer;
    }
}