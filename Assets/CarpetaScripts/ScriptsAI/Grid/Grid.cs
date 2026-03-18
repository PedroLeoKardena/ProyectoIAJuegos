using System;
using System.Collections.Generic;
using UnityEngine;

// Estructura de datos genérica para gestionar una cuadrícula 2D.
public class Grid<T>
{
    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private T[,] gridArray;

    public int Width { get { return width; } }
    public int Height { get { return height; } }
    public float CellSize { get { return cellSize; } }

    // Logica Formaciones
    private Vector2Int leaderPosition; // Posición de celda del líder

    public Vector2Int LeaderPosition
    {
        get { return leaderPosition; }
        set { leaderPosition = value; }
    }

    // Dada una posición (x, z) del grid retorna su posición relativa con respecto al líder.
    public Vector2Int GetRelativeFromLeader(int x, int z)
    {
        return new Vector2Int(x - leaderPosition.x, z - leaderPosition.y);
    }

    // Constructor: Inicializa la cuadrícula con dimensiones, tamaño de celda y origen.
    public Grid(int width, int height, float cellSize, Vector3 originPosition, Func<Grid<T>, int, int, T> createGridObject)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new T[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] = createGridObject(this, x, z);
            }
        }
    }

    // Convierte coordenadas de rejilla (x, z) a posición en el mundo real.
    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + originPosition;
    }

    // Convierte una posición del mundo real a coordenadas de la rejilla (x, z).
    public void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
    }

    // Establece el valor (objeto) en una celda específica de la rejilla.
    public void SetGridObject(int x, int z, T value)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            gridArray[x, z] = value;
        }
    }

    public void SetGridObject(Vector3 worldPosition, T value)
    {
        int x, z;
        GetXZ(worldPosition, out x, out z);
        SetGridObject(x, z, value);
    }

    // Obtiene el objeto almacenado en una celda específica de la rejilla.
    public T GetGridObject(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            return gridArray[x, z];
        }
        else
        {
            return default(T);
        }
    }

    // Obtiene el objeto de la rejilla correspondiente a una posición en el mundo.
    public T GetGridObject(Vector3 worldPosition)
    {
        int x, z;
        GetXZ(worldPosition, out x, out z);
        return GetGridObject(x, z);
    }
}
