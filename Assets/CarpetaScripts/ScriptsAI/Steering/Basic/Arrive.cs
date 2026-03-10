using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrive : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour

    // Tiempo en el que tenemos que llegar a la velocidad del target.
    float timeToTarget = 0.1f;

    // Definimos radios por defecto para cuando no hay un Target asignado
    public float arrivalRadiusFormacion = 2.0f;
    public float interiorRadiusFormacion = 0.5f;
    
    void Start()
    {
        this.nameSteering = "Arrive";
    }


    public override Steering GetSteering(Agent agent)
    {
        Vector3 targetPos;
        float currentArrivalRadius;
        float currentInteriorRadius;

        // Extracción de datos dependiendo de si es un target asignado o es por una formación
        if (this.target != null) 
        {
            // Escena de Test: Usamos el objeto asignado
            targetPos = this.target.Position;
            currentArrivalRadius = this.target.ArrivalRadius;
            currentInteriorRadius = this.target.InteriorRadius;
        } 
        else 
        {
            // Escena de Formación: Usamos el punto calculado por el FormationManager
            targetPos = ((AgentNPC)agent).TargetFormacion.position;
            currentArrivalRadius = arrivalRadiusFormacion;
            currentInteriorRadius = interiorRadiusFormacion;
        }

        Steering steer = new Steering();

        Vector3 direccion = targetPos - agent.Position;

        float distancia = direccion.magnitude;

        if (distancia < currentInteriorRadius){

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
        if (distancia > currentArrivalRadius){
            //Si estamos fuera del Arrival Radius, vamos a maxima velocidad
            targetSpeed = agent.MaxSpeed;
        }else{
            //Si no, calculamos una velocidad escalada
            targetSpeed = agent.MaxSpeed * distancia / currentArrivalRadius;
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