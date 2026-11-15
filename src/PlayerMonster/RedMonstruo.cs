using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace JugadorMonstruo;

internal static class RedMonstruo
{
	private const string ClaveMonstruo = "jm_monstruo";

	private const string ClaveEnemigo = "jm_enemigo";

	private static readonly HashSet<PlayerAvatar> Ocultados = new HashSet<PlayerAvatar>();

	private static float _siguienteRevision;

	public static void Anunciar(bool soyMonstruo)
	{
		if (SemiFunc.IsMultiplayer() && PhotonNetwork.LocalPlayer != null)
		{
			PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "jm_monstruo", soyMonstruo } });
		}
	}

	private static int _enemigoAnunciado = -1;

	/// <summary>
	/// Publica qué enemigo pilota el jugador local. Se puede llamar cada frame: solo envía algo
	/// cuando cambia. (Antes se llamaba una única vez al cambiar de enemigo, y si en ese instante
	/// el enemigo aún no existía en este cliente se publicaba "ninguno" y ya no se corregía.)
	/// </summary>
	public static void AnunciarEnemigo(Enemy? enemigo)
	{
		if (SemiFunc.IsMultiplayer() && PhotonNetwork.LocalPlayer != null)
		{
			int num = ((enemigo != null && (bool)enemigo && (bool)enemigo.PhotonView) ? enemigo.PhotonView.ViewID : 0);
			if (num != _enemigoAnunciado)
			{
				_enemigoAnunciado = num;
				PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "jm_enemigo", num } });
			}
		}
	}

	/// <summary>
	/// El enemigo que pilota ahora mismo el jugador-monstruo, visto desde cualquier cliente
	/// (null si no hay monstruo, o si está muerto esperando reaparecer).
	/// </summary>
	public static Enemy? MonstruoEnElNivel()
	{
		Enemy? enemigo = null;
		if (!SemiFunc.IsMultiplayer())
		{
			enemigo = Autoridad.Actual;
		}
		else
		{
			foreach (Player jugador in PhotonNetwork.PlayerList)
			{
				if (EsMonstruo(jugador))
				{
					enemigo = EnemigoDe(jugador);
					if (enemigo != null)
					{
						break;
					}
				}
			}
		}
		if (enemigo == null || !enemigo || !enemigo.EnemyParent || !enemigo.EnemyParent.Spawned)
		{
			return null;
		}
		return enemigo;
	}

	public static bool EsMonstruo(Player jugador)
	{
		if (jugador != null && jugador.CustomProperties.TryGetValue("jm_monstruo", out var value))
		{
			if (value is bool)
			{
				return (bool)value;
			}
			return false;
		}
		return false;
	}

	private static Enemy? EnemigoDe(Player jugador)
	{
		if (jugador == null || !jugador.CustomProperties.TryGetValue("jm_enemigo", out var value) || !(value is int num) || num == 0)
		{
			return null;
		}
		PhotonView photonView = PhotonView.Find(num);
		if (!photonView)
		{
			return null;
		}
		return photonView.GetComponent<Enemy>();
	}

	public static void Tick()
	{
		if (!SemiFunc.IsMultiplayer() || !GameDirector.instance)
		{
			Ocultados.RemoveWhere((PlayerAvatar avatar) => !avatar);
			return;
		}
		Voces();
		if (Time.time < _siguienteRevision)
		{
			return;
		}
		_siguienteRevision = Time.time + 0.5f;
		Ocultados.RemoveWhere((PlayerAvatar avatar) => !avatar);
		foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
		{
			if ((bool)player && !player.isLocal && (bool)player.photonView)
			{
				bool flag = EsMonstruo(player.photonView.Owner);
				if (flag && !Ocultados.Contains(player))
				{
					player.playerAvatarVisuals.gameObject.SetActive(value: false);
					player.gameObject.SetActive(value: false);
					Ocultados.Add(player);
				}
				else if (!flag && Ocultados.Contains(player))
				{
					player.gameObject.SetActive(value: true);
					player.playerAvatarVisuals.gameObject.SetActive(value: true);
					Ocultados.Remove(player);
				}
			}
		}
	}

	private static void Voces()
	{
		if (!Plugin.VozCorrupta.Value)
		{
			return;
		}
		foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
		{
			if ((bool)player && (bool)player.photonView && (bool)player.voiceChat && EsMonstruo(player.photonView.Owner))
			{
				Enemy enemy = EnemigoDe(player.photonView.Owner);
				if (!(enemy == null) && (bool)enemy.EnemyParent && enemy.EnemyParent.Spawned)
				{
					Vector3 position = (enemy.CenterTransform ? enemy.CenterTransform.position : (enemy.transform.position + Vector3.up));
					player.voiceChat.OverridePosition(position, 0.2f);
					player.voiceChat.OverrideVoiceDistortion(0.2f);
					player.voiceChat.OverridePitch(Plugin.VozTono.Value, 0.5f, 0.5f, 0.2f, 0.1f, 8f);
				}
			}
		}
	}
}
