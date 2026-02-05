using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seek_IanMillington : SteeringBehaviour
{


    public virtual void Awake()
    {
        nameSteering = "Seek Ian Millingt.";
    }


    public override Steering GetSteering(Agent agent)
    {
        Steering steer = new Steering();

        //Este es el seek que pone en las diapos, es el de Ian Millington
        Vector3 direccion = target.Position - agent.Position;
        //normalizamos la direccion
        direccion = direccion.normalized;
        //aplicamos la aceleracion maxima
        direccion *= agent.MaxAcceleration;

        steer.linear = direccion;
        steer.angular = 0;

        steer.angular = 0; // NO genera acleración angular


        return steer;
    }
}
