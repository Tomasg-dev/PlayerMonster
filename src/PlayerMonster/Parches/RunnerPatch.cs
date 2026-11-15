using HarmonyLib;
using UnityEngine;

namespace JugadorMonstruo.Parches;

[HarmonyPatch(typeof(EnemyRunner))]
internal static class RunnerPatch
{
	private const float VelocidadAndar = 1.5f;

	[HarmonyPrefix]
	[HarmonyPatch("Update")]
	private static bool UpdatePrefix(EnemyRunner __instance)
	{
		Enemy enemy = __instance.enemy;
		if (!Autoridad.Controla(enemy))
		{
			return true;
		}
		__instance.HeadSpringUpdate();
		if (enemy.CurrentState == EnemyState.Despawn && !enemy.IsStunned())
		{
			__instance.UpdateState(EnemyRunner.State.Despawn);
			__instance.StateDespawn();
			return false;
		}
		if (enemy.IsStunned())
		{
			__instance.UpdateState(EnemyRunner.State.Stun);
			__instance.StateStun();
			return false;
		}
		EntradaMonstruo entrada = Autoridad.Entrada;
		EnemyNavMeshAgent navMeshAgent = enemy.NavMeshAgent;
		bool flag = entrada.Direccion != Vector3.zero;
		if (flag)
		{
			float speed = ((entrada.Atacar || entrada.Correr) ? __instance.agentSpeed : 1.5f);
			navMeshAgent.SetDestination(enemy.transform.position + entrada.Direccion * 3f);
			navMeshAgent.OverrideAgent(speed, navMeshAgent.DefaultAcceleration, 0.2f);
		}
		else
		{
			navMeshAgent.ResetPath();
		}
		EnemyRunner.State state = (entrada.Atacar ? EnemyRunner.State.AttackPlayer : ((!flag) ? EnemyRunner.State.Idle : EnemyRunner.State.Roam));
		__instance.UpdateState(state);
		Rotar(__instance, entrada);
		__instance.TimerLogic();
		return false;
	}

	private static void Rotar(EnemyRunner runner, EntradaMonstruo entrada)
	{
		Vector3 vector = entrada.Direccion;
		if (vector == Vector3.zero && entrada.Atacar)
		{
			vector = entrada.MiradaPlana; // ataca quieto: gira hacia donde mira el piloto
		}
		if (vector != Vector3.zero)
		{
			runner.rotationTarget = Quaternion.LookRotation(vector);
		}
		runner.rotationSpring.speed = 10f;
		runner.transform.rotation = SemiFunc.SpringQuaternionGet(runner.rotationSpring, runner.rotationTarget);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnVision")]
	private static bool OnVisionPrefix(EnemyRunner __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnInvestigate")]
	private static bool OnInvestigatePrefix(EnemyRunner __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnGrabbed")]
	private static bool OnGrabbedPrefix(EnemyRunner __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}
}
