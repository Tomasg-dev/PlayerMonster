using UnityEngine;
using UnityEngine.Rendering;

namespace JugadorMonstruo;

internal static class VistaModelo
{
	private static readonly Vector3 Lugar = new Vector3(0f, -3000f, 0f);

	private static GameObject? _raiz;

	private static Transform? _pivote;

	private static Camera? _camara;

	private static RenderTexture? _textura;

	private static EnemySetup? _mostrado;

	public static Texture? Textura => _textura;

	public static void Mostrar(EnemySetup? setup)
	{
		Crear();
		if (setup != _mostrado)
		{
			_mostrado = setup;
			Construir(setup);
		}
		_camara.enabled = true;
		_pivote.Rotate(0f, 40f * Time.unscaledDeltaTime, 0f, Space.World);
	}

	public static void Ocultar()
	{
		if (_camara != null)
		{
			_camara.enabled = false;
		}
	}

	private static void Construir(EnemySetup? setup)
	{
		for (int num = _pivote.childCount - 1; num >= 0; num--)
		{
			Object.Destroy(_pivote.GetChild(num).gameObject);
		}
		_pivote.localRotation = Quaternion.Euler(0f, 160f, 0f);
		GameObject gameObject = null;
		if (setup != null)
		{
			foreach (PrefabRef spawnObject in setup.spawnObjects)
			{
				if (spawnObject != null && spawnObject.Prefab != null && (bool)spawnObject.Prefab.GetComponent<EnemyParent>())
				{
					gameObject = spawnObject.Prefab;
					break;
				}
			}
		}
		if (gameObject == null)
		{
			return;
		}
		Transform transform = new GameObject("Estatua").transform;
		transform.SetParent(_pivote, worldPositionStays: false);
		Matrix4x4 worldToLocalMatrix = gameObject.transform.worldToLocalMatrix;
		bool flag = false;
		Bounds bounds = default;
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if (!renderer.enabled || !ActivoEnElPrefab(renderer.transform, gameObject.transform))
			{
				continue;
			}
			Mesh mesh;
			if (renderer is MeshRenderer)
			{
				mesh = (renderer.GetComponent<MeshFilter>() ? renderer.GetComponent<MeshFilter>().sharedMesh : null);
			}
			else
			{
				mesh = ((!(renderer is SkinnedMeshRenderer skinnedMeshRenderer)) ? null : skinnedMeshRenderer.sharedMesh);
			}
			Mesh mesh2 = mesh;
			if (!(mesh2 == null))
			{
				Matrix4x4 matrix4x = worldToLocalMatrix * renderer.transform.localToWorldMatrix;
				GameObject gameObject2 = new GameObject(renderer.name);
				gameObject2.transform.SetParent(transform, worldPositionStays: false);
				gameObject2.transform.localPosition = matrix4x.GetColumn(3);
				gameObject2.transform.localRotation = matrix4x.rotation;
				gameObject2.transform.localScale = matrix4x.lossyScale;
				gameObject2.AddComponent<MeshFilter>().sharedMesh = mesh2;
				MeshRenderer meshRenderer = gameObject2.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterials = renderer.sharedMaterials;
				meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
				if (!flag)
				{
					bounds = meshRenderer.bounds;
					flag = true;
				}
				else
				{
					bounds.Encapsulate(meshRenderer.bounds);
				}
			}
		}
		if (flag)
		{
			transform.position += _pivote.position - bounds.center;
			float num2 = Mathf.Max(bounds.extents.magnitude, 0.3f);
			_camara.transform.position = Lugar + new Vector3(0f, num2 * 0.25f, (0f - num2) * 2.6f);
			_camara.transform.LookAt(Lugar);
		}
	}

	private static bool ActivoEnElPrefab(Transform pieza, Transform raiz)
	{
		Transform transform = pieza;
		while (transform != null && transform != raiz)
		{
			if (!transform.gameObject.activeSelf)
			{
				return false;
			}
			transform = transform.parent;
		}
		return true;
	}

	private static void Crear()
	{
		if (!(_raiz != null))
		{
			_raiz = new GameObject("PlayerMonster Vista");
			Object.DontDestroyOnLoad(_raiz);
			_raiz.hideFlags = HideFlags.HideAndDontSave;
			_raiz.transform.position = Lugar;
			_pivote = new GameObject("Pivote").transform;
			_pivote.SetParent(_raiz.transform, worldPositionStays: false);
			_textura = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32)
			{
				name = "PlayerMonster Vista"
			};
			GameObject gameObject = new GameObject("Camara");
			gameObject.transform.SetParent(_raiz.transform, worldPositionStays: false);
			_camara = gameObject.AddComponent<Camera>();
			_camara.targetTexture = _textura;
			_camara.clearFlags = CameraClearFlags.Color;
			_camara.backgroundColor = new Color(0.04f, 0.02f, 0.02f, 1f);
			_camara.fieldOfView = 30f;
			_camara.nearClipPlane = 0.05f;
			_camara.farClipPlane = 60f;
			_camara.enabled = false;
			Light light = gameObject.AddComponent<Light>();
			light.type = LightType.Point;
			light.range = 40f;
			light.intensity = 2.5f;
			light.color = new Color(1f, 0.92f, 0.85f);
		}
	}
}
