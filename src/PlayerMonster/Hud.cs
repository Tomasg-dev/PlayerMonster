using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JugadorMonstruo;

internal static class Hud
{
	private static GameObject? _raiz;

	private static RectTransform? _relleno;

	private static Image? _rellenoImagen;

	private static TextMeshProUGUI? _vida;

	private static RectTransform? _presencia;

	private static Image? _presenciaImagen;

	private static readonly Color ColorPresencia = new Color(0.9f, 0.7f, 0.15f);

	private static readonly Color ColorPresenciaAlta = new Color(1f, 0.35f, 0.1f);

	private static readonly Color ColorFuria = new Color(0.85f, 0.1f, 0.1f);

	private static readonly Color ColorRecargando = new Color(0.55f, 0.35f, 0.1f);

	private static readonly Color ColorAgotado = new Color(0.35f, 0.35f, 0.35f);

	public static void Tick()
	{
		Enemy enemigo = Piloto.Enemigo;
		if (!(enemigo != null) || !enemigo || !enemigo.EnemyParent.Spawned)
		{
			if (_raiz != null && _raiz.activeSelf)
			{
				_raiz.SetActive(value: false);
			}
			return;
		}
		Crear();
		if (!_raiz.activeSelf)
		{
			_raiz.SetActive(value: true);
		}
		EstadoMonstruo estado = Piloto.Estado;
		_relleno.anchorMax = new Vector2(estado.Furia, 1f);
		Image? rellenoImagen = _rellenoImagen;
		Color color;
		if (estado.Agotado)
		{
			color = ColorAgotado;
		}
		else
		{
			color = ((estado.Furia >= 1f) ? ColorFuria : ColorRecargando);
		}
		rellenoImagen.color = color;
		// Barra de presencia: se llena mientras estás junto a los jugadores. A partir del 75 % avisa.
		_presencia.anchorMax = new Vector2(Mathf.Clamp01(estado.Presencia), 1f);
		bool alta = estado.Presencia >= 0.75f || estado.Retirada;
		_presenciaImagen.color = (alta ? ColorPresenciaAlta : ColorPresencia);
		string aviso = (estado.Retirada ? ("   <color=#FF6030>" + Idioma.T("RETIRADA", "RETREAT") + "</color>") : (alta ? ("   <color=#FF6030>" + Idioma.T("¡MUÉVETE!", "MOVE ON!") + "</color>") : ""));
		if (estado.VidaMaxima > 0)
		{
			_vida.text = $"{Idioma.T("VIDA", "HP")} {estado.Vida} / {estado.VidaMaxima}{aviso}";
		}
		if ((bool)BigMessageUI.instance && (bool)BigMessageUI.instance.Text && _vida.font != BigMessageUI.instance.Text.font)
		{
			_vida.font = BigMessageUI.instance.Text.font;
		}
	}

	private static void Crear()
	{
		if (!(_raiz != null))
		{
			_raiz = new GameObject("PlayerMonster HUD");
			Object.DontDestroyOnLoad(_raiz);
			_raiz.hideFlags = HideFlags.HideAndDontSave;
			Canvas canvas = _raiz.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 29990;
			CanvasScaler canvasScaler = _raiz.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
			canvasScaler.matchWidthOrHeight = 0.5f;
			RectTransform rectTransform = Rectangulo("Fondo", _raiz.transform, new Color(0f, 0f, 0f, 0.6f));
			Vector2 anchorMin = (rectTransform.anchorMax = new Vector2(0.5f, 0f));
			rectTransform.anchorMin = anchorMin;
			rectTransform.pivot = new Vector2(0.5f, 0f);
			rectTransform.anchoredPosition = new Vector2(0f, 60f);
			rectTransform.sizeDelta = new Vector2(420f, 18f);
			_relleno = Rectangulo("Relleno", rectTransform, ColorFuria);
			_relleno.anchorMin = Vector2.zero;
			_relleno.anchorMax = Vector2.one;
			_relleno.offsetMin = new Vector2(2f, 2f);
			_relleno.offsetMax = new Vector2(-2f, -2f);
			_rellenoImagen = _relleno.GetComponent<Image>();
			// Segunda barra, fina, debajo de la de furia: presencia.
			RectTransform fondoPresencia = Rectangulo("Fondo presencia", _raiz.transform, new Color(0f, 0f, 0f, 0.6f));
			fondoPresencia.anchorMin = (fondoPresencia.anchorMax = new Vector2(0.5f, 0f));
			fondoPresencia.pivot = new Vector2(0.5f, 0f);
			fondoPresencia.anchoredPosition = new Vector2(0f, 46f);
			fondoPresencia.sizeDelta = new Vector2(420f, 10f);
			_presencia = Rectangulo("Presencia", fondoPresencia, ColorPresencia);
			_presencia.anchorMin = Vector2.zero;
			_presencia.anchorMax = new Vector2(0f, 1f);
			_presencia.offsetMin = new Vector2(2f, 2f);
			_presencia.offsetMax = new Vector2(-2f, -2f);
			_presenciaImagen = _presencia.GetComponent<Image>();
			GameObject gameObject = new GameObject("Vida");
			gameObject.transform.SetParent(_raiz.transform, worldPositionStays: false);
			_vida = gameObject.AddComponent<TextMeshProUGUI>();
			_vida.alignment = TextAlignmentOptions.Center;
			_vida.fontSize = 30f;
			_vida.color = Color.white;
			_vida.raycastTarget = false;
			_vida.richText = true;
			_vida.textWrappingMode = TextWrappingModes.NoWrap;
			RectTransform rectTransform2 = _vida.rectTransform;
			anchorMin = (rectTransform2.anchorMax = new Vector2(0.5f, 0f));
			rectTransform2.anchorMin = anchorMin;
			rectTransform2.pivot = new Vector2(0.5f, 0f);
			rectTransform2.anchoredPosition = new Vector2(0f, 84f);
			rectTransform2.sizeDelta = new Vector2(600f, 40f);
		}
	}

	private static RectTransform Rectangulo(string nombre, Transform padre, Color color)
	{
		GameObject gameObject = new GameObject(nombre);
		gameObject.transform.SetParent(padre, worldPositionStays: false);
		Image image = gameObject.AddComponent<Image>();
		image.color = color;
		image.raycastTarget = false;
		return image.rectTransform;
	}
}
