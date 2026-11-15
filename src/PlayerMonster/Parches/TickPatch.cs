using HarmonyLib;
using UnityEngine;

namespace JugadorMonstruo.Parches;

[HarmonyPatch(typeof(EnemyTick))]
internal static class TickPatch
{
	private const int MaximoPorMordisco = 50;

	private const float AlcanceSalto = 2.5f;

	private const float MultiplicadorCorrer = 1.5f;

	private static bool _enAtaque;

	private static int _chupado;

	[HarmonyPrefix]
	[HarmonyPatch("Update")]
	private static bool UpdatePrefix(EnemyTick __instance)
	{
		Enemy enemy = __instance.enemy;
		if (!Autoridad.Controla(enemy))
		{
			return true;
		}
		if (!LevelGenerator.Instance.Generated)
		{
			return false;
		}
		if (enemy.IsStunned())
		{
			__instance.UpdateState(EnemyTick.State.Stun);
		}
		else if (enemy.CurrentState == EnemyState.Despawn)
		{
			__instance.UpdateState(EnemyTick.State.Despawn);
		}
		if ((!__instance.playerTarget || __instance.playerTarget.isDisabled) && (__instance.currentState == EnemyTick.State.Bite || __instance.currentState == EnemyTick.State.Tumble))
		{
			__instance.UpdateState(EnemyTick.State.MoveBackToNavmesh);
		}
		__instance.transform.rotation = SemiFunc.SpringQuaternionGet(__instance.rotationSpring, __instance.rotationTarget);
		__instance.BackToNavmeshPosition();
		switch (__instance.currentState)
		{
		case EnemyTick.State.Tumble:
			__instance.StateTumble();
			if (__instance.currentState == EnemyTick.State.Tumble && (bool)__instance.playerTarget && Vector3.Distance(enemy.Rigidbody.transform.position, __instance.playerTarget.transform.position) < 2f)
			{
				__instance.UpdateState(EnemyTick.State.Bite);
			}
			break;
		case EnemyTick.State.Bite:
			__instance.StateBite();
			break;
		case EnemyTick.State.TumbleEnd:
			__instance.StateTumbleEnd();
			break;
		case EnemyTick.State.MoveBackToNavmesh:
			__instance.StateMoveBackToNavMesh();
			break;
		case EnemyTick.State.Stun:
			__instance.StateStun();
			break;
		case EnemyTick.State.Despawn:
			__instance.StateDespawn();
			break;
		default:
			if (_enAtaque)
			{
				_enAtaque = false;
				Furia.FinDeSecuencia();
				Digerir(__instance);
			}
			__instance.RotationLogic();
			Controlar(__instance, enemy, Autoridad.Entrada);
			break;
		}
		return false;
	}

	private static void Controlar(EnemyTick tick, Enemy enemy, EntradaMonstruo entrada)
	{
		if (entrada.Atacar && Furia.PuedeAtacar)
		{
			foreach (PhysGrabber item in enemy.Rigidbody.physGrabObject.playerGrabbing)
			{
				// El robot del piloto está "desactivado", así que basta con ese filtro. Antes se descartaba
				// además al jugador local, que en este código es el anfitrión: nunca se le podía morder.
				if ((bool)item.playerAvatar && !item.playerAvatar.isDisabled)
				{
					EmpezarMordisco(tick, item.playerAvatar, EnemyTick.State.Bite);
					return;
				}
			}
			PlayerAvatar playerAvatar = VictimaCercana(enemy);
			if (playerAvatar != null)
			{
				EmpezarMordisco(tick, playerAvatar, EnemyTick.State.Tumble);
				return;
			}
		}
		EnemyNavMeshAgent navMeshAgent = enemy.NavMeshAgent;
		if (entrada.Direccion == Vector3.zero)
		{
			navMeshAgent.ResetPath();
			tick.UpdateState(EnemyTick.State.Idle);
			return;
		}
		navMeshAgent.SetDestination(enemy.transform.position + entrada.Direccion * 3f);
		if (entrada.Correr)
		{
			navMeshAgent.OverrideAgent(navMeshAgent.DefaultSpeed * 1.5f, navMeshAgent.DefaultAcceleration, 0.2f);
		}
		tick.UpdateState(EnemyTick.State.Roam);
	}

	private static void EmpezarMordisco(EnemyTick tick, PlayerAvatar victima, EnemyTick.State estado)
	{
		_enAtaque = true;
		_chupado = 0;
		tick.playerTarget = victima;
		tick.NetworkTargetQueue(victima);
		tick.lastSeenGrabbedObject = victima.transform;
		tick.UpdateState(estado);
	}

	private static PlayerAvatar? VictimaCercana(Enemy enemy)
	{
		PlayerAvatar result = null;
		float num = 2.5f;
		Vector3 position = enemy.Rigidbody.transform.position;
		foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
		{
			if ((bool)player && !player.isDisabled)
			{
				float num2 = Vector3.Distance(position, player.transform.position);
				if (num2 < num)
				{
					num = num2;
					result = player;
				}
			}
		}
		return result;
	}

	private static void Digerir(EnemyTick tick)
	{
		EnemyHealth health = tick.enemy.Health;
		if (health.healthCurrent > health.health)
		{
			health.healthCurrent = health.health;
			tick.UpdateSyncedHealth(health.healthCurrent);
			tick.UpdateMassScale();
		}
		tick.stolenHealth = 0;
	}

	[HarmonyPrefix]
	[HarmonyPatch("Suck")]
	private static bool SuckPrefix(EnemyTick __instance, out int __state)
	{
		__state = __instance.stolenHealth;
		if (!Autoridad.EstaPoseido(__instance.enemy))
		{
			return true;
		}
		if (_chupado >= 50)
		{
			__instance.UpdateState(EnemyTick.State.TumbleEnd);
			return false;
		}
		return true;
	}

	[HarmonyPostfix]
	[HarmonyPatch("Suck")]
	private static void SuckPostfix(EnemyTick __instance, int __state)
	{
		if (Autoridad.EstaPoseido(__instance.enemy))
		{
			_chupado += Mathf.Max(0, __instance.stolenHealth - __state);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnVision")]
	private static bool OnVisionPrefix(EnemyTick __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnInvestigate")]
	private static bool OnInvestigatePrefix(EnemyTick __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}
}
