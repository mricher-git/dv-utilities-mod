using BepInEx;
using BepInEx.Configuration;
using DV;
using DV.Damage;
using DV.Utils;
using DV.InventorySystem;
using DV.RemoteControls;
using DV.ThingTypes;
using DV.Simulation.Cars;
using UnityEngine;

namespace UtilitiesMod
{
    internal static class PluginInfo
    {
        public const string Guid = "UtilitiesMod";
        public const string Name = "Utilities Mod";
        public const string Version = "0.2.0";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class UtilitiesMod : BaseUnityPlugin
    {
        public static UtilitiesMod Instance { get; private set; }

        public static UtilitiesModSettings Settings { get; private set; }
        private static bool showGui = false;
        private static GUIStyle buttonStyle = new GUIStyle() { fontSize = 8 };
        private GameObject DE6Prefab;

        public void Awake()
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
            if (Settings.RemoteControlDE6.Value)
            {
                enableDE6Remote();
            }
        }

        public static void Error(string message)
        {
            Instance.Logger.LogError(message);
        }

        private void OnLoadingFinished()
        {
            if (Settings.DisableDerailment.Value) disableDerail();
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
                GUILayout.Label("Money", centeredLabel, GUILayout.ExpandWidth(true));
                GUILayout.BeginHorizontal();
                GUILayout.Label("Money: ");
                GUILayout.Label("$" + SingletonBehaviour<Inventory>.Instance.PlayerMoney.ToString("N"), new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleRight });
                //GUILayout.Button("Set");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("> $1k"))
                {
                    //double playerMoney = SingletonBehaviour<Inventory>.Instance.PlayerMoney;
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
                    //double playerMoney = SingletonBehaviour<Inventory>.Instance.PlayerMoney;
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
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUI.skin.box);
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
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUI.skin.box);
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
                GUILayout.EndVertical();

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Cheats", centeredLabel);
                GUILayout.BeginHorizontal();
                //GUILayout.Label("Derailment");

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
                    Object.Destroy(DE6Prefab.GetComponent<RemoteControllerModule>());
                    DE6Prefab.GetComponent<SimController>().remoteController = null;
                }
            }
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

        public readonly ConfigEntry<bool> DisableDerailment;
        public readonly ConfigEntry<bool> RemoteControlDE6;

        public UtilitiesModSettings(UtilitiesMod plugin)
        {
            DisableDerailment = plugin.Config.Bind(CHEATS_SECTION, "DisableDerailment", false, "Disable Derailment");
            RemoteControlDE6 = plugin.Config.Bind(CHEATS_SECTION, "RemoteControlDE6", false, "Enabled Remote Controller for DE6");
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
