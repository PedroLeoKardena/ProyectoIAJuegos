using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Steering/Polybank/Jump")]
public class Jump : VelocityMatching
{
    // Punto de salto a usar
    public JumpPoint jumpPoint;

    // Controla si el salto es posible de realizar
    bool canAchieve = false;

    // Velocidad máxima vertical de salto
    public float maxYVelocity = 10f;

    public float maxSpeed = 10f; 

    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    private GameObject projectileTargetObj; // GameObject auxiliar para el target
    private Agent projectileTarget; //Agent auxiliar

    void Start()
    {
        this.NameSteering = "Jump";
        
        // Crea un gameObject oculto para actuar como target para VelocityMatching
        projectileTargetObj = new GameObject("JumpTarget_Internal");

        //Vamos a probar inicialmente sin esto:  projectileTargetObj.hideFlags = HideFlags.HideInHierarchy;
        projectileTarget = projectileTargetObj.AddComponent<Agent>();
        
        // Asigna el target de la clase base al nuestro interno
        this.target = projectileTarget;
    }

    void OnDestroy()
    {
        if (projectileTarget != null)
        {
            if (Application.isPlaying) Destroy(projectileTargetObj);
            else DestroyImmediate(projectileTargetObj);
        }
    }

    public override Steering GetSteering(Agent agent)
    {
        // Comprueba si tenemos una trayectoria, y crea una si no.
        if (!canAchieve)
        {
            CalculateTarget(agent);
        }

        // Comprueba si la trayectoria es cero (si el cálculo falló)
        if (!canAchieve)
        {
            return new Steering(); // Sin steering
        }

        // Comprueba si hemos alcanzado el punto de salto
        //Usamos jumpPoint por que se trata de un lugar fijo. Podríamos usar target.Position pero el salto se haría justo al llegar a ese punto, y queremos que se haga al llegar al punto de salto, no al de aterrizaje.
        bool nearPos = Vector3.Distance(agent.Position, jumpPoint.jumpLocation) < 1f;
        
        Vector3 agentVelPlane = agent.Velocity; 
        agentVelPlane.y = 0;
        Vector3 targetVelPlane = target.Velocity; 
        targetVelPlane.y = 0;
        bool nearVel = Vector3.Distance(agentVelPlane, targetVelPlane) < 1f;
        
        if (nearPos && nearVel)
        {
            Debug.Log("Llegamos al punto de salto");
            ScheduleJumpAction(agent);
            return new Steering();
        }

        // Delega el steering al VelocityMatch base
        return base.GetSteering(agent);
    }
    
    private void CalculateTarget(Agent agent)
    {
        // El target fantasma se coloca en el punto de aterrizaje
        projectileTarget.Position = jumpPoint.landingLocation;
        
        // Ecuación de la diapositiva 22 del tema 6
        float sqrtTermArg = 2 * gravity.y * jumpPoint.deltaPosition.y + maxYVelocity * maxYVelocity;
        
        if (sqrtTermArg < 0) {
            Debug.LogWarning("Salto imposible: No hay suficiente fuerza vertical.");
            return; 
        }

        float sqrtTerm = Mathf.Sqrt(sqrtTermArg);

        float time1 = (-maxYVelocity - sqrtTerm) / gravity.y;
        float time2 = (-maxYVelocity + sqrtTerm) / gravity.y;

        // Comprueba si podemos usarlo
        if (!CheckJumpTime(time1))
        {
            // De lo contrario prueba el otro tiempo
            if (!CheckJumpTime(time2))
            {
                canAchieve = false;
            }
        }
    }

    private bool CheckJumpTime(float time)
    {
        if (time <= 0.001f) return false;

        // Calcula la velocidad planar
        Vector3 deltaPos = jumpPoint.deltaPosition;
        float vx = deltaPos.x / time;
        float vz = deltaPos.z / time; 
        float speedSq = vx * vx + vz * vz;
        
        if (speedSq < maxSpeed * maxSpeed)
        {
            // Tenemos una solución válida. Configuramos la velocidad del target fantasma y marcamos que podemos realizar el salto.
            projectileTarget.Velocity = new Vector3(vx, 0, vz);
            canAchieve = true;

            return true;
        }
        return false;
    }

    private void ScheduleJumpAction(Agent agent)
    {
        Debug.Log("JUMP!");
        Vector3 velocity = agent.Velocity;
        velocity.y = maxYVelocity;
        agent.Velocity = velocity;
    }
    
    /*private bool IsNear(Vector3 a, Vector3 b, Agent agent)
    {
        return Vector3.Distance(a, b) < 1f; 
    }
    */
    
}

[System.Serializable]
public struct JumpPoint
{
    public Vector3 jumpLocation;
    public Vector3 landingLocation;

    // Ayudante para obtener delta
    public Vector3 deltaPosition
    {
        get { return landingLocation - jumpLocation; }
    }
}
