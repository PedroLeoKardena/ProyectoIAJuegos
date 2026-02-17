using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seek : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour
    
    
    void Start()
    {
        this.nameSteering = "Seek";
    }


    public override Steering GetSteering(Agent agent)
    {
        Steering steer = new Steering();

        // Calcula el steering.
        Vector3 direccion = target.Position - agent.Position;
        direccion.y = 0; // Importante para no hundirse en el suelo
        //Calculamos la direccion deseada
        Vector3 desiredVelocity = direccion.normalized * agent.MaxSpeed;

        steer.linear = desiredVelocity - agent.Velocity;
        steer.angular = 0;
        // Retornamos el resultado final.
        return steer;
    }
}