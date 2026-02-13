using UnityEngine;

public class Wander : Face 
{
    [Header("Wander Settings")]
    public float wanderOffset = 4.0f; // Distancia al círculo
    public float wanderRadius = 2.0f; // Radio del círculo
    public float wanderRate = 10.0f;   // Cuánto puede cambiar como máximo cada vez (en grados)

    private float wanderOrientation = 0.0f; // El ángulo actual en el círculo

    // Target auxiliar (fantasma) para delegar en Face
    private GameObject auxTargetObjWander;
    private Agent auxTargetAgentWander;

    protected override void Start()
    {
        //Llamar al Start() del padre (Face) para inicializar auxTargetAgent
        base.Start();

        this.nameSteering = "Wander";
        
        // Inicializamos el target fantasma igual que hiciste en Pursue
        auxTargetObjWander = new GameObject("WanderGhost");
        auxTargetAgentWander = auxTargetObjWander.AddComponent<AgentNPC>();
                
        wanderOrientation = 0.0f;
    }

    void OnDestroy()
    {
        if (auxTargetObjWander != null) Destroy(auxTargetObjWander);
    }

    public override Steering GetSteering(Agent agent)
    {
        // 1. Calcular el cambio de orientación aleatorio y actualizamos el ángulo 
        float randomBinomial = Random.value - Random.value;
        wanderOrientation += randomBinomial * wanderRate;

        // 2. calculamos la orientacion combinada
        float targetOrientation = wanderOrientation + agent.Orientation;

        // 3. Calculamos el centro del circulo
        float charOrientationRad = agent.Orientation * Mathf.Deg2Rad;
        Vector3 target = agent.Position + 
                         wanderOffset * new Vector3(Mathf.Sin(charOrientationRad), 0, Mathf.Cos(charOrientationRad));

        // 4. La posicion del target
        float targetOrientationRad = targetOrientation * Mathf.Deg2Rad;
        target += wanderRadius * new Vector3(Mathf.Sin(targetOrientationRad), 0, Mathf.Cos(targetOrientationRad));

        // 5. Delegamos en Face
        auxTargetAgentWander.Position = target;
        this.target = auxTargetAgentWander;

        Steering steering = base.GetSteering(agent);

        // 6. La aceleracion lineal tiene que ser la maxima en la direccion de orientacion
        steering.linear = agent.MaxAcceleration * new Vector3(Mathf.Sin(charOrientationRad), 0, Mathf.Cos(charOrientationRad));

        return steering;
    }

    // Dibuja el círculo y el target para entender qué está pasando
    void OnDrawGizmos()
    {
        if (auxTargetAgentWander != null)
        {
            Agent agent = GetComponent<Agent>();
            if (agent == null) return;

            // Calcular centro del círculo visualmente
            float charOrientation = agent.Orientation * Mathf.Deg2Rad;
            Vector3 circleCenter = agent.Position + new Vector3(Mathf.Sin(charOrientation), 0, Mathf.Cos(charOrientation)) * wanderOffset;

            // Dibujar círculo
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(circleCenter, wanderRadius);

            // Dibujar target
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(auxTargetAgentWander.Position, 0.3f);
            
            // Dibujar línea de visión
            Gizmos.DrawLine(agent.Position, circleCenter);
            Gizmos.DrawLine(circleCenter, auxTargetAgentWander.Position);
        }
    }
}
