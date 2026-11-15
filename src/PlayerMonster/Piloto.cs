using Photon.Pun;
using UnityEngine;

namespace JugadorMonstruo;

internal static class Piloto
{
	private static EntradaMonstruo _entrada;

	private static Enemy? _enemigoRemoto;

	private static TipoMonstruo? _eleccionEnviada;

	private static bool _eleccionPendiente;

	private static int _pasoSecuencia = -1;

	private static float _mensajeFinalHasta;

	private const int PasosDeFallo = 3;

	private static string MensajeDeFallo(int paso)
	{
		return paso switch
		{
			0 => Idioma.T("ERROR DE SISTEMA", "SYSTEM ERROR"),
			1 => Idioma.T("CIRCUITOS COMPROMETIDOS", "CIRCUITS COMPROMISED"),
			_ => Idioma.T("PERDIENDO EL CONTROL", "LOSING CONTROL"),
		};
	}

	private static readonly Color RojoOscuro = new Color(0.5f, 0f, 0f);

	public static EstadoMonstruo Estado { get; private set; }

	public static bool Activo => Estado.Fase != FaseMonstruo.Ninguna;

	public static Enemy? Enemigo
	{
		get
		{
			if (Estado.Fase != FaseMonstruo.Vivo)
			{
				return null;
			}
			if (Mensajes.SoyAutoridad)
			{
				Enemy actual = Autoridad.Actual;
				if (!(actual != null) || !actual)
				{
					return null;
				}
				return actual;
			}
			if ((_enemigoRemoto == null || !_enemigoRemoto) && Estado.EnemigoViewId != 0)
			{
				PhotonView photonView = PhotonView.Find(Estado.EnemigoViewId);
				_enemigoRemoto = (photonView ? photonView.GetComponent<Enemy>() : null);
			}
			if (!(_enemigoRemoto != null) || !_enemigoRemoto)
			{
				return null;
			}
			return _enemigoRemoto;
		}
	}

	private static bool EsMonstruo(FaseMonstruo fase)
	{
		if (fase != FaseMonstruo.Vivo)
		{
			return fase == FaseMonstruo.Esperando;
		}
		return true;
	}

	private static float _siguienteEleccion;

	private static float _ultimoEstadoEn;

	public static void RecibirEstado(EstadoMonstruo nuevo)
	{
		EstadoMonstruo estado = Estado;
		Estado = nuevo;
		_ultimoEstadoEn = Time.unscaledTime;
		if (nuevo.Fase == FaseMonstruo.Transformando && estado.Fase != FaseMonstruo.Transformando)
		{
			// En la primera transformación no se elige: el monstruo sale al azar. El panel de
			// selección solo aparece al reaparecer, cuando el jugador ya sabe a qué está jugando.
			_pasoSecuencia = -1;
		}
		if (!EsMonstruo(estado.Fase) && EsMonstruo(nuevo.Fase) && Plugin.OcultarCuerpo.Value)
		{
			CuerpoJugador.Ocultar();
		}
		if (nuevo.Serie != estado.Serie)
		{
			_enemigoRemoto = null;
			_entrada = default;
			Camara.MostrarModelo();
			Sentidos.Ocultar();
			Olfato.Reiniciar();
		}
		if (estado.Fase == FaseMonstruo.Transformando && nuevo.Fase == FaseMonstruo.Vivo)
		{
			Sacudida(5f, 1f, 0.3f);
			_mensajeFinalHasta = Time.time + 4f;
			SemiFunc.UIFocusText(Idioma.T("Impide que el equipo cumpla la cuota", "Stop the team from meeting the quota"), Color.red, Color.white, 8f);
		}
		if (estado.Fase == FaseMonstruo.Vivo && nuevo.Fase == FaseMonstruo.Esperando)
		{
			Seleccion.Empezar(nuevo.Opciones);
			_eleccionPendiente = true;
			Sacudida(4f, 0.8f, 0f);
		}
		if (EsMonstruo(estado.Fase) && nuevo.Fase == FaseMonstruo.Ninguna)
		{
			CuerpoJugador.Restaurar();
		}
	}

	private static void Sacudida(float fuerza, float duracion, float duracionImpacto)
	{
		if ((bool)CameraGlitch.Instance)
		{
			CameraGlitch.Instance.PlayLong();
		}
		if ((bool)GameDirector.instance)
		{
			GameDirector.instance.CameraShake.Shake(fuerza, duracion);
			if (duracionImpacto > 0f)
			{
				GameDirector.instance.CameraImpact.Shake(fuerza, duracionImpacto);
			}
		}
	}

	public static void RecibirMareo(float segundos)
	{
		Sentidos.Marear(segundos);
	}

	public static void RecibirRuido(Vector3 posicion, float radio)
	{
		Sentidos.RegistrarRuido(posicion, radio);
	}

	public static void EnviarEntrada()
	{
		if (Estado.Fase == FaseMonstruo.Vivo)
		{
			_entrada = EntradaMonstruo.LeerLocal(Enemigo);
			Mensajes.EnviarEntrada(_entrada);
		}
	}

	public static void Tick()
	{
		if (Time.time < _mensajeFinalHasta)
		{
			Aviso.Mostrar(Idioma.T("ERES EL MONSTRUO", "YOU ARE THE MONSTER"), 130f, Color.red);
		}
		// Red de seguridad: mientras somos el monstruo, el anfitrión nos manda el estado diez veces por
		// segundo. Si deja de llegar (cambió el anfitrión, se cortó, eligió a otro piloto...), dejamos
		// de ser monstruo en vez de quedarnos atascados con su vista y sin robot.
		if (!Mensajes.SoyAutoridad && Activo && Time.unscaledTime - _ultimoEstadoEn > 3f)
		{
			Plugin.Log.LogWarning("Piloto: hace 3 s que no llega el estado del anfitrión; vuelvo a ser robot.");
			RecibirEstado(default);
		}
		// Los efectos de vista solo valen mientras se controla un monstruo vivo.
		if (Estado.Fase != FaseMonstruo.Vivo)
		{
			Sentidos.Ocultar();
			Camara.MostrarModelo();
		}
		// Se publica cada frame (solo envía si cambia): también "ninguno" al dejar de ser monstruo.
		RedMonstruo.AnunciarEnemigo(Enemigo);
		if (!Activo || !SemiFunc.RunIsLevel() || !PlayerController.instance || !GameDirector.instance)
		{
			return;
		}
		if (Estado.Fase == FaseMonstruo.Transformando)
		{
			SecuenciaDeFallo(Estado.Progreso);
			return;
		}
		if (Plugin.OcultarCuerpo.Value)
		{
			CuerpoJugador.Ocultar();
		}
		PlayerController.instance.InputDisable(0.1f);
		CuerpoJugador.Tick();
		if (Estado.Fase == FaseMonstruo.Esperando)
		{
			if (Estado.SegundosParaReaparecer > 0f)
			{
				Aviso.Mostrar(Idioma.T("REAPARECES EN ", "RESPAWNING IN ") + Mathf.CeilToInt(Estado.SegundosParaReaparecer), 90f, Color.red);
			}
			Seleccion.Mostrar(Estado.Opciones);
			EnviarEleccion();
			Camara.Mirar(Estado.UltimaPosicion + Vector3.up);
			return;
		}
		if (Estado.Retirada)
		{
			Aviso.Mostrar(Idioma.T("RETIRADA", "RETREAT"), 70f, new Color(1f, 0.45f, 0.1f));
		}
		Enemy enemigo = Enemigo;
		if (!(enemigo == null))
		{
			EntradaMonstruo entrada = _entrada;
			if (Estado.Agotado)
			{
				entrada.Atacar = false;
				entrada.Correr = false;
			}
			Sentidos.Tick(Estado.Tipo, enemigo, entrada);
			Olfato.Tick(enemigo);
			if (Estado.Tipo == TipoMonstruo.Hunter)
			{
				Mira.Mostrar(entrada.Atacar);
			}
			Camara.Seguir(enemigo);
		}
	}

	private static void SecuenciaDeFallo(float progreso)
	{
		GameObject gameObject = Plugin.Instancia.gameObject;
		if ((bool)PostProcessing.Instance)
		{
			PostProcessing.Instance.VignetteOverride(RojoOscuro, Mathf.Lerp(0.15f, 0.6f, progreso), 1f, 3f, 1f, 0.2f, gameObject);
			PostProcessing.Instance.SaturationOverride(Mathf.Lerp(-10f, -70f, progreso), 3f, 1f, 0.2f, gameObject);
			PostProcessing.Instance.ContrastOverride(Mathf.Lerp(0f, 15f, progreso), 3f, 1f, 0.2f, gameObject);
		}
		int num = Mathf.Min((int)(progreso * (float)PasosDeFallo), PasosDeFallo - 1);
		if (num != _pasoSecuencia)
		{
			_pasoSecuencia = num;
			float strengthAdd = 1f + (float)num;
			if (num == 0)
			{
				CameraGlitch.Instance.PlayTiny();
			}
			else
			{
				CameraGlitch.Instance.PlayShort();
			}
			GameDirector.instance.CameraShake.Shake(strengthAdd, 0.6f);
			GameDirector.instance.CameraImpact.Shake(strengthAdd, 0.15f);
		}
		Aviso.Mostrar(MensajeDeFallo(num), 80f + (float)num * 15f, Color.red);
	}

	private static void EnviarEleccion()
	{
		// Se envía al cambiar y, además, se repite dos veces por segundo mientras el panel está
		// abierto: así un mensaje perdido o llegado a destiempo no deja al anfitrión sin la elección.
		TipoMonstruo? cursor = Seleccion.Cursor;
		if (_eleccionPendiente || cursor != _eleccionEnviada || Time.unscaledTime >= _siguienteEleccion)
		{
			_eleccionPendiente = false;
			_eleccionEnviada = cursor;
			_siguienteEleccion = Time.unscaledTime + 0.5f;
			Mensajes.EnviarEleccion(cursor);
		}
	}
}
