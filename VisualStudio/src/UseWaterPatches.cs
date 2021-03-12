using Harmony;
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
    //* When the "refuel" button is pressed.
    [HarmonyPatch(typeof(Panel_Inventory_Examine), "OnRefuel")]
    internal class Panel_Inventory_OnRefuel
    {
        internal static void OnRefuelFinished(bool success, bool playerCancel, float progress)
        {
            Panel_Inventory_Examine panel_Inventory_Examine = InterfaceManager.m_Panel_Inventory_Examine;
            LiquidItem liquidItem = panel_Inventory_Examine.m_GearItem.m_LiquidItem;
            // Remove water and adjust the water supply.
            float maxWaterInBottle = Mathf.Min(Water.GetActual(liquidItem.m_LiquidQuality), liquidItem.m_LiquidCapacityLiters);
            float maximumWaterRefuel = maxWaterInBottle - liquidItem.m_LiquidLiters;
            float finalWaterRefuel = maximumWaterRefuel * progress;
            float finalWaterInBottle = finalWaterRefuel + liquidItem.m_LiquidLiters;
            liquidItem.m_LiquidLiters = 0;
            Water.WATER.Remove(finalWaterRefuel, liquidItem.m_LiquidQuality);
            liquidItem.m_LiquidLiters = finalWaterInBottle;
            panel_Inventory_Examine.RefreshMainWindow();
        }
        internal static void Refuel(Panel_Inventory_Examine __instance)
        {
            var liquidItem = __instance.m_GearItem.m_LiquidItem;
            if (liquidItem.m_LiquidLiters >= liquidItem.m_LiquidCapacityLiters)
            {
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_Lampalreadyfull")); // There could be a better message..
                GameAudioManager.PlayGUIError();
                __instance.RefreshMainWindow();
                return;
            }
            if (Water.GetActual(liquidItem.m_LiquidQuality) <= 0.001f) // If the current water supply is empty.
            {
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_Empty"));
                GameAudioManager.PlayGUIError();
                __instance.RefreshMainWindow();
                return;
            }
            float maxWaterInBottle = Mathf.Min(Water.GetActual(liquidItem.m_LiquidQuality), liquidItem.m_LiquidCapacityLiters);
            float maximumWaterRefuel = Mathf.Max(maxWaterInBottle - liquidItem.m_LiquidLiters, 0);
            if (maximumWaterRefuel <= 0.001f) // If nothing gets transferred.
            {
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_None"));
                GameAudioManager.PlayGUIError();
                __instance.RefreshMainWindow();
                return;
            }
            GameAudioManager.PlayGuiConfirm();
            float refuelDuration = Mathf.Max(maximumWaterRefuel * 4, 1);
            InterfaceManager.m_Panel_GenericProgressBar.Launch(Localization.Get("GAMEPLAY_RefuelingProgress"), refuelDuration, 0f, 0f,
                            "Play_SndActionRefuelLantern", null, false, true, new System.Action<bool, bool, float>(OnRefuelFinished));

        }
        internal static void OnPourFinished(bool success, bool playerCancel, float progress)
        {
            Panel_Inventory_Examine panel_Inventory_Examine = InterfaceManager.m_Panel_Inventory_Examine;
            LiquidItem liquidItem = panel_Inventory_Examine.m_GearItem.m_LiquidItem;
            float lostLiters = liquidItem.m_LiquidLiters * progress;
            if (liquidItem.m_LiquidQuality == LiquidQuality.Potable) // Potable water
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
            liquidItem.m_LiquidLiters = Mathf.Max(liquidItem.m_LiquidLiters - lostLiters, 0);
            Water.AdjustWaterSupplyToWater();
            panel_Inventory_Examine.RefreshMainWindow();
            panel_Inventory_Examine.OnSelectHarvestButton();
        }
        internal static void Pour(Panel_Inventory_Examine __instance)
        {
            var liquidItem = __instance.m_GearItem.m_LiquidItem;
            if (liquidItem.m_LiquidLiters <= 0.001f)
            {
                HUDMessage.AddMessage(Localization.Get("GAMEPLAY_Empty"));
                GameAudioManager.PlayGUIError();
                return;
            }

            GameAudioManager.PlayGuiConfirm();
            float lostLitersDuration = Mathf.Max(liquidItem.m_LiquidLiters * 4, 1);

            InterfaceManager.m_Panel_GenericProgressBar.Launch(Localization.Get("GAMEPLAY_RefuelingProgress"), lostLitersDuration, 0f, 0f,
                            "Play_SndActionRefuelLantern", null, false, true, new System.Action<bool, bool, float>(OnPourFinished));

        }
        internal static bool Prefix(Panel_Inventory_Examine __instance)
        {
            if (WaterUtils.IsWaterItem(__instance.m_GearItem))
            {
                // Pour on harvest, refuel on refuel.
                if (__instance.m_Buttons[__instance.m_SelectedButtonIndex] == __instance.m_Button_Harvest)
                {
                    Pour(__instance);
                }
                else
                {
                    Refuel(__instance);
                }
                return false;
            }
            return true;
        }
    }
    //* Build the new "water refuel" panel and override the old "refuel" panel. Dynamic changes.
    [HarmonyPatch(typeof(Panel_Inventory_Examine), "RefreshRefuelPanel")]
    internal class Panel_Inventory_RefreshRefuelPanel
    {
        internal static bool Prefix(Panel_Inventory_Examine __instance)
        {
            if (WaterUtils.IsWaterItem(__instance.m_GearItem))
            {
                // Activate harvest button
                __instance.m_Button_Harvest.gameObject.SetActive(true);

                // This is basically the old "RefreshRefuelPanel" method adapted to the new "water pour" panel
                __instance.m_RefuelPanel.SetActive(false);
                __instance.m_Button_Refuel.gameObject.SetActive(true);
                var liquidItem = __instance.m_GearItem.m_LiquidItem;
                float currentWater = liquidItem.m_LiquidLiters;
                float maxWater = liquidItem.m_LiquidCapacityLiters;
                // Update water Quality
                float totalWater = GameManager.GetPlayerManagerComponent().GetTotalLiters(GearLiquidTypeEnum.Water);
                if (currentWater <= 0.001f && Water.GetActual(liquidItem.m_LiquidQuality) <= 0.001f && totalWater > 0.001f)
                {
                    liquidItem.m_LiquidQuality = liquidItem.m_LiquidQuality == LiquidQuality.Potable ? LiquidQuality.NonPotable : LiquidQuality.Potable;
                    Panel_Helper.UpdateFuelSupplyTexture();
                }
                __instance.m_Refuel_X.gameObject.SetActive(false);
                __instance.m_RequiresFuelMessage.SetActive(false);
                bool refuelPossible = currentWater == maxWater || currentWater >= totalWater - 0.001f;
                __instance.m_Button_Refuel.gameObject.GetComponent<Panel_Inventory_Examine_MenuItem>().SetDisabled(refuelPossible);

                __instance.m_MouseRefuelButton.SetActive(false);
                // Display bottle water amount
                string currentLocalWaterString = Utils.GetLiquidQuantityStringNoOunces(InterfaceManager.m_Panel_OptionsMenu.m_State.m_Units, Mathf.Floor(currentWater * 100) / 100);
                string maxLocalWaterString = Utils.GetLiquidQuantityStringWithUnitsNoOunces(InterfaceManager.m_Panel_OptionsMenu.m_State.m_Units, maxWater);
                __instance.m_LanternFuelAmountLabel.text = currentLocalWaterString + "/" + maxLocalWaterString;
                // Display Total water
                string currentTotalWater = Utils.GetLiquidQuantityStringWithUnitsNoOunces(InterfaceManager.m_Panel_OptionsMenu.m_State.m_Units, Water.GetActual(liquidItem.m_LiquidQuality));
                __instance.m_FuelSupplyAmountLabel.text = currentTotalWater;
                // Switch refuel label on harvest selected.
                bool harvestSelected = __instance.m_Buttons[__instance.m_SelectedButtonIndex] == __instance.m_Button_Harvest;
                Panel_Helper.oUseRefuelBLabel.SetActive(!harvestSelected);
                Panel_Helper.nUseRefuelBLabel.SetActive(harvestSelected);
                return false;
            }
            return true;
        }
    }
    //* Disables the harvest window and enables the refuel panel.
    [HarmonyPatch(typeof(Panel_Inventory_Examine), "RefreshButton")]
    internal class Panel_Inventory_RefreshButton
    {
        internal static void Postfix(Panel_Inventory_Examine __instance)
        {
            if (WaterUtils.IsWaterItem(__instance.m_GearItem))
            {
                __instance.m_HarvestWindow.SetActive(false);
                __instance.m_RefuelPanel.SetActive(true);
                bool harvestSelected = __instance.m_Buttons[__instance.m_SelectedButtonIndex] == __instance.m_Button_Harvest;
                Panel_Helper.oUseRefuelBLabel.SetActive(!harvestSelected);
                Panel_Helper.nUseRefuelBLabel.SetActive(harvestSelected);
            }
        }
    }
    //* Build the new "water pour" panel and override the old "refuel" panel. Static changes.
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
    //* Panel Helper for water operations.
    internal static class Panel_Helper
    {
        internal static Texture previousLanternTexture;
        internal static Texture previousFuelSupplyTexture;
        internal static string previousRefuelButtonSprite = "";
        internal static string previousHarvestButtonSprite = "";
        internal static GameObject oSelectHarvestBLabel; // Original select harvest button label.
        internal static GameObject nSelectHarvestBLabel; // New select harvest button label.
        internal static GameObject oUseRefuelBLabel; // Original use refuel button label.
        internal static GameObject nUseRefuelBLabel; // New use refuel button label
        internal static void Initialize(Panel_Inventory_Examine panel_ie) // Create new labels.
        {
            oSelectHarvestBLabel = panel_ie.m_Button_Harvest.GetComponentInChildren<UILabel>().gameObject;
            nSelectHarvestBLabel = GameObject.Instantiate(oSelectHarvestBLabel, oSelectHarvestBLabel.transform.parent);
            nSelectHarvestBLabel.GetComponentInChildren<UILocalize>().key = "GAMEPLAY_Unload";
            nSelectHarvestBLabel.SetActive(false);

            oUseRefuelBLabel = panel_ie.m_RefuelPanel.transform.Find("RefuelPanel_Buttons").GetComponentInChildren<UILabel>().gameObject;
            nUseRefuelBLabel = GameObject.Instantiate(oUseRefuelBLabel, oUseRefuelBLabel.transform.parent);
            nUseRefuelBLabel.GetComponent<UILocalize>().key = "GAMEPLAY_Unload";
            nUseRefuelBLabel.SetActive(false);
        }
        internal static void LabelsSetActive(Panel_Inventory_Examine panel_ie, bool value)
        {
            // Old labels
            panel_ie.m_RefuelPanel.transform.Find("FuelDisplay/Lanter_Label").gameObject.SetActive(!value);
            panel_ie.m_RefuelPanel.transform.Find("FuelDisplay/FuelSupply_Label").gameObject.SetActive(!value);
            oSelectHarvestBLabel.SetActive(!value);
            // New labels
            nSelectHarvestBLabel.SetActive(value);
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
                UpdateFuelSupplyTexture();
            }
            // Change Button sprites
            previousRefuelButtonSprite = panel_ie.m_Button_Refuel.normalSprite;
            panel_ie.m_Button_Refuel.normalSprite = panel_ie.m_Button_Harvest.normalSprite;
            previousHarvestButtonSprite = panel_ie.m_Button_Harvest.normalSprite;
            panel_ie.m_Button_Harvest.normalSprite = panel_ie.m_Button_Unload.normalSprite;
            // Deactivate/ activate button labels
            LabelsSetActive(panel_ie, true);
            panel_ie.m_Button_Harvest.gameObject.GetComponent<Panel_Inventory_Examine_MenuItem>().m_LabelTitle = nSelectHarvestBLabel.GetComponent<UILabel>();
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
            panel_ie.m_Button_Harvest.normalSprite = previousHarvestButtonSprite;
            // Deactivate/ activate labels
            Panel_Helper.LabelsSetActive(panel_ie, false);
            panel_ie.m_Button_Harvest.gameObject.GetComponent<Panel_Inventory_Examine_MenuItem>().m_LabelTitle = oSelectHarvestBLabel.GetComponent<UILabel>();
        }
        internal static void UpdateFuelSupplyTexture()
        {
            Panel_Inventory_Examine panel_Inventory_Examine = InterfaceManager.m_Panel_Inventory_Examine;
            UITexture FuelSupplyTexture = panel_Inventory_Examine.m_RefuelPanel.transform.Find("FuelDisplay/FuelSupply_Texture").GetComponent<UITexture>();
            if (FuelSupplyTexture)
            {
                if (panel_Inventory_Examine.m_GearItem.m_LiquidItem.m_LiquidQuality == LiquidQuality.Potable)
                {
                    FuelSupplyTexture.mainTexture = Utils.GetInventoryIconTexture(GameManager.GetInventoryComponent().GetPotableWaterSupply());
                }
                else
                {
                    FuelSupplyTexture.mainTexture = Utils.GetInventoryIconTexture(GameManager.GetInventoryComponent().GetNonPotableWaterSupply());
                }
            }
        }

    }
    //* Creates and initializes the new pour botton labels.
    [HarmonyPatch(typeof(Panel_Inventory_Examine), "Start")]
    internal class Panel_Inventory_Examine_Start
    {
        internal static void Postfix(Panel_Inventory_Examine __instance)
        {
            Panel_Helper.Initialize(__instance);
        }
    }
}