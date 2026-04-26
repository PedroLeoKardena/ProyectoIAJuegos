using UnityEngine;

public enum UnitType
{
    InfanteriaPesada,
    Velites,
    Exploradores
}

[RequireComponent(typeof(Bodi))]
public class TerrainSpeedModifier : MonoBehaviour
{
    [Tooltip("Tipo de unidad.")]
    public UnitType unitType = UnitType.InfanteriaPesada;

    [Header("Detección de Terreno")]
    [Tooltip("Distancia del raycast hacia abajo")]
    [SerializeField] private float raycastDistance = 2f;
    [Tooltip("Filtro de capas para el raycast")]
    [SerializeField] private LayerMask groundLayer = ~0;

    private Bodi bodi;
    public string currentTerrain { get; private set; } = "Llanura";

    private void Awake()
    {
        bodi = GetComponent<Bodi>();
        ApplySpeedForTerrain(currentTerrain);
    }

    private void Update()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            string detectedTag = hit.collider.tag;

            if (detectedTag != currentTerrain && IsValidTerrainTag(detectedTag))
            {
                currentTerrain = detectedTag;
                ApplySpeedForTerrain(currentTerrain);
            }
        }
    }

    private bool IsValidTerrainTag(string tag)
    {
        return tag == "Camino" || tag == "Bosque" || tag == "Llanura";
    }

    /// <summary>
    /// Obtiene la velocidad de la unidad en el terreno dado. Fuente única de verdad para movimiento y pathfinding.
    /// </summary>
    public static float GetSpeed(UnitType unitType, string terrainTag)
    {
        switch (unitType)
        {
            case UnitType.InfanteriaPesada:
                if (terrainTag == "Camino") return 3.5f;
                if (terrainTag == "Bosque") return 2f;
                return 3f;
            case UnitType.Velites:
                if (terrainTag == "Camino") return 5f;
                if (terrainTag == "Bosque") return 3.5f;
                return 4f;
            case UnitType.Exploradores:
                if (terrainTag == "Camino") return 7f;
                if (terrainTag == "Bosque") return 5.5f;
                return 6f;
            default:
                return 3f;
        }
    }

    /// <summary>
    /// Obtiene la velocidad máxima posible de la unidad (en Camino), usada para escalar la heurística de A*.
    /// </summary>
    public static float GetMaxSpeed(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.InfanteriaPesada: return 3.5f;
            case UnitType.Velites:          return 5f;
            case UnitType.Exploradores:     return 7f;
            default:                        return 3f;
        }
    }

    private void ApplySpeedForTerrain(string terrain)
    {
        bodi.MaxSpeed = GetSpeed(unitType, terrain);
    }
}
