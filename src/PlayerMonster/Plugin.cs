using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JugadorMonstruo;

[BepInPlugin(Guid, Nombre, Version)]
[BepInDependency("REPOLib", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BaseUnityPlugin
{
	public const string Guid = "tommy777.playermonster";

	public const string Nombre = "PlayerMonster";

	public const string Version = "1.0.0";

	internal static Plugin Instancia;

	internal static ManualLogSource Log;

	internal static ConfigEntry<Key> TeclaPosesion;

	internal static ConfigEntry<Key> TeclaCambiarTipo;

	internal static ConfigEntry<Key> TeclaMatar;

	internal static ConfigEntry<Key> TeclaCamara;

	internal static ConfigEntry<bool> PrimeraPersona;

	internal static ConfigEntry<Key> TeclaRuido;

	internal static ConfigEntry<float> DistanciaCamara;

	internal static ConfigEntry<bool> ModoAutomatico;

	internal static ConfigEntry<float> Probabilidad;

	internal static ConfigEntry<float> RetrasoTransformacion;

	internal static ConfigEntry<bool> MonstruoAleatorio;

	internal static ConfigEntry<bool> SoloJugadorMonstruo;

	internal static ConfigEntry<bool> ProtegerObjetos;

	internal static ConfigEntry<bool> ElegirMonstruo;

	internal static ConfigEntry<float> BloqueoDificultad1;

	internal static ConfigEntry<float> BloqueoDificultad2;

	internal static ConfigEntry<float> BloqueoDificultad3;

	internal static ConfigEntry<bool> VozCorrupta;

	internal static ConfigEntry<float> VozTono;

	internal static ConfigEntry<bool> UsarFuria;

	internal static ConfigEntry<float> VidaBase = null!;

	internal static ConfigEntry<float> VidaExtraPorJugador = null!;

	internal static ConfigEntry<float> VidaExtraPorNivel = null!;

	internal static ConfigEntry<float> VidaMultiplicadorMaximo = null!;

	internal static ConfigEntry<float> VidaExtraPorMuerte = null!;

	internal static ConfigEntry<bool> UsarPresencia = null!;

	internal static ConfigEntry<bool> AvisoCercania = null!;

	internal static ConfigEntry<float> CercaniaLejos = null!;

	internal static ConfigEntry<float> CercaniaCerca = null!;

	internal static ConfigEntry<float> CercaniaVolumen = null!;

	internal static ConfigEntry<bool> ReglaCamion = null!;

	internal static ConfigEntry<float> CamionSegundos = null!;

	internal static ConfigEntry<float> PausaTrasGolpe = null!;

	internal static ConfigEntry<bool> UsarOlfato = null!;

	internal static ConfigEntry<float> OlfatoSegundos = null!;

	internal static ConfigEntry<float> PresenciaSegundos = null!;

	internal static ConfigEntry<float> PresenciaRetirada = null!;

	internal static ConfigEntry<float> PresenciaRadio = null!;

	internal static ConfigEntry<bool> TeclasDePrueba = null!;

	internal static ConfigEntry<IdiomaMod> IdiomaTextos = null!;

	internal static ConfigEntry<bool> UsarSentidos;

	internal static ConfigEntry<float> CegueraHunter;





	internal static ConfigEntry<float> TiempoReaparicion;

	internal static ConfigEntry<float> ProteccionReaparicion;

	internal static ConfigEntry<int> OrbesMaximos;

	internal static ConfigEntry<bool> CrearLejos;

	internal static ConfigEntry<bool> OcultarCuerpo;

	internal static ConfigEntry<bool> ReutilizarExistente;

	internal static ConfigEntry<bool> EvitarDerrotaEnSolitario;

	internal static ConfigEntry<TipoMonstruo> MonstruoDePrueba;

	internal static ConfigEntry<ForzarPiloto> PilotoForzado;

	internal static ConfigEntry<int> RondasPorMonstruo;

	private readonly Harmony _harmony = new Harmony("tomas.jugadormonstruo");

	private void Awake()
	{
		Instancia = this;
		Log = Logger;
		transform.parent = null;
		gameObject.hideFlags = HideFlags.HideAndDontSave;
		TeclaPosesion = Config.Bind("Pruebas", "TeclaPosesion", Key.F8, "Tecla para poseer / soltar un enemigo (hito 1: solo host o un jugador).");
		TeclaCambiarTipo = Config.Bind("Pruebas", "TeclaCambiarTipo", Key.F9, "Tecla para pasar al siguiente tipo de monstruo de prueba.");
		TeclaMatar = Config.Bind("Pruebas", "TeclaMatar", Key.F10, "Tecla para matar al monstruo que controlas (para probar la reaparición jugando solo).");
		TeclaRuido = Config.Bind("Pruebas", "TeclaRuido", Key.F11, "Tecla para emitir un ruido delante de la cámara (para probar el oído del Hunter jugando solo).");
		MonstruoDePrueba = Config.Bind("Pruebas", "MonstruoDePrueba", TipoMonstruo.Runner, "Qué enemigo posee la tecla: usa el más cercano de ese tipo o crea uno delante de ti.");
		PilotoForzado = Config.Bind("Pruebas", "ForzarPiloto", ForzarPiloto.Nadie, "Saltarse el sorteo y fijar quién es el monstruo: Nadie (sorteo normal), Anfitrion o PrimerCliente. Manda el valor del anfitrión.");
		RondasPorMonstruo = Config.Bind("Modo", "RondasPorMonstruo", 1, new ConfigDescription("Cuántas rondas seguidas (niveles con monstruo) es monstruo el mismo jugador antes de sortear a otro. 0 = infinitas: el mismo toda la partida. Manda el valor del anfitrión.", new AcceptableValueRange<int>(0, 100)));
		ModoAutomatico = Config.Bind("Modo", "Automatico", defaultValue: true, "Al empezar cada nivel, un jugador elegido por sorteo se transforma en monstruo sin pulsar nada.");
		Probabilidad = Config.Bind("Modo", "Probabilidad", 100f, new ConfigDescription("Probabilidad (%) de que haya jugador-monstruo en cada nivel.", new AcceptableValueRange<float>(0f, 100f)));
		RetrasoTransformacion = Config.Bind("Modo", "RetrasoTransformacion", 8f, "Duración (s) de la secuencia de fallo en los circuitos antes de transformarte.");
		MonstruoAleatorio = Config.Bind("Modo", "MonstruoAleatorio", defaultValue: true, "Elegir el tipo de monstruo al azar entre los soportados. Si es false, usa MonstruoDePrueba.");
		SoloJugadorMonstruo = Config.Bind("Modo", "SoloJugadorMonstruo", defaultValue: true, "En los niveles con jugador-monstruo, el juego no crea sus enemigos normales.");
		ProtegerObjetos = Config.Bind("Monstruo", "ProtegerObjetos", defaultValue: true, "El jugador-monstruo no puede romper objetos de valor (ni con golpes ni empujándolos).");
		ElegirMonstruo = Config.Bind("Modo", "ElegirMonstruo", defaultValue: true, "Mostrar la lista para elegir monstruo (flechas izquierda / derecha) al transformarte y al reaparecer.");
		BloqueoDificultad1 = Config.Bind("Equilibrio", "BloqueoDificultad1", 30f, "Segundos que queda bloqueado un monstruo de dificultad 1 después de morir con él.");
		BloqueoDificultad2 = Config.Bind("Equilibrio", "BloqueoDificultad2", 75f, "Segundos que queda bloqueado un monstruo de dificultad 2 después de morir con él.");
		BloqueoDificultad3 = Config.Bind("Equilibrio", "BloqueoDificultad3", 150f, "Segundos que queda bloqueado un monstruo de dificultad 3 después de morir con él.");
		VozCorrupta = Config.Bind("Monstruo", "VozCorrupta", defaultValue: true, "La voz del jugador-monstruo sale del enemigo que controla, distorsionada (solo multijugador).");
		VozTono = Config.Bind("Monstruo", "VozTono", 0.8f, "Tono de la voz del monstruo (1 = normal, menos = más grave).");
		UsarFuria = Config.Bind("Equilibrio", "UsarFuria", defaultValue: true, "Barra de furia: limita cuánto tiempo seguido puede atacar el monstruo.");
		// Claves nuevas a propósito (no reutilizan "VidaPorJugador", etc.): así no se arrastran los
		// valores antiguos que cada jugador tenga guardados en su archivo de configuración.
		VidaBase = Config.Bind("Equilibrio", "VidaBase", 0.5f, "Fracción de la vida original del enemigo que tiene el monstruo con un solo humano en el nivel 1.");
		VidaExtraPorJugador = Config.Bind("Equilibrio", "VidaExtraPorJugador", 0.15f, "Vida que se suma por cada humano a partir del segundo (0.15 = +15 % de la original).");
		VidaExtraPorNivel = Config.Bind("Equilibrio", "VidaExtraPorNivel", 0.1f, "Crecimiento de la vida por cada nivel a partir del segundo (0.1 = +10 %).");
		VidaMultiplicadorMaximo = Config.Bind("Equilibrio", "VidaMultiplicadorMaximo", 2.5f, "Tope del multiplicador de vida por jugadores y nivel.");
		VidaExtraPorMuerte = Config.Bind("Equilibrio", "VidaExtraPorMuerte", 0.1f, "Vida extra cada vez que matan al monstruo en el mismo nivel (hasta 3 muertes).");
		UsarPresencia = Config.Bind("Equilibrio", "UsarPresencia", defaultValue: true, "El monstruo no puede quedarse junto a los jugadores: al llenarse la barra de presencia se retira solo.");
		AvisoCercania = Config.Bind("Humanos", "AvisoCercania", defaultValue: true, "Oír un latido (y ver un borde rojo de cerca) cuando el monstruo anda cerca. Es una opción de cada jugador.");
		CercaniaLejos = Config.Bind("Humanos", "CercaniaLejos", 18f, "Distancia (m) a la que empieza a oírse el latido. Atraviesa paredes.");
		CercaniaCerca = Config.Bind("Humanos", "CercaniaCerca", 8f, "Distancia (m) a la que aparece además el borde rojo.");
		CercaniaVolumen = Config.Bind("Humanos", "CercaniaVolumen", 0.7f, new ConfigDescription("Volumen del latido.", new AcceptableValueRange<float>(0f, 1f)));
		ReglaCamion = Config.Bind("Humanos", "ReglaCamion", defaultValue: true, "Dentro del camión el monstruo no te hace daño, salvo que acampes más de CamionSegundos.");
		CamionSegundos = Config.Bind("Humanos", "CamionSegundos", 20f, "Segundos seguidos en el camión (con un monstruo vivo) a partir de los cuales deja de protegerte.");
		PausaTrasGolpe = Config.Bind("Equilibrio", "PausaTrasGolpe", 1.5f, "Segundos sin poder atacar después de que un golpe del monstruo haga daño (monstruos de ataque continuo).");
		UsarOlfato = Config.Bind("Equilibrio", "UsarOlfato", defaultValue: true, "Si el monstruo lleva mucho sin nadie cerca, ve por un momento hacia dónde está el jugador más cercano.");
		OlfatoSegundos = Config.Bind("Equilibrio", "OlfatoSegundos", 60f, "Segundos sin ningún jugador a menos de 15 m para que se active el olfato.");
		PresenciaSegundos = Config.Bind("Equilibrio", "PresenciaSegundos", 30f, "Segundos junto a los jugadores (misma sala o muy cerca) que llenan la barra. Lejos se vacía el doble de rápido.");
		PresenciaRetirada = Config.Bind("Equilibrio", "PresenciaRetirada", 10f, "Segundos que dura la retirada forzada.");
		PresenciaRadio = Config.Bind("Equilibrio", "PresenciaRadio", 8f, "Distancia (m) a un jugador que cuenta como 'junto a él' aunque no estén en la misma sala.");
		TeclasDePrueba = Config.Bind("Pruebas", "TeclasDePrueba", defaultValue: false, "Activar F8-F11 (poseer, cambiar tipo, matar al monstruo, ruido). Apagadas por defecto para partidas reales.");
		IdiomaTextos = Config.Bind("General", "Idioma", IdiomaMod.Auto, "Idioma de los textos en pantalla / On-screen text language: Auto, Espanol, English.");
		UsarSentidos = Config.Bind("Equilibrio", "UsarSentidos", defaultValue: true, "Cada monstruo ve el mundo con una limitación propia (túnel, ceguera, mareo...).");
		CegueraHunter = Config.Bind("Equilibrio", "CegueraHunter", 0.95f, new ConfigDescription("Oscuridad general del Hunter. El centro de la pantalla siempre queda más claro que los bordes.", new AcceptableValueRange<float>(0f, 1f)));
		TiempoReaparicion = Config.Bind("Equilibrio", "TiempoReaparicion", 12f, "Segundos que tarda el monstruo en reaparecer tras morir.");
		ProteccionReaparicion = Config.Bind("Equilibrio", "ProteccionReaparicion", 3f, "Segundos de invulnerabilidad al reaparecer.");
		OrbesMaximos = Config.Bind("Equilibrio", "OrbesMaximos", 2, "Cuántas muertes del monstruo por nivel sueltan orbe de recompensa.");
		CrearLejos = Config.Bind("Monstruo", "CrearLejos", defaultValue: true, "El monstruo aparece en el punto del nivel más alejado de los jugadores. Si es false, aparece delante de ti.");
		OcultarCuerpo = Config.Bind("Monstruo", "OcultarCuerpo", defaultValue: true, "El robot del jugador-monstruo desaparece y cuenta como muerto, sin dejar cabeza.");
		ReutilizarExistente = Config.Bind("Pruebas", "ReutilizarExistente", defaultValue: false, "Poseer el enemigo más cercano de ese tipo que ya esté en el nivel en vez de crear uno nuevo.");
		EvitarDerrotaEnSolitario = Config.Bind("Pruebas", "EvitarDerrotaEnSolitario", defaultValue: true, "Jugando solo, evita que el nivel se dé por perdido al convertirte en monstruo.");
		TeclaCamara = Config.Bind("Camara", "TeclaCamara", Key.V, "Tecla para alternar entre primera y tercera persona siendo monstruo.");
		PrimeraPersona = Config.Bind("Camara", "PrimeraPersona", defaultValue: false, "Ver desde los ojos del monstruo en vez de desde detrás.");
		DistanciaCamara = Config.Bind("Camara", "Distancia", 3.5f, "Distancia de la cámara en tercera persona detrás del monstruo.");
		Mensajes.Registrar();
		_harmony.PatchAll();
		Log.LogInfo($"{Nombre} v{Version} cargado. Por Tommy777 y Dragonico.");
	}

	private void Update()
	{
		Keyboard current = Keyboard.current;
		Keyboard? pruebas = (TeclasDePrueba.Value ? current : null);
		if (pruebas != null && current[TeclaPosesion.Value].wasPressedThisFrame)
		{
			Autoridad.Alternar();
		}
		if (pruebas != null && current[TeclaCambiarTipo.Value].wasPressedThisFrame)
		{
			Autoridad.SiguienteTipo();
		}
		if (pruebas != null && current[TeclaMatar.Value].wasPressedThisFrame)
		{
			Autoridad.MatarDePrueba();
		}
		if (pruebas != null && current[TeclaRuido.Value].wasPressedThisFrame)
		{
			Sentidos.RuidoDePrueba();
		}
		if (current != null && current[TeclaCamara.Value].wasPressedThisFrame && Piloto.Activo)
		{
			Camara.Alternar();
		}
		RedMonstruo.Tick();
		Rotacion.Tick();
		ModoJuego.Tick();
		Piloto.EnviarEntrada();
		Autoridad.Tick();
		Piloto.Tick();
		Aviso.Tick();
		Seleccion.Tick();
		Hud.Tick();
		Cercania.Tick();
		Camion.Tick();
		Olfato.Mantener();
		Mira.Mantener();
	}
}
