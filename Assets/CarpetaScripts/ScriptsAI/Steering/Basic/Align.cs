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
        Steering steer = new Steering();

        float interiorAngle = agent.InteriorAngle;
        float exteriorAngle = agent.ExteriorAngle;

        // Calcula el steering.

        float rotacion = target.Orientation - agent.Orientation;
        rotacion = Bodi.MapToRange(rotacion);

        float tamañoRotacion = Mathf.Abs(rotacion);

        if (tamañoRotacion < interiorAngle){
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
        if (tamañoRotacion > exteriorAngle){
            targetRotacion = agent.MaxRotation;
        }else{
            targetRotacion = agent.MaxRotation * tamañoRotacion / exteriorAngle;
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