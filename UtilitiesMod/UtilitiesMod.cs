using DV;
using DV.Damage;
using DV.InventorySystem;
using DV.RemoteControls;
using DV.Simulation.Cars;
using DV.ThingTypes;
using DV.Utils;
using DV.WeatherSystem;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UtilitiesMod
{

    public class UtilitiesMod : MonoBehaviour
    {
        public const string Version = "1.1.0";

        // Cached original values
        private GameObject DE6Prefab;
        private bool originalWheelslipAllowed;
        private bool originalWheelSlideAllowed;
        private float originalResourceConsumptionModifier;
        private float originalRerailMaxPrice;
        private float originalDeleteCarMaxPrice;
        private float originalWorkTrainSummonMaxPrice;

        // GUI vars
        private static readonly GUIStyle buttonStyle = new GUIStyle() { fontSize = 8 };
        private bool showGui = false;
        private Rect buttonRect = new Rect(0, 30, 20, 20);
        private Rect windowRect = new Rect(20, 30, 0, 0);
        private Vector2 scrollPosition;
        private Rect scrollRect;
        private Rect WeatherPresetRect;
        private int weatherPreset = -1;
        private bool weatherPresetShow = false;
        private Rect last;

        internal UMM.Loader.UtilitiesModSettings Settings;

        void Start()
        {
            DE6Prefab = Utils.FindPrefab("LocoDE6");
            if (DE6Prefab == null) UMM.Loader.LogError("DE6 Prefab not found");

            WorldStreamingInit.LoadingFinished += OnLoadingFinished;
            UnloadWatcher.UnloadRequested += UnloadRequested;

            if (WorldStreamingInit.IsLoaded) OnLoadingFinished();
        }

        void OnDestroy()
        {
            if (UnloadWatcher.isQuitting || UnloadWatcher.isUnloading)
            {
                showGui = false;
                return;
            }

            WorldStreamingInit.LoadingFinished -= OnLoadingFinished;
            UnloadWatcher.UnloadRequested -= UnloadRequested;

            if (Settings.NoWheelslip) DisableNoWheelslip();
            if (Settings.NoWheelSlide) DisableNoWheelSlide();
            if (Settings.UnlimitedResources) DisableUnlimitedResources();
            if (Settings.DisableDerailment) DisableNoDerail();
            if (Settings.RemoteControlDE6) DisableDE6Remote();
            if (Settings.CommsRadioSpawner) DisableCommsSpawner();
            if (Settings.FreeCaboose) DisableFreeCaboose();
            if (Settings.FreeRerail) DisableFreeRerail();
            if (Settings.FreeClear) DisableFreeClear();
        }

        private void OnLoadingFinished()
        {
            StartCoroutine(this.InitCoro());
        }

        private void UnloadRequested()
        {
            showGui = false;
        }

        private IEnumerator InitCoro()
        {
            while (!AStartGameData.carsAndJobsLoadingFinished || !SingletonBehaviour<StartingItemsController>.Instance.itemsLoaded)
            {
                yield return null;
            }
            UMM.Loader.Log("Initializing Utilities Mod");
            UMM.Loader.LogDebug("Rerail:" + Globals.G.GameParams.RerailMaxPrice);
            UMM.Loader.LogDebug("Delete:" + Globals.G.GameParams.DeleteCarMaxPrice);
            UMM.Loader.LogDebug("Rerail:" + Globals.G.GameParams.WorkTrainSummonMaxPrice);

            originalWheelslipAllowed = Globals.G.GameParams.WheelslipAllowed;
            originalWheelSlideAllowed = Globals.G.GameParams.WheelSlideAllowed;
            originalResourceConsumptionModifier = Globals.G.GameParams.ResourceConsumptionModifier;
            originalRerailMaxPrice = Globals.G.GameParams.RerailMaxPrice;
            originalDeleteCarMaxPrice = Globals.G.GameParams.DeleteCarMaxPrice;
            originalWorkTrainSummonMaxPrice = Globals.G.GameParams.WorkTrainSummonMaxPrice;

            if (Settings.NoWheelslip) EnableNoWheelslip();
            if (Settings.NoWheelSlide) EnableNoWheelSlide();
            if (Settings.UnlimitedResources) EnableUnlimitedResources();
            if (Settings.DisableDerailment) EnableNoDerail();
            if (Settings.RemoteControlDE6) EnableDE6Remote();
            if (Settings.CommsRadioSpawner) EnableCommsSpawner();
            if (Settings.FreeCaboose) EnableFreeCaboose();
            if (Settings.FreeRerail) EnableFreeRerail();
            if (Settings.FreeClear) EnableFreeClear();

            yield break;
        }

        void OnGUI()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                showGui = false;
                return;
            }

            if (GUI.Button(buttonRect, "U", new GUIStyle(GUI.skin.button) { fontSize = 10, clipping = TextClipping.Overflow})) showGui = !showGui;

            if (showGui)
            {
                windowRect = GUILayout.Window(555, windowRect, Window, "Utilities");

                if (weatherPresetShow)
                {
                    if (WeatherPresetRect.width > 0)
                    {
                        WeatherPresetRect.x = windowRect.x + windowRect.width;
                        WeatherPresetRect.y = windowRect.y + windowRect.height - WeatherPresetRect.height;
                    }
                    WeatherPresetRect = GUILayout.Window(556, WeatherPresetRect, WeatherPresetWindow, "Select Weather Preset");
                }
            }
        }

        void Window(int windowId)
        {
            GUIStyle centeredLabel = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(270 + GUI.skin.verticalScrollbar.fixedWidth), GUILayout.Height(scrollRect.height + GUI.skin.box.margin.vertical), GUILayout.MaxHeight(Screen.height - 130));
            GUILayout.BeginVertical();
            {
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
                        ResourceContainerController resourceContainerController = component?.resourceContainerController;
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
                            EnableNoWheelslip();
                        else
                            DisableNoWheelslip();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool wheelSlide = GUILayout.Toggle(Settings.NoWheelSlide, "Disable Wheelslide");
                    if (wheelSlide != Settings.NoWheelSlide)
                    {
                        Settings.NoWheelSlide = wheelSlide;
                        if (wheelSlide)
                            EnableNoWheelSlide();
                        else
                            DisableNoWheelSlide();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool unlimitedResources = GUILayout.Toggle(Settings.UnlimitedResources, "Unlimited Resources");
                    if (unlimitedResources != Settings.UnlimitedResources)
                    {
                        Settings.UnlimitedResources = unlimitedResources;
                        if (unlimitedResources)
                            EnableUnlimitedResources();
                        else
                            DisableUnlimitedResources();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool derail = GUILayout.Toggle(Settings.DisableDerailment, "No Derailment");
                    if (derail != Settings.DisableDerailment)
                    {
                        Settings.DisableDerailment = derail;
                        if (derail)
                            EnableNoDerail();
                        else
                            DisableNoDerail();
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
                            EnableDE6Remote();
                        else
                            DisableDE6Remote();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool commsSpawner = GUILayout.Toggle(Settings.CommsRadioSpawner, "Comms Radio Spawner");
                    if (commsSpawner != Settings.CommsRadioSpawner)
                    {
                        Settings.CommsRadioSpawner = commsSpawner;
                        if (commsSpawner)
                            EnableCommsSpawner();
                        else
                            DisableCommsSpawner();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool freeCaboose = GUILayout.Toggle(Settings.FreeCaboose, "Free Caboose");
                    if (freeCaboose != Settings.FreeCaboose)
                    {
                        Settings.FreeCaboose = freeCaboose;
                        if (freeCaboose)
                            EnableFreeCaboose();
                        else
                            DisableFreeCaboose();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool freeRerail = GUILayout.Toggle(Settings.FreeRerail, "Free Rerail");
                    if (freeRerail != Settings.FreeRerail)
                    {
                        Settings.FreeRerail = freeRerail;
                        if (freeRerail)
                            EnableFreeRerail();
                        else
                            DisableFreeRerail();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    bool freeClear = GUILayout.Toggle(Settings.FreeClear, "Free Clear/Delete");
                    if (freeClear != Settings.FreeClear)
                    {
                        Settings.FreeClear = freeClear;
                        if (freeClear)
                            EnableFreeClear();
                        else
                            DisableFreeClear();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUI.skin.box);
                {
                    GUILayout.Label("Environment", centeredLabel);
                    GUILayout.BeginHorizontal();
                    WeatherDriver.Instance.manager.todTime.ProgressTime = !GUILayout.Toggle(!WeatherDriver.Instance.manager.todTime.ProgressTime, "Lock Time", GUILayout.Width(80));
                    GUILayout.Label(WeatherDriver.Instance.manager.DateTime.ToString("hh:mm tt"), centeredLabel, GUILayout.ExpandWidth(true));
                    GUILayout.Space(80);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("12A")) WeatherDriver.Instance.manager.SetTimeOfDay(0f);
                    if (GUILayout.Button("6A")) WeatherDriver.Instance.manager.SetTimeOfDay(0.25f);
                    if (GUILayout.Button("12P")) WeatherDriver.Instance.manager.SetTimeOfDay(0.50f);
                    if (GUILayout.Button("6P")) WeatherDriver.Instance.manager.SetTimeOfDay(0.75f);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("<", GUILayout.ExpandWidth(false))) WeatherDriver.Instance.manager.AdvanceTime(-3600);
                    var time = GUILayout.HorizontalSlider(WeatherDriver.Instance.manager.timeOfDay, 0f, 1f);
                    if (time != WeatherDriver.Instance.manager.timeOfDay) WeatherDriver.Instance.manager.SetTimeOfDay(Mathf.Min(time, 1f - 1f / 1440f));
                    if (GUILayout.Button(">", GUILayout.ExpandWidth(false))) WeatherDriver.Instance.manager.AdvanceTime(+3600);
                    GUILayout.EndHorizontal();
                    
                    GUILayout.Label("Weather Preset");
                    if (GUILayout.Button(WeatherDriver.Instance.presetOverride ? WeatherDriver.Instance.presetOverride?.name.Remove(0, 4) : "None"))
                    {
                        weatherPresetShow = true;
                        weatherPreset = -1;
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint)
            {
                scrollRect = GUILayoutUtility.GetLastRect();
            }
            GUILayout.EndScrollView();
        }

        private void WeatherPresetWindow(int windowId)
        {
            weatherPreset = GUILayout.SelectionGrid(weatherPreset, WeatherDriver.Instance.Pack.presets.Select(x => x.name.Remove(0, 4)).Prepend("None").ToArray(), 1);
            if (weatherPreset != -1)
            {
                weatherPresetShow = false;
                if (weatherPreset == 0)
                    WeatherDriver.Instance.SetPreset(null);
                else
                    WeatherDriver.Instance.SetPreset(weatherPreset - 1);
                weatherPreset = -1;
            }
        }

        private void EnableDE6Remote()
        {
            if (DE6Prefab == null)
            {
                UMM.Loader.LogError("DE6 Prefab not found");
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

        private void DisableDE6Remote()
        {
            if (DE6Prefab == null)
            {
                UMM.Loader.LogError("DE6 Prefab not found");
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

        private void EnableNoWheelslip()
        {
            Globals.G.GameParams.WheelslipAllowed = false;
        }

        private void DisableNoWheelslip()
        {
            Globals.G.GameParams.WheelslipAllowed = originalWheelslipAllowed;
        }

        private void EnableNoWheelSlide()
        {
            Globals.G.GameParams.WheelSlideAllowed = false;
        }

        private void DisableNoWheelSlide()
        {
            Globals.G.GameParams.WheelSlideAllowed = originalWheelSlideAllowed;
        }

        private void EnableUnlimitedResources()
        {
            Globals.G.GameParams.ResourceConsumptionModifier = 0;
        }

        private void DisableUnlimitedResources()
        {
            Globals.G.GameParams.ResourceConsumptionModifier = originalResourceConsumptionModifier;
        }

        private void EnableNoDerail()
        {
            Globals.G.GameParams.DerailStressThreshold = float.PositiveInfinity;
        }

        private void DisableNoDerail()
        {
            Globals.G.GameParams.DerailStressThreshold = Globals.G.GameParams.defaultStressThreshold;
        }

        private void EnableCommsSpawner()
        {
            foreach (var rc in Resources.FindObjectsOfTypeAll<CommsRadioController>())
            {
                rc.cheatModeOverride = true;
                if (rc.gameObject.scene.name != null) rc.UpdateModesAvailability();
            }
        }

        private void DisableCommsSpawner()
        {
            foreach (var rc in Resources.FindObjectsOfTypeAll<CommsRadioController>())
            {
                rc.cheatModeOverride = false;
                if (rc.gameObject.scene.name != null) rc.UpdateModesAvailability();
            }
        }

        private void EnableFreeCaboose()
        {
            Globals.G.GameParams.WorkTrainSummonMaxPrice = 0;
        }

        private void DisableFreeCaboose()
        {
            Globals.G.GameParams.WorkTrainSummonMaxPrice = originalWorkTrainSummonMaxPrice;
        }

        private void EnableFreeRerail()
        {
            Globals.G.GameParams.RerailMaxPrice = 0;
        }

        private void DisableFreeRerail()
        {
            Globals.G.GameParams.RerailMaxPrice = originalRerailMaxPrice;
        }

        private void EnableFreeClear()
        {
            Globals.G.GameParams.DeleteCarMaxPrice = 0;
        }

        private void DisableFreeClear()
        {
            Globals.G.GameParams.DeleteCarMaxPrice = originalDeleteCarMaxPrice;
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

    public class Utils
    {
        public static GameObject FindPrefab(string name)
        {
            var gos = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (var go in gos)
            {
                if (go.name == name && go.activeInHierarchy == false && go.transform.parent == null)
                {
                    return go;
                }
            }

            return null;
        }
    }
}