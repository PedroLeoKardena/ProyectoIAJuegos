using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntiAlign : SteeringBehaviour
{

    // Declara las variables que necesites para este SteeringBehaviour

    float timeToTarget = 0.1f;
    void Start()
    {
        this.nameSteering = "AntiAlign";
    }


    public override Steering GetSteering(Agent agent)
    {
        Steering steer = new Steering();

        // Calcula el steering.
        float interiorAngle = agent.InteriorAngle;
        float exteriorAngle = agent.ExteriorAngle;

        // Calcula el steering.

        //Lo unico que cambiamos es que en vez de mirar al mismo lado que le target target miramos en la direccion opuesta
        float targetOrientationOpposite = target.Orientation + 180f;
        float rotacion = targetOrientationOpposite - agent.Orientation;
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
        // Retornamos el resultado final.
        return steer;
    }
}