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

    private Agent projectileTarget;

    void Start()
    {
        this.NameSteering = "Jump";
        // Crea un gameObject oculto para actuar como target para VelocityMatching
        GameObject go = new GameObject("JumpTarget_Internal");
        go.hideFlags = HideFlags.HideInHierarchy;
        projectileTarget = go.AddComponent<Agent>();
        
        // Asigna el target de la clase base al nuestro interno
        this.target = projectileTarget;
    }

    void OnDestroy()
    {
        if (projectileTarget != null)
        {
            if (Application.isPlaying) Destroy(projectileTarget.gameObject);
            else DestroyImmediate(projectileTarget.gameObject);
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
        bool nearPos = IsNear(agent.Position, target.Position, agent);
        bool nearVel = IsNear(agent.Velocity, target.Velocity, agent);
        
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
        // Referencia al target interno
        target = projectileTarget;
        
        target.Position = jumpPoint.jumpLocation;
        
        // Calcula el tiempo del primer salto
        float sqrtTerm = Mathf.Sqrt(2 * gravity.y * jumpPoint.deltaPosition.y + maxYVelocity * maxYVelocity);
        
        float time = (-maxYVelocity - sqrtTerm) / gravity.y;

        // Comprueba si podemos usarlo
        if (!CheckJumpTime(time))
        {
            // De lo contrario prueba el otro tiempo
            time = (-maxYVelocity + sqrtTerm) / gravity.y;
            
            CheckJumpTime(time);
        }
    }

    private bool CheckJumpTime(float time)
    {
        if (time <= 0) return false;

        // Calcula la velocidad planar
        Vector3 deltaPos = jumpPoint.deltaPosition;
        float vx = deltaPos.x / time;
        float vz = deltaPos.z / time; 
        float speedSq = vx * vx + vz * vz;
        
        if (speedSq < maxSpeed * maxSpeed)
        {
            // Tenemos una solución válida
            Vector3 newVel = target.Velocity;
            newVel.x = vx;
            newVel.z = vz;
            // Para la aproximación queremos Y=0
            newVel.y = 0; 
            
            target.Velocity = newVel;
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
    
    private bool IsNear(Vector3 a, Vector3 b, Agent agent)
    {
        return Vector3.Distance(a, b) < 1f; 
    }
    
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
