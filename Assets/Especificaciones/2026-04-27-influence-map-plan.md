# Influence Map — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Crear `InfluenceMap.cs` — un MonoBehaviour singleton que calcula periódicamente la proyección táctica de cada bando sobre el grid y expone una API de consulta + visualización Gizmos.

**Architecture:** Un único MonoBehaviour con dos arrays `float[,]` paralelos al grid (`_allied`, `_enemy`). Se refresca con `InvokeRepeating`. Itera el grid usando `GridManager.GridToWorld` + `NodeFromWorldPoint`, cacheando los nodos en `Awake` para eficiencia. No modifica ningún archivo existente.

**Tech Stack:** Unity C# (MonoBehaviour, Gizmos, InvokeRepeating), `#if UNITY_EDITOR` para `Handles.Label`.

---

## Estructura de archivos

| Acción | Ruta |
|---|---|
| Crear | `Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs` |

No se modifica ningún archivo existente.

---

## Task 1: Scaffold — estructura base y ciclo de vida

**Files:**
- Create: `Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs`

- [ ] **Step 1.1: Crear el archivo con la estructura base**

Crear `Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs` con el siguiente contenido:

```csharp
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Mapa de influencia táctico. Calcula periódicamente la proyección de fuerza
// militar de cada bando sobre el grid y expone una API de consulta.
public class InfluenceMap : MonoBehaviour
{
    // Instancia única accesible globalmente.
    public static InfluenceMap Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private GridManager gridManager;

    [Header("Actualización")]
    [Tooltip("Segundos entre refrescos del mapa (0.5 – 2 recomendado).")]
    [SerializeField] private float refreshInterval = 1f;

    [Header("Influencia")]
    [Tooltip("Radio máximo de efecto por unidad en unidades de mundo.")]
    [SerializeField] private float influenceRadius = 10f;

    [Header("Modificadores de Terreno")]
    [SerializeField] private float bosqueMultiplier  = 0.8f;
    [SerializeField] private float llanuraMultiplier = 1.2f;
    [SerializeField] private float caminoMultiplier  = 1.0f;

    [Header("Potencia Base (I₀) por Tipo de Unidad")]
    [SerializeField] private float I0_InfanteriaPesada = 15f;
    [SerializeField] private float I0_Velites           = 8f;
    [SerializeField] private float I0_Exploradores      = 5f;

    [Header("Debug")]
    [SerializeField] private bool debugMode         = false;
    [SerializeField] private bool showNumericValues = false;

    private float[,] _allied;
    private float[,] _enemy;
    private int _width;
    private int _height;
    private Node[,] _nodeCache;

    // Inicializa el singleton. Solo asigna la referencia al gridManager; el grid aún no está listo.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    // Reserva arrays, cachea nodos del grid (ya inicializado por GridManager.Awake) y arranca el refresco.
    private void Start()
    {
        _width  = gridManager.width;
        _height = gridManager.height;
        _allied = new float[_width, _height];
        _enemy  = new float[_width, _height];

        _nodeCache = new Node[_width, _height];
        for (int x = 0; x < _width; x++)
            for (int z = 0; z < _height; z++)
                _nodeCache[x, z] = gridManager.NodeFromWorldPoint(gridManager.GridToWorld(x, z));

        InvokeRepeating(nameof(RefreshMap), 0f, refreshInterval);
    }

    // Recalcula toda la influencia del mapa. Llamado periódicamente, nunca en Update.
    private void RefreshMap()
    {
        // Stub — se implementa en Task 2
    }
}
```

- [ ] **Step 1.2: Añadir el componente a la escena**

En el editor de Unity:
1. Crear un GameObject vacío llamado `InfluenceMapManager`.
2. Añadirle el componente `InfluenceMap`.
3. Asignar el `GridManager` existente en la escena al campo `gridManager` (o dejarlo en null para que lo busque automáticamente).

- [ ] **Step 1.3: Verificar compilación y arranque sin errores**

Entrar en Play Mode. En la consola de Unity no deben aparecer errores (warnings de compilación o NullReferenceException). Si `gridManager` es null en Awake, se verá un NullReferenceException — en ese caso asignar el campo manualmente.

- [ ] **Step 1.4: Commit**

```bash
git add Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs
git add Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs.meta
git commit -m "feat: scaffold InfluenceMap con singleton, arrays y ciclo de vida"
```

---

## Task 2: Lógica de cálculo — RefreshMap y helpers

**Files:**
- Modify: `Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs`

- [ ] **Step 2.1: Implementar GetI0 y GetTerrainMultiplier**

Añadir los siguientes métodos privados en la clase, después de `RefreshMap`:

```csharp
// Devuelve I₀ según el UnitType del GameObject. Usa I0_Exploradores como fallback.
private float GetI0(GameObject unit)
{
    TerrainSpeedModifier tsm = unit.GetComponent<TerrainSpeedModifier>();
    if (tsm == null) return I0_Exploradores;
    switch (tsm.unitType)
    {
        case UnitType.InfanteriaPesada: return I0_InfanteriaPesada;
        case UnitType.Velites:          return I0_Velites;
        case UnitType.Exploradores:     return I0_Exploradores;
        default:                        return I0_Exploradores;
    }
}

// Devuelve el multiplicador de terreno para el terrainTag del nodo candidato.
private float GetTerrainMultiplier(string terrainTag)
{
    switch (terrainTag)
    {
        case "Bosque":  return bosqueMultiplier;
        case "Llanura": return llanuraMultiplier;
        case "Camino":  return caminoMultiplier;
        default:        return 1f;
    }
}
```

- [ ] **Step 2.2: Implementar ProcessUnits**

Añadir el método privado `ProcessUnits` después de los helpers:

```csharp
// Acumula la influencia de un grupo de unidades sobre el array target.
// Fórmula: I_d = I₀ / √(1 + d), con d = distancia euclídea en unidades de mundo.
private void ProcessUnits(GameObject[] units, float[,] target)
{
    foreach (GameObject unit in units)
    {
        Node origin = gridManager.NodeFromWorldPoint(unit.transform.position);
        if (origin == null) continue;

        float i0 = GetI0(unit);

        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _height; z++)
            {
                Node candidate = _nodeCache[x, z];
                if (candidate == null) continue;

                float d = Vector3.Distance(origin.worldPosition, candidate.worldPosition);
                if (d > influenceRadius) continue;

                float influence = i0 / Mathf.Sqrt(1f + d);
                influence *= GetTerrainMultiplier(candidate.terrainTag);
                target[x, z] += influence;
            }
        }
    }
}
```

- [ ] **Step 2.3: Conectar RefreshMap**

Reemplazar el stub `RefreshMap` por la implementación real:

```csharp
// Recalcula toda la influencia del mapa. Llamado periódicamente, nunca en Update.
private void RefreshMap()
{
    System.Array.Clear(_allied, 0, _allied.Length);
    System.Array.Clear(_enemy,  0, _enemy.Length);

    ProcessUnits(GameObject.FindGameObjectsWithTag("Aliado"), _allied);
    ProcessUnits(GameObject.FindGameObjectsWithTag("Enemigo"), _enemy);
}
```

- [ ] **Step 2.4: Verificar en Play Mode**

Asegurarse de que los GameObjects de unidades en la escena tienen los tags `"Aliado"` o `"Enemigo"` asignados en Unity. Entrar en Play Mode — no deben aparecer errores. Para confirmar que el cálculo funciona, añadir temporalmente en `RefreshMap` al final:

```csharp
Debug.Log($"[InfluenceMap] Refresh. Aliado[0,0]={_allied[0,0]:F2} Enemigo[0,0]={_enemy[0,0]:F2}");
```

Si las unidades están en la escena, los valores deben ser mayores que 0. Eliminar el Debug.Log tras confirmar.

- [ ] **Step 2.5: Commit**

```bash
git add Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs
git commit -m "feat: implementar RefreshMap con fórmula I0/sqrt(1+d) y modificadores de terreno"
```

---

## Task 3: API pública de consulta

**Files:**
- Modify: `Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs`

- [ ] **Step 3.1: Añadir los seis métodos públicos**

Añadir los siguientes métodos públicos antes de `GetI0`:

```csharp
// Devuelve la influencia aliada acumulada en el nodo dado. Devuelve 0 si es null o fuera del grid.
public float GetAlliedInfluence(Node node)
{
    if (node == null || node.x < 0 || node.z < 0 || node.x >= _width || node.z >= _height) return 0f;
    return _allied[node.x, node.z];
}

// Devuelve la influencia enemiga acumulada en el nodo dado. Devuelve 0 si es null o fuera del grid.
public float GetEnemyInfluence(Node node)
{
    if (node == null || node.x < 0 || node.z < 0 || node.x >= _width || node.z >= _height) return 0f;
    return _enemy[node.x, node.z];
}

// Devuelve el balance de control (aliado - enemigo) en el nodo dado.
public float GetControl(Node node) => GetAlliedInfluence(node) - GetEnemyInfluence(node);

// Devuelve la influencia aliada en la posición del mundo dada.
public float GetAlliedInfluence(Vector3 worldPos) => GetAlliedInfluence(gridManager.NodeFromWorldPoint(worldPos));

// Devuelve la influencia enemiga en la posición del mundo dada.
public float GetEnemyInfluence(Vector3 worldPos) => GetEnemyInfluence(gridManager.NodeFromWorldPoint(worldPos));

// Devuelve el balance de control en la posición del mundo dada.
public float GetControl(Vector3 worldPos) => GetControl(gridManager.NodeFromWorldPoint(worldPos));
```

- [ ] **Step 3.2: Verificar la API en Play Mode**

Añadir temporalmente en `Start` (después de `InvokeRepeating`) para esperar el primer refresco:

```csharp
Invoke(nameof(VerifyAPI), 0.1f);
```

Y el método de verificación (temporal, eliminar tras confirmar):

```csharp
private void VerifyAPI()
{
    Node n = gridManager.NodeFromWorldPoint(transform.position);
    Debug.Assert(GetAlliedInfluence(null) == 0f, "GetAlliedInfluence(null) debe devolver 0");
    Debug.Assert(GetEnemyInfluence(null) == 0f, "GetEnemyInfluence(null) debe devolver 0");
    Debug.Log($"[InfluenceMap] API OK. Control en origen: {GetControl(n):F2}");
}
```

Entrar en Play Mode y comprobar que no aparecen assertion failures. Eliminar `VerifyAPI` e `Invoke` tras confirmar.

- [ ] **Step 3.3: Commit**

```bash
git add Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs
git commit -m "feat: añadir API pública GetAlliedInfluence, GetEnemyInfluence, GetControl"
```

---

## Task 4: Visualización Gizmos (modo debug)

**Files:**
- Modify: `Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs`

- [ ] **Step 4.1: Añadir OnDrawGizmos**

Añadir el siguiente método al final de la clase, antes del cierre `}`:

```csharp
// Visualiza el balance de control sobre el grid en el Scene View.
// Azul = dominio aliado, Rojo = dominio enemigo, Gris = equilibrio.
private void OnDrawGizmos()
{
    if (!debugMode || !Application.isPlaying || _allied == null || gridManager == null) return;

    // Calcular el valor máximo para normalizar los colores.
    float maxAbs = 0.001f;
    for (int x = 0; x < _width; x++)
        for (int z = 0; z < _height; z++)
        {
            float abs = Mathf.Abs(_allied[x, z] - _enemy[x, z]);
            if (abs > maxAbs) maxAbs = abs;
        }

    float cs = gridManager.cellSize;

    for (int x = 0; x < _width; x++)
    {
        for (int z = 0; z < _height; z++)
        {
            Node node = _nodeCache[x, z];
            if (node == null || !node.isWalkable) continue;

            float control = _allied[x, z] - _enemy[x, z];
            float t = Mathf.Clamp01(Mathf.Abs(control) / maxAbs);

            Color col = control > 0f
                ? Color.Lerp(Color.gray, Color.blue, t)
                : control < 0f
                    ? Color.Lerp(Color.gray, Color.red, t)
                    : Color.gray;
            col.a = Mathf.Lerp(0.1f, 0.6f, t);

            Gizmos.color = col;
            Gizmos.DrawCube(node.worldPosition, new Vector3(cs, 0.1f, cs) * 0.9f);

#if UNITY_EDITOR
            if (showNumericValues)
                UnityEditor.Handles.Label(node.worldPosition + Vector3.up * 0.2f, control.ToString("F1"));
#endif
        }
    }
}
```

- [ ] **Step 4.2: Verificar visualización**

1. Entrar en Play Mode con unidades `"Aliado"` y `"Enemigo"` en la escena.
2. Activar `debugMode = true` en el Inspector del componente `InfluenceMap`.
3. En la ventana **Scene View**, verificar que aparecen cubos de colores sobre el grid (azul cerca de aliados, rojo cerca de enemigos).
4. Activar `showNumericValues = true` para confirmar que los valores numéricos aparecen sobre cada celda en la Scene View.

- [ ] **Step 4.3: Commit**

```bash
git add Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs
git commit -m "feat: añadir visualización Gizmos del balance de control con colores aliado/enemigo"
```

---

## Task 5: Verificación final de integración

**Files:** ninguno nuevo

- [ ] **Step 5.1: Verificar comportamiento completo en escena**

Con la escena corriendo y `debugMode = true`:
1. Mover una unidad aliada. Tras el siguiente refresco (`refreshInterval` segundos), la zona azul debe desplazarse.
2. Eliminar una unidad enemiga en tiempo de ejecución. La zona roja debe desaparecer en el siguiente refresco.
3. Comprobar que el campo `refreshInterval` es efectivamente configurable: cambiar a `0.5f` y a `2f` en el Inspector y observar la frecuencia de refresco en los colores.
4. Verificar que no hay degradación de framerate visible (el refresco periódico no debe ejecutarse en `Update`).

- [ ] **Step 5.2: Commit final**

```bash
git add Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs
git commit -m "feat: InfluenceMap completo — cálculo periódico, API pública y Gizmos de control"
```
