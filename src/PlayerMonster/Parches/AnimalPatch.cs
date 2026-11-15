using HarmonyLib;
using UnityEngine;

namespace JugadorMonstruo.Parches;

[HarmonyPatch(typeof(EnemyAnimal))]
internal static class AnimalPatch
{
	private const float VelocidadAndar = 2.5f;

	private const float VelocidadCorrer = 5f;

	private const float Aceleracion = 12f;

	[HarmonyPrefix]
	[HarmonyPatch("Update")]
	private static bool UpdatePrefix(EnemyAnimal __instance, Enemy ___enemy)
	{
		if (!Autoridad.Controla(___enemy))
		{
			return true;
		}
		EntradaMonstruo entrada = Autoridad.Entrada;
		EnemyNavMeshAgent navMeshAgent = ___enemy.NavMeshAgent;
		if (___enemy.IsStunned())
		{
			return false;
		}
		if (entrada.Direccion == Vector3.zero)
		{
			navMeshAgent.ResetPath();
			__instance.UpdateState((!entrada.Atacar) ? EnemyAnimal.State.Idle : EnemyAnimal.State.WreakHavoc);
			return false;
		}
		bool flag = entrada.Correr || entrada.Atacar;
		navMeshAgent.SetDestination(___enemy.transform.position + entrada.Direccion * 3f);
		navMeshAgent.OverrideAgent(flag ? 5f : 2.5f, 12f, 0.25f);
		__instance.UpdateState(entrada.Atacar ? EnemyAnimal.State.WreakHavoc : EnemyAnimal.State.Roam);
		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnVision")]
	private static bool OnVisionPrefix(Enemy ___enemy)
	{
		return !Autoridad.EstaPoseido(___enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnInvestigate")]
	private static bool OnInvestigatePrefix(Enemy ___enemy)
	{
		return !Autoridad.EstaPoseido(___enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnGrabbed")]
	private static bool OnGrabbedPrefix(Enemy ___enemy)
	{
		return !Autoridad.EstaPoseido(___enemy);
	}
}
