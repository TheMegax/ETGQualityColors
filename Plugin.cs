using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using RadialGunSelect;
using UnityEngine;
using OpCodes = System.Reflection.Emit.OpCodes;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace Mod;

[BepInDependency(ETGModMainBehaviour.GUID)]
[BepInDependency("morphious86.etg.radialgunselect", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(GUID, NAME, VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string GUID = "megax.etg.qualitycolors";
    private const string NAME = "Quality Colors";
    private const string VERSION = "1.0.0";
    private const string TEXT_COLOR = "#00FFFF";

    private static ConfigEntry<Color> SQualityColor;
    private static ConfigEntry<Color> AQualityColor;
    private static ConfigEntry<Color> BQualityColor;
    private static ConfigEntry<Color> CQualityColor;
    private static ConfigEntry<Color> DQualityColor;
    private static ConfigEntry<Color> SpecialQualityColor;
    private static ConfigEntry<bool> UseColorsInWeaponWheelSelect;

    public void Awake()
    {
        SQualityColor = Config.Bind("Colors", "QualityS", new Color(0.60f, 0.60f, 0.60f));
        AQualityColor = Config.Bind("Colors", "QualityA", new Color(0.75f, 0.07f, 0.1f));
        BQualityColor = Config.Bind("Colors", "QualityB", new Color(0.29f, 0.72f, 0.23f));
        CQualityColor = Config.Bind("Colors", "QualityC", new Color(0.21f, 0.71f, 0.8f));
        DQualityColor = Config.Bind("Colors", "QualityD", new Color(0.67f, 0.44f, 0.11f));
        SpecialQualityColor = Config.Bind("Colors", "QualitySpecial",  new Color(0.5f, 0.2f, 1f));
        UseColorsInWeaponWheelSelect = Config.Bind("Mod Compatibility", "UseColorsInWeaponWheelSelect", true);
    }
    
    public void Start()
    {
        ETGModMainBehaviour.WaitForGameManagerStart(GMStart);
    }

    public void GMStart(GameManager g)
    {
        var harmony = new Harmony(GUID);
        harmony.PatchAll(typeof(Gun_OnExitRange_Patch));
        harmony.PatchAll(typeof(Gun_DropGun_Patch));
        harmony.PatchAll(typeof(PassiveItem_Start_Patch));
        harmony.PatchAll(typeof(PassiveItem_Drop_Patch));
        harmony.PatchAll(typeof(PlayerItem_Start_Patch));
        harmony.PatchAll(typeof(PlayerItem_OnExitRange_Patch));
        harmony.PatchAll(typeof(RewardPedestal_DetermineContents_Patch));
        harmony.PatchAll(typeof(RewardPedestal_OnExitRange_Patch));
        harmony.PatchAll(typeof(AmmonomiconPokedexEntry_UpdateSynergyHighlights_Patch));
        harmony.PatchAll(typeof(AmmonomiconPokedexEntry_LostFocus_Patch));
        harmony.PatchAll(typeof(AmmonomiconPageRenderer_DoRefreshData_Patch));

        if (UseColorsInWeaponWheelSelect.Value && Chainloader.PluginInfos.ContainsKey("morphious86.etg.radialgunselect"))
        {
            harmony.PatchAll(typeof(RadialSegment_SetHovered_Patch));
        }

        ETGModConsole.Log("<color=#cecece>Quality</color> " +
                          "<color=#BF121A>Colors</color> " +
                          $"<color=#4AB83B>v{VERSION}</color> " +
                          "<color=#36B5CC>started</color> " +
                          "<color=#AB701C>successfully!</color>");
        
    }

    public static void Log(string text, string color="#FFFFFF")
    {
        ETGModConsole.Log($"<color={color}>{text}</color>");
    }
    
    private static Color _outlineColor = Color.black;
    private static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");

    private static Color GetOutlineColor()
    {
        return _outlineColor;
    }

    private static void SetOutlineColor(PickupObject.ItemQuality quality)
    {
        _outlineColor = quality switch
        {
            PickupObject.ItemQuality.S => SQualityColor.Value,
            PickupObject.ItemQuality.A => AQualityColor.Value,
            PickupObject.ItemQuality.B => BQualityColor.Value,
            PickupObject.ItemQuality.C => CQualityColor.Value,
            PickupObject.ItemQuality.D => DQualityColor.Value,
            PickupObject.ItemQuality.COMMON => DQualityColor.Value,
            PickupObject.ItemQuality.SPECIAL => SpecialQualityColor.Value,
            _ => Color.black
        };
    }

    private static IEnumerable<CodeInstruction> ColorReplacer(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(
                false,
                new CodeMatch(
                    OpCodes.Call, AccessTools.PropertyGetter(typeof(Color),
                        nameof(Color.black))
                )
            ).SetInstructionAndAdvance(
                new CodeInstruction(
                    OpCodes.Call, AccessTools.Method(typeof(Plugin), nameof(GetOutlineColor))
                )
            ).MatchForward(
                true,
                new CodeMatch(OpCodes.Ldc_R4),
                new CodeMatch(OpCodes.Ldc_R4)
            ).SetOperandAndAdvance(0f).InstructionEnumeration();
    }
    
    // This is rather stupid, but despite what the docs say it seemingly only lets me do this one by one
    // I probably messed up somewhere yup
    [HarmonyPatch(typeof(Gun), nameof(Gun.OnExitRange))]
    public static class Gun_OnExitRange_Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ColorReplacer(instructions);
        }

        private static void Prefix(PickupObject __instance)
        {
            SetOutlineColor(__instance.quality);
        }
    }
    
    [HarmonyPatch(typeof(Gun), nameof(Gun.DropGun))]
    public static class Gun_DropGun_Patch
    {
        private static void Prefix(PickupObject __instance)
        {
            SetOutlineColor(__instance.quality);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ColorReplacer(instructions);
        }
    }
    
    [HarmonyPatch(typeof(PassiveItem), nameof(PassiveItem.Start))]
    public static class PassiveItem_Start_Patch
    {
        private static void Prefix(PickupObject __instance)
        {
            SetOutlineColor(__instance.quality);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ColorReplacer(instructions);
        }
    }
    
    [HarmonyPatch(typeof(PassiveItem), nameof(PassiveItem.Drop))]
    public static class PassiveItem_Drop_Patch
    {
        private static void Prefix(PickupObject __instance)
        {
            SetOutlineColor(__instance.quality);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ColorReplacer(instructions);
        }
    }
    
    [HarmonyPatch(typeof(PassiveItem), nameof(PassiveItem.OnExitRange))]
    public static class PassiveItem_OnExitRange_Patch
    {
        private static void Prefix(PickupObject __instance)
        {
            SetOutlineColor(__instance.quality);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ColorReplacer(instructions);
        }
    }
    
    [HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.Start))]
    public static class PlayerItem_Start_Patch
    {
        private static void Prefix(PickupObject __instance)
        {
            SetOutlineColor(__instance.quality);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ColorReplacer(instructions);
        }
    }
    
    [HarmonyPatch(typeof(PlayerItem), nameof(PlayerItem.OnExitRange))]
    public static class PlayerItem_OnExitRange_Patch
    {
        private static void Prefix(PickupObject __instance)
        {
            SetOutlineColor(__instance.quality);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ColorReplacer(instructions);
        }
    }
    
    [HarmonyPatch(typeof(RewardPedestal), nameof(RewardPedestal.DetermineContents))]
    public static class RewardPedestal_DetermineContents_Patch
    {
        private static void Postfix(RewardPedestal __instance)
        {
            SpriteOutlineManager.RemoveOutlineFromSprite(__instance.m_itemDisplaySprite);
            SetOutlineColor(__instance.contents.quality);
            SpriteOutlineManager.AddOutlineToSprite(__instance.m_itemDisplaySprite, _outlineColor, 0.1f, 0.05f);
        }
    }
    
    [HarmonyPatch(typeof(RewardPedestal), nameof(RewardPedestal.OnExitRange))]
    public static class RewardPedestal_OnExitRange_Patch
    {
        private static void Prefix(RewardPedestal __instance)
        {
            SetOutlineColor(__instance.contents.quality);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return ColorReplacer(instructions);
        }
    }
    
    [HarmonyPatch(typeof(AmmonomiconPokedexEntry), nameof(AmmonomiconPokedexEntry.UpdateSynergyHighlights))]
    public static class AmmonomiconPokedexEntry_UpdateSynergyHighlights_Patch
    {
        private static void Postfix(AmmonomiconPokedexEntry __instance)
        {
            var pokedexEntries = AmmonomiconController.Instance.BestInteractingLeftPageRenderer.GetPokedexEntries();
            
            foreach (var t in pokedexEntries.Where(t => 
                         t != __instance && SpriteOutlineManager.HasOutline(t.m_childSprite)))
            {
                var pickupObj = PickupObjectDatabase.Instance.Objects[t.pickupID];
                SpriteOutlineManager.RemoveOutlineFromSprite(t.m_childSprite, true);
                SetOutlineColor(pickupObj.quality);
                SpriteOutlineManager.AddScaledOutlineToSprite<tk2dClippedSprite>(t.m_childSprite, _outlineColor, 0.1f, 0f);
            }
        }
    }
    
    [HarmonyPatch(typeof(AmmonomiconPageRenderer), nameof(AmmonomiconPageRenderer.DoRefreshData))]
    public static class AmmonomiconPageRenderer_DoRefreshData_Patch
    {
        private static void Postfix()
        {
            if (AmmonomiconController.Instance.BestInteractingLeftPageRenderer == null)
            {
                return;
            }
            
            var pokedexEntries = AmmonomiconController.Instance.BestInteractingLeftPageRenderer.GetPokedexEntries();
            
            foreach (var t in pokedexEntries.Where(t => 
                         t.pickupID != -1))
            {
                var pickupObj = PickupObjectDatabase.Instance.Objects[t.pickupID];
                SpriteOutlineManager.RemoveOutlineFromSprite(t.m_childSprite, true);
                SetOutlineColor(pickupObj.quality);
                SpriteOutlineManager.AddScaledOutlineToSprite<tk2dClippedSprite>(t.m_childSprite, _outlineColor, 0.1f, 0f);
            }
        }
    }
    
    [HarmonyPatch(typeof(AmmonomiconPokedexEntry), nameof(AmmonomiconPokedexEntry.m_button_LostFocus))]
    public static class AmmonomiconPokedexEntry_LostFocus_Patch
    {
        private static void Postfix(AmmonomiconPokedexEntry __instance)
        {
            var pickupObj = PickupObjectDatabase.Instance.Objects[__instance.pickupID];
            SpriteOutlineManager.RemoveOutlineFromSprite(__instance.m_childSprite, true);
            SetOutlineColor(pickupObj.quality);
            SpriteOutlineManager.AddScaledOutlineToSprite<tk2dClippedSprite>(__instance.m_childSprite, _outlineColor, 0.1f, 0f);
        }
    }

    [HarmonyPatch(typeof(RadialSegment), nameof(RadialSegment.SetHovered))]
    public static class RadialSegment_SetHovered_Patch
    {
        // Sorry for replacing your code Morphious87 ^^"
        private static void Prefix(
            ref bool __runOriginal, bool hovered, Color ___hoveredOutlineColor, Color ___unhoveredOutlineColor,
            Material ___material, tk2dSprite[] ___gunOutlineSprites, tk2dClippedSprite ___gunSprite, Gun ___originalGun)
        {
            if (!__runOriginal)
                return;
            var oCol = hovered ? ___hoveredOutlineColor : ___unhoveredOutlineColor;
            ___material.SetColor(OutlineColor, oCol);

            if (___gunOutlineSprites == null) return;
            SetOutlineColor(___originalGun.quality);
            
            SpriteOutlineManager.RemoveOutlineFromSprite(___gunSprite);
            SpriteOutlineManager.AddOutlineToSprite(___gunSprite, _outlineColor, 0.1f);
            __runOriginal = false;
        }
    }
}