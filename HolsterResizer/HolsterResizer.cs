using HarmonyLib;
using HarmonyLib.Tools;
using LuxURPEssentials;
using MelonLoader;
using SLZ.Bonelab;
using SLZ.Interaction;
using SLZ.Marrow.Data;
using SLZ.Player;
using SLZ.Props;
using SLZ.Props.Weapons;
using SLZ.Rig;
using SLZ.VRMK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace HolsterResizer
{
    public class HolsterResizer : MelonMod
    {
        public static MelonLogger.Instance Logger => Melon<HolsterResizer>.Logger;
        public static BonelabGameControl GameControl { get; private set; }
        public static RigManager PlayerRig => GameControl.PlayerRigManager;

        private static float _relativeSize;
        public static float RelativeSize
        {
            get => _relativeSize;
            set => _relativeSize = value / 1.8f;
        }

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Loaded mod!");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            GameControl = GameObject.FindObjectOfType(Il2CppType.Of<BonelabGameControl>()) as BonelabGameControl;
        }
    }

    /// <summary>
    /// This patch resizes all inventory body slots on avatar change. It also resizes the Items that are holstered
    /// </summary>
    [HarmonyPatch(typeof(RigManager), nameof(RigManager.SwitchAvatar))]
    public static class SwitchAvatarPatch
    {
        public static void Postfix(RigManager __instance, Avatar newAvatar)
        {
            HolsterResizer.RelativeSize = __instance.avatar.height;
            float relSize = HolsterResizer.RelativeSize;

            // Resize the body log
            PullCordDevice bodyLog = GameObject.FindObjectOfType<PullCordDevice>();
            bodyLog.GetComponent<Transform>().localScale = new Vector3(relSize, relSize, relSize);

            foreach (var bodySlot in __instance.inventory.bodySlots)
            {
                bodySlot.GetComponent<Transform>().localScale = new Vector3(relSize, relSize, relSize);
                if (bodySlot.name.Equals("BeltLf1"))
                {
                    InventoryAmmoReceiver iar = bodySlot.GetComponentInChildren<InventoryAmmoReceiver>();

                    if (iar == null)
                    {
                        HolsterResizer.Logger.Warning("Couldn't find iar!");
                        continue;
                    }

                    foreach (var mag in iar._magazineArts)
                    {
                        mag.GetComponent<Transform>().localScale = new Vector3(1, 1, 1);
                    }
                }
            }
        }
    }

    /// <summary>
    /// This patch resizes the item when dropped into an inventory slot
    /// </summary>
    [HarmonyPatch(typeof(InventorySlotReceiver), nameof(InventorySlotReceiver.OnHandDrop))]
    public static class OnHandDropPatch
    {
        public static void Postfix(InventorySlotReceiver __instance, IGrippable host)
        {
            //GameObject go = __instance._slottedWeapon.interactableHost.gameObject;
            GameObject go = host.GetHostGameObject();
            float relSize = HolsterResizer.RelativeSize;
            go.GetComponent<Transform>().localScale = new Vector3(relSize, relSize, relSize);
        }
    }

    /// <summary>
    /// This patch resizes the item back to normal when taking it out of an inventory slot
    /// </summary>
    [HarmonyPatch(typeof(InventorySlotReceiver), nameof(InventorySlotReceiver.OnHandGrab))]
    public static class OnHandGrabPatch
    {
        public static void Prefix(InventorySlotReceiver __instance, Hand hand)
        {
            GameObject go = __instance._slottedWeapon.interactableHost.gameObject;
            float relSize = 1 / HolsterResizer.RelativeSize;
            go.GetComponent<Transform>().localScale = new Vector3(relSize, relSize, relSize);
        }
    }

    /// <summary>
    /// This patch handles the ammo pouch and it's magazines when you switch between ammo types
    /// </summary>
    [HarmonyPatch(typeof(InventoryAmmoReceiver), nameof(InventoryAmmoReceiver.SwitchMagazine))]
    public static class SwitchMagazinePatch
    {
        public static void Postfix(InventoryAmmoReceiver __instance, MagazineData magazineData, CartridgeData cartridgeData)
        {
            float relSize = HolsterResizer.RelativeSize;
            foreach (var mag in __instance._magazineArts)
            {
                mag.GetComponent<Transform>().localScale = new Vector3(1, 1, 1);
            }
        }
    }

    /// <summary>
    /// Should you grab a magazine that is the wrong size, this patch will scale it back
    /// </summary>
    [HarmonyPatch(typeof(Magazine), nameof(Magazine.OnGrab))]
    public static class OnGrabPatch
    {
        public static void Postfix(Magazine __instance, Hand hand)
        {
            __instance.GetComponent<Transform>().localScale = new Vector3(1, 1, 1);
        }
    }
}
