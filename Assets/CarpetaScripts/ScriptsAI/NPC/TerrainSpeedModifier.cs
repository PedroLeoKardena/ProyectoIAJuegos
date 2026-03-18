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
    private string currentTerrain = "Llanura"; // Terreno asumido por defecto

    private void Awake()
    {
        bodi = GetComponent<Bodi>();
        // Aplicamos la velocidad inicial asumiendo Llanura
        ApplySpeedForTerrain(currentTerrain);
    }

    private void Update()
    {
        // Detectar el suelo debajo del personaje
        // Se lanza el rayo desde un poco más arriba del objeto hacia abajo
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            string detectedTag = hit.collider.tag;
            
            // Si el tag detectado es uno de nuestros terrenos y ha cambiado
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

    private void ApplySpeedForTerrain(string terrain)
    {
        float newSpeed = 0f;

        switch (unitType)
        {
            case UnitType.InfanteriaPesada:
                if (terrain == "Camino") newSpeed = 3.5f;
                else if (terrain == "Bosque") newSpeed = 2f;
                else if (terrain == "Llanura") newSpeed = 3f;
                break;
            
            case UnitType.Velites:
                if (terrain == "Camino") newSpeed = 5f;
                else if (terrain == "Bosque") newSpeed = 3.5f;
                else if (terrain == "Llanura") newSpeed = 4f;
                break;
                
            case UnitType.Exploradores:
                if (terrain == "Camino") newSpeed = 7f;
                else if (terrain == "Bosque") newSpeed = 5.5f;
                else if (terrain == "Llanura") newSpeed = 6f;
                break;
        }

        // Asignamos el valor al script que maneja las velocidades del agente
        bodi.MaxSpeed = newSpeed;
    }
}
