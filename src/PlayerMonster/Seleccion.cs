using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JugadorMonstruo;

internal static class Seleccion
{
	private class Opcion
	{
		public TipoMonstruo Tipo;

		public EnemySetup? Setup;

		public string Nombre = "";

		public int Dificultad = 1;

		public bool PermitidoPorNivel;
	}

	private static readonly Dictionary<TipoMonstruo, float> Bloqueos = new Dictionary<TipoMonstruo, float>();

	private static readonly List<Opcion> Opciones = new List<Opcion>();

	private static int _nivelPreparado = -1;

	private static int _indice;

	private static float[]? _disponibilidad;

	private static GameObject? _raiz;

	private static RawImage? _imagen;

	private static TextMeshProUGUI? _titulo;

	private static TextMeshProUGUI? _nombre;

	private static TextMeshProUGUI? _estado;

	private static TextMeshProUGUI? _pie;

	private static float _visibleHasta;

	public static TipoMonstruo? Cursor
	{
		get
		{
			if (!Plugin.ElegirMonstruo.Value || _indice < 0 || _indice >= Opciones.Count)
			{
				return null;
			}
			return Opciones[_indice].Tipo;
		}
	}

	private static float BloqueadoHasta(TipoMonstruo tipo)
	{
		if (!Bloqueos.TryGetValue(tipo, out var value))
		{
			return 0f;
		}
		return value;
	}

	private static bool Disponible(Opcion o)
	{
		if (o.PermitidoPorNivel)
		{
			return Time.time >= BloqueadoHasta(o.Tipo);
		}
		return false;
	}

	public static void Bloquear(TipoMonstruo tipo)
	{
		Preparar();
		float num = (Opciones.FirstOrDefault((Opcion o) => o.Tipo == tipo)?.Dificultad ?? 2) switch
		{
			1 => Plugin.BloqueoDificultad1.Value, 
			2 => Plugin.BloqueoDificultad2.Value, 
			_ => Plugin.BloqueoDificultad3.Value, 
		};
		Bloqueos[tipo] = Time.time + num;
		Plugin.Log.LogInfo($"Selección: {tipo} bloqueado {num:0} s.");
	}

	public static float[] Disponibilidad()
	{
		Preparar();
		float[] array = new float[Enum.GetValues(typeof(TipoMonstruo)).Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = -1f;
		}
		foreach (Opcion opcione in Opciones)
		{
			array[(int)opcione.Tipo] = (opcione.PermitidoPorNivel ? Mathf.Max(0f, BloqueadoHasta(opcione.Tipo) - Time.time) : (-1f));
		}
		return array;
	}

	public static TipoMonstruo Resolver(TipoMonstruo? preferida)
	{
		Preparar();
		if (!Plugin.MonstruoAleatorio.Value && !Plugin.ElegirMonstruo.Value)
		{
			return Plugin.MonstruoDePrueba.Value;
		}
		if (Opciones.Count == 0)
		{
			return Plugin.MonstruoDePrueba.Value;
		}
		Opcion opcion = ((Plugin.ElegirMonstruo.Value && preferida.HasValue) ? Opciones.FirstOrDefault((Opcion o) => o.Tipo == preferida.Value && Disponible(o)) : null);
		if (opcion == null)
		{
			List<Opcion> list = Opciones.Where(Disponible).ToList();
			if (list.Count == 0)
			{
				list = (from o in Opciones
					where o.PermitidoPorNivel
					orderby BloqueadoHasta(o.Tipo)
					select o).Take(1).ToList();
			}
			if (list.Count == 0)
			{
				list = Opciones.ToList();
			}
			opcion = list[UnityEngine.Random.Range(0, list.Count)];
		}
		return opcion.Tipo;
	}

	private static float Valor(Opcion o)
	{
		if (_disponibilidad == null || (int)o.Tipo >= _disponibilidad.Length)
		{
			return 0f;
		}
		return _disponibilidad[(int)o.Tipo];
	}

	public static void Empezar(float[]? disponibilidad)
	{
		_nivelPreparado = -1;
		Preparar();
		_disponibilidad = disponibilidad;
		_indice = Mathf.Max(0, Opciones.FindIndex((Opcion o) => Valor(o) == 0f));
	}

	public static void Mostrar(float[]? disponibilidad)
	{
		if (!Plugin.ElegirMonstruo.Value)
		{
			return;
		}
		Preparar();
		if (disponibilidad != null)
		{
			_disponibilidad = disponibilidad;
		}
		if (Opciones.Count == 0)
		{
			return;
		}
		Keyboard current = Keyboard.current;
		if (current != null)
		{
			if (current[Key.RightArrow].wasPressedThisFrame)
			{
				_indice = (_indice + 1) % Opciones.Count;
			}
			if (current[Key.LeftArrow].wasPressedThisFrame)
			{
				_indice = (_indice - 1 + Opciones.Count) % Opciones.Count;
			}
		}
		_indice = Mathf.Clamp(_indice, 0, Opciones.Count - 1);
		Opcion opcion = Opciones[_indice];
		float num = Valor(opcion);
		Crear();
		VistaModelo.Mostrar(opcion.Setup);
		_imagen.texture = VistaModelo.Textura;
		_nombre.text = opcion.Nombre.ToUpperInvariant();
		if (num < 0f)
		{
			_estado.text = "<color=#888888>" + Idioma.T("Aún no en este nivel", "Not on this level yet") + "</color>";
			_imagen.color = new Color(0.35f, 0.35f, 0.35f);
		}
		else if (num > 0f)
		{
			_estado.text = $"<color=#888888>{Idioma.T("Bloqueado", "Locked")} {Mathf.CeilToInt(num)} s</color>";
			_imagen.color = new Color(0.35f, 0.35f, 0.35f);
		}
		else
		{
			_estado.text = "<color=#FF4040>" + Idioma.T("Disponible", "Available") + "</color>";
			_imagen.color = Color.white;
		}
		_pie.text = $"<  {_indice + 1} / {Opciones.Count}  >\n<size=75%><color=#AAAAAA>{Idioma.T("Flechas izquierda / derecha", "Left / right arrows")}</color></size>";
		TMP_FontAsset tMP_FontAsset = (((bool)BigMessageUI.instance && (bool)BigMessageUI.instance.Text) ? BigMessageUI.instance.Text.font : null);
		if (tMP_FontAsset != null && _nombre.font != tMP_FontAsset)
		{
			TextMeshProUGUI? titulo = _titulo;
			TextMeshProUGUI? nombre = _nombre;
			TextMeshProUGUI? estado = _estado;
			TMP_FontAsset tMP_FontAsset2 = (_pie.font = tMP_FontAsset);
			TMP_FontAsset tMP_FontAsset4 = (estado.font = tMP_FontAsset2);
			TMP_FontAsset font = (nombre.font = tMP_FontAsset4);
			titulo.font = font;
		}
		_visibleHasta = Time.unscaledTime + 0.15f;
	}

	public static void Tick()
	{
		if (_raiz == null)
		{
			return;
		}
		bool flag = Time.unscaledTime < _visibleHasta;
		if (_raiz.activeSelf != flag)
		{
			_raiz.SetActive(flag);
			if (!flag)
			{
				VistaModelo.Ocultar();
			}
		}
	}

	private static void Preparar()
	{
		LevelGenerator instance = LevelGenerator.Instance;
		int num = (instance ? instance.GetInstanceID() : 0);
		if (num == _nivelPreparado && Opciones.Count > 0)
		{
			return;
		}
		_nivelPreparado = num;
		Opciones.Clear();
		EnemyDirector instance2 = EnemyDirector.instance;
		int num2 = (RunManager.instance ? RunManager.instance.levelsCompleted : 0);
		int[] array = ((!instance2) ? new int[4] : new int[4] { 0, instance2.amountCurve1Value, instance2.amountCurve2Value, instance2.amountCurve3Value });
		int num3;
		if (array[3] > 0)
		{
			num3 = 3;
		}
		else
		{
			num3 = ((array[2] > 0) ? 2 : ((array[1] > 0) ? 1 : 0));
		}
		TipoMonstruo[] array2 = (TipoMonstruo[])Enum.GetValues(typeof(TipoMonstruo));
		for (int i = 0; i < array2.Length; i++)
		{
			TipoMonstruo tipo = array2[i];
			EnemySetup enemySetup = Autoridad.SetupDe(tipo);
			EnemyParent enemyParent = enemySetup?.spawnObjects.Where((PrefabRef o) => o != null && o.Prefab != null).Select((PrefabRef o) => o.Prefab.GetComponent<EnemyParent>()).FirstOrDefault((EnemyParent p) => p != null);
			if (!(enemySetup == null) && !(enemyParent == null))
			{
				int num4 = (int)(enemyParent.difficulty + 1);
				bool flag = !enemySetup.levelsCompletedCondition || (num2 >= enemySetup.levelsCompletedMin && (enemySetup.levelsCompletedMax == 0 || num2 <= enemySetup.levelsCompletedMax));
				Opciones.Add(new Opcion
				{
					Tipo = tipo,
					Setup = enemySetup,
					Nombre = (string.IsNullOrEmpty(enemyParent.enemyName) ? tipo.ToString() : enemyParent.enemyName),
					Dificultad = num4,
					PermitidoPorNivel = (flag && num4 <= num3)
				});
			}
		}
		Opciones.Sort((Opcion a, Opcion b) => a.Dificultad.CompareTo(b.Dificultad));
		if (Opciones.Count > 0 && !Opciones.Any((Opcion o) => o.PermitidoPorNivel))
		{
			int minima = Opciones.Min((Opcion o) => o.Dificultad);
			foreach (Opcion item in Opciones.Where((Opcion o) => o.Dificultad == minima))
			{
				item.PermitidoPorNivel = true;
			}
		}
		if (Mensajes.SoyAutoridad)
		{
			Plugin.Log.LogInfo("Selección: nivel " + (num2 + 1) + $", cupos del juego d1={array[1]} d2={array[2]} d3={array[3]} -> " + string.Join(", ", Opciones.Select((Opcion o) => string.Format("{0}(d{1}{2})", o.Nombre, o.Dificultad, o.PermitidoPorNivel ? "" : ", no"))));
		}
	}

	private static void Crear()
	{
		if (!(_raiz != null))
		{
			_raiz = new GameObject("PlayerMonster Seleccion");
			UnityEngine.Object.DontDestroyOnLoad(_raiz);
			_raiz.hideFlags = HideFlags.HideAndDontSave;
			Canvas canvas = _raiz.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 29995;
			CanvasScaler canvasScaler = _raiz.AddComponent<CanvasScaler>();
			canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
			canvasScaler.matchWidthOrHeight = 0.5f;
			GameObject gameObject = new GameObject("Panel");
			gameObject.transform.SetParent(_raiz.transform, worldPositionStays: false);
			Image image = gameObject.AddComponent<Image>();
			image.color = new Color(0f, 0f, 0f, 0.78f);
			image.raycastTarget = false;
			RectTransform rectTransform = image.rectTransform;
			Vector2 anchorMin = (rectTransform.anchorMax = new Vector2(1f, 0.5f));
			rectTransform.anchorMin = anchorMin;
			rectTransform.pivot = new Vector2(1f, 0.5f);
			rectTransform.anchoredPosition = new Vector2(-50f, 0f);
			rectTransform.sizeDelta = new Vector2(400f, 600f);
			GameObject gameObject2 = new GameObject("Franja");
			gameObject2.transform.SetParent(gameObject.transform, worldPositionStays: false);
			Image image2 = gameObject2.AddComponent<Image>();
			image2.color = new Color(0.75f, 0.08f, 0.08f);
			image2.raycastTarget = false;
			RectTransform rectTransform2 = image2.rectTransform;
			rectTransform2.anchorMin = new Vector2(0f, 1f);
			rectTransform2.anchorMax = new Vector2(1f, 1f);
			rectTransform2.pivot = new Vector2(0.5f, 1f);
			rectTransform2.sizeDelta = new Vector2(0f, 6f);
			_titulo = Texto(gameObject.transform, Idioma.T("ELIGE TU FORMA", "CHOOSE YOUR FORM"), 34f, -48f, 50f);
			_titulo.color = new Color(1f, 0.25f, 0.25f);
			GameObject gameObject3 = new GameObject("Vista");
			gameObject3.transform.SetParent(gameObject.transform, worldPositionStays: false);
			_imagen = gameObject3.AddComponent<RawImage>();
			_imagen.raycastTarget = false;
			RectTransform rectTransform3 = _imagen.rectTransform;
			anchorMin = (rectTransform3.anchorMax = new Vector2(0.5f, 1f));
			rectTransform3.anchorMin = anchorMin;
			rectTransform3.pivot = new Vector2(0.5f, 1f);
			rectTransform3.anchoredPosition = new Vector2(0f, -90f);
			rectTransform3.sizeDelta = new Vector2(340f, 340f);
			_nombre = Texto(gameObject.transform, "", 44f, -470f, 56f);
			_estado = Texto(gameObject.transform, "", 28f, -516f, 40f);
			_pie = Texto(gameObject.transform, "", 30f, -565f, 70f);
			_raiz.SetActive(value: false);
		}
	}

	private static TextMeshProUGUI Texto(Transform padre, string contenido, float tamano, float y, float alto)
	{
		GameObject gameObject = new GameObject("Texto");
		gameObject.transform.SetParent(padre, worldPositionStays: false);
		TextMeshProUGUI textMeshProUGUI = gameObject.AddComponent<TextMeshProUGUI>();
		textMeshProUGUI.text = contenido;
		textMeshProUGUI.fontSize = tamano;
		textMeshProUGUI.alignment = TextAlignmentOptions.Center;
		textMeshProUGUI.richText = true;
		textMeshProUGUI.raycastTarget = false;
		textMeshProUGUI.textWrappingMode = TextWrappingModes.NoWrap;
		RectTransform rectTransform = textMeshProUGUI.rectTransform;
		rectTransform.anchorMin = new Vector2(0f, 1f);
		rectTransform.anchorMax = new Vector2(1f, 1f);
		rectTransform.pivot = new Vector2(0.5f, 0.5f);
		rectTransform.anchoredPosition = new Vector2(0f, y);
		rectTransform.sizeDelta = new Vector2(0f, alto);
		return textMeshProUGUI;
	}
}
