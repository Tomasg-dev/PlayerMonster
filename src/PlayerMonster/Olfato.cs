using UnityEngine;
using UnityEngine.UI;

namespace JugadorMonstruo;

/// <summary>
/// Olfato del monstruo: si lleva mucho tiempo sin tener a nadie cerca, "huele" al jugador más
/// cercano. Evita que esconderse alargue la partida sin fin.
///
/// Corre en el cliente del piloto, que ya conoce la posición de todos los jugadores. Pasado el
/// tiempo sin contacto, cada pocos segundos aparece durante un momento un rombo rojo en la dirección
/// del humano vivo más cercano (pegado al borde de la pantalla si queda fuera de la vista).
/// </summary>
internal static class Olfato
{
    private const float RadioContacto = 15f;
    private const float CadaSegundos = 12f;
    private const float DuracionMarca = 4f;

    private static float _ultimoContacto;
    private static float _siguienteRastro;
    private static float _marcaHasta;
    private static float _pedidoEn = -1f;

    private static GameObject? _raiz;
    private static Image? _marca;

    /// <summary>Al empezar a pilotar un enemigo nuevo: el contador empieza de cero.</summary>
    public static void Reiniciar()
    {
        _ultimoContacto = Time.time;
        _marcaHasta = 0f;
    }

    /// <summary>Cada frame en el cliente del piloto mientras el monstruo está vivo.</summary>
    public static void Tick(Enemy enemigo)
    {
        if (!Plugin.UsarOlfato.Value || !GameDirector.instance)
        {
            return;
        }

        Vector3 yo = enemigo.CenterTransform ? enemigo.CenterTransform.position : enemigo.transform.position;
        PlayerAvatar? cercano = null;
        float distancia = float.MaxValue;
        foreach (PlayerAvatar jugador in GameDirector.instance.PlayerList)
        {
            if (!jugador || jugador.isLocal || jugador.isDisabled)
            {
                continue;
            }
            float d = Vector3.Distance(yo, jugador.transform.position);
            if (d < distancia)
            {
                distancia = d;
                cercano = jugador;
            }
        }

        if (cercano == null || distancia <= RadioContacto)
        {
            _ultimoContacto = Time.time; // hay contacto (o no queda nadie a quien oler)
            return;
        }
        if (Time.time - _ultimoContacto < Plugin.OlfatoSegundos.Value)
        {
            return;
        }

        if (Time.time >= _siguienteRastro)
        {
            _siguienteRastro = Time.time + CadaSegundos;
            _marcaHasta = Time.time + DuracionMarca;
        }
        if (Time.time < _marcaHasta)
        {
            Dibujar(cercano.transform.position + Vector3.up);
        }
    }

    /// <summary>Cada frame desde el plugin: oculta la marca cuando deja de pedirse.</summary>
    public static void Mantener()
    {
        if (_raiz != null)
        {
            bool visible = Time.unscaledTime - _pedidoEn < 0.1f;
            if (_raiz.activeSelf != visible)
            {
                _raiz.SetActive(visible);
            }
        }
    }

    private static void Dibujar(Vector3 objetivo)
    {
        Camera camara = Camera.main;
        if (camara == null)
        {
            return;
        }
        Crear();

        // Viewport: (0,0) abajo-izquierda, (1,1) arriba-derecha; z < 0 = detrás de la cámara.
        Vector3 vista = camara.WorldToViewportPoint(objetivo);
        var punto = new Vector2(vista.x, vista.y);
        if (vista.z < 0f)
        {
            // Detrás: la proyección sale invertida. La reflejamos y la empujamos hacia fuera para
            // que acabe en el borde correcto ("date la vuelta por este lado").
            punto = Vector2.one - punto;
            punto = new Vector2(0.5f, 0.5f) + (punto - new Vector2(0.5f, 0.5f)).normalized * 2f;
        }
        punto.x = Mathf.Clamp(punto.x, 0.06f, 0.94f);
        punto.y = Mathf.Clamp(punto.y, 0.1f, 0.9f);

        RectTransform rect = _marca!.rectTransform;
        rect.anchorMin = rect.anchorMax = punto;
        // Parpadeo suave para que llame la atención.
        float alfa = 0.55f + 0.45f * Mathf.Sin(Time.time * 9f);
        _marca.color = new Color(1f, 0.1f, 0.1f, alfa);
        _pedidoEn = Time.unscaledTime;
    }

    private static void Crear()
    {
        if (_raiz != null)
        {
            return;
        }

        _raiz = new GameObject("PlayerMonster Olfato");
        Object.DontDestroyOnLoad(_raiz);
        _raiz.hideFlags = HideFlags.HideAndDontSave;

        var canvas = _raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 29980;
        var escalador = _raiz.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        // Un cuadrado girado 45 grados = un rombo. Sin imágenes: un Image sin sprite es un rectángulo liso.
        var objeto = new GameObject("Marca");
        objeto.transform.SetParent(_raiz.transform, false);
        _marca = objeto.AddComponent<Image>();
        _marca.raycastTarget = false;
        RectTransform rect = _marca.rectTransform;
        rect.sizeDelta = new Vector2(34f, 34f);
        rect.anchoredPosition = Vector2.zero;
        rect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        _raiz.SetActive(false);
    }
}
