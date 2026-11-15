using UnityEngine;
using UnityEngine.UI;

namespace JugadorMonstruo;

/// <summary>
/// Mira del cazador: una cruz en el centro de la pantalla del piloto.
///
/// Es fiable porque el disparo apunta exactamente al punto que hay bajo la cruz: el cliente del
/// piloto lanza un rayo desde su cámara, calcula en qué punto del mundo acaba (EntradaMonstruo.PuntoMira)
/// y el anfitrión orienta el arma hacia ese punto. Antes el arma solo copiaba la dirección de la
/// cámara, y como la cámara no está donde el arma, el tiro se desviaba.
///
/// Se dibuja por encima de la oscuridad del cazador. Blanca en reposo, roja mientras apunta.
/// </summary>
internal static class Mira
{
    private static GameObject? _raiz;
    private static readonly Image[] Piezas = new Image[5];
    private static float _pedidaEn = -1f;

    /// <summary>Cada frame mientras el piloto es el cazador y está vivo.</summary>
    public static void Mostrar(bool apuntando)
    {
        Crear();
        Color color = apuntando ? new Color(1f, 0.2f, 0.15f, 0.95f) : new Color(1f, 1f, 1f, 0.75f);
        foreach (Image pieza in Piezas)
        {
            pieza.color = color;
        }
        // Al apuntar, la cruz se cierra un poco: se nota que "está fijando".
        float separacion = apuntando ? 10f : 16f;
        Piezas[0].rectTransform.anchoredPosition = new Vector2(0f, separacion);
        Piezas[1].rectTransform.anchoredPosition = new Vector2(0f, -separacion);
        Piezas[2].rectTransform.anchoredPosition = new Vector2(-separacion, 0f);
        Piezas[3].rectTransform.anchoredPosition = new Vector2(separacion, 0f);
        _pedidaEn = Time.unscaledTime;
    }

    /// <summary>Cada frame desde el plugin: oculta la mira cuando deja de pedirse.</summary>
    public static void Mantener()
    {
        if (_raiz != null)
        {
            bool visible = Time.unscaledTime - _pedidaEn < 0.1f;
            if (_raiz.activeSelf != visible)
            {
                _raiz.SetActive(visible);
            }
        }
    }

    private static void Crear()
    {
        if (_raiz != null)
        {
            return;
        }

        _raiz = new GameObject("PlayerMonster Mira");
        Object.DontDestroyOnLoad(_raiz);
        _raiz.hideFlags = HideFlags.HideAndDontSave;

        var canvas = _raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 29500; // por encima de la capa negra del cazador (29000), bajo el HUD
        var escalador = _raiz.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        // Cuatro rayitas (arriba, abajo, izquierda, derecha) y un punto central.
        Vector2 vertical = new(3f, 14f);
        Vector2 horizontal = new(14f, 3f);
        Piezas[0] = Pieza(vertical);
        Piezas[1] = Pieza(vertical);
        Piezas[2] = Pieza(horizontal);
        Piezas[3] = Pieza(horizontal);
        Piezas[4] = Pieza(new Vector2(4f, 4f));

        _raiz.SetActive(false);
    }

    private static Image Pieza(Vector2 tamano)
    {
        var objeto = new GameObject("Pieza");
        objeto.transform.SetParent(_raiz!.transform, false);
        var imagen = objeto.AddComponent<Image>();
        imagen.raycastTarget = false;
        RectTransform rect = imagen.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); // centro de la pantalla
        rect.sizeDelta = tamano;
        rect.anchoredPosition = Vector2.zero;
        return imagen;
    }
}
