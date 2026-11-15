using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JugadorMonstruo;

internal static class Sentidos
{
	private struct Ruido
	{
		public Vector3 Posicion;

		public float Radio;

		public float Inicio;
	}

	private const float DuracionAnillo = 2.5f;

	private const float AlcanceOido = 45f;

	private static readonly List<Ruido> Ruidos = new List<Ruido>();

	private static readonly List<Image> Anillos = new List<Image>();

	private static GameObject? _raiz;

	private static Image? _negro;

	private static Sprite? _spriteAnillo;

	private static float _mareadoHasta;

	private static readonly List<Renderer> JugadoresOcultos = new List<Renderer>();

	private static float _siguienteBarridoJugadores;

	private static float _destello;

	public static void Marear(float segundos)
	{
		_mareadoHasta = Time.time + segundos;
	}

	private static float _pedidoEn = -1f;

	/// <summary>
	/// Cada frame desde el plugin. Los efectos de vista se apagan solos si Tick() deja de llamarse,
	/// pase lo que pase con el resto del mod: nunca deben sobrevivir a la partida.
	/// </summary>
	public static void Mantener()
	{
		if (_raiz != null && _raiz.activeSelf && Time.unscaledTime - _pedidoEn > 0.25f)
		{
			Ocultar();
		}
	}

	public static void Tick(TipoMonstruo tipo, Enemy enemy, EntradaMonstruo entrada)
	{
		_pedidoEn = Time.unscaledTime;
		if (!Plugin.UsarSentidos.Value || !PostProcessing.Instance)
		{
			Ocultar();
			return;
		}
		GameObject gameObject = Plugin.Instancia.gameObject;
		bool flag = tipo == TipoMonstruo.Hunter;
		switch (tipo)
		{
		case TipoMonstruo.Hunter:
			PostProcessing.Instance.SaturationOverride(-100f, 5f, 2f, 0.2f, gameObject);
			break;
		case TipoMonstruo.Runner:
		{
			bool flag2 = entrada.Correr || entrada.Atacar;
			PostProcessing.Instance.VignetteOverride(Color.black, flag2 ? 0.75f : 0.45f, 1f, 2f, 2f, 0.2f, gameObject);
			break;
		}
		case TipoMonstruo.Animal:
			CameraZoom.Instance.OverrideZoomSet(100f, 0.2f, 2f, 2f, gameObject, 60);
			break;
		case TipoMonstruo.Tumbler:
			if (Time.time < _mareadoHasta)
			{
				float num = Mathf.Sin(Time.time * 7f);
				CameraZoom.Instance.OverrideZoomSet(75f + num * 18f, 0.2f, 6f, 2f, gameObject, 60);
				CameraNoise.Instance.Override(0.2f, 0.2f);
				PostProcessing.Instance.SaturationOverride(-40f, 4f, 2f, 0.2f, gameObject);
			}
			break;
		case TipoMonstruo.Bowtie:
			if (!entrada.Atacar)
			{
				PostProcessing.Instance.ResolutionOverride(0.35f);
			}
			break;
		}
		if (!flag)
		{
			Ocultar();
			return;
		}
		OcultarJugadores();
		Crear();
		_raiz.SetActive(value: true);
		_destello = Mathf.MoveTowards(_destello, 0f, Time.deltaTime / 1.5f);
		float a = Mathf.Clamp01(Plugin.CegueraHunter.Value) * (1f - 0.5f * _destello);
		_negro.color = new Color(0f, 0f, 0f, a);
		DibujarAnillos(enemy);
	}

	public static void RuidoDePrueba()
	{
		Camera main = Camera.main;
		if (!(main == null) && !(EnemyDirector.instance == null) && SemiFunc.RunIsLevel())
		{
			Vector3 vector = main.transform.right * Random.Range(-5f, 5f);
			Vector3 vector2 = main.transform.position + main.transform.forward * Random.Range(6f, 14f) + vector;
			EnemyDirector.instance.SetInvestigate(vector2, 10f);
			Plugin.Log.LogInfo($"Sentidos: ruido de prueba en {vector2}.");
		}
	}

	private static void OcultarJugadores()
	{
		if (Time.time < _siguienteBarridoJugadores || !GameDirector.instance)
		{
			return;
		}
		_siguienteBarridoJugadores = Time.time + 0.5f;
		foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
		{
			if (!player || player.isLocal || !player.playerAvatarVisuals)
			{
				continue;
			}
			Renderer[] componentsInChildren = player.playerAvatarVisuals.GetComponentsInChildren<Renderer>(includeInactive: true);
			foreach (Renderer renderer in componentsInChildren)
			{
				if (!renderer.forceRenderingOff)
				{
					renderer.forceRenderingOff = true;
					JugadoresOcultos.Add(renderer);
				}
			}
		}
	}

	private static void MostrarJugadores()
	{
		foreach (Renderer jugadoresOculto in JugadoresOcultos)
		{
			if ((bool)jugadoresOculto)
			{
				jugadoresOculto.forceRenderingOff = false;
			}
		}
		JugadoresOcultos.Clear();
		_siguienteBarridoJugadores = 0f;
	}

	public static void Ocultar()
	{
		if (JugadoresOcultos.Count > 0)
		{
			MostrarJugadores();
		}
		if (_raiz != null && _raiz.activeSelf)
		{
			_raiz.SetActive(value: false);
		}
		Ruidos.Clear();
	}

	public static void RegistrarRuido(Vector3 posicion, float radio)
	{
		Enemy enemigo = Piloto.Enemigo;
		if (!(enemigo == null) && Piloto.Estado.Tipo == TipoMonstruo.Hunter && Ruidos.Count <= 40)
		{
			Ruidos.Add(new Ruido
			{
				Posicion = posicion,
				Radio = radio,
				Inicio = Time.time
			});
			float num = Vector3.Distance(posicion, enemigo.transform.position);
			if (num < 45f)
			{
				_destello = Mathf.Max(_destello, Mathf.Lerp(1f, 0.3f, num / 45f));
			}
		}
	}

	private static void DibujarAnillos(Enemy enemy)
	{
		Camera main = Camera.main;
		Ruidos.RemoveAll((Ruido r) => Time.time - r.Inicio > 2.5f);
		int num = 0;
		if (main != null)
		{
			foreach (Ruido ruido in Ruidos)
			{
				if (num >= Anillos.Count)
				{
					break;
				}
				if (!(Vector3.Distance(ruido.Posicion, enemy.transform.position) > 45f))
				{
					Vector3 vector = main.WorldToViewportPoint(ruido.Posicion);
					if (!(vector.z <= 0f))
					{
						float num2 = (Time.time - ruido.Inicio) / 2.5f;
						Image image = Anillos[num++];
						RectTransform rectTransform = image.rectTransform;
						Vector2 anchorMin = (rectTransform.anchorMax = new Vector2(Mathf.Clamp01(vector.x), Mathf.Clamp01(vector.y)));
						rectTransform.anchorMin = anchorMin;
						rectTransform.anchoredPosition = Vector2.zero;
						float num3 = Mathf.Clamp(ruido.Radio * 60f / Mathf.Max(vector.z, 2f), 40f, 420f);
						rectTransform.sizeDelta = Vector2.one * num3 * Mathf.Lerp(0.5f, 1.4f, num2);
						image.color = new Color(1f, 1f, 1f, 1f - num2);
						image.enabled = true;
					}
				}
			}
		}
		for (int num4 = num; num4 < Anillos.Count; num4++)
		{
			Anillos[num4].enabled = false;
		}
	}

	private static void Crear()
	{
		if (!(_raiz != null))
		{
			_raiz = new GameObject("PlayerMonster Sentidos");
			Object.DontDestroyOnLoad(_raiz);
			_raiz.hideFlags = HideFlags.HideAndDontSave;
			Canvas canvas = _raiz.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 29000;
			CanvasScaler canvasScaler = _raiz.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
			canvasScaler.matchWidthOrHeight = 0.5f;
			GameObject gameObject = new GameObject("Negro");
			gameObject.transform.SetParent(_raiz.transform, worldPositionStays: false);
			_negro = gameObject.AddComponent<Image>();
			_negro.sprite = CrearSpriteOscuridad();
			_negro.raycastTarget = false;
			RectTransform rectTransform = _negro.rectTransform;
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			Vector2 offsetMin = (rectTransform.offsetMax = Vector2.zero);
			rectTransform.offsetMin = offsetMin;
			_spriteAnillo = CrearSpriteAnillo();
			for (int i = 0; i < 24; i++)
			{
				GameObject gameObject2 = new GameObject("Anillo");
				gameObject2.transform.SetParent(_raiz.transform, worldPositionStays: false);
				Image image = gameObject2.AddComponent<Image>();
				image.sprite = _spriteAnillo;
				image.raycastTarget = false;
				image.enabled = false;
				Anillos.Add(image);
			}
		}
	}

	private static Sprite CrearSpriteOscuridad()
	{
		Texture2D texture2D = new Texture2D(128, 128, TextureFormat.RGBA32, mipChain: false);
		texture2D.wrapMode = TextureWrapMode.Clamp;
		Vector2 b = new Vector2(63.5f, 63.5f);
		for (int i = 0; i < 128; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				float value = Vector2.Distance(new Vector2(j, i), b) / 64f;
				float a = Mathf.Lerp(0.72f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.8f, value)));
				texture2D.SetPixel(j, i, new Color(1f, 1f, 1f, a));
			}
		}
		texture2D.Apply();
		return Sprite.Create(texture2D, new Rect(0f, 0f, 128f, 128f), new Vector2(0.5f, 0.5f));
	}

	private static Sprite CrearSpriteAnillo()
	{
		Texture2D texture2D = new Texture2D(128, 128, TextureFormat.RGBA32, mipChain: false);
		Vector2 b = new Vector2(63.5f, 63.5f);
		for (int i = 0; i < 128; i++)
		{
			for (int j = 0; j < 128; j++)
			{
				float num = Vector2.Distance(new Vector2(j, i), b) / 64f;
				float a = Mathf.Clamp01(1f - Mathf.Abs(num - 0.85f) / 0.08f);
				texture2D.SetPixel(j, i, new Color(1f, 1f, 1f, a));
			}
		}
		texture2D.Apply();
		return Sprite.Create(texture2D, new Rect(0f, 0f, 128f, 128f), new Vector2(0.5f, 0.5f));
	}
}
