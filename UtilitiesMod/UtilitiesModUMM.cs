using System;
using UnityEngine;
using UnityModManagerNet;

namespace UtilitiesMod.UMM
{
#if DEBUG
    [EnableReloading]
#endif
    public static class Loader
    {
        public static UnityModManager.ModEntry ModEntry { get; private set; }
        public static UtilitiesMod Instance { get; private set; }

        internal static UtilitiesModSettings Settings;

        private static bool Main(UnityModManager.ModEntry modEntry)
        {
            if (ModEntry != null || Instance != null)
            {
                modEntry.Logger.Warning("Utilities is already loaded!");
                return false;
            }

            ModEntry = modEntry;
            Settings = UnityModManager.ModSettings.Load<UtilitiesModSettings>(modEntry);
            ModEntry.OnUnload = Unload;
            ModEntry.OnSaveGUI = Settings.Save;

            var go = new GameObject("[UtilitiesMod]");
            Instance = go.AddComponent<UtilitiesMod>();
            UnityEngine.Object.DontDestroyOnLoad(go);
            Instance.Settings = Settings;

            return true;
        }
        private static bool Unload(UnityModManager.ModEntry modEntry)
        {
            if (Instance != null) UnityEngine.Object.Destroy(Instance);
            return true;
        }

        public static void LogError(string message)
        {
            ModEntry.Logger.Error(message);
        }

        public static void Log(string message)
        {
            ModEntry.Logger.Log(message);
        }

        public static void LogWarning(string message)
        {
            ModEntry.Logger.Warning(message);
        }

        public static void LogException(Exception e)
        {
            ModEntry.Logger.LogException(e);
        }

        public static void LogDebug(string message)
        {
#if DEBUG
            ModEntry.Logger.Log(message);
#endif
        }



        public class UtilitiesModSettings : UnityModManager.ModSettings
        {
            public bool RemoteControlDE6;
            public bool CommsRadioSpawner;
            public bool NoWheelslip;
            public bool NoWheelSlide;
            public bool UnlimitedResources;
            public bool DisableDerailment;
            public bool FreeCaboose;
            public bool FreeRerail;
            public bool FreeClear;

            public override void Save(UnityModManager.ModEntry modEntry)
            {
                Save(this, modEntry);
            }
        }
    }
}
