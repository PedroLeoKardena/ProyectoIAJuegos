using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Gestiona la generación, visualización y lógica de la cuadrícula en la escena
public class GridManager : MonoBehaviour
{
    public int width = 20;
    public int height = 20;
    public float cellSize = 1f;
    public LayerMask obstacleLayer;
    public bool showDebug = true;

    private Grid<Node> grid;

    private void Awake()
    {
        //Debug.Log("GridManager Awake started.");
        // Inicializa la rejilla con nodos vacíos
        grid = new Grid<Node>(width, height, cellSize, transform.position, (Grid<Node> g, int x, int z) => new Node(x, z, Vector3.zero));
        
        // Calcula posiciones y detecta obstáculos
        BakeGrid();
        //Debug.Log($"Grid Initialized. Width: {width}, Height: {height}. Grid object is null? {grid == null}");
    }

    private void Update()
    {
        if (showDebug && grid != null)
        {
            grid.DebugDrawGrid((Node n) => n.isWalkable ? Color.white : Color.red);
        }
    }

    public List<Node> GetAllNodesWalkables()
    {
        List<Node> nodes = new List<Node>();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Node node = grid.GetGridObject(x, z);
                if (node.isWalkable)
                {
                    nodes.Add(node);
                }
            }
        }
        return nodes;
    }

    // Recorre la rejilla y verifica colisiones para marcar zonas transitables.
    public void BakeGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // Centro de la celda
                Vector3 worldPos = grid.GetWorldPosition(x, z) + new Vector3(cellSize, 0, cellSize) * 0.5f;
                
                Node node = grid.GetGridObject(x, z);
                node.worldPosition = worldPos;

                // Comprueba si hay obstáculos en la capa definida
                bool isWalkable = !Physics.CheckSphere(worldPos, cellSize * 0.4f, obstacleLayer);
                node.isWalkable = isWalkable;
            }
        }
    }

    public void WorldToGrid(Vector3 worldPosition, out int x, out int z)
    {
        grid.GetXZ(worldPosition, out x, out z);
    }

    public Vector3 GridToWorld(int x, int z)
    {
        return grid.GetWorldPosition(x, z) + new Vector3(cellSize, 0, cellSize) * 0.5f;
    }
    
    // Devuelve el nodo correspondiente a una posición dada en el mundo.
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        return grid.GetGridObject(worldPosition);
    }

    // Devuelve los vecinos de un nodo (horizontal, vertical y diagonal)
    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue;

                int checkX = node.x + x;
                int checkZ = node.z + z;

                if (checkX >= 0 && checkX < width && checkZ >= 0 && checkZ < height)
                {
                    neighbors.Add(grid.GetGridObject(checkX, checkZ));
                }
            }
        }

        return neighbors;
    }

    // Dibuja la rejilla en el editor para depuración visual.
    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Draw bounds always (WireCube)
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position + new Vector3(width * cellSize, 0, height * cellSize) * 0.5f;
        Vector3 size = new Vector3(width * cellSize, 1, height * cellSize);
        Gizmos.DrawWireCube(center, size);

        if (grid != null)
        {
            // Use existing grid data (Play Mode usually)
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    Node node = grid.GetGridObject(x, z);
                    if (node != null)
                    {
                        Gizmos.color = node.isWalkable ? new Color(1, 1, 1, 0.3f) : new Color(1, 0, 0, 0.5f);
                        // Draw flatter cubes to sit on ground better, or wireframes
                        Gizmos.DrawCube(node.worldPosition, new Vector3(cellSize, 0.1f, cellSize) * 0.9f); 
                    }
                }
            }
        }
        else
        {
            // Calculate on the fly for Editor visualization (Edit Mode)
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    Vector3 worldPos = transform.position + new Vector3(x * cellSize, 0, z * cellSize) + new Vector3(cellSize, 0, cellSize) * 0.5f;
                    bool isWalkable = !Physics.CheckSphere(worldPos, cellSize * 0.4f, obstacleLayer);
                    
                    Gizmos.color = isWalkable ? new Color(1, 1, 1, 0.3f) : new Color(1, 0, 0, 0.5f);
                    Gizmos.DrawCube(worldPos, new Vector3(cellSize, 0.1f, cellSize) * 0.9f);
                }
            }
        }
    }
}
