using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Satellite;

[ModInitializer("Init")]
public static class SatelliteMod
{
	private const string ModName = "SatelliteMod";

	public static void Init()
	{
		Harmony harmony = new Harmony(ModName);
		harmony.PatchAll();
		GD.Print("[Satellite] Mod initialized");
	}
}
