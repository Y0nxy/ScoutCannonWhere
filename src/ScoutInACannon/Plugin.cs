using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace ScoutInACannon
{
    [BepInAutoPlugin]
    public partial class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log { get; private set; } = null!;
        static ConfigEntry<KeyCode> emulateKey;
        public static ConfigEntry<float> extraUp;
        public static ConfigEntry<float> deleteTime;
        public static ConfigEntry<float> stopTime;
        static bool holdingCannon = false;
        public static Vector3 tubeForward;
        static Vector3 spawnPos;
        string ZombiePrefab = "MushroomZombie_Player";
        static GameObject ZombieObj;
        private void Awake()
        {
            Log = Logger;

            Log.LogInfo($"Plugin {Name} is loaded!");
            emulateKey = Config.Bind("hi dryeetman", "Emulate Key", KeyCode.X);
            extraUp = Config.Bind("hi dryeetman", "Up Correctment", 0.25f, new ConfigDescription("", new AcceptableValueRange<float>(-2f, 2f)));
            deleteTime = Config.Bind("hi dryeetman", "deleteTime", 5f, new ConfigDescription("", new AcceptableValueRange<float>(1f, 10f)));
            stopTime = Config.Bind("hi dryeetman", "stopTime", 1.5f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 5f)));
            Harmony harmony = new Harmony("ScoutInACannon");
            harmony.PatchAll();
        }

        void Update()
        {
            if (tubeForward == null || spawnPos == null) return;
            if (holdingCannon && Input.GetKeyDown(emulateKey.Value))
            {
                GameObject prefab = Resources.Load<GameObject>(ZombiePrefab);
                prefab.AddComponent<LaunchThisGuy>();
                //prefab.GetComponent<MushroomZombie>().isNPCZombie = false;
                //Destroy(prefab.GetComponent<CharacterItems>());
                //Destroy(prefab.GetComponent<CharacterCustomization>());
                ZombieObj = Instantiate(prefab, spawnPos + Vector3.up * extraUp.Value, new Quaternion(0, 0, 0, 0));
                Destroy(ZombieObj, deleteTime.Value);
            }

        }


        [HarmonyPatch]
        static class AllPatches
        {
            [HarmonyPatch(typeof(Constructable), "CreateOrMovePreview")]
            [HarmonyPostfix]
            public static void CreateOrMovePreviewPostFix(Constructable __instance)
            {
                var preview = __instance.currentPreview;
                if (preview != null && Input.GetKey(emulateKey.Value))
                {
                    Transform Cannon = preview.transform.GetChild(0).GetChild(0);
                    tubeForward = Cannon.forward;
                    spawnPos = Cannon.parent.GetChild(1).position;//spawn at cannon feet
                    return;
                }
            }

            [HarmonyPatch(typeof(CharacterData), nameof(CharacterData.currentItem), MethodType.Setter)]
            [HarmonyPostfix]
            static void CurrentItemPatch(CharacterData __instance, Item value)
            {
                if (value == null) return;
                if (!__instance.character.IsLocal) return;

                if (value.UIData.itemName.Contains("Cannon")) holdingCannon = true;
                else holdingCannon = false;
                log("holdingCannon = " + holdingCannon);
            }

            [HarmonyPatch(typeof(MushroomZombie))]
            class InstantZombie
            {
                [HarmonyPatch("HideAllRenderers")]
                [HarmonyPrefix]
                public static bool stopHiding(MushroomZombie __instance)
                {
                    //if (__instance.gameObject == ZombieObj)
                    return false;
                    //return true;
                }
                [HarmonyPatch("FadeInRenderers")]
                static bool InstantRendereres(MushroomZombie __instance)
                {
                    //if (__instance.gameObject != ZombieObj) return true;
                    __instance.character.refs.customization.ShowAllRenderers();
                    __instance.SetMushroomMan();
                    for (int i = 0; i < __instance.character.refs.customization.refs.AllRenderers.Length; i++)
                    {
                        for (int j = 0; j < __instance.character.refs.customization.refs.AllRenderers[i].materials.Length; j++)
                        {
                            __instance.character.refs.customization.refs.AllRenderers[i].materials[j].SetFloat("_Opacity", 1.5f);
                        }
                    }

                    return false;
                }
                [HarmonyPatch("Start")]
                static bool DontHideMe(MushroomZombie __instance)
                {
                    if (__instance.GetComponent<LaunchThisGuy>() != null) return true;
                    if (__instance.isNPCZombie)
                    {
                        __instance.StartSleeping();
                    }
                    __instance.character.isZombie = true;
                    __instance.StartCoroutine(__instance.ZombieGrunts());
                    return false;
                }
            }

            //[HarmonyPatch(typeof(CharacterAnimations), "Update")]
            //[HarmonyPrefix]
            //public static bool CharacterAnimations_Update_Patch(CharacterAnimations __instance, Character ___character, HeadBobSetting ___headBobSetting)
            //{
            //    // 1. Ensure the instance exists
            //    if (__instance == null) return false;

            //    // 2. Ensure the base private fields are populated
            //    if (___character == null || ___headBobSetting == null) return false;

            //    // 3. Deep check all the nested references the Update method relies on
            //    if (___character.refs == null ||
            //        ___character.refs.animator == null ||
            //        ___character.data == null ||
            //        ___character.input == null ||
            //        ___character.refs.items == null ||
            //        ___character.refs.afflictions == null)
            //    {
            //        return false; // Skip Update if the character is missing core data components
            //    }

            //    // 4. Ensure the Hip bodypart exists, as it is accessed directly without a null check
            //    if (___character.GetBodypart(BodypartType.Hip) == null)
            //    {
            //        return false;
            //    }

            //    // All checks passed; allow the original Update method to run
            //    return true;
            //}

            //[HarmonyPatch(typeof(ScoutCannon), "RPCA_SetTarget")]
            //public static class ScoutCannon_RPCA_SetTarget_Patch
            //{
            //    [HarmonyPrefix]
            //    public static bool Prefix(ScoutCannon __instance)
            //    {
            //        // 1. Safety check: Prevent execution if the cannon itself was destroyed 
            //        // while the RPC was traveling over the network.
            //        if (__instance == null || __instance.gameObject == null)
            //        {
            //            return false;
            //        }

            //        return true;
            //    }

            //    [HarmonyFinalizer]
            //    public static Exception Finalizer(Exception __exception)
            //    {
            //        // 2. Catch any exceptions thrown during the RPCA_SetTarget execution
            //        if (__exception != null)
            //        {
            //            // 3. If it's the NullReferenceException we are looking for...
            //            if (__exception is NullReferenceException || __exception.InnerException is NullReferenceException)
            //            {
            //                // Returning null swallows the exception completely.
            //                // This stops it from bubbling up into PhotonNetwork and breaking FixedUpdate.
            //                return null;
            //            }
            //        }

            //        // Allow any other types of severe exceptions to pass through normally for debugging
            //        return __exception;
            //    }
            //}

            //[HarmonyPatch(typeof(Character), "Start")]
            //[HarmonyFinalizer]
            //public static Exception Finalizer(Exception __exception, Character __instance)
            //{
            //    // 1. Check if an exception occurred during Character.Start()
            //    if (__exception != null)
            //    {
            //        // 2. Log exactly which character broke to help isolate the issue
            //        string characterName = __instance != null ? __instance.gameObject.name : "Unknown Null Character";
            //        Debug.LogError($"[Mod Fix] Intercepted NRE in Character.Start for '{characterName}'. The character may be missing core references!");
            //        Debug.LogError($"[Mod Fix] Original Error: {__exception.Message}");

            //        // 3. Swallow the exception. 
            //        // This stops the crash from bubbling up and breaking MushroomZombie's search routines.
            //        return null;
            //    }

            //    return null;
            //}

            //[HarmonyPatch(typeof(CharacterCustomization), "Start")]
            //[HarmonyPrefix]
            //public static bool CharacterCustomization_Start_Patch(CharacterCustomization __instance)
            //{
            //    // Check if the character has a valid network identity
            //    var view = __instance.GetComponent<PhotonView>();

            //    // If there is no PhotonView or the Owner is missing, this is a "broken" bot
            //    // Skip the Start() logic to prevent the PersistentPlayerDataService crash
            //    if (view == null || view.Owner == null)
            //    {
            //        return false;
            //    }

            //    return true;
            //}
            //[HarmonyPatch(typeof(CharacterItems), "Awake")]
            //public static class CharacterItems_Awake_Patch
            //{
            //    [HarmonyPrefix]
            //    public static bool Prefix(CharacterItems __instance)
            //    {
            //        // If this is a local bot we just spawned, stop the routine from starting[cite: 1]
            //        // You can check if the object has your custom component as a marker
            //        if (__instance.GetComponent<LaunchThisGuy>() != null)
            //        {
            //            return false; // Skip the original Awake/Coroutine start[cite: 1]
            //        }
            //        return true;
            //    }
            //}
        }
        static void log(string msg)
        {
            Log.LogInfo(msg);
        }

        [HarmonyPatch]
        public static class Cleanup_Safety_Patch
        {
            // Target the problematic OnDestroy methods
            [HarmonyTargetMethods]
            public static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(CharacterItems), "OnDestroy");
                yield return AccessTools.Method(typeof(CharacterCustomization), "OnDestroy");
            }

            [HarmonyPrefix]
            public static bool Prefix(Component __instance)
            {
                // If the character/player reference is missing, don't even attempt the cleanup
                // This stops the NRE from being thrown in the first place
                if (__instance == null) return false;

                return true;
            }

            [HarmonyFinalizer]
            public static Exception Finalizer(Exception __exception)
            {
                // Swallow any NREs that happen during destruction
                return null;
            }
        }
    }
}
