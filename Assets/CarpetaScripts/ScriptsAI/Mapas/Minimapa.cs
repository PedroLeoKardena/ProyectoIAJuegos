using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// =============================================================================
//  Minimapa táctico genérico.
// -----------------------------------------------------------------------------
//  Crea en runtime un Canvas + RawImage en una esquina de la pantalla y vuelca
//  los datos de un IMapaTactico cualquiera a una Texture2D.
//
//  La clase NO conoce a InfluenceMap directamente: trabaja contra el contrato
//  IMapaTactico resuelto vía ServicioMapaTactico, por lo que un futuro mapa de
//  visibilidad / coste de terreno / etc. se podría visualizar aquí cambiando
//  únicamente el TipoVista y añadiendo una rama al dispatcher PintarPixel().
//
//  Vista por defecto (Influencia): azul = control aliado, rojo = control
//  enemigo, magenta = celda disputada por ambos bandos, gris oscuro = celda
//  no transitable.
//
//  Controles:
//      M  -> mostrar / ocultar minimapa.
//      N  -> alternar entre vistas registradas (cuando haya más de una).
//
// =============================================================================
[DisallowMultipleComponent]
public class Minimapa : MonoBehaviour
{
    // -----------------------------------------------------------------------
    //  Configuración pública (Inspector)
    // -----------------------------------------------------------------------
    [Header("Referencias")]
    [Tooltip("Override opcional. Si se deja vacío, se resolverá ServicioMapaTactico.MapaPrimario al iniciar.\n" +
             "El campo está tipado como MonoBehaviour para que Unity lo serialice; debe implementar IMapaTactico.")]
    [SerializeField] private MonoBehaviour mapaOverride;
    private IMapaTactico mapa;

    [Header("Refresco")]
    [Tooltip("Segundos entre redibujados de la textura (≥ refreshInterval del mapa táctico).")]
    [SerializeField] private float refreshInterval = 0.5f;

    [Header("Estilo del HUD")]
    [Tooltip("Tamaño en píxeles de pantalla del minimapa.")]
    [SerializeField] private Vector2 sizePx = new Vector2(220f, 220f);
    [Tooltip("Margen contra la esquina inferior derecha.")]
    [SerializeField] private Vector2 marginPx = new Vector2(20f, 20f);
    [Tooltip("Color de fondo para celdas no transitables.")]
    [SerializeField] private Color nonWalkableColor = new Color(0.12f, 0.12f, 0.12f, 1f);
    [Tooltip("Color para celdas transitables sin influencia (fondo del minimapa).")]
    [SerializeField] private Color emptyCellColor   = new Color(0.25f, 0.25f, 0.25f, 1f);

    [Header("Controles")]
    [SerializeField] private KeyCode toggleVisibleKey = KeyCode.M;
    [SerializeField] private KeyCode cycleViewKey     = KeyCode.N;
    [Tooltip("Mostrar el minimapa al iniciar la escena.")]
    [SerializeField] private bool visibleAtStart = true;

    // -----------------------------------------------------------------------
    //  Tipos de vista (preparado para varios mapas tácticos).
    // -----------------------------------------------------------------------
    public enum TipoVista
    {
        Influencia,   // Aliados azul, enemigos rojo, magenta = disputado.
        Visibilidad,  // Amarillo claro = celda expuesta. Gris/negro = celda cubierta.
    }

    [Tooltip("Lista ordenada de vistas que se podrán alternar con la tecla N.")]
    [SerializeField] private List<TipoVista> vistasRegistradas = new List<TipoVista>
    {
        TipoVista.Influencia,
        TipoVista.Visibilidad
    };
    [SerializeField] private int vistaActualIdx = 0;

    // -----------------------------------------------------------------------
    //  Estado interno
    // -----------------------------------------------------------------------
    private Canvas    canvas;
    private RawImage  rawImage;
    private Text      etiquetaVista;     // Texto pequeño que indica qué vista se está mostrando.
    private Texture2D textura;
    private Color32[] pixelBuffer;
    private float     timer;
    private int       texW, texH;

    // -----------------------------------------------------------------------
    //  Ciclo de Unity
    // -----------------------------------------------------------------------
    private void Start()
    {
        // Resolución del mapa: override por inspector → servicio locator → fallo.
        if (mapaOverride is IMapaTactico inyectado) mapa = inyectado;
        if (mapa == null) mapa = ServicioMapaTactico.MapaPrimario;

        if (mapa == null)
        {
            Debug.LogError("[Minimapa] No hay ningún IMapaTactico registrado en ServicioMapaTactico.");
            enabled = false;
            return;
        }

        if (ServicioMapaTactico.GetMapa("Visibilidad") != null
            && !vistasRegistradas.Contains(TipoVista.Visibilidad))
        {
            vistasRegistradas.Add(TipoVista.Visibilidad);
            Debug.Log("[Minimapa] Auto-registrada vista Visibilidad (detectado MapaVisibilidad en escena).");
        }

        ConstruirHUD();
        canvas.enabled = visibleAtStart;
    }

    // API pública para que el HUDBotones pueda alternar la visibilidad sin simular teclas.
    public void ToggleVisible() { if (canvas != null) canvas.enabled = !canvas.enabled; }
    public bool EsVisible       => canvas != null && canvas.enabled;

    private void Update()
    {
        // Toggle visibilidad
        if (Input.GetKeyDown(toggleVisibleKey) && canvas != null)
            canvas.enabled = !canvas.enabled;

        // Cambiar de vista (cuando haya varias registradas)
        if (Input.GetKeyDown(cycleViewKey) && vistasRegistradas.Count > 1)
        {
            vistaActualIdx = (vistaActualIdx + 1) % vistasRegistradas.Count;
            ActualizarEtiqueta();
            timer = refreshInterval; // Forzar redibujado inmediato.
        }

        if (canvas == null || !canvas.enabled) return;

        timer += Time.deltaTime;
        if (timer >= refreshInterval)
        {
            timer = 0f;
            RedibujarTextura();
        }
    }

    // -----------------------------------------------------------------------
    //  Construcción del HUD (Canvas + RawImage + etiqueta)
    // -----------------------------------------------------------------------
    private void ConstruirHUD()
    {
        // Canvas dedicado para no chocar con HUDs existentes.
        GameObject canvasGO = new GameObject("MinimapaCanvas");
        canvasGO.transform.SetParent(this.transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Marco/fondo
        GameObject frameGO = new GameObject("Frame");
        frameGO.transform.SetParent(canvasGO.transform, false);
        Image frame = frameGO.AddComponent<Image>();
        frame.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform frameRT = frame.rectTransform;
        frameRT.anchorMin = new Vector2(1f, 0f);
        frameRT.anchorMax = new Vector2(1f, 0f);
        frameRT.pivot     = new Vector2(1f, 0f);
        frameRT.anchoredPosition = new Vector2(-marginPx.x, marginPx.y);
        frameRT.sizeDelta = sizePx + new Vector2(8f, 24f); // hueco para etiqueta arriba

        // RawImage que mostrará la textura del mapa.
        GameObject rawGO = new GameObject("MapaRaw");
        rawGO.transform.SetParent(frameGO.transform, false);
        rawImage = rawGO.AddComponent<RawImage>();
        RectTransform rawRT = rawImage.rectTransform;
        rawRT.anchorMin = new Vector2(0.5f, 0f);
        rawRT.anchorMax = new Vector2(0.5f, 0f);
        rawRT.pivot     = new Vector2(0.5f, 0f);
        rawRT.anchoredPosition = new Vector2(0f, 4f);
        rawRT.sizeDelta = sizePx;

        // Etiqueta superior con el nombre de la vista actual.
        GameObject lblGO = new GameObject("EtiquetaVista");
        lblGO.transform.SetParent(frameGO.transform, false);
        etiquetaVista = lblGO.AddComponent<Text>();
        etiquetaVista.text = "Mapa: Influencia";
        etiquetaVista.alignment = TextAnchor.UpperCenter;
        etiquetaVista.color = Color.white;
        etiquetaVista.fontSize = 12;
        etiquetaVista.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform lblRT = etiquetaVista.rectTransform;
        lblRT.anchorMin = new Vector2(0f, 1f);
        lblRT.anchorMax = new Vector2(1f, 1f);
        lblRT.pivot     = new Vector2(0.5f, 1f);
        lblRT.anchoredPosition = new Vector2(0f, -2f);
        lblRT.sizeDelta = new Vector2(0f, 18f);

        // Crear textura del tamaño del grid.
        texW = Mathf.Max(1, mapa.Width);
        texH = Mathf.Max(1, mapa.Height);
        textura = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point; // pixel art = celdas nítidas
        textura.wrapMode   = TextureWrapMode.Clamp;
        rawImage.texture   = textura;

        pixelBuffer = new Color32[texW * texH];
        ActualizarEtiqueta();
        RedibujarTextura();
    }

    private void ActualizarEtiqueta()
    {
        if (etiquetaVista == null) return;
        TipoVista v = vistasRegistradas[vistaActualIdx];
        IMapaTactico mapaVista = ResolverMapaParaVista(v);
        string nombreMapa = (mapaVista != null) ? mapaVista.Nombre : "(no registrado)";
        etiquetaVista.text = $"Mapa: {nombreMapa} ({v})  [{toggleVisibleKey}]ocultar  [{cycleViewKey}]cambiar";
    }

    // -----------------------------------------------------------------------
    //  Resolver el IMapaTactico que corresponde a una vista.
    //  Influencia  → busca el mapa con Nombre "Influencia" (o cae al primario).
    //  Visibilidad → busca el mapa con Nombre "Visibilidad".
    // -----------------------------------------------------------------------
    private IMapaTactico ResolverMapaParaVista(TipoVista vista)
    {
        switch (vista)
        {
            case TipoVista.Visibilidad:
                return ServicioMapaTactico.GetMapa("Visibilidad");
            case TipoVista.Influencia:
            default:
                return ServicioMapaTactico.GetMapa("Influencia") ?? mapa;
        }
    }

    // -----------------------------------------------------------------------
    //  Redibujado de la textura
    // -----------------------------------------------------------------------
    private void RedibujarTextura()
    {
        if (textura == null) return;

        TipoVista vista = vistasRegistradas[vistaActualIdx];
        IMapaTactico mapaVista = ResolverMapaParaVista(vista);

        // Si el mapa de esta vista no está registrado, dejamos la textura vacía
        if (mapaVista == null)
        {
            for (int i = 0; i < pixelBuffer.Length; i++) pixelBuffer[i] = nonWalkableColor;
            textura.SetPixels32(pixelBuffer);
            textura.Apply(false);
            return;
        }

        // Por seguridad si el grid cambió en runtime.
        if (mapaVista.Width != texW || mapaVista.Height != texH)
        {
            texW = Mathf.Max(1, mapaVista.Width);
            texH = Mathf.Max(1, mapaVista.Height);
            textura.Reinitialize(texW, texH);
            pixelBuffer = new Color32[texW * texH];
        }

        float maxAllied = Mathf.Max(0.001f, mapaVista.MaxAliado());
        float maxEnemy  = Mathf.Max(0.001f, mapaVista.MaxEnemigo());

        for (int z = 0; z < texH; z++)
        {
            for (int x = 0; x < texW; x++)
            {
                int idx = z * texW + x;
                pixelBuffer[idx] = PintarPixel(vista, mapaVista, x, z, maxAllied, maxEnemy);
            }
        }

        textura.SetPixels32(pixelBuffer);
        textura.Apply(false);
    }

    private Color PintarPixel(TipoVista vista, IMapaTactico m, int x, int z, float maxAllied, float maxEnemy)
    {
        if (!m.IsWalkable(x, z)) return nonWalkableColor;

        switch (vista)
        {
            case TipoVista.Influencia:  return PintarInfluencia (m, x, z, maxAllied, maxEnemy);
            case TipoVista.Visibilidad: return PintarVisibilidad(m, x, z, maxAllied);
            default: return emptyCellColor;
        }
    }

    // Vista combinada azul/rojo/magenta.
    private Color PintarInfluencia(IMapaTactico m, int x, int z, float maxAllied, float maxEnemy)
    {
        float a = Mathf.Clamp01(m.ValorAliadoEn(x, z)  / maxAllied);
        float e = Mathf.Clamp01(m.ValorEnemigoEn(x, z) / maxEnemy);

        if (a < 0.01f && e < 0.01f) return emptyCellColor;

        // Curva gamma para que zonas con poca influencia destaquen.
        a = Mathf.Pow(a, 0.6f);
        e = Mathf.Pow(e, 0.6f);

        // Mezcla: rojo = enemigo, azul = aliado, magenta donde se solapan.
        Color col = new Color(e, 0f, a, 1f);

        // Mantener un fondo gris cuando ambos son muy bajos para que el píxel no quede negro.
        float intensidad = Mathf.Max(a, e);
        return Color.Lerp(emptyCellColor, col, intensidad);
    }

    // Mapa mono-canal: ValorAliadoEn = ValorEnemigoEn = exposición de la celda
    //   amarillo claro = celda muy expuesta (rayos llegan lejos sin chocar).
    //   gris oscuro    = celda muy cubierta (rayos cortan rápido por obstáculos).
    private Color PintarVisibilidad(IMapaTactico m, int x, int z, float maxValor)
    {
        float v = Mathf.Clamp01(m.ValorAliadoEn(x, z) / maxValor);
        v = Mathf.Pow(v, 1.4f);
        // Color base: amarillo cálido. Mezclamos con el fondo gris según intensidad.
        Color expuesta = new Color(1f, 0.95f, 0.2f, 1f);
        return Color.Lerp(emptyCellColor, expuesta, v);
    }

    private void OnDestroy()
    {
        if (textura != null) Destroy(textura);
    }
}
