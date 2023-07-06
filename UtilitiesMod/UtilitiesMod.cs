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
        public static bool Debug = true;
        private static bool WheelslipAllowed;
        private static bool WheelSlideAllowed;
        private static float ResourceConsumptionModifier;
        private static GameObject DE6Prefab;
        private static float RerailMaxPrice;
        private static float DeleteCarMaxPrice;
        private static float WorkTrainSummonMaxPrice;

        private static bool showGui = false;
        private static GUIStyle buttonStyle = new GUIStyle() { fontSize = 8 };
        private Rect ButtonRect = new Rect(0, 30, 20, 20);
        private Rect WindowRect = new Rect(20, 30, 250, 0);
        private Vector2 ScrollPosition;
        private Rect ScrollRect;

        void Start()
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

            // For ScriptEngine
            if (WorldStreamingInit.IsLoaded) OnLoadingFinished();

            if (Debug)
            {
                SingletonBehaviour<DevGUI>.Instance.gameObject.SetActive(true);
            }
        }

        // For ScriptEngine
        void OnDestroy()
        {
            if (UnloadWatcher.isQuitting || UnloadWatcher.isUnloading)
            {
                return;
            }

            WorldStreamingInit.LoadingFinished -= OnLoadingFinished;
            UnloadWatcher.UnloadRequested -= UnloadRequested;

            if (Settings.NoWheelslip.Value) disableNoWheelslip();
            if (Settings.NoWheelSlide.Value) disableNoWheelSlide();
            if (Settings.UnlimitedResources.Value) disableUnlimitedResources();
            if (Settings.RemoteControlDE6.Value) disableDE6Remote();
            if (Settings.CommsRadioSpawner.Value) disableCommsSpawner();
            if (Settings.FreeCaboose.Value) disableFreeCaboose();
            if (Settings.FreeRerail.Value) disableFreeRerail();
            if (Settings.FreeClear.Value) disableFreeClear();
            if (Settings.DisableDerailment.Value) disableNoDerail();
        }

        private void OnLoadingFinished()
        {
            StartCoroutine(this.InitCoro());
        }

        private void UnloadRequested()
        {
        }

        private IEnumerator InitCoro()
        {
            while (!AStartGameData.carsAndJobsLoadingFinished || !SingletonBehaviour<StartingItemsController>.Instance.itemsLoaded)
            {
                yield return null;
            }
            Instance.Logger.LogInfo("Initializing Utilities Mod");
            LogDebug("Rerail:" + Globals.G.GameParams.RerailMaxPrice);
            LogDebug("Delete:" + Globals.G.GameParams.DeleteCarMaxPrice);
            LogDebug("Rerail:" + Globals.G.GameParams.WorkTrainSummonMaxPrice);

            WheelslipAllowed = Globals.G.GameParams.WheelslipAllowed;
            WheelSlideAllowed = Globals.G.GameParams.WheelSlideAllowed;
            ResourceConsumptionModifier = Globals.G.GameParams.ResourceConsumptionModifier;
            RerailMaxPrice = Globals.G.GameParams.RerailMaxPrice;
            DeleteCarMaxPrice = Globals.G.GameParams.DeleteCarMaxPrice;
            WorkTrainSummonMaxPrice = Globals.G.GameParams.WorkTrainSummonMaxPrice;

            if (Settings.NoWheelslip.Value) enableNoWheelslip();
            if (Settings.NoWheelSlide.Value) enableNoWheelSlide();
            if (Settings.UnlimitedResources.Value) enableUnlimitedResources();
            if (Settings.RemoteControlDE6.Value) enableDE6Remote();
            if (Settings.CommsRadioSpawner.Value) enableCommsSpawner();
            if (Settings.FreeCaboose.Value) enableFreeCaboose();
            if (Settings.FreeRerail.Value) enableFreeRerail();
            if (Settings.FreeClear.Value) enableFreeClear();
            if (Settings.DisableDerailment.Value) enableNoDerail();

            yield break;
        }

        public static void LogError(string message)
        {
            Instance.Logger.LogError(message);
        }
        public static void LogDebug(string message)
        {
            if (Debug) Instance.Logger.LogDebug(message);
        }

        void OnGUI()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                showGui = false;
                return;
            }

            if (GUI.Button(ButtonRect, "U", new GUIStyle(GUI.skin.button) { fontSize = 10 })) showGui = !showGui;

            if (showGui)
            {
                WindowRect = GUILayout.Window(555, WindowRect, Window, "Utilities");
            }
        }

        void Window(int windowId)
        {
            GUIStyle centeredLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Width(250 + GUI.skin.verticalScrollbar.fixedWidth), GUILayout.Height(ScrollRect.height+GUI.skin.box.margin.vertical), GUILayout.MaxHeight(Screen.height-130));//GUILayout.Height(ScrollRect.height+GUI.skin.scrollView.margin.vertical*2), GUILayout.MaxHeight(Screen.width-100-30));//, (WindowRect.height > Screen.height - 100) ? GUILayout.Height(Screen.height - 100) : null);
            GUILayout.BeginVertical();
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
                    SingletonBehaviour<Inventory>.Instance.AddMoney((double)1000);
                if (GUILayout.Button("> $10k"))
                    SingletonBehaviour<Inventory>.Instance.AddMoney((double)10000);
                if (GUILayout.Button("> $100k"))
                    SingletonBehaviour<Inventory>.Instance.AddMoney((double)100000);
                if (GUILayout.Button("> $1M"))
                    SingletonBehaviour<Inventory>.Instance.AddMoney((double)1000000);
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("< $1k"))
                    SingletonBehaviour<Inventory>.Instance.RemoveMoney((double)1000);
                if (GUILayout.Button("< $10k"))
                    SingletonBehaviour<Inventory>.Instance.RemoveMoney((double)10000);
                if (GUILayout.Button("< $100k"))
                    SingletonBehaviour<Inventory>.Instance.RemoveMoney((double)100000);
                if (GUILayout.Button("< $1M"))
                    SingletonBehaviour<Inventory>.Instance.RemoveMoney((double)1000000);
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
                GUILayout.BeginHorizontal();
                bool wheelslip = GUILayout.Toggle(Settings.NoWheelslip.Value, "Disable Wheelslip");
                if (wheelslip != Settings.NoWheelslip.Value)
                {
                    Settings.NoWheelslip.Value = wheelslip;
                    if (wheelslip)
                        enableNoWheelslip();
                    else
                        disableNoWheelslip();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                bool wheelSlide = GUILayout.Toggle(Settings.NoWheelSlide.Value, "Disable Wheelslide");
                if (wheelSlide != Settings.NoWheelSlide.Value)
                {
                    Settings.NoWheelSlide.Value = wheelSlide;
                    if (wheelSlide)
                        enableNoWheelSlide();
                    else
                        disableNoWheelSlide();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                bool unlimitedResources = GUILayout.Toggle(Settings.UnlimitedResources.Value, "Unlimited Resources");
                if (unlimitedResources != Settings.UnlimitedResources.Value)
                {
                    Settings.UnlimitedResources.Value = unlimitedResources;
                    if (unlimitedResources)
                        enableUnlimitedResources();
                    else
                        disableUnlimitedResources();
                }
                GUILayout.EndHorizontal();
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
                        enableDE6Remote();
                    else 
                        disableDE6Remote();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                bool commsSpawner = GUILayout.Toggle(Settings.CommsRadioSpawner.Value, "Comms Radio Spawner");
                if (commsSpawner != Settings.CommsRadioSpawner.Value)
                {
                    Settings.CommsRadioSpawner.Value = commsSpawner;
                    if (commsSpawner)
                        enableCommsSpawner();
                    else
                        disableCommsSpawner();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                bool freeCaboose = GUILayout.Toggle(Settings.FreeCaboose.Value, "Free Caboose");
                if (freeCaboose != Settings.FreeCaboose.Value)
                {
                    Settings.FreeCaboose.Value = freeCaboose;
                    if (freeCaboose)
                        enableFreeCaboose();
                    else
                        disableFreeCaboose();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                bool freeRerail = GUILayout.Toggle(Settings.FreeRerail.Value, "Free Rerail");
                if (freeRerail != Settings.FreeRerail.Value)
                {
                    Settings.FreeRerail.Value = freeRerail;
                    if (freeRerail)
                        enableFreeRerail();
                    else
                        disableFreeRerail();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                bool freeClear = GUILayout.Toggle(Settings.FreeClear.Value, "Free Clear/Delete");
                if (freeClear != Settings.FreeClear.Value)
                {
                    Settings.FreeClear.Value = freeClear;
                    if (freeClear)
                        enableFreeClear();
                    else
                        disableFreeClear();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                bool derail = GUILayout.Toggle(Settings.DisableDerailment.Value, "No Derailment");
                if (derail != Settings.DisableDerailment.Value)
                {
                    Settings.DisableDerailment.Value = derail;
                    if (derail)
                        enableNoDerail();
                    else
                        disableNoDerail();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndVertical();
            ScrollRect = GUILayoutUtility.GetLastRect();
            GUILayout.EndScrollView();
        }
        //gameParams.ResourceConsumptionModifier
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

        private void enableNoWheelslip()
        {
            Globals.G.GameParams.WheelslipAllowed = false;
        }

        private void disableNoWheelslip()
        {
            Globals.G.GameParams.WheelslipAllowed = WheelslipAllowed;
        }

        private void enableNoWheelSlide()
        {
            Globals.G.GameParams.WheelSlideAllowed = false;
        }

        private void disableNoWheelSlide()
        {
            Globals.G.GameParams.WheelSlideAllowed = WheelSlideAllowed;
        }

        private void enableUnlimitedResources()
        {
            Globals.G.GameParams.ResourceConsumptionModifier = 0;
        }

        private void disableUnlimitedResources()
        {
            Globals.G.GameParams.ResourceConsumptionModifier = ResourceConsumptionModifier;
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
        private void enableNoDerail()
        {
            Globals.G.GameParams.DerailStressThreshold = float.PositiveInfinity;
        }

        private void disableNoDerail()
        {
            Globals.G.GameParams.DerailStressThreshold = Globals.G.GameParams.defaultStressThreshold;
        }
    }

    public class UtilitiesModSettings
    {
        private const string CHEATS_SECTION = "Cheats";

        public readonly ConfigEntry<bool> NoWheelslip;
        public readonly ConfigEntry<bool> NoWheelSlide;
        public readonly ConfigEntry<bool> UnlimitedResources;
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
            NoWheelslip = plugin.Config.Bind(CHEATS_SECTION, "NoWheelslip", false, "Disable Wheelslip");
            NoWheelSlide = plugin.Config.Bind(CHEATS_SECTION, "NoWheelSlide", false, "Disable WheelSlide");
            UnlimitedResources = plugin.Config.Bind(CHEATS_SECTION, "UnlimitedResources", false, "Unlimited Resources (Sand/Oil/Fuel/Coal/Water)");
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
