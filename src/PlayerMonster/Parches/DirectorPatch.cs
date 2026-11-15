using HarmonyLib;

namespace JugadorMonstruo.Parches;

[HarmonyPatch(typeof(EnemyDirector), "AmountSetup")]
internal static class DirectorPatch
{
	private static void Postfix(EnemyDirector __instance)
	{
		if (Plugin.SoloJugadorMonstruo.Value && ModoJuego.HayMonstruoEnEsteNivel())
		{
			Plugin.Log.LogInfo($"Modo: nivel sin enemigos propios (el juego iba a crear {__instance.totalAmount}).");
			__instance.totalAmount = 0;
		}
	}
}
