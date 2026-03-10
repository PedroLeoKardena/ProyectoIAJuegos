using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Align : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour
    // Tiempo en el que tenemos que llegar a la velocidad del target.
    float timeToTarget = 0.5f; // Aumentado para evitar oscilaciones bruscas
    

    void Start()
    {
        this.nameSteering = "Align";
    }


    public override Steering GetSteering(Agent agent)
    {
        float targetOr;
        float currentInteriorAngle;
        float currentExteriorAngle;

        // Extracción de datos dependiendo de si es un target asignado o es por una formación
        if (this.target != null) 
        {
            // Escena de Test: Usamos el objeto físico asignado
            targetOr = this.target.Orientation;
            currentInteriorAngle = agent.InteriorAngle;
            currentExteriorAngle = agent.ExteriorAngle;
        } 
        else 
        {
            // Escena de Formación: Usamos la orientación enviada por el FormationManager [cite: 77, 84]
            targetOr = ((AgentNPC)agent).TargetFormacion.orientation;
            
            // Valores por defecto para formación si el agente no tiene radios definidos
            currentInteriorAngle = 1.0f; // Grados de margen para detenerse
            currentExteriorAngle = 5.0f; // Grados de margen para empezar a frenar
        }

        Steering steer = new Steering();

        // Calcula el steering.

        float rotacion = targetOr - agent.Orientation;
        rotacion = Bodi.MapToRange(rotacion);

        float tamañoRotacion = Mathf.Abs(rotacion);

        if (tamañoRotacion < currentInteriorAngle){
            //Si estamos dentro del circulo tenemos que intentar quedarnos parados mirando
            //al target
            steer.angular = -agent.Rotation / timeToTarget;
            
            // Limitamos esta frenada también por seguridad
            float frenadaAbs = Mathf.Abs(steer.angular);
            if (frenadaAbs > agent.MaxAngularAcc)
            {
                steer.angular /= frenadaAbs;
                steer.angular *= agent.MaxAngularAcc;
            }
            
            steer.linear = Vector3.zero;
            return steer;
        }

        float targetRotacion;
        if (tamañoRotacion > currentExteriorAngle){
            targetRotacion = agent.MaxRotation;
        }else{
            targetRotacion = agent.MaxRotation * tamañoRotacion / currentExteriorAngle;
        }

        targetRotacion *= rotacion / tamañoRotacion;

        steer.angular = targetRotacion - agent.Rotation;
        steer.angular /= timeToTarget;

        float angularAcc = Mathf.Abs(steer.angular);

        if(angularAcc > agent.MaxAngularAcc){
            steer.angular /= angularAcc;
            steer.angular *= agent.MaxAngularAcc;
        }

        steer.linear = Vector3.zero;
        // Retornamos el resultado final.
        return steer;
    }
}