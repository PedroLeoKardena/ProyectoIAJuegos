using UnityEngine;

public class PathFollowingSinOffset : Seek
{
    public enum PathFollowingMode
    {
        StayAtEnd,  // Se queda en el último nodo del camino
        Patrol,     // Hace el camino de ida y vuelta (patrulla)
        Loop,       // Vuelve al principio al terminar
    }

    [Header("Path Following Settings")]
    public Path path; // El camino a seguir
    public float radius = 30.0f; // Radio para determinar si hemos llegado al nodo actual

    [Tooltip("Modo de comportamiento al final del camino:\n" +
             "StayAtEnd: Se queda en el último nodo.\n" +
             "Patrol: Ida y vuelta constante.\n" +
             "Loop: Reinicia el camino desde el inicio.\n")]
    public PathFollowingMode behaviorMode = PathFollowingMode.StayAtEnd;

    private int currentNode = 0; // Nodo actual en el camino
    private int pathDir = 1; // Dirección del camino (1 = adelante, -1 = atrás)

    // Target auxiliar (fantasma) para delegar en Seek
    private GameObject auxTargetObj;
    private Agent auxTargetAgent;

    // Cache del AStarPathfinder en este mismo GameObject. Null si el agente no usa A*.
    private AStarPathfinder astarPathfinder;
    // Guarda el último camino activo para detectar cambios y reiniciar el índice.
    private Path lastPath;

    void Start()
    {
        this.nameSteering = "PathFollowingSinOffset";

        // Si el agente tiene AStarPathfinder, sus caminos tendrán prioridad sobre path manual.
        astarPathfinder = GetComponent<AStarPathfinder>();

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
        // Usar el camino de AStarPathfinder si está disponible; si no, el path manual.
        Path effectivePath = (astarPathfinder != null) ? astarPathfinder.CurrentPath : path;

        // Al cambiar de camino (nuevo recálculo A* o asignación manual), reiniciar índice.
        if (effectivePath != lastPath)
        {
            currentNode = 0;
            lastPath = effectivePath;
        }

        // Verificar que tenemos un camino válido
        if (effectivePath == null || effectivePath.nodes == null || effectivePath.nodes.Length == 0)
        {
            return new Steering();
        }

        // 1. Verificar si hemos llegado al target actual
        Vector3 targetPosition = effectivePath.GetPosition(currentNode);
        float distanceToTarget = Vector3.Distance(agent.Position, targetPosition);

        if (distanceToTarget <= radius)
        {
            // Si hemos llegado, pasar al siguiente nodo o cambiar dirección según modo
            switch (behaviorMode)
            {
                case PathFollowingMode.StayAtEnd:
                    currentNode += pathDir;
                    if (currentNode >= effectivePath.nodes.Length)
                        currentNode = effectivePath.nodes.Length - 1;
                    else if (currentNode < 0)
                        currentNode = 1;
                    break;

                case PathFollowingMode.Patrol:
                    currentNode += pathDir;
                    if (currentNode >= effectivePath.nodes.Length || currentNode < 0)
                    {
                        pathDir = -pathDir;
                        currentNode += 2 * pathDir; // Retrocedemos al nodo anterior
                        // Asegurar dentro de rango
                        currentNode = Mathf.Clamp(currentNode, 0, effectivePath.nodes.Length - 1);
                    }
                    break;

                case PathFollowingMode.Loop:
                    currentNode += pathDir;
                    if (currentNode >= effectivePath.nodes.Length)
                        currentNode = 0;
                    else if (currentNode < 0)
                        currentNode = effectivePath.nodes.Length - 1;
                    break;

            }
        }

        // 2. Obtener la posición del target
        targetPosition = effectivePath.GetPosition(currentNode);

        // 3. Colocar el target fantasma en la posición del nodo actual
        auxTargetAgent.Position = targetPosition;

        // 4. Delegar en Seek
        this.target = auxTargetAgent;
        return base.GetSteering(agent);
    }

    // Dibuja el camino y el target para depuración
    void OnDrawGizmos()
    {
        Path effectivePath = (astarPathfinder != null) ? astarPathfinder.CurrentPath : path;

        if (effectivePath != null && effectivePath.nodes != null && effectivePath.nodes.Length > 0)
        {
            // Dibujar los nodos del camino
            Gizmos.color = Color.blue;
            for (int i = 0; i < effectivePath.nodes.Length; i++)
            {
                Gizmos.DrawSphere(effectivePath.nodes[i], 0.5f);

                // Dibujar líneas entre nodos
                if (i < effectivePath.nodes.Length - 1)
                {
                    Gizmos.DrawLine(effectivePath.nodes[i], effectivePath.nodes[i + 1]);
                }
            }

            // Dibujar el nodo actual en rojo
            if (currentNode >= 0 && currentNode < effectivePath.nodes.Length)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(effectivePath.nodes[currentNode], 0.7f);
                Gizmos.DrawWireSphere(effectivePath.nodes[currentNode], radius);
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
