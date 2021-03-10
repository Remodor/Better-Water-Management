﻿using Harmony;
using UnityEngine;

namespace BetterWaterManagement
{
    [HarmonyPatch(typeof(GameManager), "Start")]
    internal class GameManager_Start
    {
        internal static void Postfix()
        {
            Water.AdjustWaterSupplyToWater();
        }
    }

    //
    //Changes the minimum water amount to display the "Drink" button
    //
    /*[HarmonyPatch(typeof(ItemDescriptionPage), "GetEquipButtonLocalizationId")] //Transpiler!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    internal class ItemDescriptionPageGetEquipButtonLocalizationIdPatch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldc_R4)
                {
                    continue;
                }

                var operand = codes[i].operand;
                if (!(operand is float))
                {
                    continue;
                }

                float value = (float)operand;
                if (value == 0.01f)
                {
                    codes[i].operand = Water.MIN_AMOUNT;
                }
            }

            return codes;
        }
    }*/

    [HarmonyPatch(typeof(Panel_ActionsRadial), "GetDrinkItemsInInventory")]
    internal class Panel_ActionsRadial_GetDrinkItemsInInventory
    {
        internal static bool Prefix(Panel_ActionsRadial __instance, ref Il2CppSystem.Collections.Generic.List<GearItem> __result)
        {
            __result = new Il2CppSystem.Collections.Generic.List<GearItem>();

            for (int index = 0; index < GameManager.GetInventoryComponent().m_Items.Count; ++index)
            {
                GearItem component = GameManager.GetInventoryComponent().m_Items[index].m_GearItem;
                if (component.m_FoodItem != null && component.m_FoodItem.m_IsDrink)
                {
                    if (component.m_IsInSatchel)
                    {
                        __result.Insert(0, component);
                    }
                    else
                    {
                        __result.Add(component);
                    }
                }

                if (WaterUtils.ContainsPotableWater(component))
                {
                    if (component.m_IsInSatchel)
                    {
                        __result.Insert(0, component);
                    }
                    else
                    {
                        __result.Add(component);
                    }
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Panel_Inventory), "CanBeAddedToSatchel")]
    internal class Panel_Inventory_CanBeAddedToSatchel
    {
        internal static bool Prefix(GearItem gi, ref bool __result)
        {
            if (gi.m_DisableFavoriting)
            {
                return false;
            }

            if (WaterUtils.ContainsWater(gi))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "DrinkFromWaterSupply")]//runs when you start drinking water; doesn't run when drinking tea
    internal class PlayerManager_DrinkFromWaterSupply
    {
        internal static void Postfix(WaterSupply ws, bool __result)
        {
            //Implementation.Log("PlayerManager -- DrinkFromWaterSupply");
            if (GameManager.GetThirstComponent().IsAddingThirstOverTime())
            {
                return;
            }

            LiquidItem liquidItem = ws.GetComponent<LiquidItem>();
            if (liquidItem == null)
            {
                return;
            }

            liquidItem.m_LiquidLiters = ws.m_VolumeInLiters;
            Object.Destroy(ws);
            liquidItem.GetComponent<GearItem>().m_WaterSupply = null;
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "OnDrinkWaterComplete")]
    internal class PlayerManager_OnDrinkWaterComplete
    {
        internal static void Postfix(PlayerManager __instance, float progress)
        {
            //WaterSupply waterSupply = AccessTools.Field(__instance.GetType(), "m_WaterSourceToDrinkFrom").GetValue(__instance) as WaterSupply;
            WaterSupply waterSupply = __instance.m_WaterSourceToDrinkFrom;
            if (waterSupply == null)
            {
                return;
            }

            GearItem gearItem = waterSupply.GetComponent<GearItem>();
            if (gearItem.m_LiquidItem != null)
            {
                gearItem.m_LiquidItem.m_LiquidLiters = waterSupply.m_VolumeInLiters;
                Object.Destroy(waterSupply);
                gearItem.m_WaterSupply = null;
            }

            if (gearItem.m_CookingPotItem != null)
            {
                if (!WaterUtils.IsCooledDown(gearItem.m_CookingPotItem))
                {
                    //GameManager.GetPlayerManagerComponent().ApplyFreezingBuff(20 * progress, 0.5f, 1 * progress);
                    GameManager.GetPlayerManagerComponent().ApplyFreezingBuff(20 * progress, 0.5f, 1 * progress, 24f);
                    PlayerDamageEvent.SpawnAfflictionEvent("GAMEPLAY_WarmingUp", "GAMEPLAY_BuffHeader", "ico_injury_warmingUp", InterfaceManager.m_Panel_ActionsRadial.m_FirstAidBuffColor);
                }

                WaterUtils.SetWaterAmount(gearItem.m_CookingPotItem, waterSupply.m_VolumeInLiters);
                Object.Destroy(waterSupply);
            }

            if (waterSupply is WaterSourceSupply)
            {
                WaterSourceSupply waterSourceSupply = waterSupply as WaterSourceSupply;
                waterSourceSupply.UpdateWaterSource();
            }

            Water.AdjustWaterSupplyToWater();
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "OnPurifyWaterComplete")]
    internal class PlayerManager_OnPurifyWaterComplete
    {
        internal static void Postfix()
        {
            //Implementation.Log("PlayerManager -- OnPurifyWaterComplete");
            Water.AdjustWaterToWaterSupply();
        }
    }

    /*[HarmonyPatch(typeof(PlayerManager), "UpdateInspectGear")]// Transpiler!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    internal class PlayerManager_UpdateInspectGear
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codeInstructions = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codeInstructions.Count; i++)
            {
                CodeInstruction codeInstruction = codeInstructions[i];

                if (codeInstruction.opcode != OpCodes.Callvirt)
                {
                    continue;
                }

                MethodInfo methodInfo = codeInstruction.operand as MethodInfo;
                if (methodInfo == null)
                {
                    continue;
                }

                if ((methodInfo.Name == "GetPotableWaterSupply" || methodInfo.Name == "GetNonPotableWaterSupply") && methodInfo.DeclaringType == typeof(Inventory) && methodInfo.GetParameters().Length == 0)
                {
                    codeInstructions[i - 2].opcode = OpCodes.Ldarg_0;
                    codeInstructions[i - 1].opcode = OpCodes.Ldarg_0;
                    codeInstructions[i].opcode = OpCodes.Ldfld;
                    codeInstructions[i].operand = typeof(PlayerManager).GetField("m_Gear", BindingFlags.Instance | BindingFlags.NonPublic);
                }
            }

            return codeInstructions;
        }
    }*/

    //Replacement Patches

    internal class UpdateInspectGearTracker
    {
        internal static bool isExecuting = false;
    }

    [HarmonyPatch(typeof(PlayerManager), "UpdateInspectGear")]
    internal class PlayerManager_UpdateInspectGear
    {
        private static void Prefix()
        {
            UpdateInspectGearTracker.isExecuting = true;
        }
        private static void Postfix()
        {
            UpdateInspectGearTracker.isExecuting = false;
        }
    }

    /*[HarmonyPatch(typeof(PlayerManager), "UseInventoryItem")]
    internal class PlayerManager_UseInventoryItem
    {
        private static void Prefix(ref GearItem gi,float volumeAvailable)
        {
            if (UpdateInspectGearTracker.isExecuting && volumeAvailable > 0f)
            {
                gi = GameManager.GetPlayerManagerComponent().m_Gear;
            }
        }
    }*/

    /*[HarmonyPatch(typeof(Inventory), "GetPotableWaterSupply")]
    internal class Inventory_GetPotableWaterSupply
    {
        private static bool Prefix(ref GearItem __result)
        {
            if (UpdateInspectGearTracker.isExecuting)
            {
                __result = GameManager.GetPlayerManagerComponent().m_Gear;
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), "GetNonPotableWaterSupply")]
    internal class Inventory_GetNonPotableWaterSupply
    {
        private static bool Prefix(ref GearItem __result)
        {
            if (UpdateInspectGearTracker.isExecuting)
            {
                __result = GameManager.GetPlayerManagerComponent().m_Gear;
                return false;
            }
            else
            {
                return true;
            }
        }
    }*/

    //End Replacements

    [HarmonyPatch(typeof(PlayerManager), "UseInventoryItem")]
    internal class PlayerManager_UseInventoryItem
    {
        internal static void Prefix(ref GearItem gi, float volumeAvailable, ref bool __result)
        {
            //Added for replacing transpiler patch:
            //ref to gi
            //float volumeAvailable
            //this if clause
            if (UpdateInspectGearTracker.isExecuting && volumeAvailable > 0f)
            {
                gi = GameManager.GetPlayerManagerComponent().m_Gear;
            }

            if (!WaterUtils.IsWaterItem(gi))
            {
                return;
            }

            LiquidItem liquidItem = gi.m_LiquidItem;

            WaterSupply waterSupply = liquidItem.GetComponent<WaterSupply>();
            if (waterSupply == null)
            {
                waterSupply = liquidItem.gameObject.AddComponent<WaterSupply>();
                gi.m_WaterSupply = waterSupply;
            }

            waterSupply.m_VolumeInLiters = liquidItem.m_LiquidLiters;
            waterSupply.m_WaterQuality = liquidItem.m_LiquidQuality;
            waterSupply.m_TimeToDrinkSeconds = liquidItem.m_TimeToDrinkSeconds;
            waterSupply.m_DrinkingAudio = liquidItem.m_DrinkingAudio;
        }
    }
    [HarmonyPatch(typeof(ItemDescriptionPage), "CanExamine")]
    internal class ItemDescriptionPage_CanExamine // Enable the additional actions menu.
    {
        internal static void Postfix(GearItem gi, ref bool __result)
        {
            if (WaterUtils.IsWaterItem(gi))
            {
                __result = true;
            }
        }
    }
    [HarmonyPatch(typeof(Panel_Inventory_Examine), "OnRefuel")] // When the "refuel" button is pressed.
    internal class Panel_Inventory_OnRefuel
    {
        internal static void OnPourFinished(bool success, bool playerCancel, float progress)
        {
            Panel_Inventory_Examine panel_Inventory_Examine = InterfaceManager.m_Panel_Inventory_Examine;
            float lostLiters = panel_Inventory_Examine.m_GearItem.m_LiquidItem.m_LiquidLiters * progress;
            if (panel_Inventory_Examine.m_GearItem.m_LiquidItem.m_LiquidQuality == LiquidQuality.Potable) // Potable water
            {
                WaterSupply potableWaterSupply = GameManager.GetInventoryComponent().GetPotableWaterSupply().m_WaterSupply;
                Water.ShowLostMessage(potableWaterSupply, "GAMEPLAY_WaterPotable", lostLiters);
            }
            else // NonPotable water
            {
                WaterSupply nonPotableWaterSupply = GameManager.GetInventoryComponent().GetNonPotableWaterSupply().m_WaterSupply;
                Water.ShowLostMessage(nonPotableWaterSupply, "GAMEPLAY_WaterUnsafe", lostLiters);
            }

            // Remove water and adjust the water supply.
            panel_Inventory_Examine.m_GearItem.m_LiquidItem.m_LiquidLiters = Mathf.Max(panel_Inventory_Examine.m_GearItem.m_LiquidItem.m_LiquidLiters - lostLiters, 0);
            Water.AdjustWaterSupplyToWater();
            panel_Inventory_Examine.RefreshMainWindow();
        }
        internal static bool Prefix(Panel_Inventory_Examine __instance)
        {
            if (WaterUtils.IsWaterItem(__instance.m_GearItem))
            {
                if (__instance.m_GearItem.m_LiquidItem.m_LiquidLiters <= 0.001f)
                {
                    HUDMessage.AddMessage(Localization.Get("GAMEPLAY_Empty"));
                    GameAudioManager.PlayGUIError();
                    return false;
                }

                GameAudioManager.PlayGuiConfirm();
                float lostLitersDuration = Mathf.Max(__instance.m_GearItem.m_LiquidItem.m_LiquidLiters * 4, 1);

                InterfaceManager.m_Panel_GenericProgressBar.Launch(Localization.Get("GAMEPLAY_RefuelingProgress"), lostLitersDuration, 0f, 0f, "Play_SndActionRefuelLantern", null, false, true, new System.Action<bool, bool, float>(OnPourFinished));

                return false;
            }
            return true;
        }
    }
    // Build the new "water pour" panel and override the old "refuel" panel. Dynamic changes.
    [HarmonyPatch(typeof(Panel_Inventory_Examine), "RefreshRefuelPanel")]
    internal class Panel_Inventory_RefreshRefuelPanel
    {
        internal static bool Prefix(Panel_Inventory_Examine __instance)
        {
            if (WaterUtils.IsWaterItem(__instance.m_GearItem))
            {
                // This is basically the old "RefreshRefuelPanel" method adapted to the new "water pour" panel
                __instance.m_RefuelPanel.SetActive(false);
                __instance.m_Button_Refuel.gameObject.SetActive(true);
                float currentWater = __instance.m_GearItem.m_LiquidItem.m_LiquidLiters;
                float maxWater = __instance.m_GearItem.m_LiquidItem.m_LiquidCapacityLiters;
                bool hasWater = currentWater > 0;
                __instance.m_Refuel_X.gameObject.SetActive(!hasWater);
                __instance.m_RequiresFuelMessage.SetActive(false);
                __instance.m_Button_Refuel.gameObject.GetComponent<Panel_Inventory_Examine_MenuItem>().SetDisabled(!hasWater);
                __instance.m_MouseRefuelButton.SetActive(hasWater);
                string currentLocalWaterString = Utils.GetLiquidQuantityStringNoOunces(InterfaceManager.m_Panel_OptionsMenu.m_State.m_Units, currentWater);
                string maxLocalWaterString = Utils.GetLiquidQuantityStringWithUnitsNoOunces(InterfaceManager.m_Panel_OptionsMenu.m_State.m_Units, maxWater);
                __instance.m_LanternFuelAmountLabel.text = currentLocalWaterString + "/" + maxLocalWaterString;
                if (__instance.m_GearItem.m_LiquidItem.m_LiquidQuality == LiquidQuality.Potable) // Lists the water amount.
                {
                    string currentTotalPotableWater = Utils.GetLiquidQuantityStringWithUnitsNoOunces(InterfaceManager.m_Panel_OptionsMenu.m_State.m_Units, Water.GetActual(LiquidQuality.Potable));
                    __instance.m_FuelSupplyAmountLabel.text = currentTotalPotableWater;
                }
                else
                {
                    string currentTotalNonPotableWater = Utils.GetLiquidQuantityStringWithUnitsNoOunces(InterfaceManager.m_Panel_OptionsMenu.m_State.m_Units, Water.GetActual(LiquidQuality.NonPotable));
                    __instance.m_FuelSupplyAmountLabel.text = currentTotalNonPotableWater;
                }
                return false;
            }
            return true;
        }
    }
    // Build the new "water pour" panel and override the old "refuel" panel. Static changes.
    [HarmonyPatch(typeof(Panel_Inventory_Examine), "Enable")]
    internal class Panel_Inventory_Examine_Enable
    {
        internal static bool pourButtonActive = false;
        internal static void Postfix(Panel_Inventory_Examine __instance, bool enable)
        {
            if (WaterUtils.IsWaterItem(__instance.m_GearItem) && enable) // Apply changes
            {
                pourButtonActive = true;
                Panel_Helper.ActivatePourPanel(__instance);
            }
            else if (pourButtonActive) // Revert changes
            {
                pourButtonActive = false;
                Panel_Helper.DeactivatePourPanel(__instance);
            }
        }
    }
    internal static class Panel_Helper
    {
        internal static Texture previousLanternTexture;
        internal static Texture previousFuelSupplyTexture;
        internal static string previousRefuelButtonSprite = "";
        internal static GameObject oSelectRefuelBLabel; // Original select refuel button label.
        internal static GameObject oUseRefuelBLabel; // Original use refuel button label.
        internal static GameObject nSelectRefuelBLabel; // New select refuel button label.
        internal static GameObject nUseRefuelBLabel; // New use refuel button label
        internal static void Initialize(Panel_Inventory_Examine panel_ie)
        {
            oSelectRefuelBLabel = panel_ie.m_Button_Refuel.GetComponentInChildren<UILabel>().gameObject;
            nSelectRefuelBLabel = GameObject.Instantiate(oSelectRefuelBLabel, oSelectRefuelBLabel.transform.parent);
            nSelectRefuelBLabel.GetComponentInChildren<UILocalize>().key = "GAMEPLAY_Drop";
            nSelectRefuelBLabel.SetActive(false);

            oUseRefuelBLabel = panel_ie.m_RefuelPanel.transform.Find("RefuelPanel_Buttons").GetComponentInChildren<UILabel>().gameObject;
            nUseRefuelBLabel = GameObject.Instantiate(oUseRefuelBLabel, oUseRefuelBLabel.transform.parent);
            nUseRefuelBLabel.GetComponentInChildren<UILocalize>().key = "GAMEPLAY_Drop";
            nUseRefuelBLabel.SetActive(false);
        }
        internal static void LabelsSetActive(Panel_Inventory_Examine panel_ie, bool value)
        {
            // Old labels
            panel_ie.m_RefuelPanel.transform.Find("FuelDisplay/Lanter_Label").gameObject.SetActive(!value);
            panel_ie.m_RefuelPanel.transform.Find("FuelDisplay/FuelSupply_Label").gameObject.SetActive(!value);
            oSelectRefuelBLabel.SetActive(!value);
            oUseRefuelBLabel.SetActive(!value);
            // New labels
            nSelectRefuelBLabel.SetActive(value);
            nUseRefuelBLabel.SetActive(value);
        }
        internal static void ActivatePourPanel(Panel_Inventory_Examine panel_ie)
        {
            // Change lantern texture. Save old texture.
            UITexture lanternTexture = panel_ie.m_RefuelPanel.transform.Find("FuelDisplay/Lantern_Texture").GetComponent<UITexture>();
            if (lanternTexture)
            {
                previousLanternTexture = lanternTexture.mainTexture;
                lanternTexture.mainTexture = Utils.GetInventoryIconTexture(panel_ie.m_GearItem);
            }
            // Change fuel supply texture. Save old texture.
            UITexture FuelSupplyTexture = panel_ie.m_RefuelPanel.transform.Find("FuelDisplay/FuelSupply_Texture").GetComponent<UITexture>();
            if (FuelSupplyTexture)
            {
                previousFuelSupplyTexture = FuelSupplyTexture.mainTexture;
                if (panel_ie.m_GearItem.m_LiquidItem.m_LiquidQuality == LiquidQuality.Potable)
                {
                    FuelSupplyTexture.mainTexture = Utils.GetInventoryIconTexture(GameManager.GetInventoryComponent().GetPotableWaterSupply());
                }
                else
                {
                    FuelSupplyTexture.mainTexture = Utils.GetInventoryIconTexture(GameManager.GetInventoryComponent().GetNonPotableWaterSupply());
                }
            }
            // Change Button sprite
            previousRefuelButtonSprite = panel_ie.m_Button_Refuel.normalSprite;
            panel_ie.m_Button_Refuel.normalSprite = panel_ie.m_Button_Harvest.normalSprite;
            // Deactivate/ activate button labels
            LabelsSetActive(panel_ie, true);
        }
        internal static void DeactivatePourPanel(Panel_Inventory_Examine panel_ie)
        {
            Transform refuelPanelTransform = panel_ie.m_RefuelPanel.transform;
            UITexture lanternTexture = refuelPanelTransform.Find("FuelDisplay/Lantern_Texture").GetComponent<UITexture>();
            if (lanternTexture)
            {
                lanternTexture.mainTexture = previousLanternTexture;
            }
            UITexture FuelSupplyTexture = refuelPanelTransform.Find("FuelDisplay/FuelSupply_Texture").GetComponent<UITexture>();
            if (FuelSupplyTexture)
            {
                FuelSupplyTexture.mainTexture = previousFuelSupplyTexture;
            }
            // Change Button sprite
            panel_ie.m_Button_Refuel.normalSprite = previousRefuelButtonSprite;
            // Deactivate/ activate labels
            Panel_Helper.LabelsSetActive(panel_ie, false);
        }
    }
    // Creates and initializes the new pour botton labels.
    [HarmonyPatch(typeof(Panel_Inventory_Examine), "Start")]
    internal class Panel_Inventory_Examine_Start
    {
        internal static void Postfix(Panel_Inventory_Examine __instance)
        {
            Panel_Helper.Initialize(__instance);
        }
    }
}