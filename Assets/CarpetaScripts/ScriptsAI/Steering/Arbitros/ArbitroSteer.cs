using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ArbitroSteer : MonoBehaviour
{
    protected AgentNPC agent; //Mi agenteNPC que llama al getSteering
  

    [SerializeField] protected bool debug = false; //Debug flag

    [SerializeField] protected Steering finalSteering;


    [SerializeField]
    protected List<SteeringBehaviour> steeringList; // Lista con todos los steering

    // Usamos Awake para crer los steering necesarios antes de cualquier Start
    protected virtual void Awake()
    {
        agent = GetComponent<AgentNPC>();

        // Añadimos los Steering a la lista
        steeringList = new List<SteeringBehaviour>();
        
        SteeringBehaviour[] behaviours = GetComponents<SteeringBehaviour>();

        foreach(var b in behaviours)
        {
            steeringList.Add(b);
        }

    }

    public abstract Steering GetSteering();

    protected virtual void OnDrawGizmos()
    {
        if (!debug) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, finalSteering.linear);
    }
}
