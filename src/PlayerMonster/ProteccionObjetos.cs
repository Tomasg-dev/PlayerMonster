using System.Collections.Generic;
using UnityEngine;

namespace JugadorMonstruo;

internal static class ProteccionObjetos
{
	private const float Radio = 4f;

	private const float Intervalo = 0.2f;

	private static readonly List<(HurtCollider collider, bool physLogicOriginal)> Modificados = new List<(HurtCollider, bool)>();

	private static readonly Collider[] Cercanos = new Collider[64];

	private static float _siguienteBarrido;

	public static void Activar(Enemy enemy)
	{
		Desactivar();
		if (Plugin.ProtegerObjetos.Value)
		{
			HurtCollider[] componentsInChildren = enemy.EnemyParent.GetComponentsInChildren<HurtCollider>(includeInactive: true);
			foreach (HurtCollider hurtCollider in componentsInChildren)
			{
				Modificados.Add((hurtCollider, hurtCollider.physLogic));
				hurtCollider.physLogic = false;
			}
		}
	}

	public static void Desactivar()
	{
		foreach (var (hurtCollider, physLogic) in Modificados)
		{
			if ((bool)hurtCollider)
			{
				hurtCollider.physLogic = physLogic;
			}
		}
		Modificados.Clear();
	}

	public static void Tick(Enemy enemy)
	{
		if (!Plugin.ProtegerObjetos.Value || Time.time < _siguienteBarrido)
		{
			return;
		}
		_siguienteBarrido = Time.time + 0.2f;
		int num = Physics.OverlapSphereNonAlloc(enemy.CenterTransform ? enemy.CenterTransform.position : enemy.transform.position, 4f, Cercanos, SemiFunc.LayerMaskGetPhysGrabObject(), QueryTriggerInteraction.Ignore);
		for (int i = 0; i < num; i++)
		{
			PhysGrabObject componentInParent = Cercanos[i].GetComponentInParent<PhysGrabObject>();
			if ((bool)componentInParent && (bool)componentInParent.impactDetector && componentInParent.impactDetector.isValuable && componentInParent.playerGrabbing.Count == 0)
			{
				componentInParent.OverrideIndestructible(0.5f);
			}
		}
	}
}
