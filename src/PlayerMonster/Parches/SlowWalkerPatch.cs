using HarmonyLib;
using UnityEngine;

namespace JugadorMonstruo.Parches;

/// <summary>
/// Trudge (EnemySlowWalker): lento y pesado. Su ataque es el mazazo original, que dura 5 s y lo
/// deja clavado en el sitio; solo puede orientarlo durante la primera mitad, igual que la IA.
/// </summary>
[HarmonyPatch(typeof(EnemySlowWalker))]
internal static class SlowWalkerPatch
{
	private const float MultiplicadorCorrer = 1.5f;

	private static bool _enAtaque;

	[HarmonyPrefix]
	[HarmonyPatch("Update")]
	private static bool UpdatePrefix(EnemySlowWalker __instance)
	{
		Enemy enemy = __instance.enemy;
		if (!Autoridad.Controla(enemy))
		{
			return true;
		}
		__instance.HeadLookAt();
		if (enemy.CurrentState == EnemyState.Despawn && !enemy.IsStunned())
		{
			__instance.UpdateState(EnemySlowWalker.State.Despawn);
		}
		if (enemy.IsStunned())
		{
			__instance.UpdateState(EnemySlowWalker.State.Stun);
		}
		EntradaMonstruo entrada = Autoridad.Entrada;
		switch (__instance.currentState)
		{
		case EnemySlowWalker.State.Attack:
			_enAtaque = true;
			__instance.StateAttack();
			break;
		case EnemySlowWalker.State.Stun:
			__instance.StateStun();
			break;
		case EnemySlowWalker.State.Despawn:
			__instance.StateDespawn();
			break;
		default:
			if (_enAtaque)
			{
				_enAtaque = false;
				Furia.FinDeSecuencia();
			}
			Controlar(__instance, enemy, entrada);
			break;
		}
		Rotar(__instance, entrada);
		__instance.TimerLogic();
		__instance.AttackOffsetLogic(); // el paso adelante del mazazo
		return false;
	}

	private static void Controlar(EnemySlowWalker trudge, Enemy enemy, EntradaMonstruo entrada)
	{
		if (entrada.Atacar && Furia.PuedeAtacar && !enemy.Jump.jumping)
		{
			trudge.UpdateState(EnemySlowWalker.State.Attack);
			return;
		}
		EnemyNavMeshAgent navMeshAgent = enemy.NavMeshAgent;
		if (entrada.Direccion == Vector3.zero)
		{
			navMeshAgent.ResetPath();
			trudge.UpdateState(EnemySlowWalker.State.Idle);
			return;
		}
		navMeshAgent.SetDestination(enemy.transform.position + entrada.Direccion * 3f);
		if (entrada.Correr)
		{
			navMeshAgent.OverrideAgent(navMeshAgent.DefaultSpeed * MultiplicadorCorrer, navMeshAgent.DefaultAcceleration, 0.2f);
		}
		trudge.UpdateState(EnemySlowWalker.State.Roam);
	}

	private static void Rotar(EnemySlowWalker trudge, EntradaMonstruo entrada)
	{
		bool atacando = trudge.currentState == EnemySlowWalker.State.Attack;
		Vector3 hacia = Vector3.zero;
		if (atacando)
		{
			if (trudge.stateTimer > 2.5f)
			{
				hacia = entrada.MiradaPlana;
			}
		}
		else if (trudge.currentState is EnemySlowWalker.State.Idle or EnemySlowWalker.State.Roam)
		{
			hacia = entrada.Direccion;
		}
		if (hacia != Vector3.zero)
		{
			trudge.rotationTarget = Quaternion.LookRotation(hacia);
		}
		trudge.horizontalRotationSpring.speed = atacando ? 15f : 5f;
		trudge.horizontalRotationSpring.damping = atacando ? 0.8f : 0.7f;
		trudge.transform.rotation = SemiFunc.SpringQuaternionGet(trudge.horizontalRotationSpring, trudge.rotationTarget);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnVision")]
	private static bool OnVisionPrefix(EnemySlowWalker __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnInvestigate")]
	private static bool OnInvestigatePrefix(EnemySlowWalker __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnGrabbed")]
	private static bool OnGrabbedPrefix(EnemySlowWalker __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}
}
