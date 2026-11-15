using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using REPOLib.Modules;
using UnityEngine;

namespace JugadorMonstruo;

internal static class Mensajes
{
	private const float IntervaloEntrada = 0.04f;

	private const float IntervaloEstado = 0.1f;

	private const float IntervaloRuido = 1f / 15f;

	private static NetworkedEvent? _evEntrada;

	private static NetworkedEvent? _evEleccion;

	private static NetworkedEvent? _evEstado;

	private static NetworkedEvent? _evMareo;

	private static NetworkedEvent? _evRuido;

	private static NetworkedEvent? _evGolpe;

	private static NetworkedEvent? _evVictoria;

	private static float _siguienteGolpe;

	private static EntradaMonstruo _entradaEnviada;

	private static float _siguienteEntrada;

	private static EstadoMonstruo _estadoEnviado;

	private static int _estadoEnviadoA = -1;

	private static float _siguienteEstado;

	private static float _siguienteRuido;

	public static bool SoyAutoridad => SemiFunc.IsMasterClientOrSingleplayer();

	public static void Registrar()
	{
		_evEntrada = new NetworkedEvent("PlayerMonster Entrada", AlRecibirEntrada);
		_evEleccion = new NetworkedEvent("PlayerMonster Eleccion", AlRecibirEleccion);
		_evEstado = new NetworkedEvent("PlayerMonster Estado", AlRecibirEstado);
		_evMareo = new NetworkedEvent("PlayerMonster Mareo", AlRecibirMareo);
		_evRuido = new NetworkedEvent("PlayerMonster Ruido", AlRecibirRuido);
		_evGolpe = new NetworkedEvent("PlayerMonster Golpe", AlRecibirGolpe);
		_evVictoria = new NetworkedEvent("PlayerMonster Victoria", AlRecibirVictoria);
	}

	public static void EnviarEntrada(EntradaMonstruo entrada)
	{
		if (SoyAutoridad)
		{
			if (Autoridad.PilotoEsLocal)
			{
				Autoridad.RecibirEntrada(entrada);
			}
		}
		else if (entrada.Correr != _entradaEnviada.Correr || entrada.Atacar != _entradaEnviada.Atacar || !(Time.unscaledTime < _siguienteEntrada))
		{
			_siguienteEntrada = Time.unscaledTime + 0.04f;
			_entradaEnviada = entrada;
			byte b = (byte)((entrada.Correr ? 1u : 0u) | (uint)(entrada.Atacar ? 2 : 0));
			_evEntrada?.RaiseEvent(new object[5]
			{
				entrada.Direccion.x,
				entrada.Direccion.z,
				b,
				entrada.Mirada,
				entrada.PuntoMira
			}, NetworkingEvents.RaiseMasterClient, SendOptions.SendUnreliable);
		}
	}

	private static void AlRecibirEntrada(EventData evento)
	{
		if (SoyAutoridad && !Autoridad.PilotoEsLocal && evento.Sender == Autoridad.PilotoActor && evento.CustomData is object[] array && array.Length == 5 && array[0] is float num && array[1] is float num2 && array[2] is byte b && array[3] is Vector3 mirada && array[4] is Vector3 puntoMira && !float.IsNaN(num) && !float.IsNaN(num2))
		{
			Vector3 vector = new Vector3(num, 0f, num2);
			vector = ((vector.sqrMagnitude > 0.01f) ? vector.normalized : Vector3.zero);
			Autoridad.RecibirEntrada(new EntradaMonstruo
			{
				Direccion = vector,
				Correr = ((b & 1) != 0),
				Atacar = ((b & 2) != 0),
				// Lo que llega por red no es de fiar: comprobamos que sea una dirección válida.
				Mirada = ((mirada.sqrMagnitude > 0.01f && !float.IsNaN(mirada.x + mirada.y + mirada.z)) ? mirada.normalized : Vector3.forward),
				// Un punto no finito (NaN o infinito) se descarta: el cazador usará solo la dirección.
				PuntoMira = ((float.IsNaN(puntoMira.x + puntoMira.y + puntoMira.z) || float.IsInfinity(puntoMira.sqrMagnitude)) ? Vector3.zero : puntoMira)
			});
		}
	}

	public static void EnviarEleccion(TipoMonstruo? tipo)
	{
		if (SoyAutoridad)
		{
			if (Autoridad.PilotoEsLocal)
			{
				Autoridad.RecibirEleccion(tipo);
			}
		}
		else
		{
			_evEleccion?.RaiseEvent((int)(tipo.HasValue ? tipo.Value : ((TipoMonstruo)(-1))), NetworkingEvents.RaiseMasterClient, SendOptions.SendReliable);
		}
	}

	private static void AlRecibirEleccion(EventData evento)
	{
		if (SoyAutoridad && !Autoridad.PilotoEsLocal && evento.Sender == Autoridad.PilotoActor && evento.CustomData is int num)
		{
			Autoridad.RecibirEleccion(Enum.IsDefined(typeof(TipoMonstruo), num) ? new TipoMonstruo?((TipoMonstruo)num) : ((TipoMonstruo?)null));
		}
	}

	public static void EnviarEstado(EstadoMonstruo estado)
	{
		if (Autoridad.PilotoEsLocal)
		{
			Piloto.RecibirEstado(estado);
			return;
		}
		int pilotoActor = Autoridad.PilotoActor;
		if (pilotoActor != _estadoEnviadoA || estado.Fase != _estadoEnviado.Fase || estado.Serie != _estadoEnviado.Serie || estado.Tipo != _estadoEnviado.Tipo || !(Time.unscaledTime < _siguienteEstado))
		{
			_siguienteEstado = Time.unscaledTime + 0.1f;
			_estadoEnviado = estado;
			_estadoEnviadoA = pilotoActor;
			object[] eventContent = new object[14]
			{
				(byte)estado.Fase,
				(byte)estado.Tipo,
				estado.Serie,
				estado.EnemigoViewId,
				estado.Furia,
				estado.Agotado,
				estado.Vida,
				estado.VidaMaxima,
				estado.SegundosParaReaparecer,
				estado.UltimaPosicion,
				estado.Progreso,
				estado.Opciones ?? Array.Empty<float>(),
				estado.Presencia,
				estado.Retirada
			};
			_evEstado?.RaiseEvent(eventContent, Solo(pilotoActor), SendOptions.SendReliable);
		}
	}

	private static void AlRecibirEstado(EventData evento)
	{
		if (!SoyAutoridad && VieneDelAnfitrion(evento) && evento.CustomData is object[] array && array.Length == 14 && array[0] is byte fase && array[1] is byte tipo && array[2] is int serie && array[3] is int enemigoViewId && array[4] is float furia && array[5] is bool agotado && array[6] is int vida && array[7] is int vidaMaxima && array[8] is float segundosParaReaparecer && array[9] is Vector3 ultimaPosicion && array[10] is float progreso && array[11] is float[] array2 && array[12] is float presencia && array[13] is bool retirada)
		{
			Piloto.RecibirEstado(new EstadoMonstruo
			{
				Fase = (FaseMonstruo)fase,
				Tipo = (TipoMonstruo)tipo,
				Serie = serie,
				EnemigoViewId = enemigoViewId,
				Furia = furia,
				Agotado = agotado,
				Vida = vida,
				VidaMaxima = vidaMaxima,
				SegundosParaReaparecer = segundosParaReaparecer,
				UltimaPosicion = ultimaPosicion,
				Progreso = progreso,
				Opciones = ((array2.Length != 0) ? array2 : null),
				Presencia = presencia,
				Retirada = retirada
			});
		}
	}

	public static void EnviarMareo(float segundos)
	{
		if (Autoridad.PilotoEsLocal)
		{
			Piloto.RecibirMareo(segundos);
		}
		else
		{
			_evMareo?.RaiseEvent(segundos, Solo(Autoridad.PilotoActor), SendOptions.SendReliable);
		}
	}

	private static void AlRecibirMareo(EventData evento)
	{
		if (!SoyAutoridad && VieneDelAnfitrion(evento) && evento.CustomData is float value)
		{
			Piloto.RecibirMareo(Mathf.Clamp(value, 0f, 10f));
		}
	}

	public static void EnviarRuido(Vector3 posicion, float radio)
	{
		if (Autoridad.PilotoEsLocal)
		{
			Piloto.RecibirRuido(posicion, radio);
		}
		else if (!(Time.unscaledTime < _siguienteRuido))
		{
			_siguienteRuido = Time.unscaledTime + 1f / 15f;
			_evRuido?.RaiseEvent(new object[2] { posicion, radio }, Solo(Autoridad.PilotoActor), SendOptions.SendUnreliable);
		}
	}

	private static void AlRecibirRuido(EventData evento)
	{
		if (!SoyAutoridad && VieneDelAnfitrion(evento) && evento.CustomData is object[] array && array.Length == 2 && array[0] is Vector3 posicion && array[1] is float radio)
		{
			Piloto.RecibirRuido(posicion, radio);
		}
	}

	private static bool VieneDelAnfitrion(EventData evento)
	{
		if (PhotonNetwork.MasterClient != null)
		{
			return evento.Sender == PhotonNetwork.MasterClient.ActorNumber;
		}
		return false;
	}

	/// <summary>El jugador local acaba de recibir daño del monstruo: se lo decimos al anfitrión.</summary>
	public static void EnviarGolpe()
	{
		if (SoyAutoridad)
		{
			Furia.GolpeAcertado();
		}
		else if (!(Time.unscaledTime < _siguienteGolpe))
		{
			_siguienteGolpe = Time.unscaledTime + 0.3f;
			_evGolpe?.RaiseEvent((byte)1, NetworkingEvents.RaiseMasterClient, SendOptions.SendReliable);
		}
	}

	private static void AlRecibirGolpe(EventData evento)
	{
		// Solo tiene sentido si hay un monstruo vivo, y nunca viene del propio piloto.
		if (SoyAutoridad && Autoridad.Actual != null && evento.Sender != Autoridad.PilotoActor)
		{
			Furia.GolpeAcertado();
		}
	}

	/// <summary>El anfitrión avisa a todos de que el equipo ha caído y ha ganado el monstruo.</summary>
	public static void EnviarVictoria(int actorDelMonstruo, string nombre)
	{
		Final.Mostrar(actorDelMonstruo, nombre); // en el propio anfitrión
		if (SemiFunc.IsMultiplayer())
		{
			_evVictoria?.RaiseEvent(new object[2] { actorDelMonstruo, nombre }, NetworkingEvents.RaiseOthers, SendOptions.SendReliable);
		}
	}

	private static void AlRecibirVictoria(EventData evento)
	{
		if (VieneDelAnfitrion(evento) && evento.CustomData is object[] array && array.Length == 2 && array[0] is int actor && array[1] is string nombre)
		{
			// El nombre viene de otro equipo: lo acortamos por si acaso.
			Final.Mostrar(actor, (nombre.Length > 32) ? nombre.Substring(0, 32) : nombre);
		}
	}

	private static RaiseEventOptions Solo(int actor)
	{
		RaiseEventOptions raiseEventOptions = new RaiseEventOptions();
		raiseEventOptions.TargetActors = new int[1] { actor };
		return raiseEventOptions;
	}
}
