using UnityEngine;

// =============================================================================
//  Capa de abstracción para mapas tácticos.
// -----------------------------------------------------------------------------
//  El objetivo de esta interfaz es que las capas que CONSUMEN un mapa táctico
//  (toma de decisiones tácticas en ComportamientoTactico, visualización en
//  Minimapa, etc.) no dependan de la implementación concreta
//  InfluenceMap.  Si en el futuro se añade un mapa de visibilidad, de coste de
//  terreno o cualquier otro mapa táctico, los consumidores no necesitan saber
//  contra qué tipo concreto están hablando: basta con que ese mapa implemente
//  IMapaTactico y se registre en el ServicioMapaTactico.
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
//  Localizador estático mínimo para que los consumidores resuelvan el mapa
//  táctico activo sin conocer su implementación concreta.  El mapa concreto se
//  auto-registra en su Awake/OnEnable.  Si en el futuro hay varios mapas, este
//  servicio puede ampliarse a un diccionario por nombre.
// =============================================================================
public static class ServicioMapaTactico
{
    public static IMapaTactico MapaPrimario { get; private set; }

    // Devuelve el mapa primario o null si nadie se ha registrado.
    public static void Registrar(IMapaTactico mapa)
    {
        if (mapa == null) return;
        MapaPrimario = mapa;
    }

    // Si el mapa que se desregistra es el primario, lo borra. Si no, no hace nada.
    public static void Quitar(IMapaTactico mapa)
    {
        if (MapaPrimario == mapa) MapaPrimario = null;
    }
}
