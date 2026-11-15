using HarmonyLib;

namespace JugadorMonstruo;

[HarmonyPatch(typeof(RunManager), "ChangeLevel")]
internal static class NivelPerdidoPatch
{
	private static void Prefix(bool _levelFailed)
	{
		if (_levelFailed && Mensajes.SoyAutoridad)
		{
			Rotacion.Reiniciar("el equipo perdió el nivel");
		}
	}
}
