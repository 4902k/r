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
using Unity.Netcode;

namespace shark
{
	[BepInPlugin("4902.Shark", "Shark", "1.0.0")]
	public class gura : BaseUnityPlugin
	{
		public static readonly Harmony harmony = new Harmony("4902.Shark");

		public static ManualLogSource mls;

		public static ConfigEntry<bool> temp_saveseeds;
		public static ConfigEntry<int>  temp_millisecond;
		public static ConfigEntry<bool> temp_synced_percentages;
		public static ConfigEntry<int>  temp_sfx;
		public static ConfigEntry<int>  temp_blue;
		public static ConfigEntry<int>  temp_pink;

		public static cfg_bool cfg_saveseeds = new cfg_bool();
		public static cfg_int  cfg_millisecond = new cfg_int();
		public static cfg_bool cfg_synced_percentages = new cfg_bool();
		public static cfg_int  cfg_sfx = new cfg_int();
		public static cfg_int  blue_percentage = new cfg_int();
		public static cfg_int  pink_percentage = new cfg_int();

		private void Awake()
		{
			temp_saveseeds = Config.Bind("#", "save/load", true, "[Save/load seeds]\nwhether the large axle/blahaj item types should be saved to the save file. this saves the seed that determines the items type, so it will be the same when rejoining.\nloading a save file that has saved seeds without having this enabled or while in lan isn't recommended as the saved seeds can be reset if the number of items is changed.\nsaved item types are synced with other players if the synced percentages config is enabled"); cfg_saveseeds.Value = temp_saveseeds.Value;
			temp_millisecond = Config.Bind("#", "timer", 100, "[Client wait_timer]\nwhen joining a lobby as non-host blahaj items will wait to set their item type until the network message sent by the host has been received or the time spent waiting reached the maximum amount set by this config.\nthere will be a log message specifying if the network message was received first or the timer ended first. if the timer is often ending before the message from the host is being received then this should be increased. if the host doesn't have this mod then save/load can be disabled since there won't be a received message from the host so the items would always wait the full timer.\nmin is 20, max is 4000. 100 is before the player spawn animation ends, 500 is about 10 seconds, 1000 is about 15 seconds"); cfg_millisecond.Value = temp_millisecond.Value;
			temp_synced_percentages = Config.Bind("#", "sync", true, "[Synced percentages]\nautomatically sync config item percentages with the host. only disable if you're aware that disabling this can cause the item types to not be the same as other players if playing with others that have this mod"); cfg_synced_percentages.Value = temp_synced_percentages.Value;
			temp_sfx = Config.Bind("#", "sfx", 1, "[Sound Effects]\nset which grab/drop sound effects to use.\n1 = blahaj (original)\n2 = plushie pajama man\n3 = zed dog"); cfg_sfx.Value = temp_sfx.Value;
			temp_blue = Config.Bind("#", "blahaj_percent", 50, "percentage that blahaj will replace axle\ninput = percentage, 50 is 50% etc"); blue_percentage.Value = temp_blue.Value;
			temp_pink = Config.Bind("#", "pink_variant_percent", 10, "percentage that blahaj will be pink\ninput = percentage"); pink_percentage.Value = temp_pink.Value;
			mls = BepInEx.Logging.Logger.CreateLogSource("Shark");
			mls.LogInfo("Blahaj is in your walls");
			harmony.PatchAll(typeof(blahaj));

			}}public class cfg_bool{public bool Value{get;set;
			}}public class cfg_int{public int Value{get;set;
		}
	}
	public class blahaj
	{
//		// scrap items //
		private static string[] item = new string[2];

		private static Mesh[] fish = new Mesh[2];

		private static Material[][] cats = new Material[3][];

		private static AudioClip[] take = new AudioClip[2];

		private static AudioClip[] give = new AudioClip[2];

		private static bool dots = false;

		private static UInt64 lobbyid = 0uL;

		private static bool[] first_item = new bool[] {true, true, true};

		[HarmonyPatch(typeof(GrabbableObject), "Start"), HarmonyPostfix]
		private static void pst1(GrabbableObject __instance)
		{
			pst1async(__instance);
		}
		private static async void pst1async(GrabbableObject __instance)
		{
			if (__instance.itemProperties.name == "Cog1")
			{
				await wait_frames(1);
				if (__instance.gameObject.GetComponent<shark.temporary>() != null) return;
				__instance.gameObject.AddComponent<shark.temporary>();
				if (first_item[0] == true)
				{
					Item r = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Cog1");
					r.restingRotation = new Vector3(7f, 0f, 0f);
					r.verticalOffset = 0.5f;
				}
				if (GameNetworkManager.Instance.disableSteam == false && seeds == "nil")
				{
					if (GameNetworkManager.Instance.isHostingGame == false)
					{
						if (first_item[0] == true) { gura.mls.LogInfo("await wait_timer"); first_item[0] = false; }
						await wait_timer(gura.cfg_millisecond.Value >= 20 && gura.cfg_millisecond.Value <= 4000 ? gura.cfg_millisecond.Value : 100);
						if (disconnected[0] == true) return;
					}
					if (seeds == "nil") seeds = "?";
				}
				if (item[0] == null)
				{
					string text = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "4902-shark").Replace("\\", "/");
					gura.mls.LogMessage("Searching this filepath:" + text);
					AssetBundle asset = AssetBundle.LoadFromFileAsync(text).assetBundle;
					//axle
					item[0] = "Large axle";
					fish[0] = __instance.GetComponent<MeshFilter>().mesh;
					cats[0] = __instance.GetComponent<MeshRenderer>().materials;
					take[0] = __instance.itemProperties.grabSFX;
					give[0] = __instance.itemProperties.dropSFX;
					//shark
					item[1] = "Shark plushie";
					fish[1] = asset.LoadAsset<Mesh>("blahaj.fbx");
					cats[1] = new Material[1] {asset.LoadAsset<Material>("blahaj.mat")}; //blue
					cats[2] = new Material[1] {asset.LoadAsset<Material>("beyoublahaj.mat")}; //pink
					if (gura.cfg_sfx.Value == 2)
					{
						take[1] = StartOfRound.Instance.unlockablesList.unlockables.First(_ => _.unlockableName == "Plushie pajama man").prefabObject.GetComponentInChildren<AnimatedObjectTrigger>(true).boolFalseAudios[0];
						give[1] = take[1];
					}
					else if (gura.cfg_sfx.Value == 3)
					{
						take[1] = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Zeddog").grabSFX;
						give[1] = take[1];
					}
					else
					{
						take[1] = asset.LoadAsset<AudioClip>("blahajgrab.mp3");
						give[1] = asset.LoadAsset<AudioClip>("blahajdrop.mp3");
					}
				}
				if (item[0] != null)
				{
					Shion cr;
					if (GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && __instance.GetComponent<NetworkObject>() != null)
					{
						UInt64 n64;
						if (gura.cfg_saveseeds.Value == true && seeds != "nil" && seeds != "?" && seeds.Contains(__instance.GetComponent<NetworkObject>().NetworkObjectId + ".") == true)
						{
							if (first_item[2] == true) gura.mls.LogInfo("custom_random = new Shion(saved seed)");
							int start = seeds.IndexOf(__instance.GetComponent<NetworkObject>().NetworkObjectId + ".");
							int n = __instance.GetComponent<NetworkObject>().NetworkObjectId.ToString().Length + 1;
							n64 = UInt64.Parse(seeds.Substring(start + n, seeds.IndexOf("/", start) - (start + n)));
						}
						else
						{
							if (first_item[2] == true) gura.mls.LogInfo("custom_random = new Shion(lobbyid+networkobjectid)");
							n64 = lobbyid + __instance.GetComponent<NetworkObject>().NetworkObjectId;
						}
						cr = new Shion(n64);
						if (gura.cfg_saveseeds.Value == true)
						{
							__instance.gameObject.AddComponent<ItemTypeSeed>();
							__instance.gameObject.GetComponent<ItemTypeSeed>().seed = n64.ToString();
						}
					}
					else
					{
						if (first_item[2] == true) gura.mls.LogInfo("custom_random = new Shion()");
						cr = new Shion();
					}
					if (first_item[2] == true) gura.mls.LogInfo(gura.cfg_synced_percentages.Value == true && synced_percents[0] != -1 ? "host config" : "local config");
					first_item[2] = false;
					int[] percents = (gura.cfg_synced_percentages.Value == true && synced_percents[0] != -1 ? synced_percents : new int[] {-1, gura.blue_percentage.Value, gura.pink_percentage.Value});
					int num = (cr.next32mm(0, 100) < percents[1] ? 1 : 0);
					if (num == 1)
					{
						if (dots == false)
						{
							Vector3[] vertices = fish[1].vertices;
							Vector3[] normals = fish[1].normals;
							Vector3 center = fish[1].bounds.center;
							for (int n = 0; n < vertices.Length; n = n + 1)
							{
								vertices[n] = vertices[n] - center;
								vertices[n] = vertices[n] * 48f;
								float x = vertices[n].x;
								float z = vertices[n].z;
								float nx = normals[n].x;
								float nz = normals[n].z;
								vertices[n].x = z;
								vertices[n].z = -x;
								normals[n].x = nz;
								normals[n].z = -nx;
								vertices[n] = vertices[n] + center;
							}
							fish[1].vertices = vertices;
							fish[1].normals = normals;
							fish[1].RecalculateBounds();
							fish[1].RecalculateNormals();
							dots = true;
						}
						__instance.GetComponent<MeshRenderer>().materials = (cr.next32mm(0, 100) < percents[2] ? cats[2] : cats[1]);
					}
					else
					{
						__instance.GetComponent<MeshRenderer>().materials = cats[0];
					}
					__instance.GetComponentInChildren<ScanNodeProperties>().headerText = item[num];
					__instance.GetComponent<MeshFilter>().mesh = fish[num];
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
			if (disconnected[0] == true) { if (disconnected[1] == false) { gura.mls.LogMessage("disconnected before wait_timer ended"); disconnected[1] = true; } return; }
			if (first_item[1] == true) { gura.mls.LogMessage(seeds == "nil" && client_received == false ? "timer ended before receiving network message (" + n + "/" + gura.cfg_millisecond.Value + ")" : "received network message before timer ended (" + n + "/" + gura.cfg_millisecond.Value + ")"); first_item[1] = false; }
		}
		[HarmonyPatch(typeof(GrabbableObject), "PlayDropSFX"), HarmonyPrefix]
		private static void pre1(ref GrabbableObject __instance)
		{
			if (item[0] != null && __instance != null && __instance.itemProperties.name == "Cog1" && __instance.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				__instance.itemProperties.dropSFX = (__instance.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? give[1] : give[0]);
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
					yield return new CodeInstruction(OpCodes.Call, typeof(blahaj).GetMethod("grab_item"));
				}
				//gura.mls.LogInfo(l[n].ToString());
			}
		}
		public static void grab_item(GrabbableObject currentlyGrabbingObject)
		{
			if (item[0] != null && currentlyGrabbingObject != null && currentlyGrabbingObject.itemProperties.name == "Cog1" && currentlyGrabbingObject.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				currentlyGrabbingObject.itemProperties.grabSFX = (currentlyGrabbingObject.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? take[1] : take[0]);
				currentlyGrabbingObject.itemProperties.itemName = currentlyGrabbingObject.GetComponentInChildren<ScanNodeProperties>().headerText;
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
					yield return new CodeInstruction(OpCodes.Call, typeof(blahaj).GetMethod("hold_item"));
				}
				//gura.mls.LogInfo(l[n].ToString());
			}
		}
		public static void hold_item(PlayerControllerB player)
		{
			GrabbableObject _item = player.ItemSlots[player.currentItemSlot];
			if (item[0] != null && _item != null && _item.itemProperties.name == "Cog1" && _item.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				_item.itemProperties.grabSFX = (_item.GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? take[1] : take[0]);
				PlayerControllerB local_player = GameNetworkManager.Instance.localPlayerController;
				if (player == local_player || local_player.ItemSlots[local_player.currentItemSlot] == null || local_player.ItemSlots[local_player.currentItemSlot].itemProperties.name != "MagnifyingGlass" || local_player.isPlayerDead == true)
				{
					_item.itemProperties.itemName = _item.GetComponentInChildren<ScanNodeProperties>().headerText;
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
					yield return new CodeInstruction(OpCodes.Call, typeof(blahaj).GetMethod("display_items"));
				}
				yield return l[n];
				//gura.mls.LogInfo(l[n].ToString());
			}
		}
		public static void display_items(HUDManager instance, GameObject displayingObject)
		{
			if (item[0] != null && instance.itemsToBeDisplayed[0] != null && instance.itemsToBeDisplayed[0].itemProperties.name == "Cog1" && displayingObject.name == "Cog(Clone)" && instance.itemsToBeDisplayed[0].GetComponentInChildren<ScanNodeProperties>() != null)
			{
				int n = (instance.itemsToBeDisplayed[0].GetComponentInChildren<ScanNodeProperties>().headerText == item[1] ? 1 : 0);
				instance.itemsToBeDisplayed[0].itemProperties.itemName = item[n];
				displayingObject.GetComponent<MeshFilter>().mesh = fish[n];
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "Awake"), HarmonyPostfix]
		private static void pst2()
		{
			if (GameNetworkManager.Instance.disableSteam == false)
			{
				disconnected = new bool[] {false, false};
				synced_percents = new int[] {-1, -1, -1};
				if (GameNetworkManager.Instance.currentLobby.HasValue == true)
				{
					lobbyid = (GameNetworkManager.Instance.currentLobby.Value.Id % 1000000000);
					gura.mls.LogInfo(lobbyid);
				}
				else
				{
					gura.mls.LogError("current lobby id is null");
				}
			}
		}

//		// network syncing //
		private static bool sync = false;

		private static string seeds = "nil";

		private static bool[] disconnected = new bool[] {false, false};

		private static int[] synced_percents = new int[] {-1, -1, -1};

		private static bool client_received = false;

		[HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject"), HarmonyPostfix]
		private static void pst3()
		{
			if (sync == false)
			{
				if (NetworkManager.Singleton.IsHost == true)
				{
					NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("4902.Blahaj-Host", host_receive);
				}
				else
				{
					gura.mls.LogInfo("requesting message from host");
					NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("4902.Blahaj-Client", client_receive);
					FastBufferWriter w = new FastBufferWriter(0, Unity.Collections.Allocator.Temp);
					NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Blahaj-Host", NetworkManager.ServerClientId, w);
					w.Dispose();
				}
				sync = true;
			}
		}
		private static void host_receive(ulong id, FastBufferReader r)
		{
			if (NetworkManager.Singleton.IsHost == true)
			{
				gura.mls.LogInfo("received request from client");
				string message = "1," + gura.blue_percentage.Value + "," + gura.pink_percentage.Value + "^" + lobbyid + "^" + seeds;
				gura.mls.LogInfo("sending message " + message);
				FastBufferWriter w = new FastBufferWriter(FastBufferWriter.GetWriteSize(message), Unity.Collections.Allocator.Temp);
				w.WriteValueSafe(message);
				NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Blahaj-Client", id, w, NetworkDelivery.ReliableFragmentedSequenced);
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
				gura.mls.LogInfo("client received message " + message);
				int n = 2;
				if ((message.Length - message.Replace("^", "").Length) == n)
				{
					string[] s = message.Split(new char[]{'^'}, n + 1);
					synced_percents = System.Array.ConvertAll(s[0].Split(new char[]{','}), System.Convert.ToInt32);
					lobbyid = UInt64.Parse(s[1]);
					seeds = s[2];
				}
				else
				{
					gura.mls.LogError("received message was not what was expected. wasn't able to sync variables with host. (are the mod versions not the same?)");
					gura.mls.LogError("found " + (message.Length - message.Replace("^", "").Length) + "/" + n + " ^ in message " + message);
				}
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "Disconnect"), HarmonyPrefix]
		private static void pre2()
		{
			disconnected[0] = true;
			if (StartOfRound.Instance != null && NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
			{
				try { NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("4902.Blahaj-Host"); NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("4902.Blahaj-Client"); } catch (System.Exception error) { gura.mls.LogError(error); }
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "Disconnect"), HarmonyPostfix]
		private static void pst4()
		{
			sync = false;
			seeds = "nil";
			lobbyid = 0uL;
			first_item = new bool[] {true, true, true};
			saved_axle = "";
			loaded_axle = new List<string>();
			synced_percents = new int[] {-1, -1, -1};
			client_received = false;
		}

//		// saving/loading //
		private static string saved_axle = "";

		private static List<string> loaded_axle = new List<string>();

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
					yield return new CodeInstruction(OpCodes.Call, typeof(blahaj).GetMethod("save_axle"));
				}
				//gura.mls.LogInfo(l[n].ToString());
			}
		}
		public static void save_axle(GrabbableObject[] _items, int index)
		{
			if (gura.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && _items[index].itemProperties.name == "Cog1")
			{
				if (_items[index].GetComponent<ItemTypeSeed>() != null)
				{
					ItemTypeSeed component = _items[index].GetComponent<ItemTypeSeed>();
					saved_axle = saved_axle + component.seed + "/";
				}
				else if (_items[index].GetComponent<NetworkObject>() != null)
				{
					saved_axle = saved_axle + (lobbyid + _items[index].GetComponent<NetworkObject>().NetworkObjectId).ToString() + "/";
					gura.mls.LogInfo("ItemTypeSeed is null! saving seed for this item as lobbyid+networkobjectid");
				}
				else
				{
					saved_axle = saved_axle + new Shion().next32mm(1, 101).ToString() + "/";
					gura.mls.LogInfo("ItemTypeSeed and NetworkObject are null! saving seed for this item as a random number from 1 to 100");
				}
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "SaveGame"), HarmonyPostfix]
		private static void pst5()
		{
			if (gura.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && StartOfRound.Instance.inShipPhase == true && StartOfRound.Instance.isChallengeFile == false)
			{
				try
				{
					if (saved_axle == "") saved_axle = "nil";
					if (saved_axle.EndsWith("/") == true) saved_axle = saved_axle.Substring(0, saved_axle.Length - 1);
					gura.mls.LogInfo("saving " + saved_axle);
					ES3.Save("4902.Blahaj-1", saved_axle, GameNetworkManager.Instance.currentSaveFileName);
					saved_axle = "";
				}
				catch (System.Exception error)
				{
					gura.mls.LogError("Error saving item types: " + error);
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
					yield return new CodeInstruction(OpCodes.Call, typeof(blahaj).GetMethod("load_axle"));
				}
				//gura.mls.LogInfo(l[n].ToString());
			}
		}
		public static void load_axle(GrabbableObject _item)
		{
			if (gura.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && _item.itemProperties.name == "Cog1")
			{
				if (_item.GetComponent<NetworkObject>() != null)
				{
					loaded_axle.Add(_item.GetComponent<NetworkObject>().NetworkObjectId.ToString());
				}
				else
				{
					loaded_axle.Add("nil");
					gura.mls.LogInfo("NetworkObject is null! the seed can't be loaded for this item");
				}
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "Start"), HarmonyPostfix]
		private static void pst6()
		{
			if (gura.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false)
			{
				try
				{
					string temp = ES3.Load("4902.Blahaj-1", GameNetworkManager.Instance.currentSaveFileName, "nil");
					gura.mls.LogInfo("loaded " + temp);
					string[] s = temp.Split(new char[]{'/'});
					if (s[0] != "nil" && s[0] != "" && s.Length == loaded_axle.Count)
					{
						seeds = "";
						for (int n = 0; n < loaded_axle.Count; n = n + 1)
						{
							seeds = seeds + loaded_axle[n] + "." + s[n] + "/";
						}
						gura.mls.LogInfo("current networkobjectids + saved seeds " + seeds);
					}
					loaded_axle = new List<string>();
				}
				catch (System.Exception error)
				{
					gura.mls.LogError("Error loading item types: " + error);
				}
			}
		}
	}

//	// custom component //
	public class temporary : MonoBehaviour {}
	public class ItemTypeSeed : MonoBehaviour
	{
		public string guid = gura.harmony.Id;
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
			double scale = ((double)(max - min)) / UInt32.MaxValue;
			return (int)(min + (value * scale));
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
			UInt64 nextInt64 = xoshiro256ss(); //0 inclusive, 1 exclusive
			return (double)nextInt64 / (double)(UInt64.MaxValue);
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
/*previous version of this mods code. i remade it for uploading, mostly to add percentages for axle and configs
//this was my first mod for lethal company, most of the code i referenced from https://thunderstore.io/c/lethal-company/p/Mellowdy/Maxwell/
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Blahaj.Patches;
using UnityEngine;

namespace Blahaj
{
	[BepInPlugin("4902.SharkReplacer", "SharkReplacer", "1.0.0")]
	public class SharkReplacerMod : BaseUnityPlugin
	{
		private readonly Harmony harmony = new Harmony("SharkReplacer");

		public static ManualLogSource mls;

		private static SharkReplacerMod instance;

		private void Awake()
		{
			if ((Object)(object)instance == (Object)null)
			{
				instance = this;
			}
			mls = BepInEx.Logging.Logger.CreateLogSource("SharkReplacer");
			mls.LogInfo("Blahaj is in your walls");
			harmony.PatchAll(typeof(SharkReplacerMod));
			harmony.PatchAll(typeof(ItemReplacerPatch));
		}
	}
}
namespace Blahaj.Patches
{
	[HarmonyPatch(typeof(GrabbableObject))]
	internal class ItemReplacerPatch
	{
		private static string assetName = "shark.plushie";

		private static string name = "Shark plushie";

		private static Mesh model = null;

		private static Material blue;

		private static Material pink;

		private static AudioClip grab;

		private static AudioClip drop;

		private static float size = 48f;

		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void ReplaceModel(GrabbableObject __instance, ref MeshRenderer ___mainObjectRenderer)
		{
			try
			{
				if (model == null)
				{
					string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
					string text = Path.Combine(directoryName, assetName).Replace("\\", "/");
					SharkReplacerMod.mls.LogMessage("Searching this filepath:" + text);
					AssetBundleCreateRequest val = AssetBundle.LoadFromFileAsync(text);
					AssetBundle assetBundle = val.assetBundle;
					model = assetBundle.LoadAsset<Mesh>("blahaj.fbx");
					blue = assetBundle.LoadAsset<Material>("blahaj.mat");
					pink = assetBundle.LoadAsset<Material>("beyoublahaj.mat");
					grab = assetBundle.LoadAsset<AudioClip>("blahajgrab.mp3");
					drop = assetBundle.LoadAsset<AudioClip>("blahajdrop.mp3");
				}
			}
			catch
			{
				SharkReplacerMod.mls.LogError("Assets did not load");
			}
			//int random = new Random().Next(0, 10);
			if (model != null && ((Object)__instance.itemProperties).name == "Cog1")
			{
				MeshFilter component = ___mainObjectRenderer.GetComponent<MeshFilter>();
				component.mesh = Object.Instantiate<Mesh>(model);
				Mesh mesh = component.mesh;
				Vector3[] vertices = mesh.vertices;
				Vector3[] normals = mesh.normals;
				Bounds bounds = mesh.bounds;
				Vector3 center = bounds.center;
				for (int n = 0; n < vertices.Length; n++)
				{
					//vertices[n] = center + ((vertices[n] - center) * size);
					vertices[n] = vertices[n] - center;
					vertices[n] = vertices[n] * size;
					float x = vertices[n].x;
					float z = vertices[n].z;
					float nx = normals[n].x;
					float nz = normals[n].z;
					vertices[n].x = z;
					vertices[n].z = -x;
					normals[n].x = nz;
					normals[n].z = -nx;
					vertices[n] = vertices[n] + center;
				}
				mesh.vertices = vertices;
				mesh.normals = normals;
				mesh.RecalculateBounds();
				mesh.RecalculateNormals();
				if (Random.Range(0, 10) == 0)
				{
					___mainObjectRenderer.materials = new Material[1] {pink};
				}
				else
				{
					___mainObjectRenderer.materials = new Material[1] {blue};
				}
				__instance.itemProperties.itemName = name;
				___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = name;
				__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = mesh;
				__instance.itemProperties.grabSFX = grab;
				__instance.itemProperties.dropSFX = drop;
			}
		}
	}
}*/