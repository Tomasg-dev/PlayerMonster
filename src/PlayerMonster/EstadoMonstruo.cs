using UnityEngine;

namespace JugadorMonstruo;

internal struct EstadoMonstruo
{
	public FaseMonstruo Fase;

	public TipoMonstruo Tipo;

	public int Serie;

	public int EnemigoViewId;

	public float Furia;

	public bool Agotado;

	public int Vida;

	public int VidaMaxima;

	public float SegundosParaReaparecer;

	public Vector3 UltimaPosicion;

	public float Progreso;

	public float[]? Opciones;

	/// <summary>Barra de presencia (0..1): cuánto lleva el monstruo junto a los jugadores.</summary>
	public float Presencia;

	/// <summary>true mientras dura la retirada forzada (el piloto no controla al enemigo).</summary>
	public bool Retirada;
}
