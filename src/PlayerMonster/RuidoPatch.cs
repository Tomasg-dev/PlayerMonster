using HarmonyLib;
using UnityEngine;

namespace JugadorMonstruo;

[HarmonyPatch(typeof(EnemyDirector), "SetInvestigate")]
internal static class RuidoPatch
{
	private static void Postfix(Vector3 position, float radius)
	{
		Autoridad.RuidoEnElNivel(position, radius);
	}
}
