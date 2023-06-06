using HarmonyLib;
using HarmonyLib.Tools;
using MelonLoader;
using SLZ.Bonelab;
using SLZ.Interaction;
using SLZ.Player;
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
    public class HolsterResize : MelonMod
    {
        public static MelonLogger.Instance Logger => Melon<HolsterResize>.Logger;
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
    public static class SwapAvatarPatch
    {
        public static void Postfix(RigManager __instance, Avatar newAvatar)
        {
            HolsterResize.RelativeSize = __instance.avatar.height;
            float relSize = HolsterResize.RelativeSize;

            //HolsterResize.Logger.Msg($"Loading avatar {__instance.avatar.name} with height abs: {__instance.avatar.height} rel: {relSize}!");

            foreach (var v in __instance.inventory.bodySlots)
            {
                v.GetComponent<Transform>().localScale = new Vector3(relSize, relSize, relSize);
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
            GameObject go = __instance._slottedWeapon.interactableHost.gameObject;
            float relSize = HolsterResize.RelativeSize;
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
            float relSize = 1 / HolsterResize.RelativeSize;
            go.GetComponent<Transform>().localScale = new Vector3(relSize, relSize, relSize);
        }
    }
}
