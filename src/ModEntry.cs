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
    private static readonly Dictionary<NCreature, RegentSatelliteOrbit> ActiveOrbits = new();

    private const string RegentId = "REGENT";

    public static void UpdateForRoom(NCombatRoom room)
    {
        if (room == null || !GodotObject.IsInstanceValid(room)) return;

        List<NCreature> creatures = room.CreatureNodes.ToList();
        HashSet<NCreature> current = new(creatures);

        // 清理不再存在的生物
        foreach (NCreature stale in ActiveOrbits.Keys.Where(k => !current.Contains(k)).ToList())
        {
            RemoveOrbit(stale);
        }

        // 更新现有生物
        foreach (NCreature creature in creatures)
        {
            TryUpdateCreatureOrbit(creature, room);
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

    private static void TryUpdateCreatureOrbit(NCreature creature, NCombatRoom room)
    {
        if (!GodotObject.IsInstanceValid(creature) || !TryGetRegentStars(creature, out int stars))
        {
            RemoveOrbit(creature);
            return;
        }

        if (!ActiveOrbits.TryGetValue(creature, out RegentSatelliteOrbit? orbit) || !GodotObject.IsInstanceValid(orbit))
        {
            orbit = new RegentSatelliteOrbit();

            Node parent = creature.GetParent();
            if (parent == null) return;
            
            parent.AddChild(orbit);
            
            int creatureIndex = creature.GetIndex();
            parent.MoveChild(orbit, creatureIndex);
            
            orbit.Initialize(creature, creature.Hitbox);
            ActiveOrbits[creature] = orbit;
        }
        
        if (GodotObject.IsInstanceValid(orbit))
        {
            orbit.UpdateFromStars(stars);
        }
    }

    private static bool TryGetRegentStars(NCreature creatureNode, out int stars)
    {
        stars = 0;
        if (!creatureNode.Entity.IsPlayer || !LocalContext.IsMe(creatureNode.Entity))
        {
            return false;
        }
        
        var player = creatureNode.Entity.Player;
        if (player?.Character?.Id?.Entry != RegentId)
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

        SatelliteOrbitController controller = new();
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