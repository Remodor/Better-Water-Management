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
    internal class ItemDescriptionPage_CanExamine
    {
        public static void Postfix(GearItem gi, ref bool __result)
        {
            if (WaterUtils.IsWaterItem(gi))
            {
                __result = true;
            }
        }
    }
    [HarmonyPatch(typeof(Panel_Inventory_Examine), "RefreshRefuelPanel")]
    internal class Panel_Inventory_RefreshRefuelPanel
    {
        public static bool Prefix(Panel_Inventory_Examine __instance)
        {
            if (WaterUtils.IsWaterItem(__instance.m_GearItem))
            {
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
                if (__instance.m_GearItem.m_LiquidItem.m_LiquidQuality == LiquidQuality.Potable)
                {
                    string currentTotalPotableWater = Utils.GetLiquidQuantityStringWithUnitsNoOunces(InterfaceManager.m_Panel_OptionsMenu.m_State.m_Units, Water.GetActual(LiquidQuality.Potable));
                    __instance.m_FuelSupplyAmountLabel.text = currentTotalPotableWater;
                } else
                {
                    string currentTotalNonPotableWater = Utils.GetLiquidQuantityStringWithUnitsNoOunces(InterfaceManager.m_Panel_OptionsMenu.m_State.m_Units, Water.GetActual(LiquidQuality.NonPotable));
                    __instance.m_FuelSupplyAmountLabel.text = currentTotalNonPotableWater;
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Panel_Inventory_Examine), "OnRefuel")]
    internal class Panel_Inventory_OnRefuel
    {
        private static void OnPourFinished(bool success, bool playerCancel, float progress)
        {
            Panel_Inventory_Examine panel_Inventory_Examine = InterfaceManager.m_Panel_Inventory_Examine;

            MelonLoader.MelonLogger.Log("gi: {0}", panel_Inventory_Examine.m_GearItem.name);
            MelonLoader.MelonLogger.Log("water: {0}", Water.GetActual(LiquidQuality.Potable));
            MelonLoader.MelonLogger.Log("water: {0}", Water.GetActual(LiquidQuality.NonPotable));
            float lostLiters = panel_Inventory_Examine.m_GearItem.m_LiquidItem.m_LiquidLiters * progress;
            MelonLoader.MelonLogger.Log("progress: {0}", progress);
            MelonLoader.MelonLogger.Log("lostLiters: {0}", lostLiters);

            if (panel_Inventory_Examine.m_GearItem.m_LiquidItem.m_LiquidQuality == LiquidQuality.Potable)
            {
                WaterSupply potableWaterSupply = GameManager.GetInventoryComponent().GetPotableWaterSupply().m_WaterSupply;
                Water.ShowLostMessage(potableWaterSupply, "GAMEPLAY_WaterPotable", lostLiters);
            }
            else
            {
                WaterSupply nonPotableWaterSupply = GameManager.GetInventoryComponent().GetNonPotableWaterSupply().m_WaterSupply;
                Water.ShowLostMessage(nonPotableWaterSupply, "GAMEPLAY_WaterUnsafe", lostLiters);
            }

            panel_Inventory_Examine.m_GearItem.m_LiquidItem.m_LiquidLiters = Mathf.Max(panel_Inventory_Examine.m_GearItem.m_LiquidItem.m_LiquidLiters - lostLiters, 0);
            Water.AdjustWaterSupplyToWater();
            MelonLoader.MelonLogger.Log("water: {0}", Water.GetActual(LiquidQuality.Potable));
            MelonLoader.MelonLogger.Log("water: {0}", Water.GetActual(LiquidQuality.NonPotable));


            panel_Inventory_Examine.RefreshMainWindow();
        }
        public static bool Prefix(Panel_Inventory_Examine __instance)
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
    // This patch changes the refuel interface to look like pouring.
    [HarmonyPatch(typeof(Panel_Inventory_Examine), "Enable")]
    internal class Panel_Inventory_Examine_Enable
    {
        internal static bool changedToPour = false;
        internal static Texture previousLanternTexture;
        internal static Texture previousFuelSupplyTexture;
        internal static string previousButtonLabel;
        internal static string previousButtonSprite;
        public static void Prefix(Panel_Inventory_Examine __instance, bool enable)
        {
            if (WaterUtils.IsWaterItem(__instance.m_GearItem) && enable) // Convert the panel to water pouring.
            {
                changedToPour = true;
                Transform refuelPanelTransform = __instance.m_RefuelPanel.transform;
                // Change lantern texture. Save old texture.
                UITexture lanternTexture = refuelPanelTransform.Find("FuelDisplay/Lantern_Texture").GetComponent<UITexture>();
                if (lanternTexture)
                {
                    previousLanternTexture = lanternTexture.mainTexture;
                    lanternTexture.mainTexture = Utils.GetInventoryIconTexture(__instance.m_GearItem);
                }
                // Change fuel supply texture. Save old texture.
                UITexture FuelSupplyTexture = refuelPanelTransform.Find("FuelDisplay/FuelSupply_Texture").GetComponent<UITexture>();
                if (FuelSupplyTexture)
                {
                    previousFuelSupplyTexture = FuelSupplyTexture.mainTexture;
                    if (__instance.m_GearItem.m_LiquidItem.m_LiquidQuality == LiquidQuality.Potable)
                    {
                        FuelSupplyTexture.mainTexture = Utils.GetInventoryIconTexture(GameManager.GetInventoryComponent().GetPotableWaterSupply());
                    } else
                    {
                        FuelSupplyTexture.mainTexture = Utils.GetInventoryIconTexture(GameManager.GetInventoryComponent().GetNonPotableWaterSupply());
                    }
                }
                // Hide wrong labels
                refuelPanelTransform.Find("FuelDisplay/Lanter_Label").gameObject.SetActive(false);
                refuelPanelTransform.Find("FuelDisplay/FuelSupply_Label").gameObject.SetActive(false);
                // Change Button sprite
                previousButtonSprite = __instance.m_Button_Refuel.normalSprite;
                __instance.m_Button_Refuel.normalSprite = __instance.m_Button_Harvest.normalSprite;
                __instance.m_Button_Refuel.GetComponentInChildren<UILocalize>().key = "GAMEPLAY_Drop";
                refuelPanelTransform.Find("RefuelPanel_Buttons").gameObject.GetComponentInChildren<UILocalize>().key = "GAMEPLAY_Drop";
                __instance.m_Button_Refuel.GetComponentInChildren<UILocalize>().OnLocalize();
                refuelPanelTransform.Find("RefuelPanel_Buttons").GetComponentInChildren<UILocalize>().OnLocalize();

            }
            else if (changedToPour) // Revert the changes.
            {
                Transform refuelPanelTransform = __instance.m_RefuelPanel.transform;
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
                // Reactivate labels
                refuelPanelTransform.Find("FuelDisplay/Lanter_Label").gameObject.SetActive(true);
                refuelPanelTransform.Find("FuelDisplay/FuelSupply_Label").gameObject.SetActive(true);
                // Change Button label and sprite
                __instance.m_Button_Refuel.normalSprite = previousButtonSprite;

                __instance.m_Button_Refuel.GetComponentInChildren<UILocalize>().key = previousButtonLabel;
                refuelPanelTransform.Find("RefuelPanel_Buttons").GetComponentInChildren<UILocalize>().key = previousButtonLabel;
                __instance.m_Button_Refuel.GetComponentInChildren<UILocalize>().OnLocalize();
                refuelPanelTransform.Find("RefuelPanel_Buttons").GetComponentInChildren<UILocalize>().OnLocalize();
                changedToPour = false;
            }
        }
    }
}