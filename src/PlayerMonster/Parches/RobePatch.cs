using HarmonyLib;
using UnityEngine;

namespace JugadorMonstruo.Parches;

/// <summary>
/// Robe: camina, corre y da su zarpazo original. El ataque es de un solo golpe: se lanza al
/// pulsar con la furia llena y dura lo que dura el estado Attack del juego (2 s).
/// </summary>
[HarmonyPatch(typeof(EnemyRobe))]
internal static class RobePatch
{
	private const float MultiplicadorCorrer = 2f;

	private static bool _enAtaque;

	[HarmonyPrefix]
	[HarmonyPatch("Update")]
	private static bool UpdatePrefix(EnemyRobe __instance)
	{
		Enemy enemy = __instance.enemy;
		if (!Autoridad.Controla(enemy))
		{
			return true;
		}
		__instance.EndPieceLogic();
		__instance.RigidbodyRotationSpeed();
		if (enemy.IsStunned())
		{
			__instance.UpdateState(EnemyRobe.State.Stun);
		}
		else if (enemy.CurrentState == EnemyState.Despawn)
		{
			__instance.UpdateState(EnemyRobe.State.Despawn);
		}
		EntradaMonstruo entrada = Autoridad.Entrada;
		switch (__instance.currentState)
		{
		case EnemyRobe.State.Attack:
			_enAtaque = true;
			__instance.StateAttack();
			// Al terminar, la IA pasaría a buscar al jugador (SeekPlayer): lo cortamos en Idle.
			if (__instance.currentState != EnemyRobe.State.Attack)
			{
				__instance.UpdateState(EnemyRobe.State.Idle);
			}
			break;
		case EnemyRobe.State.Stun:
			__instance.StateStun();
			break;
		case EnemyRobe.State.Despawn:
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
		return false;
	}

	private static void Controlar(EnemyRobe robe, Enemy enemy, EntradaMonstruo entrada)
	{
		if (entrada.Atacar && Furia.PuedeAtacar)
		{
			robe.UpdateState(EnemyRobe.State.Attack);
			return;
		}
		EnemyNavMeshAgent navMeshAgent = enemy.NavMeshAgent;
		if (entrada.Direccion == Vector3.zero)
		{
			navMeshAgent.ResetPath();
			robe.UpdateState(EnemyRobe.State.Idle);
			return;
		}
		navMeshAgent.SetDestination(enemy.transform.position + entrada.Direccion * 3f);
		if (entrada.Correr)
		{
			navMeshAgent.OverrideAgent(navMeshAgent.DefaultSpeed * MultiplicadorCorrer, navMeshAgent.DefaultAcceleration * MultiplicadorCorrer, 0.2f);
		}
		robe.UpdateState(EnemyRobe.State.Roam);
	}

	private static void Rotar(EnemyRobe robe, EntradaMonstruo entrada)
	{
		// Atacando o quieto mira hacia donde mira el piloto; andando, hacia donde camina.
		Vector3 hacia = robe.currentState == EnemyRobe.State.Attack ? entrada.MiradaPlana : entrada.Direccion;
		if (robe.currentState is EnemyRobe.State.Stun or EnemyRobe.State.Despawn)
		{
			hacia = Vector3.zero;
		}
		if (hacia != Vector3.zero)
		{
			robe.rotationTarget = Quaternion.LookRotation(hacia);
		}
		robe.transform.rotation = SemiFunc.SpringQuaternionGet(robe.rotationSpring, robe.rotationTarget);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnVision")]
	private static bool OnVisionPrefix(EnemyRobe __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnInvestigate")]
	private static bool OnInvestigatePrefix(EnemyRobe __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnGrabbed")]
	private static bool OnGrabbedPrefix(EnemyRobe __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}
}
