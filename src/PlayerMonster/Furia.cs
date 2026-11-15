using UnityEngine;

namespace JugadorMonstruo;

internal static class Furia
{
	private readonly struct Ajuste(bool continuo, float ataque, float recarga, float agotado)
	{
		public readonly bool Continuo = continuo;

		public readonly float SegundosDeAtaque = ataque;

		public readonly float SegundosDeRecarga = recarga;

		public readonly float SegundosAgotado = agotado;
	}

	private static float _agotadoHasta;

	public static float Nivel { get; private set; } = 1f;

	public static bool Agotado => Time.time < _agotadoHasta;

	public static bool PuedeAtacar => Nivel >= 1f;

	private static Ajuste Para(TipoMonstruo tipo)
	{
		return tipo switch
		{
			TipoMonstruo.Runner => new Ajuste(continuo: true, 3f, 5f, 2.5f), 
			TipoMonstruo.Animal => new Ajuste(continuo: true, 5f, 5f, 2f), 
			TipoMonstruo.Tumbler => new Ajuste(continuo: false, 0f, 4f, 0f), 
			TipoMonstruo.Bowtie => new Ajuste(continuo: false, 0f, 6f, 0f), 
			TipoMonstruo.Hunter => new Ajuste(continuo: false, 0f, 2f, 0f), 
			TipoMonstruo.Tick => new Ajuste(continuo: false, 0f, 30f, 0f), 
			_ => new Ajuste(continuo: true, 3f, 5f, 2.5f), 
		};
	}

	public static void Reiniciar()
	{
		Nivel = 1f;
		_agotadoHasta = 0f;
		_pausaHasta = 0f;
	}

	private static float _pausaHasta;

	/// <summary>
	/// Un golpe del monstruo ha hecho daño a un jugador (lo avisa el cliente de la víctima).
	/// Los monstruos de ataque continuo quedan un momento sin poder atacar, para que la víctima
	/// tenga ocasión de escapar en vez de recibir un golpe tras otro.
	/// </summary>
	public static void GolpeAcertado()
	{
		_pausaHasta = Time.time + Mathf.Max(0f, Plugin.PausaTrasGolpe.Value);
	}

	public static void FinDeSecuencia()
	{
		Nivel = 0f;
	}

	public static void Filtrar(ref EntradaMonstruo entrada, TipoMonstruo tipo)
	{
		if (!Plugin.UsarFuria.Value)
		{
			Nivel = 1f;
			return;
		}
		Ajuste ajuste = Para(tipo);
		float deltaTime = Time.deltaTime;
		if (ajuste.Continuo && Time.time < _pausaHasta)
		{
			entrada.Atacar = false; // pausa tras acertar; mientras tanto la barra recarga
		}
		if (!ajuste.Continuo)
		{
			Nivel = Mathf.MoveTowards(Nivel, 1f, deltaTime / ajuste.SegundosDeRecarga);
		}
		else if (Agotado)
		{
			entrada.Atacar = false;
			entrada.Correr = false;
		}
		else if (entrada.Atacar)
		{
			Nivel = Mathf.MoveTowards(Nivel, 0f, deltaTime / ajuste.SegundosDeAtaque);
			if (Nivel <= 0f)
			{
				_agotadoHasta = Time.time + ajuste.SegundosAgotado;
				entrada.Atacar = false;
				entrada.Correr = false;
			}
		}
		else
		{
			Nivel = Mathf.MoveTowards(Nivel, 1f, deltaTime / ajuste.SegundosDeRecarga);
		}
	}
}
