using HarmonyLib;
using UnityEngine;

namespace JugadorMonstruo.Parches;

[HarmonyPatch(typeof(EnemyHunter))]
internal static class HunterPatch
{
	private const float MultiplicadorCorrer = 1.4f;

	private const float ApuntadoMinimo = 0.5f;

	private static bool _enAtaque;

	private static float _apuntandoDesde;

	[HarmonyPrefix]
	[HarmonyPatch("Update")]
	private static bool UpdatePrefix(EnemyHunter __instance)
	{
		Enemy enemy = __instance.enemy;
		if (!Autoridad.Controla(enemy))
		{
			return true;
		}
		__instance.VerticalRotationLogic();
		__instance.HurtColliderTimer();
		__instance.LineRendererLogic();
		if (enemy.IsStunned())
		{
			__instance.UpdateState(EnemyHunter.State.Stun);
		}
		if (enemy.CurrentState == EnemyState.Despawn && !enemy.IsStunned())
		{
			__instance.UpdateState(EnemyHunter.State.Despawn);
		}
		__instance.HorizontalRotationLogic();
		EntradaMonstruo entrada = Autoridad.Entrada;
		switch (__instance.currentState)
		{
		case EnemyHunter.State.Aim:
			Apuntar(__instance, entrada);
			break;
		case EnemyHunter.State.Shoot:
			Disparar(__instance);
			break;
		case EnemyHunter.State.ShootEnd:
			__instance.StateShootEnd();
			break;
		case EnemyHunter.State.Stun:
			__instance.StateStun();
			break;
		case EnemyHunter.State.Despawn:
			__instance.StateDespawn();
			break;
		default:
			if (_enAtaque)
			{
				_enAtaque = false;
				Furia.FinDeSecuencia();
			}
			__instance.shotsFired = 0;
			Controlar(__instance, enemy, entrada);
			break;
		}
		return false;
	}

	private static void Controlar(EnemyHunter hunter, Enemy enemy, EntradaMonstruo entrada)
	{
		EnemyNavMeshAgent navMeshAgent = enemy.NavMeshAgent;
		if (entrada.Atacar && Furia.PuedeAtacar)
		{
			navMeshAgent.ResetPath();
			_enAtaque = true;
			_apuntandoDesde = Time.time;
			hunter.investigatePointHasTransform = false;
			hunter.investigatePoint = PuntoDeMira(hunter);
			hunter.UpdateState(EnemyHunter.State.Aim);
		}
		else if (entrada.Direccion == Vector3.zero)
		{
			navMeshAgent.ResetPath();
			hunter.UpdateState(EnemyHunter.State.Idle);
		}
		else
		{
			navMeshAgent.SetDestination(enemy.transform.position + entrada.Direccion * 3f);
			if (entrada.Correr)
			{
				navMeshAgent.OverrideAgent(navMeshAgent.DefaultSpeed * 1.4f, navMeshAgent.DefaultAcceleration, 0.2f);
			}
			hunter.UpdateState(EnemyHunter.State.Roam);
		}
	}

	private static void Apuntar(EnemyHunter hunter, EntradaMonstruo entrada)
	{
		hunter.enemy.NavMeshAgent.ResetPath();
		hunter.investigatePointHasTransform = false;
		hunter.investigatePoint = PuntoDeMira(hunter);
		hunter.AimLogic();
		bool flag = Time.time - _apuntandoDesde >= 0.5f;
		if (!entrada.Atacar & flag)
		{
			hunter.UpdateState(EnemyHunter.State.Shoot);
		}
	}

	/// <summary>
	/// El disparo original sale en la dirección en la que mira el arma en ese instante. El arma gira
	/// con muelles alrededor de un pivote que no está en el cañón, así que nunca queda apuntando
	/// exactamente al punto de la mira (en las pruebas el tiro caía por debajo). Para el frame en
	/// que se calcula el disparo, orientamos el cañón al punto exacto y luego lo dejamos como estaba:
	/// todo lo demás (impacto, daño, efectos, sonido) sigue siendo el código del juego.
	/// </summary>
	private static void Disparar(EnemyHunter hunter)
	{
		if (!hunter.stateImpulse)
		{
			hunter.StateShoot();
			return;
		}
		Transform arma = hunter.gunAimTransform;
		Quaternion antes = arma.rotation;
		Vector3 hacia = PuntoDeMira(hunter) - arma.position;
		if (hacia.sqrMagnitude > 0.01f)
		{
			arma.rotation = Quaternion.LookRotation(hacia);
		}
		hunter.StateShoot();
		arma.rotation = antes;
	}

	// A dónde apunta el arma: al punto exacto que el piloto tiene bajo la mira (ver Mira.cs).
	// Si no hay punto válido, o está pegado al arma, se usa la dirección de su mirada.
	private static Vector3 PuntoDeMira(EnemyHunter hunter)
	{
		Vector3 arma = hunter.gunAimTransform.position;
		Vector3 punto = Autoridad.Entrada.PuntoMira;
		if (punto != Vector3.zero && Vector3.Distance(punto, arma) > 1.5f)
		{
			return punto;
		}
		Vector3 mirada = Autoridad.Entrada.Mirada;
		if (mirada.sqrMagnitude < 0.01f)
		{
			mirada = hunter.transform.forward;
		}
		return arma + mirada.normalized * 20f;
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnInvestigate")]
	private static bool OnInvestigatePrefix(EnemyHunter __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnTouchPlayer")]
	private static bool OnTouchPlayerPrefix(EnemyHunter __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnTouchPlayerGrabbedObject")]
	private static bool OnTouchObjetoPrefix(EnemyHunter __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnGrabbed")]
	private static bool OnGrabbedPrefix(EnemyHunter __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}
}
