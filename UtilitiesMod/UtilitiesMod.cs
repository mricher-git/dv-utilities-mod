using DV;
using DV.Damage;
using DV.InventorySystem;
using DV.RemoteControls;
using DV.Simulation.Cars;
using DV.ThingTypes;
using DV.Utils;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace UtilitiesMod
{
    public static class PluginInfo
    {
        public const string Guid = "UtilitiesMod";
        public const string Name = "DV Utilities";
        public const string Version = "1.0.0";
    }

#if DEBUG
[EnableReloading]
#endif
    public static class UtilitiesModLoader
    {
        public static UnityModManager.ModEntry ModEntry { get; private set; }
        public static UtilitiesMod Instance { get; private set; }

        private static UtilitiesModSettings Settings;

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

        public class UtilitiesMod : MonoBehaviour
        {
            // Cached original values
            private bool WheelslipAllowed;
            private bool WheelSlideAllowed;
            private float ResourceConsumptionModifier;
            private GameObject DE6Prefab;
            private float RerailMaxPrice;
            private float DeleteCarMaxPrice;
            private float WorkTrainSummonMaxPrice;

            // GUI vars
            private static GUIStyle buttonStyle = new GUIStyle() { fontSize = 8 };
            private bool showGui = false;
            private Rect ButtonRect = new Rect(0, 30, 20, 20);
            private Rect WindowRect = new Rect(20, 30, 250, 0);
            private Vector2 ScrollPosition;
            private Rect ScrollRect;

            void Start()
            {
                DE6Prefab = Utils.FindPrefab("LocoDE6");
                if (DE6Prefab == null) LogError("DE6 Prefab not found");

                WorldStreamingInit.LoadingFinished += OnLoadingFinished;
                UnloadWatcher.UnloadRequested += UnloadRequested;

                // For ScriptEngine
                if (WorldStreamingInit.IsLoaded) OnLoadingFinished();
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

                if (Settings.NoWheelslip) disableNoWheelslip();
                if (Settings.NoWheelSlide) disableNoWheelSlide();
                if (Settings.UnlimitedResources) disableUnlimitedResources();
                if (Settings.DisableDerailment) disableNoDerail();
                if (Settings.RemoteControlDE6) disableDE6Remote();
                if (Settings.CommsRadioSpawner) disableCommsSpawner();
                if (Settings.FreeCaboose) disableFreeCaboose();
                if (Settings.FreeRerail) disableFreeRerail();
                if (Settings.FreeClear) disableFreeClear();
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
                Log("Initializing Utilities Mod");
                LogDebug("Rerail:" + Globals.G.GameParams.RerailMaxPrice);
                LogDebug("Delete:" + Globals.G.GameParams.DeleteCarMaxPrice);
                LogDebug("Rerail:" + Globals.G.GameParams.WorkTrainSummonMaxPrice);

                WheelslipAllowed = Globals.G.GameParams.WheelslipAllowed;
                WheelSlideAllowed = Globals.G.GameParams.WheelSlideAllowed;
                ResourceConsumptionModifier = Globals.G.GameParams.ResourceConsumptionModifier;
                RerailMaxPrice = Globals.G.GameParams.RerailMaxPrice;
                DeleteCarMaxPrice = Globals.G.GameParams.DeleteCarMaxPrice;
                WorkTrainSummonMaxPrice = Globals.G.GameParams.WorkTrainSummonMaxPrice;

                if (Settings.NoWheelslip) enableNoWheelslip();
                if (Settings.NoWheelSlide) enableNoWheelSlide();
                if (Settings.UnlimitedResources) enableUnlimitedResources();
                if (Settings.DisableDerailment) enableNoDerail();
                if (Settings.RemoteControlDE6) enableDE6Remote();
                if (Settings.CommsRadioSpawner) enableCommsSpawner();
                if (Settings.FreeCaboose) enableFreeCaboose();
                if (Settings.FreeRerail) enableFreeRerail();
                if (Settings.FreeClear) enableFreeClear();

                yield break;
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

                ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Width(250 + GUI.skin.verticalScrollbar.fixedWidth), GUILayout.Height(ScrollRect.height + GUI.skin.box.margin.vertical), GUILayout.MaxHeight(Screen.height - 130));//GUILayout.Height(ScrollRect.height+GUI.skin.scrollView.margin.vertical*2), GUILayout.MaxHeight(Screen.width-100-30));//, (WindowRect.height > Screen.height - 100) ? GUILayout.Height(Screen.height - 100) : null);
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
                    bool wheelslip = GUILayout.Toggle(Settings.NoWheelslip, "Disable Wheelslip");
                    if (wheelslip != Settings.NoWheelslip)
                    {
                        Settings.NoWheelslip = wheelslip;
                        if (wheelslip)
                            enableNoWheelslip();
                        else
                            disableNoWheelslip();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool wheelSlide = GUILayout.Toggle(Settings.NoWheelSlide, "Disable Wheelslide");
                    if (wheelSlide != Settings.NoWheelSlide)
                    {
                        Settings.NoWheelSlide = wheelSlide;
                        if (wheelSlide)
                            enableNoWheelSlide();
                        else
                            disableNoWheelSlide();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool unlimitedResources = GUILayout.Toggle(Settings.UnlimitedResources, "Unlimited Resources");
                    if (unlimitedResources != Settings.UnlimitedResources)
                    {
                        Settings.UnlimitedResources = unlimitedResources;
                        if (unlimitedResources)
                            enableUnlimitedResources();
                        else
                            disableUnlimitedResources();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool derail = GUILayout.Toggle(Settings.DisableDerailment, "No Derailment");
                    if (derail != Settings.DisableDerailment)
                    {
                        Settings.DisableDerailment = derail;
                        if (derail)
                            enableNoDerail();
                        else
                            disableNoDerail();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Cheats", centeredLabel);
                    GUILayout.BeginHorizontal();
                    bool remoteDE6 = GUILayout.Toggle(Settings.RemoteControlDE6, "Remote Controller for DE6");
                    if (remoteDE6 != Settings.RemoteControlDE6)
                    {
                        Settings.RemoteControlDE6 = remoteDE6;
                        if (remoteDE6)
                            enableDE6Remote();
                        else
                            disableDE6Remote();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool commsSpawner = GUILayout.Toggle(Settings.CommsRadioSpawner, "Comms Radio Spawner");
                    if (commsSpawner != Settings.CommsRadioSpawner)
                    {
                        Settings.CommsRadioSpawner = commsSpawner;
                        if (commsSpawner)
                            enableCommsSpawner();
                        else
                            disableCommsSpawner();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool freeCaboose = GUILayout.Toggle(Settings.FreeCaboose, "Free Caboose");
                    if (freeCaboose != Settings.FreeCaboose)
                    {
                        Settings.FreeCaboose = freeCaboose;
                        if (freeCaboose)
                            enableFreeCaboose();
                        else
                            disableFreeCaboose();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool freeRerail = GUILayout.Toggle(Settings.FreeRerail, "Free Rerail");
                    if (freeRerail != Settings.FreeRerail)
                    {
                        Settings.FreeRerail = freeRerail;
                        if (freeRerail)
                            enableFreeRerail();
                        else
                            disableFreeRerail();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool freeClear = GUILayout.Toggle(Settings.FreeClear, "Free Clear/Delete");
                    if (freeClear != Settings.FreeClear)
                    {
                        Settings.FreeClear = freeClear;
                        if (freeClear)
                            enableFreeClear();
                        else
                            disableFreeClear();
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
                    LogError("DE6 Prefab not found");
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

            private void enableNoDerail()
            {
                Globals.G.GameParams.DerailStressThreshold = float.PositiveInfinity;
            }

            private void disableNoDerail()
            {
                Globals.G.GameParams.DerailStressThreshold = Globals.G.GameParams.defaultStressThreshold;
            }

            private void disableDE6Remote()
            {
                if (DE6Prefab == null)
                {
                    LogError("DE6 Prefab not found");
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
                foreach (var rc in Resources.FindObjectsOfTypeAll<CommsRadioController>())
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

            public static void ApplyForceToTrain(float force)
            {
                SimController controller = null;
                TrainCar train = PlayerManager.Car?.GetComponent<TrainCar>();

                if (train == null) return;
                switch (PlayerManager.Car.carType)
                {
                    case TrainCarType.LocoShunter:
                    case TrainCarType.LocoDM3:
                    case TrainCarType.LocoDH4:
                    case TrainCarType.LocoDiesel:
                    case TrainCarType.LocoSteamHeavy:
                        {
                            controller = PlayerManager.Car.GetComponent<SimController>();
                        }
                        break;
                }
                if (controller == null) return;


                if (train.isEligibleForSleep)
                {
                    train.ForceOptimizationState(false, false);
                }

                float totalMass = 0;
                List<TrainCar> cars = train.trainset.cars;

                for (int i = 0; i < cars.Count; i++)
                {
                    totalMass += cars[i].massController.TotalMass;
                }
                var dir = force > 0 ? 1 : -1;
                float totalAppliedForcePerBogie = dir * (totalMass + Mathf.Abs(force)) / train.Bogies.Length;
                Bogie[] bogies = train.Bogies;
                for (int i = 0; i < bogies.Length; i++)
                {
                    bogies[i].ApplyForce(totalAppliedForcePerBogie);
                }
            }

            void Update()
            {
                const float force = 100000f;
                int dir = 1;
                if (Input.GetKey(KeyCode.F7))
                {
                    ApplyForceToTrain(-force * (Input.GetKey(KeyCode.LeftShift) ? 3 : 1));
                }
                if (Input.GetKey(KeyCode.F8))
                {
                    ApplyForceToTrain(force * (Input.GetKey(KeyCode.LeftShift) ? 3 : 1));
                }

            }
        }

        public class UtilitiesModSettings : UnityModManager.ModSettings
        {
            private const string CHEATS_SECTION = "Cheats";

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
}
