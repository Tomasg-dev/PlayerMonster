using HarmonyLib;
using UnityEngine;

namespace JugadorMonstruo.Parches;

[HarmonyPatch(typeof(EnemyTumbler))]
internal static class TumblerPatch
{
	private static bool _enAtaque;

	[HarmonyPrefix]
	[HarmonyPatch("Update")]
	private static bool UpdatePrefix(EnemyTumbler __instance)
	{
		Enemy enemy = __instance.enemy;
		if (!Autoridad.Controla(enemy))
		{
			return true;
		}
		EntradaMonstruo entrada = Autoridad.Entrada;
		__instance.visionTimer = 1f;
		__instance.visionPrevious = true;
		if (__instance.currentState == EnemyTumbler.State.Tell)
		{
			__instance.mainMeshTargetRotation = Quaternion.LookRotation(DireccionCamara());
		}
		__instance.HopLogic();
		__instance.RotationLogic();
		if (enemy.IsStunned())
		{
			__instance.UpdateState(EnemyTumbler.State.Stunned);
		}
		else if (enemy.CurrentState == EnemyState.Despawn)
		{
			__instance.UpdateState(EnemyTumbler.State.Despawn);
		}
		switch (__instance.currentState)
		{
		case EnemyTumbler.State.Tell:
			_enAtaque = true;
			__instance.StateTell();
			break;
		case EnemyTumbler.State.Tumble:
			if (__instance.stateImpulse)
			{
				Lanzar(__instance);
			}
			__instance.StateTumble();
			break;
		case EnemyTumbler.State.TumbleEnd:
			__instance.StateTumbleEnd();
			break;
		case EnemyTumbler.State.BackToNavmesh:
			__instance.StateBackToNavmesh();
			break;
		case EnemyTumbler.State.Stunned:
			__instance.StateStunned();
			break;
		case EnemyTumbler.State.Despawn:
			__instance.StateDespawn();
			break;
		default:
			if (_enAtaque)
			{
				_enAtaque = false;
				Furia.FinDeSecuencia();
				Mensajes.EnviarMareo(3.5f);
			}
			Controlar(__instance, enemy, entrada);
			break;
		case EnemyTumbler.State.Dead:
			break;
		}
		return false;
	}

	private static void Controlar(EnemyTumbler tumbler, Enemy enemy, EntradaMonstruo entrada)
	{
		bool flag = enemy.Grounded.grounded && !enemy.Jump.jumping;
		if ((entrada.Atacar & flag) && Furia.PuedeAtacar)
		{
			tumbler.UpdateState(EnemyTumbler.State.Tell);
			return;
		}
		if (entrada.Direccion == Vector3.zero)
		{
			enemy.NavMeshAgent.ResetPath();
			tumbler.UpdateState(EnemyTumbler.State.Idle);
			return;
		}
		if (enemy.Grounded.grounded)
		{
			enemy.NavMeshAgent.SetDestination(enemy.transform.position + entrada.Direccion * 4f);
		}
		tumbler.UpdateState(entrada.Correr ? EnemyTumbler.State.MoveToPlayer : EnemyTumbler.State.Roam);
	}

	private static void Lanzar(EnemyTumbler tumbler)
	{
		Enemy enemy = tumbler.enemy;
		tumbler.stateImpulse = false;
		tumbler.stateTimer = 1f;
		enemy.NavMeshAgent.Warp(enemy.Rigidbody.transform.position);
		enemy.NavMeshAgent.ResetPath();
		Vector3 normalized = Vector3.Lerp(DireccionCamara() * 5f, Vector3.up, 0.6f).normalized;
		enemy.Rigidbody.rb.AddForce(normalized * 40f, ForceMode.Impulse);
		enemy.Rigidbody.rb.AddTorque(enemy.Rigidbody.transform.right * 8f, ForceMode.Impulse);
	}

	// La mirada llega con los controles del piloto (ver EntradaMonstruo.Mirada), no de la cámara local.
	private static Vector3 DireccionCamara()
	{
		return Autoridad.Entrada.MiradaPlana;
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnVision")]
	private static bool OnVisionPrefix(EnemyTumbler __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnInvestigate")]
	private static bool OnInvestigatePrefix(EnemyTumbler __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnGrabbed")]
	private static bool OnGrabbedPrefix(EnemyTumbler __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}
}
