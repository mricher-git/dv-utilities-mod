using BepInEx;
using DV;
using DV.Damage;
using DV.Utils;
using DV.InventorySystem;
using DV.ThingTypes;
using DV.Simulation.Cars;
using UnityEngine;

namespace UtilitiesMod
{
    internal static class PluginInfo
    {
        public const string Guid = "UtilitiesMod";
        public const string Name = "Utilities Mod";
        public const string Version = "0.1.0";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class UtilitiesMod : BaseUnityPlugin
    {
        public static UtilitiesMod Instance { get; private set; }
        //public static UtilitiesModSettings Settings { get; private set; }
        private static bool showGui = false;
        private static GUIStyle buttonStyle = new GUIStyle() { fontSize = 8 };
        public void Awake()
        {
            if (Instance != null)
            {
                Logger.LogFatal("Utilities is already loaded!");
                Destroy(this);
                return;
            }

            Instance = this;

            //Settings = new UtilitiesModSettings(this);

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
                GUILayout.BeginArea(new Rect(20, 20, 250, 500));
                GUILayout.BeginVertical("Utilities", GUI.skin.window);

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Money: ");
                GUILayout.TextField("$" + SingletonBehaviour<Inventory>.Instance.PlayerMoney.ToString("N"), new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleRight });
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

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }
    }
}
