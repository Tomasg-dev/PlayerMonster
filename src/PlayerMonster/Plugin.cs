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

	public const string Version = "1.0.2";

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

	private readonly Harmony _harmony = new Harmony(Guid);

	/// <summary>Identificador de las versiones anteriores del mod (JugadorMonstruo 0.x).</summary>
	private const string GuidAntiguo = "tomas.jugadormonstruo";

	/// <summary>true si además de este mod está cargada una versión antigua: nos apagamos.</summary>
	internal static bool Conflicto;

	private bool _conflictoComprobado;

	private float _avisoConflictoHasta;

	private void Awake()
	{
		Instancia = this;
		Log = Logger;
		transform.parent = null;
		gameObject.hideFlags = HideFlags.HideAndDontSave;
		// Los nombres de sección, de opción y las descripciones van en inglés: es lo que ve el público
		// en el archivo .cfg y en REPOConfig. Los nombres de las variables siguen en español.

		// --- General ---
		IdiomaTextos = Config.Bind("General", "Language", IdiomaMod.Auto, "On-screen text language: Auto (Spanish if your system is in Spanish, English otherwise), Spanish or English.");

		// --- Mode: the host's values decide ---
		ModoAutomatico = Config.Bind("Mode", "Enabled", defaultValue: true, "At the start of each level, one player is drawn and turns into a monster.");
		Probabilidad = Config.Bind("Mode", "Chance", 100f, new ConfigDescription("Chance (%) that a level has a player-monster.", new AcceptableValueRange<float>(0f, 100f)));
		RondasPorMonstruo = Config.Bind("Mode", "RoundsPerMonster", 1, new ConfigDescription("How many levels in a row the same player is the monster before another one is drawn. 0 = infinite: the same player for the whole run.", new AcceptableValueRange<int>(0, 100)));
		SoloJugadorMonstruo = Config.Bind("Mode", "OnlyPlayerMonster", defaultValue: true, "On levels with a player-monster, the game does not spawn its own enemies.");
		RetrasoTransformacion = Config.Bind("Mode", "TransformationSeconds", 8f, "Length (s) of the circuit-failure sequence before the player transforms.");
		ElegirMonstruo = Config.Bind("Mode", "ChooseOnRespawn", defaultValue: true, "Show the panel to choose your next monster (left / right arrows) when respawning. The first form is always random.");
		MonstruoAleatorio = Config.Bind("Mode", "RandomMonster", defaultValue: true, "Pick a random monster among the available ones. If false and ChooseOnRespawn is also false, TestMonster is always used.");

		// --- Monster ---
		CrearLejos = Config.Bind("Monster", "SpawnFarAway", defaultValue: true, "The monster spawns at the point of the level farthest from the players and the truck. If false, right in front of the player.");
		OcultarCuerpo = Config.Bind("Monster", "HideRobot", defaultValue: true, "The monster player's robot disappears and counts as dead, leaving no head to revive.");
		ProtegerObjetos = Config.Bind("Monster", "ProtectValuables", defaultValue: true, "The player-monster cannot break valuables, neither by hitting nor by pushing them.");
		VozCorrupta = Config.Bind("Monster", "DistortedVoice", defaultValue: true, "The monster player's voice chat comes out of the monster, distorted (multiplayer only).");
		VozTono = Config.Bind("Monster", "VoicePitch", 0.8f, "Pitch of the monster's voice (1 = normal, lower = deeper).");

		// --- Balance ---
		VidaBase = Config.Bind("Balance", "HealthBase", 0.5f, "Fraction of the enemy's original health the monster has with a single human on level 1.");
		VidaExtraPorJugador = Config.Bind("Balance", "HealthPerExtraPlayer", 0.15f, "Health added for each human beyond the first (0.15 = +15 % of the original).");
		VidaExtraPorNivel = Config.Bind("Balance", "HealthPerLevel", 0.1f, "Health growth for each level beyond the first (0.1 = +10 %).");
		VidaMultiplicadorMaximo = Config.Bind("Balance", "HealthMaxMultiplier", 2.5f, "Cap for the health multiplier from players and level.");
		VidaExtraPorMuerte = Config.Bind("Balance", "HealthPerDeath", 0.1f, "Extra health each time the monster is killed on the same level (up to 3 deaths).");
		UsarFuria = Config.Bind("Balance", "RageBar", defaultValue: true, "Rage bar: limits how long the monster can keep attacking.");
		PausaTrasGolpe = Config.Bind("Balance", "HitPauseSeconds", 1.5f, "Seconds without attacking after a hit from the monster deals damage (hold-to-attack monsters).");
		UsarPresencia = Config.Bind("Balance", "PresenceBar", defaultValue: true, "The monster cannot stay next to the players: when the presence bar fills up, it retreats on its own.");
		PresenciaSegundos = Config.Bind("Balance", "PresenceSeconds", 75f, "Seconds next to the players (same room or very close) that fill the bar. Away from them it drains twice as fast.");
		PresenciaRetirada = Config.Bind("Balance", "PresenceRetreatSeconds", 10f, "Length (s) of the forced retreat.");
		PresenciaRadio = Config.Bind("Balance", "PresenceRadius", 8f, "Distance (m) to a player that counts as 'next to them' even in a different room.");
		UsarSentidos = Config.Bind("Balance", "Senses", defaultValue: true, "Each monster perceives the world with its own handicap (tunnel vision, blindness, dizziness...).");
		CegueraHunter = Config.Bind("Balance", "HuntsmanDarkness", 0.95f, new ConfigDescription("Overall darkness of the Huntsman's view. The centre of the screen is always brighter than the edges.", new AcceptableValueRange<float>(0f, 1f)));
		UsarOlfato = Config.Bind("Balance", "Scent", defaultValue: true, "If nobody has been near the monster for a while, it briefly sees a marker towards the closest player.");
		OlfatoSegundos = Config.Bind("Balance", "ScentSeconds", 60f, "Seconds with no player within 15 m before scent kicks in.");
		TiempoReaparicion = Config.Bind("Balance", "RespawnSeconds", 12f, "Seconds the monster takes to respawn after dying.");
		ProteccionReaparicion = Config.Bind("Balance", "RespawnProtectionSeconds", 3f, "Seconds of invulnerability after respawning.");
		OrbesMaximos = Config.Bind("Balance", "MaxOrbs", 2, "How many monster deaths per level drop a reward orb.");
		BloqueoDificultad1 = Config.Bind("Balance", "LockSecondsTier1", 30f, "Seconds a tier 1 monster stays locked after you die with it.");
		BloqueoDificultad2 = Config.Bind("Balance", "LockSecondsTier2", 75f, "Seconds a tier 2 monster stays locked after you die with it.");
		BloqueoDificultad3 = Config.Bind("Balance", "LockSecondsTier3", 150f, "Seconds a tier 3 monster stays locked after you die with it.");

		// --- Humans: each player's own choice ---
		AvisoCercania = Config.Bind("Humans", "Heartbeat", defaultValue: true, "Hear a heartbeat (and see a red edge up close) when the monster is near. This is each player's own setting.");
		CercaniaLejos = Config.Bind("Humans", "HeartbeatDistance", 18f, "Distance (m) at which the heartbeat starts. It goes through walls.");
		CercaniaCerca = Config.Bind("Humans", "RedEdgeDistance", 8f, "Distance (m) at which the red edge also appears.");
		CercaniaVolumen = Config.Bind("Humans", "HeartbeatVolume", 0.7f, new ConfigDescription("Heartbeat volume.", new AcceptableValueRange<float>(0f, 1f)));
		ReglaCamion = Config.Bind("Humans", "TruckRule", defaultValue: true, "Inside the truck the monster cannot hurt you, unless you camp there longer than TruckSeconds.");
		CamionSegundos = Config.Bind("Humans", "TruckSeconds", 20f, "Seconds in a row inside the truck (with a live monster) after which it stops protecting you.");

		// --- Camera ---
		TeclaCamara = Config.Bind("Camera", "ToggleKey", Key.V, "Key to switch between first and third person as the monster.");
		PrimeraPersona = Config.Bind("Camera", "FirstPerson", defaultValue: false, "See through the monster's eyes instead of from behind.");
		DistanciaCamara = Config.Bind("Camera", "Distance", 3.5f, "Third-person camera distance behind the monster.");

		// --- Testing: for development, off by default ---
		TeclasDePrueba = Config.Bind("Testing", "TestKeys", defaultValue: false, "Enable the test keys below (host only). Leave off for real games.");
		TeclaPosesion = Config.Bind("Testing", "PossessKey", Key.F8, "Test key: become / stop being the monster.");
		TeclaCambiarTipo = Config.Bind("Testing", "NextMonsterKey", Key.F9, "Test key: switch TestMonster to the next type.");
		TeclaMatar = Config.Bind("Testing", "KillKey", Key.F10, "Test key: kill the monster (to test respawning).");
		TeclaRuido = Config.Bind("Testing", "NoiseKey", Key.F11, "Test key: emit a noise in front of the camera (to test the Huntsman's hearing).");
		MonstruoDePrueba = Config.Bind("Testing", "TestMonster", TipoMonstruo.Runner, "Monster used by the test key. Internal names: Runner = Reaper, Tumbler = Chef, Hunter = Huntsman.");
		PilotoForzado = Config.Bind("Testing", "ForcePilot", ForzarPiloto.None, "Skip the draw and fix who is the monster: None (normal draw), Host or FirstClient.");
		ReutilizarExistente = Config.Bind("Testing", "ReuseExistingEnemy", defaultValue: false, "Possess the closest enemy of that type already in the level instead of spawning a new one.");
		EvitarDerrotaEnSolitario = Config.Bind("Testing", "NoDefeatWhenSolo", defaultValue: true, "Playing alone, do not fail the level when you become the monster.");
		Mensajes.Registrar();
		_harmony.PatchAll();
		Log.LogInfo($"{Nombre} v{Version} cargado. Por Tommy777 y Dragonico.");
	}

	/// <summary>
	/// PlayerMonster y el antiguo JugadorMonstruo son el mismo mod con distinto identificador, así
	/// que BepInEx carga los dos si ambos están instalados. Juntos, cada uno sortea y crea SU
	/// monstruo y dibuja SU interfaz: salen dos monstruos, textos superpuestos y elecciones que no
	/// se respetan. Si detectamos la versión antigua, esta se desactiva por completo y lo avisa.
	/// Se comprueba en el primer Update, cuando BepInEx ya ha cargado todos los plugins.
	/// </summary>
	private void ComprobarConflicto()
	{
		_conflictoComprobado = true;
		if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(GuidAntiguo))
		{
			return;
		}
		Conflicto = true;
		_harmony.UnpatchSelf();
		_avisoConflictoHasta = Time.unscaledTime + 60f;
		Log.LogError("CONFLICTO: también está instalado el mod antiguo 'JugadorMonstruo'. PlayerMonster se ha desactivado. "
			+ "Quita o desactiva 'JugadorMonstruo' en tu gestor de mods y reinicia el juego. / "
			+ "CONFLICT: the old 'JugadorMonstruo' mod is also installed. PlayerMonster has disabled itself. "
			+ "Remove or disable 'JugadorMonstruo' in your mod manager and restart the game.");
	}

	private void Update()
	{
		if (!_conflictoComprobado)
		{
			ComprobarConflicto();
		}
		if (Conflicto)
		{
			if (Time.unscaledTime < _avisoConflictoHasta)
			{
				Aviso.Mostrar(Idioma.T("PLAYERMONSTER DESACTIVADO:\nquita el mod antiguo JugadorMonstruo", "PLAYERMONSTER DISABLED:\nremove the old JugadorMonstruo mod"), 54f, Color.yellow);
			}
			Aviso.Tick();
			return;
		}
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
		Sentidos.Mantener();
		Mira.Mantener();
	}
}
