using HarmonyLib;
using MelonLoader;
using SLZ.Bonelab;
using SLZ.Interaction;
using SLZ.Marrow.Data;
using SLZ.Props;
using SLZ.Props.Weapons;
using SLZ.Rig;
using SLZ.VRMK;
using System;
using System.Diagnostics;
using UnhollowerRuntimeLib;
using UnityEngine;

namespace HolsterResizer
{
    public class HolsterResizer : MelonMod
    {
        public static MelonLogger.Instance Logger => Melon<HolsterResizer>.Logger;
        public static float RelativeSize
        {
            get => _relativeSize;
            set => _relativeSize = value / 1.8f;
        }
        private static float _relativeSize;
        public static Vector3 RelativeSizeVec => new Vector3(_relativeSize, _relativeSize, _relativeSize);

        public static void ToAvatarScale(Transform trans)
        {
            if (trans.lossyScale != RelativeSizeVec)
            {
                float factor = trans.localScale.x * (RelativeSize / trans.lossyScale.x);
                trans.localScale = new Vector3(factor, factor, factor);
            }
        }

        public static void ToNormalScale(Transform trans)
        {
            if (trans.lossyScale != new Vector3(1, 1, 1))
            {
                float factor = trans.localScale.x * (1 / trans.lossyScale.x);
                trans.localScale = new Vector3(factor, factor, factor);
            }
        }

        [Conditional("DEBUG")]
        public static void DbgLog(string msg)
        {
            Logger.Msg(msg);
        }
        [Conditional("DEBUG")]
        public static void DbgLog(string msg, ConsoleColor color)
        {
            Logger.Msg(color, msg);
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
            HolsterResizer.DbgLog("Avatar change resize", ConsoleColor.DarkMagenta);

            HolsterResizer.RelativeSize = __instance.avatar.height;
            float relSize = HolsterResizer.RelativeSize;

            // Resize the body log
            PullCordDevice bodyLog = __instance.physicsRig.GetComponentInChildren<PullCordDevice>();

            if (bodyLog != null)
            {
                bodyLog.GetComponent<Transform>().localScale = new Vector3(relSize, relSize, relSize);
                HolsterResizer.DbgLog($"Resized bodylog: {bodyLog.name}");
            }
            else
            {
                HolsterResizer.DbgLog("Couldn't find bodylog");
            }

            foreach (var bodySlot in __instance.inventory.bodySlots)
            {
                bodySlot.GetComponent<Transform>().localScale = new Vector3(relSize, relSize, relSize);
                HolsterResizer.DbgLog($"Resized {bodySlot.name}");
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
            GameObject go = host.GetHostGameObject();

            if (__instance._weaponHost == null)
            {
                HolsterResizer.DbgLog($"Tried to holster {go.name} but didn't");
                return;
            }

            float relSize = HolsterResizer.RelativeSize;
            HolsterResizer.ToAvatarScale(go.GetComponent<Transform>());
            HolsterResizer.DbgLog($"Holstered weapon resize: {go.name}", ConsoleColor.DarkMagenta);
        }
    }

    /// <summary>
    /// This patch resizes the item back to normal when taking it out of an inventory slot
    /// </summary>
    [HarmonyPatch(typeof(InventorySlotReceiver), nameof(InventorySlotReceiver.OnHandGrab))]
    public static class OnHandGrabPatch
    {
        public static void Postfix(InventorySlotReceiver __instance, Hand hand)
        {
            GameObject go = hand.AttachedReceiver.Host.GetHostGameObject();
            HolsterResizer.ToNormalScale(go.GetComponent<Transform>());
            HolsterResizer.DbgLog($"Unholstered weapon resize: {go.name}", ConsoleColor.DarkMagenta);
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
            Vector3 unitVec = new Vector3(1, 1, 1);
            foreach (var mag in __instance._magazineArts)
            {
                mag.GetComponent<Transform>().localScale = unitVec;
                mag._firstCartridgeArt.GetComponent<Transform>().localScale = unitVec;
                mag._secondCartridgeArt.GetComponent<Transform>().localScale = unitVec;
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
            Vector3 unitVec = new Vector3(1, 1, 1);
            __instance.GetComponent<Transform>().localScale = unitVec;
            __instance._firstCartridgeArt.GetComponent<Transform>().localScale = unitVec;
            __instance._secondCartridgeArt.GetComponent<Transform>().localScale = unitVec;
        }
    }

    /// <summary>
    /// This patch fixes the first magazine of a new ammo type being the wrong size
    /// </summary>
    [HarmonyPatch(typeof(Magazine), nameof(Magazine.OnSpawn))]
    public static class OnSpawnMagPatch
    {
        public static void Postfix(Magazine __instance, GameObject go)
        {
            Vector3 unitVec = new Vector3(1, 1, 1);
            HolsterResizer.DbgLog($"Spawned mag: {go.name}");
            go.GetComponent<Transform>().localScale = unitVec;
        }
    }
}
