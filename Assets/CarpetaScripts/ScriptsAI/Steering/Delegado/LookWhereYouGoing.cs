using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookWhereYouGoing : Align
{

    private GameObject auxTargetObj;
    private Agent auxTargetAgent;

    void Start()
    {
        this.nameSteering = "LookWhereYouGoing";

        // Creamos el "Fantasma" interno
        auxTargetObj = new GameObject("LookWhereGhost");
        auxTargetAgent = auxTargetObj.AddComponent<AgentNPC>();
        
        // Desactivamos gizmos del fantasma para que no molesten (Opción rápida)
        auxTargetObj.SetActive(false);
    }

    // Limpieza de memoria
    void OnDestroy()
    {
        if (auxTargetObj != null) Destroy(auxTargetObj);
    }


    // Update is called once per frame
    public override Steering GetSteering(Agent agent)
    {
        if(agent.Speed == 0){
            return null;
        }

        float targetOrientation = Mathf.Atan2(agent.Velocity.x, agent.Velocity.z) * Mathf.Rad2Deg;
        
        auxTargetAgent.Orientation = targetOrientation;
        // Guardamos el target REAL en una variable temporal
        Agent realTarget = this.target;

        // Cambiamos el target del Seek (this.target) por el Fantasma
        this.target = auxTargetAgent;

        Steering steer = base.GetSteering(agent);

        this.target = realTarget;
        
        return steer;
    }   
}
