using HarmonyLib;

namespace JugadorMonstruo;

[HarmonyPatch(typeof(RunManager), "ResetProgress")]
internal static class PartidaNuevaPatch
{
	private static void Prefix()
	{
		Rotacion.Reiniciar("partida nueva");
	}
}
