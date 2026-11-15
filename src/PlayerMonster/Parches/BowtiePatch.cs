using HarmonyLib;
using UnityEngine;

namespace JugadorMonstruo.Parches;

[HarmonyPatch(typeof(EnemyBowtie))]
internal static class BowtiePatch
{
	private const float MultiplicadorCorrer = 1.6f;

	private static bool _enAtaque;

	[HarmonyPrefix]
	[HarmonyPatch("Update")]
	private static bool UpdatePrefix(EnemyBowtie __instance)
	{
		Enemy enemy = __instance.enemy;
		if (!Autoridad.Controla(enemy))
		{
			return true;
		}
		__instance.HurtColliderLeaveLogic();
		__instance.SpringLogic();
		__instance.PlayerEyesLogic();
		if (!LevelGenerator.Instance.Generated)
		{
			return false;
		}
		EntradaMonstruo entrada = Autoridad.Entrada;
		EnemyBowtie.State currentState = __instance.currentState;
		if (currentState == EnemyBowtie.State.PlayerNotice || currentState == EnemyBowtie.State.Yell)
		{
			Quaternion b = Quaternion.LookRotation(DireccionCamara());
			__instance.horizontalRotationTarget = Quaternion.Slerp(__instance.horizontalRotationTarget, b, 3f * Time.deltaTime);
		}
		__instance.HorizontalRotationLogic();
		__instance.VerticalRotationLogic();
		if (enemy.IsStunned())
		{
			__instance.UpdateState(EnemyBowtie.State.Stun);
		}
		else if (enemy.CurrentState == EnemyState.Despawn)
		{
			__instance.UpdateState(EnemyBowtie.State.Despawn);
		}
		switch (__instance.currentState)
		{
		case EnemyBowtie.State.PlayerNotice:
			_enAtaque = true;
			__instance.StatePlayerNotice();
			break;
		case EnemyBowtie.State.Yell:
			if (!entrada.Atacar && !__instance.stateImpulse && __instance.stateTimer < 4f)
			{
				__instance.stateTimer = 0f;
			}
			__instance.StateYell();
			break;
		case EnemyBowtie.State.YellEnd:
			__instance.StateYellEnd();
			break;
		case EnemyBowtie.State.Stun:
			__instance.StateStun();
			break;
		default:
			if (_enAtaque)
			{
				_enAtaque = false;
				Furia.FinDeSecuencia();
			}
			Controlar(__instance, enemy, entrada);
			break;
		case EnemyBowtie.State.Despawn:
			break;
		}
		return false;
	}

	private static void Controlar(EnemyBowtie bowtie, Enemy enemy, EntradaMonstruo entrada)
	{
		if (entrada.Atacar && Furia.PuedeAtacar)
		{
			bowtie.UpdateState(EnemyBowtie.State.PlayerNotice);
			return;
		}
		EnemyNavMeshAgent navMeshAgent = enemy.NavMeshAgent;
		if (entrada.Direccion == Vector3.zero)
		{
			navMeshAgent.ResetPath();
			bowtie.UpdateState(EnemyBowtie.State.Idle);
			return;
		}
		navMeshAgent.SetDestination(enemy.transform.position + entrada.Direccion * 3f);
		if (entrada.Correr)
		{
			navMeshAgent.OverrideAgent(navMeshAgent.DefaultSpeed * 1.6f, navMeshAgent.DefaultAcceleration, 0.2f);
		}
		bowtie.UpdateState(EnemyBowtie.State.Roam);
	}

	// La mirada llega con los controles del piloto (ver EntradaMonstruo.Mirada), no de la cámara local.
	private static Vector3 DireccionCamara()
	{
		return Autoridad.Entrada.MiradaPlana;
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnVisionTriggered")]
	private static bool OnVisionPrefix(EnemyBowtie __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnInvestigate")]
	private static bool OnInvestigatePrefix(EnemyBowtie __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnGrabbed")]
	private static bool OnGrabbedPrefix(EnemyBowtie __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}
}
