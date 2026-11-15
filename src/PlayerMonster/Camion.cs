using HarmonyLib;
using UnityEngine;

namespace JugadorMonstruo;

/// <summary>
/// Regla del camión: dentro del camión el monstruo no puede hacerte daño... salvo que acampes.
///
/// Cada jugador lleva su propio contador de "tiempo acampado": sube mientras está dentro del camión
/// con un monstruo vivo en el nivel y baja (el doble de rápido) cuando sale. Mientras el contador
/// esté por debajo del límite, el daño del monstruo se anula. Pasado el límite, el camión deja de
/// protegerle hasta que salga un rato.
///
/// Se hace en el cliente de la víctima porque es ahí donde el juego aplica el daño de los enemigos
/// (ver MAPA_ENEMIGOS.md, sección 4). Por eso solo protege a quien tenga el mod instalado.
/// </summary>
internal static class Camion
{
    private static float _acampado;
    private static int _avatar;
    private static bool _avisado;

    /// <summary>¿Está el jugador local protegido ahora mismo del daño del monstruo?</summary>
    public static bool Protegido
    {
        get
        {
            PlayerAvatar yo = PlayerAvatar.instance;
            return Plugin.ReglaCamion.Value && yo && yo.RoomVolumeCheck && yo.RoomVolumeCheck.inTruck
                && _acampado < Plugin.CamionSegundos.Value;
        }
    }

    /// <summary>Cada frame desde el plugin.</summary>
    public static void Tick()
    {
        PlayerAvatar yo = PlayerAvatar.instance;
        if (!yo || !SemiFunc.RunIsLevel())
        {
            _acampado = 0f;
            return;
        }
        if (yo.GetInstanceID() != _avatar)
        {
            // Avatar nuevo = nivel nuevo: el contador empieza de cero.
            _avatar = yo.GetInstanceID();
            _acampado = 0f;
            _avisado = false;
        }

        // Solo cuenta mientras hay un monstruo vivo: al empezar el nivel todos están en el camión
        // esperando y eso no es acampar.
        bool dentro = !yo.isDisabled && yo.RoomVolumeCheck && yo.RoomVolumeCheck.inTruck;
        if (dentro && RedMonstruo.MonstruoEnElNivel() != null)
        {
            _acampado += Time.deltaTime;
        }
        else
        {
            _acampado = Mathf.Max(0f, _acampado - Time.deltaTime * 2f);
        }

        float limite = Plugin.CamionSegundos.Value;
        if (Plugin.ReglaCamion.Value && dentro && _acampado >= limite && !_avisado)
        {
            _avisado = true;
            SemiFunc.UIFocusText(Idioma.T("El camión ya no te protege", "The truck no longer protects you"), Color.red, Color.white, 4f);
        }
        if (_acampado < limite * 0.5f)
        {
            _avisado = false;
        }
    }
}

/// <summary>
/// PlayerHealth.Hurt es por donde pasa todo el daño que recibe un jugador, y se ejecuta en su propio
/// cliente. Si el daño viene del enemigo que pilota el monstruo:
///   - dentro del camión y sin acampar: se anula (devolver false cancela el método original);
///   - si no: se deja pasar y se avisa al anfitrión de que el golpe ha acertado (pausa de ataque).
/// </summary>
[HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
internal static class DanoDelMonstruoPatch
{
    private static bool Prefix(PlayerHealth __instance, int damage, int enemyIndex)
    {
        if (enemyIndex < 0 || damage <= 0 || !__instance.playerAvatar || !__instance.playerAvatar.isLocal)
        {
            return true;
        }

        Enemy? monstruo = RedMonstruo.MonstruoEnElNivel();
        if (monstruo == null || SemiFunc.EnemyGetFromIndex(enemyIndex) != monstruo)
        {
            return true; // daño de otro enemigo: no es asunto nuestro
        }

        if (Camion.Protegido)
        {
            return false;
        }

        Mensajes.EnviarGolpe();
        return true;
    }
}
