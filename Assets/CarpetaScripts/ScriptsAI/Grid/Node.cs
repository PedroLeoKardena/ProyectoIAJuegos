using UnityEngine;

// Unidad básica de la cuadrícula. Representa una celda en el espacio.
public class Node
{
    // Coordenadas en la matriz del grid
    public int x;
    public int z;

    // Posición central en el mundo real
    public Vector3 worldPosition;

    // Indica si la celda es transitable o es un obstáculo
    public bool isWalkable;

    // Pathfinding
    public float gCost;
    public float hCost;
    public Node parent;
    
    // Coste total fCost = gCost + hCost
    public float fCost { get { return gCost + hCost; } }

    // Formaciones
    // El NPC asignado a esta celda
    public Agent assignedAgent;
    // La orientación que debe tener en la celda
    public float orientation;

    public Node(int x, int z, Vector3 worldPosition, bool isWalkable = true)
    {
        this.x = x;
        this.z = z;
        this.worldPosition = worldPosition;
        this.isWalkable = isWalkable;
    }
}
