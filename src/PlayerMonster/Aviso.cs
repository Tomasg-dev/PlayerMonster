using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JugadorMonstruo;

internal static class Aviso
{
	private static GameObject? _raiz;

	private static TextMeshProUGUI? _texto;

	private static string _ultimo = "";

	private static float _visibleHasta;

	private static float _golpe;

	public static void Mostrar(string mensaje, float tamano, Color color)
	{
		Crear();
		if (!(_texto == null))
		{
			if (mensaje != _ultimo)
			{
				_ultimo = mensaje;
				_texto.text = mensaje;
				_golpe = 1f;
			}
			_texto.fontSize = tamano;
			_texto.color = color;
			_visibleHasta = Time.unscaledTime + 0.15f;
		}
	}

	public static void Tick()
	{
		if (_raiz == null || _texto == null)
		{
			return;
		}
		bool flag = Time.unscaledTime < _visibleHasta;
		if (_raiz.activeSelf != flag)
		{
			_raiz.SetActive(flag);
		}
		if (!flag)
		{
			_ultimo = "";
			return;
		}
		_golpe = Mathf.MoveTowards(_golpe, 0f, Time.unscaledDeltaTime * 3f);
		_texto.rectTransform.localScale = Vector3.one * (1f + 0.5f * _golpe * _golpe);
		_texto.rectTransform.anchoredPosition = Random.insideUnitCircle * (2f + 10f * _golpe);
		if ((bool)BigMessageUI.instance && (bool)BigMessageUI.instance.Text && _texto.font != BigMessageUI.instance.Text.font)
		{
			_texto.font = BigMessageUI.instance.Text.font;
		}
	}

	private static void Crear()
	{
		if (!(_raiz != null))
		{
			_raiz = new GameObject("PlayerMonster Aviso");
			Object.DontDestroyOnLoad(_raiz);
			_raiz.hideFlags = HideFlags.HideAndDontSave;
			Canvas canvas = _raiz.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 30000;
			CanvasScaler canvasScaler = _raiz.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
			canvasScaler.matchWidthOrHeight = 0.5f;
			GameObject gameObject = new GameObject("Texto");
			gameObject.transform.SetParent(_raiz.transform, worldPositionStays: false);
			_texto = gameObject.AddComponent<TextMeshProUGUI>();
			_texto.alignment = TextAlignmentOptions.Center;
			_texto.textWrappingMode = TextWrappingModes.NoWrap;
			_texto.raycastTarget = false;
			_texto.fontStyle = FontStyles.Bold;
			RectTransform rectTransform = _texto.rectTransform;
			Vector2 anchorMin = (rectTransform.anchorMax = new Vector2(0.5f, 0.62f));
			rectTransform.anchorMin = anchorMin;
			rectTransform.sizeDelta = new Vector2(1800f, 300f);
			_raiz.SetActive(value: false);
		}
	}
}
