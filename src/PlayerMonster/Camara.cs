using System.Collections.Generic;
using UnityEngine;

namespace JugadorMonstruo;

internal static class Camara
{
	private static readonly List<Renderer> Ocultos = new List<Renderer>();

	private static Enemy? _enemigoOculto;

	public static void Seguir(Enemy enemy)
	{
		Camera main = Camera.main;
		if (!(main == null) && !(CameraPosition.instance == null))
		{
			if (Plugin.PrimeraPersona.Value)
			{
				OcultarModelo(enemy);
				Vector3 position;
				if (enemy.HasVision && (bool)enemy.Vision.VisionTransform)
				{
					position = enemy.Vision.VisionTransform.position;
				}
				else
				{
					position = (enemy.CenterTransform ? enemy.CenterTransform.position : (enemy.transform.position + Vector3.up));
				}
				CameraPosition.instance.OverridePosition(position);
			}
			else
			{
				MostrarModelo();
				Vector3 centro = (enemy.CenterTransform ? enemy.CenterTransform.position : (enemy.transform.position + Vector3.up));
				EnTerceraPersona(main, centro);
			}
		}
	}

	public static void Mirar(Vector3 centro)
	{
		Camera main = Camera.main;
		if (!(main == null) && !(CameraPosition.instance == null))
		{
			MostrarModelo();
			EnTerceraPersona(main, centro);
		}
	}

	private static void EnTerceraPersona(Camera camara, Vector3 centro)
	{
		Vector3 vector = centro + Vector3.up * 0.6f;
		Vector3 vector2 = -camara.transform.forward;
		float num = Plugin.DistanciaCamara.Value;
		if (Physics.SphereCast(vector, 0.3f, vector2, out var hitInfo, num, SemiFunc.LayerMaskGetVisionObstruct(), QueryTriggerInteraction.Ignore))
		{
			num = Mathf.Max(0.2f, hitInfo.distance);
		}
		CameraPosition.instance.OverridePosition(vector + vector2 * num);
	}

	private static void OcultarModelo(Enemy enemy)
	{
		if (_enemigoOculto == enemy)
		{
			return;
		}
		MostrarModelo();
		_enemigoOculto = enemy;
		Renderer[] componentsInChildren = enemy.EnemyParent.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if (!(renderer is ParticleSystemRenderer) && !(renderer is LineRenderer) && !renderer.forceRenderingOff)
			{
				renderer.forceRenderingOff = true;
				Ocultos.Add(renderer);
			}
		}
	}

	public static void MostrarModelo()
	{
		foreach (Renderer oculto in Ocultos)
		{
			if ((bool)oculto)
			{
				oculto.forceRenderingOff = false;
			}
		}
		Ocultos.Clear();
		_enemigoOculto = null;
	}

	public static void Alternar()
	{
		Plugin.PrimeraPersona.Value = !Plugin.PrimeraPersona.Value;
		Plugin.Log.LogInfo("Cámara: " + (Plugin.PrimeraPersona.Value ? "primera" : "tercera") + " persona.");
	}
}
