using System.Collections;
using System.Collections.Generic;
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

    //En el caso de haber un grid, se usa para que no se intente ir a un lugar inaccesible
    public GridManager gridManager;
    public int radioBusqueda = 2;

    protected override void Start()
    {
        //Llamar al Start() del padre (Face) para inicializar auxTargetAgent
        base.Start();

        this.nameSteering = "Wander";
        
        // Inicializamos el target fantasma igual que hiciste en Pursue
        auxTargetObjWander = new GameObject("WanderGhost");
        auxTargetAgentWander = auxTargetObjWander.AddComponent<AgentNPC>();
 
        if (gridManager == null)
            gridManager = FindAnyObjectByType<GridManager>();
    }

    void OnDestroy()
    {
        if (auxTargetObjWander != null) Destroy(auxTargetObjWander);
    }

    public override Steering GetSteering(Agent agent)
    {
        if (auxTargetAgentWander == null) return new Steering();

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

        //Tenemos en cuenta si hay grid
        if (gridManager != null)
            target = AjustarAEntorno(target);

        // 5. Delegamos en Face
        auxTargetAgentWander.Position = target;
        this.target = auxTargetAgentWander;

        Steering steering = base.GetSteering(agent);

        // 6. La aceleracion lineal tiene que ser la maxima en la direccion de orientacion
        steering.linear = agent.MaxAcceleration * new Vector3(Mathf.Sin(charOrientationRad), 0, Mathf.Cos(charOrientationRad));

        return steering;
    }

    // Tiene en cuenta el grid
    Vector3 AjustarAEntorno(Vector3 pos)
    {
        Node nodo = gridManager.NodeFromWorldPoint(pos);
        if (nodo != null && nodo.isWalkable)
            return pos;
        Node centro = gridManager.NodeFromWorldPoint(transform.position);
        if (centro == null)
            return transform.position;
        List<Node> candidatos = new List<Node>();
        List<Node> vecinos = gridManager.GetNeighbors(centro);
        foreach (Node v in vecinos)
            if (v.isWalkable)
                candidatos.Add(v);
        if (candidatos.Count > 0)
        {
            Node elegido = candidatos[Random.Range(0, candidatos.Count)];
            return elegido.worldPosition;
        }
        return transform.position;
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
