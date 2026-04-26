using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Gestiona la generación, visualización y lógica de la cuadrícula en la escena
public class GridManager : MonoBehaviour
{
    [Header("Configuración del Escenario")]
    [Tooltip("Selecciona el Collider que hace de suelo. Sus medidas definirán el área total del grid.")]
    public Collider groundCollider;

    [Tooltip("Tamaño de la cuadrícula (ej. 30x30, 100x100)")]
    public Vector2Int gridSize = new Vector2Int(30, 30);
    
    [Tooltip("Layer que designa a los obstáculos")]
    public LayerMask obstacleLayer;

    [Tooltip("Capas del terreno para detectar su tag (Camino, Llanura, Bosque).")]
    [SerializeField] private LayerMask groundLayer = ~0;
    public bool drawGizmos = true;

    // Propiedades internas
    public int width => gridSize.x;
    public int height => gridSize.y;

    // Calculado automáticamente a partir del tamaño del suelo y gridSize
    [HideInInspector]
    public float cellSize;
    
    private Vector3 originPosition;
    private Grid<Node> grid;

    private void Awake()
    {
        CalculateGridParameters();

        // Inicializa la rejilla con nodos vacíos
        grid = new Grid<Node>(width, height, cellSize, originPosition, (Grid<Node> g, int x, int z) => new Node(x, z, Vector3.zero));
        
        // Calcula posiciones y detecta obstáculos
        BakeGrid();
    }

    // Método para recalcular cellSize y originPosition según el suelo asignado
    private void CalculateGridParameters()
    {
        if (groundCollider != null)
        {
            Bounds bounds = groundCollider.bounds;
            // Se asume división simétrica
            cellSize = bounds.size.x / gridSize.x;
            
            // El origen es la esquina inferior izquierda (X, Z min.) manteniendo la Y del collider
            originPosition = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        }
        else
        {
            cellSize = 1f;
            originPosition = transform.position;
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

                // Detecta el tag del terreno bajo el nodo para el coste de pathfinding.
                node.terrainTag = "Llanura";
                if (Physics.Raycast(worldPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit terrainHit, 2f, groundLayer))
                {
                    string tag = terrainHit.collider.tag;
                    if (tag == "Camino" || tag == "Bosque" || tag == "Llanura")
                        node.terrainTag = tag;
                }
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
        if (!drawGizmos) return;

        // Calcular parametros para poder mostrar los Gizmos incluso si el juego no está corriendo
        CalculateGridParameters();

        // Dibujar siempre los límites del área total esperada
        Gizmos.color = Color.yellow;
        Vector3 center = originPosition + new Vector3(width * cellSize, 0, height * cellSize) * 0.5f;
        Vector3 size = new Vector3(width * cellSize, 1, height * cellSize);
        Gizmos.DrawWireCube(center, size);

        if (grid != null && Application.isPlaying)
        {
            // MODO EJECUCIÓN (Play Mode): Usa los datos reales de la rejilla ya calculada.
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    Node node = grid.GetGridObject(x, z);
                    if (node != null)
                    {
                        Gizmos.color = node.isWalkable ? new Color(1, 1, 1, 0.3f) : new Color(1, 0, 0, 0.5f);
                        // Dibuja cubos planos sobre el suelo para representar cada celda
                        Gizmos.DrawCube(node.worldPosition, new Vector3(cellSize, 0.1f, cellSize) * 0.9f); 
                    }
                }
            }
        }
        else
        {
            // MODO EDICIÓN (Edit Mode): Calcula la rejilla al vuelo para previsualizarla antes de dar al Play.
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    Vector3 worldPos = originPosition + new Vector3(x * cellSize, 0, z * cellSize) + new Vector3(cellSize, 0, cellSize) * 0.5f;
                    bool isWalkable = !Physics.CheckSphere(worldPos, cellSize * 0.4f, obstacleLayer);
                    
                    Gizmos.color = isWalkable ? new Color(1, 1, 1, 0.3f) : new Color(1, 0, 0, 0.5f);
                    Gizmos.DrawCube(worldPos, new Vector3(cellSize, 0.1f, cellSize) * 0.9f);
                }
            }
        }
    }
}
