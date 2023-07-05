using BepInEx;
using BepInEx.Configuration;
using DV;
using DV.Common;
using DV.Damage;
using DV.Utils;
using DV.InventorySystem;
using DV.RemoteControls;
using DV.ThingTypes;
using DV.Simulation.Cars;
using LocoSim.Implementations;
using UnityEngine;
using HarmonyLib;
using System.Collections;

namespace UtilitiesMod
{
    internal static class PluginInfo
    {
        public const string Guid = "UtilitiesMod";
        public const string Name = "Utilities Mod";
        public const string Version = "0.3.0";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class UtilitiesMod : BaseUnityPlugin
    {
        public static UtilitiesMod Instance { get; private set; }

        public static UtilitiesModSettings Settings { get; private set; }
        private static bool showGui = false;
        private static GUIStyle buttonStyle = new GUIStyle() { fontSize = 8 };
        private static GameObject DE6Prefab;
        private static float RerailMaxPrice;
        private static float DeleteCarMaxPrice;
        private static float WorkTrainSummonMaxPrice;


        public void Start()
        {
            if (Instance != null)
            {
                Logger.LogFatal("Utilities is already loaded!");
                Destroy(this);
                return;
            }

            Instance = this;
            Settings = new UtilitiesModSettings(this);
            Instance.Config.SaveOnConfigSet = true;

            DE6Prefab = Utils.FindPrefab("LocoDE6");
            if (DE6Prefab == null) Logger.LogFatal("DE6 Prefab not found");

            WorldStreamingInit.LoadingFinished += OnLoadingFinished;
            UnloadWatcher.UnloadRequested += UnloadRequested;
        }

        public void OnDestroy()
        {
            UnloadRequested();
        }

        private void OnLoadingFinished()
        {
            StartCoroutine(this.InitCoro());
        }

        private void UnloadRequested()
        {
            disableFreeCaboose();
            disableFreeClear();
            disableFreeRerail();
        }

        private IEnumerator InitCoro()
        {
            while (!AStartGameData.carsAndJobsLoadingFinished)
            {
                yield return null;
            }
            Instance.Logger.LogInfo("Initializing Utilities Mod");
            RerailMaxPrice = Globals.G.GameParams.RerailMaxPrice;
            DeleteCarMaxPrice = Globals.G.GameParams.DeleteCarMaxPrice;
            WorkTrainSummonMaxPrice = Globals.G.GameParams.WorkTrainSummonMaxPrice;

            if (Settings.CommsRadioSpawner.Value) enableCommsSpawner();
            if (Settings.DisableDerailment.Value) disableDerail();
            if (Settings.FreeCaboose.Value) enableFreeCaboose();
            if (Settings.FreeRerail.Value) enableFreeRerail();
            if (Settings.FreeClear.Value) enableFreeClear();
            if (Settings.RemoteControlDE6.Value) enableDE6Remote();

            yield break;
        }

        public static void Error(string message)
        {
            Instance.Logger.LogError(message);
        }

        void OnGUI()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                showGui = false;
                return;
            }

            if (GUI.Button(new Rect(0, 20, 20, 20), "U", new GUIStyle(GUI.skin.button) { fontSize = 10 })) showGui = !showGui;

            if (showGui)
            {
                GUIUtility.GetControlID(FocusType.Passive);
                GUIStyle centeredLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                
                GUILayout.BeginArea(new Rect(20, 20, 250, 500));
                GUILayout.BeginVertical("Utilities", GUI.skin.window);
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Money", centeredLabel, GUILayout.ExpandWidth(true));
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Money: ");
                    GUILayout.Label("$" + SingletonBehaviour<Inventory>.Instance.PlayerMoney.ToString("N"), new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleRight });
                    //GUILayout.Button("Set");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("> $1k"))
                    {
                        SingletonBehaviour<Inventory>.Instance.AddMoney((double)1000);
                    }
                    if (GUILayout.Button("> $10k"))
                    {
                        SingletonBehaviour<Inventory>.Instance.AddMoney((double)10000);
                    }
                    if (GUILayout.Button("> $100k"))
                    {
                        SingletonBehaviour<Inventory>.Instance.AddMoney((double)100000);
                    }
                    if (GUILayout.Button("> $1M"))
                    {
                        SingletonBehaviour<Inventory>.Instance.AddMoney((double)1000000);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("< $1k"))
                    {
                        SingletonBehaviour<Inventory>.Instance.RemoveMoney((double)1000);
                    }
                    if (GUILayout.Button("< $10k"))
                    {
                        SingletonBehaviour<Inventory>.Instance.RemoveMoney((double)10000);
                    }
                    if (GUILayout.Button("< $100k"))
                    {
                        SingletonBehaviour<Inventory>.Instance.RemoveMoney((double)100000);
                    }
                    if (GUILayout.Button("< $1M"))
                    {
                        SingletonBehaviour<Inventory>.Instance.RemoveMoney((double)1000000);
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Licenses", centeredLabel);
                    if (GUILayout.Button("Aquire All Loco Licenses"))
                    {
                        Globals.G.Types.generalLicenses.ForEach(delegate (GeneralLicenseType_v2 l)
                        {
                            SingletonBehaviour<LicenseManager>.Instance.AcquireGeneralLicense(l);
                        });
                    }
                    if (GUILayout.Button("Aquire All Job Licenses"))
                    {
                        Globals.G.Types.jobLicenses.ForEach(delegate (JobLicenseType_v2 l)
                        {
                            SingletonBehaviour<LicenseManager>.Instance.AcquireJobLicense(l);
                        });
                    }
                    if (GUILayout.Button("Unlock All Garages"))
                    {
                        Globals.G.Types.garages.ForEach(delegate (GarageType_v2 g)
                        {
                            SingletonBehaviour<LicenseManager>.Instance.UnlockGarage(g);
                        });
                    }
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Loco", centeredLabel);
                    if (GUILayout.Button("Refill Loco"))
                    {
                        if (PlayerManager.Car == null) return;
                        SimController component = PlayerManager.Car.GetComponent<SimController>();
                        ResourceContainerController resourceContainerController = ((component != null) ? component.resourceContainerController : null);
                        if (resourceContainerController == null) return;

                        resourceContainerController.RefillAllResourceContainers();
                    }

                    if (GUILayout.Button("Repair Loco"))
                    {
                        if (PlayerManager.Car == null) return;
                        DamageController component = PlayerManager.Car.GetComponent<DamageController>();
                        if (component == null) return;

                        component.RepairAll();
                    }
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Cheats", centeredLabel);
                    GUILayout.BeginHorizontal();
                    bool remoteDE6 = GUILayout.Toggle(Settings.RemoteControlDE6.Value, "Remote Controller for DE6");
                    if (remoteDE6 != Settings.RemoteControlDE6.Value)
                    {
                        Settings.RemoteControlDE6.Value = remoteDE6;
                        if (remoteDE6)
                        {
                            enableDE6Remote();
                        }
                        else
                        {
                            disableDE6Remote();
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool commsSpawner = GUILayout.Toggle(Settings.CommsRadioSpawner.Value, "Comms Radio Spawner");
                    if (commsSpawner != Settings.CommsRadioSpawner.Value)
                    {
                        Settings.CommsRadioSpawner.Value = commsSpawner;
                        if (commsSpawner)
                        {
                            enableCommsSpawner();
                        }
                        else
                        {
                            disableCommsSpawner();
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool freeCaboose = GUILayout.Toggle(Settings.FreeCaboose.Value, "Free Caboose");
                    if (freeCaboose != Settings.FreeCaboose.Value)
                    {
                        Settings.FreeCaboose.Value = freeCaboose;
                        if (freeCaboose)
                        {
                            enableFreeCaboose();
                        }
                        else
                        {
                            disableFreeCaboose();
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool freeRerail = GUILayout.Toggle(Settings.FreeRerail.Value, "Free Rerail");
                    if (freeRerail != Settings.FreeRerail.Value)
                    {
                        Settings.FreeRerail.Value = freeRerail;
                        if (freeRerail)
                        {
                            enableFreeRerail();
                        }
                        else
                        {
                            disableFreeRerail();
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool freeClear = GUILayout.Toggle(Settings.FreeClear.Value, "Free Clear/Delete");
                    if (freeClear != Settings.FreeClear.Value)
                    {
                        Settings.FreeClear.Value = freeClear;
                        if (freeClear)
                        {
                            enableFreeClear();
                        }
                        else
                        {
                            disableFreeClear();
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool derail = GUILayout.Toggle(Settings.DisableDerailment.Value, "No Derailment");
                    if (derail != Settings.DisableDerailment.Value)
                    {
                        Settings.DisableDerailment.Value = derail;
                        if (derail)
                        {
                            disableDerail();
                        }
                        else
                        {
                            enableDerail();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

        private void enableDE6Remote()
        {
            if (DE6Prefab == null)
            {
                Logger.LogError("DE6 Prefab not found");
            }
            else
            {
                if (DE6Prefab.GetComponent<RemoteControllerModule>() == null)
                {
                    var remote = DE6Prefab.AddComponent<RemoteControllerModule>();
                    DE6Prefab.GetComponent<SimController>().remoteController = remote;
                }

                foreach (var sc in FindObjectsOfType<SimController>())
                {
                    if (sc.name == "LocoDE6(Clone)" && sc.GetComponent<RemoteControllerModule>() == null)
                    {
                        var rcm = sc.gameObject.AddComponent<RemoteControllerModule>();
                        rcm.Init(Traverse.Create(sc).Field("train").GetValue() as TrainCar, sc.wheelslipController, sc.controlsOverrider, sc.simFlow);
                    }
                }
            }
        }

        private void disableDE6Remote()
        {
            if (DE6Prefab == null)
            {
                Logger.LogError("DE6 Prefab not found");
            }
            else
            {
                if (DE6Prefab.GetComponent<RemoteControllerModule>() != null)
                {
                    Destroy(DE6Prefab.GetComponent<RemoteControllerModule>());
                    DE6Prefab.GetComponent<SimController>().remoteController = null;
                }

                foreach (var sc in FindObjectsOfType<SimController>())
                {
                    if (sc.name == "LocoDE6(Clone)" && sc.TryGetComponent<RemoteControllerModule>(out RemoteControllerModule rcm))
                    {
                        LocomotiveRemoteController lrc = Traverse.Create(rcm).Field("pairedLocomotiveRemote").GetValue() as LocomotiveRemoteController;
                        if (lrc != null)
                        {
                            Traverse.Create(lrc).Method("Unpair").GetValue();
                        }
                        Destroy(rcm);
                    }
                }
            }
        }
        private void enableCommsSpawner()
        {
            foreach (var rc in Resources.FindObjectsOfTypeAll<CommsRadioController>())
            {
                rc.cheatModeOverride = true;
                if (rc.gameObject.scene.name != null) rc.UpdateModesAvailability();
            }
        }

        private void disableCommsSpawner()
        {
            foreach (var rc in Resources.FindObjectsOfTypeAll<CommsRadioController>())//FindObjectsOfType<CommsRadioController>())
            {
                rc.cheatModeOverride = false;
                if (rc.gameObject.scene.name != null) rc.UpdateModesAvailability();
            }
        }

        private void enableFreeCaboose()
        {
            Globals.G.GameParams.WorkTrainSummonMaxPrice = 0;
        }

        private void disableFreeCaboose()
        {
            Globals.G.GameParams.WorkTrainSummonMaxPrice = WorkTrainSummonMaxPrice;
        }

        private void enableFreeRerail()
        {
            Globals.G.GameParams.RerailMaxPrice = 0;
        }

        private void disableFreeRerail()
        {
            Globals.G.GameParams.RerailMaxPrice = RerailMaxPrice;
        }

        private void enableFreeClear()
        {
            Globals.G.GameParams.DeleteCarMaxPrice = 0;
        }

        private void disableFreeClear()
        {
            Globals.G.GameParams.DeleteCarMaxPrice = DeleteCarMaxPrice;
        }
        private void disableDerail()
        {
            Globals.G.GameParams.DerailStressThreshold = float.PositiveInfinity;
        }

        private void enableDerail()
        {
            Globals.G.GameParams.DerailStressThreshold = Globals.G.GameParams.defaultStressThreshold;
        }
    }

    public class UtilitiesModSettings
    {
        private const string CHEATS_SECTION = "Cheats";

        public readonly ConfigEntry<bool> RemoteControlDE6;
        public readonly ConfigEntry<bool> CommsRadioSpawner;
        public readonly ConfigEntry<bool> FreeCaboose;
        public readonly ConfigEntry<bool> FreeRerail;
        public readonly ConfigEntry<bool> FreeClear;
        public readonly ConfigEntry<bool> DisableDerailment;

        public UtilitiesModSettings(UtilitiesMod plugin)
        {
            RemoteControlDE6 = plugin.Config.Bind(CHEATS_SECTION, "RemoteControlDE6", false, "Enabled Remote Controller for DE6");
            CommsRadioSpawner = plugin.Config.Bind(CHEATS_SECTION, "CommsRadioSpawner", false, "Allows spawning and cargo from comms menu");
            FreeCaboose = plugin.Config.Bind(CHEATS_SECTION, "FreeCaboose", false, "Allows spawning Caboose for free");
            FreeRerail = plugin.Config.Bind(CHEATS_SECTION, "FreeRerail", false, "Allows rerailing for free");
            FreeClear = plugin.Config.Bind(CHEATS_SECTION, "FreeClear", false, "Allows clearing traincars for free");
            DisableDerailment = plugin.Config.Bind(CHEATS_SECTION, "DisableDerailment", false, "Disable Derailment");
        }
    }

    public class Utils
    {
        public static GameObject FindPrefab(string name)
        {
            var gos = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (var go in gos)
            {
                if (go.name.StartsWith(name) && go.activeInHierarchy == false && go.transform.parent == null)
                {
                    return go;
                }
            }

            return null;
        }
    }
}
