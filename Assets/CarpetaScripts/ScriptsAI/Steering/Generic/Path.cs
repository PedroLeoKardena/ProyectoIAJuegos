using UnityEngine;

[System.Serializable]
public class Path
{
    public Vector3[] nodes; // Array de posiciones que forman el camino

    public Path(Vector3[] nodes)
    {
        this.nodes = nodes;
    }

    // Retorna el índice del nodo más cercano
    public int GetParam(Vector3 position, int lastParam)
    {
        int closestNode = lastParam;
        float closestDistance = Vector3.Distance(position, nodes[lastParam]);

        // Buscar el nodo más cercano
        for (int i = 0; i < nodes.Length; i++)
        {
            float distance = Vector3.Distance(position, nodes[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = i;
            }
        }

        return closestNode;
    }

    // Método para obtener la posición en el camino dado un índice
    public Vector3 GetPosition(int param)
    {
        if (param < 0 || param >= nodes.Length)
        {
            return Vector3.zero;
        }
        return nodes[param];
    }
}
