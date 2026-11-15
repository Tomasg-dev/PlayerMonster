using UnityEngine;

namespace JugadorMonstruo;

internal struct EntradaMonstruo
{
	public Vector3 Direccion;

	public bool Correr;

	public bool Atacar;

	/// <summary>
	/// Hacia dónde mira la cámara del PILOTO (dirección 3D normalizada). El anfitrión la usa para
	/// apuntar disparos, saltos y gritos: antes usaba su propia cámara, y si el monstruo era un
	/// cliente apuntaba hacia donde miraba el anfitrión.
	/// </summary>
	public Vector3 Mirada;

	/// <summary>La mirada proyectada sobre el suelo (para girar el cuerpo), o "adelante" si no hay.</summary>
	public readonly Vector3 MiradaPlana
	{
		get
		{
			Vector3 plana = Vector3.ProjectOnPlane(Mirada, Vector3.up);
			return plana.sqrMagnitude > 0.001f ? plana.normalized : Vector3.forward;
		}
	}

	/// <summary>
	/// El punto del mundo que hay justo bajo el centro de la pantalla del piloto (donde está la
	/// mira). Lo usa el cazador para apuntar el arma a ese punto exacto.
	/// </summary>
	public Vector3 PuntoMira;

	private const float AlcanceMira = 40f;

	private static readonly RaycastHit[] Impactos = new RaycastHit[16];

	/// <param name="propio">El enemigo que pilota este jugador, para que el rayo de la mira lo ignore.</param>
	public static EntradaMonstruo LeerLocal(Enemy? propio = null)
	{
		EntradaMonstruo result = default;
		Camera main = Camera.main;
		if (main == null)
		{
			return result;
		}
		Vector2 vector = SemiFunc.InputMovement();
		Vector3 normalized = Vector3.ProjectOnPlane(main.transform.forward, Vector3.up).normalized;
		Vector3 vector2 = Vector3.Cross(Vector3.up, normalized);
		Vector3 vector3 = normalized * vector.y + vector2 * vector.x;
		result.Direccion = ((vector3.sqrMagnitude > 0.01f) ? vector3.normalized : Vector3.zero);
		result.Correr = SemiFunc.InputHold(InputKey.Sprint);
		result.Atacar = SemiFunc.InputHold(InputKey.Grab);
		result.Mirada = main.transform.forward;
		result.PuntoMira = CalcularPuntoMira(main, propio);
		return result;
	}

	/// <summary>
	/// Rayo desde la cámara hacia delante: devuelve el primer punto que toca (pared, objeto o
	/// jugador), saltándose el cuerpo del propio monstruo, que en tercera persona queda delante de
	/// la cámara. Si no toca nada, un punto lejano en esa dirección.
	/// </summary>
	private static Vector3 CalcularPuntoMira(Camera camara, Enemy? propio)
	{
		Vector3 origen = camara.transform.position;
		Vector3 direccion = camara.transform.forward;
		int mascara = (int)SemiFunc.LayerMaskGetVisionObstruct() | LayerMask.GetMask("Player", "PhysGrabObject");
		int cuantos = Physics.RaycastNonAlloc(origen, direccion, Impactos, AlcanceMira, mascara, QueryTriggerInteraction.Ignore);

		float mejor = AlcanceMira;
		EnemyParent? padrePropio = (propio != null && (bool)propio) ? propio.EnemyParent : null;
		for (int i = 0; i < cuantos; i++)
		{
			RaycastHit impacto = Impactos[i];
			if (padrePropio != null && impacto.collider.GetComponentInParent<EnemyParent>() == padrePropio)
			{
				continue;
			}
			if (impacto.distance < mejor)
			{
				mejor = impacto.distance;
			}
		}
		return origen + direccion * mejor;
	}
}
