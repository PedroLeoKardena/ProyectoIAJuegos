using UnityEngine;

// =============================================================================
//  HUD principal del juego.
// -----------------------------------------------------------------------------
//  Bloques que pinta:
//    - Esquina sup. izquierda  : Modo estratégico del bando aliado
//                                (Defensivo / Ofensivo / Guerra Total)
//                                + indicador del modo actual.
//    - Esquina sup. derecha    : Toggles de visualización
//                                (Minimapa, Debug estratégico, Influencia f).
//    - Centro superior         : Estado de captura / victoria.
//    - Persistente             : Botón "?" que muestra el panel de ayuda
//                                con todos los controles del proyecto.
//
// =============================================================================
[DisallowMultipleComponent]
public class HUDBotones : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Si está activo, se mostrará el panel de ayuda al pulsar la tecla configurada.")]
    public KeyCode teclaAyuda = KeyCode.H;

    [Tooltip("Tamaño base del texto en pantalla.")]
    public int fontSize = 14;

    [Tooltip("Mostrar el panel de ayuda al iniciar la escena (luego se oculta con la tecla).")]
    public bool ayudaVisibleAlInicio = false;

    // Referencias resueltas en Start.
    private ManagerEstrategico managerAliado;
    private ManagerEstrategico managerEnemigo;
    private DebugEstrategico   debugStrat;
    private Minimapa           minimapa;
    private CondicionVictoria  condicionAliada;
    private CondicionVictoria  condicionEnemiga;

    // Estado UI.
    private bool mostrarAyuda;
    private Vector2 scrollAyuda;
    private GUIStyle styleBoton, styleLabel, styleTitulo, styleAyuda, styleEstado;

    private void Start()
    {
        mostrarAyuda = ayudaVisibleAlInicio;

        // Buscar los ManagerEstrategico de ambos bandos: el aliado para los botones,
        // el enemigo para detectar su victoria también.
        foreach (var m in FindObjectsByType<ManagerEstrategico>(FindObjectsSortMode.None))
        {
            if      (m.faction == Faction.Aliado)  managerAliado  = m;
            else if (m.faction == Faction.Enemigo) managerEnemigo = m;
        }

        debugStrat = FindFirstObjectByType<DebugEstrategico>();
        minimapa   = FindFirstObjectByType<Minimapa>();

        if (managerAliado  != null) condicionAliada  = managerAliado .GetComponent<CondicionVictoria>();
        if (managerEnemigo != null) condicionEnemiga = managerEnemigo.GetComponent<CondicionVictoria>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(teclaAyuda)) mostrarAyuda = !mostrarAyuda;
    }

    private void OnGUI()
    {
        InicializarEstilos();

        DibujarPanelEstrategia();   // sup. izquierda
        DibujarPanelToggles();      // sup. derecha
        DibujarEstadoVictoria();    // centro superior
        DibujarBotonAyuda();        // esquina sup. derecha esquina
        if (mostrarAyuda) DibujarPanelAyuda();
    }

    // -----------------------------------------------------------------------
    //  Sección 1: panel de modo estratégico (sup. izquierda)
    // -----------------------------------------------------------------------
    private void DibujarPanelEstrategia()
    {
        const float W = 220f, H = 140f, M = 10f;
        GUI.Box(new Rect(M, M, W, H), "");

        GUI.Label(new Rect(M + 8, M + 4, W, 22), "ESTRATEGIA (Aliados)", styleTitulo);

        string modoActual = managerAliado != null && managerAliado.Contexto != null
            ? managerAliado.Contexto.modo.ToString()
            : "(sin manager)";
        GUI.Label(new Rect(M + 8, M + 26, W, 20),
                  $"Modo actual: <b>{modoActual}</b>", styleLabel);

        bool guerraActiva = managerAliado != null
            && managerAliado.Contexto != null
            && managerAliado.Contexto.guerraTotal;

        // Botones (deshabilitados durante Guerra Total)
        GUI.enabled = managerAliado != null && !guerraActiva;
        if (GUI.Button(new Rect(M + 8, M + 50, (W - 24) / 2, 26), "Defensivo [1]", styleBoton))
            managerAliado.SetModo(ModoEstrategico.Defensivo);
        if (GUI.Button(new Rect(M + 16 + (W - 24) / 2, M + 50, (W - 24) / 2, 26), "Ofensivo [2]", styleBoton))
            managerAliado.SetModo(ModoEstrategico.Ofensivo);

        GUI.enabled = managerAliado != null;
        Color prev = GUI.color;
        GUI.color = guerraActiva ? new Color(1f, 0.5f, 0.2f) : Color.white;
        if (GUI.Button(new Rect(M + 8, M + 84, W - 16, 30), "¡GUERRA TOTAL! [G]", styleBoton))
            managerAliado.ActivarGuerraTotal();
        GUI.color = prev;
        GUI.enabled = true;
    }

    // -----------------------------------------------------------------------
    //  Sección 2: panel de toggles de visualización (sup. derecha)
    // -----------------------------------------------------------------------
    private void DibujarPanelToggles()
    {
        const float W = 220f, H = 130f, M = 10f;
        float x = Screen.width - W - M;
        GUI.Box(new Rect(x, M, W, H), "");

        GUI.Label(new Rect(x + 8, M + 4, W, 22), "VISUALIZACIÓN", styleTitulo);

        // Minimapa
        bool miniVis = minimapa != null && minimapa.EsVisible;
        GUI.enabled = minimapa != null;
        if (GUI.Button(new Rect(x + 8, M + 26, W - 16, 26),
                       $"Minimapa: {(miniVis ? "ON" : "OFF")}  [M]", styleBoton))
            minimapa.ToggleVisible();

        // Debug estratégico
        bool debugVis = debugStrat != null && debugStrat.EsActivo;
        GUI.enabled = debugStrat != null;
        if (GUI.Button(new Rect(x + 8, M + 56, W - 16, 26),
                       $"Debug estratégico: {(debugVis ? "ON" : "OFF")}  [F1]", styleBoton))
            debugStrat.ToggleActivo();

        // Influencia en selección de objetivo (apartado f)
        // Recorremos todos los SelectorObjetivoInfluencia y los flippeamos en bloque.
        var selectores = FindObjectsByType<SelectorObjetivoInfluencia>(FindObjectsSortMode.None);
        bool hayInfluencia = selectores != null && selectores.Length > 0;
        bool todosOn = false;
        if (hayInfluencia)
        {
            todosOn = true;
            foreach (var s in selectores) if (!s.usarInfluencia) { todosOn = false; break; }
        }
        GUI.enabled = hayInfluencia;
        if (GUI.Button(new Rect(x + 8, M + 86, W - 16, 26),
                       $"Influencia objetivo: {(todosOn ? "ON" : "OFF")}  [I]", styleBoton))
        {
            bool nuevoEstado = !todosOn;
            foreach (var s in selectores) s.usarInfluencia = nuevoEstado;
        }
        GUI.enabled = true;
    }

    // -----------------------------------------------------------------------
    //  Sección 3: estado de victoria / derrota (centro superior).
    //  Considera AMBOS bandos: si gana el rojo se muestra DERROTA, si gana
    //  el azul VICTORIA.  Si ambos están capturando a la vez, se muestran
    //  los dos contadores en paralelo.
    // -----------------------------------------------------------------------
    private void DibujarEstadoVictoria()
    {
        if (condicionAliada == null && condicionEnemiga == null) return;

        const float W = 460f, H = 50f;
        float x = (Screen.width - W) / 2f;

        // Victoria aliada (verde)
        if (condicionAliada != null && condicionAliada.VictoriaDeclarada)
        {
            Color prev = GUI.color;
            GUI.color = new Color(0.2f, 1f, 0.2f);
            GUI.Box(new Rect(x, 10, W, H), "<b>¡VICTORIA ALIADOS!</b>", styleEstado);
            GUI.color = prev;
            return;
        }

        // Victoria enemiga (rojo)
        if (condicionEnemiga != null && condicionEnemiga.VictoriaDeclarada)
        {
            Color prev = GUI.color;
            GUI.color = new Color(1f, 0.25f, 0.25f);
            GUI.Box(new Rect(x, 10, W, H), "<b>¡DERROTA — GANAN LOS ENEMIGOS!</b>", styleEstado);
            GUI.color = prev;
            return;
        }

        // Aún no ha ganado nadie. Mostramos contadores SI alguien está capturando.
        float tA = condicionAliada  != null ? condicionAliada .TiempoCaptura : 0f;
        float tE = condicionEnemiga != null ? condicionEnemiga.TiempoCaptura : 0f;
        if (tA <= 0f && tE <= 0f) return;

        float maxA = condicionAliada  != null ? condicionAliada .tiempoNecesario : 20f;
        float maxE = condicionEnemiga != null ? condicionEnemiga.tiempoNecesario : 20f;

        string txt = "";
        if (tA > 0f) txt += $"<color=#80FF80>Aliados:  <b>{tA:F1}s / {maxA:F0}s</b></color>";
        if (tA > 0f && tE > 0f) txt += "    ";
        if (tE > 0f) txt += $"<color=#FF8080>Enemigos: <b>{tE:F1}s / {maxE:F0}s</b></color>";

        GUI.Box(new Rect(x, 10, W, H), txt, styleEstado);
    }

    // -----------------------------------------------------------------------
    //  Sección 4: botón de ayuda + panel
    // -----------------------------------------------------------------------
    private void DibujarBotonAyuda()
    {
        const float S = 32f, M = 10f;
        float x = Screen.width - S - M;
        float y = Screen.height - S - M;
        if (GUI.Button(new Rect(x, y, S, S), "?", styleBoton))
            mostrarAyuda = !mostrarAyuda;
    }

    private void DibujarPanelAyuda()
    {
        // Tamaño adaptativo
        float W = Mathf.Clamp(Screen.width  * 0.55f, 480f, 720f);
        float H = Mathf.Clamp(Screen.height * 0.70f, 360f, 620f);
        float x = (Screen.width  - W) / 2f;
        float y = (Screen.height - H) / 2f;

        GUI.Box(new Rect(x, y, W, H), "");

        // Título. Para cerrar se usa la tecla H o el botón "?" inferior derecho.
        GUI.Label(new Rect(x + 14, y + 8, W - 28, 22),
                  "<b>CONTROLES — Pulsa H o el botón ? para cerrar</b>", styleTitulo);

        // ScrollView para que, sea cual sea la resolución, el texto siempre se pueda leer.
        Rect viewport  = new Rect(x + 8, y + 34, W - 16, H - 42);
        Rect contenido = new Rect(0, 0, viewport.width - 20, 580);

        scrollAyuda = GUI.BeginScrollView(viewport, scrollAyuda, contenido);

        string txt =
            "<b>SELECCIÓN Y MOVIMIENTO</b>\n" +
            "  Click izquierdo ............... Seleccionar unidad / arrastrar = caja de selección\n" +
            "  Shift + click ................. Selección aditiva\n" +
            "  Click derecho ................. Mover unidades seleccionadas a punto\n" +
            "  Esc ........................... Deseleccionar todo\n" +
            "  F ............................. Formar formación con seleccionadas\n" +
            "  B ............................. Detener formación\n" +
            "\n" +
            "<b>ESTRATEGIA (apartados b/g/k)</b>\n" +
            "  1 ............................. Modo Defensivo (aliados)\n" +
            "  2 ............................. Modo Ofensivo (aliados)\n" +
            "  G ............................. ¡Guerra Total!\n" +
            "  F1 ............................ Mostrar/ocultar overlays de debug\n" +
            "\n" +
            "<b>MAPA TÁCTICO (apartados e/f)</b>\n" +
            "  M ............................. Mostrar/ocultar minimapa\n" +
            "  N ............................. Cambiar de vista del minimapa (si hay más de uno)\n" +
            "  I ............................. Activar/desactivar influencia en selección de objetivo\n" +
            "\n" +
            "<b>CÁMARA</b>\n" +
            "  W A S D / flechas ............. Desplazar cámara\n" +
            "  Shift + dirección ............. Acelerar\n" +
            "  Rueda del ratón ............... Zoom\n" +
            "  R / Home ...................... Reset cámara\n" +
            "\n" +
            "<b>OTROS</b>\n" +
            "  Espacio ....................... Recalcular A* (configurable por unidad)\n" +
            "  H / botón ? ................... Mostrar/ocultar esta ayuda";

        GUI.Label(new Rect(8, 0, contenido.width - 16, contenido.height), txt, styleAyuda);
        GUI.EndScrollView();
    }

    // -----------------------------------------------------------------------
    //  Estilos: se inicializan en el primer OnGUI.
    // -----------------------------------------------------------------------
    private void InicializarEstilos()
    {
        if (styleBoton != null) return;

        styleBoton = new GUIStyle(GUI.skin.button)
        {
            fontSize = fontSize,
            richText = true,
            alignment = TextAnchor.MiddleCenter
        };

        styleLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            richText = true,
            alignment = TextAnchor.MiddleLeft
        };
        styleLabel.normal.textColor = Color.white;

        styleTitulo = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize + 1,
            richText = true,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        styleTitulo.normal.textColor = new Color(1f, 0.85f, 0.25f);

        styleAyuda = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            richText = true,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };
        styleAyuda.normal.textColor = Color.white;

        styleEstado = new GUIStyle(GUI.skin.box)
        {
            fontSize = fontSize + 4,
            richText = true,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        styleEstado.normal.textColor = Color.white;
    }
}
