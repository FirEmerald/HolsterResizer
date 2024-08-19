using HarmonyLib;
using MelonLoader;
using System;
using System.Diagnostics;
using UnityEngine;
using BoneLib;
using System.Linq;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Bonelab;
using Il2CppSLZ.VRMK;
using Il2CppSLZ.Marrow.Data;
using BoneLib.BoneMenu;
using LabFusion.Player;

namespace HolsterResizer
{
    public class HolsterResizer : MelonMod
    {
        public const string BoneLibName = "BoneLib";
        public const string FusionName = "LabFusion";
        public const float DefaultSize = 1.8f;

        // Instances
        public static HolsterResizer Instance => Melon<HolsterResizer>.Instance;
        public static MelonLogger.Instance Logger => Melon<HolsterResizer>.Logger;

        // Avatar sizing
        public static float LocalRelativeSize
        {
            get => _localRelativeSize * Melon<HolsterResizer>.Instance.SizeMultiplier;
            set => _localRelativeSize = value / DefaultSize;
        }
        private static float _localRelativeSize;
        public static Vector3 RelativeSizeVec => new Vector3(_localRelativeSize, _localRelativeSize, _localRelativeSize);
        public RigManager LocalRig { get; private set; }

        // Some state info
        public bool IsBoneLibLoaded { get; private set; } = false;
        public bool IsFusionLoaded { get; private set; } = false;

        // Menu elements
        public float SizeMultiplier { get; private set; }
        public bool ScaleUp { get; private set; }
        public bool ScaleDown { get; private set; }
        public bool ScaleBodylog { get; private set; }
        protected FloatElement SizeMultiplierElement => _sizeMultiplierElement as FloatElement;
        private object _sizeMultiplierElement;

        // Preferences
        private MelonPreferences_Category _localPreferences;
        private MelonPreferences_Entry<float> _prefLocalSizeMultiplier;
        private MelonPreferences_Entry<bool> _prefScaleUp;
        private MelonPreferences_Entry<bool> _prefScaleDown;
        private MelonPreferences_Entry<bool> _prefScaleBodylog;

        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();

            // Create or get preferences from MelonLoader
            _localPreferences = MelonPreferences.CreateCategory("Holster Resizer");
            _prefLocalSizeMultiplier = _localPreferences.CreateEntry("localSizeMultiplier", 1.0f);
            _prefScaleUp = _localPreferences.CreateEntry("scaleUp", true);
            _prefScaleDown = _localPreferences.CreateEntry("scaleDown", true);
            _prefScaleBodylog= _localPreferences.CreateEntry("scaleBodylog", true);

            // Get preference values
            SizeMultiplier = _prefLocalSizeMultiplier.Value;
            ScaleUp = _prefScaleUp.Value;
            ScaleDown = _prefScaleDown.Value;
            ScaleBodylog = _prefScaleBodylog.Value;

            // If BoneLib is installed, add the submenu
            if (GetMelonByName(BoneLibName) != null)
            {
                DbgLog("Found BoneLib");
                IsBoneLibLoaded = true;
                InitializeWithBonelib();

                // If LabFusion is installed we need another way to get the local player rig as there might be multiple rigs
                if (GetMelonByName(FusionName) != null)
                {
                    DbgLog("Found LabFusion");
                    IsFusionLoaded = true;
                    InitializeWithFusion();
                }
            }
            else
            {
                DbgLog("Initializing without any optional dependencies");
            }
        }

        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();

            // Save preferences
            _prefScaleUp.Value = ScaleUp;
            _prefScaleDown.Value = ScaleDown;
            _prefLocalSizeMultiplier.Value = SizeMultiplier;
            MelonPreferences.SaveCategory<MelonPreferences_Category>("localPreferences");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            if (!IsFusionLoaded)
            {
                LocalRig = GameObject.FindObjectOfType<RigManager>();
            }
        }

        private void InitializeWithFusion()
        {
            // TODO(Toedtmanns): Check if this works in multiplayer
            LocalPlayer.OnLocalRigCreated += OnLocalPlayerCreated;
        }

        private void InitializeWithBonelib()
        {
            // Create the submenu
            BoneLib.BoneMenu.Page menuCategory = BoneLib.BoneMenu.Page.Root.CreatePage("Holster Resizer", Color.white);
            _sizeMultiplierElement = menuCategory.CreateFloat("Holster size multiplier", Color.white, SizeMultiplier, 0.05f, 0.05f, 2.0f, OnSizeMultiplierChange);
            menuCategory.CreateFunction("Reset multiplier", Color.white, OnSizeMultiplierReset);
            menuCategory.CreateBool("Scale up with Avatar", Color.white, ScaleUp, OnScaleUpChange);
            menuCategory.CreateBool("Scale down with Avatar", Color.white, ScaleDown, OnScaleDownChange);
            menuCategory.CreateBool("Scale Bodylog", Color.white, ScaleBodylog, OnScaleBodylogChange);
        }

        // Menu callbacks
        private void OnSizeMultiplierChange(float multiplier)
        {
            SizeMultiplier = multiplier;
            ScaleHolsters(LocalRig, LocalRelativeSize);
        }
        private void OnSizeMultiplierReset()
        {
            SizeMultiplier = 1.0f;
            SizeMultiplierElement.Value = SizeMultiplier;
            ScaleHolsters(LocalRig, LocalRelativeSize);
        }
        private void OnScaleUpChange(bool scaleUp)
        {
            ScaleUp = scaleUp;
        }
        private void OnScaleDownChange(bool scaleDown)
        {
            ScaleDown = scaleDown;
        }
        private void OnScaleBodylogChange(bool scaleBodylog)
        {
            ScaleBodylog = scaleBodylog;
            ScaleHolsters(LocalRig, LocalRelativeSize);
        }

        // LabFusion integration
        private void OnLocalPlayerCreated(RigManager rig)
        {
            DbgLog("Got local player rig");
            LocalRig = rig;
        }

        // Static methods

        public static void ToAvatarScale(Transform trans)
        {
            if (trans.lossyScale != RelativeSizeVec)
            {
                float factor = trans.localScale.x * (LocalRelativeSize / trans.lossyScale.x);
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

        public static MelonBase GetMelonByName(string name)
        {
            foreach (var assembly in MelonAssembly.LoadedAssemblies)
            {
                foreach (var melon in assembly.LoadedMelons)
                {
                    if (melon.Info.Name == name)
                        return melon;
                }
            }
            return null;
        }

        public static void ScaleHolsters(RigManager rig, float size, bool scaleBodylog = true)
        {
            // Resize the bodylog
            PullCordDevice bodyLog = rig.physicsRig.GetComponentInChildren<PullCordDevice>();

            if (bodyLog != null)
            {
                if (scaleBodylog)
                    bodyLog.GetComponent<Transform>().localScale = new Vector3(size, size, size);
                else
                {
                    float relSize = HolsterResizer.Instance.SizeMultiplier;
                    bodyLog.GetComponent<Transform>().localScale = new Vector3(relSize, relSize, relSize);
                }
                DbgLog($"Resized bodylog: {bodyLog.name}");
            }
            else
            {
                DbgLog("Couldn't find bodylog");
            }

            foreach (var bodySlot in rig.inventory.bodySlots)
            {
                bodySlot.GetComponent<Transform>().localScale = new Vector3(size, size, size);
                DbgLog($"Resized {bodySlot.name}");
                if (bodySlot.name.Equals("BeltLf1"))
                {
                    InventoryAmmoReceiver iar = bodySlot.GetComponentInChildren<InventoryAmmoReceiver>();

                    if (iar == null)
                    {
                        Logger.Warning("Couldn't find iar!");
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
    /// This patch resizes all inventory body slots on avatar change. It also resizes the Items that are holstered
    /// </summary>
    [HarmonyPatch(typeof(RigManager), nameof(RigManager.SwitchAvatar))]
    public static class SwitchAvatarPatch
    {
        public static void Postfix(RigManager __instance, Avatar newAvatar)
        {
            // Resize only if it's the rig of the local player
            if (HolsterResizer.Instance.LocalRig != null && __instance != HolsterResizer.Instance.LocalRig)
                return;

            HolsterResizer.DbgLog("Avatar change resize", ConsoleColor.DarkMagenta);

            if ((HolsterResizer.Instance.ScaleDown || __instance.avatar.height > HolsterResizer.DefaultSize) &&
                (HolsterResizer.Instance.ScaleUp || __instance.avatar.height < HolsterResizer.DefaultSize))
            {
                HolsterResizer.LocalRelativeSize = __instance.avatar.height;
            }
            else
            {
                HolsterResizer.LocalRelativeSize = HolsterResizer.DefaultSize;
            }
            HolsterResizer.ScaleHolsters(__instance, HolsterResizer.LocalRelativeSize, HolsterResizer.Instance.ScaleBodylog);
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

            float relSize = HolsterResizer.LocalRelativeSize;
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
    /// This patch fixes the first magazine of a new ammo type being the wrong size TODO this is still broken, IDK how to fix it. System.ArgumentException: Undefined target method for patch method static void HolsterResizer.OnSpawnMagPatch::Postfix(Il2CppSLZ.Marrow.Magazine __instance)
    /// </summary>
    [HarmonyPatch(typeof(Magazine), nameof(Magazine.OnPoolSpawn))]
    public static class OnSpawnMagPatch
    {
        public static void Postfix(Magazine __instance)
        {
            GameObject go = __instance._poolee.gameObject;
            Vector3 unitVec = new Vector3(1, 1, 1);
            HolsterResizer.DbgLog($"Spawned mag: {go.name}");
            go.GetComponent<Transform>().localScale = unitVec;
        }
    }
}
