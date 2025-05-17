using UInt32 = System.UInt32;
using UInt64 = System.UInt64;
using System.IO;
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
using UnityEngine.Rendering;
using Unity.Netcode;

namespace Kurosaki
{
	[BepInPlugin("4902.Koyuki", "Koyuki", "1.0.0")]
	public class koyu : BaseUnityPlugin
	{
		public static readonly Harmony harmony = new Harmony("4902.Koyuki");

		public static ManualLogSource mls;

		public static ConfigEntry<bool> temp_saveseeds;
		public static ConfigEntry<int>  temp_millisecond;
		public static ConfigEntry<bool> temp_synced_percentages;
		public static ConfigEntry<int>  temp_percentage;

		public static cfg_bool cfg_saveseeds = new cfg_bool();
		public static cfg_int  cfg_millisecond = new cfg_int();
		public static cfg_bool cfg_synced_percentages = new cfg_bool();
		public static cfg_int  percentage = new cfg_int();

		private void Awake()
		{
			temp_saveseeds = Config.Bind("#", "save/load", true, "[Save/load seeds]\nwhether the fish/koyuki item types should be saved to the save file. this saves the seed that determines the items type, so it will be the same when rejoining.\nloading a save file that has saved seeds without having this enabled or while in lan isn't recommended as the saved seeds can be reset if the number of items is changed.\nsaved item types are synced with other players if the synced percentages config is enabled"); cfg_saveseeds.Value = temp_saveseeds.Value;
			temp_millisecond = Config.Bind("#", "timer", 100, "[Client wait_timer]\nwhen joining a lobby as non-host koyuki items will wait to set their item type until the network message sent by the host has been received or the time spent waiting reached the maximum amount set by this config.\nthere will be a log message specifying if the network message was received first or the timer ended first. if the timer is often ending before the message from the host is being received then this should be increased. if the host doesn't have this mod then save/load can be disabled since there won't be a received message from the host so the items would always wait the full timer.\nmin is 20, max is 4000. 100 is before the player spawn animation ends, 500 is about 10 seconds, 1000 is about 15 seconds"); cfg_millisecond.Value = temp_millisecond.Value;
			temp_synced_percentages = Config.Bind("#", "sync", true, "[Synced percentages]\nautomatically sync config item percentages with the host. only disable if you're aware that disabling this can cause the item types to not be the same as other players if playing with others that have this mod"); cfg_synced_percentages.Value = temp_synced_percentages.Value;
			temp_percentage = Config.Bind("#", "koyuki_percent", 50, "percentage that koyuki will replace fish\ninput = percentage, 50 is 50% etc"); percentage.Value = temp_percentage.Value;
			mls = BepInEx.Logging.Logger.CreateLogSource("Koyuki");
			mls.LogInfo("yaata");
			harmony.PatchAll(typeof(koyuki));

			}}public class cfg_bool{public bool Value{get;set;
			}}public class cfg_int{public int Value{get;set;
		}
	}
	public class koyuki
	{
//		// scrap items //
		private static string[] item = new string[2];

		private static Mesh[] fish = new Mesh[7];

		private static AudioClip[] take = new AudioClip[3];

		private static AudioClip[] give = new AudioClip[3];

		private static Vector3[] position = new Vector3[2];

		private static Vector3[] rotation = new Vector3[2];

		private static bool[] z = new bool[2];

		private static Transform[] sequel = new Transform[6];

		private static UInt64 lobbyid = 0uL;

		private static bool[] first_item = new bool[] {true, true, true};

		[HarmonyPatch(typeof(GrabbableObject), "Start"), HarmonyPostfix]
		private static void pst1(GrabbableObject __instance)
		{
			pst1async(__instance);
		}
		private static async void pst1async(GrabbableObject __instance)
		{
			if (__instance.itemProperties.name == "FishTestProp")
			{
				await wait_frames(1);
				if (__instance.gameObject.GetComponent<Kurosaki.temporary>() != null) return;
				__instance.gameObject.AddComponent<Kurosaki.temporary>();
				__instance.itemProperties.restingRotation = new Vector3(0f, 0f, 90f);
				__instance.itemProperties.verticalOffset = 0.15f;
				if (GameNetworkManager.Instance.disableSteam == false && seeds == "nil")
				{
					if (GameNetworkManager.Instance.isHostingGame == false)
					{
						if (first_item[0] == true) { koyu.mls.LogInfo("await wait_timer"); first_item[0] = false; }
						await wait_timer(koyu.cfg_millisecond.Value >= 20 && koyu.cfg_millisecond.Value <= 4000 ? koyu.cfg_millisecond.Value : 100);
						if (disconnected[0] == true) return;
					}
					if (seeds == "nil") seeds = "?";
				}
				if (item[0] == null)
				{
					string text = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "4902-koyuki").Replace("\\", "/");
					koyu.mls.LogMessage("Searching this filepath:" + text);
					AssetBundle asset = AssetBundle.LoadFromFileAsync(text).assetBundle;
					//fish
					item[0] = "Fish";
					fish[0] = __instance.GetComponent<MeshFilter>().mesh;
					take[0] = __instance.itemProperties.grabSFX;
					give[0] = __instance.itemProperties.dropSFX;
					position[0] = __instance.itemProperties.positionOffset;
					rotation[0] = __instance.itemProperties.rotationOffset;
					z[0] = false;
					//koyuki
					item[1] = "Koyuki";
					Transform[] tr = Object.Instantiate<GameObject>(asset.LoadAsset<GameObject>("koyuki_merged_90x.obj")).GetComponentsInChildren<Transform>();
					for (int n = 1; n <= 6; n = n + 1)
					{
						tr[n].GetComponent<MeshFilter>().mesh = fish1.fish2(tr[n].GetComponent<MeshFilter>().mesh);
						Object.Instantiate<Transform>(tr[n]).SetParent(__instance.transform);
						tr[n].SetParent(__instance.itemProperties.spawnPrefab.transform);
						Transform[] temp = __instance.GetComponentsInChildren<Transform>();
						temp[n + 1].localPosition = tr[n].localPosition = new Vector3(-7.5f, 0f, 0f);
						temp[n + 1].localRotation = tr[n].localRotation = new Quaternion(0f, 0.7071f, 0f, 0.7071f);
						temp[n + 1].localScale = tr[n].localScale = new Vector3(10f, 10f, 32.6530612f);
						fish[n] = Object.Instantiate<Mesh>(temp[n + 1].GetComponent<MeshFilter>().mesh);
						sequel[n - 1] = tr[n];
					}
					take[1] = asset.LoadAsset<AudioClip>("koyuki_commonskill");
					take[2] = asset.LoadAsset<AudioClip>("koyuki_formation_in_1");
					give[1] = asset.LoadAsset<AudioClip>("koyuki_battle_damage_1");
					give[2] = asset.LoadAsset<AudioClip>("koyuki_formation_select");
					position[1] = new Vector3(-0.12f, 0.09f, -0.08f); //-0.13,0.15,-0.1
					rotation[1] = new Vector3(23f, 0f, 0f);
					z[1] = true;
				}
				else if (__instance.GetComponentsInChildren<Transform>().Length < 6)
				{
					for (int n = 0; n < 6; n = n + 1)
					{
						Object.Instantiate<Transform>(sequel[n]).SetParent(__instance.transform);
						Transform[] temp = __instance.GetComponentsInChildren<Transform>();
						temp[n + 2].localPosition = new Vector3(-7.5f, 0f, 0f);
						temp[n + 2].localRotation = new Quaternion(0f, 0.7071f, 0f, 0.7071f);
						temp[n + 2].localScale = new Vector3(10f, 10f, 32.6530612f);
					}
				}
				if (item[0] != null)
				{
					Shion cr;
					if (GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && __instance.GetComponent<NetworkObject>() != null)
					{
						UInt64 n64;
						if (koyu.cfg_saveseeds.Value == true && seeds != "nil" && seeds != "?" && seeds.Contains(__instance.GetComponent<NetworkObject>().NetworkObjectId + ".") == true)
						{
							if (first_item[2] == true) koyu.mls.LogInfo("custom_random = new Shion(saved seed)");
							int start = seeds.IndexOf(__instance.GetComponent<NetworkObject>().NetworkObjectId + ".");
							int n = __instance.GetComponent<NetworkObject>().NetworkObjectId.ToString().Length + 1;
							n64 = UInt64.Parse(seeds.Substring(start + n, seeds.IndexOf("/", start) - (start + n)));
						}
						else
						{
							if (first_item[2] == true) koyu.mls.LogInfo("custom_random = new Shion(lobbyid+networkobjectid)");
							n64 = lobbyid + __instance.GetComponent<NetworkObject>().NetworkObjectId;
						}
						cr = new Shion(n64);
						if (koyu.cfg_saveseeds.Value == true)
						{
							__instance.gameObject.AddComponent<ItemTypeSeed>();
							__instance.gameObject.GetComponent<ItemTypeSeed>().seed = n64.ToString();
						}
					}
					else
					{
						if (first_item[2] == true) koyu.mls.LogInfo("custom_random = new Shion()");
						cr = new Shion();
					}
					if (first_item[2] == true) koyu.mls.LogInfo(koyu.cfg_synced_percentages.Value == true && synced_percent != -1 ? "host config" : "local config");
					first_item[2] = false;
					int num = (koyu.cfg_synced_percentages.Value == true && synced_percent != -1 ? (cr.next32mm(0, 100) < synced_percent ? 1 : 0) : (cr.next32mm(0, 100) < koyu.percentage.Value ? 1 : 0));
					if (num == 1)
					{
						//koyuki
						for (int n = 1; n <= 6; n = n + 1)
						{
							__instance.GetComponentsInChildren<Transform>()[n + 1].GetComponent<MeshFilter>().mesh = fish[n];
						}
						BoxCollider box = __instance.GetComponent<BoxCollider>();
						box.center = new Vector3(8f, 0.3f, 0f);
						box.size = new Vector3(31f, 3.3f, 3.3f);
						__instance.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(8f, 0.3f, 0f);
						__instance.GetComponent<MeshFilter>().mesh = null;
					}
					else
					{
						//fish
						for (int n = 1; n <= 6; n = n + 1)
						{
							__instance.GetComponentsInChildren<Transform>()[n + 1].GetComponent<MeshFilter>().mesh = null;
						}
						__instance.GetComponent<MeshFilter>().mesh = fish[0];
					}
					__instance.GetComponentInChildren<ScanNodeProperties>().headerText = item[num];
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
			if (disconnected[0] == true) { if (disconnected[1] == false) { koyu.mls.LogMessage("disconnected before wait_timer ended"); disconnected[1] = true; } return; }
			if (first_item[1] == true) { koyu.mls.LogMessage(seeds == "nil" && client_received == false ? "timer ended before receiving network message (" + n + "/" + koyu.cfg_millisecond.Value + ")" : "received network message before timer ended (" + n + "/" + koyu.cfg_millisecond.Value + ")"); first_item[1] = false; }
		}
		[HarmonyPatch(typeof(GrabbableObject), "PlayDropSFX"), HarmonyPrefix]
		private static void pre1(ref GrabbableObject __instance)
		{
			if (item[0] != null && __instance != null && __instance.itemProperties.name == "FishTestProp" && __instance.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				__instance.itemProperties.dropSFX = (__instance.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? give[new Shion().next32mm(1, 3)] : give[0]);
			}
		}
		[HarmonyPatch(typeof(PlayerControllerB), "BeginGrabObject"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn1(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if (n > 0 && l[n - 1].ToString() == "call static float UnityEngine.Mathf::Clamp(float value, float min, float max)" && l[n].ToString() == "stfld float GameNetcodeStuff.PlayerControllerB::carryWeight")
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldfld, typeof(PlayerControllerB).GetField("currentlyGrabbingObject", BindingFlags.NonPublic | BindingFlags.Instance));
					yield return new CodeInstruction(OpCodes.Call, typeof(koyuki).GetMethod("grab_item"));
				}
				//koyu.mls.LogInfo(l[n].ToString());
			}
		}
		public static void grab_item(GrabbableObject currentlyGrabbingObject)
		{
			if (item[0] != null && currentlyGrabbingObject != null && currentlyGrabbingObject.itemProperties.name == "FishTestProp" && currentlyGrabbingObject.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				int n = (currentlyGrabbingObject.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? 1 : 0);
				currentlyGrabbingObject.itemProperties.grabSFX = (n == 1 ? take[new Shion().next32mm(1, 3)] : take[0]);
				currentlyGrabbingObject.itemProperties.canBeInspected = z[n];
				if (GameNetworkManager.Instance.isHostingGame == true)
				{
					currentlyGrabbingObject.itemProperties.positionOffset = position[n];
					currentlyGrabbingObject.itemProperties.rotationOffset = rotation[n];
				}
				currentlyGrabbingObject.itemProperties.itemName = currentlyGrabbingObject.GetComponentInChildren<ScanNodeProperties>().headerText;
			}
		}
		[HarmonyPatch(typeof(GrabbableObject), "GrabItemOnClient"), HarmonyPrefix]
		private static void pre2(GrabbableObject __instance)
		{
			if (item[0] != null && __instance != null && __instance.itemProperties.name == "FishTestProp" && __instance.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				int n = (__instance.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? 1 : 0);
				__instance.itemProperties.positionOffset = position[n];
				__instance.itemProperties.rotationOffset = rotation[n];
			}
		}
		[HarmonyPatch(typeof(PlayerControllerB), "SwitchToItemSlot"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn2(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if (n < (l.Count - 4) && l[n + 4].ToString() == "callvirt virtual void GrabbableObject::EquipItem()")
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, typeof(koyuki).GetMethod("hold_item"));
				}
				//koyu.mls.LogInfo(l[n].ToString());
			}
		}
		public static void hold_item(PlayerControllerB player)
		{
			GrabbableObject _item = player.ItemSlots[player.currentItemSlot];
			if (item[0] != null && _item != null && _item.itemProperties.name == "FishTestProp" && _item.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				int n = (_item.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? 1 : 0);
				_item.itemProperties.grabSFX = (n == 1 ? take[new Shion().next32mm(1, 3)] : take[0]);
				PlayerControllerB local_player = GameNetworkManager.Instance.localPlayerController;
				if (player == local_player || local_player.ItemSlots[local_player.currentItemSlot] == null || local_player.ItemSlots[local_player.currentItemSlot].itemProperties.name != "FishTestProp" || local_player.isPlayerDead == true)
				{
					_item.itemProperties.canBeInspected = z[n];
					_item.itemProperties.itemName = item[n];
					if (_item.isHeld == true || player != local_player)
					{
						_item.itemProperties.positionOffset = position[n];
						_item.itemProperties.rotationOffset = rotation[n];
					}
				}
			}
		}
		[HarmonyPatch(typeof(HUDManager), "DisplayNewScrapFound"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn3(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				if (n < (l.Count - 1) && l[n + 1].ToString() == "callvirt UnityEngine.Renderer[] UnityEngine.GameObject::GetComponentsInChildren()")
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Call, typeof(koyuki).GetMethod("display_items"));
				}
				yield return l[n];
				//koyu.mls.LogInfo(l[n].ToString());
			}
		}
		public static void display_items(HUDManager instance, GameObject displayingObject)
		{
			if (item[0] != null && instance.itemsToBeDisplayed[0] != null && instance.itemsToBeDisplayed[0].itemProperties.name == "FishTestProp" && displayingObject.name == "FishTestProp(Clone)" && instance.itemsToBeDisplayed[0].GetComponentInChildren<ScanNodeProperties>() != null)
			{
				int r = (instance.itemsToBeDisplayed[0].GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? 1 : 0);
				instance.itemsToBeDisplayed[0].itemProperties.itemName = item[r];
				for (int n = 1; n <= 6; n = n + 1)
				{
					displayingObject.GetComponentsInChildren<Transform>()[n + 1].GetComponent<MeshFilter>().mesh = (r == 1 ? fish[n] : null);
				}
				displayingObject.GetComponent<MeshFilter>().mesh = (r == 1 ? null : fish[0]);
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "Awake"), HarmonyPrefix]
		private static void pre3()
		{
			disconnected = new bool[] {false, false};
			reset_local_variables("StartOfRound.Awake");
		}
		[HarmonyPatch(typeof(StartOfRound), "Awake"), HarmonyPostfix]
		private static void pst2()
		{
			if (GameNetworkManager.Instance.disableSteam == false)
			{
				if (GameNetworkManager.Instance.currentLobby.HasValue == true)
				{
					lobbyid = (GameNetworkManager.Instance.currentLobby.Value.Id % 1000000000);
					koyu.mls.LogInfo(lobbyid);
				}
				else
				{
					koyu.mls.LogError("current lobby id is null");
				}
			}
		}

//		// network syncing //
		private static bool sync = false;

		private static string seeds = "nil";

		private static bool[] disconnected = new bool[] {false, false};

		private static int synced_percent = -1;

		private static bool client_received = false;

		[HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject"), HarmonyPostfix]
		private static void pst3()
		{
			if (sync == false)
			{
				if (NetworkManager.Singleton.IsHost == true)
				{
					NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("4902.Koyuki-Host", host_receive);
				}
				else
				{
					koyu.mls.LogInfo("requesting message from host");
					NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("4902.Koyuki-Client", client_receive);
					FastBufferWriter w = new FastBufferWriter(0, Unity.Collections.Allocator.Temp);
					NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Koyuki-Host", NetworkManager.ServerClientId, w);
					w.Dispose();
				}
				sync = true;
			}
		}
		private static void host_receive(ulong id, FastBufferReader r)
		{
			if (NetworkManager.Singleton.IsHost == true)
			{
				koyu.mls.LogInfo("received request from client");
				string message = koyu.percentage.Value + "^" + lobbyid + "^" + seeds;
				koyu.mls.LogInfo("sending message " + message);
				FastBufferWriter w = new FastBufferWriter(FastBufferWriter.GetWriteSize(message), Unity.Collections.Allocator.Temp);
				w.WriteValueSafe(message);
				NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Koyuki-Client", id, w, NetworkDelivery.ReliableFragmentedSequenced);
				w.Dispose();
			}
		}
		private static void client_receive(ulong id, FastBufferReader r)
		{
			if (NetworkManager.Singleton.IsHost == false)
			{
				string message;
				r.ReadValueSafe(out message, false);
				client_received = true;
				koyu.mls.LogInfo("client received message " + message);
				int n = 2;
				if ((message.Length - message.Replace("^", "").Length) == n)
				{
					string[] s = message.Split(new char[]{'^'}, n + 1);
					synced_percent = System.Int32.Parse(s[0]);
					lobbyid = UInt64.Parse(s[1]);
					seeds = s[2];
				}
				else
				{
					koyu.mls.LogError("received message was not what was expected. wasn't able to sync variables with host. (are the mod versions not the same?)");
					koyu.mls.LogError("found " + (message.Length - message.Replace("^", "").Length) + "/" + n + " ^ in message " + message);
				}
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "Disconnect"), HarmonyPrefix]
		private static void pre4()
		{
			disconnected[0] = true;
		}
		[HarmonyPatch(typeof(GameNetworkManager), "Disconnect"), HarmonyPostfix]
		private static void pst4()
		{
			reset_local_variables("GameNetworkManager.Disconnect");
		}
		[HarmonyPatch(typeof(StartOfRound), "OnDisable"), HarmonyPrefix]
		private static void pre5()
		{
			reset_local_variables("StartOfRound.OnDisable");
			if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
			{
				try { NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("4902.Koyuki-Host"); NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("4902.Koyuki-Client"); } catch (System.Exception error) { koyu.mls.LogError(error); }
			}
		}
		private static void reset_local_variables(string s)
		{
			sync = false;
			seeds = "nil";
			lobbyid = 0uL;
			first_item = new bool[] {true, true, true};
			saved_fish = "";
			loaded_fish = new List<string>();
			synced_percent = -1;
			client_received = false;
			koyu.mls.LogInfo("reset local variables (" + s + ")");
		}

//		// saving/loading //
		private static string saved_fish = "";

		private static List<string> loaded_fish = new List<string>();

		[HarmonyPatch(typeof(GameNetworkManager), "SaveItemsInShip"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn4(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if (l[n].ToString() == "callvirt virtual void System.Collections.Generic.List<UnityEngine.Vector3>::Add(UnityEngine.Vector3 item)")
				{
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldloc, 6);
					yield return new CodeInstruction(OpCodes.Call, typeof(koyuki).GetMethod("save_fish"));
				}
				//koyu.mls.LogInfo(l[n].ToString());
			}
		}
		public static void save_fish(GrabbableObject[] _items, int index)
		{
			if (koyu.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && _items[index].itemProperties.name == "FishTestProp")
			{
				if (_items[index].GetComponent<ItemTypeSeed>() != null)
				{
					ItemTypeSeed component = _items[index].GetComponent<ItemTypeSeed>();
					saved_fish = saved_fish + component.seed + "/";
				}
				else if (_items[index].GetComponent<NetworkObject>() != null)
				{
					saved_fish = saved_fish + (lobbyid + _items[index].GetComponent<NetworkObject>().NetworkObjectId).ToString() + "/";
					koyu.mls.LogInfo("ItemTypeSeed is null! saving seed for this item as lobbyid+networkobjectid");
				}
				else
				{
					saved_fish = saved_fish + new Shion().next32mm(1, 101).ToString() + "/";
					koyu.mls.LogInfo("ItemTypeSeed and NetworkObject are null! saving seed for this item as a random number from 1 to 100");
				}
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "SaveGame"), HarmonyPostfix]
		private static void pst5()
		{
			if (koyu.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && StartOfRound.Instance.inShipPhase == true && StartOfRound.Instance.isChallengeFile == false)
			{
				try
				{
					if (saved_fish == "") saved_fish = "nil";
					if (saved_fish.EndsWith("/") == true) saved_fish = saved_fish.Substring(0, saved_fish.Length - 1);
					koyu.mls.LogInfo("saving " + saved_fish);
					ES3.Save("4902.Koyuki-1", saved_fish, GameNetworkManager.Instance.currentSaveFileName);
					saved_fish = "";
				}
				catch (System.Exception error)
				{
					koyu.mls.LogError("Error saving item types: " + error);
				}
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "LoadShipGrabbableItems"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn5(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if (l[n].ToString() == "callvirt void Unity.Netcode.NetworkObject::Spawn(bool destroyWithScene)")
				{
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Call, typeof(koyuki).GetMethod("load_fish"));
				}
				//koyu.mls.LogInfo(l[n].ToString());
			}
		}
		public static void load_fish(GrabbableObject _item)
		{
			if (koyu.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && _item.itemProperties.name == "FishTestProp")
			{
				if (_item.GetComponent<NetworkObject>() != null)
				{
					loaded_fish.Add(_item.GetComponent<NetworkObject>().NetworkObjectId.ToString());
				}
				else
				{
					loaded_fish.Add("nil");
					koyu.mls.LogInfo("NetworkObject is null! the seed can't be loaded for this item");
				}
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "Start"), HarmonyPostfix]
		private static void pst6()
		{
			if (koyu.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false)
			{
				try
				{
					string temp = ES3.Load("4902.Koyuki-1", GameNetworkManager.Instance.currentSaveFileName, "nil");
					koyu.mls.LogInfo("loaded " + temp);
					string[] s = temp.Split(new char[]{'/'});
					if (s[0] != "nil" && s[0] != "" && s.Length == loaded_fish.Count)
					{
						seeds = "";
						for (int n = 0; n < loaded_fish.Count; n = n + 1)
						{
							seeds = seeds + loaded_fish[n] + "." + s[n] + "/";
						}
						koyu.mls.LogInfo("current networkobjectids + saved seeds " + seeds);
					}
					loaded_fish = new List<string>();
				}
				catch (System.Exception error)
				{
					koyu.mls.LogError("Error loading item types: " + error);
				}
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "ResetSavedGameValues"), HarmonyPrefix]
		private static void pre6(GameNetworkManager __instance)
		{
			seeds = "nil";
			saved_fish = "";
			loaded_fish = new List<string>();
			if (__instance.isHostingGame == true && ES3.KeyExists("4902.Koyuki-1", __instance.currentSaveFileName) == true)
			{
				ES3.DeleteKey("4902.Koyuki-1", __instance.currentSaveFileName);
			}
		}
	}

//	// unreadable mesh to readable mesh //
	public class fish1
	{
		public static Mesh fish2(Mesh nonReadableMesh)
		{
			Mesh meshCopy = new Mesh();
			meshCopy.indexFormat = nonReadableMesh.indexFormat;
			// Handle vertices
			GraphicsBuffer verticesBuffer = nonReadableMesh.GetVertexBuffer(0);
			int totalSize = verticesBuffer.stride * verticesBuffer.count;
			byte[] data = new byte[totalSize];
			verticesBuffer.GetData(data);
			meshCopy.SetVertexBufferParams(nonReadableMesh.vertexCount, nonReadableMesh.GetVertexAttributes());
			meshCopy.SetVertexBufferData(data, 0, 0, totalSize);
			verticesBuffer.Release();
			// Handle triangles
			meshCopy.subMeshCount = nonReadableMesh.subMeshCount;
			GraphicsBuffer indexesBuffer = nonReadableMesh.GetIndexBuffer();
			int tot = indexesBuffer.stride * indexesBuffer.count;
			byte[] indexesData = new byte[tot];
			indexesBuffer.GetData(indexesData);
			meshCopy.SetIndexBufferParams(indexesBuffer.count, nonReadableMesh.indexFormat);
			meshCopy.SetIndexBufferData(indexesData, 0, 0, tot);
			indexesBuffer.Release();
			// Restore submesh structure
			uint currentIndexOffset = 0;
			for (int i = 0; i < meshCopy.subMeshCount; i++)
			{
				uint subMeshIndexCount = nonReadableMesh.GetIndexCount(i);
				meshCopy.SetSubMesh(i, new SubMeshDescriptor((int)currentIndexOffset, (int)subMeshIndexCount));
				currentIndexOffset += subMeshIndexCount;
			}
			// Recalculate normals and bounds
			meshCopy.RecalculateNormals();
			meshCopy.RecalculateBounds();
			return meshCopy;
		}
	}

//	// custom component //
	public class temporary : MonoBehaviour {}
	public class ItemTypeSeed : MonoBehaviour
	{
		public string guid = koyu.harmony.Id;
		public string seed;
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
			if (value == uint.MaxValue) value = value - 1;
			double scale = ((double)(max - min)) / UInt32.MaxValue;
			return (int)(min + (value * scale)); //[min, max)
		}
		public uint next32mm(uint min, uint max, bool unsigned)
		{
			uint value = next32();
			if (value == uint.MaxValue) value = value - 1;
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
