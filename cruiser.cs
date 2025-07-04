using UInt32 = System.UInt32;
using UInt64 = System.UInt64;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Unity.Netcode;

namespace kirby
{
	[BepInPlugin("4902.Cruiser_Additions", "Cruiser_Additions", "1.0.0")]
	public class ca : BaseUnityPlugin
	{
		public static readonly Harmony harmony = new Harmony("4902.Cruiser_Additions");

		public static ManualLogSource mls;

		public static ConfigEntry<bool> temp1_magnet;
		public static ConfigEntry<int>  temp2_moveitems;
		public static ConfigEntry<bool> temp2_scraponly;
		public static ConfigEntry<bool> temp3_engine;
		public static ConfigEntry<bool> temp3_radio;
		public static ConfigEntry<bool> temp4_light;
		public static ConfigEntry<bool> temp4_night;
		public static ConfigEntry<bool> temp5_scrap;
		public static ConfigEntry<int>  temp5_cruiser;
		public static ConfigEntry<int>  temp5_cruiseralt;
		public static ConfigEntry<bool> temp5_playwithtoy;
		public static ConfigEntry<bool> temp5_saveseeds;
		public static ConfigEntry<int>  temp5_millisecond;
		public static ConfigEntry<bool> temp5_percentages;
		public static ConfigEntry<bool> temp6_speedometer;
		public static ConfigEntry<bool> temp7_unlockables;
		public static ConfigEntry<bool> temp8_collide;
		public static ConfigEntry<bool> temp9_1up;
		public static ConfigEntry<bool> tempk_lever;
		public static ConfigEntry<int>  tempi_headlights;
		public static ConfigEntry<bool> tempr_input;
		public static ConfigEntry<Key>  tempb_keybind;

		public static cfg_bool cfg1_magnet = new cfg_bool();      //true
		public static cfg_int  cfg2_moveitems = new cfg_int();    //2
		public static cfg_bool cfg2_scraponly = new cfg_bool();   //true
		public static cfg_bool cfg3_engine = new cfg_bool();      //true
		public static cfg_bool cfg3_radio = new cfg_bool();       //true
		public static cfg_bool cfg4_light = new cfg_bool();       //true
		public static cfg_bool cfg4_night = new cfg_bool();       //true
		public static cfg_bool cfg5_scrap = new cfg_bool();       //true
		public static cfg_int  cfg5_cruiser = new cfg_int();      //40
		public static cfg_int  cfg5_cruiseralt = new cfg_int();   //40
		public static cfg_bool cfg5_playwithtoy = new cfg_bool(); //true
		public static cfg_bool cfg5_saveseeds = new cfg_bool();   //true
		public static cfg_int  cfg5_millisecond = new cfg_int();  //100
		public static cfg_bool cfg5_percentages = new cfg_bool(); //true
		public static cfg_bool cfg6_speedometer = new cfg_bool(); //true
		public static cfg_bool cfg7_unlockables = new cfg_bool(); //true
		public static cfg_bool cfg8_collide = new cfg_bool();     //true
		public static cfg_bool cfg9_1up = new cfg_bool();         //false
		public static cfg_bool cfgk_lever = new cfg_bool();       //true
		public static cfg_int  cfgi_headlights = new cfg_int();   //2
		public static cfg_bool cfgr_input = new cfg_bool();       //false
		public static cfg_key  cfgb_keybind = new cfg_key();      //None

		private void Awake()
		{
			temp1_magnet = Config.Bind("Cruiser+", "magnet_rotation", true, "[Magnet rotation]\nfixes the cruiser rotation when magneted to only be 90 or 270 (parallel to the ship). otherwise at certain angles (~225 or 315-360) it can be magneted to 180 or 360 and be partially inside the ship.\nhost must have this enabled for it to apply. all clients will see the fixed rotation if host has this enabled"); cfg1_magnet.Value = temp1_magnet.Value;
			temp2_moveitems = Config.Bind("Cruiser+", "move_items", 2, "[Move items]\nmove items from the cruiser to the ship when going into orbit. the items are only moved if they were collected (like with the magnet).\nthis is client sided so items are only shown to have been moved to players with this enabled.\nif one player has this enabled and another player has this disabled (or doesn't have this mod), the item positions will be different, however picking up the item/s will resync the position.\nin vanilla any collected items in the cruiser will be moved to the ship when leaving and rejoining after the game saved, so this feature is the same as vanilla except without having to leave and rejoin.\n1 = disabled\n2 = only enabled when you're host, or if the host has this enabled\n3 = always enabled"); cfg2_moveitems.Value = temp2_moveitems.Value;
			temp2_scraponly = Config.Bind("Cruiser+", "only_scrap", true, "[Only move scrap items]\nonly moves scrap and leaves other items in the cruiser.\nif the host has this mod this will be set to what the host has this set to"); cfg2_scraponly.Value = temp2_scraponly.Value;
			temp3_engine = Config.Bind("Cruiser+", "mute_engine", true, "[Mute engine audio]\nmute engine audio while in orbit"); cfg3_engine.Value = temp3_engine.Value;
			temp3_radio = Config.Bind("Cruiser+", "mute_radio", true, "[Mute radio audio]\nmute radio audio while in orbit"); cfg3_radio.Value = temp3_radio.Value;
			temp4_light = Config.Bind("Cruiser+", "light", true, "[Storage light]\nadds a light to the storage area of the cruiser"); cfg4_light.Value = temp4_light.Value;
			temp4_night = Config.Bind("Cruiser+", "light_switch", true, "[Storage light switch]\nadds a button in the storage area of the cruiser for turning on/off the storage light.\nturning the light on/off is synced with other players if the host has this mod"); cfg4_night.Value = temp4_night.Value;
			temp5_scrap = Config.Bind("Cruiser+", "scrap", true, "[Scrap items]\nwhether cruiser scrap items should be enabled or disabled.\nitems are a percentage based retexture of v-type engine, and are synced with other players if the synced percentages config is enabled"); cfg5_scrap.Value = temp5_scrap.Value;
			temp5_cruiser = Config.Bind("Cruiser+", "cruiser_percentage", 40, "[Cruiser item percentage]\npercentage that the cruiser item will replace v-type engine.\ninput = percentage, 40 is 40%"); cfg5_cruiser.Value = temp5_cruiser.Value;
			temp5_cruiseralt = Config.Bind("Cruiser+", "cruiser_alt_percentage", 40, "[Cruiser alt item percentage]\npercentage that the cruiser item will be an alternate version.\ninput = percentage, 40 is 40%"); cfg5_cruiseralt.Value = temp5_cruiseralt.Value;
			temp5_playwithtoy = Config.Bind("Cruiser+", "cruiser_item_interaction", true, "[Cruiser item interaction]\nadds interacting with the cruiser items by clicking while holding them to play an event.\nevents are a mix of audio and visual effects, there are about 49 events, some events have follow-up effects that are related to or continue the previous effect.\nhost must have this config and the scrap config enabled for the events to be synced between players. if host has either config disabled or doesn't have this mod then the events will play locally instead"); cfg5_playwithtoy.Value = temp5_playwithtoy.Value;
			temp5_saveseeds = Config.Bind("Cruiser+", "save/load", true, "[Save/load seeds]\nwhether the engine/cruiser item types should be saved to the save file. this saves the seed that determines the items type, so it will be the same when rejoining.\nloading a save file that has saved seeds without having the scrap config enabled or without having this enabled or while in lan isn't recommended as the saved seeds can be reset if the number of items is changed.\nsaved item types are synced with other players if the synced percentages config is enabled.\nscrap config must be enabled for this to be enabled"); cfg5_saveseeds.Value = temp5_saveseeds.Value;
			temp5_millisecond = Config.Bind("Cruiser+", "timer", 100, "[Client wait_timer]\nwhen joining a lobby as non-host engine items will wait to set their item type until the network message sent by the host has been received or the time spent waiting reached the maximum amount set by this config.\nthere will be a log message specifying if the network message was received first or the timer ended first. if the timer is often ending before the message from the host is being received then this should be increased.\nmin is 20, max is 4000. 100 is before the player spawn animation ends, 500 is about 10 seconds, 1000 is about 15 seconds"); cfg5_millisecond.Value = temp5_millisecond.Value;
			temp5_percentages = Config.Bind("Cruiser+", "sync", true, "[Synced percentages]\nautomatically sync config cruiser item percentages with the host. only disable if you're aware that disabling this can cause the cruiser item types to not be the same as other players if playing with others that have this mod"); cfg5_percentages.Value = temp5_percentages.Value;
			temp6_speedometer = Config.Bind("Cruiser+", "speedometer", true, "[Speedometer]\nadds a speedometer"); cfg6_speedometer.Value = temp6_speedometer.Value;
			temp7_unlockables = Config.Bind("Cruiser+", "fix", true, "[Cruiser position/rotation after joining]\nwhen joining a lobby with a cruiser, it will be on the magnet instead of on the opposite side of the ship.\nalso fixes the 'rotation quaternions must be unit length' error from the cruiser after joining"); cfg7_unlockables.Value = temp7_unlockables.Value;
			temp8_collide = Config.Bind("Cruiser+", "collide", true, "[QueryTriggerInteraction]\nchanges the Physics.OverlapSphere QueryTriggerInteraction in VehicleController.CollectItemsInTruck from Ignore to Collide.\nattempts to fix some modded items (and teeth) not being collected by the magnet and not having isInShipRoom set to true, so the move items script would skip them"); cfg8_collide.Value = temp8_collide.Value;
			temp9_1up = Config.Bind("Cruiser+", "1up", false, "[Cruiser 1up]\nallows purchasing another cruiser in the same day if the current cruiser is destroyed"); cfg9_1up.Value = temp9_1up.Value;
			tempk_lever = Config.Bind("Cruiser+", "lever", true, "[Magnet lever]\nadds a lever to the cruiser for turning on/off the ship magnet"); cfgk_lever.Value = tempk_lever.Value;
			tempi_headlights = Config.Bind("Cruiser+", "headlights", 2, "[Headlights]\nwhether the headlights are on or off when the cruiser is spawned.\n1 = headlights on, headlights material off (same as vanilla)\n2 = headlights on, headlights material on\n3 = headlights off, headlights material off"); cfgi_headlights.Value = tempi_headlights.Value;
			tempr_input = Config.Bind("Cruiser+", "disable_input", false, "[Disable input while typing]\nwhether driving the cruiser (wasd/space) is disabled while typing in chat"); cfgr_input.Value = tempr_input.Value;
			tempb_keybind = Config.Bind("Cruiser+", "keybind", Key.None, "[Separate jump/boost keybinds]\nmakes the boost keybind [Space] plus [config value]. setting to None (or an invalid key) will disable this config and be the same as vanilla. for example if set to LeftShift will make the boost keybind Space+LeftShift, so only pressing Space will jump the cruiser instead.\nhttps://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.Key.html"); cfgb_keybind.Value = tempb_keybind.Value;

			mls = BepInEx.Logging.Logger.CreateLogSource("Cruiser+");
			mls.LogInfo(":red_car:");
			harmony.PatchAll(typeof(kirby.cruiser_additions));

			}}public class cfg_bool{public bool Value{get;set;
			}}public class cfg_int{public int Value{get;set;
			}}public class cfg_key{public Key Value{get;set;
		}
	}
	public class cruiser_additions
	{
//		// magnet rotation //
		private static Vector3 cruiser_rotation;

		[HarmonyPatch(typeof(VehicleController), "StartMagneting"), HarmonyPrefix]
		private static void pre1(VehicleController __instance)
		{
			cruiser_rotation = __instance.transform.eulerAngles;
		}
		[HarmonyPatch(typeof(VehicleController), "StartMagneting"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn1(IEnumerable<CodeInstruction> Instrs)
		{
			if (ca.cfg1_magnet.Value == true)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					if (n >= 11 && l[n - 11].ToString() == "call static UnityEngine.Color UnityEngine.Color::get_white()")
					{
						yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("return_rotation"));
					}
					yield return l[n];
					//ca.mls.LogInfo(l[n].ToString());
				}
			}
		}
		public static float return_rotation(float r)
		{
			if (ca.cfg1_magnet.Value == true)
			{
				if (cruiser_rotation.y < 180f)
				{
					ca.mls.LogInfo("eulerAngles.y " + cruiser_rotation.y + " < 180, returning 90");
					return 90f;
				}
				ca.mls.LogInfo("eulerAngles.y " + cruiser_rotation.y + " > 180, returning 270");
				return 270f;
			}
			return r;
		}

		[HarmonyPatch(typeof(StartOfRound), "EndOfGame"), HarmonyPrefix]
		private static void pre2(StartOfRound __instance)
		{
			VehicleController car = __instance.attachedVehicle;
			if (car != null)
			{
//				// move items //
				if (ca.cfg2_moveitems.Value == 3 || (ca.cfg2_moveitems.Value == 2 && (hostmoveitems == "true" || GameNetworkManager.Instance.isHostingGame == true)))
				{
					ca.mls.LogInfo("moving items");
					ca.mls.LogInfo(("host:" + hostscraponly + " client:" + ca.cfg2_scraponly.Value).ToLower());
					Collider[] objects = Physics.OverlapBox(car.transform.position, new Vector3(4f, 11f, 11f) / 2, new Quaternion(0f, 0.7071068f, 0f, 0.7071068f), 64, QueryTriggerInteraction.Collide);
					ca.mls.LogInfo("total items found in cruiser: " + objects.Length);
					for (int n = 0; n < objects.Length; n = n + 1)
					{
						GrabbableObject item = objects[n].GetComponent<GrabbableObject>();
						if ((item) && item != null && (hostscraponly == "false" || (hostscraponly == "true" && item.itemProperties.isScrap == true) || (hostscraponly == "nil" && ca.cfg2_scraponly.Value == false) || (hostscraponly == "nil" && ca.cfg2_scraponly.Value == true && item.itemProperties.isScrap == true)) && item.isHeld == false && item.isHeldByEnemy == false && item.isInShipRoom == true) //objects[n].transform.parent == car.transform)
						{
							ca.mls.LogInfo("moving item " + (n + 1) + " " + item.itemProperties.itemName + " / " + item.itemProperties.name + " / " + ((item.gameObject) && item.gameObject != null ? item.name : ".gameObject was null"));
							item.transform.SetParent(StartOfRound.Instance.elevatorTransform);
							Shion cr = (GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && item.GetComponent<NetworkObject>() != null ? new Shion(lobbyid + item.GetComponent<NetworkObject>().NetworkObjectId) : new Shion());
							var angle = cr.next01() * System.Math.PI * 2;
							var radius = cr.next01() * 1;
							var random_x = -0.33f + radius * System.Math.Cos(angle);
							var random_z = -14.4f + radius * System.Math.Sin(angle);
							Vector3 pos = item.transform.parent.InverseTransformPoint(new Vector3((float)random_x, item.itemProperties.verticalOffset + 0.2362f, (float)random_z));
							item.transform.eulerAngles = new Vector3(item.itemProperties.restingRotation.x, (float)cr.next32mm(0, 360), item.itemProperties.restingRotation.z);
							item.transform.position = pos;
							item.startFallingPosition = pos;
							item.targetFloorPosition = pos;
							if (item.itemProperties.spawnPrefab != null)
							{
								item.transform.localScale = item.itemProperties.spawnPrefab.transform.localScale;
							}
							else
							{
								item.transform.localScale = item.originalScale;
							}
						}
						else if (item != null)
						{
							ca.mls.LogInfo("skipped item " + (n + 1) + " " + item.itemProperties.itemName + " / " + item.itemProperties.name + " / " + item.name);
							if (item.isInShipRoom == false)
							{
								ca.mls.LogInfo("skipped item " + (n + 1) + " wasn't collected! isInShipRoom was false. it will be in the cruiser");
							}
						}
					}
				}

//				// mute engine/radio //
				if (ca.cfg3_engine.Value == true)
				{
					ca.mls.LogInfo("muting engine");
					car.engineAudio1.mute = true;
					car.engineAudio2.mute = true;
				}
				if (ca.cfg3_radio.Value == true)
				{
					ca.mls.LogInfo("muting radio");
					car.radioAudio.mute = true;
					car.radioInterference.mute = true;
				}
			}
		}

//		// unmute engine/radio //
		[HarmonyPatch(typeof(StartOfRound), "openingDoorsSequence", MethodType.Enumerator), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn2(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if ((ca.cfg3_engine.Value == true || ca.cfg3_radio.Value == true) && n >= 2 && l[n - 2].ToString() == "ldstr \"Closed\"")
				{
					yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("unmute_audio"));
				}
				//ca.mls.LogInfo(l[n].ToString());
			}
		}
		public static void unmute_audio()
		{
			VehicleController car = StartOfRound.Instance.attachedVehicle;
			if (car != null)
			{
				if (ca.cfg3_engine.Value == true)
				{
					ca.mls.LogInfo("unmuting engine");
					car.engineAudio1.mute = false;
					car.engineAudio2.mute = false;
				}
				if (ca.cfg3_radio.Value == true)
				{
					ca.mls.LogInfo("unmuting radio");
					car.radioAudio.mute = false;
					car.radioInterference.mute = false;
				}
			}
		}

		private static Transform light_tree = null;

		private static Transform nightlight = null;

		private static Transform meter_cube = null;

		private static Transform meter_text = null;

		private static Transform lever_tree = null;

		[HarmonyPatch(typeof(VehicleController), "Start"), HarmonyPrefix]
		private static void pre3(VehicleController __instance)
		{
//			// headlights //
			if (ca.cfgi_headlights.Value == 2 || ca.cfgi_headlights.Value == 3) { try { set_headlights(__instance); } catch (System.Exception error) { ca.mls.LogError("error setting headlights, " + error); } }

			if (__instance.vehicleID != 0) return;

			string singlestring = (ca.cfg4_light.Value == true ? ", light" : "") + (ca.cfg4_light.Value == true && ca.cfg4_night.Value == true ? ", light switch" : "") + (ca.cfg6_speedometer.Value == true ? ", speedometer" : "") + (ca.cfgk_lever.Value == true ? ", magnet lever" : "");
			if (singlestring != null && singlestring != "") ca.mls.LogInfo("adding " + singlestring.Substring(2));

//			// storage light //
			if (ca.cfg4_light.Value == true)
			{
				if (light_tree == null)
				{
					Transform light_object = Object.Instantiate<Transform>(__instance.frontCabinLightContainer.GetComponentsInChildren<Transform>(true)[1]);
					light_object.name = "cruiser_storage_light";
					Light light = light_object.GetComponentInChildren<Light>();
					light.type = LightType.Spot;
					light.spotAngle = 120f;
					light.range = 10f;
					light_tree = light_object;
					light_tree.gameObject.SetActive(false);
					Object.DontDestroyOnLoad(light_tree);
					ca.mls.LogInfo("saved light");
				}
				if (light_tree != null)
				{
					Transform light_object = Object.Instantiate<Transform>(light_tree);
					light_object.SetParent(__instance.GetComponentsInChildren<Transform>().First(_ => _.name == "Meshes"));
					light_object.localPosition = new Vector3(0f, 3f, -2f);
					light_object.localEulerAngles = new Vector3(90f, 0f, 0f);
					light_object.localScale = new Vector3(1f, 1f, 1f);
					light_object.gameObject.SetActive(__instance.frontCabinLightContainer.activeSelf);
					ca.mls.LogInfo("added light");
				}

//				// light switch //
				if (ca.cfg4_night.Value == true)
				{
					if (nightlight == null)
					{
						Transform border = Object.Instantiate<Transform>(GameObject.Find("Environment/HangarShip/damageTrigger/Cube").transform);
						border.GetComponent<MeshRenderer>().materials = new Material[] {GameObject.Find("Environment/HangarShip/ShipModels2b/MonitorWall/Cube").GetComponent<MeshRenderer>().materials[1]};

						Transform suichi = Object.Instantiate<Transform>(GameObject.Find("Environment/HangarShip/ShipModels2b/MonitorWall/Cube.001/CameraMonitorOnButton").transform);
						suichi.SetParent(border);
						suichi.localPosition = new Vector3(0f, 0f, 0.375f); //-1.31 0.9 -3.2
						suichi.localEulerAngles = new Vector3(90f, 180f, 0f); //90 270 0
						suichi.localScale = new Vector3(4.8137f, 19.5712f, 4.8137f); //0.7221 1.5657 0.7221
						suichi.gameObject.SetActive(true);
						Object.DestroyImmediate(suichi.GetChild(0).gameObject);

						Transform light_object = Object.Instantiate<Transform>(light_tree);
						light_object.SetParent(border);
						light_object.localPosition = new Vector3(0f, 0f, 0.875f); //-1.27 0.9 -3.2
						light_object.localEulerAngles = new Vector3(0f, 0f, 0f);
						light_object.localScale = new Vector3(1f, 1f, 1f);
						light_object.gameObject.SetActive(false);

						Light light1 = light_object.GetComponent<Light>(); //copying parts of spiketrap light
						light1.color = new Color(0.165f, 0f, 0f, 1f);
						light1.colorTemperature = 1f;
						light1.innerSpotAngle = 5.2f;
						light1.intensity = 5f;
						light1.range = 0.06f;
						light1.shadows = LightShadows.None;
						light1.spotAngle = 5.2f;
						light1.type = LightType.Point;
						var light2 = light_object.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
						light2.customSpotLightShadowCone = 5.2f;
						light2.innerSpotPercent = 66.4f;
						light2.intensity = 14.7321f;
						light1.intensity = 5f;
						light2.shadowFadeDistance = 8f;
						light2.shadowNearPlane = 0f;
						light2.shapeRadius = 0.2f;
						light2.volumetricDimmer = 0.86f;

						Transform interactable = Object.Instantiate<Transform>(__instance.GetComponentsInChildren<Transform>(true).First(_ => _.name == "ChangeChannel (3)"));
						interactable.SetParent(border);
						interactable.localPosition = new Vector3(0f, 0f, 0.25f); //-1.32 0.9 -3.2
						interactable.localEulerAngles = new Vector3(0f, 0f, 0f); //0 90 0
						interactable.localScale = new Vector3(2f, 2f, 1.25f); //0.4 0.4 0.1
						interactable.gameObject.SetActive(true);

						InteractTrigger trigger = interactable.GetComponent<InteractTrigger>();
						trigger.hoverTip = "Switch light: [LMB]";
						trigger.onInteract = new InteractEvent();
						trigger.twoHandedItemAllowed = true;
						border.gameObject.AddComponent<cruiser_light_switch_component>();

						border.gameObject.AddComponent<AudioSource>();
						AudioSource audio = border.gameObject.GetComponent<AudioSource>(); //copying parts of flashlight audiosource
						audio.clip = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "ProFlashlight").spawnPrefab.GetComponent<FlashlightItem>().flashlightClips[0];
						audio.dopplerLevel = 0.3f;
						audio.maxDistance = 9f;
						audio.minDistance = 4f;
						audio.playOnAwake = false;
						audio.rolloffMode = AudioRolloffMode.Linear;
						audio.spatialBlend = 1f;
						audio.spread = 71f;

						border.name = "cruiser_light_switch";
						suichi.name = "cruiser_night_switch";
						light_object.name = "cruiser_night_light";
						interactable.name = "cruiser_night_interact";
						nightlight = border;
						nightlight.gameObject.SetActive(false);
						Object.DontDestroyOnLoad(nightlight);
						ca.mls.LogInfo("saved light switch");
					}
					if (nightlight != null)
					{
						Transform night = Object.Instantiate<Transform>(nightlight);
						night.SetParent(__instance.GetComponentsInChildren<Transform>().First(_ => _.name == "Triggers"));
						night.localPosition = new Vector3(-1.34f, 0.9f, -3.2f);
						night.localEulerAngles = new Vector3(0f, 90f, 0f);
						night.localScale = new Vector3(0.15f, 0.15f, 0.08f);
						night.GetChild(1).GetComponent<Light>().intensity = 5f;
						night.gameObject.SetActive(true);
						night.GetChild(1).gameObject.SetActive(!__instance.GetComponentsInChildren<Transform>(true).First(_ => _.name == "cruiser_storage_light(Clone)").gameObject.activeSelf);
						ca.mls.LogInfo("added light switch");
					}
				}
			}

//			// speedometer //
			if (ca.cfg6_speedometer.Value == true)
			{
				if (meter_cube == null)
				{
					//cube
					Transform screen = Object.Instantiate<Transform>(GameObject.Find("Environment/HangarShip/damageTrigger/Cube").transform);
					Transform border = Object.Instantiate<Transform>(screen);
					screen.GetComponent<MeshRenderer>().materials = new Material[] {GameObject.Find("Environment/HangarShip/SmallDetails/Cameras/TinyCamera").GetComponent<MeshRenderer>().materials[0]};
					screen.GetComponent<MeshRenderer>().materials[0].color = new Color(0f, 0f, 0f, 0f);
					border.GetComponent<MeshRenderer>().materials = new Material[] {GameObject.Find("Environment/HangarShip/ShipModels2b/MonitorWall/Cube").GetComponent<MeshRenderer>().materials[1]};
					border.SetParent(screen);
					border.localPosition = new Vector3(0f, 0f, 0f);
					border.localEulerAngles = new Vector3(0f, 0f, 0f);
					border.localScale = new Vector3(0.98f, 1.2f, 1.2f);
					screen.name = "cruiser_speedometer_screen";
					border.name = "cruiser_speedometer_border";
					meter_cube = screen;
					meter_cube.gameObject.SetActive(false);
					Object.DontDestroyOnLoad(meter_cube);
					ca.mls.LogInfo("saved speedometer object");
					//text
					Transform upper = Object.Instantiate<Transform>(GameObject.Find("Environment/HangarShip/ShipModels2b/MonitorWall/Cube/Canvas (1)").transform);
					Transform lower = upper.GetComponentsInChildren<Transform>(true).First(_ => _.name == "HeaderText");
					lower.SetParent(upper);
					lower.gameObject.SetActive(true);
					Transform temp = upper.GetComponentsInChildren<Transform>(true).First(_ => _.name == "MainContainer");
					Object.DestroyImmediate(temp.GetComponent<UnityEngine.UI.Image>());
					Object.DestroyImmediate(temp.gameObject);
					var text = lower.GetComponent<TMPro.TextMeshProUGUI>();
					text.enabled = true;
					text.alignment = TMPro.TextAlignmentOptions.Center;
					text.color = new Color(1f, 1f, 1f, 1f);
					text.fontStyle = (TMPro.FontStyles)FontStyle.Bold;
					text.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
					text.text = "444";
					upper.name = "cruiser_speedometer_text1";
					lower.name = "cruiser_speedometer_text2";
					meter_text = upper;
					meter_text.gameObject.SetActive(false);
					Object.DontDestroyOnLoad(meter_text);
					ca.mls.LogInfo("saved speedometer text");
				}
				if (meter_cube != null && meter_text != null)
				{
					Transform cube = Object.Instantiate<Transform>(meter_cube);
					cube.SetParent(__instance.GetComponentsInChildren<Transform>().First(_ => _.name == "Meshes"));
					cube.localPosition = new Vector3(-1f, 0.115f, 2.65f);
					cube.localEulerAngles = new Vector3(0f, 90f, 30f);
					cube.localScale = new Vector3(0.2f, 0.08f, 0.13f);
					cube.gameObject.SetActive(true);
					Transform text = Object.Instantiate<Transform>(meter_text);
					text.SetParent(__instance.GetComponentsInChildren<Transform>().First(_ => _.name == "Meshes"));
					text.localPosition = new Vector3(-0.995f, 0.157f, 2.558f);
					text.localEulerAngles = new Vector3(30f, 0f, 0f);
					text.localScale = new Vector3(0.001f, 0.001f, 0.001f);
					Transform lower = text.GetChild(0);
					lower.localPosition = new Vector3(-4.7f, 7f, 0f);
					lower.localEulerAngles = new Vector3(0f, 0f, 0f);
					lower.localScale = new Vector3(1f, 1f, 1f);
					var txt = lower.GetComponent<TMPro.TextMeshProUGUI>();
					txt.enabled = true;
					txt.enableWordWrapping = false;
					txt.autoSizeTextContainer = false;
					txt.enableAutoSizing = false;
					txt.text = "444";
					txt.autoSizeTextContainer = true;
					txt.enableAutoSizing = true;
					text.gameObject.SetActive(__instance.frontCabinLightContainer.activeSelf);
					ca.mls.LogInfo("added speedometer");
				}
			}

//			// magnet lever //
			if (ca.cfgk_lever.Value == true)
			{
				Transform door = __instance.GetComponentsInChildren<InteractTrigger>(true).First(_ => _.name == "DoorTrigger" && _.transform.parent.parent.name == "DoorLeftContainer").transform;
				door.localPosition = new Vector3(1.1171f, -0.0073f, -0.4219f); //x + 0.1
				door.localScale = new Vector3(1.8201f, 0.2525f, 1.8792f); //x - 0.2
				if (lever_tree == null)
				{
					GameObject ship_magnet = GameObject.Find("Environment/HangarShip/MagnetLever");
					Transform lever = Object.Instantiate<Transform>(ship_magnet.transform);
					AudioSource audio = lever.GetComponent<AudioSource>();
					audio.clip = ship_magnet.GetComponent<AnimatedObjectTrigger>().boolFalseAudios[0];
					audio.dopplerLevel = 0.3f;
					audio.maxDistance = 17f;
					audio.spread = 45f;
					audio.volume = 0.9f;
					audio.playOnAwake = false;
					Transform interactable = Object.Instantiate<Transform>(__instance.GetComponentsInChildren<Transform>(true).First(_ => _.name == "ChangeChannel (3)"));
					interactable.SetParent(lever);
					interactable.localPosition = new Vector3(0.0667f, -0.0833f, 0f); //-1.76 0 2.74
					interactable.localEulerAngles = new Vector3(0f, 225f, 60f); //315 0 330
					interactable.localScale = new Vector3(0.2f, 0.4167f, 0.4167f); //0.12 0.25 0.25
					interactable.gameObject.SetActive(true);
					InteractTrigger trigger = interactable.GetComponent<InteractTrigger>();
					trigger.hoverTip = "Pull lever: [LMB]";
					trigger.onInteract = new InteractEvent();
					trigger.twoHandedItemAllowed = true;
					trigger.holdInteraction = true;
					trigger.cooldownTime = 1f;
					trigger.timeToHold = 1.7f;
					trigger.timeToHoldSpeedMultiplier = 1f;
					lever.gameObject.AddComponent<cruiser_magnet_lever_component>();
					Object.DestroyImmediate(lever.GetComponent<BoxCollider>());
					Object.DestroyImmediate(lever.GetComponent<AnimatedObjectTrigger>());
					Object.DestroyImmediate(lever.GetComponent<InteractTrigger>());
					interactable.name = "cruiser_magnet_lever_interact";
					lever.name = "cruiser_magnet_lever";
					lever_tree = lever;
					lever_tree.gameObject.SetActive(false);
					Object.DontDestroyOnLoad(lever_tree);
					ca.mls.LogInfo("saved magnet lever");
				}
				if (lever_tree != null)
				{
					Transform lever = Object.Instantiate<Transform>(lever_tree);
					lever.SetParent(__instance.GetComponentsInChildren<Transform>().First(_ => _.name == "Meshes"));
					lever.localPosition = new Vector3(-1.71f, 0f, 2.78f);
					lever.localEulerAngles = new Vector3(90f, 90f, 0f);
					lever.localScale = new Vector3(0.6f, 0.6f, 0.6f);
					lever.gameObject.SetActive(true);
					lever.GetComponent<Animator>().SetBool("leverUp", StartOfRound.Instance.magnetOn);
					ca.mls.LogInfo("added magnet lever");
				}
			}
		}
		private static async void set_headlights(VehicleController v)
		{
			if (disconnected[0] == true) return;
			v.headlightsContainer.SetActive(ca.cfgi_headlights.Value == 2 ? true : false);
			Material cat = (ca.cfgi_headlights.Value == 2 ? v.headlightsOnMat : v.headlightsOffMat);
			Material[] cats = v.mainBodyMesh.sharedMaterials;
			cats[1] = cat;
			v.mainBodyMesh.sharedMaterials = cats;
			await wait_frames(1);
			if (disconnected[0] == true) return;
			v.headlightsContainer.SetActive(ca.cfgi_headlights.Value == 2 ? true : false);
			cats = v.mainBodyMesh.sharedMaterials;
			cats[1] = cat;
			v.mainBodyMesh.sharedMaterials = cats;
			await wait_frames(4);
			if (disconnected[0] == true) return;
			v.headlightsContainer.SetActive(ca.cfgi_headlights.Value == 2 ? true : false);
			cats = v.mainBodyMesh.sharedMaterials;
			cats[1] = cat;
			v.mainBodyMesh.sharedMaterials = cats;
		}
		[HarmonyPatch(typeof(VehicleController), "CancelTryIgnitionClientRpc"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn3(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if ((ca.cfg4_light.Value == true || ca.cfg6_speedometer.Value == true) && l[n].ToString() == "call void VehicleController::SetFrontCabinLightOn(bool setOn)")
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("set_lights"));
				}
				//ca.mls.LogInfo(l[n].ToString());
			}
		}
		[HarmonyPatch(typeof(VehicleController), "TryIgnition", MethodType.Enumerator), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn4(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if ((ca.cfg4_light.Value == true || ca.cfg6_speedometer.Value == true) && l[n].ToString() == "call void VehicleController::SetFrontCabinLightOn(bool setOn)")
				{
					yield return new CodeInstruction(OpCodes.Ldloc_1);
					yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("set_lights"));
				}
				//ca.mls.LogInfo(l[n].ToString());
			}
		}
		[HarmonyPatch(typeof(VehicleController), "SetIgnition"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn5(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if ((ca.cfg4_light.Value == true || ca.cfg6_speedometer.Value == true) && l[n].ToString() == "call void VehicleController::SetFrontCabinLightOn(bool setOn)")
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("set_lights"));
				}
				//ca.mls.LogInfo(l[n].ToString());
			}
		}
		[HarmonyPatch(typeof(VehicleController), "RemoveKey", MethodType.Enumerator), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn6(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if ((ca.cfg4_light.Value == true || ca.cfg6_speedometer.Value == true) && l[n].ToString() == "call void VehicleController::SetFrontCabinLightOn(bool setOn)")
				{
					yield return new CodeInstruction(OpCodes.Ldloc_1);
					yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("set_lights"));
				}
				//ca.mls.LogInfo(l[n].ToString());
			}
		}
		public static void set_lights(VehicleController __instance)
		{
			if (__instance.vehicleID != 0 || __instance.averageCount < 4) return;
			bool set_on = (bool)typeof(VehicleController).GetField("keyIsInIgnition", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
			if (ca.cfg4_light.Value == true && light_tree != null)
			{
				__instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(_ => _.name == "cruiser_storage_light(Clone)").gameObject.SetActive(set_on);
				if (ca.cfg4_night.Value == true && nightlight != null)
				{
					__instance.GetComponentsInChildren<Transform>(true).FirstOrDefault(_ => _.name == "cruiser_night_light").gameObject.SetActive(!set_on);
				}
			}
			if (ca.cfg6_speedometer.Value == true && meter_cube != null && meter_text != null)
			{
				RectTransform rt = __instance.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(_ => _.name == "cruiser_speedometer_text1(Clone)");
				if (rt != null)
				{
					if (rt.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text == "444")
					{
						rt.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "0";
					}
					rt.gameObject.SetActive(set_on);
				}
			}
		}
		[HarmonyPatch(typeof(VehicleController), "DestroyCar"), HarmonyPostfix]
		private static void pst1(VehicleController __instance)
		{
			if (__instance.vehicleID != 0) return;
			if (ca.cfg4_light.Value == true && light_tree != null)
			{
				__instance.GetComponentsInChildren<Transform>(true).First(_ => _.name == "cruiser_storage_light(Clone)").gameObject.SetActive(false);
			}
			if (ca.cfg6_speedometer.Value == true && meter_cube != null && meter_text != null)
			{
				__instance.GetComponentsInChildren<Transform>(true).First(_ => _.name == "cruiser_speedometer_screen(Clone)").gameObject.SetActive(false);
				__instance.GetComponentsInChildren<RectTransform>(true).First(_ => _.name == "cruiser_speedometer_text1(Clone)").gameObject.SetActive(false);
			}
			if (ca.cfgk_lever.Value == true && lever_tree != null)
			{
				__instance.GetComponentsInChildren<Transform>(true).First(_ => _.name == "cruiser_magnet_lever(Clone)").gameObject.SetActive(false);
			}
		}
		[HarmonyPatch(typeof(VehicleController), "FixedUpdate"), HarmonyPostfix]
		private static void pst2(VehicleController __instance)
		{
			if (__instance.vehicleID == 0 && ca.cfg6_speedometer.Value == true && meter_cube != null && meter_text != null && __instance.ignitionStarted == true && __instance.averageCount >= 4)
			{
				RectTransform rt = __instance.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(_ => _.name == "cruiser_speedometer_text1(Clone)");
				if (rt != null && rt.gameObject.activeSelf == true)
				{
					var text = rt.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
					string velocity = Mathf.Round(__instance.mainRigidbody.velocity.magnitude * 2).ToString();
					if (velocity != text.text)
					{
						if (velocity.Length > 5)
						{
							velocity = "Error";
						}
						text.text = velocity;
					}
				}
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "SetMagnetOnClientRpc"), HarmonyPostfix]
		private static void pst3(StartOfRound __instance, bool on)
		{
			if (ca.cfgk_lever.Value == true && lever_tree != null)
			{
				foreach (VehicleController vehicle in Object.FindObjectsByType<VehicleController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
				{
					if (vehicle.vehicleID != 0) continue;
					Transform lever = vehicle.GetComponentsInChildren<Transform>(true).FirstOrDefault(_ => _.name == "cruiser_magnet_lever(Clone)");
					if (lever == null) continue;
					Animator anime = lever.GetComponent<Animator>();
					if (anime.GetBool("leverUp") != on)
					{
						AudioSource audio = lever.GetComponent<AudioSource>();
						audio.PlayOneShot(on == true ? __instance.magnetOnAudio : __instance.magnetOffAudio);
						audio.Play();
					}
					anime.SetBool("leverUp", on);
				}
			}
		}

//		// light switch event //
		public static void switch_light_event(PlayerControllerB player)
		{
			Ray r = new Ray(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward);
			RaycastHit hit;
			Physics.Raycast(r, out hit, player.grabDistance, 1073742656); //player.interactableObjectsMask
			VehicleController car = hit.collider.gameObject.transform.root.GetComponent<VehicleController>();
			if (car != null && car.vehicleID == 0 && car.GetComponent<NetworkObject>() != null)
			{
				UInt64 guid = car.GetComponent<NetworkObject>().NetworkObjectId;
				bool red = !car.GetComponentsInChildren<Transform>(true).First(_ => _.name == "cruiser_night_light").gameObject.activeSelf;
				if (StartOfRound.Instance.connectedPlayersAmount + 1 != 1 && sync == true)
				{
					if (GameNetworkManager.Instance.isHostingGame == true)
					{
						update_clients_lights(guid, red);
						return;
					}
					if (client_received == true && NetworkManager.Singleton.IsHost == false)
					{
						string message = "LightSwitchRequest^" + guid + "^" + red;
						FastBufferWriter w = new FastBufferWriter(FastBufferWriter.GetWriteSize(message), Unity.Collections.Allocator.Temp);
						w.WriteValueSafe(message);
						NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Cruiser_Additions-Host", NetworkManager.ServerClientId, w, NetworkDelivery.ReliableFragmentedSequenced);
						w.Dispose();
						return;
					}
				}
				switch_light(guid, red);
			}
			else
			{
				ca.mls.LogError("switch_light_event error" + (car == null ? ", cruiser was null" : (car.vehicleID != 0 ? ", vehicleID wasn't 0" : ", cruiser NetworkObject component was null")));
			}
		}
		private static void switch_light(UInt64 guid, bool red)
		{
			if (ca.cfg4_light.Value == true && ca.cfg4_night.Value == true && nightlight != null)
			{
				VehicleController car = Object.FindObjectsByType<VehicleController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(_ => _.GetComponent<NetworkObject>() != null && _.GetComponent<NetworkObject>().NetworkObjectId == guid);
				if (car != null && car.vehicleID == 0)
				{
					Transform light = car.GetComponentsInChildren<Transform>(true).FirstOrDefault(_ => _.name == "cruiser_storage_light(Clone)");
					if (light.gameObject.activeSelf == red)
					{
						Transform light_switch = car.GetComponentsInChildren<Transform>(true).FirstOrDefault(_ => _.name == "cruiser_light_switch(Clone)");
						Transform red_light = light_switch.GetChild(1);
						red_light.GetComponent<Light>().intensity = 5f;
						red_light.gameObject.SetActive(red);
						AudioSource audio = light_switch.gameObject.GetComponent<AudioSource>();
						if (audio != null) audio.PlayOneShot(audio.clip);
						light.gameObject.SetActive(!red);
					}
				}
				else
				{
					ca.mls.LogError("switch_light error" + (car == null ? ", cruiser was null" : ", vehicleID wasn't 0"));
				}
			}
		}

//		// scrap items //
		private static string[] item = new string[2];

		private static Transform[] tree = new Transform[2];

		private static AudioClip[] take = new AudioClip[2];

		private static AudioClip[] give = new AudioClip[2];

		private static Vector3[] position = new Vector3[3];

		private static Vector3[] rotation = new Vector3[2];

		private static bool first_pending = false;

		private static UInt64 lobbyid = 0uL;

		private static bool[] first_item = new bool[] {true, true, true};

		[HarmonyPatch(typeof(GrabbableObject), "Start"), HarmonyPostfix]
		private static void pst4(GrabbableObject __instance)
		{
			pst4async(__instance);
		}
		private static async void pst4async(GrabbableObject __instance)
		{
			if (ca.cfg5_scrap.Value == true && __instance.itemProperties.name == "EnginePart1")
			{
				await wait_frames(1);
				if (__instance.gameObject.GetComponent<kirby.temporary>() != null) return;
				__instance.gameObject.AddComponent<kirby.temporary>();
				__instance.itemProperties.restingRotation = new Vector3(0f, 0f, 90f);
				__instance.itemProperties.verticalOffset = 0.4f;
				if (GameNetworkManager.Instance.disableSteam == false && seeds == "nil")
				{
					if (GameNetworkManager.Instance.isHostingGame == false)
					{
						if (first_item[0] == true) { ca.mls.LogInfo("await wait_timer"); first_item[0] = false; }
						await wait_timer(ca.cfg5_millisecond.Value >= 20 && ca.cfg5_millisecond.Value <= 4000 ? ca.cfg5_millisecond.Value : 100);
						if (disconnected[0] == true) return;
					}
					if (seeds == "nil") seeds = "?";
				}
				if (first_pending == true)
				{
					await wait_frames(2);
				}
				if (item[0] == null && first_pending == false)
				{
					ca.mls.LogInfo("first engine");
					first_pending = true;
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.VehiclesList[0].GetComponentsInChildren<Transform>().First(_ => _.name == "Meshes"));
					var sl = new List<string>() {
						"Cube.002",
						"Cube.003",
						"HoodTrigger",
						"DoorTrigger",
						"Collider",
						"DoorTrigger",
						"Collider",
						"Links",
						"ClosedCollider",
						"ClosedTrigger",
						"OpenCollider",
						"OpenTrigger",
						"DoorLeft",
						"ShiftToReverseTrigger",
						"ShiftToDriveTrigger",
						"ShiftToParkTrigger",
						"CarKeyTurnedPos",
						"CarKeyNotTurnedPos",
						"StartIgnition",
						"StopIgnition",
						"InteractionBlockers",
						"Cube",
						"Cube",
						"WindshieldInteractBlocker",
						"FrontCabinLight",
						"BackLights",
						"SparkParticle (1)",
					};
					foreach (Transform ctr in tr.GetComponentsInChildren<Transform>(true))
					{
						foreach (string s in sl)
						{
							if (ctr.name == s)
							{
								Object.Destroy(ctr.gameObject);
								sl.Remove(s);
								break;
							}
						}
						if (ctr.name.Contains("Audio") == true)
						{
							Object.Destroy(ctr.gameObject);
						}
						else if (ctr.name == "MainBodyDestroyed")
						{
							tree[0] = Object.Instantiate<Transform>(ctr);
							tree[0].gameObject.SetActive(false);
							tree[0].name = "ToyCar(Exploded)";
							Object.DontDestroyOnLoad(tree[0]);
							Object.Destroy(ctr.gameObject);
						}
						else if (ctr.name == "CarHoodFire" && ca.cfg5_playwithtoy.Value == true)
						{
							ctr.GetComponent<ParticleSystem>().scalingMode = ParticleSystemScalingMode.Hierarchy;
							ctr.GetComponentInChildren<Light>(true).intensity = 45f;
						}
						else if (ctr.name == "Headlights" && ca.cfg5_playwithtoy.Value == false)
						{
							Object.Destroy(ctr.gameObject);
						}
					}
					foreach (AnimatedObjectTrigger component in tr.GetComponentsInChildren<AnimatedObjectTrigger>()) Object.Destroy(component);
					foreach (PlayAudioAnimationEvent component in tr.GetComponentsInChildren<PlayAudioAnimationEvent>()) Object.Destroy(component);
					Transform eject = Object.Instantiate<Transform>(StartOfRound.Instance.VehiclesList[0].GetComponentsInChildren<Transform>().First(_ => _.name == "ButtonAnimContainer"));
					Object.Destroy(eject.GetChild(0).GetComponent<BoxCollider>());
					Object.Destroy(eject.GetChild(0).GetComponent<InteractTrigger>());
					Object.Destroy(eject.GetChild(1).GetComponent<BoxCollider>());
					Object.Destroy(eject.GetChild(1).GetComponent<InteractTrigger>());
					Object.Destroy(eject.GetChild(1).GetComponent<AnimatedObjectTrigger>());
					eject.SetParent(tr);
					if (ca.cfg5_playwithtoy.Value == true)
					{
						Transform glass = Object.Instantiate<Transform>(StartOfRound.Instance.VehiclesList[0].GetComponentsInChildren<Transform>().First(_ => _.name == "Particle System"));
						glass.GetComponent<ParticleSystem>().scalingMode = ParticleSystemScalingMode.Hierarchy;
						glass.SetParent(tr);
					}
					tr.GetComponentInChildren<LODGroup>().size = 60f;
					await wait_frames(1);
					tree[1] = tr;
					tree[1].gameObject.SetActive(false);
					tree[1].name = "ToyCar";
					Object.DontDestroyOnLoad(tree[1]);
					ca.mls.LogInfo("saved cruiser scrap transforms");
					//engine
					item[0] = "V-type engine";
					take[0] = __instance.itemProperties.grabSFX;
					give[0] = __instance.itemProperties.dropSFX;
					position[0] = __instance.itemProperties.positionOffset;
					rotation[0] = __instance.itemProperties.rotationOffset;
					//cruiser
					Item temp = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "ToyCube");
					item[1] = "Toy car";
					take[1] = temp.grabSFX;
					give[1] = temp.dropSFX;
					position[1] = new Vector3(-0.035f, 0.2f, 0.02f);
					position[2] = new Vector3(-0.02f, 0.31f, -0.008f); //-0.035 0.295 -0.0075
					rotation[1] = new Vector3(0f, 16f, 76f);
					first_pending = false;
					ca.mls.LogInfo("saved engine/cruiser scrap variables");
				}
				if (item[0] != null)
				{
					Shion cr;
					if (GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && __instance.GetComponent<NetworkObject>() != null)
					{
						UInt64 n64;
						if (ca.cfg5_saveseeds.Value == true && seeds != "nil" && seeds != "?" && seeds.Contains(__instance.GetComponent<NetworkObject>().NetworkObjectId + ".") == true)
						{
							if (first_item[2] == true) ca.mls.LogInfo("custom_random = new Shion(saved seed)");
							int start = seeds.IndexOf(__instance.GetComponent<NetworkObject>().NetworkObjectId + ".");
							int n = __instance.GetComponent<NetworkObject>().NetworkObjectId.ToString().Length + 1;
							n64 = UInt64.Parse(seeds.Substring(start + n, seeds.IndexOf("/", start) - (start + n)));
						}
						else
						{
							if (first_item[2] == true) ca.mls.LogInfo("custom_random = new Shion(lobbyid+networkobjectid)");
							n64 = lobbyid + __instance.GetComponent<NetworkObject>().NetworkObjectId;
						}
						cr = new Shion(n64);
						if (ca.cfg5_saveseeds.Value == true)
						{
							__instance.gameObject.AddComponent<ItemTypeSeed>();
							__instance.gameObject.GetComponent<ItemTypeSeed>().seed = n64.ToString();
						}
					}
					else
					{
						if (first_item[2] == true) ca.mls.LogInfo("custom_random = new Shion()");
						cr = new Shion();
					}
					if (first_item[2] == true) ca.mls.LogInfo(ca.cfg5_percentages.Value == true && synced_percents[0] != -1 ? "host config" : "local config");
					first_item[2] = false;
					int[] percents = (ca.cfg5_percentages.Value == true && synced_percents[0] != -1 ? synced_percents : new int[] {-1, ca.cfg5_cruiser.Value, ca.cfg5_cruiseralt.Value});
					Transform tr = null;
					int num = (cr.next32mm(0, 100) < percents[1] ? 1 : 0);
					if (num == 1)
					{
						BoxCollider box = __instance.GetComponent<BoxCollider>();
						if (cr.next32mm(0, 100) < percents[2]) //kuro
						{
							tr = Object.Instantiate<Transform>(tree[0], __instance.transform);
							tr.localPosition = new Vector3(21.5f, 0f, 0f);
							tr.localEulerAngles = new Vector3(0f, 90f, 180f);
							tr.localScale = new Vector3(-6f, -6f, -6f);
							tr.gameObject.SetActive(true);
							box.center = new Vector3(5f, 0f, 0f);
							box.size = new Vector3(27.5f, 23f, 60f);
							__instance.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(5f, 0f, 0f);
						}
						else
						{
							tr = Object.Instantiate<Transform>(tree[1], __instance.transform);
							tr.localPosition = new Vector3(3.5f, 0f, 0f);
							tr.localEulerAngles = new Vector3(0f, 180f, 90f);
							tr.localScale = new Vector3(-6f, -6f, -6f);
							tr.gameObject.SetActive(true);
							box.center = new Vector3(2.5f, 0f, 0f);
							box.size = new Vector3(33f, 23f, 60f);
							__instance.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(2.5f, 0f, 0f);
							__instance.GetComponentsInChildren<Transform>().First(_ => _.name == "SteeringWheelContainer").GetComponent<Animator>().SetFloat("steeringWheelTurnSpeed", 0.875f);
						}
						__instance.GetComponent<MeshFilter>().mesh = null;
					}
					__instance.GetComponentInChildren<ScanNodeProperties>().headerText = item[num];
					if (ca.cfg5_playwithtoy.Value == true && num == 1 && __instance.transform.childCount >= 2)
					{
						__instance.useCooldown = 0.1f;
						__instance.itemProperties.holdButtonUse = false;
						__instance.itemProperties.requiresBattery = false;
						__instance.itemProperties.syncUseFunction = false;
						PlayWithToy play = __instance.gameObject.AddComponent<PlayWithToy>();
						play.toycar = tr;
						play.vehicle = StartOfRound.Instance.VehiclesList[0].GetComponent<VehicleController>();
						play.redkuro = (tr.name == "ToyCar(Clone)" || tr.name == "ToyCar(Exploded)(Clone)" ? (tr.name == "ToyCar(Clone)" ? red_toy : kuro_toy) : null);
						Transform tca = new GameObject("ToyCarAudio").transform;
						tca.parent = tr;
						tca.localPosition = (tr.name == "ToyCar(Clone)" ? new Vector3(0f, 1f, 1f) : new Vector3(-1f, 0f, 3f));
						tca.localEulerAngles = new Vector3(0f, 0f, 0f);
						tca.localScale = new Vector3(1f, 1f, 1f);
						AudioSource audio = tca.gameObject.AddComponent<AudioSource>();
						AudioSource mixed = tca.gameObject.AddComponent<AudioSource>();
						AudioSource third = tca.gameObject.AddComponent<AudioSource>();
						play.audio = audio;
						play.mixed = mixed;
						play.third = third;
						third.dopplerLevel = mixed.dopplerLevel = audio.dopplerLevel = 0.3f;
						third.maxDistance = mixed.maxDistance = audio.maxDistance = 17f;
						third.minDistance = mixed.minDistance = audio.minDistance = 1f;
						third.playOnAwake = mixed.playOnAwake = audio.playOnAwake = false;
						third.rolloffMode = mixed.rolloffMode = audio.rolloffMode = AudioRolloffMode.Linear;
						third.spatialBlend = mixed.spatialBlend = audio.spatialBlend = 1f;
						third.spread = mixed.spread = audio.spread = 45f;
						third.loop = mixed.loop = audio.loop = true;
						third.volume = audio.volume = 0.5f;
						mixed.volume = 0f;
					}
				}
			}
		}
		private static async Task wait_frames(int frames)
		{
			int previous_frames = Time.frameCount;
			while ((Time.frameCount > (previous_frames + frames)) == false)
			{
				await Task.Delay(4);
			}
		}
		private static async Task wait_timer(int maxwell)
		{
			int n = 0;
			while (seeds == "nil" && client_received == false && n < maxwell && disconnected[0] == false)
			{
				n = n + 1;
				await Task.Delay(4);
			}
			if (disconnected[0] == true) { if (disconnected[1] == false) { ca.mls.LogMessage("disconnected before wait_timer ended"); disconnected[1] = true; } return; }
			if (first_item[1] == true) { ca.mls.LogMessage(seeds == "nil" && client_received == false ? "timer ended before receiving network message (" + n + "/" + ca.cfg5_millisecond.Value + ")" : "received network message before timer ended (" + n + "/" + ca.cfg5_millisecond.Value + ")"); first_item[1] = false; }
		}
		[HarmonyPatch(typeof(GrabbableObject), "PlayDropSFX"), HarmonyPrefix]
		private static void pre4(ref GrabbableObject __instance)
		{
			if (item[0] != null && __instance != null && __instance.itemProperties.name == "EnginePart1" && __instance.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				__instance.itemProperties.dropSFX = (__instance.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? give[1] : give[0]);
			}
		}

		[HarmonyPatch(typeof(PlayerControllerB), "BeginGrabObject"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn7(IEnumerable<CodeInstruction> Instrs)
		{
			if (ca.cfg5_scrap.Value == true)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					yield return l[n];
					if (n > 0 && l[n - 1].ToString() == "call static float UnityEngine.Mathf::Clamp(float value, float min, float max)" && l[n].ToString() == "stfld float GameNetcodeStuff.PlayerControllerB::carryWeight")
					{
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldfld, typeof(PlayerControllerB).GetField("currentlyGrabbingObject", BindingFlags.NonPublic | BindingFlags.Instance));
						yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("grab_item"));
					}
					//ca.mls.LogInfo(l[n].ToString());
				}
			}
		}
		public static void grab_item(GrabbableObject currentlyGrabbingObject)
		{
			if (item[0] != null && currentlyGrabbingObject != null && currentlyGrabbingObject.itemProperties.name == "EnginePart1" && currentlyGrabbingObject.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				int n = (currentlyGrabbingObject.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? 1 : 0);
				currentlyGrabbingObject.itemProperties.grabSFX = take[n];
				if (GameNetworkManager.Instance.isHostingGame == true)
				{
					currentlyGrabbingObject.itemProperties.positionOffset = (((currentlyGrabbingObject.GetComponentsInChildren<Transform>().Length > 2) && currentlyGrabbingObject.GetComponentsInChildren<Transform>()[2].name.Contains("Exploded") == true) ? position[2] : position[n]);
					currentlyGrabbingObject.itemProperties.rotationOffset = rotation[n];
				}
				currentlyGrabbingObject.itemProperties.itemName = item[n];
				if (ca.cfg5_playwithtoy.Value == true) currentlyGrabbingObject.itemProperties.toolTips = (n == 1 ? new string[1] {"Play with toy : [RMB]"} : new string[0]);
			}
		}
		[HarmonyPatch(typeof(GrabbableObject), "GrabItemOnClient"), HarmonyPrefix]
		private static void pre5(GrabbableObject __instance)
		{
			if (item[0] != null && __instance != null && __instance.itemProperties.name == "EnginePart1" && __instance.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				int n = (__instance.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? 1 : 0);
				__instance.itemProperties.positionOffset = (((__instance.GetComponentsInChildren<Transform>().Length > 2) && __instance.GetComponentsInChildren<Transform>()[2].name.Contains("Exploded") == true) ? position[2] : position[n]);
				__instance.itemProperties.rotationOffset = rotation[n];
			}
		}
		[HarmonyPatch(typeof(PlayerControllerB), "SwitchToItemSlot"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn8(IEnumerable<CodeInstruction> Instrs)
		{
			if (ca.cfg5_scrap.Value == true)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					yield return l[n];
					if (n < (l.Count - 4) && l[n + 4].ToString() == "callvirt virtual void GrabbableObject::EquipItem()")
					{
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("hold_item"));
					}
					//ca.mls.LogInfo(l[n].ToString());
				}
			}
		}
		public static void hold_item(PlayerControllerB player)
		{
			GrabbableObject _item = player.ItemSlots[player.currentItemSlot];
			if (item[0] != null && _item != null && _item.itemProperties.name == "EnginePart1" && _item.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				int n = (_item.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? 1 : 0);
				_item.itemProperties.grabSFX = take[n];
				PlayerControllerB local_player = GameNetworkManager.Instance.localPlayerController;
				if (player == local_player || local_player.ItemSlots[local_player.currentItemSlot] == null || local_player.ItemSlots[local_player.currentItemSlot].itemProperties.name != "EnginePart1" || local_player.isPlayerDead == true)
				{
					_item.itemProperties.itemName = item[n];
					if (ca.cfg5_playwithtoy.Value == true) _item.itemProperties.toolTips = (n == 1 ? new string[1] {"Play with toy : [RMB]"} : new string[0]);
					if (_item.isHeld == true || player != local_player)
					{
						_item.itemProperties.positionOffset = (((_item.GetComponentsInChildren<Transform>().Length > 2) && _item.GetComponentsInChildren<Transform>()[2].name.Contains("Exploded") == true) ? position[2] : position[n]);
						_item.itemProperties.rotationOffset = rotation[n];
					}
				}
			}
		}
		[HarmonyPatch(typeof(HUDManager), "DisplayNewScrapFound"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn9(IEnumerable<CodeInstruction> Instrs)
		{
			if (ca.cfg5_scrap.Value == true)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					if (n < (l.Count - 1) && l[n + 1].ToString() == "callvirt UnityEngine.Renderer[] UnityEngine.GameObject::GetComponentsInChildren()")
					{
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						yield return new CodeInstruction(OpCodes.Ldloc_0);
						yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("display_cruiser"));
					}
					yield return l[n];
					//ca.mls.LogInfo(l[n].ToString());
				}
			}
		}
		public static void display_cruiser(HUDManager instance, GameObject displayingObject)
		{
			if (item[0] != null && instance.itemsToBeDisplayed[0] != null && instance.itemsToBeDisplayed[0].itemProperties.name == "EnginePart1" && displayingObject.name == "EnginePart(Clone)" && instance.itemsToBeDisplayed[0].GetComponentInChildren<ScanNodeProperties>() != null)
			{
				int n = (instance.itemsToBeDisplayed[0].GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? 1 : 0);
				instance.itemsToBeDisplayed[0].itemProperties.itemName = item[n];
				if (n == 1)
				{
					displayingObject.GetComponent<MeshFilter>().mesh = null;
					if (instance.itemsToBeDisplayed[0].transform.childCount >= 2)
					{
						if (instance.itemsToBeDisplayed[0].transform.GetChild(1).name == "ToyCar(Clone)")
						{
							Transform tr = Object.Instantiate<Transform>(tree[1], displayingObject.transform);
							tr.localPosition = new Vector3(3.5f, 0f, 0f);
							tr.localEulerAngles = new Vector3(0f, 180f, 90f);
							tr.localScale = new Vector3(-6f, -6f, -6f);
							tr.gameObject.SetActive(true);
						}
						else if (instance.itemsToBeDisplayed[0].transform.GetChild(1).name == "ToyCar(Exploded)(Clone)")
						{
							Transform tr = Object.Instantiate<Transform>(tree[0], displayingObject.transform);
							tr.localPosition = new Vector3(21.5f, 0f, 0f);
							tr.localEulerAngles = new Vector3(0f, 90f, 180f);
							tr.localScale = new Vector3(-6f, -6f, -6f);
							tr.gameObject.SetActive(true);
						}
					}
				}
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "Awake"), HarmonyPrefix]
		private static void pre6()
		{
			disconnected = new bool[] {false, false};
			reset_local_variables("StartOfRound.Awake");
		}
		public static int discoball = 1;
		[HarmonyPatch(typeof(StartOfRound), "Awake"), HarmonyPostfix]
		private static void pst5()
		{
			if (GameNetworkManager.Instance.disableSteam == false)
			{
				if (GameNetworkManager.Instance.currentLobby.HasValue == true)
				{
					lobbyid = (GameNetworkManager.Instance.currentLobby.Value.Id % 1000000000);
					ca.mls.LogInfo(lobbyid);
				}
				else
				{
					ca.mls.LogError("current lobby id is null");
				}
			}
			if (discoball == 1 && ca.cfg5_scrap.Value == true && ca.cfg5_playwithtoy.Value == true)
			{
				discoball = 2;
				var d1 = StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Disco Ball").prefabObject.transform;
				var d2 = d1.GetComponentsInChildren<Transform>(true);
				var d3 = d2.First(_ => _.name == "AnimContainer");
				var d4 = d2.First(_ => _.name == "Audio");
				d4.SetParent(d1);
				Object.DestroyImmediate(d3.GetComponent<CozyLights>());
				var d5 = d1.gameObject.AddComponent<CozyLights>();
				d5.cozyLightsAnimator = d3.GetComponent<Animator>();
				d5.turnOnAudio = d4.GetComponent<AudioSource>();
				discoball = 3;
			}
		}

//		// playing with toy event //
		public static string[] red_toy = new string[] {
			"r/Music1^playMusic1^follow_effect,stopMusic",
			"r/Music2^playMusic2^follow_effect,stopMusic",
			"r/Music3^playMusic3^follow_effect,stopMusic",
			"r/Music4^playMusic4^follow_effect,stopMusic",
			"r/ExtremeStress^extremeStress^follow_effect,switchGear1/switchGear2,duration,60",
			"r/SwitchGear^switchGear1/switchGear2/switchGear3",
			"r/PressSpace^jump/boost",
			"r/SetPlayerInCar^sitDown",
			"r/Key^insertKey/twistKey/removeKey",
			"r/Collisions1^minCollision1/minCollision2^chance_percent_one,30,chance_effect_one,maxCollision1/maxCollision2/breakWindshield",
			"r/Collisions2^medCollision1/medCollision2/medCollision3^chance_percent_one,30,chance_effect_one,maxCollision1/maxCollision2/breakWindshield",
			"r/FrontHood^openFrontHood^follow_effect,closeFrontHood,chance_percent_two,40,chance_effect_two,addHealth/addBoost",
			"r/Rolling^startRolling^follow_effect,stopRolling",
			"r/LeftDoor^openLeftDoor^follow_effect,closeLeftDoor,chance_percent_two,5,chance_effect_two,sitDown",
			"r/RightDoor^openRightDoor^follow_effect,closeRightDoor,chance_percent_two,5,chance_effect_two,sitDown",
			"r/Spring^spring",
			"r/Windshield^enableWind^follow_effect,disableWind",
			"r/CabinWindow^closeWindow^follow_effect,openWindow",
			"r/LiftGlass^openGlass^follow_effect,closeGlass,chance_percent_two,30,chance_effect_two,spring",
			"r/FrontHoodFire^startFire^follow_effect,stopFire,chance_percent_two,10,chance_effect_two,addHealth",
			"r/StorageDoor^openBackDoor^follow_effect,closeBackDoor,chance_percent_two,15,chance_effect_two,switchLight",
			"r/Headlights^enableHeadlights^follow_effect,disableHeadlights",
			"r/StorageLightSwitch^switchLight",
			"r/Magnet^enableMagnet1/enableMagnet2^follow_effect,disableMagnet",
			"r/OpenRandomDoors^openDoors^follow_effect,closeDoors,rarity,80,shion",
			"r/ScaleToyCar^decreaseScale^follow_effect,resetScale,duration,90,rarity,60",
			"r/ColoredHeadlights^enableHeadlights^follow_effect,disableHeadlights,rarity,60,shion",
			"r/Pumpkin^spawnPumpkin^chain,chain_end_effect,spawnPumpkin,chain_end_next,despawnPumpkin,chain_timer_override,rarity,40",
			"r/Plushie^spawnPlushie^chain,chain_end_effect,spawnPlushie,chain_end_next,despawnPlushie,chain_timer_override,rarity,70",
			"r/Goldfish^spawnGoldfish^follow_effect,despawnGoldfish,rarity,60",
			"r/DiscoBall^spawnDiscoBall^follow_effect,despawnDiscoBall,rarity,70",
			"r/Boombox^spawnBoombox^follow_effect,despawnBoombox,rarity,40,shion",
		};
		public static string[] kuro_toy = new string[] {
		       @"k\Collisions1^minCollision1/minCollision2",
		       @"k\Collisions2^medCollision1/medCollision2/medCollision3",
		       @"k\Collisions3^maxCollision1/maxCollision2",
		       @"k\Collisions4^obsCollision1/obsCollision2/obsCollision3",
		       @"k\ChainedCollisions^minCollision1_cd/minCollision2_cd/medCollision1_cd/medCollision2_cd/medCollision3_cd/maxCollision1_cd/maxCollision2_cd^chain",
		       @"k\ChainedCollisionsExplosion^minCollision1_cd/minCollision2_cd/medCollision1_cd/medCollision2_cd/medCollision3_cd/maxCollision1_cd/maxCollision2_cd^chain,chain_end_effect,explosion,chain_end_next,stopFire",
		       @"k\Explosion^explosion^follow_effect,stopFire,chance_percent_two,10,chance_effect_two,addHealth",
		       @"k\ChainedBoost^boost^chain,shion",
		       @"k\ChainedBoostExplosion^boost^chain,chain_end_effect,explosion,chain_end_next,stopFire,shion",
		       @"k\ExtremeStressExplosion^extremeStress^follow_effect,stopFire,chance_percent_two,100,chance_effect_two,explosion,duration,60",
		       @"k\CollectToyCar^collect^chance_percent_one,20,chance_effect_one,collectN/collect240/collect240N,follow_effect,uncollect,duration,160",
		       @"k\Pumpkin^spawnPumpkin^chain,chain_end_effect,spawnPumpkin,chain_end_next,despawnPumpkin,chain_timer_override,rarity,60",
		       @"k\Plushie^spawnPlushie^chain,chain_end_effect,spawnPlushie,chain_end_next,despawnPlushie,chain_timer_override,rarity,20",
		       @"k\Goldfish^spawnGoldfish^follow_effect,despawnGoldfish,rarity,20",
		       @"k\DiscoBall^spawnDiscoBall^follow_effect,despawnDiscoBall,rarity,60",
		       @"k\Boombox^spawnBoombox^follow_effect,despawnBoombox,rarity,60,shion",
		       //@"k\ChainedEnemyCollisions^enemyCollision^chain,shion",
		       //@"k\ChainedEnemyCollisionsExplosion^enemyCollision^chain,chain_end_effect,explosion,chain_end_next,stopFire,shion",
		};
		//r/EventName^effectName^properties	k\EventName^effectName/...^properties
		//follow_effect,effectName		follow_effect,effectName/...
		//chance_percent_one,int
		//chance_percent_two,int
		//chance_effect_one,effectName		chance_effect_one,effectName/...	(for caret[1])
		//chance_effect_two,effectName		chance_effect_two,effectName/...	(for caret[2])
		//duration,floatseconds*10
		//chain
		//chain_end_effect,effectName		chain_end_effect,effectName/...
		//chain_end_next,effectName		chain_end_next,effectName/...
		//chain_timer_override
		//shion
		//rarity
		[HarmonyPatch(typeof(GrabbableObject), "ItemActivate"), HarmonyPrefix]
		private static void play_with_toy_event(GrabbableObject __instance)
		{
			if (__instance.itemProperties.name == "EnginePart1" && __instance.GetComponentInChildren<ScanNodeProperties>() != null && __instance.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] && ca.cfg5_scrap.Value == true && ca.cfg5_playwithtoy.Value == true && __instance.playerHeldBy == GameNetworkManager.Instance.localPlayerController)
			{
				PlayWithToy play = __instance.GetComponent<PlayWithToy>();
				if (play == null || play.toycar == null || __instance.GetComponent<NetworkObject>() == null)
				{
					ca.mls.LogError("play_with_toy_event error" + (play == null ? ", PlayWithToy component was null" : "") + (play != null ? (play.toycar == null ? "ToyCar gameobject was null" : "") : "") + (__instance.GetComponent<NetworkObject>() == null ? ", NetworkObject component was null" : ""));
					return;
				}
				if (play.cooldown <= 0f)
				{
					UInt64 guid = __instance.GetComponent<NetworkObject>().NetworkObjectId;
					if (GameNetworkManager.Instance.disableSteam == false && StartOfRound.Instance.connectedPlayersAmount + 1 != 1 && sync == true)
					{
						if (GameNetworkManager.Instance.isHostingGame == true)
						{
							update_clients_toy(guid, __instance);
							return;
						}
						if (client_received == true)
						{
							if (hosttoycarcfg == "true" && NetworkManager.Singleton.IsHost == false)
							{
								__instance.GetComponent<PlayWithToy>().cooldown = 0.8f;
								string message = "ToyCarRequest^" + guid;
								FastBufferWriter w = new FastBufferWriter(FastBufferWriter.GetWriteSize(message), Unity.Collections.Allocator.Temp);
								w.WriteValueSafe(message);
								NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Cruiser_Additions-Host", NetworkManager.ServerClientId, w, NetworkDelivery.ReliableFragmentedSequenced);
								w.Dispose();
								return;
							}
						}
						else if (play.time_since_spawned < 8f)
						{
							return;
						}
					}
					play_with_toy(guid, __instance, true, "nil");
				}
			}
		}
		private static string play_with_toy(UInt64 guid, GrabbableObject _item = null, bool determine = false, string effect = "nil")
		{
			if (ca.cfg5_scrap.Value == true && ca.cfg5_playwithtoy.Value == true)
			{
				if (_item == null) { PlayWithToy temp = Object.FindObjectsByType<PlayWithToy>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(_ => _.GetComponent<NetworkObject>() != null && _.GetComponent<NetworkObject>().NetworkObjectId == guid); if (temp != null) _item = temp.gameObject.GetComponent<GrabbableObject>(); }
				if (_item != null && _item.itemProperties.name == "EnginePart1" && _item.GetComponentInChildren<ScanNodeProperties>() != null && _item.GetComponentInChildren<ScanNodeProperties>().headerText == item[1])
				{
					PlayWithToy play = _item.GetComponent<PlayWithToy>();
					if (play != null && play.toycar != null)
					{
						if (determine == true && effect == "nil")
						{
							if (play.cooldown > 0f) return "nil";
							effect = play.determine_next_event();
						}
						else if ((effect.StartsWith("r.") == true && play.toycar.name != "ToyCar(Clone)") || (effect.StartsWith("k.") == true && play.toycar.name != "ToyCar(Exploded)(Clone)"))
						{
							ca.mls.LogError("play_with_toy error, effect identifier doesn't match toy car type. " + effect + " " + play.toycar + (_item.transform.childCount >= 2 ? " " + _item.transform.GetChild(1).name : ""));
							return "nil";
						}
						try { play.play_effect(effect); } catch (System.Exception error) { ca.mls.LogError("! play_effect error, caught error while playing effect " + effect + ". " + play.toycar.name + ", " + determine + ", " + play.previous_event + " !\n" + error); }
						return effect;
					}
					if (play == null) ca.mls.LogError("play_with_toy error, PlayWithToy component was null");
					else if (play.toycar == null) ca.mls.LogError("play_with_toy error, ToyCar gameobject was null");
				}
				if (_item == null) ca.mls.LogError("play_with_toy error, item was null or item wasn't a toy car. " + client_received.ToString().ToLower() + ", " + hosttoycarcfg + ", " + (StartOfRound.Instance.connectedPlayersAmount + 1) + ", " + NetworkManager.Singleton.IsHost.ToString().ToLower());
			}
			return "nil";
		}

//		// rotation quaternions must be unit length + start position/rotation //
		[HarmonyPatch(typeof(StartOfRound), "SyncShipUnlockablesClientRpc"), HarmonyPostfix]
		private static void pst6(StartOfRound __instance)
		{
			if (ca.cfg7_unlockables.Value == true && __instance.attachedVehicle != null && GameNetworkManager.Instance.isHostingGame == false)
			{
				typeof(VehicleController).GetField("magnetTargetRotation", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance.attachedVehicle, new Quaternion(0f, 0.7071068f, 0f, 0.7071068f));
				typeof(VehicleController).GetField("magnetStartRotation", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance.attachedVehicle, new Quaternion(0f, 0.7071068f, 0f, 0.7071068f));
				typeof(VehicleController).GetField("magnetTargetPosition", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance.attachedVehicle, (__instance.magnetPoint.position - __instance.magnetPoint.forward * 5.5f));
				typeof(VehicleController).GetField("magnetStartPosition", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance.attachedVehicle, (__instance.magnetPoint.position + __instance.magnetPoint.forward * 5f));
				ca.mls.LogInfo("attached cruiser to the magnet");
			}
		}

//		// QueryTriggerInteraction //
		[HarmonyPatch(typeof(VehicleController), "CollectItemsInTruck"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn10(IEnumerable<CodeInstruction> Instrs)
		{
			if (ca.cfg8_collide.Value == true)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					if (n < (l.Count - 1) && l[n + 1].ToString() == "call static UnityEngine.Collider[] UnityEngine.Physics::OverlapSphere(UnityEngine.Vector3 position, float radius, int layerMask, UnityEngine.QueryTriggerInteraction queryTriggerInteraction)")
					{
						yield return new CodeInstruction(OpCodes.Ldc_I4_2);
					}
					else
					{
						yield return l[n];
					}
					//ca.mls.LogInfo(l[n].ToString());
				}
			}
		}

//		// cruiser 1up //
		[HarmonyPatch(typeof(Terminal), "LoadNewNodeIfAffordable"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn11(IEnumerable<CodeInstruction> Instrs)
		{
			if (ca.cfg9_1up.Value == true)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					if (l[n].ToString() == "call static VehicleController UnityEngine.Object::FindObjectOfType()")
					{
						yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("return_exploded"));
						n = n + 1;
					}
					else
					{
						yield return l[n];
					}
					//ca.mls.LogInfo(l[n].ToString());
				}
			}
		}
		public static bool return_exploded()
		{
			return (bool)Object.FindObjectsByType<VehicleController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(_ => (_ != null && _.carDestroyed == false));
		}

//		// input //
		[HarmonyPatch(typeof(VehicleController), "DoTurboBoost"), HarmonyPrefix]
		private static bool pre7()
		{
			return (ca.cfgr_input.Value == true && GameNetworkManager.Instance.localPlayerController.isTypingChat == true ? false : true);
		}
		[HarmonyPatch(typeof(VehicleController), "GetVehicleInput"), HarmonyPrefix]
		private static bool pre8()
		{
			return (ca.cfgr_input.Value == true && GameNetworkManager.Instance.localPlayerController.isTypingChat == true ? false : true);
		}
		[HarmonyPatch(typeof(VehicleController), "SetCarEffects"), HarmonyPrefix]
		private static void pre9(VehicleController __instance, ref float setSteering, ref float ___steeringWheelAnimFloat)
		{
			if (__instance.localPlayerInControl)
			{
				setSteering = 0f;
				___steeringWheelAnimFloat = __instance.steeringInput / 6f;
			}
		}
		[HarmonyPatch(typeof(VehicleController), "UseTurboBoostLocalClient"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn12(IEnumerable<CodeInstruction> Instrs)
		{
			if (ca.cfgb_keybind.Value != Key.None)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					if (l[n].ToString() == "ldfld bool VehicleController::ignitionStarted")
					{
						yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("return_ignition_plus_keybind"));
					}
					else
					{
						yield return l[n];
					}
					//ca.mls.LogInfo(l[n].ToString());
				}
			}
		}
		public static bool return_ignition_plus_keybind(VehicleController self)
		{
			if (self.ignitionStarted == true && (ca.cfgb_keybind.Value == Key.None || Keyboard.current[ca.cfgb_keybind.Value].isPressed == true))
			{
				return true;
			}
			return false;
		}

//		// network syncing //
		private static bool sync = false;

		private static string hostmoveitems = "nil";

		private static string hostscraponly = "nil";

		private static string hosttoycarcfg = "nil";

		private static string seeds = "nil";

		private static bool[] disconnected = new bool[] {false, false};

		private static int[] synced_percents = new int[] {-1, -1, -1};

		private static bool client_received = false;

		[HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject"), HarmonyPostfix]
		private static void pst7()
		{
			if (sync == false)
			{
				if (NetworkManager.Singleton.IsHost == true)
				{
					NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("4902.Cruiser_Additions-Host", host_receive);
				}
				else
				{
					ca.mls.LogInfo("requesting message from host");
					NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("4902.Cruiser_Additions-Client", client_receive);
					FastBufferWriter w = new FastBufferWriter(0, Unity.Collections.Allocator.Temp);
					NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Cruiser_Additions-Host", NetworkManager.ServerClientId, w);
					w.Dispose();
				}
				sync = true;
			}
		}
		private static void host_receive(ulong id, FastBufferReader r)
		{
			if (NetworkManager.Singleton.IsHost == true)
			{
				if (r.TryBeginRead(1) == true)
				{
					string request;
					r.ReadValueSafe(out request, false);
					if (request.StartsWith("LightSwitchRequest") == true)
					{
						int n = 2;
						if (count_carets(request, n) == false) return;
						string[] lsr = request.Split(new char[]{'^'}, n + 1);
						update_clients_lights(UInt64.Parse(lsr[1]), bool.Parse(lsr[2]));
						return;
					}
					if (request.StartsWith("ToyCarRequest") == true && ca.cfg5_scrap.Value == true && ca.cfg5_playwithtoy.Value == true)
					{
						int n = 1;
						if (count_carets(request, n) == false) return;
						string[] tcr = request.Split(new char[]{'^'}, n + 1);
						update_clients_toy(UInt64.Parse(tcr[1]));
						return;
					}
				}
				ca.mls.LogInfo("received request from client");
				string message = ((ca.cfg2_moveitems.Value == 2 || ca.cfg2_moveitems.Value == 3) ? "true" : "false") + "^" + ca.cfg2_scraponly.Value.ToString().ToLower() + "^" + (ca.cfg5_scrap.Value && ca.cfg5_playwithtoy.Value).ToString().ToLower() + "^1," + ca.cfg5_cruiser.Value + "," + ca.cfg5_cruiseralt.Value + "^" + lobbyid + "^" + seeds;
				ca.mls.LogInfo("sending message " + message);
				FastBufferWriter w = new FastBufferWriter(FastBufferWriter.GetWriteSize(message), Unity.Collections.Allocator.Temp);
				w.WriteValueSafe(message);
				NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Cruiser_Additions-Client", id, w, NetworkDelivery.ReliableFragmentedSequenced);
				w.Dispose();
			}
		}
		private static void client_receive(ulong id, FastBufferReader r)
		{
			if (NetworkManager.Singleton.IsHost == false)
			{
				string message;
				r.ReadValueSafe(out message, false);
				int n = 5;
				if (message.StartsWith("LightSwitchReceive") == true)
				{
					n = 2;
					if (count_carets(message, n) == false) return;
					string[] lsr = message.Split(new char[]{'^'}, n + 1);
					switch_light(UInt64.Parse(lsr[1]), bool.Parse(lsr[2]));
					return;
				}
				if (message.StartsWith("ToyCarReceive") == true)
				{
					n = 2;
					if (count_carets(message, n) == false) return;
					string[] tcr = message.Split(new char[]{'^'}, n + 1);
					play_with_toy(UInt64.Parse(tcr[1]), null, false, tcr[2]);
					return;
				}
				client_received = true;
				ca.mls.LogInfo("client received message " + message);
				if (count_carets(message, n) == false) return;
				string[] s = message.Split(new char[]{'^'}, n + 1);
				hostmoveitems = s[0];
				hostscraponly = s[1];
				hosttoycarcfg = s[2];
				synced_percents = System.Array.ConvertAll(s[3].split(","), System.Convert.ToInt32);
				lobbyid = UInt64.Parse(s[4]);
				seeds = s[5];
			}
		}
		private static void update_clients_lights(UInt64 guid, bool red)
		{
			if (NetworkManager.Singleton.IsHost == true)
			{
				switch_light(guid, red);
				string message = "LightSwitchReceive^" + guid + "^" + red;
				FastBufferWriter w = new FastBufferWriter(FastBufferWriter.GetWriteSize(message), Unity.Collections.Allocator.Temp);
				w.WriteValueSafe(message);
				NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("4902.Cruiser_Additions-Client", w, NetworkDelivery.ReliableFragmentedSequenced);
				w.Dispose();
			}
		}
		private static void update_clients_toy(UInt64 guid, GrabbableObject _item = null)
		{
			if (NetworkManager.Singleton.IsHost == true)
			{
				string effect = play_with_toy(guid, _item, true, "nil");
				if (effect != "nil")
				{
					string message = "ToyCarReceive^" + guid + "^" + effect;
					FastBufferWriter w = new FastBufferWriter(FastBufferWriter.GetWriteSize(message), Unity.Collections.Allocator.Temp);
					w.WriteValueSafe(message);
					NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("4902.Cruiser_Additions-Client", w, NetworkDelivery.ReliableFragmentedSequenced);
					w.Dispose();
				}
			}
		}
		private static bool count_carets(string message = "counting carets", int n = 1)
		{
			if ((message.Length - message.Replace("^", "").Length) == n) return true;
			ca.mls.LogError("received message was not what was expected. wasn't able to sync variables with host. (are the mod versions not the same?)");
			ca.mls.LogError("found " + (message.Length - message.Replace("^", "").Length) + "/" + n + " ^ in message " + message);
			return false;
		}
		[HarmonyPatch(typeof(GameNetworkManager), "Disconnect"), HarmonyPrefix]
		private static void pre10()
		{
			disconnected[0] = true;
		}
		[HarmonyPatch(typeof(GameNetworkManager), "Disconnect"), HarmonyPostfix]
		private static void pst8()
		{
			reset_local_variables("GameNetworkManager.Disconnect");
		}
		[HarmonyPatch(typeof(StartOfRound), "OnDisable"), HarmonyPrefix]
		private static void pre11()
		{
			reset_local_variables("StartOfRound.OnDisable");
			if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
			{
				try { NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("4902.Cruiser_Additions-Host"); NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("4902.Cruiser_Additions-Client"); } catch (System.Exception error) { ca.mls.LogError(error); }
			}
		}
		private static void reset_local_variables(string s)
		{
			sync = false;
			hostmoveitems = "nil";
			hostscraponly = "nil";
			hosttoycarcfg = "nil";
			seeds = "nil";
			lobbyid = 0uL;
			first_item = new bool[] {true, true, true};
			saved_engine = "";
			loaded_engine = new List<string>();
			synced_percents = new int[] {-1, -1, -1};
			client_received = false;
			ca.mls.LogInfo("reset local variables (" + s + ")");
		}

//		// saving/loading //
		private static string saved_engine = "";

		private static List<string> loaded_engine = new List<string>();

		[HarmonyPatch(typeof(GameNetworkManager), "SaveItemsInShip"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn13(IEnumerable<CodeInstruction> Instrs)
		{
			if (ca.cfg5_scrap.Value == true && ca.cfg5_saveseeds.Value == true)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					yield return l[n];
					if (l[n].ToString() == "callvirt virtual void System.Collections.Generic.List<UnityEngine.Vector3>::Add(UnityEngine.Vector3 item)")
					{
						yield return new CodeInstruction(OpCodes.Ldloc_0);
						yield return new CodeInstruction(OpCodes.Ldloc, 6);
						yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("save_engine"));
					}
					//ca.mls.LogInfo(l[n].ToString());
				}
			}
		}
		public static void save_engine(GrabbableObject[] items, int index)
		{
			if (ca.cfg5_scrap.Value == true && ca.cfg5_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && items[index].itemProperties.name == "EnginePart1")
			{
				if (items[index].GetComponent<ItemTypeSeed>() != null)
				{
					ItemTypeSeed component = items[index].GetComponent<ItemTypeSeed>();
					saved_engine = saved_engine + component.seed + "/";
				}
				else if (items[index].GetComponent<NetworkObject>() != null)
				{
					saved_engine = saved_engine + (lobbyid + items[index].GetComponent<NetworkObject>().NetworkObjectId).ToString() + "/";
					ca.mls.LogInfo("ItemTypeSeed is null! saving seed for this item as lobbyid+networkobjectid");
				}
				else
				{
					saved_engine = saved_engine + new Shion().next32mm(1, 101).ToString() + "/";
					ca.mls.LogInfo("ItemTypeSeed and NetworkObject are null! saving seed for this item as a random number from 1 to 100");
				}
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "SaveGame"), HarmonyPostfix]
		private static void pst9()
		{
			if (ca.cfg5_scrap.Value == true && ca.cfg5_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && StartOfRound.Instance.inShipPhase == true && StartOfRound.Instance.isChallengeFile == false)
			{
				try
				{
					if (saved_engine == "") saved_engine = "nil";
					if (saved_engine.EndsWith("/") == true) saved_engine = saved_engine.Substring(0, saved_engine.Length - 1);
					ca.mls.LogInfo("saving " + saved_engine);
					ES3.Save("4902.Cruiser_Additions-1", saved_engine, GameNetworkManager.Instance.currentSaveFileName);
					saved_engine = "";
				}
				catch (System.Exception error)
				{
					ca.mls.LogError("Error saving item types: " + error);
				}
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "LoadShipGrabbableItems"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn14(IEnumerable<CodeInstruction> Instrs)
		{
			if (ca.cfg5_scrap.Value == true && ca.cfg5_saveseeds.Value == true)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					yield return l[n];
					if (l[n].ToString() == "callvirt void Unity.Netcode.NetworkObject::Spawn(bool destroyWithScene)")
					{
						yield return new CodeInstruction(OpCodes.Ldloc_0);
						yield return new CodeInstruction(OpCodes.Call, typeof(cruiser_additions).GetMethod("load_engine"));
					}
					//ca.mls.LogInfo(l[n].ToString());
				}
			}
		}
		public static void load_engine(GrabbableObject item)
		{
			if (ca.cfg5_scrap.Value == true && ca.cfg5_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && item.itemProperties.name == "EnginePart1")
			{
				if (item.GetComponent<NetworkObject>() != null)
				{
					loaded_engine.Add(item.GetComponent<NetworkObject>().NetworkObjectId.ToString());
				}
				else
				{
					loaded_engine.Add("nil");
					ca.mls.LogInfo("NetworkObject is null! the seed can't be loaded for this item");
				}
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "Start"), HarmonyPostfix]
		private static void pst10()
		{
			if (ca.cfg5_scrap.Value == true && ca.cfg5_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false)
			{
				try
				{
					string temp = ES3.Load("4902.Cruiser_Additions-1", GameNetworkManager.Instance.currentSaveFileName, "nil");
					ca.mls.LogInfo("loaded " + temp);
					string[] s = temp.split("/");
					if (s[0] != "nil" && s[0] != "" && s.Length == loaded_engine.Count)
					{
						seeds = "";
						for (int n = 0; n < loaded_engine.Count; n = n + 1)
						{
							seeds = seeds + loaded_engine[n] + "." + s[n] + "/";
						}
						ca.mls.LogInfo("current networkobjectids + saved seeds " + seeds);
					}
					loaded_engine = new List<string>();
				}
				catch (System.Exception error)
				{
					ca.mls.LogError("Error loading item types: " + error);
				}
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "ResetSavedGameValues"), HarmonyPrefix]
		private static void pre12(GameNetworkManager __instance)
		{
			seeds = "nil";
			saved_engine = "";
			loaded_engine = new List<string>();
			if (__instance.isHostingGame == true && ES3.KeyExists("4902.Cruiser_Additions-1", __instance.currentSaveFileName) == true)
			{
				ES3.DeleteKey("4902.Cruiser_Additions-1", __instance.currentSaveFileName);
			}
		}
	}

//	// custom component //
	public class cruiser_light_switch_component : MonoBehaviour
	{
		public string guid = ca.harmony.Id;

		public void Awake()
		{
			if (this.name == "cruiser_light_switch(Clone)")
			{
				this.GetComponentInChildren<InteractTrigger>().onInteract.AddListener(new UnityAction<PlayerControllerB>(placeholderfunction));
			}
		}
		public void placeholderfunction(PlayerControllerB player)
		{
			cruiser_additions.switch_light_event(player);
		}
	}

//	// custom component //
	public class cruiser_magnet_lever_component : MonoBehaviour
	{
		public string guid = ca.harmony.Id;

		public void Awake()
		{
			if (this.name == "cruiser_magnet_lever(Clone)")
			{
				this.GetComponentInChildren<InteractTrigger>().onInteract.AddListener(new UnityAction<PlayerControllerB>(placeholderfunction));
			}
		}
		public void placeholderfunction(PlayerControllerB player)
		{
			StartOfRound.Instance.magnetLever.GetComponent<InteractTrigger>().onInteract.Invoke(player);
		}
	}

//	// custom component //
	public class PlayWithToy : MonoBehaviour
	{
		public string guid = ca.harmony.Id;

		public Transform toycar = null;

		public VehicleController vehicle = null;

		public string[] redkuro = null;

		public string next_effect = "nil";
		public string previous_event = "nil";
		public string previous_effect = "nil";
		public int chain_percent = 135;

		public Animator[] savedAnimators;
		public table<string, Animator> saved_a = new table<string, Animator>();
		public static table<string, AudioClip> saved_ac = new table<string, AudioClip>();
		public table<string, ParticleSystem> saved_ps = new table<string, ParticleSystem>();
		public table<string, MeshRenderer> saved_mr = new table<string, MeshRenderer>();
		public table<string, GameObject> saved_go = new table<string, GameObject>();
		public static table<string, Material> saved_mt = new table<string, Material>();

		public AudioSource audio = null;
		public AudioSource mixed = null;
		public AudioSource third = null;
		public bool mixed_active = false;
		public float mixed_lowest;
		public float mixed_highest;
		public float mixed_lerpSpeed;

		public float cooldown = 0f;
		public float time_since_spawned = 0f;
		public float time_of_event = 0f;
		public float time_since_last_gear = 0f;
		public float mixed_disable_delay = 0f;

		public int gear = 1;
		public float gearAnimValue = 0f;

		public float kuro1080 = 0f;
		public float kuro_z = 72f;
		public float red666 = 0f;
		public float red_s = 0.1f;

		public void Awake()
		{
			savedAnimators = this.gameObject.GetComponentsInChildren<Animator>(true);
			Transform headlights = this.GetComponentsInChildren<Transform>(true).FirstOrDefault(_ => _.name == "Headlights");
			if (headlights != null)
			{
				headlights.gameObject.SetActive(false);
				foreach (Light light in headlights.GetComponentsInChildren<Light>(true))
				{
					light.intensity = 45f;
				}
			}
		}
		public void Update()
		{
			if (cooldown > 0f) cooldown = cooldown - Time.deltaTime;
			time_since_spawned = time_since_spawned + Time.deltaTime;
			if (mixed_disable_delay > 0f)
			{
				mixed_disable_delay = mixed_disable_delay - Time.deltaTime;
				if (mixed_disable_delay <= 0f) mixed_active = false;
			}
			if (mixed_active == true)
			{
				mixed.volume = Mathf.Max(Mathf.Lerp(mixed.volume, mixed_highest, mixed_lerpSpeed * Time.deltaTime), mixed_lowest);
			}
			else if (mixed.isPlaying == true && mixed.clip != null && mixed.clip.name != "Fireplace")
			{
				mixed.volume = Mathf.Lerp(mixed.volume, 0f, 3f * Time.deltaTime);
				if (mixed.volume == 0f) mixed.Stop();
			}
			if (saved_a["gear"] != null && time_since_spawned - time_since_last_gear < 4f)
			{
				//drive reverse park
				if (gear == 1) gearAnimValue = Mathf.MoveTowards(gearAnimValue, 0f, 15f * Time.deltaTime * (time_since_spawned - time_since_last_gear));
				else if (gear == 2) gearAnimValue = Mathf.MoveTowards(gearAnimValue, 0.5f, 15f * Time.deltaTime * (time_since_spawned - time_since_last_gear));
				else if (gear == 3) gearAnimValue = Mathf.MoveTowards(gearAnimValue, 1f, 15f * Time.deltaTime * (time_since_spawned - time_since_last_gear));
				saved_a["gear"].SetFloat("gear", Mathf.Clamp(gearAnimValue, 0.01f, 0.99f));
			}
			if (kuro1080 > 0f && toycar != null) //collect
			{
				kuro1080 = kuro1080 - Time.deltaTime;
				toycar.Rotate(0f, 0f, kuro_z * Time.deltaTime);
				if (kuro1080 <= 0f)
				{
					if (saved_mt["kuro"] == null) saved_mt["kuro"] = toycar.GetComponent<MeshRenderer>().material;
					if (saved_mt["kuro"] != null) toycar.GetComponent<MeshRenderer>().materials = new Material[] {saved_mt["kuro"]};
					toycar.localEulerAngles = new Vector3(0f, 90f, 180f);
					kuro_z = 72f;
				}
			}
			if (red666 > 0f && toycar != null) //scale
			{
				red666 = red666 - Time.deltaTime;
				red_s = (red_s + 1.8f) * Time.deltaTime;
				if ((toycar.localScale.x + red_s) < 0f) toycar.localScale = toycar.localScale + new Vector3(red_s, red_s, red_s);
				else toycar.localScale = toycar.localScale * 0f;
				if (red666 <= 0f)
				{
					red_s = 0.1f;
					toycar.localScale = new Vector3(-6f, -6f, -6f);
				}
			}
		}
		public string determine_next_event()
		{
			if (redkuro == null || toycar == null)
			{
				ca.mls.LogError("determine_next_event error, ToyCar gameobject was null." + (this.transform.childCount >= 2 ? " " + this.transform.GetChild(1).name : ""));
				return "nil";
			}
			if (previous_event != "nil")
			{
				string[] caret = previous_event.split("^");
				if (caret.Length > 2)
				{
					string[] properties = caret[2].split(",");
					if (next_effect != "nil")
					{
						if (caret[2].Contains("duration") == true && caret[2].Contains("chance_percent_one") == true && !(time_since_spawned - time_of_event < (float.Parse(properties[properties.indexof("duration") + 1]) / 10))) { next_effect = "nil"; return new_event(); }
						previous_effect = return_one_effect(next_effect);
						next_effect = "nil";
						return previous_event[0] + "." + previous_effect;
					}
					if (caret[2].Contains("chain") == true && caret[1].Contains(previous_effect) == true)
					{
						if (time_since_spawned - time_of_event < 2f)
						{
							if (chain_percent > 100 || new Shion().next32mm(0, 100) < chain_percent)
							{
								chain_percent = chain_percent - 15;
								time_of_event = time_since_spawned;
								previous_effect = return_one_effect(caret[1]);
								return previous_event[0] + "." + previous_effect + (caret[2].Contains("shion") == true ? "/" + new Shion().next32() : "");
							}
							else if (caret[2].Contains("chain_end_effect") == true)
							{
								if (caret[2].Contains("chain_end_next") == true) next_effect = return_one_effect(properties[properties.indexof("chain_end_next") + 1]);
								previous_effect = return_one_effect(properties[properties.indexof("chain_end_effect") + 1]);
								return previous_event[0] + "." + previous_effect;
							}
						}
						else if (caret[2].Contains("chain_timer_override") == true && caret[2].Contains("chain_end_effect") == true)
						{
							if (caret[2].Contains("chain_end_next") == true) next_effect = return_one_effect(properties[properties.indexof("chain_end_next") + 1]);
							previous_effect = return_one_effect(properties[properties.indexof("chain_end_effect") + 1]);
							return previous_event[0] + "." + previous_effect;
						}
						return new_event();
					}
					if (caret[2].Contains("follow_effect") == true && caret[1].Contains(previous_effect) == true)
					{
						if (caret[2].Contains("duration") == true && !(time_since_spawned - time_of_event < (float.Parse(properties[properties.indexof("duration") + 1]) / 10))) return new_event();
						string follow = return_one_effect(properties[properties.indexof("follow_effect") + 1]);
						if (caret[2].Contains("chance_percent_two") == true && new Shion().next32mm(0, 100) < int.Parse(properties[properties.indexof("chance_percent_two") + 1]))
						{
							next_effect = follow;
							previous_effect = return_one_effect(properties[properties.indexof("chance_effect_two") + 1]);
							return previous_event[0] + "." + previous_effect;
						}
						previous_effect = follow;
						return previous_event[0] + "." + previous_effect;
					}
				}
			}
			return new_event();
		}
		public string new_event()
		{
			previous_event = redkuro[new Shion().next32mm(0, redkuro.Length)];
			time_of_event = time_since_spawned;
			chain_percent = 135;
			string[] caret = previous_event.split("^");
			if (caret.Length > 2)
			{
				string[] properties = caret[2].split(",");
				if (previous_event.Contains("rarity") == true && !(new Shion().next32mm(0, 100) < int.Parse(properties[properties.indexof("rarity") + 1]))) return new_event();
				if (caret[2].Contains("chance_percent_one") == true && new Shion().next32mm(0, 100) < int.Parse(properties[properties.indexof("chance_percent_one") + 1]))
				{
					if (caret[2].Contains("follow_effect") == true) next_effect = return_one_effect(properties[properties.indexof("follow_effect") + 1]);
					previous_effect = return_one_effect(properties[properties.indexof("chance_effect_one") + 1]);
					return previous_event[0] + "." + previous_effect;
				}
			}
			previous_effect = return_one_effect(caret[1]);
			return previous_event[0] + "." + previous_effect + (caret.Length > 2 && caret[2].Contains("shion") == true ? "/" + new Shion().next32() : "");
		}
		public string return_one_effect(string s)
		{
			if (s.Contains("/") == true)
			{
				string[] r = s.split("/");
				s = r[new Shion().next32mm(0, r.Length)];
			}
			return s;
		}
		public void play_effect(string effect)
		{
			if (effect == "nil")
			{
				ca.mls.LogError("play_effect error, effect was nil");
				return;
			}
			if (toycar == null)
			{
				ca.mls.LogError("play_effect error, ToyCar gameobject was null." + (this.transform.childCount >= 2 ? " " + this.transform.GetChild(1).name : ""));
				return;
			}
			if (effect.StartsWith("r.") == false && effect.StartsWith("k.") == false)
			{
				ca.mls.LogError("play_effect error, effect identifier was missing. " + effect);
				return;
			}
			if (vehicle == null || audio == null || mixed == null || third == null)
			{
				ca.mls.LogError("play_effect error" + (vehicle == null ? ", vehicle reference was null" : "") + (audio == null || mixed == null || third == null ? ", AudioSource component/s was null" : ""));
				return;
			}

//			// red //
			float cd = 0.5f;
			audio.volume = 0.5f;
			third.volume = 0.5f;
			if (effect.StartsWith("k.") == true) {}
			else if (effect == "r.playMusic1")
			{
				audio.clip = vehicle.radioClips[0];
				audio.Play();
			}
			else if (effect == "r.playMusic2")
			{
				audio.clip = vehicle.radioClips[1];
				audio.Play();
			}
			else if (effect == "r.playMusic3")
			{
				audio.clip = vehicle.radioClips[2];
				audio.Play();
			}
			else if (effect == "r.playMusic4")
			{
				audio.clip = vehicle.radioClips[3];
				audio.Play();
			}
			else if (effect == "r.stopMusic")
			{
				audio.Stop();
				cd = 0.2f;
			}
			else if (effect == "r.extremeStress")
			{
				if (saved_a["gear"] == null) saved_a["gear"] = savedAnimators.FirstOrDefault(_ => _.name == "GearStickContainer");
				rsnv(saved_a, "gear", effect);
				time_since_last_gear = time_since_spawned;
				gear = 3;
				mixed.volume = 0f;
				mixed.Stop();
				mixed.clip = vehicle.extremeStressAudio.clip;
				mixed_lowest = 0.2f;
				mixed_highest = 0.5f;
				mixed_lerpSpeed = 1f;
				mixed.Play();
				mixed_active = true;
				mixed_disable_delay = 5f;
				cd = 0.2f;
			}
			else if (effect == "r.switchGear1")
			{
				if (saved_a["gear"] == null) saved_a["gear"] = savedAnimators.FirstOrDefault(_ => _.name == "GearStickContainer");
				rsnv(saved_a, "gear", effect);
				time_since_last_gear = time_since_spawned;
				gear = 1;
				audio.volume = 1f;
				audio.PlayOneShot(vehicle.gearStickAudios[0]);
				mixed_disable_delay = 0.2f;
			}
			else if (effect == "r.switchGear2")
			{
				if (saved_a["gear"] == null) saved_a["gear"] = savedAnimators.FirstOrDefault(_ => _.name == "GearStickContainer");
				rsnv(saved_a, "gear", effect);
				time_since_last_gear = time_since_spawned;
				gear = 2;
				audio.volume = 1f;
				audio.PlayOneShot(vehicle.gearStickAudios[1]);
				mixed_disable_delay = 0.2f;
			}
			else if (effect == "r.switchGear3")
			{
				if (saved_a["gear"] == null) saved_a["gear"] = savedAnimators.FirstOrDefault(_ => _.name == "GearStickContainer");
				rsnv(saved_a, "gear", effect);
				time_since_last_gear = time_since_spawned;
				gear = 3;
				audio.volume = 1f;
				audio.PlayOneShot(vehicle.gearStickAudios[2]);
			}
			else if (effect == "r.jump")
			{
				audio.volume = 1f;
				audio.PlayOneShot(vehicle.jumpInCarSFX);
			}
			else if (effect == "r.boost")
			{
				audio.PlayOneShot(vehicle.turboBoostSFX);
				audio.PlayOneShot(vehicle.turboBoostSFX2);
			}
			else if (effect == "r.sitDown")
			{
				audio.PlayOneShot(vehicle.sitDown);
			}
			else if (effect == "r.breakWindshield")
			{
				if (saved_ps["brokenGlass"] == null) saved_ps["brokenGlass"] = this.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(_ => _.name == "Particle System(Clone)");
				if (saved_mr["mainBodyMesh"] == null) saved_mr["mainBodyMesh"] = this.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(_ => _.name == "MainBody");
				if (rsnv(saved_ps, "brokenGlass", effect) == true && rsnv(saved_mr, "mainBodyMesh", effect) == true)
				{
					Material[] materials = saved_mr["mainBodyMesh"].materials;
					materials[2] = vehicle.windshieldBrokenMat;
					saved_mr["mainBodyMesh"].materials = materials;
					saved_ps["brokenGlass"].Play();
					audio.volume = 0.3f;
					audio.PlayOneShot(vehicle.windshieldBreak);
				}
			}
			else if (effect == "r.insertKey")
			{
				audio.PlayOneShot(vehicle.insertKey);
			}
			else if (effect == "r.twistKey")
			{
				audio.PlayOneShot(vehicle.twistKey);
			}
			else if (effect == "r.removeKey")
			{
				audio.PlayOneShot(vehicle.removeKey);
			}
			else if (effect == "r.minCollision1")
			{
				audio.PlayOneShot(vehicle.minCollisions[0]);
			}
			else if (effect == "r.minCollision2")
			{
				audio.PlayOneShot(vehicle.minCollisions[1]);
			}
			else if (effect == "r.medCollision1")
			{
				audio.volume = 0.4f;
				audio.PlayOneShot(vehicle.medCollisions[0]);
			}
			else if (effect == "r.medCollision2")
			{
				audio.volume = 0.4f;
				audio.PlayOneShot(vehicle.medCollisions[1]);
			}
			else if (effect == "r.medCollision3")
			{
				audio.volume = 0.4f;
				audio.PlayOneShot(vehicle.medCollisions[2]);
			}
			else if (effect == "r.maxCollision1")
			{
				audio.volume = 0.3f;
				audio.PlayOneShot(vehicle.maxCollisions[0]);
			}
			else if (effect == "r.maxCollision2")
			{
				audio.volume = 0.3f;
				audio.PlayOneShot(vehicle.maxCollisions[1]);
			}
			else if (effect == "r.openFrontHood")
			{
				if (saved_a["hood"] == null) saved_a["hood"] = savedAnimators.FirstOrDefault(_ => _.name == "CarHoodMesh");
				if (saved_ac["hoodAudio1"] == null) saved_ac["hoodAudio1"] = vehicle.GetComponentsInChildren<PlayAudioAnimationEvent>(true).First(_ => _.name == "CarHoodMesh").audioClip2;
				if (rsnv(saved_a, "hood", effect) == true && rsnv(saved_ac, "hoodAudio1", effect) == true)
				{
					saved_a["hood"].SetBool("hoodOpen", true);
					audio.PlayOneShot(saved_ac["hoodAudio1"]);
				}
			}
			else if (effect == "r.addHealth")
			{
				if (saved_ac["sprayAudio"] == null) saved_ac["sprayAudio"] = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "WeedKillerBottle").spawnPrefab.GetComponent<SprayPaintItem>().spraySFX;
				if (rsnv(saved_ac, "sprayAudio", effect) == true)
				{
					audio.PlayOneShot(saved_ac["sprayAudio"]);
					audio.PlayOneShot(vehicle.pourOil);
				}
			}
			else if (effect == "r.addBoost")
			{
				if (saved_ac["sprayAudio"] == null) saved_ac["sprayAudio"] = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "WeedKillerBottle").spawnPrefab.GetComponent<SprayPaintItem>().spraySFX;
				if (rsnv(saved_ac, "sprayAudio", effect) == true)
				{
					audio.PlayOneShot(saved_ac["sprayAudio"]);
					audio.PlayOneShot(vehicle.pourTurbo);
				}
			}
			else if (effect == "r.closeFrontHood")
			{
				if (saved_a["hood"] == null) saved_a["hood"] = savedAnimators.FirstOrDefault(_ => _.name == "CarHoodMesh");
				if (saved_ac["hoodAudio2"] == null) saved_ac["hoodAudio2"] = vehicle.GetComponentsInChildren<PlayAudioAnimationEvent>(true).First(_ => _.name == "CarHoodMesh").audioClip;
				if (rsnv(saved_a, "hood", effect) == true && rsnv(saved_ac, "hoodAudio2", effect) == true)
				{
					saved_a["hood"].SetBool("hoodOpen", false);
					audio.PlayOneShot(saved_ac["hoodAudio2"]);
				}
			}
			else if (effect == "r.startRolling")
			{
				mixed.volume = 0f;
				mixed.Stop();
				mixed.clip = vehicle.rollingAudio.clip;
				mixed_lowest = 0f;
				mixed_highest = 0.5f;
				mixed_lerpSpeed = 2f;
				mixed.Play();
				mixed_active = true;
			}
			else if (effect == "r.stopRolling")
			{
				mixed_active = false;
				cd = 0.2f;
			}
			else if (effect == "r.openLeftDoor")
			{
				if (saved_a["leftDoor"] == null) saved_a["leftDoor"] = savedAnimators.FirstOrDefault(_ => _.name == "DoorLeftContainer");
				if (saved_ac["leftDoorAudio1"] == null) saved_ac["leftDoorAudio1"] = vehicle.GetComponentsInChildren<PlayAudioAnimationEvent>(true).First(_ => _.name == "DoorLeftContainer").audioClip2;
				if (rsnv(saved_a, "leftDoor", effect) == true && rsnv(saved_ac, "leftDoorAudio1", effect) == true)
				{
					saved_a["leftDoor"].SetBool("doorOpen", true);
					audio.PlayOneShot(saved_ac["leftDoorAudio1"]);
				}
			}
			else if (effect == "r.closeLeftDoor")
			{
				if (saved_a["leftDoor"] == null) saved_a["leftDoor"] = savedAnimators.FirstOrDefault(_ => _.name == "DoorLeftContainer");
				if (saved_ac["leftDoorAudio2"] == null) saved_ac["leftDoorAudio2"] = vehicle.GetComponentsInChildren<PlayAudioAnimationEvent>(true).First(_ => _.name == "DoorLeftContainer").audioClip;
				if (rsnv(saved_a, "leftDoor", effect) == true && rsnv(saved_ac, "leftDoorAudio2", effect) == true)
				{
					saved_a["leftDoor"].SetBool("doorOpen", false);
					audio.PlayOneShot(saved_ac["leftDoorAudio2"]);
				}
			}
			else if (effect == "r.openRightDoor" || effect == "r.openRightDoor_mute")
			{
				if (saved_a["rightDoor"] == null) saved_a["rightDoor"] = savedAnimators.FirstOrDefault(_ => _.name == "DoorRightContainer");
				if (saved_ac["rightDoorAudio1"] == null) saved_ac["rightDoorAudio1"] = vehicle.GetComponentsInChildren<PlayAudioAnimationEvent>(true).First(_ => _.name == "DoorRightContainer").audioClip2;
				if (rsnv(saved_a, "rightDoor", effect) == true && rsnv(saved_ac, "rightDoorAudio1", effect) == true)
				{
					saved_a["rightDoor"].SetBool("doorOpen", true);
					if (effect != "r.openRightDoor_mute") audio.PlayOneShot(saved_ac["rightDoorAudio1"]);
				}
			}
			else if (effect == "r.closeRightDoor" || effect == "r.closeRightDoor_mute")
			{
				if (saved_a["rightDoor"] == null) saved_a["rightDoor"] = savedAnimators.FirstOrDefault(_ => _.name == "DoorRightContainer");
				if (saved_ac["rightDoorAudio2"] == null) saved_ac["rightDoorAudio2"] = vehicle.GetComponentsInChildren<PlayAudioAnimationEvent>(true).First(_ => _.name == "DoorRightContainer").audioClip;
				if (rsnv(saved_a, "rightDoor", effect) == true && rsnv(saved_ac, "rightDoorAudio2", effect) == true)
				{
					saved_a["rightDoor"].SetBool("doorOpen", false);
					if (effect != "r.closeRightDoor_mute") audio.PlayOneShot(saved_ac["rightDoorAudio2"]);
				}
			}
			else if (effect == "r.spring")
			{
				if (saved_a["spring"] == null) saved_a["spring"] = savedAnimators.FirstOrDefault(_ => _.name == "DriverSeatContainer");
				if (rsnv(saved_a, "spring", effect) == true)
				{
					saved_a["spring"].SetTrigger("spring");
					audio.PlayOneShot(vehicle.springAudio.clip);
				}
				cd = 1.7f;
			}
			else if (effect == "r.enableWind")
			{
				if (saved_a["wind"] == null) saved_a["wind"] = savedAnimators.FirstOrDefault(_ => _.name == "WindwipersAnim");
				if (rsnv(saved_a, "wind", effect) == true) saved_a["wind"].SetBool("wiping", true);
			}
			else if (effect == "r.disableWind")
			{
				if (saved_a["wind"] == null) saved_a["wind"] = savedAnimators.FirstOrDefault(_ => _.name == "WindwipersAnim");
				if (rsnv(saved_a, "wind", effect) == true) saved_a["wind"].SetBool("wiping", false);
			}
			else if (effect == "r.closeWindow")
			{
				if (saved_a["cabinWindow"] == null) saved_a["cabinWindow"] = savedAnimators.FirstOrDefault(_ => _.name == "CabinWindowAnimContainer");
				if (rsnv(saved_a, "cabinWindow", effect) == true)
				{
					saved_a["cabinWindow"].SetBool("isOpen", false);
					audio.volume = 1f;
					audio.PlayOneShot(saved_a["cabinWindow"].GetComponent<AudioSource>().clip);
				}
				cd = 0.2f;
			}
			else if (effect == "r.openWindow")
			{
				if (saved_a["cabinWindow"] == null) saved_a["cabinWindow"] = savedAnimators.FirstOrDefault(_ => _.name == "CabinWindowAnimContainer");
				if (rsnv(saved_a, "cabinWindow", effect) == true)
				{
					saved_a["cabinWindow"].SetBool("isOpen", true);
					audio.volume = 1f;
					audio.PlayOneShot(saved_a["cabinWindow"].GetComponent<AudioSource>().clip);
				}
				cd = 0.2f;
			}
			else if (effect == "r.openGlass")
			{
				if (saved_a["glass"] == null) saved_a["glass"] = savedAnimators.FirstOrDefault(_ => _.name == "ButtonAnimContainer(Clone)");
				if (rsnv(saved_a, "glass", effect) == true) saved_a["glass"].SetBool("GlassOpen", true);
			}
			else if (effect == "r.closeGlass")
			{
				if (saved_a["glass"] == null) saved_a["glass"] = savedAnimators.FirstOrDefault(_ => _.name == "ButtonAnimContainer(Clone)");
				if (rsnv(saved_a, "glass", effect) == true) saved_a["glass"].SetBool("GlassOpen", false);
			}
			else if (effect == "r.startFire")
			{
				if (saved_a["hood"] == null) saved_a["hood"] = savedAnimators.FirstOrDefault(_ => _.name == "CarHoodMesh");
				if (saved_ac["hoodAudio1"] == null) saved_ac["hoodAudio1"] = vehicle.GetComponentsInChildren<PlayAudioAnimationEvent>(true).First(_ => _.name == "CarHoodMesh").audioClip2;
				if (saved_ps["fire1"] == null) saved_ps["fire1"] = this.gameObject.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(_ => _.name == "CarHoodFire");
				if (rsnv(saved_a, "hood", effect) == true && rsnv(saved_ac, "hoodAudio1", effect) == true && rsnv(saved_ps, "fire1", effect) == true)
				{
					mixed_active = false;
					mixed.volume = 0f;
					mixed.Stop();
					saved_a["hood"].SetBool("hoodOpen", true);
					audio.PlayOneShot(saved_ac["hoodAudio1"]);
					saved_ps["fire1"].Play();
					mixed.clip = vehicle.hoodFireAudio.clip;
					mixed.volume = 0.5f;
					mixed.Play();
				}
			}
			else if (effect == "r.stopFire")
			{
				if (saved_a["hood"] == null) saved_a["hood"] = savedAnimators.FirstOrDefault(_ => _.name == "CarHoodMesh");
				if (saved_ac["hoodAudio2"] == null) saved_ac["hoodAudio2"] = vehicle.GetComponentsInChildren<PlayAudioAnimationEvent>(true).First(_ => _.name == "CarHoodMesh").audioClip;
				if (saved_ps["fire1"] == null) saved_ps["fire1"] = this.gameObject.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(_ => _.name == "CarHoodFire");
				if (rsnv(saved_a, "hood", effect) == true && rsnv(saved_ac, "hoodAudio2", effect) == true && rsnv(saved_ps, "fire1", effect) == true)
				{
					mixed_active = false;
					mixed.Stop();
					saved_ps["fire1"].Stop();
					saved_a["hood"].SetBool("hoodOpen", false);
					mixed.volume = 0.5f;
					mixed.PlayOneShot(saved_ac["hoodAudio2"]);
				}
			}
			else if (effect == "r.openBackDoor")
			{
				if (saved_a["storage"] == null) saved_a["storage"] = savedAnimators.FirstOrDefault(_ => _.name == "BackDoorContainer");
				if (saved_ac["storageAudio1"] == null) saved_ac["storageAudio1"] = vehicle.GetComponentsInChildren<AnimatedObjectTrigger>(true).First(_ => _.name == "BackDoorContainer").boolTrueAudios[0];
				if (rsnv(saved_a, "storage", effect) == true && rsnv(saved_ac, "storageAudio1", effect) == true)
				{
					saved_a["storage"].SetBool("isOpen", true);
					third.volume = 1f;
					third.PlayOneShot(saved_ac["storageAudio1"]);
				}
			}
			else if (effect == "r.closeBackDoor")
			{
				if (saved_a["storage"] == null) saved_a["storage"] = savedAnimators.FirstOrDefault(_ => _.name == "BackDoorContainer");
				if (saved_ac["storageAudio2"] == null) saved_ac["storageAudio2"] = vehicle.GetComponentsInChildren<AnimatedObjectTrigger>(true).First(_ => _.name == "BackDoorContainer").boolFalseAudios[0];
				if (rsnv(saved_a, "storage", effect) == true && rsnv(saved_ac, "storageAudio2", effect) == true)
				{
					saved_a["storage"].SetBool("isOpen", false);
					third.volume = 1f;
					third.PlayOneShot(saved_ac["storageAudio2"]);
				}
			}
			else if (effect == "r.enableHeadlights" || (effect.Contains("r.enableHeadlights/") == true && effect.EndsWith("/") == false))
			{
				if (saved_go["headlights"] == null) saved_go["headlights"] = this.GetComponentsInChildren<Transform>(true).First(_ => _.name == "Headlights").gameObject;
				if (saved_mr["mainBodyMesh"] == null) saved_mr["mainBodyMesh"] = this.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(_ => _.name == "MainBody");
				if (rsnv(saved_go, "headlights", effect) == true && rsnv(saved_mr, "mainBodyMesh", effect) == true)
				{
					Color color = new Color(1f, 1f, 1f, 1f);
					if (effect != "r.enableHeadlights") color = Color.HSVToRGB(Mathf.Lerp(0f, 1f, (float)new Shion(UInt32.Parse(effect.split("/")[1])).next01()), 0.9f, 1f, true);
					color.a = 1f;
					foreach(Light light in saved_go["headlights"].GetComponentsInChildren<Light>()) light.color = color;
					saved_go["headlights"].SetActive(true);
					audio.volume = 1f;
					audio.PlayOneShot(vehicle.headlightsToggleSFX);
					Material cat = vehicle.headlightsOnMat;
					Material[] cats = saved_mr["mainBodyMesh"].sharedMaterials;
					cats[1] = cat;
					saved_mr["mainBodyMesh"].sharedMaterials = cats;
				}
			}
			else if (effect == "r.disableHeadlights")
			{
				if (saved_go["headlights"] == null) saved_go["headlights"] = this.GetComponentsInChildren<Transform>(true).First(_ => _.name == "Headlights").gameObject;
				if (saved_mr["mainBodyMesh"] == null) saved_mr["mainBodyMesh"] = this.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(_ => _.name == "MainBody");
				if (rsnv(saved_go, "headlights", effect) == true && rsnv(saved_mr, "mainBodyMesh", effect) == true)
				{
					saved_go["headlights"].SetActive(false);
					audio.volume = 1f;
					audio.PlayOneShot(vehicle.headlightsToggleSFX);
					Material cat = vehicle.headlightsOffMat;
					Material[] cats = saved_mr["mainBodyMesh"].sharedMaterials;
					cats[1] = cat;
					saved_mr["mainBodyMesh"].sharedMaterials = cats;
				}
			}
			else if (effect == "r.switchLight")
			{
				if (saved_ac["lightSwitchAudio"] == null) saved_ac["lightSwitchAudio"] = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "ProFlashlight").spawnPrefab.GetComponent<FlashlightItem>().flashlightClips[0];
				if (rsnv(saved_ac, "lightSwitchAudio", effect) == true)
				{
					audio.volume = 0.6f;
					audio.PlayOneShot(saved_ac["lightSwitchAudio"]);
				}
			}
			else if (effect == "r.enableMagnet1")
			{
				audio.volume = 0.8f;
				audio.PlayOneShot(StartOfRound.Instance.magnetLever.boolFalseAudios[0]);
				audio.PlayOneShot(StartOfRound.Instance.magnetOnAudio);
			}
			else if (effect == "r.enableMagnet2")
			{
				audio.volume = 0.8f;
				audio.PlayOneShot(StartOfRound.Instance.magnetLever.boolFalseAudios[0]);
				audio.PlayOneShot(StartOfRound.Instance.magnetOnAudio);
				audio.PlayOneShot(HUDManager.Instance.displayCollectedScrapSFX);
			}
			else if (effect == "r.disableMagnet")
			{
				audio.volume = 0.8f;
				audio.PlayOneShot(StartOfRound.Instance.magnetLever.boolFalseAudios[0]);
				audio.PlayOneShot(StartOfRound.Instance.magnetOffAudio);
			}
			else if (effect.Contains("r.openDoors/") == true && effect.EndsWith("/") == false)
			{
				string[] doors = new string[] {"r.openLeftDoor", "r.openRightDoor", "r.openBackDoor", "r.openFrontHood"};
				int opened = 0;
				Shion cr = new Shion(UInt32.Parse(effect.split("/")[1]));
				if (cr.next32mm(0, 100) < 20) doors[3] = "r.startFire";
				for (int n = 0; n < doors.Length; n = n + 1)
				{
					if (cr.next32mm(0, 2) < 1)
					{
						opened = opened + 1;
						play_effect(doors[n] + (n == 1 && opened == 2 ? "_mute" : ""));
					}
				}
				if (opened == 0) play_effect(doors[cr.next32mm(0, doors.Length)]);
				return;
			}
			else if (effect == "r.closeDoors")
			{
				bool temp = false;
				if (saved_a["leftDoor"] != null && saved_a["leftDoor"].GetBool("doorOpen") == true) { temp = true; play_effect("r.closeLeftDoor"); }
				if (saved_a["rightDoor"] != null && saved_a["rightDoor"].GetBool("doorOpen") == true) play_effect("r.closeRightDoor" + (temp == true ? "_mute" : ""));
				if (saved_a["storage"] != null && saved_a["storage"].GetBool("isOpen") == true) play_effect("r.closeBackDoor");
				if (saved_a["hood"] != null && saved_a["hood"].GetBool("hoodOpen") == true) play_effect("r.stopFire");
				return;
			}
			else if (effect == "r.decreaseScale")
			{
				red_s = 0.1f;
				red666 = 8f;
				cd = 3f;
			}
			else if (effect == "r.resetScale")
			{
				red666 = 0f;
				red_s = 0.1f;
				toycar.localScale = new Vector3(-6f, -6f, -6f);
				cd = 0.2f;
			}
			else if (effect == "r.spawnPumpkin")
			{
				if (saved_go["pumpkin"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "JackOLantern").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "PumpkinMesh"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(1.04f, -0.18f, 1.7f);
					tr.localEulerAngles = new Vector3(270f, 195f, 0f);
					tr.localScale = new Vector3(0.3861f, 0.3861f, 0.3861f);
					tr.GetComponentInChildren<Light>(true).intensity = 15f;
					saved_go["pumpkin"] = tr.gameObject;
				}
				if (saved_ac["pumpkinAudio"] == null) saved_ac["pumpkinAudio"] = StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "JackOLantern").prefabObject.GetComponentInChildren<AnimatedObjectTrigger>(true).boolFalseAudios[0];
				if (rsnv(saved_go, "pumpkin", effect) == true && rsnv(saved_ac, "pumpkinAudio", effect) == true)
				{
					saved_go["pumpkin"].SetActive(true);
					audio.PlayOneShot(saved_ac["pumpkinAudio"]);
				}
				cd = 0.2f;
			}
			else if (effect == "r.despawnPumpkin")
			{
				if (saved_go["pumpkin"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "JackOLantern").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "PumpkinMesh"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(1.04f, -0.18f, 1.7f);
					tr.localEulerAngles = new Vector3(270f, 195f, 0f);
					tr.localScale = new Vector3(0.3861f, 0.3861f, 0.3861f);
					tr.GetComponentInChildren<Light>(true).intensity = 15f;
					saved_go["pumpkin"] = tr.gameObject;
				}
				if (rsnv(saved_go, "pumpkin", effect) == true) saved_go["pumpkin"].SetActive(false);
				cd = 0.2f;
			}
			else if (effect == "r.spawnPlushie")
			{
				if (saved_go["plushie"] == null)
				{
					Transform tr = new GameObject("PlushieMesh").transform;
					tr.gameObject.SetActive(false);
					Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Plushie pajama man").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "PJManPlush"), tr);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(1.04f, 0.17f, 1.64f);
					tr.localEulerAngles = new Vector3(270f, 84f, 0f);
					tr.localScale = new Vector3(1f, 1f, 1f);
					saved_go["plushie"] = tr.gameObject;
				}
				if (saved_ac["plushieAudio"] == null) saved_ac["plushieAudio"] = StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Plushie pajama man").prefabObject.GetComponentInChildren<AnimatedObjectTrigger>(true).boolFalseAudios[0];
				if (rsnv(saved_go, "plushie", effect) == true && rsnv(saved_ac, "plushieAudio", effect) == true)
				{
					saved_go["plushie"].SetActive(true);
					saved_go["plushie"].GetComponentInChildren<Animator>(true).SetBool("squeeze", true);
					audio.PlayOneShot(saved_ac["plushieAudio"]);
				}
				cd = 0.2f;
			}
			else if (effect == "r.despawnPlushie")
			{
				if (saved_go["plushie"] == null)
				{
					Transform tr = new GameObject("PlushieMesh").transform;
					tr.gameObject.SetActive(false);
					Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Plushie pajama man").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "PJManPlush"), tr);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(1.04f, 0.17f, 1.64f);
					tr.localEulerAngles = new Vector3(270f, 84f, 0f);
					tr.localScale = new Vector3(1f, 1f, 1f);
					saved_go["plushie"] = tr.gameObject;
				}
				if (rsnv(saved_go, "plushie", effect) == true) saved_go["plushie"].SetActive(false);
				cd = 0.2f;
			}
			else if (effect == "r.spawnGoldfish")
			{
				if (saved_go["goldfish"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Goldfish").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "FishBowl"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(1.04f, -0.23f, 1.7f);
					tr.localEulerAngles = new Vector3(270f, 0f, 0f);
					tr.localScale = new Vector3(0.2872f, 0.2872f, 0.2872f);
					saved_go["goldfish"] = tr.gameObject;
				}
				if (saved_ac["goldfishAudio"] == null) saved_ac["goldfishAudio"] = StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Goldfish").prefabObject.GetComponentInChildren<PlaceableShipObject>(true).placeObjectSFX;
				if (rsnv(saved_go, "goldfish", effect) == true && rsnv(saved_ac, "goldfishAudio", effect) == true)
				{
					saved_go["goldfish"].SetActive(true);
					audio.volume = 1f;
					audio.PlayOneShot(saved_ac["goldfishAudio"]);
				}
			}
			else if (effect == "r.despawnGoldfish")
			{
				if (saved_go["goldfish"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Goldfish").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "FishBowl"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(1.04f, -0.23f, 1.7f);
					tr.localEulerAngles = new Vector3(270f, 0f, 0f);
					tr.localScale = new Vector3(0.2872f, 0.2872f, 0.2872f);
					saved_go["goldfish"] = tr.gameObject;
				}
				if (rsnv(saved_go, "goldfish", effect) == true) saved_go["goldfish"].SetActive(false);
				cd = 0.2f;
			}
			else if (effect == "r.spawnDiscoBall")
			{
				if (saved_go["disco"] == null && cruiser_additions.discoball == 3)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Disco Ball").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "AnimContainer"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(0f, 0.89f, 2.5f);
					tr.localEulerAngles = new Vector3(270f, 0f, 0f);
					tr.localScale = new Vector3(1f, 1f, 1f);
					foreach (Light light in tr.GetComponentsInChildren<Light>(true)) light.intensity = 45f;
					saved_go["disco"] = tr.gameObject;
				}
				if (saved_ac["discoAudio"] == null) saved_ac["discoAudio"] = StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Disco Ball").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "Audio").GetComponent<AudioSource>().clip;
				if (rsnv(saved_go, "disco", effect) == true && rsnv(saved_ac, "discoAudio", effect) == true)
				{
					saved_go["disco"].SetActive(true);
					saved_go["disco"].GetComponent<Animator>().SetBool("on", true);
					third.clip = saved_ac["discoAudio"];
					third.volume = 0.4f;
					third.Play();
				}
			}
			else if (effect == "r.despawnDiscoBall")
			{
				third.Stop();
				if (saved_go["disco"] == null && cruiser_additions.discoball == 3)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Disco Ball").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "AnimContainer"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(0f, 0.89f, 2.5f);
					tr.localEulerAngles = new Vector3(270f, 0f, 0f);
					tr.localScale = new Vector3(1f, 1f, 1f);
					foreach (Light light in tr.GetComponentsInChildren<Light>(true)) light.intensity = 45f;
					saved_go["disco"] = tr.gameObject;
				}
				if (rsnv(saved_go, "disco", effect) == true)
				{
					saved_go["disco"].SetActive(false);
					saved_go["disco"].GetComponent<Animator>().SetBool("on", false);
				}
				cd = 0.2f;
			}
			else if (effect.Contains("r.spawnBoombox/") == true && effect.EndsWith("/") == false)
			{
				if (saved_go["boombox"] == null)
				{
					GameObject box = new GameObject("Boombox");
					box.SetActive(false);
					box.transform.SetParent(toycar);
					box.AddComponent<MeshFilter>();
					box.AddComponent<MeshRenderer>();
					GameObject temp = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Boombox").spawnPrefab;
					box.GetComponent<MeshFilter>().mesh = temp.GetComponent<MeshFilter>().mesh;
					box.GetComponent<MeshRenderer>().materials = temp.GetComponent<MeshRenderer>().materials;
					box.transform.localPosition = new Vector3(1.04f, -0.27f, 1.7f);
					box.transform.localEulerAngles = new Vector3(355f, 330f, 90f);
					box.transform.localScale = new Vector3(0.0221f, 0.0223f, 0.0142f);
					saved_go["boombox"] = box;
				}
				if (saved_ac["boomboxAudio"] == null)
				{
					BoomboxItem temp = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Boombox").spawnPrefab.GetComponent<BoomboxItem>();
					saved_ac["boomboxAudio1"] = temp.musicAudios[0];
					saved_ac["boomboxAudio2"] = temp.musicAudios[1];
					saved_ac["boomboxAudio3"] = temp.musicAudios[2];
					saved_ac["boomboxAudio4"] = temp.musicAudios[3];
					saved_ac["boomboxAudio5"] = temp.musicAudios[4];
					saved_ac["boomboxAudio"] = temp.stopAudios[0];
				}
				if (rsnv(saved_go, "boombox", effect) == true && rsnv(saved_ac, "boomboxAudio", effect) == true)
				{
					saved_go["boombox"].SetActive(true);
					Shion cr = new Shion(UInt32.Parse(effect.split("/")[1]));
					bool boombox = (cr.next32mm(0, 100) < 60);
					third.clip = (boombox == true ? saved_ac["boomboxAudio" + cr.next32mm(1, 6)] : vehicle.radioClips[cr.next32mm(0, 4)]);
					if (boombox == true) third.volume = 0.7f;
					third.Play();
				}
			}
			else if (effect == "r.despawnBoombox")
			{
				third.Stop();
				if (saved_go["boombox"] == null)
				{
					GameObject box = new GameObject("Boombox");
					box.SetActive(false);
					box.transform.SetParent(toycar);
					box.AddComponent<MeshFilter>();
					box.AddComponent<MeshRenderer>();
					GameObject temp = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Boombox").spawnPrefab;
					box.GetComponent<MeshFilter>().mesh = temp.GetComponent<MeshFilter>().mesh;
					box.GetComponent<MeshRenderer>().materials = temp.GetComponent<MeshRenderer>().materials;
					box.transform.localPosition = new Vector3(1.04f, -0.27f, 1.7f);
					box.transform.localEulerAngles = new Vector3(355f, 330f, 90f);
					box.transform.localScale = new Vector3(0.0221f, 0.0223f, 0.0142f);
					saved_go["boombox"] = box;
				}
				if (saved_ac["boomboxAudio"] == null)
				{
					BoomboxItem temp = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Boombox").spawnPrefab.GetComponent<BoomboxItem>();
					saved_ac["boomboxAudio1"] = temp.musicAudios[0];
					saved_ac["boomboxAudio2"] = temp.musicAudios[1];
					saved_ac["boomboxAudio3"] = temp.musicAudios[2];
					saved_ac["boomboxAudio4"] = temp.musicAudios[3];
					saved_ac["boomboxAudio5"] = temp.musicAudios[4];
					saved_ac["boomboxAudio"] = temp.stopAudios[0];
				}
				if (rsnv(saved_go, "boombox", effect) == true && rsnv(saved_ac, "boomboxAudio", effect) == true)
				{
					audio.PlayOneShot(saved_ac["boomboxAudio"]);
					saved_go["boombox"].SetActive(false);
				}
			}
			else
			{
				ca.mls.LogError("play_effect error, effect doesn't exist (are the mod versions not the same?). " + effect + " " + previous_event);
			}

//			// kuro //
			if (effect.StartsWith("r.") == true) {}
			else if (effect == "k.minCollision1" || effect == "k.minCollision1_cd")
			{
				audio.PlayOneShot(vehicle.minCollisions[0]);
				if (effect.Contains("_cd") == true) cd = 0.2f;
			}
			else if (effect == "k.minCollision2" || effect == "k.minCollision2_cd")
			{
				audio.PlayOneShot(vehicle.minCollisions[1]);
				if (effect.Contains("_cd") == true) cd = 0.2f;
			}
			else if (effect == "k.medCollision1" || effect == "k.medCollision1_cd")
			{
				audio.volume = 0.4f;
				audio.PlayOneShot(vehicle.medCollisions[0]);
				if (effect.Contains("_cd") == true) cd = 0.2f;
			}
			else if (effect == "k.medCollision2" || effect == "k.medCollision2_cd")
			{
				audio.volume = 0.4f;
				audio.PlayOneShot(vehicle.medCollisions[1]);
				if (effect.Contains("_cd") == true) cd = 0.2f;
			}
			else if (effect == "k.medCollision3" || effect == "k.medCollision3_cd")
			{
				audio.volume = 0.4f;
				audio.PlayOneShot(vehicle.medCollisions[2]);
				if (effect.Contains("_cd") == true) cd = 0.2f;
			}
			else if (effect == "k.maxCollision1" || effect == "k.maxCollision1_cd")
			{
				audio.volume = 0.3f;
				audio.PlayOneShot(vehicle.maxCollisions[0]);
				if (effect.Contains("_cd") == true) cd = 0.2f;
			}
			else if (effect == "k.maxCollision2" || effect == "k.maxCollision2_cd")
			{
				audio.volume = 0.3f;
				audio.PlayOneShot(vehicle.maxCollisions[1]);
				if (effect.Contains("_cd") == true) cd = 0.2f;
			}
			else if (effect == "k.obsCollision1")
			{
				audio.volume = 0.4f;
				audio.PlayOneShot(vehicle.obstacleCollisions[0]);
			}
			else if (effect == "k.obsCollision2")
			{
				audio.volume = 0.3f;
				audio.PlayOneShot(vehicle.obstacleCollisions[1]);
			}
			else if (effect == "k.obsCollision3")
			{
				audio.volume = 0.3f;
				audio.PlayOneShot(vehicle.obstacleCollisions[2]);
			}
			else if (effect == "k.explosion")
			{
				if (saved_go["explosion"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.explosionPrefab.GetComponentsInChildren<Transform>(true).First(_ => _.name == "MainBlast"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-4f, 0f, 2.5f);
					tr.localEulerAngles = new Vector3(0f, 0f, 0f);
					tr.localScale = new Vector3(0.6f, 0.6f, 0.6f);
					saved_go["explosion"] = tr.gameObject;
				}
				if (saved_ac["explosionAudio"] == null) saved_ac["explosionAudio"] = StartOfRound.Instance.explosionPrefab.GetComponentInChildren<AudioSource>(true).clip;
				if (saved_ps["fire2"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(vehicle.GetComponentsInChildren<Transform>(true).First(_ => _.name == "CarHoodFire"));
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-4f, 0f, 2.5f);
					tr.localEulerAngles = new Vector3(0f, 270f, 270f);
					tr.localScale = new Vector3(0.1556f, 0.1556f, 0.1556f);
					tr.GetComponentInChildren<Light>(true).intensity = 45f;
					saved_ps["fire2"] = tr.GetComponent<ParticleSystem>();
				}
				if (rsnv(saved_go, "explosion", effect) == true && rsnv(saved_ac, "explosionAudio", effect) == true && rsnv(saved_ps, "fire2", effect) == true)
				{
					mixed_active = false;
					mixed.volume = 0f;
					mixed.Stop();
					saved_go["explosion"].SetActive(true);
					audio.PlayOneShot(saved_ac["explosionAudio"]);
					saved_ps["fire2"].Play();
					mixed.clip = vehicle.hoodFireAudio.clip;
					mixed.volume = 0.5f;
					mixed.Play();
				}
			}
			else if (effect == "k.stopFire")
			{
				if (saved_ps["fire2"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(vehicle.GetComponentsInChildren<Transform>(true).First(_ => _.name == "CarHoodFire"));
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-4f, 0f, 2.5f);
					tr.localEulerAngles = new Vector3(0f, 270f, 270f);
					tr.localScale = new Vector3(0.1556f, 0.1556f, 0.1556f);
					tr.GetComponentInChildren<Light>(true).intensity = 45f;
					saved_ps["fire2"] = tr.GetComponent<ParticleSystem>();
				}
				if (rsnv(saved_ps, "fire2", effect) == true)
				{
					mixed.Stop();
					saved_ps["fire2"].Stop();
				}
			}
			else if (effect == "k.addHealth")
			{
				if (saved_ac["sprayAudio"] == null) saved_ac["sprayAudio"] = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "WeedKillerBottle").spawnPrefab.GetComponent<SprayPaintItem>().spraySFX;
				if (rsnv(saved_ac, "sprayAudio", effect) == true)
				{
					audio.PlayOneShot(saved_ac["sprayAudio"]);
					audio.PlayOneShot(vehicle.pourOil);
				}
			}
			else if (effect.Contains("k.boost/") == true && effect.EndsWith("/") == false)
			{
				string[] collisions = new string[] {"k.medCollision1_cd", "k.medCollision2_cd", "k.medCollision3_cd", "k.maxCollision1_cd", "k.maxCollision2_cd"};
				Shion cr = new Shion(UInt32.Parse(effect.split("/")[1]));
				third.PlayOneShot(vehicle.turboBoostSFX);
				third.PlayOneShot(vehicle.turboBoostSFX2);
				if (cr.next32mm(0, 100) < 30) play_effect(collisions[cr.next32mm(0, collisions.Length)]);
				cooldown = 0.2f;
				return;
			}
			else if (effect == "k.extremeStress")
			{
				mixed.volume = 0f;
				mixed.Stop();
				mixed.clip = vehicle.extremeStressAudio.clip;
				mixed_lowest = 0.2f;
				mixed_highest = 0.5f;
				mixed_lerpSpeed = 1f;
				mixed.Play();
				mixed_active = true;
				mixed_disable_delay = 5f;
				cd = 0.2f;
			}
			else if (effect.Contains("k.collect") == true)
			{
				if (saved_mt["kuro"] == null) saved_mt["kuro"] = toycar.GetComponent<MeshRenderer>().material;
				if (rsnv(saved_mt, "kuro", effect) == true)
				{
					audio.volume = 0.8f;
					audio.PlayOneShot(HUDManager.Instance.displayCollectedScrapSFX);
					toycar.GetComponent<MeshRenderer>().materials = new Material[] {HUDManager.Instance.hologramMaterial};
					kuro_z = (effect.Contains("240") == true ? 240f : 72f) * (effect.EndsWith("N") == true ? -1 : 1);
					kuro1080 = 15f;
				}
			}
			else if (effect == "k.uncollect")
			{
				if (saved_mt["kuro"] == null) saved_mt["kuro"] = toycar.GetComponent<MeshRenderer>().material;
				if (rsnv(saved_mt, "kuro", effect) == true)
				{
					toycar.GetComponent<MeshRenderer>().materials = new Material[] {saved_mt["kuro"]};
					kuro1080 = 0f;
					toycar.localEulerAngles = new Vector3(0f, 90f, 180f);
					kuro_z = 72f;
				}
				cd = 0.2f;
			}
			else if (effect == "k.spawnPumpkin")
			{
				if (saved_go["pumpkin"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "JackOLantern").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "PumpkinMesh"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-1.6f, -1.04f, 1.95f);
					tr.localEulerAngles = new Vector3(0f, 0f, 105f);
					tr.localScale = new Vector3(0.3861f, 0.3861f, 0.3861f);
					tr.GetComponentInChildren<Light>(true).intensity = 15f;
					saved_go["pumpkin"] = tr.gameObject;
				}
				if (saved_ac["pumpkinAudio"] == null) saved_ac["pumpkinAudio"] = StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "JackOLantern").prefabObject.GetComponentInChildren<AnimatedObjectTrigger>(true).boolFalseAudios[0];
				if (rsnv(saved_go, "pumpkin", effect) == true && rsnv(saved_ac, "pumpkinAudio", effect) == true)
				{
					saved_go["pumpkin"].SetActive(true);
					audio.PlayOneShot(saved_ac["pumpkinAudio"]);
				}
				cd = 0.2f;
			}
			else if (effect == "k.despawnPumpkin")
			{
				if (saved_go["pumpkin"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "JackOLantern").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "PumpkinMesh"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-1.6f, -1.04f, 1.95f);
					tr.localEulerAngles = new Vector3(0f, 0f, 105f);
					tr.localScale = new Vector3(0.3861f, 0.3861f, 0.3861f);
					tr.GetComponentInChildren<Light>(true).intensity = 15f;
					saved_go["pumpkin"] = tr.gameObject;
				}
				if (rsnv(saved_go, "pumpkin", effect) == true) saved_go["pumpkin"].SetActive(false);
				cd = 0.2f;
			}
			else if (effect == "k.spawnPlushie")
			{
				if (saved_go["plushie"] == null)
				{
					Transform tr = new GameObject("PlushieMesh").transform;
					tr.gameObject.SetActive(false);
					Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Plushie pajama man").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "PJManPlush"), tr);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-1.5f, -1.04f, 2.33f);
					tr.localEulerAngles = new Vector3(0f, 4f, 354f);
					tr.localScale = new Vector3(1f, 1f, 1f);
					saved_go["plushie"] = tr.gameObject;
				}
				if (saved_ac["plushieAudio"] == null) saved_ac["plushieAudio"] = StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Plushie pajama man").prefabObject.GetComponentInChildren<AnimatedObjectTrigger>(true).boolFalseAudios[0];
				if (rsnv(saved_go, "plushie", effect) == true && rsnv(saved_ac, "plushieAudio", effect) == true)
				{
					saved_go["plushie"].SetActive(true);
					saved_go["plushie"].GetComponentInChildren<Animator>(true).SetBool("squeeze", true);
					audio.PlayOneShot(saved_ac["plushieAudio"]);
				}
				cd = 0.2f;
			}
			else if (effect == "k.despawnPlushie")
			{
				if (saved_go["plushie"] == null)
				{
					Transform tr = new GameObject("PlushieMesh").transform;
					tr.gameObject.SetActive(false);
					Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Plushie pajama man").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "PJManPlush"), tr);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-1.5f, -1.04f, 2.33f);
					tr.localEulerAngles = new Vector3(0f, 4f, 354f);
					tr.localScale = new Vector3(1f, 1f, 1f);
					saved_go["plushie"] = tr.gameObject;
				}
				if (rsnv(saved_go, "plushie", effect) == true) saved_go["plushie"].SetActive(false);
				cd = 0.2f;
			}
			else if (effect == "k.spawnGoldfish")
			{
				if (saved_go["goldfish"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Goldfish").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "FishBowl"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-1.6f, -1.04f, 1.92f);
					tr.localEulerAngles = new Vector3(0f, 0f, 0f);
					tr.localScale = new Vector3(0.2872f, 0.2872f, 0.2872f);
					saved_go["goldfish"] = tr.gameObject;
				}
				if (saved_ac["goldfishAudio"] == null) saved_ac["goldfishAudio"] = StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Goldfish").prefabObject.GetComponentInChildren<PlaceableShipObject>(true).placeObjectSFX;
				if (rsnv(saved_go, "goldfish", effect) == true && rsnv(saved_ac, "goldfishAudio", effect) == true)
				{
					saved_go["goldfish"].SetActive(true);
					audio.volume = 1f;
					audio.PlayOneShot(saved_ac["goldfishAudio"]);
				}
			}
			else if (effect == "k.despawnGoldfish")
			{
				if (saved_go["goldfish"] == null)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Goldfish").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "FishBowl"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-1.6f, -1.04f, 1.92f);
					tr.localEulerAngles = new Vector3(0f, 0f, 0f);
					tr.localScale = new Vector3(0.2872f, 0.2872f, 0.2872f);
					saved_go["goldfish"] = tr.gameObject;
				}
				if (rsnv(saved_go, "goldfish", effect) == true) saved_go["goldfish"].SetActive(false);
				cd = 0.2f;
			}
			else if (effect == "k.spawnDiscoBall")
			{
				if (saved_go["disco"] == null && cruiser_additions.discoball == 3)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Disco Ball").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "AnimContainer"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-2.25f, 0f, 2.76f);
					tr.localEulerAngles = new Vector3(0f, 0f, 0f);
					tr.localScale = new Vector3(1f, 1f, 1f);
					foreach (Light light in tr.GetComponentsInChildren<Light>(true)) light.intensity = 45f;
					saved_go["disco"] = tr.gameObject;
				}
				if (saved_ac["discoAudio"] == null) saved_ac["discoAudio"] = StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Disco Ball").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "Audio").GetComponent<AudioSource>().clip;
				if (rsnv(saved_go, "disco", effect) == true && rsnv(saved_ac, "discoAudio", effect) == true)
				{
					saved_go["disco"].SetActive(true);
					saved_go["disco"].GetComponent<Animator>().SetBool("on", true);
					third.clip = saved_ac["discoAudio"];
					third.volume = 0.4f;
					third.Play();
				}
			}
			else if (effect == "k.despawnDiscoBall")
			{
				third.Stop();
				if (saved_go["disco"] == null && cruiser_additions.discoball == 3)
				{
					Transform tr = Object.Instantiate<Transform>(StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Disco Ball").prefabObject.GetComponentsInChildren<Transform>(true).First(_ => _.name == "AnimContainer"));
					tr.gameObject.SetActive(false);
					tr.SetParent(toycar);
					tr.localPosition = new Vector3(-2.25f, 0f, 2.76f);
					tr.localEulerAngles = new Vector3(0f, 0f, 0f);
					tr.localScale = new Vector3(1f, 1f, 1f);
					foreach (Light light in tr.GetComponentsInChildren<Light>(true)) light.intensity = 45f;
					saved_go["disco"] = tr.gameObject;
				}
				if (rsnv(saved_go, "disco", effect) == true)
				{
					saved_go["disco"].SetActive(false);
					saved_go["disco"].GetComponent<Animator>().SetBool("on", false);
				}
				cd = 0.2f;
			}
			else if (effect.Contains("k.spawnBoombox/") == true && effect.EndsWith("/") == false)
			{
				if (saved_go["boombox"] == null)
				{
					GameObject box = new GameObject("Boombox");
					box.SetActive(false);
					box.transform.SetParent(toycar);
					box.AddComponent<MeshFilter>();
					box.AddComponent<MeshRenderer>();
					GameObject temp = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Boombox").spawnPrefab;
					box.GetComponent<MeshFilter>().mesh = temp.GetComponent<MeshFilter>().mesh;
					box.GetComponent<MeshRenderer>().materials = temp.GetComponent<MeshRenderer>().materials;
					box.transform.localPosition = new Vector3(-1.6f, -1.04f, 1.88f);
					box.transform.localEulerAngles = new Vector3(340f, 275f, 0f);
					box.transform.localScale = new Vector3(0.0221f, 0.0223f, 0.0142f);
					saved_go["boombox"] = box;
				}
				if (saved_ac["boomboxAudio"] == null)
				{
					BoomboxItem temp = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Boombox").spawnPrefab.GetComponent<BoomboxItem>();
					saved_ac["boomboxAudio1"] = temp.musicAudios[0];
					saved_ac["boomboxAudio2"] = temp.musicAudios[1];
					saved_ac["boomboxAudio3"] = temp.musicAudios[2];
					saved_ac["boomboxAudio4"] = temp.musicAudios[3];
					saved_ac["boomboxAudio5"] = temp.musicAudios[4];
					saved_ac["boomboxAudio"] = temp.stopAudios[0];
				}
				if (rsnv(saved_go, "boombox", effect) == true && rsnv(saved_ac, "boomboxAudio", effect) == true)
				{
					saved_go["boombox"].SetActive(true);
					Shion cr = new Shion(UInt32.Parse(effect.split("/")[1]));
					bool boombox = (cr.next32mm(0, 100) < 60);
					third.clip = (boombox == true ? saved_ac["boomboxAudio" + cr.next32mm(1, 6)] : vehicle.radioClips[cr.next32mm(0, 4)]);
					if (boombox == true) third.volume = 0.7f;
					third.Play();
				}
			}
			else if (effect == "k.despawnBoombox")
			{
				third.Stop();
				if (saved_go["boombox"] == null)
				{
					GameObject box = new GameObject("Boombox");
					box.SetActive(false);
					box.transform.SetParent(toycar);
					box.AddComponent<MeshFilter>();
					box.AddComponent<MeshRenderer>();
					GameObject temp = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Boombox").spawnPrefab;
					box.GetComponent<MeshFilter>().mesh = temp.GetComponent<MeshFilter>().mesh;
					box.GetComponent<MeshRenderer>().materials = temp.GetComponent<MeshRenderer>().materials;
					box.transform.localPosition = new Vector3(-1.6f, -1.04f, 1.88f);
					box.transform.localEulerAngles = new Vector3(340f, 275f, 0f);
					box.transform.localScale = new Vector3(0.0221f, 0.0223f, 0.0142f);
					saved_go["boombox"] = box;
				}
				if (saved_ac["boomboxAudio"] == null)
				{
					BoomboxItem temp = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Boombox").spawnPrefab.GetComponent<BoomboxItem>();
					saved_ac["boomboxAudio1"] = temp.musicAudios[0];
					saved_ac["boomboxAudio2"] = temp.musicAudios[1];
					saved_ac["boomboxAudio3"] = temp.musicAudios[2];
					saved_ac["boomboxAudio4"] = temp.musicAudios[3];
					saved_ac["boomboxAudio5"] = temp.musicAudios[4];
					saved_ac["boomboxAudio"] = temp.stopAudios[0];
				}
				if (rsnv(saved_go, "boombox", effect) == true && rsnv(saved_ac, "boomboxAudio", effect) == true)
				{
					audio.PlayOneShot(saved_ac["boomboxAudio"]);
					saved_go["boombox"].SetActive(false);
				}
			}
			else
			{
				ca.mls.LogError("play_effect error, effect doesn't exist (are the mod versions not the same?). " + effect + " " + previous_event);
			}
			cooldown = cd;
		}
		public bool rsnv(table<string, Animator> saved, string key = "_key", string effect = "_effect") { return return_saved_null_value(saved[key], key, effect, "animator"); }
		public bool rsnv(table<string, AudioClip> saved, string key = "_key", string effect = "_effect") { return return_saved_null_value(saved[key], key, effect, "audioclip"); }
		public bool rsnv(table<string, ParticleSystem> saved, string key = "_key", string effect = "_effect") { return return_saved_null_value(saved[key], key, effect, "particle"); }
		public bool rsnv(table<string, MeshRenderer> saved, string key = "_key", string effect = "_effect") { return return_saved_null_value(saved[key], key, effect, "meshrenderer"); }
		public bool rsnv(table<string, GameObject> saved, string key = "_key", string effect = "_effect") { return return_saved_null_value(saved[key], key, effect, "gameobject"); }
		public bool rsnv(table<string, Material> saved, string key = "_key", string effect = "_effect") { return return_saved_null_value(saved[key], key, effect, "material"); }
		public bool return_saved_null_value(Object value, string key, string effect, string component)
		{
			if (value != null) return true;
			ca.mls.LogError("play_effect error, effect " + component + " was null. " + key + " " + effect + " " + previous_event);
			return false;
		}
	}

//	// custom component //
	public class temporary : MonoBehaviour
	{
		public string guid = ca.harmony.Id;
		public float cd = 2f;
		public int attempts = 10;

		public void Awake()
		{
			cd = cd + (float)(new Shion().next01() * 2);
		}
		public void Update()
		{
			if (cd > 0f)
			{
				cd = cd - Time.deltaTime;
			}
			else
			{
				cd = 2f;
				GrabbableObject go = this.GetComponent<GrabbableObject>();
				if (go != null && go.isPocketed == false)
				{
					foreach (Renderer rend in this.GetComponentsInChildren<Renderer>(true))
					{
						if (rend != null)
						{
							rend.enabled = true;
							rend.forceRenderingOff = false;
						}
					}
					if (attempts > 0 && go.itemProperties != null)
					{
						attempts = attempts - 1;
						if (attempts == 0 && (!(go.itemProperties.restingRotation == new Vector3(0f, 0f, 90f)) || Mathf.Approximately(go.itemProperties.verticalOffset, 0.4f) == false))
						{
							ca.mls.LogError("engine restingRotation and or verticalOffset are still being set by another mod after 10 seconds. restingRotation " + go.itemProperties.restingRotation + ", verticalOffset " + go.itemProperties.verticalOffset);
						}
						go.itemProperties.restingRotation = new Vector3(0f, 0f, 90f);
						go.itemProperties.verticalOffset = 0.4f;
						if (go.isHeld == false && go.isHeldByEnemy == false)
						{
							if (go.floorYRot == -1)
							{
								go.transform.rotation = Quaternion.Euler(go.itemProperties.restingRotation.x, go.transform.eulerAngles.y, go.itemProperties.restingRotation.z);
							}
							else
							{
								go.transform.rotation = Quaternion.Euler(go.itemProperties.restingRotation.x, (float)(go.floorYRot + go.itemProperties.floorYOffset) + 90f, go.itemProperties.restingRotation.z);
							}
							if (attempts == 9 && StartOfRound.Instance != null && StartOfRound.Instance.inShipPhase == true && Vector3.Distance(go.transform.position, StartOfRound.Instance.elevatorTransform.position) < 100f)
							{
								RaycastHit hitInfo;
								if (Physics.Raycast(go.transform.position, Vector3.down, out hitInfo, 10f, 268437760, QueryTriggerInteraction.Ignore) || Physics.Raycast(go.transform.position, Vector3.up, out hitInfo, 10f, 268437760, QueryTriggerInteraction.Ignore))
								{
									go.targetFloorPosition = hitInfo.point + go.itemProperties.verticalOffset * Vector3.up;
									if (go.transform.parent != null)
									{
										go.targetFloorPosition = go.transform.parent.InverseTransformPoint(go.targetFloorPosition);
									}
								}
							}
						}
					}
				}
			}
		}
	}
	public class ItemTypeSeed : MonoBehaviour
	{
		public string guid = ca.harmony.Id;
		public string seed;
	}

//	// custom flavor //
	public static class MintChocolateChipIceCream
	{
		public static System.String[] split(this string _string, string _split)
		{
			return _string.Split(new string[] {_split}, System.StringSplitOptions.None);
		}
		public static int indexof(this System.Array _array, System.Object _indexof)
		{
			return System.Array.IndexOf(_array, _indexof);
		}
	}
	public class table<TKey, TValue>
	{
		public Dictionary<TKey, TValue> t = new Dictionary<TKey, TValue>();
		public TValue this[TKey k]
		{
			get { return t.ContainsKey(k) ? t[k] : default(TValue); }
			set { t[k] = value; }
		}
	}

//	// custom random (better than seeded System.Random) //
	public class Shion
	{
		private UInt64[] state;

		public Shion()
		{
			System.Security.Cryptography.RandomNumberGenerator rand = System.Security.Cryptography.RandomNumberGenerator.Create();
			byte[] randBytes = new byte[8];
			rand.GetBytes(randBytes, 0, 8);
			UInt64 seed = System.BitConverter.ToUInt64(randBytes, 0);
			xorshift256_init(seed);
		}
		public Shion(UInt64 seed)
		{
			xorshift256_init(seed);
		}

		//next
		public int next32mm(int min, int max)
		{
			uint value = next32();
			if (value == UInt32.MaxValue) value = value - 1;
			double scale = ((double)(max - min)) / UInt32.MaxValue;
			return (int)(min + (value * scale)); //[min, max)
		}
		public uint next32mm(uint min, uint max, bool unsigned)
		{
			uint value = next32();
			if (value == UInt32.MaxValue) value = value - 1;
			double scale = ((double)(max - min)) / UInt32.MaxValue;
			return (uint)(min + (value * scale)); //[min, max)
		}
		public byte[] next8()
		{
			UInt64 nextInt64 = xoshiro256ss();
			return System.BitConverter.GetBytes(nextInt64);
		}
		public UInt32 next32()
		{
			byte[] randBytes = next8();
			return System.BitConverter.ToUInt32(randBytes, 0);
		}
		public UInt64 next64()
		{
			return xoshiro256ss();
		}
		public double next01()
		{
			UInt64 nextInt64 = xoshiro256ss();
			if (nextInt64 == UInt64.MaxValue) nextInt64 = nextInt64 - 1;
			return (double)nextInt64 / (double)(UInt64.MaxValue); //[0, 1)
		}

		//misc
		private UInt64 splitmix64(UInt64 partialstate)
		{
			partialstate = partialstate + 0x9E3779B97f4A7C15;
			partialstate = (partialstate ^ (partialstate >> 30)) * 0xBF58476D1CE4E5B9;
			partialstate = (partialstate ^ (partialstate >> 27)) * 0x94D049BB133111EB;
			return partialstate ^ (partialstate >> 31);
		}
		private void xorshift256_init(UInt64 seed)
		{
			UInt64[] result = new UInt64[4];
			result[0] = splitmix64(seed);
			result[1] = splitmix64(result[0]);
			result[2] = splitmix64(result[1]);
			result[3] = splitmix64(result[2]);
			state = result;
		}
		private UInt64 rotl64(UInt64 x, int k)
		{
			return (x << k) | (x >> (64 - k));
		}
		private UInt64 xoshiro256ss()
		{
			UInt64 result = rotl64(state[1] * 5, 7) * 9;
			UInt64 t = state[1] << 17;
			state[2] ^= state[0];
			state[3] ^= state[1];
			state[1] ^= state[2];
			state[0] ^= state[3];
			state[2] ^= t;
			state[3] = rotl64(state[3], 45);
			return result;
		}
	}
}
