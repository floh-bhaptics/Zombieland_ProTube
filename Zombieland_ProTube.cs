using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using MelonLoader;
using HarmonyLib;
using System.IO;
using System.Threading;

[assembly: MelonInfo(typeof(Zombieland_ProTube.Zombieland_ProTube), "Zombieland_ProTube", "1.0.0", "Florian Fahrenberger")]
[assembly: MelonGame("XR Games", "zombieland_vr_headshot_fever")]


namespace Zombieland_ProTube
{
    public class Zombieland_ProTube : MelonMod
    {
        public static string configPath = Directory.GetCurrentDirectory() + "\\UserData\\";
        public static bool dualWield = false;

        public override void OnInitializeMelon()
        {
            InitializeProTube();
        }

        public static void saveChannel(string channelName, string proTubeName)
        {
            string fileName = configPath + channelName + ".pro";
            File.WriteAllText(fileName, proTubeName, Encoding.UTF8);
        }

        public static string readChannel(string channelName)
        {
            string fileName = configPath + channelName + ".pro";
            if (!File.Exists(fileName)) return "";
            return File.ReadAllText(fileName, Encoding.UTF8);
        }

        public static void dualWieldSort()
        {
            ForceTubeVRInterface.FTChannelFile myChannels = JsonConvert.DeserializeObject<ForceTubeVRInterface.FTChannelFile>(ForceTubeVRInterface.ListChannels());
            var pistol1 = myChannels.channels.pistol1;
            var pistol2 = myChannels.channels.pistol2;
            if ((pistol1.Count > 0) && (pistol2.Count > 0))
            {
                dualWield = true;
                MelonLogger.Msg("Two ProTube devices detected, player is dual wielding.");
                if ((readChannel("rightHand") == "") || (readChannel("leftHand") == ""))
                {
                    MelonLogger.Msg("No configuration files found, saving current right and left hand pistols.");
                    saveChannel("rightHand", pistol1[0].name);
                    saveChannel("leftHand", pistol2[0].name);
                }
                else
                {
                    string rightHand = readChannel("rightHand");
                    string leftHand = readChannel("leftHand");
                    MelonLogger.Msg("Found and loaded configuration. Right hand: " + rightHand + ", Left hand: " + leftHand);
                    // Channels 4 and 5 are ForceTubeVRChannel.pistol1 and pistol2
                    ForceTubeVRInterface.ClearChannel(4);
                    ForceTubeVRInterface.ClearChannel(5);
                    ForceTubeVRInterface.AddToChannel(4, rightHand);
                    ForceTubeVRInterface.AddToChannel(5, leftHand);
                }
            }
        }

        private static void InitializeProTube()
        {
            MelonLogger.Msg("Initializing ProTube gear...");
            ForceTubeVRInterface.InitAsync(true);
            Thread.Sleep(10000);
            dualWieldSort();
        }

        [HarmonyPatch(typeof(Zombieland.Gameplay.Player.AbstractPlayerGunBehaviour), "HandleAnyGunFired", new Type[] { typeof(Zombieland.Gameplay.Weapons.Ammunition.Ammo) })]
        public class bhaptics_FireGun
        {
            [HarmonyPostfix]
            public static void Postfix(Zombieland.Gameplay.Player.AbstractPlayerGunBehaviour __instance, Zombieland.Gameplay.Weapons.Ammunition.Ammo ammo)
            {
                bool isPrimaryGun = false;
                bool primaryIsRight = __instance.PrimaryHand.IsRightHand;
                byte kickPower = 200;
                if (ammo.FiringGun == __instance.PrimaryGun)
                {
                    isPrimaryGun = true;
                    if (__instance.PrimaryGun.GunId == Zombieland.Config.GunIDs.HUNTER_Z) { kickPower = 220; }
                }
                else
                {
                    if (__instance.SecondaryGun.GunId == Zombieland.Config.GunIDs.DOUBLE_MAD) { kickPower = 255; }
                    if (__instance.SecondaryGun.GunId == Zombieland.Config.GunIDs.VANILLI_M4) { kickPower = 240; }
                }
                bool isRightHand = !(isPrimaryGun ^ primaryIsRight);
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                if (!isRightHand) myChannel = ForceTubeVRChannel.pistol2;
                ForceTubeVRInterface.Kick(kickPower, myChannel);
                if (kickPower > 210) ForceTubeVRInterface.Rumble(200, 30f, myChannel);
            }
        }


    }
}
