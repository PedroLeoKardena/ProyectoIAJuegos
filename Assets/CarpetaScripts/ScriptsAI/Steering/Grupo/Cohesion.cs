using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cohesion : Seek
{

    [SerializeField]
    public List<Agent> neighbors;

    [SerializeField]
    public float threshold = 5f;

    private GameObject auxTargetObj;
    private Agent auxTargetAgent;

    // Start is called before the first frame update
    void Start()
    {
        this.nameSteering = "Cohesion";

        auxTargetObj = new GameObject("CohesionGhost");
        auxTargetAgent = auxTargetObj.AddComponent<AgentNPC>(); 
        auxTargetAgent.drawGizmos = false;
    }

    private void OnDestroy()
    {
        // Limpiamos memoria
        if (auxTargetObj != null) Destroy(auxTargetObj);
    }


    // Update is called once per frame
    public override Steering GetSteering(Agent agent){
        
        Vector3 centerOfMass = Vector3.zero;

        int count = 0;

        foreach (Agent neighbor in neighbors){
            if (agent == neighbor || neighbor == null) continue;

            Vector3 direction = neighbor.Position - agent.Position;
            float distance = direction.magnitude;

            if (distance > threshold) continue;

            centerOfMass += neighbor.Position;
            count++;
        }

        if (count == 0){
            return new Steering();
        }

        centerOfMass /= count;

        //Delegamos comportamiento
        auxTargetAgent.Position = centerOfMass;
        Agent realTarget = this.target;

        this.target = auxTargetAgent;

        Steering steer = base.GetSteering(agent);

        this.target = realTarget;

        return steer;
    }
}
