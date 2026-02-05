using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrive : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour

    // Tiempo en el que tenemos que llegar a la velocidad del target.
    float timeToTarget = 0.1f;
    
    void Start()
    {
        this.nameSteering = "Arrive";
    }


    public override Steering GetSteering(Agent agent)
    {

       

        Steering steer = new Steering();

        Vector3 direccion = target.Position - agent.Position;

        float distancia = direccion.magnitude;

        if (distancia < target.InteriorRadius){

            // Queremos detenernos por completo (Velocidad Deseada = 0)
            // Fórmula: Aceleración = (VelocidadDeseada - VelocidadActual) / tiempo
            
            steer.linear = -agent.Velocity / timeToTarget;
            
            if (steer.linear.magnitude > agent.MaxAcceleration)
            {
                steer.linear = steer.linear.normalized * agent.MaxAcceleration;
            }
            
            steer.angular = 0;
            return steer;
        }        
        
        float targetSpeed;
        if (distancia > target.ArrivalRadius){
            //Si estamos fuera del Arrival Radius, vamos a maxima velocidad
            targetSpeed = agent.MaxSpeed;
        }else{
            //Si no, calculamos una velocidad escalada
            targetSpeed = agent.MaxSpeed * distancia / target.ArrivalRadius;
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