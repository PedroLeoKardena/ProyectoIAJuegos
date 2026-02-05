using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flee : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour
    public float panicDistance = 5f;
    
    void Start()
    {
        this.nameSteering = "Flee";
    }


    public override Steering GetSteering(Agent agent)
    {
        //Mismo que seek pero en direccion contraria
        Steering steer = new Steering();

        // Calcula el steering.
        Vector3 direccion = agent.Position - target.Position;
        float distancia = direccion.magnitude;
        if(distancia > panicDistance)
        {
            //Si la velocidad es muy baja, detenemos el agente completamente
            if (agent.Speed < 0.1f)
            {
                agent.Velocity = Vector3.zero; // Frenado total (trampa física opcional)
                steer.linear = Vector3.zero;
                steer.angular = 0;
                return steer;
            }

            float timeToBrake = 0.5f; // Tiempo en segundos para detenerse suavemente
            steer.linear = -agent.Velocity / timeToBrake;

            // Limitamos la frenada a la capacidad de frenado del coche (MaxAcceleration)
            if (steer.linear.magnitude > agent.MaxAcceleration)
            {
                steer.linear = steer.linear.normalized * agent.MaxAcceleration;
            }

            steer.angular = 0;
            return steer;
        }
        
        //normalizamos la direccion
        direccion = direccion.normalized;
        
        //aplicamos la aceleracion maxima
        direccion *= agent.MaxAcceleration;


        steer.linear = direccion;
        steer.angular = 0;
        // Retornamos el resultado final.
        return steer;
    }
}