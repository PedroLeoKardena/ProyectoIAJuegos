using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pursue : Seek
{

    public float MaxPrediction = 1.0f;

    //Target auxiliar para el seek
    private GameObject auxTargetObj;
    private Agent auxTargetAgent;

    // Start is called before the first frame update
    void Start()
    {
        this.nameSteering = "Pursue";

        //Auxtarget es el target del Seek
        auxTargetObj = new GameObject("PursueGhost");
        auxTargetAgent = auxTargetObj.AddComponent<AgentNPC>();
    }

    private void OnDestroy()
    {
        // Limpieza
        if (auxTargetObj != null) Destroy(auxTargetObj);
    }

    // Update is called once per frame
    public override Steering GetSteering(Agent agent)
    {
        Vector3 direction = target.Position - agent.Position;
        float distance = direction.magnitude;

        float speed = agent.Speed;

        float prediction;
        //Vemos si la velocidad es muy baja para evitar division por 0
        if(speed <= distance / MaxPrediction)
        {
            prediction = MaxPrediction;
        }
        else
        {
            prediction = distance / speed;
        }

        //target es el target en si de Pursue
        Vector3 futurePosition = target.Position + target.Velocity * prediction;

        auxTargetAgent.Position = futurePosition;

        // Guardamos el target REAL en una variable temporal
        Agent realTarget = this.target;

        // Cambiamos el target del Seek (this.target) por el Fantasma
        this.target = auxTargetAgent;

        // Llamamos al Seek original (base) para que calcule la fuerza hacia el fantasma
        Steering steer = base.GetSteering(agent);

        // Restauramos el target real (para no romper nada en el siguiente frame)
        this.target = realTarget;

        return steer;
    }

    // Para depuración: Dibuja hacia dónde cree que va a ir el target
    void OnDrawGizmos()
    {
        if(auxTargetAgent != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(auxTargetAgent.Position, 0.5f);
            Gizmos.DrawLine(target.Position, auxTargetAgent.Position);
        }
    }
}
