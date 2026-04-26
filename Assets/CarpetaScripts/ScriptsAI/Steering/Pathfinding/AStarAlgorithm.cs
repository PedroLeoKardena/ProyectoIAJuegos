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

// Placeholder para que el archivo compile en este paso.
// Se completará en la Task 2.
public static class AStarAlgorithm { }
