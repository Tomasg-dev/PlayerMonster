namespace JugadorMonstruo;

internal static class CuerpoJugador
{
	private static bool _oculto;

	public static void Ocultar()
	{
		PlayerAvatar instance = PlayerAvatar.instance;
		if (!_oculto && !(instance == null) && (bool)PlayerController.instance)
		{
			instance.physGrabber.ReleaseObject(-1);
			instance.SetDisabled();
			instance.playerAvatarVisuals.gameObject.SetActive(value: false);
			instance.gameObject.SetActive(value: false);
			PlayerController.instance.col.enabled = false;
			_oculto = true;
			RedMonstruo.Anunciar(soyMonstruo: true);
		}
	}

	public static void Tick()
	{
		if (_oculto)
		{
			PlayerController.instance.Kinematic(0.2f);
			PlayerController.instance.InputDisable(0.1f);
			SemiFunc.UIHideHealth();
			SemiFunc.UIHideOvercharge();
			SemiFunc.UIHideEnergy();
			SemiFunc.UIHideInventory();
			SemiFunc.UIHideAim();
		}
	}

	public static void Restaurar()
	{
		PlayerAvatar instance = PlayerAvatar.instance;
		if (!_oculto || instance == null)
		{
			if (_oculto)
			{
				RedMonstruo.Anunciar(soyMonstruo: false);
			}
			_oculto = false;
			return;
		}
		instance.isDisabled = false;
		instance.gameObject.SetActive(value: true);
		instance.playerAvatarVisuals.gameObject.SetActive(value: true);
		if ((bool)PlayerController.instance)
		{
			PlayerController.instance.col.enabled = true;
		}
		_oculto = false;
		RedMonstruo.Anunciar(soyMonstruo: false);
	}

	public static void Olvidar()
	{
		_oculto = false;
	}
}
