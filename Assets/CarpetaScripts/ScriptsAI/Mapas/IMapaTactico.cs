using UnityEngine;

// =============================================================================
//  Capa de abstracción para mapas tácticos.
// -----------------------------------------------------------------------------
//  El objetivo de esta interfaz es que las capas que CONSUMEN un mapa táctico
//  (toma de decisiones tácticas en ComportamientoTactico, visualización en
//  Minimapa, etc.) no dependan de una implementación concreta de un mapa táctico.
// =============================================================================
public interface IMapaTactico
{
    // Identificador legible (se muestra en el HUD del minimapa).
    string Nombre { get; }

    // Dimensiones del grid lógico sobre el que está definido el mapa.
    int Width  { get; }
    int Height { get; }

    // Indica si la celda (x,z) es transitable (para que las vistas puedan
    // distinguir terreno bloqueado del resto).
    bool IsWalkable(int x, int z);

    // ------------------------------------------------------------------
    //  Canales aliado / enemigo
    //  Para mapas tácticos que no representen bandos (visibilidad p.ej.)
    //  basta con devolver 0 en uno de los dos canales.
    // ------------------------------------------------------------------
    float ValorAliadoEn (int x, int z);
    float ValorEnemigoEn(int x, int z);
    float MaxAliado ();
    float MaxEnemigo();

    // Balance neto (aliado - enemigo) en una posición del mundo.
    // Para mapas no de influencia, devolver 0.
    float Control(Vector3 worldPos);
}


// =============================================================================
//  ServicioMapaTactico
// -----------------------------------------------------------------------------
//  Localizador estático para que los consumidores resuelvan los mapas tácticos
//  activos sin conocer su implementación concreta.  Cada mapa concreto se
//  auto-registra en su OnEnable y se quita en OnDisable.
//
//  Soporta VARIOS mapas a la vez. El "MapaPrimario" es el primero que se registró — típicamente
//  el InfluenceMap.
// =============================================================================
public static class ServicioMapaTactico
{
    private static readonly System.Collections.Generic.List<IMapaTactico> _mapas
        = new System.Collections.Generic.List<IMapaTactico>();

    // Mapa principal: el primero registrado. Coincide con InfluenceMap en escenas normales.
    public static IMapaTactico MapaPrimario => _mapas.Count > 0 ? _mapas[0] : null;

    // Lista de TODOS los mapas registrados (solo lectura para el cliente).
    public static System.Collections.Generic.IReadOnlyList<IMapaTactico> Mapas => _mapas;

    public static void Registrar(IMapaTactico mapa)
    {
        if (mapa == null) return;
        if (!_mapas.Contains(mapa)) _mapas.Add(mapa);
    }

    public static void Quitar(IMapaTactico mapa)
    {
        if (mapa == null) return;
        _mapas.Remove(mapa);
    }

    // Búsqueda por nombre (usa la propiedad IMapaTactico.Nombre, ej. "Influencia", "Visibilidad").
    public static IMapaTactico GetMapa(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;
        foreach (var m in _mapas)
            if (m != null && m.Nombre == nombre) return m;
        return null;
    }
}
