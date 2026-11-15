using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace JugadorMonstruo;

/// <summary>
/// Final de la ronda cuando gana el monstruo.
///
/// Si mueren todos los humanos, el propio juego da el nivel por perdido y lleva a todos a la arena
/// (la "batalla de los perdedores"). Eso no lo tocamos: solo añadimos, para todos los jugadores con
/// el mod, un aviso de que ha ganado el monstruo y quién era.
///
/// El aviso NO se muestra en el momento: quedaría encima de la pantalla de carga y no se leería.
/// Se guarda como pendiente y aparece cuando el nivel siguiente (la arena) ya está cargado y jugable.
/// </summary>
internal static class Final
{
    private const float Duracion = 8f;
    private const float EsperaMaxima = 60f; // si en un minuto no carga nada, se descarta

    private static bool _pendiente;
    private static int _nivelAlAnunciar;
    private static float _caducaEn;
    private static float _hasta;
    private static string _nombre = "";
    private static bool _eraYo;

    private static float _todosMuertosDesde = -1f;

    /// <summary>
    /// Cada frame en el anfitrión. Cubre un hueco del juego: cuando mueren todos, RunManager solo
    /// pasa a la arena si el ANFITRIÓN tiene abierta su cámara de espectador, es decir, si él mismo
    /// ha muerto de verdad. Cuando el anfitrión es el monstruo no ha "muerto" (solo tiene el robot
    /// oculto), esa cámara no existe y la partida se quedaba parada con todos los humanos muertos.
    /// En ese caso damos nosotros la orden, con la misma función que usa el juego.
    /// </summary>
    public static void VigilarDerrota()
    {
        if (!Mensajes.SoyAutoridad || !SemiFunc.IsMultiplayer() || !Autoridad.Activa || !SemiFunc.RunIsLevel()
            || !RunManager.instance || RunManager.instance.restarting || RunManager.instance.allPlayersDeadCheckDisabled
            || !GameDirector.instance || GameDirector.instance.currentState != GameDirector.gameState.Main
            || GameDirector.instance.PlayerList.Count == 0)
        {
            _todosMuertosDesde = -1f;
            return;
        }

        // Mismo criterio que el juego: todos los jugadores "desactivados". El robot del monstruo
        // también lo está, así que esto equivale a "no queda ningún humano vivo".
        foreach (PlayerAvatar jugador in GameDirector.instance.PlayerList)
        {
            if (jugador && !jugador.isDisabled)
            {
                _todosMuertosDesde = -1f;
                return;
            }
        }

        // Si el anfitrión sí tiene cámara de espectador, el juego se encarga solo.
        if (SpectateCamera.instance)
        {
            _todosMuertosDesde = -1f;
            return;
        }

        if (_todosMuertosDesde < 0f)
        {
            _todosMuertosDesde = Time.time;
        }
        // Unos segundos de margen para que el último jugador vea su propia muerte, como en el juego.
        if (Time.time - _todosMuertosDesde >= 5f)
        {
            _todosMuertosDesde = -1f;
            Plugin.Log.LogInfo("Final: no queda ningún humano vivo y el anfitrión es el monstruo; se pasa a la arena.");
            RunManager.instance.ChangeLevel(false, true);
        }
    }

    /// <summary>El anfitrión lo llama cuando el nivel se pierde habiendo un jugador-monstruo.</summary>
    public static void Anunciar()
    {
        string nombre = "";
        int actor = 0;
        if (SemiFunc.IsMultiplayer() && PhotonNetwork.CurrentRoom != null)
        {
            actor = Autoridad.PilotoActor;
            Player? piloto = PhotonNetwork.CurrentRoom.GetPlayer(actor);
            nombre = piloto != null ? piloto.NickName : "";
        }
        Plugin.Log.LogInfo($"Final: el equipo ha caído, gana el monstruo ('{nombre}').");
        Mensajes.EnviarVictoria(actor, nombre);
    }

    /// <summary>En cada cliente, al llegar el aviso del anfitrión.</summary>
    public static void Mostrar(int actorDelMonstruo, string nombre)
    {
        _nombre = nombre ?? "";
        _eraYo = !SemiFunc.IsMultiplayer()
            || (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.ActorNumber == actorDelMonstruo);
        _pendiente = true;
        _nivelAlAnunciar = LevelGenerator.Instance ? LevelGenerator.Instance.GetInstanceID() : 0;
        _caducaEn = Time.unscaledTime + EsperaMaxima;
    }

    /// <summary>
    /// ¿Ha terminado ya la carga del nivel siguiente? El juego recarga la escena en cada nivel, así
    /// que basta con ver un LevelGenerator distinto del que había, ya generado y con la partida en marcha.
    /// </summary>
    private static bool SiguienteNivelListo()
    {
        LevelGenerator generador = LevelGenerator.Instance;
        return generador && generador.GetInstanceID() != _nivelAlAnunciar && generador.Generated
            && GameDirector.instance && GameDirector.instance.currentState == GameDirector.gameState.Main;
    }

    /// <summary>Cada frame desde el plugin.</summary>
    public static void Tick()
    {
        if (_pendiente)
        {
            if (SiguienteNivelListo())
            {
                _pendiente = false;
                _hasta = Time.unscaledTime + Duracion;
            }
            else if (Time.unscaledTime >= _caducaEn)
            {
                _pendiente = false;
            }
        }
        if (Time.unscaledTime >= _hasta)
        {
            return;
        }

        // Para el monstruo es una victoria; para el resto, la noticia de quién los cazó.
        string titulo = _eraYo
            ? Idioma.T("HAS GANADO", "YOU WIN")
            : Idioma.T("EL MONSTRUO GANA", "THE MONSTER WINS");
        string detalle;
        if (_eraYo)
        {
            detalle = Idioma.T("Nadie cumplió la cuota", "Nobody met the quota");
        }
        else if (_nombre.Length > 0)
        {
            detalle = _nombre + Idioma.T(" era el monstruo", " was the monster");
        }
        else
        {
            detalle = Idioma.T("Nadie cumplió la cuota", "Nobody met the quota");
        }

        // <noparse> evita que un nombre de jugador con etiquetas raras rompa el formato del texto.
        Aviso.Mostrar($"{titulo}\n<size=40%><color=#FFFFFF><noparse>{detalle}</noparse></color></size>", 120f, Color.red);
    }
}

/// <summary>
/// RunManager.ChangeLevel es por donde el juego cambia de nivel; con _levelFailed = true significa
/// que el equipo ha perdido. Solo actúa el anfitrión.
/// </summary>
[HarmonyPatch(typeof(RunManager), "ChangeLevel")]
internal static class NivelPerdidoPatch
{
    private static void Prefix(bool _levelFailed)
    {
        if (!_levelFailed || !Mensajes.SoyAutoridad)
        {
            return;
        }
        // Solo hay "victoria del monstruo" si en este nivel había jugador-monstruo.
        if (Autoridad.Activa && SemiFunc.RunIsLevel())
        {
            Final.Anunciar();
        }
        Rotacion.Reiniciar("el equipo perdió el nivel");
    }
}
