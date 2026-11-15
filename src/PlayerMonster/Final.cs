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
