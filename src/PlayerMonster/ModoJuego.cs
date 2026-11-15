using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace JugadorMonstruo;

internal static class ModoJuego
{
	private static int _nivelProcesado;

	private static int _nivelDecidido;

	private static bool _hayMonstruo;

	public static bool HayMonstruoEnEsteNivel()
	{
		LevelGenerator instance = LevelGenerator.Instance;
		if (!Plugin.ModoAutomatico.Value || !instance || !Mensajes.SoyAutoridad)
		{
			return false;
		}
		int instanceID = instance.GetInstanceID();
		if (instanceID != _nivelDecidido)
		{
			_nivelDecidido = instanceID;
			_hayMonstruo = Random.Range(0f, 100f) < Plugin.Probabilidad.Value;
			Plugin.Log.LogInfo(_hayMonstruo ? "Modo: en este nivel hay jugador-monstruo." : "Modo: en este nivel no hay jugador-monstruo (probabilidad).");
		}
		return _hayMonstruo;
	}

	public static int PilotoDeLaTecla()
	{
		if (!SemiFunc.IsMultiplayer() || PhotonNetwork.LocalPlayer == null)
		{
			return 0;
		}
		if (Plugin.PilotoForzado.Value == ForzarPiloto.PrimerCliente && PhotonNetwork.PlayerListOthers.Length != 0)
		{
			int num = int.MaxValue;
			Player[] playerListOthers = PhotonNetwork.PlayerListOthers;
			foreach (Player player in playerListOthers)
			{
				num = Mathf.Min(num, player.ActorNumber);
			}
			return num;
		}
		return PhotonNetwork.LocalPlayer.ActorNumber;
	}

	public static void Tick()
	{
		if (!Plugin.ModoAutomatico.Value || !Mensajes.SoyAutoridad)
		{
			return;
		}
		LevelGenerator instance = LevelGenerator.Instance;
		if (!instance || !instance.Generated || !SemiFunc.RunIsLevel() || !PlayerAvatar.instance || !GameDirector.instance || GameDirector.instance.currentState != GameDirector.gameState.Main)
		{
			return;
		}
		int instanceID = instance.GetInstanceID();
		if (instanceID != _nivelProcesado)
		{
			_nivelProcesado = instanceID;
			if (HayMonstruoEnEsteNivel())
			{
				Autoridad.EmpezarTransformacion(Rotacion.PilotoParaEsteNivel(), Mathf.Max(1f, Plugin.RetrasoTransformacion.Value));
			}
		}
	}
}
