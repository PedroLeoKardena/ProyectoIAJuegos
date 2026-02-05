using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VelocityMatching : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour
    float timeToTarget = 0.1f; 
    
    void Start()
    {
        this.nameSteering = "VelocityMatching";
    }


    public override Steering GetSteering(Agent agent)
    {
        Steering steer = new Steering();

        // Calcula el steering.

        //Intentamos alcanzar la velocidad del target
        steer.linear = target.Velocity - agent.Velocity;
        steer.linear /= timeToTarget;

        //Comprobación de que la aceleración es muy rápida
        if (steer.linear.magnitude > agent.MaxAcceleration){
            steer.linear = steer.linear.normalized * agent.MaxAcceleration;
        }

        steer.angular = 0;

        // Retornamos el resultado final.
        return steer;
    }
}