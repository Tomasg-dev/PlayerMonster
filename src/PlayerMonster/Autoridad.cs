using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using REPOLib.Modules;
using UnityEngine;

namespace JugadorMonstruo;

internal static class Autoridad
{
	private static readonly Dictionary<TipoMonstruo, Type> Cerebros = new Dictionary<TipoMonstruo, Type>
	{
		[TipoMonstruo.Animal] = typeof(EnemyAnimal),
		[TipoMonstruo.Runner] = typeof(EnemyRunner),
		[TipoMonstruo.Tumbler] = typeof(EnemyTumbler),
		[TipoMonstruo.Bowtie] = typeof(EnemyBowtie),
		[TipoMonstruo.Hunter] = typeof(EnemyHunter),
		[TipoMonstruo.Tick] = typeof(EnemyTick),
		[TipoMonstruo.Robe] = typeof(EnemyRobe),
		[TipoMonstruo.SlowWalker] = typeof(EnemySlowWalker),
		[TipoMonstruo.Beamer] = typeof(EnemyBeamer)
	};

	private const float CaducidadEntrada = 0.5f;

	public static EntradaMonstruo Entrada;

	private static EntradaMonstruo _entradaRecibida;

	private static float _entradaRecibidaEn = float.NegativeInfinity;

	private static TipoMonstruo? _eleccionPiloto;

	private static float _margenAparicionHasta;

	private static bool _derrotaBloqueada;

	private static bool _creadoPorNosotros;

	private static int _serie;

	private static bool _transformando;

	private static float _transformarDesde;

	private static float _transformarDuracion;

	private static int _muertes;

	private static bool _esperandoReaparicion;

	private static float _reaparecerEn;

	private static bool _protegerAlAparecer;

	private static float _proteccionHasta;

	private static Vector3 _ultimaPosicion;

	public static Enemy? Actual { get; private set; }

	public static TipoMonstruo TipoActual { get; private set; }

	public static bool Activa
	{
		get
		{
			if (!(Actual != null) && !_esperandoReaparicion)
			{
				return _transformando;
			}
			return true;
		}
	}

	public static int PilotoActor { get; private set; }

	public static bool PilotoEsLocal
	{
		get
		{
			if (SemiFunc.IsMultiplayer() && PhotonNetwork.LocalPlayer != null)
			{
				return PilotoActor == PhotonNetwork.LocalPlayer.ActorNumber;
			}
			return true;
		}
	}

	public static bool EstaPoseido(Enemy enemy)
	{
		if (Actual != null)
		{
			return Actual == enemy;
		}
		return false;
	}

	/// <summary>
	/// ¿Está el piloto moviendo a este enemigo ahora mismo? Es EstaPoseido salvo durante la retirada
	/// forzada: ahí el enemigo sigue siendo "nuestro" (no reacciona a lo que ve u oye), pero su
	/// Update original vuelve a ejecutarse para que la IA lo saque de la sala.
	/// </summary>
	public static bool Controla(Enemy enemy)
	{
		return EstaPoseido(enemy) && !Presencia.EnRetirada;
	}

	public static void RecibirEntrada(EntradaMonstruo entrada)
	{
		_entradaRecibida = entrada;
		_entradaRecibidaEn = Time.unscaledTime;
	}

	public static void RecibirEleccion(TipoMonstruo? tipo)
	{
		if (tipo != _eleccionPiloto)
		{
			Plugin.Log.LogInfo("Selección: el piloto pide " + (tipo.HasValue ? tipo.Value.ToString() : "al azar") + ".");
		}
		_eleccionPiloto = tipo;
	}

	public static void Alternar()
	{
		if (Activa)
		{
			Soltar("tecla");
			return;
		}
		PilotoActor = ModoJuego.PilotoDeLaTecla();
		Iniciar(Plugin.MonstruoDePrueba.Value);
	}

	public static void SiguienteTipo()
	{
		if (Activa)
		{
			Plugin.Log.LogInfo("Autoridad: deja de ser monstruo (tecla de posesión) antes de cambiar de tipo.");
			return;
		}
		TipoMonstruo[] array = (TipoMonstruo[])Enum.GetValues(typeof(TipoMonstruo));
		int num = (Array.IndexOf(array, Plugin.MonstruoDePrueba.Value) + 1) % array.Length;
		Plugin.MonstruoDePrueba.Value = array[num];
		Plugin.Log.LogInfo($"Autoridad: monstruo de prueba = {array[num]}.");
	}

	public static void MatarDePrueba()
	{
		if (Actual != null && (bool)Actual && Actual.HasHealth && Actual.EnemyParent.Spawned)
		{
			Actual.Health.Hurt(999999, Vector3.up);
		}
	}

	public static void EmpezarTransformacion(int pilotoActor, float duracion)
	{
		if (!Activa && Mensajes.SoyAutoridad && SemiFunc.RunIsLevel())
		{
			PilotoActor = pilotoActor;
			_transformando = true;
			_transformarDesde = Time.time;
			_transformarDuracion = Mathf.Max(0.1f, duracion);
			_eleccionPiloto = null;
			Plugin.Log.LogInfo(string.Format("Autoridad: empieza la transformación del piloto (actor {0}{1}).", pilotoActor, PilotoEsLocal ? ", local" : ", remoto"));
			PublicarEstado();
		}
	}

	public static bool Iniciar(TipoMonstruo tipo)
	{
		if (!SemiFunc.RunIsLevel())
		{
			Plugin.Log.LogInfo("Autoridad: solo disponible dentro de un nivel.");
			return false;
		}
		if (!Mensajes.SoyAutoridad)
		{
			Plugin.Log.LogInfo("Autoridad: solo el anfitrión puede crear al monstruo.");
			return false;
		}
		bool esperandoReaparicion = _esperandoReaparicion;
		if (!esperandoReaparicion)
		{
			_muertes = 0;
		}
		Type cerebro = Cerebros[tipo];
		Enemy enemy = ((Plugin.ReutilizarExistente.Value && !esperandoReaparicion) ? BuscarCercano(cerebro) : null);
		bool flag = false;
		if (enemy == null)
		{
			enemy = Crear(cerebro);
			flag = true;
		}
		if (enemy == null)
		{
			Plugin.Log.LogWarning($"Autoridad: no encontré ni pude crear un {tipo}.");
			return false;
		}
		Actual = enemy;
		TipoActual = tipo;
		_serie++;
		_esperandoReaparicion = false;
		_protegerAlAparecer = esperandoReaparicion;
		_creadoPorNosotros = flag;
		_margenAparicionHasta = Time.time + 8f;
		_ultimaPosicion = enemy.transform.position;
		_entradaRecibidaEn = float.NegativeInfinity;
		Entrada = default;
		ProteccionObjetos.Activar(enemy);
		Furia.Reiniciar();
		Presencia.Reiniciar();
		AjustarVida(enemy);
		Plugin.Log.LogInfo("Autoridad: el piloto controla a '" + enemy.EnemyParent.enemyName + "' (" + (flag ? "recién creado" : "ya estaba en el nivel") + ").");
		PublicarEstado();
		return true;
	}

	/// <summary>
	/// Vida del jugador-monstruo. Parte POR DEBAJO de la vida original del enemigo y crece con los
	/// humanos y con el nivel, para que al principio (robots sin armas) se le pueda hacer frente:
	///   vida = original x (base + porJugador x (humanos - 1)) x (1 + porNivel x (nivel - 1))
	/// acotada entre x0,3 y el tope, y con un extra por cada muerte previa en el mismo nivel.
	/// La garrapata no se toca: en ella la vida es también el depósito de lo que chupa.
	/// </summary>
	private static void AjustarVida(Enemy enemigo)
	{
		if (!enemigo.HasHealth || TipoActual == TipoMonstruo.Tick)
		{
			return;
		}
		int original = enemigo.Health.health;
		int humanos = Mathf.Max(1, GameDirector.instance.PlayerList.Count - 1);
		int nivel = RunManager.instance.levelsCompleted + 1;

		float porHumanos = Plugin.VidaBase.Value + Plugin.VidaExtraPorJugador.Value * (float)(humanos - 1);
		float porNivel = 1f + Plugin.VidaExtraPorNivel.Value * (float)(nivel - 1);
		float multiplicador = Mathf.Clamp(porHumanos * porNivel, 0.3f, Plugin.VidaMultiplicadorMaximo.Value);
		multiplicador *= 1f + Plugin.VidaExtraPorMuerte.Value * (float)Mathf.Min(_muertes, 3);

		int vida = Mathf.Max(1, Mathf.RoundToInt((float)original * multiplicador));
		enemigo.Health.health = vida;
		enemigo.Health.healthCurrent = vida;
		if (_muertes >= Plugin.OrbesMaximos.Value)
		{
			enemigo.Health.spawnValuable = false;
		}
		Plugin.Log.LogInfo($"Vida: {original} -> {vida} (x{multiplicador:0.00}; humanos={humanos}, nivel={nivel}, muertes={_muertes}).");
	}

	public static void Soltar(string motivo)
	{
		Plugin.Log.LogInfo("Autoridad: terminada (" + motivo + ").");
		DejarEnemigo();
		_esperandoReaparicion = false;
		_transformando = false;
		if (_derrotaBloqueada && RunManager.instance != null)
		{
			RunManager.instance.allPlayersDeadCheckDisabled = false;
			_derrotaBloqueada = false;
		}
		PublicarEstado();
	}

	private static void DejarEnemigo()
	{
		Enemy actual = Actual;
		Actual = null;
		_serie++;
		ProteccionObjetos.Desactivar();
		if (_creadoPorNosotros && (bool)actual)
		{
			Eliminar(actual.EnemyParent);
		}
		_creadoPorNosotros = false;
	}

	public static void Tick()
	{
		if (!Activa)
		{
			return;
		}
		if (!SemiFunc.RunIsLevel() || (Actual != null && !Actual))
		{
			Soltar("el nivel terminó o el enemigo ya no existe");
			return;
		}
		if (!PilotoPresente())
		{
			Soltar("el piloto ya no está en la sala");
			return;
		}
		if (_transformando)
		{
			if (Time.time < _transformarDesde + _transformarDuracion)
			{
				PublicarEstado();
				return;
			}
			_transformando = false;
			// Primera forma: siempre al azar entre las disponibles (null = sin preferencia).
			if (!Iniciar(Seleccion.Resolver(null)))
			{
				Soltar("no se pudo transformar");
			}
			return;
		}
		MantenerPartida();
		if (_esperandoReaparicion)
		{
			TickEsperando();
			PublicarEstado();
			return;
		}
		Enemy actual = Actual;
		EnemyParent enemyParent = actual.EnemyParent;
		if (!enemyParent.Spawned)
		{
			if (Time.time >= _margenAparicionHasta)
			{
				Morir();
				return;
			}
			enemyParent.DespawnedTimerSet(0f);
		}
		else
		{
			_ultimaPosicion = actual.transform.position;
			if (_protegerAlAparecer && Time.time > _margenAparicionHasta - 7.5f)
			{
				_protegerAlAparecer = false;
				_proteccionHasta = Time.time + Plugin.ProteccionReaparicion.Value;
			}
		}
		Entrada = ((Time.unscaledTime - _entradaRecibidaEn <= 0.5f) ? _entradaRecibida : default(EntradaMonstruo));
		Furia.Filtrar(ref Entrada, TipoActual);
		enemyParent.SpawnedTimerPause(1f);
		if ((_protegerAlAparecer || Time.time < _proteccionHasta) && actual.HasHealth)
		{
			actual.Health.OverrideDamageResistance(1f, 0.2f);
		}
		ProteccionObjetos.Tick(actual);
		if (enemyParent.Spawned)
		{
			Presencia.Tick(actual, TipoActual);
		}
		PublicarEstado();
	}

	private static void MantenerPartida()
	{
		if (Plugin.EvitarDerrotaEnSolitario.Value && GameDirector.instance.PlayerList.Count == 1)
		{
			RunManager.instance.allPlayersDeadCheckDisabled = true;
			_derrotaBloqueada = true;
		}
	}

	private static void Morir()
	{
		_muertes++;
		Seleccion.Bloquear(TipoActual);
		DejarEnemigo();
		_esperandoReaparicion = true;
		_eleccionPiloto = null;
		_reaparecerEn = Time.time + Mathf.Max(1f, Plugin.TiempoReaparicion.Value);
		Plugin.Log.LogInfo($"Autoridad: el monstruo ha muerto ({_muertes} en este nivel). Reaparece en {Plugin.TiempoReaparicion.Value:0.#} s.");
		PublicarEstado();
	}

	private static void TickEsperando()
	{
		if (Time.time < _reaparecerEn)
		{
			return;
		}
		TipoMonstruo elegido = Seleccion.Resolver(_eleccionPiloto);
		Plugin.Log.LogInfo("Selección: reaparece como " + elegido + " (el piloto pidió " + (_eleccionPiloto.HasValue ? _eleccionPiloto.Value.ToString() : "al azar") + ").");
		if (!Iniciar(elegido))
		{
			Soltar("no se pudo reaparecer");
		}
	}

	private static void PublicarEstado()
	{
		EstadoMonstruo estado = new EstadoMonstruo
		{
			Fase = (Activa ? (_transformando ? FaseMonstruo.Transformando : (_esperandoReaparicion ? FaseMonstruo.Esperando : FaseMonstruo.Vivo)) : FaseMonstruo.Ninguna),
			Tipo = TipoActual,
			Serie = _serie,
			Furia = Furia.Nivel,
			Agotado = Furia.Agotado,
			Presencia = Presencia.Nivel,
			Retirada = (Actual != null && Presencia.EnRetirada),
			UltimaPosicion = _ultimaPosicion
		};
		Enemy actual = Actual;
		if (actual != null && (bool)actual)
		{
			estado.EnemigoViewId = (actual.PhotonView ? actual.PhotonView.ViewID : 0);
			if (actual.HasHealth)
			{
				estado.Vida = actual.Health.healthCurrent;
				estado.VidaMaxima = actual.Health.health;
			}
		}
		if (_esperandoReaparicion)
		{
			estado.SegundosParaReaparecer = Mathf.Max(0f, _reaparecerEn - Time.time);
		}
		if (_transformando)
		{
			estado.Progreso = Mathf.Clamp01((Time.time - _transformarDesde) / _transformarDuracion);
		}
		if (_transformando || _esperandoReaparicion)
		{
			estado.Opciones = Seleccion.Disponibilidad();
		}
		Mensajes.EnviarEstado(estado);
	}

	public static void RuidoEnElNivel(Vector3 posicion, float radio)
	{
		if (Mensajes.SoyAutoridad && !(Actual == null) && (bool)Actual && TipoActual == TipoMonstruo.Hunter)
		{
			Mensajes.EnviarRuido(posicion, radio);
		}
	}

	private static bool PilotoPresente()
	{
		if (!PilotoEsLocal)
		{
			if (PhotonNetwork.CurrentRoom != null)
			{
				return PhotonNetwork.CurrentRoom.GetPlayer(PilotoActor) != null;
			}
			return false;
		}
		return true;
	}

	private static PlayerAvatar AvatarDelPiloto()
	{
		if (!PilotoEsLocal && (bool)GameDirector.instance)
		{
			foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
			{
				if ((bool)player && (bool)player.photonView && player.photonView.Owner != null && player.photonView.Owner.ActorNumber == PilotoActor)
				{
					return player;
				}
			}
		}
		return PlayerAvatar.instance;
	}

	private static void Eliminar(EnemyParent padre)
	{
		if ((bool)padre)
		{
			EnemyDirector.instance.enemiesSpawned.Remove(padre);
			if (SemiFunc.IsMultiplayer())
			{
				PhotonNetwork.Destroy(padre.gameObject);
			}
			else
			{
				UnityEngine.Object.Destroy(padre.gameObject);
			}
		}
	}

	private static Enemy? BuscarCercano(Type cerebro)
	{
		Vector3 yo = AvatarDelPiloto().transform.position;
		return (from p in EnemyDirector.instance.enemiesSpawned
			where (bool)p && p.Spawned && (bool)p.Enemy && (bool)p.Enemy.GetComponent(cerebro)
			select p.Enemy into e
			orderby Vector3.Distance(e.transform.position, yo)
			select e).FirstOrDefault();
	}

	private static Enemy? Crear(Type cerebro)
	{
		EnemySetup enemySetup = SetupDe(cerebro);
		if (enemySetup == null)
		{
			return null;
		}
		List<EnemyParent>? list = Enemies.SpawnEnemy(enemySetup, PuntoDeAparicion(), Quaternion.identity);
		if (list == null)
		{
			return null;
		}
		return list.Select((EnemyParent p) => p.GetComponentInChildren<Enemy>(includeInactive: true)).FirstOrDefault((Enemy e) => e != null);
	}

	private static EnemySetup? SetupDe(Type cerebro)
	{
		return Enemies.AllEnemies.FirstOrDefault((EnemySetup s) => s != null && s.spawnObjects.Any((PrefabRef o) => o != null && o.Prefab != null && o.Prefab.GetComponentInChildren(cerebro, includeInactive: true) != null));
	}

	public static EnemySetup? SetupDe(TipoMonstruo tipo)
	{
		return SetupDe(Cerebros[tipo]);
	}

	private static Vector3 PuntoDeAparicion()
	{
		Transform transform = AvatarDelPiloto().transform;
		if (!Plugin.CrearLejos.Value)
		{
			return transform.position + transform.forward * 2.5f;
		}
		List<Vector3> list = new List<Vector3>();
		foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
		{
			if ((bool)player && !player.isDisabled)
			{
				list.Add(player.transform.position);
			}
		}
		if (list.Count == 0)
		{
			list.Add(transform.position);
		}
		foreach (LevelPoint levelPathPoint in LevelGenerator.Instance.LevelPathPoints)
		{
			if ((bool)levelPathPoint && (levelPathPoint.Truck || ((bool)levelPathPoint.Room && levelPathPoint.Room.Truck)))
			{
				list.Add(levelPathPoint.transform.position);
			}
		}
		LevelPoint levelPoint = null;
		float num = -1f;
		foreach (LevelPoint levelPathPoint2 in LevelGenerator.Instance.LevelPathPoints)
		{
			if (!levelPathPoint2 || levelPathPoint2.Truck || ((bool)levelPathPoint2.Room && levelPathPoint2.Room.Truck))
			{
				continue;
			}
			float num2 = float.MaxValue;
			foreach (Vector3 item in list)
			{
				num2 = Mathf.Min(num2, Vector3.Distance(levelPathPoint2.transform.position, item));
			}
			if (num2 > num)
			{
				num = num2;
				levelPoint = levelPathPoint2;
			}
		}
		if (!levelPoint)
		{
			return transform.position + transform.forward * 2.5f;
		}
		return levelPoint.transform.position;
	}
}
