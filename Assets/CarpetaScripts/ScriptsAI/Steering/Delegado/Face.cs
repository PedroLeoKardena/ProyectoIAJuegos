using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Face : Align
{

    private GameObject auxTargetObj;
    private Agent auxTargetAgent;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        this.nameSteering = "Face";

        //Auxtarget es el target del Align        
        auxTargetObj = new GameObject("FaceGhost");
        auxTargetAgent = auxTargetObj.AddComponent<AgentNPC>();
        auxTargetAgent.drawGizmos = false;
    }

    private void OnDestroy()
    {
        // Limpieza
        if (auxTargetObj != null) Destroy(auxTargetObj);
    }

    // Update is called once per frame
    public override Steering GetSteering(Agent agent)
    {
        // Si no hay target, no podemos mirar a nadie
        if (this.target == null){
            return new Steering();
        }

        Vector3 direction = target.Position - agent.Position;

        if (direction.magnitude == 0){
            return new Steering();
        }
        
        float targetOrientation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        
        auxTargetAgent.Orientation = targetOrientation;
        // Guardamos el target REAL en una variable temporal
        Agent realTarget = this.target;

        // Cambiamos el target del Seek (this.target) por el Fantasma
        this.target = auxTargetAgent;

        // Llamamos al Seek original (base) para que calcule la fuerza hacia el fantasma
        Steering steer = base.GetSteering(agent);

        this.target = realTarget;

        return steer;
    }
}
