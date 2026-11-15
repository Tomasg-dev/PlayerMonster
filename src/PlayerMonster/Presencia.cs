using UnityEngine;

namespace JugadorMonstruo;

/// <summary>
/// Obliga al monstruo a moverse: no puede quedarse a vivir en la sala donde están los jugadores.
///
/// Una barra de "presencia" (0..1) se llena mientras el monstruo está junto a algún humano
/// (misma sala o muy cerca) y se vacía, más deprisa, cuando se aleja. Al llenarse hay RETIRADA:
/// durante unos segundos el jugador pierde el control y corre la IA original del enemigo con la
/// orden de marcharse, que es algo que el juego ya hace con sus enemigos (EnemyParent.forceLeave:
/// la IA pasa a su estado "Leave" y camina sola hasta un punto lejano).
///
/// Todo esto corre solo en el anfitrión, que es quien mueve al enemigo.
/// </summary>
internal static class Presencia
{
    /// <summary>Nivel de la barra, de 0 (vacía) a 1 (llena = retirada).</summary>
    public static float Nivel { get; private set; }

    public static bool EnRetirada => Time.time < _retiradaHasta;

    private static float _retiradaHasta;
    private static float _siguienteOrden;
    private static float _siguienteRevision;
    private static bool _cerca;

    public static void Reiniciar()
    {
        Nivel = 0f;
        _retiradaHasta = 0f;
    }

    /// <summary>Cada frame en el anfitrión mientras el monstruo está vivo.</summary>
    public static void Tick(Enemy enemigo, TipoMonstruo tipo)
    {
        if (!Plugin.UsarPresencia.Value)
        {
            Reiniciar();
            return;
        }

        if (EnRetirada)
        {
            // La orden de marcharse se "gasta" cuando la IA la lee, así que la repetimos: si llega
            // a su destino y vuelve a estar ociosa, se marcha otra vez en lugar de ponerse a cazar.
            if (Time.time >= _siguienteOrden)
            {
                _siguienteOrden = Time.time + 1f;
                enemigo.EnemyParent.forceLeave = true;
            }
            Nivel = Mathf.MoveTowards(Nivel, 0f, Time.deltaTime / Mathf.Max(1f, Plugin.PresenciaRetirada.Value));
            return;
        }

        // Comprobar la cercanía cuatro veces por segundo es de sobra y evita trabajo cada frame.
        if (Time.time >= _siguienteRevision)
        {
            _siguienteRevision = Time.time + 0.25f;
            _cerca = HayHumanoCerca(enemigo);
        }

        float llenado = Mathf.Max(1f, Plugin.PresenciaSegundos.Value);
        Nivel = _cerca
            ? Mathf.MoveTowards(Nivel, 1f, Time.deltaTime / llenado)
            : Mathf.MoveTowards(Nivel, 0f, Time.deltaTime / (llenado * 0.5f));

        // Barra llena: retirada, pero solo si el enemigo no está a mitad de un ataque (ver PonerEnReposo).
        // Si lo está, la barra se queda llena y se vuelve a intentar en el siguiente frame.
        if (Nivel >= 1f && PonerEnReposo(enemigo, tipo))
        {
            _retiradaHasta = Time.time + Mathf.Max(1f, Plugin.PresenciaRetirada.Value);
            _siguienteOrden = 0f;
            Plugin.Log.LogInfo("Presencia: demasiado tiempo junto a los jugadores, retirada forzada.");
        }
    }

    /// <summary>
    /// Deja el "cerebro" del enemigo en reposo (Idle) antes de devolverle el control a su IA.
    /// Hace falta porque mientras lo pilota un jugador no tiene objetivo, y varios estados de la IA
    /// original (perseguir, avisar, atacar) dan por hecho que lo hay. Desde Idle, la IA lee la orden
    /// de marcharse y pasa a su estado Leave, que no necesita objetivo.
    /// Devuelve false si está en plena secuencia de ataque o aturdido: hay que esperar a que termine.
    /// </summary>
    private static bool PonerEnReposo(Enemy enemigo, TipoMonstruo tipo)
    {
        switch (tipo)
        {
            case TipoMonstruo.Animal:
                enemigo.GetComponent<EnemyAnimal>().UpdateState(EnemyAnimal.State.Idle);
                return true;

            case TipoMonstruo.Runner:
            {
                EnemyRunner c = enemigo.GetComponent<EnemyRunner>();
                if (c.currentState is EnemyRunner.State.Stun or EnemyRunner.State.Despawn)
                {
                    return false;
                }
                c.UpdateState(EnemyRunner.State.Idle);
                return true;
            }

            case TipoMonstruo.Tumbler:
            {
                EnemyTumbler c = enemigo.GetComponent<EnemyTumbler>();
                if (c.currentState is not (EnemyTumbler.State.Idle or EnemyTumbler.State.Roam or EnemyTumbler.State.MoveToPlayer))
                {
                    return false;
                }
                c.UpdateState(EnemyTumbler.State.Idle);
                return true;
            }

            case TipoMonstruo.Bowtie:
            {
                EnemyBowtie c = enemigo.GetComponent<EnemyBowtie>();
                if (c.currentState is not (EnemyBowtie.State.Idle or EnemyBowtie.State.Roam or EnemyBowtie.State.Leave))
                {
                    return false;
                }
                c.UpdateState(EnemyBowtie.State.Idle);
                return true;
            }

            case TipoMonstruo.Hunter:
            {
                EnemyHunter c = enemigo.GetComponent<EnemyHunter>();
                if (c.currentState is not (EnemyHunter.State.Idle or EnemyHunter.State.Roam))
                {
                    return false;
                }
                c.UpdateState(EnemyHunter.State.Idle);
                return true;
            }

            case TipoMonstruo.Tick:
            {
                EnemyTick c = enemigo.GetComponent<EnemyTick>();
                if (c.currentState is not (EnemyTick.State.Idle or EnemyTick.State.Roam))
                {
                    return false;
                }
                c.UpdateState(EnemyTick.State.Idle);
                return true;
            }

            default:
                return false;
        }
    }

    /// <summary>¿Hay algún humano vivo en la misma sala que el monstruo, o a pocos metros?</summary>
    private static bool HayHumanoCerca(Enemy enemigo)
    {
        Vector3 yo = enemigo.CenterTransform ? enemigo.CenterTransform.position : enemigo.transform.position;
        float radio = Plugin.PresenciaRadio.Value;

        foreach (PlayerAvatar jugador in GameDirector.instance.PlayerList)
        {
            // El piloto no cuenta: su robot está "desactivado" igual que un jugador muerto.
            if (!jugador || jugador.isDisabled)
            {
                continue;
            }
            if (Vector3.Distance(yo, jugador.transform.position) <= radio)
            {
                return true;
            }
            // Misma sala: el juego mantiene, para enemigos y jugadores, la lista de salas en las que están.
            if (jugador.RoomVolumeCheck != null)
            {
                foreach (RoomVolume sala in jugador.RoomVolumeCheck.CurrentRooms)
                {
                    if (sala && enemigo.EnemyParent.currentRooms.Contains(sala))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
}
