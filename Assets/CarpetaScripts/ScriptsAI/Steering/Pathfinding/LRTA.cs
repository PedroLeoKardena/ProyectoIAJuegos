using System.Collections.Generic;
using UnityEngine;

public enum HeuristicType { Manhattan, Chebyshev, Euclidean }

// LRTA* (Learning Real-Time A*) con subespacio de busqueda local (LSS-LRTA*).
// Algoritmo por paso:
//   1. BFS limitado a 'lookaheadDepth' construye S_lss.
//   2. ValueUpdateStep: actualiza h-values de todos los nodos en S_lss (Algoritmo 11.2).
//   3. LookaheadOne: elige el mejor vecino del nodo actual y actualiza h(currentNode).
//   4. Arrive mueve el agente fisicamente hacia el siguiente nodo.

public class LRTA : Arrive
{
    [Header("Configuracion LRTA")]
    [SerializeField] private GridManager grid;
    [SerializeField] private HeuristicType heuristicType = HeuristicType.Euclidean;
    [SerializeField] private Transform objetivoFinal;

    [Header("Espacio de busqueda local")]
    [Range(1, 20)]
    [SerializeField] private int lookaheadDepth = 5;

    [Header("Afinacion de Arrive")]
    [Tooltip("Distancia al centro del nodo para considerarlo alcanzado.")]
    [SerializeField] private float nodeReachedDistance = 0.6f;

    private Node targetNode;
    private Node nextStepNode;

    private Agent nodeTargetHelper;

    void Start()
    {
        this.nameSteering = "LRTA*-LSS";
        if (grid == null) grid = FindFirstObjectByType<GridManager>();

        GameObject helperGO = new GameObject("LRTA_InternalTarget");
        helperGO.hideFlags = HideFlags.HideInHierarchy;
        nodeTargetHelper = helperGO.AddComponent<Agent>();
        this.target = nodeTargetHelper;

        targetNode   = grid.NodeFromWorldPoint(objetivoFinal.position);
        nextStepNode = null;

        nodeTargetHelper.ArrivalRadius  = grid.cellSize * 2f;
        nodeTargetHelper.InteriorRadius = nodeReachedDistance;
    }

    private void OnDestroy()
    {
        if (nodeTargetHelper != null)
            Destroy(nodeTargetHelper.gameObject);
    }

    public override Steering GetSteering(Agent agent)
    {
        if (targetNode == null) return new Steering();

        Node currentNode = grid.NodeFromWorldPoint(agent.Position);
        if (currentNode == null) return new Steering();

        // Llegamos al objetivo?
        if (currentNode == targetNode)
        {
            Debug.Log("LRTA*: Objetivo Final Alcanzado!");
            nextStepNode = null;
            return new Steering();
        }

        // Hay que calcular el siguiente paso?
        bool needNewStep = nextStepNode == null
            || currentNode == nextStepNode
            || Vector3.Distance(agent.Position, nextStepNode.worldPosition) < nodeReachedDistance;

        if (needNewStep)
        {
            // 1. Construir S_lss mediante BFS limitado
            List<Node> Slss = GetLocalSearchSpace(currentNode, lookaheadDepth);

            // 2. Algoritmo 11.2: 
            ValueUpdateStep(Slss);

            // 3. Lookahead-One: elegir mejor vecino y actualizar h(currentNode)
            Node  bestNext;
            float updatedH;
            LookaheadOne(currentNode, out bestNext, out updatedH);

            nextStepNode = bestNext;

            if (nextStepNode != null)
            {
                Debug.Log("LRTA*: [" + currentNode.x + "," + currentNode.z + "]->[" + nextStepNode.x + "," + nextStepNode.z + "]  h=" + updatedH.ToString("F3"));
                nodeTargetHelper.transform.position = nextStepNode.worldPosition;
            }
        }

        // Delegar movimiento a Arrive
        return base.GetSteering(agent);
    }

    // Algoritmo 11.2
    // Actualiza los h-values de todos los nodos en S_lss.
    private void ValueUpdateStep(List<Node> Slss)
    {
        // Diccionario para guardar los precios actuales.
        Dictionary<Node, float> tempH = new Dictionary<Node, float>();

        // for each u in Slss: backup h and set to infinity
        foreach (Node u in Slss)
        {
            // Si el nodo no tiene heuristica inicial, la calculamos primero
            if (u.hCost <= 0 && u != targetNode)
                u.hCost = GetInitialH(u, targetNode);

            tempH[u] = u.hCost;
            u.hCost = float.MaxValue; // h(u) <- infinito
        }

        // El nodo objetivo siempre tiene h = 0
        if (Slss.Contains(targetNode))
            targetNode.hCost = 0f;

        // while (existan nodos en Slss con h(u) == infinito)
        bool nodesRemaining = true;
        while (nodesRemaining)
        {
            Node v = null;
            float minValFound = float.MaxValue;

            // v <- argmin_{u en Slss | h(u) = inf} max{temp(u), minCostNeighbors}
            foreach (Node u in Slss)
            {
                if (u.hCost != float.MaxValue) continue;

                float minNeighborCost = GetMinNeighborCost(u);
                float currentMax = Mathf.Max(tempH[u], minNeighborCost);

                if (currentMax < minValFound)
                {
                    minValFound = currentMax;
                    v = u;
                }
            }

            if (v != null)
            {
                v.hCost = minValFound; // h(v) <- max{temp(v), min...}

                // if (h(v) == infinito) return; // No hay mejora posible
                if (v.hCost >= float.MaxValue) break;
            }
            else
            {
                nodesRemaining = false;
            }
        }
    }

    // Lookahead-One: elige el mejor vecino del nodo u y actualiza h(u).
    private void LookaheadOne(Node u, out Node bestAction, out float updatedH)
    {
        bestAction = null;
        float minF = float.MaxValue;

        // a <- argmin_{a en A(u)} { w(u,a) + h(Succ(u,a)) }
        foreach (Node neighbor in grid.GetNeighbors(u))
        {
            if (!neighbor.isWalkable) continue;

            float weight = Vector3.Distance(u.worldPosition, neighbor.worldPosition);
            float hSuccessor = neighbor.hCost;

            float fCost = weight + hSuccessor;

            if (fCost < minF)
            {
                minF = fCost;
                bestAction = neighbor;
            }
        }

        // h(u) <- max{ h(u), w(u,a) + h(Succ(u,a)) }
        updatedH = Mathf.Max(u.hCost, minF);
        u.hCost = updatedH;
    }

    // Calcula min_{a en A(u)} { w(u,a) + h(Succ(u,a)) }
    // Usado por ValueUpdateStep para evaluar el coste minimo hacia sucesores.
    private float GetMinNeighborCost(Node u)
    {
        float minCost = float.MaxValue;
        foreach (Node neighbor in grid.GetNeighbors(u))
        {
            if (!neighbor.isWalkable) continue;

            float weight = Vector3.Distance(u.worldPosition, neighbor.worldPosition);

            float hNeighbor = neighbor.hCost;
            if (hNeighbor == float.MaxValue) continue; // sucesor sin valor finito aun

            float cost = weight + hNeighbor;
            if (cost < minCost) minCost = cost;
        }
        return minCost;
    }

    // BFS limitado para construir el subespacio de busqueda local S_lss.
    private List<Node> GetLocalSearchSpace(Node start, int depth)
    {
        List<Node> space = new List<Node>();
        Queue<(Node node, int d)> queue = new Queue<(Node, int)>();
        HashSet<Node> visited = new HashSet<Node>();

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, d) = queue.Dequeue();
            space.Add(current);

            if (d < depth)
            {
                foreach (Node n in grid.GetNeighbors(current))
                {
                    if (n.isWalkable && !visited.Contains(n))
                    {
                        visited.Add(n);
                        queue.Enqueue((n, d + 1));
                    }
                }
            }
        }
        return space;
    }

    // Heuristica inicial admisible segun el tipo seleccionado.
    private float GetInitialH(Node a, Node b)
    {
        float dx = Mathf.Abs(a.x - b.x);
        float dz = Mathf.Abs(a.z - b.z);
        float D  = grid.cellSize;
        float D2 = Mathf.Sqrt(2f) * D;

        switch (heuristicType)
        {
            // Manhattan (4 direcciones)
            case HeuristicType.Manhattan:
                return D * (dx + dz);

            // Chebyshev (8 direcciones)
            case HeuristicType.Chebyshev:
                float hDiag     = Mathf.Min(dx, dz);
                float hStraight = dx + dz;
                return D2 * hDiag + D * (hStraight - 2f * hDiag);

            // Euclidea
            case HeuristicType.Euclidean:
                return D * Mathf.Sqrt(dx * dx + dz * dz);

            default:
                return 0f;
        }
    }
}