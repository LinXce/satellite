using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Satellite.src;

public static class OrbitState
{
	private static readonly Dictionary<NCreature, RegentSatelliteOrbit> ActiveOrbits = new Dictionary<NCreature, RegentSatelliteOrbit>();

	private const string RegentId = "REGENT";

	public static void UpdateForRoom(NCombatRoom room)
	{
		List<NCreature> creatures = room.CreatureNodes.ToList();
		HashSet<NCreature> current = new HashSet<NCreature>(creatures);
		foreach (NCreature stale in ActiveOrbits.Keys.Where(k => !current.Contains(k)).ToList())
		{
			RemoveOrbit(stale);
		}

		foreach (NCreature creature in creatures)
		{
			TryUpdateCreatureOrbit(creature);
		}
	}

	public static void ClearAllOrbits()
	{
		foreach (RegentSatelliteOrbit orbit in ActiveOrbits.Values)
		{
			if (GodotObject.IsInstanceValid(orbit))
			{
				orbit.QueueFree();
			}
		}
		ActiveOrbits.Clear();
	}

	private static void TryUpdateCreatureOrbit(NCreature creature)
	{
		if (!GodotObject.IsInstanceValid(creature) || !TryGetRegentStars(creature, out int stars))
		{
			RemoveOrbit(creature);
			return;
		}

		if (!ActiveOrbits.TryGetValue(creature, out RegentSatelliteOrbit? orbit) || !GodotObject.IsInstanceValid(orbit))
		{
			orbit = new RegentSatelliteOrbit();
			creature.AddChild(orbit);
			orbit.Initialize(creature, creature.Hitbox);
			ActiveOrbits[creature] = orbit;
		}
		orbit.UpdateFromStars(stars);
	}

	private static bool TryGetRegentStars(NCreature creatureNode, out int stars)
	{
		stars = 0;
		if (!creatureNode.Entity.IsPlayer || !LocalContext.IsMe(creatureNode.Entity))
		{
			return false;
		}
		var player = creatureNode.Entity.Player;
		if (player == null)
		{
			return false;
		}
		if (player.Character?.Id?.Entry != RegentId)
		{
			return false;
		}
		if (player.PlayerCombatState == null)
		{
			return false;
		}
		stars = player.PlayerCombatState.Stars;
		return true;
	}

	private static void RemoveOrbit(NCreature creature)
	{
		if (ActiveOrbits.Remove(creature, out RegentSatelliteOrbit? orbit) && GodotObject.IsInstanceValid(orbit))
		{
			orbit.QueueFree();
		}
	}
}

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
public static class SatelliteCombatRoomReadyPatch
{
	[HarmonyPostfix]
	public static void AfterReady(NCombatRoom __instance)
	{
		if (__instance.GetNodeOrNull("SatelliteOrbitController") != null)
		{
			return;
		}

		SatelliteOrbitController controller = new SatelliteOrbitController();
		__instance.AddChild(controller);
		controller.Initialize(__instance);
	}
}

[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
public static class SatelliteCombatRoomExitPatch
{
	[HarmonyPostfix]
	public static void AfterExitTree()
	{
		OrbitState.ClearAllOrbits();
	}
}

public partial class SatelliteOrbitController : Node
{
	private NCombatRoom? _room;

	public void Initialize(NCombatRoom room)
	{
		_room = room;
		Name = "SatelliteOrbitController";
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Process(double delta)
	{
		if (_room == null || !GodotObject.IsInstanceValid(_room))
		{
			QueueFree();
			return;
		}

		OrbitState.UpdateForRoom(_room);
	}
}
