using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace JugadorMonstruo.Parches;

/// <summary>
/// Clown (EnemyBeamer): dos ataques originales con un solo botón.
///  - Con un jugador a menos de 2,5 m: la patada (MeleeStart -> Melee), hacia donde mira el piloto.
///  - Si no: el rayo láser (AttackStart -> Attack -> AttackEnd), apuntado al punto de la mira. El
///    barrido de lado a lado es el del juego; el piloto solo elige el centro y la altura.
/// </summary>
[HarmonyPatch(typeof(EnemyBeamer))]
internal static class BeamerPatch
{
	private const float DistanciaPatada = 2.5f;

	private const float InclinacionMaxima = 30f;

	private static bool _enAtaque;

	[HarmonyPrefix]
	[HarmonyPatch("Update")]
	private static bool UpdatePrefix(EnemyBeamer __instance)
	{
		Enemy enemy = __instance.enemy;
		if (!Autoridad.Controla(enemy))
		{
			return true;
		}
		EntradaMonstruo entrada = Autoridad.Entrada;
		if (__instance.currentState == EnemyBeamer.State.AttackStart)
		{
			ApuntarVertical(__instance, entrada);
		}
		__instance.VerticalAimLogic();
		__instance.LaserLogic();
		__instance.MoveFastLogic();
		if (!LevelGenerator.Instance.Generated)
		{
			return false;
		}
		if (enemy.IsStunned())
		{
			__instance.UpdateState(EnemyBeamer.State.Stun);
		}
		if (enemy.CurrentState == EnemyState.Despawn)
		{
			__instance.UpdateState(EnemyBeamer.State.Despawn);
		}
		switch (__instance.currentState)
		{
		case EnemyBeamer.State.AttackStart:
			_enAtaque = true;
			EmpezarRayo(__instance);
			ApuntarHorizontal(__instance, enemy, entrada);
			break;
		case EnemyBeamer.State.Attack:
			__instance.StateAttack();
			break;
		case EnemyBeamer.State.AttackEnd:
			__instance.StateAttackEnd();
			break;
		case EnemyBeamer.State.MeleeStart:
			_enAtaque = true;
			__instance.StateMeleeStart();
			break;
		case EnemyBeamer.State.Melee:
			__instance.StateMelee();
			break;
		case EnemyBeamer.State.Stun:
			__instance.StateStun();
			break;
		case EnemyBeamer.State.Despawn:
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
		// Tras el rayo o la patada la IA pasaría a Seek sin que el piloto se mueva: lo cortamos.
		if (_enAtaque && __instance.currentState == EnemyBeamer.State.Seek)
		{
			__instance.UpdateState(EnemyBeamer.State.Idle);
		}
		__instance.RotationLogic();
		return false;
	}

	/// <summary>
	/// El primer fotograma de AttackStart lee la posición del objetivo de la IA, y pilotado no hay
	/// objetivo. Se le presta un jugador cualquiera solo durante esa llamada (el valor que lee,
	/// seekDestination, no se usa mientras lo pilotan) y se retira enseguida.
	/// </summary>
	private static void EmpezarRayo(EnemyBeamer beamer)
	{
		PlayerAvatar anterior = beamer.playerTarget;
		if (beamer.stateImpulse && !anterior)
		{
			beamer.playerTarget = PlayerAvatar.instance;
		}
		beamer.StateAttackStart();
		beamer.playerTarget = null;
	}

	private static void ApuntarHorizontal(EnemyBeamer beamer, Enemy enemy, EntradaMonstruo entrada)
	{
		Vector3 hacia = Vector3.ProjectOnPlane(entrada.PuntoMira - enemy.Rigidbody.transform.position, Vector3.up);
		if (hacia.sqrMagnitude < 1f)
		{
			hacia = entrada.MiradaPlana; // la mira cae encima del propio payaso
		}
		beamer.aimHorizontalTarget = Quaternion.Euler(0f, Quaternion.LookRotation(hacia).eulerAngles.y, 0f);
	}

	/// <summary>
	/// Altura del rayo: hacia el punto de la mira, limitada para que no acabe en el suelo o el techo.
	/// VerticalAimLogic (original) se queda solo con la inclinación de esta rotación.
	/// </summary>
	private static void ApuntarVertical(EnemyBeamer beamer, EntradaMonstruo entrada)
	{
		Vector3 hacia = entrada.PuntoMira - beamer.laserRayTransform.position;
		float plano = new Vector2(hacia.x, hacia.z).magnitude;
		if (plano < 1f)
		{
			beamer.aimVerticalTarget = Quaternion.identity;
			return;
		}
		float inclinacion = Mathf.Clamp(-Mathf.Atan2(hacia.y, plano) * Mathf.Rad2Deg, -InclinacionMaxima, InclinacionMaxima);
		beamer.aimVerticalTarget = Quaternion.Euler(inclinacion, beamer.laserRayTransform.eulerAngles.y, 0f);
	}

	private static void Controlar(EnemyBeamer beamer, Enemy enemy, EntradaMonstruo entrada)
	{
		if (entrada.Atacar && Furia.PuedeAtacar && !enemy.Jump.jumping)
		{
			if (HayJugadorCerca(enemy))
			{
				beamer.meleeTarget = enemy.Rigidbody.transform.position + entrada.MiradaPlana * 1.5f;
				beamer.meleePlayer = false;
				beamer.UpdateState(EnemyBeamer.State.MeleeStart);
			}
			else
			{
				beamer.UpdateState(EnemyBeamer.State.AttackStart);
			}
			return;
		}
		EnemyNavMeshAgent navMeshAgent = enemy.NavMeshAgent;
		if (entrada.Direccion == Vector3.zero)
		{
			navMeshAgent.ResetPath();
			beamer.UpdateState(EnemyBeamer.State.Idle);
			return;
		}
		navMeshAgent.SetDestination(enemy.transform.position + entrada.Direccion * 3f);
		if (entrada.Correr)
		{
			// Seek + moveFast es como el juego anima al payaso corriendo.
			navMeshAgent.OverrideAgent(navMeshAgent.DefaultSpeed * 2f, navMeshAgent.DefaultAcceleration * 2f, 0.1f);
			beamer.UpdateState(EnemyBeamer.State.Seek);
			if (!beamer.moveFast)
			{
				beamer.moveFast = true;
				if (SemiFunc.IsMultiplayer())
				{
					beamer.photonView.RPC("MoveFastRPC", RpcTarget.Others, true);
				}
			}
		}
		else
		{
			beamer.UpdateState(EnemyBeamer.State.Roam);
		}
	}

	private static bool HayJugadorCerca(Enemy enemy)
	{
		Vector3 posicion = enemy.Rigidbody.transform.position;
		foreach (PlayerAvatar jugador in GameDirector.instance.PlayerList)
		{
			if ((bool)jugador && !jugador.isDisabled
				&& Vector3.Distance(posicion, jugador.transform.position) < DistanciaPatada
				&& Mathf.Abs(posicion.y - jugador.transform.position.y) < 1f)
			{
				return true;
			}
		}
		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnVision")]
	private static bool OnVisionPrefix(EnemyBeamer __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}

	[HarmonyPrefix]
	[HarmonyPatch("OnInvestigate")]
	private static bool OnInvestigatePrefix(EnemyBeamer __instance)
	{
		return !Autoridad.EstaPoseido(__instance.enemy);
	}
}
