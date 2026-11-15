using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace JugadorMonstruo;

internal static class Rotacion
{
	private const string ClaveVersion = "jm_version";

	private const string ClavePiloto = "jm_piloto";

	private const string ClaveRondas = "jm_rondas";

	private static int _piloto = -1;

	private static int _rondas;

	private static float _siguienteAnuncio;

	public static void Tick()
	{
		if (!(Time.unscaledTime < _siguienteAnuncio) && PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
		{
			_siguienteAnuncio = Time.unscaledTime + 2f;
			if (!TieneNuestraVersion(PhotonNetwork.LocalPlayer))
			{
				PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable { { "jm_version", Plugin.Version } });
			}
		}
	}

	private static bool TieneNuestraVersion(Player jugador)
	{
		if (jugador.CustomProperties.TryGetValue("jm_version", out var value) && value is string text)
		{
			return text == Plugin.Version;
		}
		return false;
	}

	private static List<Player> Candidatos()
	{
		return (from j in PhotonNetwork.PlayerList
			where j.IsLocal || TieneNuestraVersion(j)
			orderby j.ActorNumber
			select j).ToList();
	}

	public static int PilotoParaEsteNivel()
	{
		if (!SemiFunc.IsMultiplayer() || PhotonNetwork.LocalPlayer == null)
		{
			return 0;
		}
		List<Player> list = Candidatos();
		int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
		switch (Plugin.PilotoForzado.Value)
		{
		case ForzarPiloto.Anfitrion:
			return actorNumber;
		case ForzarPiloto.PrimerCliente:
			return list.FirstOrDefault((Player j) => !j.IsLocal)?.ActorNumber ?? actorNumber;
		default:
		{
			int value = Plugin.RondasPorMonstruo.Value;
			bool flag = list.Any((Player j) => j.ActorNumber == _piloto);
			bool flag2 = value > 0 && _rondas >= value;
			if ((_piloto < 0 || !flag) | flag2)
			{
				List<Player> list2 = list.Where((Player j) => j.ActorNumber != _piloto).ToList();
				if (list2.Count == 0)
				{
					list2 = list;
				}
				Player player = list2[Random.Range(0, list2.Count)];
				_piloto = player.ActorNumber;
				_rondas = 0;
				Plugin.Log.LogInfo($"Rotación: sorteo entre {list2.Count} de {PhotonNetwork.PlayerList.Length} jugadores -> '{player.NickName}' (actor {_piloto}).");
			}
			_rondas++;
			Plugin.Log.LogInfo(string.Format("Rotación: ronda {0} de {1} del actor {2}.", _rondas, (value > 0) ? value.ToString() : "infinitas", _piloto));
			Publicar();
			return _piloto;
		}
		}
	}

	public static void Reiniciar(string motivo)
	{
		if (_piloto >= 0 || _rondas != 0)
		{
			_piloto = -1;
			_rondas = 0;
			Plugin.Log.LogInfo("Rotación: reiniciada (" + motivo + ").");
			Publicar();
		}
	}

	private static void Publicar()
	{
		if (SemiFunc.IsMultiplayer() && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
		{
			PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
			{
				{ "jm_piloto", _piloto },
				{ "jm_rondas", _rondas }
			});
		}
	}
}
