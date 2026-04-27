# Influence Map — Documento de Diseño

**Fecha:** 2026-04-27  
**Rama:** bloque2  
**Archivo objetivo:** `Assets/CarpetaScripts/ScriptsAI/InfluenceMap.cs`

---

## 1. Alcance

Implementar un Mapa de Influencia (`InfluenceMap`) como `MonoBehaviour` singleton independiente que:

- Calcula y almacena la influencia táctica por bando (aliado/enemigo) sobre cada nodo del grid existente.
- Se refresca periódicamente (no en cada frame).
- Expone una API pública de consulta.
- Visualiza el balance de control en el Scene View mediante Gizmos.

**Fuera de alcance:** integración con A* (se abordará en una iteración posterior).

---

## 2. Arquitectura

### Clase principal

`InfluenceMap : MonoBehaviour`

- Vive como componente en un GameObject de la escena.
- Singleton accesible via `InfluenceMap.Instance`.
- Referencia a `GridManager` (asignada en Inspector o buscada en `Awake`).
- Dos arrays paralelos al grid: `float[,] _allied` y `float[,] _enemy`.

### Actualización periódica

`InvokeRepeating("RefreshMap", 0f, refreshInterval)` llamado en `Start`.  
`refreshInterval` configurable en Inspector (rango sugerido: 0.5s – 2s, default: 1s).

---

## 3. Parámetros configurables (Inspector)

| Campo | Tipo | Default | Descripción |
|---|---|---|---|
| `refreshInterval` | float | 1.0 | Segundos entre refrescos del mapa |
| `influenceRadius` | float | 10.0 | Radio máximo de efecto por unidad (unidades de mundo) |
| `bosqueMultiplier` | float | 0.8 | Modificador terreno Bosque |
| `llanuraMultiplier` | float | 1.2 | Modificador terreno Llanura |
| `caminoMultiplier` | float | 1.0 | Modificador terreno Camino |
| `I0_InfanteriaPesada` | float | 15.0 | Potencia base InfanteriaPesada |
| `I0_Velites` | float | 8.0 | Potencia base Velites |
| `I0_Exploradores` | float | 5.0 | Potencia base Exploradores |
| `debugMode` | bool | false | Activa visualización Gizmos |
| `showNumericValues` | bool | false | Muestra valores numéricos en Scene View |

---

## 4. Lógica de cálculo — `RefreshMap()`

### Pasos

1. Poner a cero `_allied[x,z]` y `_enemy[x,z]` para todo el grid.
2. Obtener unidades: `GameObject.FindGameObjectsWithTag("Aliado")` y `"Enemigo"`.
3. Para cada unidad:
   - Obtener nodo origen: `GridManager.NodeFromWorldPoint(unit.position)`.
   - Leer `I₀` según `TerrainSpeedModifier.unitType` del GameObject.
   - Iterar sobre todos los nodos del grid dentro del radio `influenceRadius` usando `Vector3.Distance`.
4. Para cada nodo candidato dentro del radio:
   - Calcular `d = Vector3.Distance(originNode.worldPosition, candidateNode.worldPosition)`.
   - Aplicar fórmula: **`I_d = I₀ / √(1 + d)`**
   - Aplicar modificador de terreno del nodo candidato según su `terrainTag`.
   - Acumular en `_allied[x,z]` o `_enemy[x,z]` según el bando.

### Fórmula de caída

```
I_d = I₀ / √(1 + d)
```

- `d = 0` → `I_d = I₀` (máxima influencia en la propia celda).
- Siempre positiva; el radio actúa como corte explícito.
- Acumulación aditiva por bando.

### Modificadores de terreno

Aplicados sobre el nodo **destino** (donde se proyecta la influencia):

| terrainTag | Multiplicador |
|---|---|
| "Bosque" | 0.8 |
| "Llanura" | 1.2 |
| "Camino" | 1.0 |

### Identificación de bandos

- Bando aliado: Unity Tag `"Aliado"`.
- Bando enemigo: Unity Tag `"Enemigo"`.
- `I₀` leído del componente `TerrainSpeedModifier` (campo `unitType`). Si una unidad no tiene el componente, se usa `I0_Exploradores` como fallback.

---

## 5. API pública

```csharp
// Consulta por nodo
float GetAlliedInfluence(Node node)
float GetEnemyInfluence(Node node)
float GetControl(Node node)          // = allied - enemy

// Consulta por posición en el mundo
float GetAlliedInfluence(Vector3 worldPos)
float GetEnemyInfluence(Vector3 worldPos)
float GetControl(Vector3 worldPos)
```

Todos los métodos devuelven `0f` si el nodo está fuera del grid o es nulo.

---

## 6. Debug / Gizmos

Activo cuando `debugMode = true`, solo en Play Mode.

- Por cada nodo walkable del grid, dibuja un cubo plano (igual que `GridManager`) coloreado por balance de control:
  - Azul puro → control aliado máximo
  - Rojo puro → control enemigo máximo
  - Gris neutro → equilibrio (~0)
  - Transparente → sin influencia
- El color se interpola usando `Color.Lerp` entre los extremos, normalizado por el valor máximo observado en el frame.
- Si `showNumericValues = true`, dibuja el valor `GetControl(node)` con `UnityEditor.Handles.Label` sobre cada celda (solo en editor).

---

## 7. Restricciones

- Todo el código dentro de `Assets/`.
- No modificar `Node`, `Grid<T>`, `GridManager`, `AStarAlgorithm` ni `AStarPathfinder`.
- Todos los métodos y clases con comentario breve en español.
- Sin actualización en `Update`; únicamente refresco periódico.
- El bloque `showNumericValues` con `UnityEditor.Handles.Label` debe estar dentro de `#if UNITY_EDITOR` para evitar errores de compilación en builds.
