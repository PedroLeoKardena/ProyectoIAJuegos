using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Leave : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour
    // Tiempo en el que tenemos que llegar a la velocidad del target.
    float timeToTarget = 0.1f;
    
    void Start()
    {
        this.nameSteering = "Leave";
    }


    public override Steering GetSteering(Agent agent)
    {

        Steering steer = new Steering();

        Vector3 direccion = agent.Position - target.Position;

        float distancia = direccion.magnitude;

        if (distancia > target.ArrivalRadius){
            return null; //llegamos
        }        
        
        float targetSpeed;
        if (distancia < target.InteriorRadius){
            //Si estamos fuera del Arrival Radius, vamos a maxima velocidad
            targetSpeed = agent.MaxSpeed;
        }else{
            //Si no, calculamos una velocidad escalada
            targetSpeed = agent.MaxSpeed * (target.ArrivalRadius - distancia) / target.ArrivalRadius;
        }

        //Cambiamos la velocidad del target
        Vector3 direccionNorm = direccion.normalized;


        Vector3 desiredVelocity = direccionNorm * targetSpeed;

        //Aceleración intenta alcanzar la velocidad del target.
        steer.linear = desiredVelocity - agent.Velocity;
        steer.linear /= timeToTarget;

        //Vemos si la acelaración es muy rápida. (Esto no hace falta ya que de base lo hacemos)        
        if(steer.linear.magnitude > agent.MaxAcceleration){
            steer.linear = steer.linear.normalized;
            steer.linear *= agent.MaxAcceleration;
        }

        steer.angular = 0;

        // Retornamos el resultado final.
        return steer;
    }
}