using UnityEngine;

public class PathFollowingSinOffset : Seek
{
    [Header("Path Following Settings")]
    public Path path; // El camino a seguir
    public float radius = 30.0f; // Radio para determinar si hemos llegado al nodo actual

    private int currentNode = 0; // Nodo actual en el camino
    private int pathDir = 1; // Dirección del camino (1 = adelante, -1 = atrás)

    // Target auxiliar (fantasma) para delegar en Seek
    private GameObject auxTargetObj;
    private Agent auxTargetAgent;

    void Start()
    {
        this.nameSteering = "PathFollowingSinOffset";

        // Inicializamos el target fantasma
        auxTargetObj = new GameObject("PathFollowingGhost");
        auxTargetAgent = auxTargetObj.AddComponent<AgentNPC>();

        // Buscamos el componente Face en este mismo NPC
        Face faceScript = GetComponent<Face>();

        faceScript.target = auxTargetAgent;
    }

    void OnDestroy()
    {
        if (auxTargetObj != null) Destroy(auxTargetObj);
    }

    public override Steering GetSteering(Agent agent)
    {
        // Verificar que tenemos un camino válido
        if (path == null || path.nodes == null || path.nodes.Length == 0)
        {
            return new Steering();
        }

        // 1. Verificar si hemos llegado al target actual
        Vector3 targetPosition = path.GetPosition(currentNode);
        float distanceToTarget = Vector3.Distance(agent.Position, targetPosition);
        
        if (distanceToTarget <= radius)
        {
            // Si hemos llegado, pasar al siguiente nodo
            Debug.Log($"[PathFollowing] Nodo {currentNode} alcanzado. Pasando al siguiente nodo.");
            currentNode += pathDir;

            // Opción 1. Me quedo en el final.
            if (currentNode >= path.nodes.Length)
            {
                currentNode = path.nodes.Length - 1;
                Debug.Log($"[PathFollowing] Llegado al final del camino. Nodo final: {currentNode}");
            }
            else
            {
                Debug.Log($"[PathFollowing] Nuevo nodo objetivo: {currentNode}");
            }

            // Opción 2. Hago vigilancia (Vuelvo atrás)
            // if (currentNode >= path.nodes.Length || currentNode < 0)
            // {
            //     pathDir = -pathDir;
            //     currentNode += pathDir;
            // }

            // Opción 3. Nuevo estado (steering)
            // if (currentNode >= path.nodes.Length || currentNode < 0)
            // {
            //     return new Steering();
            // }
        }

        // 2. Obtener la posición del target
        targetPosition = path.GetPosition(currentNode);

        // 3. Colocar el target fantasma en la posición del nodo actual
        auxTargetAgent.Position = targetPosition;

        // 4. Delegar en Seek
        this.target = auxTargetAgent;
        return base.GetSteering(agent);
    }

    // Dibuja el camino y el target para depuración
    void OnDrawGizmos()
    {
        if (path != null && path.nodes != null && path.nodes.Length > 0)
        {
            // Dibujar los nodos del camino
            Gizmos.color = Color.blue;
            for (int i = 0; i < path.nodes.Length; i++)
            {
                Gizmos.DrawSphere(path.nodes[i], 0.5f);
                
                // Dibujar líneas entre nodos
                if (i < path.nodes.Length - 1)
                {
                    Gizmos.DrawLine(path.nodes[i], path.nodes[i + 1]);
                }
            }

            // Dibujar el nodo actual en rojo
            if (currentNode >= 0 && currentNode < path.nodes.Length)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(path.nodes[currentNode], 0.7f);
                Gizmos.DrawWireSphere(path.nodes[currentNode], radius);
            }

            // Dibujar el target fantasma
            if (auxTargetAgent != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(auxTargetAgent.Position, 0.4f);
            }
        }
    }
}
