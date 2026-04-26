using System.Collections.Generic;
using UnityEngine;

// Registro de búsqueda que almacena el estado de un nodo durante A*.
// Se mantiene separado de Node para no modificar el estado del grid compartido.
public class NodeRecord
{
    public Node node;
    public NodeRecord parent;
    public float gCost;
    public float hCost;

    // Coste total estimado del camino que pasa por este nodo.
    public float fCost => gCost + hCost;

    public NodeRecord(Node node, NodeRecord parent, float gCost, float hCost)
    {
        this.node   = node;
        this.parent = parent;
        this.gCost  = gCost;
        this.hCost  = hCost;
    }
}

// Min-heap de NodeRecord. Ordenación: menor fCost; en empate, mayor hCost.
public class BinaryHeap
{
    private readonly List<NodeRecord> heap = new List<NodeRecord>();

    // Número de elementos en el heap.
    public int Count => heap.Count;

    // Inserta un registro y restaura la propiedad del heap hacia arriba.
    public void Insert(NodeRecord record)
    {
        heap.Add(record);
        HeapifyUp(heap.Count - 1);
    }

    // Extrae y devuelve el registro con menor fCost (o mayor hCost en empate).
    public NodeRecord ExtractMin()
    {
        if (heap.Count == 0)
            throw new System.InvalidOperationException("BinaryHeap está vacío.");
        NodeRecord min = heap[0];
        int last = heap.Count - 1;
        heap[0] = heap[last];
        heap.RemoveAt(last);
        if (heap.Count > 0) HeapifyDown(0);
        return min;
    }

    // Devuelve true si 'a' tiene más prioridad que 'b' (debe estar antes en el heap).
    private bool HasPriority(NodeRecord a, NodeRecord b)
    {
        if (a.fCost < b.fCost) return true;
        if (a.fCost > b.fCost) return false;
        return a.hCost > b.hCost; // tie-breaking: mayor hCost va primero
    }

    // Sube el elemento en posición i hasta su posición correcta.
    private void HeapifyUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (HasPriority(heap[i], heap[parent]))
            {
                (heap[i], heap[parent]) = (heap[parent], heap[i]);
                i = parent;
            }
            else break;
        }
    }

    // Baja el elemento en posición i hasta su posición correcta.
    private void HeapifyDown(int i)
    {
        int n = heap.Count;
        while (true)
        {
            int best  = i;
            int left  = 2 * i + 1;
            int right = 2 * i + 2;
            if (left  < n && HasPriority(heap[left],  heap[best])) best = left;
            if (right < n && HasPriority(heap[right], heap[best])) best = right;
            if (best == i) break;
            (heap[i], heap[best]) = (heap[best], heap[i]);
            i = best;
        }
    }
}

// Algoritmo A* puro. No hereda de MonoBehaviour. Solo calcula caminos.
// IMPORTANTE: Este algoritmo NO escribe en los campos gCost/hCost/parent de Node.
// Usar NodeRecord para todo el estado de búsqueda y preservar la integridad del grid.
public static class AStarAlgorithm
{
    // Raíz cuadrada de 2, precalculada para el coste diagonal en Chebyshev.
    private static readonly float Sqrt2 = Mathf.Sqrt(2f);

    // Delegado que encapsula el coste de traversar una arista (terreno + táctica).
    public delegate float CostProvider(Node from, Node to);

    // Calcula el camino óptimo desde start hasta target usando A*.
    // Devuelve un Path con las posiciones en orden (inicio → destino), o null si no hay camino.
    public static Path FindPath(Node start, Node target, GridManager grid,
        CostProvider costProvider, HeuristicType heuristic)
    {
        if (grid == null || costProvider == null || start == null || target == null
            || !start.isWalkable || !target.isWalkable)
            return null;

        var open       = new BinaryHeap();
        var closed     = new HashSet<Node>();
        var openLookup = new Dictionary<Node, NodeRecord>();

        float h0          = Heuristic(start, target, grid.cellSize, heuristic);
        var   startRecord = new NodeRecord(start, null, 0f, h0);
        open.Insert(startRecord);
        openLookup[start] = startRecord;

        while (open.Count > 0)
        {
            NodeRecord current = open.ExtractMin();

            // Registro obsoleto: el nodo ya fue procesado con un coste menor (lazy deletion).
            if (closed.Contains(current.node)) continue;

            if (current.node == target)
                return ReconstructPath(current);

            openLookup.Remove(current.node);
            closed.Add(current.node);

            foreach (Node neighbor in grid.GetNeighbors(current.node))
            {
                if (!neighbor.isWalkable || closed.Contains(neighbor)) continue;

                // Garantía de admisibilidad: el coste de arista nunca es negativo.
                float edgeCost = Mathf.Max(0f, costProvider(current.node, neighbor));
                float newG     = current.gCost + edgeCost;

                // Si ya existe un registro en OPEN con coste igual o mejor, no actualizar.
                if (openLookup.TryGetValue(neighbor, out NodeRecord existing) && existing.gCost <= newG)
                    continue;

                float newH  = Heuristic(neighbor, target, grid.cellSize, heuristic);
                var   record = new NodeRecord(neighbor, current, newG, newH);
                open.Insert(record);
                openLookup[neighbor] = record;
            }
        }

        return null; // Sin camino disponible
    }

    // Reconstruye el Path siguiendo la cadena de padres desde el NodeRecord final.
    private static Path ReconstructPath(NodeRecord end)
    {
        var positions = new List<Vector3>();
        NodeRecord current = end;
        while (current != null)
        {
            positions.Add(current.node.worldPosition);
            current = current.parent;
        }
        positions.Reverse();
        return new Path(positions.ToArray());
    }

    // Calcula la estimación heurística entre dos nodos según el tipo seleccionado.
    // Fórmulas idénticas a LRTA.cs para consistencia del proyecto.
    private static float Heuristic(Node a, Node b, float cellSize, HeuristicType type)
    {
        float dx = Mathf.Abs(a.x - b.x);
        float dz = Mathf.Abs(a.z - b.z);
        float D  = cellSize;

        switch (type)
        {
            case HeuristicType.Manhattan:
                // Admisible solo con movimiento en 4 direcciones.
                // Con 8 direcciones (este grid), usar Chebyshev o Euclidean.
                return D * (dx + dz);
            case HeuristicType.Chebyshev:
                float hDiag = Mathf.Min(dx, dz);
                return Sqrt2 * D * hDiag + D * (dx + dz - 2f * hDiag);
            case HeuristicType.Euclidean:
                return D * Mathf.Sqrt(dx * dx + dz * dz);
            default:
                Debug.LogWarning($"[AStarAlgorithm] HeuristicType desconocido: {type}. Usando h=0.");
                return 0f;
        }
    }
}
