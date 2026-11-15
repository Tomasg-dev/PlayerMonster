using UnityEngine;

namespace JugadorMonstruo;

/// <summary>Idioma de los textos que el mod muestra en pantalla.</summary>
public enum IdiomaMod
{
    /// <summary>Español si el sistema está en español; inglés en cualquier otro caso.</summary>
    Auto,
    Espanol,
    English,
}

/// <summary>
/// Traducción mínima: cada texto visible se escribe en los dos idiomas en el mismo sitio donde se
/// usa, con Idioma.T("español", "english"). Para dos idiomas y una veintena de frases es más
/// claro que un archivo de traducciones aparte.
/// </summary>
internal static class Idioma
{
    public static bool EnEspanol => Plugin.IdiomaTextos.Value switch
    {
        IdiomaMod.Espanol => true,
        IdiomaMod.English => false,
        _ => Application.systemLanguage == SystemLanguage.Spanish,
    };

    public static string T(string es, string en) => EnEspanol ? es : en;
}
