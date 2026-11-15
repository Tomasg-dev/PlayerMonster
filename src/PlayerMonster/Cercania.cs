using UnityEngine;

namespace JugadorMonstruo;

/// <summary>
/// Aviso de cercanía para los HUMANOS: saben que el monstruo anda cerca, pero no por dónde.
///
/// Corre en cada cliente que tenga el mod, sin ayuda del anfitrión: cada uno mide la distancia
/// entre su robot y el enemigo que pilota el monstruo (se sabe cuál es por las propiedades de red
/// de RedMonstruo). Atraviesa paredes a propósito: solo cuenta la distancia.
///
///   - Dentro del radio lejano: latido de corazón, más rápido y fuerte cuanto más cerca.
///   - Dentro del radio cercano: además, un borde rojo que late en la pantalla.
///
/// El latido no es un archivo de sonido: lo generamos con una fórmula (dos golpes graves que se
/// apagan rápido) para no tener que distribuir audio con el mod.
/// </summary>
internal static class Cercania
{
    private static readonly Color Rojo = new(0.6f, 0f, 0f);

    private static AudioSource? _fuente;
    private static AudioClip? _latido;
    private static float _siguienteLatido;
    private static float _pulso; // 1 justo al latir, baja a 0: da el "latido" visual

    /// <summary>Cada frame desde el plugin.</summary>
    public static void Tick()
    {
        _pulso = Mathf.MoveTowards(_pulso, 0f, Time.deltaTime * 2.5f);

        PlayerAvatar yo = PlayerAvatar.instance;
        if (!Plugin.AvisoCercania.Value || !SemiFunc.RunIsLevel() || !yo || yo.isDisabled || Piloto.Activo)
        {
            return; // muertos y el propio monstruo no reciben aviso
        }

        Enemy? monstruo = RedMonstruo.MonstruoEnElNivel();
        if (monstruo == null)
        {
            return;
        }

        Vector3 suPosicion = monstruo.CenterTransform ? monstruo.CenterTransform.position : monstruo.transform.position;
        float distancia = Vector3.Distance(yo.transform.position, suPosicion);
        float lejos = Plugin.CercaniaLejos.Value;
        float cerca = Plugin.CercaniaCerca.Value;
        if (distancia > lejos)
        {
            return;
        }

        // 0 en el borde del radio lejano, 1 cuando lo tienes encima.
        float intensidad = Mathf.InverseLerp(lejos, 3f, distancia);

        if (Time.time >= _siguienteLatido)
        {
            _siguienteLatido = Time.time + Mathf.Lerp(1.3f, 0.45f, intensidad);
            _pulso = 1f;
            Latir(Mathf.Lerp(0.25f, 1f, intensidad) * Plugin.CercaniaVolumen.Value);
        }

        if (distancia <= cerca && PostProcessing.Instance)
        {
            float fuerza = Mathf.Lerp(0.2f, 0.45f, intensidad) + 0.2f * _pulso;
            PostProcessing.Instance.VignetteOverride(Rojo, fuerza, 1f, 8f, 3f, 0.2f, Plugin.Instancia.gameObject);
        }
    }

    private static void Latir(float volumen)
    {
        if (_fuente == null)
        {
            var objeto = new GameObject("PlayerMonster Latido");
            Object.DontDestroyOnLoad(objeto);
            objeto.hideFlags = HideFlags.HideAndDontSave;
            _fuente = objeto.AddComponent<AudioSource>();
            _fuente.spatialBlend = 0f; // sonido "en la cabeza", no en un punto del mundo
            _fuente.playOnAwake = false;
            _latido = CrearLatido();
        }
        _fuente.PlayOneShot(_latido, Mathf.Clamp01(volumen));
    }

    /// <summary>Medio segundo de audio: "lub-dub". Cada golpe es una onda grave que se apaga deprisa.</summary>
    private static AudioClip CrearLatido()
    {
        const int frecuencia = 44100;
        int total = frecuencia / 2;
        var muestras = new float[total];
        for (int i = 0; i < total; i++)
        {
            float t = i / (float)frecuencia;
            muestras[i] = Golpe(t, 1f) + Golpe(t - 0.17f, 0.7f);
        }
        AudioClip clip = AudioClip.Create("PlayerMonster latido", total, 1, frecuencia, false);
        clip.SetData(muestras, 0);
        return clip;
    }

    private static float Golpe(float t, float fuerza)
    {
        if (t < 0f)
        {
            return 0f;
        }
        float apagado = Mathf.Exp(-t * 28f);
        // Grave (60 Hz) más un armónico (120 Hz) para que también se oiga en altavoces pequeños.
        float onda = Mathf.Sin(2f * Mathf.PI * 60f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 120f * t);
        return onda * apagado * fuerza * 0.6f;
    }
}
