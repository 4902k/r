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

namespace blocks
{
	[BepInPlugin("4902.Minecraft_Scraps", "Minecraft_Scraps", "1.0.0")]
	public class yon : BaseUnityPlugin
	{
		public static readonly Harmony harmony = new Harmony("4902.Minecraft_Scraps");

		public static ManualLogSource mls;

		public static ConfigEntry<bool> temp_saveseeds;
		public static ConfigEntry<int>  temp_millisecond;
		public static ConfigEntry<bool> temp_synced_percentages;
		public static ConfigEntry<int>  temp_base_percentage;
		public static ConfigEntry<int>  temp_diamond_block;
		public static ConfigEntry<int>  temp_spider;
		public static ConfigEntry<int>  temp_zombie;
		public static ConfigEntry<int>  temp_villager;
		public static ConfigEntry<int>  temp_creeper;
		public static ConfigEntry<int>  temp_pumpkin;
		public static ConfigEntry<int>  temp_anvil;
		public static ConfigEntry<int>  temp_steve;
		public static ConfigEntry<int>  temp_tnt;
		public static ConfigEntry<int>  temp_diamond;
		public static ConfigEntry<int>  temp_diamond_sword;
		public static ConfigEntry<int>  temp_diamond_pickaxe;
		public static ConfigEntry<int>  temp_apple;
		public static ConfigEntry<int>  temp_torch;
		public static ConfigEntry<int>  temp_chest;
		public static ConfigEntry<int>  temp_pig;
		public static ConfigEntry<int>  temp_sheep;
		public static ConfigEntry<int>  temp_cow;
		public static ConfigEntry<int>  temp_chicken;
		public static ConfigEntry<int>  temp_bee;

		public static cfg_bool cfg_saveseeds = new cfg_bool();          //true
		public static cfg_int  cfg_millisecond = new cfg_int();         //100
		public static cfg_bool cfg_synced_percentages = new cfg_bool(); //true
		public static cfg_int  cfg_base_percentage = new cfg_int();     //80
		public static cfg_int  cfg_diamond_block = new cfg_int();       //. . . 10
		public static cfg_int  cfg_spider = new cfg_int();              //55
		public static cfg_int  cfg_zombie = new cfg_int();              //55
		public static cfg_int  cfg_villager = new cfg_int();            //. 40
		public static cfg_int  cfg_creeper = new cfg_int();             //55
		public static cfg_int  cfg_pumpkin = new cfg_int();             //50
		public static cfg_int  cfg_anvil = new cfg_int();               //. . 35
		public static cfg_int  cfg_steve = new cfg_int();               //. . . . 01
		public static cfg_int  cfg_tnt = new cfg_int();                 //. 40
		public static cfg_int  cfg_diamond = new cfg_int();             //. 40
		public static cfg_int  cfg_diamond_sword = new cfg_int();       //. . 30
		public static cfg_int  cfg_diamond_pickaxe = new cfg_int();     //. . 30
		public static cfg_int  cfg_apple = new cfg_int();               //50
		public static cfg_int  cfg_torch = new cfg_int();               //50
		public static cfg_int  cfg_chest = new cfg_int();               //50
		public static cfg_int  cfg_pig = new cfg_int();                 //. 45
		public static cfg_int  cfg_sheep = new cfg_int();               //. 45
		public static cfg_int  cfg_cow = new cfg_int();                 //. 45
		public static cfg_int  cfg_chicken = new cfg_int();             //. 45
		public static cfg_int  cfg_bee = new cfg_int();                 //. . . 15

		private void Awake()
		{
			temp_saveseeds          = Config.Bind("#", "save/load", true, "[Save/load seeds]\nwhether the magnifying glass/minecraft item types should be saved to the save file. this saves the seed that determines the items type, so it will be the same when rejoining.\nloading a save file that has saved seeds without having this enabled or while in lan isn't recommended as the saved seeds can be reset if the number of items is changed.\nsaved item types are synced with other players if the synced percentages config is enabled"); cfg_saveseeds.Value = temp_saveseeds.Value;
			temp_millisecond        = Config.Bind("#", "timer", 100, "[Client wait_timer]\nwhen joining a lobby as non-host magnifying glass items will wait to set their item type until the network message sent by the host has been received or the time spent waiting reached the maximum amount set by this config.\nthere will be a log message specifying if the network message was received first or the timer ended first. if the timer is often ending before the message from the host is being received then this should be increased. if the host doesn't have this mod then save/load can be disabled since there won't be a received message from the host so the items would always wait the full timer.\nmin is 20, max is 4000. 100 is before the player spawn animation ends, 500 is about 10 seconds, 1000 is about 15 seconds"); cfg_millisecond.Value = temp_millisecond.Value;
			temp_synced_percentages = Config.Bind("#", "sync", true, "[Synced percentages]\nautomatically sync config item percentages/weights with the host. only disable if you're aware that disabling this can cause the item types to not be the same as other players if playing with others that have this mod"); cfg_synced_percentages.Value = temp_synced_percentages.Value;
			temp_base_percentage    = Config.Bind("#", "base_percent", 80, "percentage that a minecraft scrap will replace magnifying glass\ninput = percentage, 50 is 50% etc"); cfg_base_percentage.Value = temp_base_percentage.Value;
			temp_diamond_block      = Config.Bind("Items", "diamond_block", 10, "item types with higher numbers relative to other item types will be more frequent\n(if there are (10 red, 20 green, 30 blue) blocks, and if one block was selected randomly, the color with more blocks would be more likely to be selected, and a color with no blocks couldn't be selected)\n(color is the item type, number of blocks is the config number)"); cfg_diamond_block.Value = temp_diamond_block.Value;
			temp_spider             = Config.Bind("Items", "spider", 55, ""); cfg_spider.Value = temp_spider.Value;
			temp_zombie             = Config.Bind("Items", "zombie", 55, ""); cfg_zombie.Value = temp_zombie.Value;
			temp_villager           = Config.Bind("Items", "villager", 40, ""); cfg_villager.Value = temp_villager.Value;
			temp_creeper            = Config.Bind("Items", "creeper", 55, ""); cfg_creeper.Value = temp_creeper.Value;
			temp_pumpkin            = Config.Bind("Items", "pumpkin", 50, ""); cfg_pumpkin.Value = temp_pumpkin.Value;
			temp_anvil              = Config.Bind("Items", "anvil", 35, ""); cfg_anvil.Value = temp_anvil.Value;
			temp_steve              = Config.Bind("Items", "steve", 1, ""); cfg_steve.Value = temp_steve.Value;
			temp_tnt                = Config.Bind("Items", "tnt", 40, ""); cfg_tnt.Value = temp_tnt.Value;
			temp_diamond            = Config.Bind("Items", "diamond", 40, ""); cfg_diamond.Value = temp_diamond.Value;
			temp_diamond_sword      = Config.Bind("Items", "diamond_sword", 30, ""); cfg_diamond_sword.Value = temp_diamond_sword.Value;
			temp_diamond_pickaxe    = Config.Bind("Items", "diamond_pickaxe", 30, ""); cfg_diamond_pickaxe.Value = temp_diamond_pickaxe.Value;
			temp_apple              = Config.Bind("Items", "apple", 50, ""); cfg_apple.Value = temp_apple.Value;
			temp_torch              = Config.Bind("Items", "torch", 50, ""); cfg_torch.Value = temp_torch.Value;
			temp_chest              = Config.Bind("Items", "chest", 50, ""); cfg_chest.Value = temp_chest.Value;
			temp_pig                = Config.Bind("Items", "pig", 45, ""); cfg_pig.Value = temp_pig.Value;
			temp_sheep              = Config.Bind("Items", "sheep", 45, ""); cfg_sheep.Value = temp_sheep.Value;
			temp_cow                = Config.Bind("Items", "cow", 45, ""); cfg_cow.Value = temp_cow.Value;
			temp_chicken            = Config.Bind("Items", "chicken", 45, ""); cfg_chicken.Value = temp_chicken.Value;
			temp_bee                = Config.Bind("Items", "bee", 15, ""); cfg_bee.Value = temp_bee.Value;

			mls = BepInEx.Logging.Logger.CreateLogSource("Minecraft Scraps");
			mls.LogInfo("1.12.2");
			harmony.PatchAll(typeof(minecraft_scraps));

			}}public class cfg_bool{public bool Value{get;set;
			}}public class cfg_int{public int Value{get;set;
		}
	}
	public class minecraft_scraps
	{
//		// scrap items //
		private static string[] item = new string[1 + 20];

		private static Mesh[] fish = new Mesh[1 + 15];

		private static Material[][] cats = new Material[1 + 15][];

		private static AudioClip[] take = new AudioClip[1 + 20];

		private static AudioClip[] give = new AudioClip[1 + 20];

		private static bool[] dots = new bool[1 + 15];

		private static int[] item_weights = new int[1 + 20];

		private static Transform chest;

		private static Transform[] tree = new Transform[5];

		private static Vector3[][] ward = new Vector3[5][];

		private static UInt64 lobbyid = 0uL;

		private static bool[] first_item = new bool[] {true, true, true};

		[HarmonyPatch(typeof(GrabbableObject), "Start"), HarmonyPostfix]
		private static void pst1(GrabbableObject __instance, ref MeshRenderer ___mainObjectRenderer)
		{
			pst1async(__instance, ___mainObjectRenderer);
		}
		private static async void pst1async(GrabbableObject __instance, MeshRenderer ___mainObjectRenderer)
		{
			if (__instance.itemProperties.name == "MagnifyingGlass")
			{
				await wait_frames(1);
				if (__instance.gameObject.GetComponent<blocks.temporary>() != null) return;
				__instance.gameObject.AddComponent<blocks.temporary>();
				if (first_item[0] == true)
				{
					Item r = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "MagnifyingGlass");
					r.restingRotation = new Vector3(0f, 90f, -90f);
					r.verticalOffset = 0.05f;
				}
				if (GameNetworkManager.Instance.disableSteam == false && seeds == "nil")
				{
					if (GameNetworkManager.Instance.isHostingGame == false)
					{
						if (first_item[0] == true) { yon.mls.LogInfo("await wait_timer"); first_item[0] = false; }
						await wait_timer(yon.cfg_millisecond.Value >= 20 && yon.cfg_millisecond.Value <= 4000 ? yon.cfg_millisecond.Value : 100);
						if (disconnected[0] == true) return;
					}
					if (seeds == "nil") seeds = "?";
				}
				MeshFilter component = ___mainObjectRenderer.GetComponent<MeshFilter>();
				if (item[0] == null)
				{
					string text = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "4902-minecraft_scraps").Replace("\\", "/");
					yon.mls.LogMessage("Searching this filepath:" + text);
					AssetBundle asset = AssetBundle.LoadFromFileAsync(text).assetBundle;
					//glass
					item[0] = __instance.itemProperties.itemName;
					fish[0] = component.mesh;
					cats[0] = ___mainObjectRenderer.materials;
					take[0] = __instance.itemProperties.grabSFX;
					give[0] = __instance.itemProperties.dropSFX;
					//diamond_block
					item[1] = "Diamond block";
					fish[1] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<Mesh>("minecraft diamond block.fbx")));
					cats[1] = new Material[] {asset.LoadAsset<Material>("diamond-minecraft-2.mat")};
					take[1] = asset.LoadAsset<AudioClip>("block place1.ogg");
					give[1] = asset.LoadAsset<AudioClip>("block place2.ogg");
					//spider
					item[2] = "Spider";
					fish[2] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<Mesh>("Spider.fbx")));
					cats[2] = new Material[] {asset.LoadAsset<Material>("spider.mat")};
					take[2] = asset.LoadAsset<AudioClip>("spider1.ogg");
					give[2] = asset.LoadAsset<AudioClip>("spider2.ogg");
					//zombie
					item[3] = "Zombie";
					fish[3] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<Mesh>("Zombie.fbx")));
					cats[3] = new Material[] {asset.LoadAsset<Material>("zombie.mat")};
					take[3] = asset.LoadAsset<AudioClip>("zombie1.ogg");
					give[3] = asset.LoadAsset<AudioClip>("zombie2.ogg");
					//villager
					item[4] = "Villager";
					fish[4] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<Mesh>("Villager.fbx")));
					cats[4] = new Material[] {asset.LoadAsset<Material>("villager_farmer.mat")};
					take[4] = asset.LoadAsset<AudioClip>("villager1.ogg");
					give[4] = asset.LoadAsset<AudioClip>("villager2.ogg");
					//creeper
					item[5] = "Creeper";
					fish[5] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<Mesh>("Creeper.fbx")));
					cats[5] = new Material[] {asset.LoadAsset<Material>("creeper.mat")};
					take[5] = asset.LoadAsset<AudioClip>("creeper.ogg");
					give[5] = take[5];
					//pumpkin
					item[6] = "Pumpkin";
					fish[6] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<GameObject>("pumpkin.obj").GetComponentInChildren<MeshFilter>().mesh));
					cats[6] = new Material[] {asset.LoadAsset<Material>("pumpkin.mat")};
					take[6] = take[1];
					give[6] = give[1];
					//anvil
					item[7] = "Anvil";
					fish[7] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<GameObject>("mineways2skfb.obj").GetComponentInChildren<MeshFilter>().mesh));
					cats[7] = new Material[] {asset.LoadAsset<Material>("mineways2skfb-rgba.mat")};
					take[7] = asset.LoadAsset<AudioClip>("anvil1.ogg");
					give[7] = asset.LoadAsset<AudioClip>("anvil2.ogg");
					//steve
					item[8] = "Steve";
					fish[8] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<Mesh>("steve.fbx")));
					cats[8] = new Material[] {asset.LoadAsset<Material>("steve.mat")};
					take[8] = take[0];
					give[8] = give[0];
					//tnt
					item[9] = "TNT";
					fish[9] = Object.Instantiate<Mesh>(asset.LoadAsset<Mesh>("mesh_id27_0_0.asset"));
					cats[9] = new Material[] {asset.LoadAsset<Material>("60_0.mat")};
					take[9] = asset.LoadAsset<AudioClip>("tnt1.ogg");
					give[9] = asset.LoadAsset<AudioClip>("tnt2.ogg");
					//diamond
					item[10] = "Diamond";
					fish[10] = Object.Instantiate<Mesh>(asset.LoadAsset<Mesh>("cube_0_0_0.asset"));
					cats[10] = new Material[] {asset.LoadAsset<Material>("material_0.mat")};
					take[10] = take[1];
					give[10] = give[1];
					//diamond_sword
					item[11] = "Diamond sword";
					fish[11] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<GameObject>("diamondsword.obj").GetComponentInChildren<MeshFilter>().mesh));
					cats[11] = new Material[] {asset.LoadAsset<Material>("diffuse1.mat")};
					take[11] = take[1];
					give[11] = give[1];
					//diamond_pickaxe
					item[12] = "Diamond pickaxe";
					fish[12] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<GameObject>("diamond-pickaxe.obj").GetComponentInChildren<MeshFilter>().mesh));
					cats[12] = new Material[] {asset.LoadAsset<Material>("diffuse2.mat")};
					take[12] = take[1];
					give[12] = give[1];
					//apple
					item[13] = "Apple";
					fish[13] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<GameObject>("apple.blend").GetComponentInChildren<MeshFilter>().mesh));
					cats[13] = new Material[] {asset.LoadAsset<Material>("apple.mat")};
					take[13] = asset.LoadAsset<AudioClip>("eat1.ogg");
					give[13] = take[13];
					//torch
					item[14] = "Torch";
					fish[14] = Object.Instantiate<Mesh>(fish1.fish2(asset.LoadAsset<GameObject>("torch.obj").GetComponentInChildren<MeshFilter>().mesh));
					cats[14] = new Material[] {asset.LoadAsset<Material>("torch_diffuse.mat")};
					take[14] = take[1];
					give[14] = give[1];
					//chest
					item[15] = "Chest";
					chest = asset.LoadAsset<GameObject>("chest.fbx").transform;
					fish[15] = Object.Instantiate<Mesh>(fish1.fish2(chest.GetComponentsInChildren<Transform>()[1].GetComponent<SkinnedMeshRenderer>().sharedMesh));
					cats[15] = new Material[] {asset.LoadAsset<Material>("diffuse.mat")};
					take[15] = asset.LoadAsset<AudioClip>("chest1.ogg");
					give[15] = asset.LoadAsset<AudioClip>("chest2.ogg");
					//pig
					item[16] = "Pig";
					take[16] = asset.LoadAsset<AudioClip>("pig1.ogg");
					give[16] = asset.LoadAsset<AudioClip>("pig2.ogg");
					tree[0] = asset.LoadAsset<GameObject>("pig.prefab").GetComponentsInChildren<Transform>()[2];
					ward[0] = new Vector3[] {new Vector3(-0.4f, 0f, 0f), new Vector3(0f, 270f, 270f), new Vector3(2f, 2f, 2f), new Vector3(-0.03f, 0.148f, -0.034f), new Vector3(314f, 357f, 204f)}; //46 177 336
					//sheep
					item[17] = "Sheep";
					take[17] = asset.LoadAsset<AudioClip>("sheep1.ogg");
					give[17] = asset.LoadAsset<AudioClip>("sheep2.ogg");
					tree[1] = asset.LoadAsset<GameObject>("sheep.prefab").GetComponentsInChildren<Transform>()[2];
					ward[1] = new Vector3[] {new Vector3(-0.85f, 0f, 0f), new Vector3(0f, 180f, 270f), new Vector3(4f, 4f, 4f), new Vector3(-0.12f, 0.11f, -0.014f), new Vector3(314f, 357f, 204f)}; //46 177 336
					//cow
					item[18] = "Cow";
					take[18] = asset.LoadAsset<AudioClip>("cow1.ogg");
					give[18] = asset.LoadAsset<AudioClip>("cow2.ogg");
					tree[2] = asset.LoadAsset<GameObject>("cow.prefab").GetComponentsInChildren<Transform>()[2];
					ward[2] = new Vector3[] {new Vector3(-1.3f, 0f, 0f), new Vector3(0f, 270f, 270f), new Vector3(4f, 4f, 4f), new Vector3(-0.16f, 0.165f, 0.046f), new Vector3(314f, 357f, 204f)}; //46 177 336
					//chicken
					item[19] = "Chicken";
					take[19] = asset.LoadAsset<AudioClip>("chicken1.ogg");
					give[19] = asset.LoadAsset<AudioClip>("chicken2.ogg");
					tree[3] = asset.LoadAsset<GameObject>("chicken.prefab").GetComponentsInChildren<Transform>()[2];
					ward[3] = new Vector3[] {new Vector3(-0.35f, 0f, 0f), new Vector3(0f, 180f, 270f), new Vector3(4f, 4f, 4f), new Vector3(-0.05f, 0.026f, -0.05f), new Vector3(314f, 357f, 204f)}; //46 177 336
					//bee
					item[20] = "Bee";
					take[20] = null;
					give[20] = null;
					tree[4] = asset.LoadAsset<GameObject>("bee.prefab").GetComponentsInChildren<Transform>()[2];
					ward[4] = new Vector3[] {new Vector3(0.8f, 0f, 0.5f), new Vector3(270f, 68f, 0f), new Vector3(50f, 50f, 50f), new Vector3(-0.03f, 0.13f, 0.03f), new Vector3(0f, 112.5f, 117.2f)}; //270 90 338
					set_item_weights();
				}
				if (item[0] != null)
				{
					Shion cr;
					if (GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && __instance.GetComponent<NetworkObject>() != null)
					{
						UInt64 n64;
						if (yon.cfg_saveseeds.Value == true && seeds != "nil" && seeds != "?" && seeds.Contains(__instance.GetComponent<NetworkObject>().NetworkObjectId + ".") == true)
						{
							if (first_item[2] == true) yon.mls.LogInfo("custom_random = new Shion(saved seed)");
							int start = seeds.IndexOf(__instance.GetComponent<NetworkObject>().NetworkObjectId + ".");
							int n = __instance.GetComponent<NetworkObject>().NetworkObjectId.ToString().Length + 1;
							n64 = UInt64.Parse(seeds.Substring(start + n, seeds.IndexOf("/", start) - (start + n)));
						}
						else
						{
							if (first_item[2] == true) yon.mls.LogInfo("custom_random = new Shion(lobbyid+networkobjectid)");
							n64 = lobbyid + __instance.GetComponent<NetworkObject>().NetworkObjectId;
						}
						cr = new Shion(n64);
						if (yon.cfg_saveseeds.Value == true)
						{
							__instance.gameObject.AddComponent<ItemTypeSeed>();
							__instance.gameObject.GetComponent<ItemTypeSeed>().seed = n64.ToString();
						}
					}
					else
					{
						if (first_item[2] == true) yon.mls.LogInfo("custom_random = new Shion()");
						cr = new Shion();
					}
					if (first_item[2] == true) yon.mls.LogInfo(yon.cfg_synced_percentages.Value == true && synced_weights[0] != -1 ? "host config" : "local config");
					first_item[2] = false;
					bool tempb = false;
					if (cr.next32mm(0, 100) < yon.cfg_base_percentage.Value)
					{
						//blocks
						int[] weights = (yon.cfg_synced_percentages.Value == true && synced_weights[0] != -1 ? synced_weights : item_weights);
						int random_number = cr.next32mm(1, weights[0] + 1);
						int index = 0, added = 0;
						for (index = 1; index < weights.Length; index = index + 1)
						{
							added = added + weights[index];
							if (added >= random_number)
							{
								break;
							}
						}
						if (index == 1)
						{
							//diamond_block
							component.mesh = fish[1];
							if (dots[1] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									//x=up-/down+, y=left-/right+, z=foward+/backward-, horizontal glass furthest
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 90f;
									float vx = vertices[n].x;
									float vz = vertices[n].z;
									float nx = normals[n].x;
									float nz = normals[n].z;
									vertices[n].x = -vz;
									vertices[n].z = vx;
									normals[n].x = -nz;
									normals[n].z = nx;
									vertices[n].x = vertices[n].x - 0.5f; //u
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[1] = mesh;
								dots[1] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.5f, 0f, 0f); //(0, 0, 0.97)
							box.size = new Vector3(1.9f, 1.9f, 1.9f); //(0.44, 1.97, 3.79)
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.5f, 0f, 0f); //(0, 0, 1.07)
							__instance.itemProperties.itemName = item[1];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[1];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[1];
							___mainObjectRenderer.materials = cats[1];
						}
						else if (index == 2)
						{
							//spider
							component.mesh = fish[2];
							if (dots[2] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 3f;
									float vx = vertices[n].x; //90cc
									float vz = vertices[n].z;
									float nx = normals[n].x;
									float nz = normals[n].z;
									vertices[n].x = -vz;
									vertices[n].z = vx;
									normals[n].x = -nz;
									normals[n].z = nx;
									float vyy = vertices[n].y; //270cw
									float vzz = vertices[n].z;
									float nyy = normals[n].y;
									float nzz = normals[n].z;
									vertices[n].y = vzz;
									vertices[n].z = -vyy;
									normals[n].y = nzz;
									normals[n].z = -nyy;
									vertices[n].y = vertices[n].y - 0.24f; //l
									vertices[n].x = vertices[n].x + 0.1f; //d
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[2] = mesh;
								dots[2] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(0.05f, 0f, 0f);
							box.size = new Vector3(0.65f, 1.9f, 1.9f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(0.05f, 0f, 0f);
							__instance.itemProperties.itemName = item[2];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[2];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[2];
							___mainObjectRenderer.materials = cats[2];
						}
						else if (index == 3)
						{
							//zombie
							component.mesh = fish[3];
							if (dots[3] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 3f;
									float vx = vertices[n].x;
									float vy = vertices[n].y;
									float nx = normals[n].x;
									float ny = normals[n].y;
									vertices[n].x = vy;
									vertices[n].y = -vx;
									normals[n].x = ny;
									normals[n].y = -nx;
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[3] = mesh;
								dots[3] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(0.05f, 0f, 0f);
							box.size = new Vector3(0.65f, 1.2f, 2.4f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(0.05f, 0f, 0f);
							__instance.itemProperties.itemName = item[3];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[3];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[3];
							___mainObjectRenderer.materials = cats[3];
						}
						else if (index == 4)
						{
							//villager
							component.mesh = fish[4];
							if (dots[4] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 3f;
									float vx = vertices[n].x;
									float vy = vertices[n].y;
									float nx = vertices[n].x;
									float ny = vertices[n].y;
									vertices[n].x = vy;
									vertices[n].y = -vx;
									normals[n].x = ny;
									normals[n].y = -nx;
									vertices[n].z = vertices[n].z + 0.4f; //f
									vertices[n].y = vertices[n].y + 0.05f; //r
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[4] = mesh;
								dots[4] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(0.05f, 0f, 0.1f);
							box.size = new Vector3(0.65f, 1.2f, 2.6f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(0.05f, 0f, 0f);
							__instance.itemProperties.itemName = item[4];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[4];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[4];
							___mainObjectRenderer.materials = cats[4];
						}
						else if (index == 5)
						{
							//creeper
							component.mesh = fish[5];
							if (dots[5] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 3f;
									float vx = vertices[n].x;
									float vy = vertices[n].y;
									float nx = normals[n].x;
									float ny = normals[n].y;
									vertices[n].x = vy;
									vertices[n].y = -vx;
									normals[n].x = ny;
									normals[n].y = -nx;
									vertices[n].z = vertices[n].z + 0.25f; //f
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[5] = mesh;
								dots[5] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(0.05f, 0f, 0.25f);
							box.size = new Vector3(0.65f, 0.6f, 1.9f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(0.05f, 0f, 0.25f);
							__instance.itemProperties.itemName = item[5];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[5];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[5];
							___mainObjectRenderer.materials = cats[5];
						}
						else if (index == 6)
						{
							//pumpkin
							component.mesh = fish[6];
							if (dots[6] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 1.11f;
									float vx = vertices[n].x;
									float vz = vertices[n].z;
									float nx = normals[n].x;
									float nz = normals[n].z;
									vertices[n].x = -vz;
									vertices[n].z = vx;
									normals[n].x = -nz;
									normals[n].z = nx;
									float vxx = vertices[n].x;
									float vyy = vertices[n].y;
									float nxx = normals[n].x;
									float nyy = normals[n].y;
									vertices[n].x = -vyy;
									vertices[n].y = vxx;
									normals[n].x = -nyy;
									normals[n].y = nxx;
									float vyyy = vertices[n].y;
									float vzzz = vertices[n].z;
									float nyyy = normals[n].y;
									float nzzz = normals[n].z;
									vertices[n].y = vzzz;
									vertices[n].z = -vyyy;
									normals[n].y = nzzz;
									normals[n].z = -nyyy;
									vertices[n].y = vertices[n].y - 0.8f; //l
									vertices[n].x = vertices[n].x - 0.5f; //u
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[6] = mesh;
								dots[6] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.5f, 0f, 0f);
							box.size = new Vector3(1.9f, 1.9f, 1.9f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.5f, 0f, 0f);
							__instance.itemProperties.itemName = item[6];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[6];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[6];
							___mainObjectRenderer.materials = cats[6];
						}
						else if (index == 7)
						{
							//anvil
							component.mesh = fish[7];
							if (dots[7] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 3.5f;
									float vx = vertices[n].x;
									float vz = vertices[n].z;
									float nx = normals[n].x;
									float nz = normals[n].z;
									vertices[n].x = -vz;
									vertices[n].z = vx;
									normals[n].x = -nz;
									normals[n].z = nx;
									float vxx = vertices[n].x;
									float vyy = vertices[n].y;
									float nxx = normals[n].x;
									float nyy = normals[n].y;
									vertices[n].x = -vyy;
									vertices[n].y = vxx;
									normals[n].x = -nyy;
									normals[n].y = nxx;
									float vyyy = vertices[n].y;
									float vzzz = vertices[n].z;
									float nyyy = normals[n].y;
									float nzzz = normals[n].z;
									vertices[n].y = vzzz;
									vertices[n].z = -vyyy;
									normals[n].y = nzzz;
									normals[n].z = -nyyy;
									vertices[n].y = vertices[n].y - 0.25f; //l
									vertices[n].x = vertices[n].x - 0.25f; //u
									vertices[n].z = vertices[n].z - 0.25f; //b
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[7] = mesh;
								dots[7] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.5f, 0f, 0f);
							box.size = new Vector3(1.9f, 1.9f, 1.425f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.5f, 0f, 0f);
							__instance.itemProperties.itemName = item[7];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[7];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[7];
							___mainObjectRenderer.materials = cats[7];
						}
						else if (index == 8)
						{
							//steve
							component.mesh = fish[8];
							if (dots[8] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 3f;
									float vx = vertices[n].x;
									float vy = vertices[n].y;
									float nx = normals[n].x;
									float ny = normals[n].y;
									vertices[n].x = vy;
									vertices[n].y = -vx;
									normals[n].x = ny;
									normals[n].y = -nx;
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[8] = mesh;
								dots[8] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(0.05f, 0f, 0f);
							box.size = new Vector3(0.65f, 1.2f, 2.4f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(0.05f, 0f, 0f);
							__instance.itemProperties.itemName = item[8];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[8];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[8];
							___mainObjectRenderer.materials = cats[8];
						}
						else if (index == 9)
						{
							//tnt
							component.mesh = fish[9];
							if (dots[9] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = (vertices[n] * 0.005f) * 1.044f;
									float vx = vertices[n].x;
									float vy = vertices[n].y;
									float nx = normals[n].x;
									float ny = normals[n].y;
									vertices[n].x = -vy;
									vertices[n].y = vx;
									normals[n].x = -ny;
									normals[n].y = nx;
									vertices[n].x = vertices[n].x - 0.5f; //u
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[9] = mesh;
								dots[9] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.5f, 0f, 0f);
							box.size = new Vector3(1.9f, 1.9f, 1.9f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.5f, 0f, 0f);
							__instance.itemProperties.itemName = item[9];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[9];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[9];
							___mainObjectRenderer.materials = cats[9];
						}
						else if (index == 10)
						{
							//diamond
							component.mesh = fish[10];
							if (dots[10] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								int[] triangles = mesh.triangles;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 0.75f;
									float vy = vertices[n].y;
									float vz = vertices[n].z;
									float ny = normals[n].y;
									float nz = normals[n].z;
									vertices[n].y = -vy;
									vertices[n].z = -vz;
									normals[n].y = -ny;
									normals[n].z = -nz;
									float vxx = vertices[n].x;
									float vyy = vertices[n].y;
									float nxx = normals[n].x;
									float nyy = normals[n].y;
									vertices[n].x = -vxx;
									vertices[n].y = -vyy;
									normals[n].x = -nxx;
									normals[n].y = -nyy;
									vertices[n].x = vertices[n].x * 0.08f;
									vertices[n].x = vertices[n].x - 0.1f; //u
									vertices[n].z = vertices[n].z + 0.3f; //f
									vertices[n].y = vertices[n].y + 0.1f; //r
									vertices[n] = vertices[n] + center;
								}
								for (int n = 0; n < triangles.Length; n = n + 3)
								{
									if (vertices[triangles[n]].x > -0.1f && vertices[triangles[n + 1]].x > -0.1f && vertices[triangles[n + 2]].x > -0.1f)
									{
										int temp = triangles[n];
										triangles[n] = triangles[n + 2];
										triangles[n + 2] = temp;
									}
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.triangles = triangles;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[10] = mesh;
								dots[10] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.1f, 0f, 0.3f);
							box.size = new Vector3(0.1f, 1.4f, 1.5f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.1f, 0f, 0.3f);
							__instance.itemProperties.itemName = item[10];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[10];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[10];
							___mainObjectRenderer.materials = cats[10];
						}
						else if (index == 11)
						{
							//diamond_sword
							component.mesh = fish[11];
							if (dots[11] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 0.06f;
									float vy = vertices[n].y;
									float vz = vertices[n].z;
									float ny = normals[n].y;
									float nz = normals[n].z;
									vertices[n].y = -vy;
									vertices[n].z = -vz;
									normals[n].y = -ny;
									normals[n].z = -nz;
									float vxx = vertices[n].x;
									float vyy = vertices[n].y;
									float nxx = normals[n].x;
									float nyy = normals[n].y;
									vertices[n].x = vyy;
									vertices[n].y = -vxx;
									normals[n].x = nyy;
									normals[n].y = -nxx;
									float vyyy = vertices[n].y;
									float vzzz = vertices[n].z;
									float nyyy = normals[n].y;
									float nzzz = normals[n].z;
									vertices[n].y = (float)(vyyy * System.Math.Cos(5.497787) - vzzz * System.Math.Sin(5.497787));
									vertices[n].z = (float)(vyyy * System.Math.Sin(5.497787) + vzzz * System.Math.Cos(5.497787));
									normals[n].y = (float)(nyyy * System.Math.Cos(5.497787) - nzzz * System.Math.Sin(5.497787));
									normals[n].z = (float)(nyyy * System.Math.Sin(5.497787) + nzzz * System.Math.Cos(5.497787));
									vertices[n].x = vertices[n].x - 0.1f; //u
									vertices[n].z = vertices[n].z + 1f; //f
									vertices[n].y = vertices[n].y + 0.01f; //r
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[11] = mesh;
								dots[11] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.1f, 0f, 1f);
							box.size = new Vector3(0.1f, 1.3f, 2.7f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.1f, 0f, 1f);
							__instance.itemProperties.itemName = item[11];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[11];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[11];
							___mainObjectRenderer.materials = cats[11];
						}
						else if (index == 12)
						{
							//diamond_pickaxe
							component.mesh = fish[12];
							if (dots[12] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 0.06f;
									float vy = vertices[n].y;
									float vz = vertices[n].z;
									float ny = normals[n].y;
									float nz = normals[n].z;
									vertices[n].y = -vy;
									vertices[n].z = -vz;
									normals[n].y = -ny;
									normals[n].z = -nz;
									float vxx = vertices[n].x;
									float vyy = vertices[n].y;
									float nxx = normals[n].x;
									float nyy = normals[n].y;
									vertices[n].x = vyy;
									vertices[n].y = -vxx;
									normals[n].x = nyy;
									normals[n].y = -nxx;
									float vyyy = vertices[n].y;
									float vzzz = vertices[n].z;
									float nyyy = normals[n].y;
									float nzzz = normals[n].z;
									vertices[n].y = (float)(vyyy * System.Math.Cos(0.7853982) - vzzz * System.Math.Sin(0.7853982));
									vertices[n].z = (float)(vyyy * System.Math.Sin(0.7853982) + vzzz * System.Math.Cos(0.7853982));
									normals[n].y = (float)(nyyy * System.Math.Cos(0.7853982) - nzzz * System.Math.Sin(0.7853982));
									normals[n].z = (float)(nyyy * System.Math.Sin(0.7853982) + nzzz * System.Math.Cos(0.7853982));
									vertices[n].x = vertices[n].x - 0.1f; //u
									vertices[n].z = vertices[n].z + 0.8f; //f
									vertices[n].y = vertices[n].y + 0.01f; //r
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[12] = mesh;
								dots[12] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.1f, 0f, 0.7f);
							box.size = new Vector3(0.1f, 1.5f, 2f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.1f, 0f, 0.7f);
							__instance.itemProperties.itemName = item[12];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[12];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[12];
							___mainObjectRenderer.materials = cats[12];
						}
						else if (index == 13)
						{
							//apple
							component.mesh = fish[13];
							if (dots[13] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								//Vector3[] normals = mesh.normals;
								int[] triangles = mesh.triangles;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 0.93f;
									vertices[n].x = vertices[n].x * 0.08f;
									vertices[n].x = vertices[n].x - 0.1f; //u
									vertices[n].z = vertices[n].z + 0.5f; //f
									vertices[n].y = vertices[n].y + 0.2f; //r
									vertices[n] = vertices[n] + center;
								}
								for (int n = 0; n < triangles.Length; n = n + 3)
								{
									if (vertices[triangles[n]].x > -0.1f && vertices[triangles[n + 1]].x > -0.1f && vertices[triangles[n + 2]].x > -0.1f)
									{
										int temp = triangles[n];
										triangles[n] = triangles[n + 2];
										triangles[n + 2] = temp;
									}
								}
								mesh.vertices = vertices;
								//mesh.normals = normals;
								mesh.triangles = triangles;
								mesh.RecalculateBounds();
								//mesh.RecalculateNormals();
								fish[13] = mesh;
								dots[13] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.1f, -0.05f, 0.4f);
							box.size = new Vector3(0.1f, 1.4f, 1.65f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.1f, -0.05f, 0.4f);
							__instance.itemProperties.itemName = item[13];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[13];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[13];
							___mainObjectRenderer.materials = cats[13];
						}
						else if (index == 14)
						{
							//torch
							component.mesh = fish[14];
							if (dots[14] == false)
							{
								Mesh mesh = component.mesh;
								Vector3[] vertices = mesh.vertices;
								Vector3[] normals = mesh.normals;
								Bounds bounds = mesh.bounds;
								Vector3 center = bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 1f;
									float vy = vertices[n].y;
									float vz = vertices[n].z;
									float ny = normals[n].y;
									float nz = normals[n].z;
									vertices[n].y = -vz;
									vertices[n].z = vy;
									normals[n].y = -nz;
									normals[n].z = ny;
									vertices[n].x = vertices[n].x - 0.1f; //u
									vertices[n].z = vertices[n].z + 0.3f; //f
									vertices[n] = vertices[n] + center;
								}
								mesh.vertices = vertices;
								mesh.normals = normals;
								mesh.RecalculateBounds();
								mesh.RecalculateNormals();
								fish[14] = mesh;
								dots[14] = true;
							}
							BoxCollider box = ___mainObjectRenderer.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.1f, 0f, 0.3f);
							box.size = new Vector3(0.25f, 0.25f, 1.25f);
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.1f, 0f, 0.3f);
							__instance.itemProperties.itemName = item[14];
							___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[14];
							__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[14];
							___mainObjectRenderer.materials = cats[14];
						}
						else if (index == 15)
						{
							//chest
							if (dots[15] == false)
							{
								Vector3[] vertices = fish[15].vertices;
								Vector3[] normals = fish[15].normals;
								Vector3 center = fish[15].bounds.center;
								for (int n = 0; n < vertices.Length; n = n + 1)
								{
									vertices[n] = vertices[n] - center;
									vertices[n] = vertices[n] * 87.5f;
									float vy = vertices[n].y;
									float vz = vertices[n].z;
									float ny = normals[n].y;
									float nz = normals[n].z;
									vertices[n].y = -vy;
									vertices[n].z = -vz;
									normals[n].y = -ny;
									normals[n].z = -nz;
									float vxx = vertices[n].x;
									float vzz = vertices[n].z;
									float nxx = normals[n].x;
									float nzz = normals[n].z;
									vertices[n].x = vzz;
									vertices[n].z = -vxx;
									normals[n].x = nzz;
									normals[n].z = -nxx;
									vertices[n].x = vertices[n].x - 0.4f; //u
									vertices[n] = vertices[n] + center;
								}
								fish[15].vertices = vertices;
								fish[15].normals = normals;
								fish[15].RecalculateBounds();
								fish[15].RecalculateNormals();
								dots[15] = true;
							}
							Object.Instantiate<Transform>(chest).SetParent(__instance.transform);
							Transform tr1 = __instance.GetComponentsInChildren<Transform>()[4];
							Transform tr2 = __instance.GetComponentsInChildren<Transform>()[2];
							tr1.localPosition = new Vector3(0.4f, 0f, 0f);
							tr1.localRotation = new Quaternion(0.7071068f, 0f, -0.7071068f, 0f);
							tr1.localScale = new Vector3(90f, 90f, 90f);
							tr2.localPosition = new Vector3(0f, 0f, 0f);
							tr2.localRotation = new Quaternion(0f, 0f, 0f, 1f);
							tr2.localScale = new Vector3(1f, 1f, 1f);
							__instance.GetComponentsInChildren<Transform>()[3].GetComponent<SkinnedMeshRenderer>().materials = cats[15];
							__instance.GetComponent<MeshFilter>().mesh = null;
							BoxCollider box = __instance.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.4f, -0.01f, -0.01f);
							box.size = new Vector3(1.6f, 1.6f, 1.6f);
							__instance.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.4f, -0.01f, -0.01f);
							__instance.itemProperties.itemName = item[15];
							__instance.GetComponentInChildren<ScanNodeProperties>().headerText = item[15];
						}
						else if (index == 16)
						{
							//pig
							Object.Instantiate<Transform>(tree[0]).SetParent(__instance.transform);
							Transform tr = __instance.GetComponentsInChildren<Transform>()[2];
							tr.localPosition = ward[0][0];
							tr.localEulerAngles = ward[0][1];
							tr.localScale = ward[0][2];
							__instance.GetComponent<MeshFilter>().mesh = null;
							BoxCollider box = __instance.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.4f, 0f, 0f);
							box.size = new Vector3(1.6f, 1f, 2.45f);
							__instance.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.4f, 0f, 0f);
							__instance.itemProperties.itemName = item[16];
							__instance.GetComponentInChildren<ScanNodeProperties>().headerText = item[16];
						}
						else if (index == 17)
						{
							//sheep
							Object.Instantiate<Transform>(tree[1]).SetParent(__instance.transform);
							Transform tr = __instance.GetComponentsInChildren<Transform>()[2];
							tr.localPosition = ward[1][0];
							tr.localEulerAngles = ward[1][1];
							tr.localScale = ward[1][2];
							__instance.GetComponent<MeshFilter>().mesh = null;
							BoxCollider box = __instance.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.7f, 0f, 0f);
							box.size = new Vector3(2.3f, 1.25f, 2.45f);
							__instance.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.85f, 0f, 0f);
							__instance.itemProperties.itemName = item[17];
							__instance.GetComponentInChildren<ScanNodeProperties>().headerText = item[17];
						}
						else if (index == 18)
						{
							//cow
							Object.Instantiate<Transform>(tree[2]).SetParent(__instance.transform);
							Transform tr = __instance.GetComponentsInChildren<Transform>()[2];
							tr.localPosition = ward[2][0];
							tr.localEulerAngles = ward[2][1];
							tr.localScale = ward[2][2];
							__instance.GetComponent<MeshFilter>().mesh = null;
							BoxCollider box = __instance.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.8f, 0f, 0.3f);
							box.size = new Vector3(2.45f, 1.25f, 2.45f);
							__instance.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.8f, 0f, 0.3f);
							__instance.itemProperties.itemName = item[18];
							__instance.GetComponentInChildren<ScanNodeProperties>().headerText = item[18];
						}
						else if (index == 19)
						{
							//chicken
							Object.Instantiate<Transform>(tree[3]).SetParent(__instance.transform);
							Transform tr = __instance.GetComponentsInChildren<Transform>()[2];
							tr.localPosition = ward[3][0];
							tr.localEulerAngles = ward[3][1];
							tr.localScale = ward[3][2];
							__instance.GetComponent<MeshFilter>().mesh = null;
							BoxCollider box = __instance.GetComponent<BoxCollider>();
							box.center = new Vector3(-0.35f, 0f, 0f);
							box.size = new Vector3(1.5f, 0.8f, 1.2f);
							__instance.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(-0.35f, 0f, 0f);
							__instance.itemProperties.itemName = item[19];
							__instance.GetComponentInChildren<ScanNodeProperties>().headerText = item[19];
						}
						else if (index == 20)
						{
							//bee
							Object.Instantiate<Transform>(tree[4]).SetParent(__instance.transform);
							Transform tr = __instance.GetComponentsInChildren<Transform>()[2];
							tr.localPosition = ward[4][0];
							tr.localEulerAngles = ward[4][1];
							tr.localScale = ward[4][2];
							__instance.GetComponent<MeshFilter>().mesh = null;
							BoxCollider box = __instance.GetComponent<BoxCollider>();
							box.center = new Vector3(0.01f, 0f, 0.24f);
							box.size = new Vector3(0.5f, 0.5f, 0.68f);
							__instance.GetComponentInChildren<ScanNodeProperties>().transform.localPosition = new Vector3(0.01f, 0f, 0.24f);
							__instance.itemProperties.itemName = item[20];
							__instance.GetComponentInChildren<ScanNodeProperties>().headerText = item[20];
						}
						else
						{
							yon.mls.LogInfo("somehow outside?");
							tempb = true;
						}
					}
					else
					{
						tempb = true;
					}
					if (tempb == true)
					{
						//glass
						component.mesh = fish[0];
						__instance.itemProperties.itemName = item[0];
						___mainObjectRenderer.GetComponentInChildren<ScanNodeProperties>().headerText = item[0];
						__instance.itemProperties.spawnPrefab.GetComponent<MeshFilter>().mesh = fish[0];
						___mainObjectRenderer.materials = cats[0];
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
			if (disconnected[0] == true) { if (disconnected[1] == false) { yon.mls.LogMessage("disconnected before wait_timer ended"); disconnected[1] = true; } return; }
			if (first_item[1] == true) { yon.mls.LogMessage(seeds == "nil" && client_received == false ? "timer ended before receiving network message (" + n + "/" + yon.cfg_millisecond.Value + ")" : "received network message before timer ended (" + n + "/" + yon.cfg_millisecond.Value + ")"); first_item[1] = false; }
		}
		[HarmonyPatch(typeof(GrabbableObject), "PlayDropSFX"), HarmonyPrefix]
		private static void pre1(ref GrabbableObject __instance)
		{
			if (item[0] != null && __instance != null && __instance.itemProperties.name == "MagnifyingGlass" && __instance.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				for (int n = 0; n < item.Length; n = n + 1)
				{
					if (__instance.GetComponentInChildren<ScanNodeProperties>().headerText == item[n])
					{
						__instance.itemProperties.dropSFX = give[n];
						break;
					}
				}
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
					yield return new CodeInstruction(OpCodes.Call, typeof(minecraft_scraps).GetMethod("grab_item"));
				}
				//yon.mls.LogInfo(l[n].ToString());
			}
		}
		public static void grab_item(GrabbableObject currentlyGrabbingObject)
		{
			if (item[0] != null && currentlyGrabbingObject != null && currentlyGrabbingObject.itemProperties.name == "MagnifyingGlass" && currentlyGrabbingObject.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				for (int n = 0; n < item.Length; n = n + 1)
				{
					if (currentlyGrabbingObject.GetComponentInChildren<ScanNodeProperties>().headerText == item[n])
					{
						currentlyGrabbingObject.itemProperties.itemName = item[n];
						currentlyGrabbingObject.itemProperties.grabSFX = take[n];
						if (GameNetworkManager.Instance.isHostingGame == true)
						{
							int r = (n > 15 && n < 20 ? (n - fish.Length) : 4);
							currentlyGrabbingObject.itemProperties.positionOffset = ward[r][3];
							currentlyGrabbingObject.itemProperties.rotationOffset = ward[r][4];
						}
						break;
					}
				}
			}
		}
		[HarmonyPatch(typeof(GrabbableObject), "GrabItemOnClient"), HarmonyPrefix]
		private static void pre2(GrabbableObject __instance)
		{
			if (item[0] != null && __instance != null && __instance.itemProperties.name == "MagnifyingGlass" && __instance.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				for (int n = 0; n < item.Length; n = n + 1)
				{
					if (__instance.GetComponentInChildren<ScanNodeProperties>().headerText == item[n])
					{
						int r = (n > 15 && n < 20 ? (n - fish.Length) : 4);
						__instance.itemProperties.positionOffset = ward[r][3];
						__instance.itemProperties.rotationOffset = ward[r][4];
					}
				}
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
					yield return new CodeInstruction(OpCodes.Call, typeof(minecraft_scraps).GetMethod("hold_item"));
				}
				//yon.mls.LogInfo(l[n].ToString());
			}
		}
		public static void hold_item(PlayerControllerB player)
		{
			GrabbableObject _item = player.ItemSlots[player.currentItemSlot];
			if (item[0] != null && _item != null && _item.itemProperties.name == "MagnifyingGlass" && _item.GetComponentInChildren<ScanNodeProperties>() != null)
			{
				for (int n = 0; n < item.Length; n = n + 1)
				{
					if (_item.GetComponentInChildren<ScanNodeProperties>().headerText == item[n])
					{
						_item.itemProperties.grabSFX = take[n];
						PlayerControllerB local_player = GameNetworkManager.Instance.localPlayerController;
						if (player == local_player || local_player.ItemSlots[local_player.currentItemSlot] == null || local_player.ItemSlots[local_player.currentItemSlot].itemProperties.name != "MagnifyingGlass" || local_player.isPlayerDead == true)
						{
							_item.itemProperties.itemName = item[n];
							if (_item.isHeld == true || player != local_player)
							{
								int r = (n > 15 && n < 20 ? (n - fish.Length) : 4);
								_item.itemProperties.positionOffset = ward[r][3];
								_item.itemProperties.rotationOffset = ward[r][4];
							}
						}
						break;
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
					yield return new CodeInstruction(OpCodes.Call, typeof(minecraft_scraps).GetMethod("display_items"));
				}
				yield return l[n];
				//yon.mls.LogInfo(l[n].ToString());
			}
		}
		public static void display_items(HUDManager instance, GameObject displayingObject)
		{
			if (item[0] != null && instance.itemsToBeDisplayed[0] != null && instance.itemsToBeDisplayed[0].itemProperties.name == "MagnifyingGlass" && displayingObject.name == "MagnifyingGlass(Clone)" && instance.itemsToBeDisplayed[0].GetComponentInChildren<ScanNodeProperties>() != null)
			{
				for (int n = 0; n < item.Length; n = n + 1)
				{
					if (instance.itemsToBeDisplayed[0].GetComponentInChildren<ScanNodeProperties>().headerText == item[n])
					{
						instance.itemsToBeDisplayed[0].itemProperties.itemName = item[n];
						if (n < fish.Length)
						{
							displayingObject.GetComponent<MeshFilter>().mesh = fish[n];
						}
						else if ((n - fish.Length) < tree.Length)
						{
							int r = n - fish.Length;
							Object.Instantiate<Transform>(tree[r]).SetParent(displayingObject.transform);
							Transform tr = displayingObject.GetComponentsInChildren<Transform>(true)[2];
							tr.localPosition = ward[r][0];
							tr.localEulerAngles = ward[r][1];
							tr.localScale = ward[r][2];
							displayingObject.GetComponent<MeshFilter>().mesh = null;
						}
						break;
					}
				}
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "Awake"), HarmonyPostfix]
		private static void pst2()
		{
			if (GameNetworkManager.Instance.disableSteam == false)
			{
				disconnected = new bool[] {false, false};
				synced_weights = new int[] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1};
				if (GameNetworkManager.Instance.currentLobby.HasValue == true)
				{
					lobbyid = (GameNetworkManager.Instance.currentLobby.Value.Id % 1000000000);
					yon.mls.LogInfo(lobbyid);
				}
				else
				{
					yon.mls.LogError("current lobby id is null");
				}
			}
		}

//		// item weights //
		private static void set_item_weights()
		{
			item_weights[0] = 0; //total
			item_weights[1] = yon.cfg_diamond_block.Value;
			item_weights[2] = yon.cfg_spider.Value;
			item_weights[3] = yon.cfg_zombie.Value;
			item_weights[4] = yon.cfg_villager.Value;
			item_weights[5] = yon.cfg_creeper.Value;
			item_weights[6] = yon.cfg_pumpkin.Value;
			item_weights[7] = yon.cfg_anvil.Value;
			item_weights[8] = yon.cfg_steve.Value;
			item_weights[9] = yon.cfg_tnt.Value;
			item_weights[10] = yon.cfg_diamond.Value;
			item_weights[11] = yon.cfg_diamond_sword.Value;
			item_weights[12] = yon.cfg_diamond_pickaxe.Value;
			item_weights[13] = yon.cfg_apple.Value;
			item_weights[14] = yon.cfg_torch.Value;
			item_weights[15] = yon.cfg_chest.Value;
			item_weights[16] = yon.cfg_pig.Value;
			item_weights[17] = yon.cfg_sheep.Value;
			item_weights[18] = yon.cfg_cow.Value;
			item_weights[19] = yon.cfg_chicken.Value;
			item_weights[20] = yon.cfg_bee.Value;
			for (int n = 1; n < item_weights.Length; n = n + 1)
			{
				if (item_weights[n] < 0) item_weights[n] = 0;
				item_weights[0] = item_weights[0] + item_weights[n];
			}
		}

//		// network syncing //
		private static bool sync = false;

		private static string seeds = "nil";

		private static bool[] disconnected = new bool[] {false, false};

		private static int[] synced_weights = new int[] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1};

		private static bool client_received = false;

		[HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject"), HarmonyPostfix]
		private static void pst3()
		{
			if (sync == false)
			{
				if (NetworkManager.Singleton.IsHost == true)
				{
					NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("4902.Minecraft_Scraps-Host", host_receive);
				}
				else
				{
					yon.mls.LogInfo("requesting message from host");
					NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("4902.Minecraft_Scraps-Client", client_receive);
					FastBufferWriter w = new FastBufferWriter(0, Unity.Collections.Allocator.Temp);
					NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Minecraft_Scraps-Host", NetworkManager.ServerClientId, w);
					w.Dispose();
				}
				sync = true;
			}
		}
		private static void host_receive(ulong id, FastBufferReader r)
		{
			if (NetworkManager.Singleton.IsHost == true)
			{
				yon.mls.LogInfo("received request from client");
				if (item_weights[0] == 0) set_item_weights();
				string message = string.Join(",", item_weights) + "^" + lobbyid + "^" + seeds;
				yon.mls.LogInfo("sending message " + message);
				FastBufferWriter w = new FastBufferWriter(FastBufferWriter.GetWriteSize(message), Unity.Collections.Allocator.Temp);
				w.WriteValueSafe(message);
				NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Minecraft_Scraps-Client", id, w, NetworkDelivery.ReliableFragmentedSequenced);
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
				yon.mls.LogInfo("client received message " + message);
				int n = 2;
				if ((message.Length - message.Replace("^", "").Length) == n)
				{
					string[] s = message.Split(new char[]{'^'}, n + 1);
					synced_weights = System.Array.ConvertAll(s[0].Split(new char[]{','}), System.Convert.ToInt32);
					lobbyid = UInt64.Parse(s[1]);
					seeds = s[2];
				}
				else
				{
					yon.mls.LogError("received message was not what was expected. wasn't able to sync variables with host. (are the mod versions not the same?)");
					yon.mls.LogError("found " + (message.Length - message.Replace("^", "").Length) + "/" + n + " ^ in message " + message);
				}
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "Disconnect"), HarmonyPrefix]
		private static void pre3()
		{
			disconnected[0] = true;
			if (StartOfRound.Instance != null && NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
			{
				try { NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("4902.Minecraft_Scraps-Host"); NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("4902.Minecraft_Scraps-Client"); } catch (System.Exception error) { yon.mls.LogError(error); }
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "Disconnect"), HarmonyPostfix]
		private static void pst4()
		{
			sync = false;
			seeds = "nil";
			lobbyid = 0uL;
			first_item = new bool[] {true, true, true};
			saved_glass = "";
			loaded_glass = new List<string>();
			synced_weights = new int[] {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1};
			client_received = false;
		}

//		// saving/loading //
		private static string saved_glass = "";

		private static List<string> loaded_glass = new List<string>();

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
					yield return new CodeInstruction(OpCodes.Call, typeof(minecraft_scraps).GetMethod("save_glass"));
				}
				//yon.mls.LogInfo(l[n].ToString());
			}
		}
		public static void save_glass(GrabbableObject[] _items, int index)
		{
			if (yon.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && _items[index].itemProperties.name == "MagnifyingGlass")
			{
				if (_items[index].GetComponent<ItemTypeSeed>() != null)
				{
					ItemTypeSeed component = _items[index].GetComponent<ItemTypeSeed>();
					saved_glass = saved_glass + component.seed + "/";
				}
				else if (_items[index].GetComponent<NetworkObject>() != null)
				{
					saved_glass = saved_glass + (lobbyid + _items[index].GetComponent<NetworkObject>().NetworkObjectId).ToString() + "/";
					yon.mls.LogInfo("ItemTypeSeed is null! saving seed for this item as lobbyid+networkobjectid");
				}
				else
				{
					saved_glass = saved_glass + new Shion().next32mm(1, 101).ToString() + "/";
					yon.mls.LogInfo("ItemTypeSeed and NetworkObject are null! saving seed for this item as a random number from 1 to 100");
				}
			}
		}
		[HarmonyPatch(typeof(GameNetworkManager), "SaveGame"), HarmonyPostfix]
		private static void pst5()
		{
			if (yon.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && StartOfRound.Instance.inShipPhase == true && StartOfRound.Instance.isChallengeFile == false)
			{
				try
				{
					if (saved_glass == "") saved_glass = "nil";
					if (saved_glass.EndsWith("/") == true) saved_glass = saved_glass.Substring(0, saved_glass.Length - 1);
					yon.mls.LogInfo("saving " + saved_glass);
					ES3.Save("4902.Minecraft_Scraps-1", saved_glass, GameNetworkManager.Instance.currentSaveFileName);
					saved_glass = "";
				}
				catch (System.Exception error)
				{
					yon.mls.LogError("Error saving item types: " + error);
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
					yield return new CodeInstruction(OpCodes.Call, typeof(minecraft_scraps).GetMethod("load_glass"));
				}
				//yon.mls.LogInfo(l[n].ToString());
			}
		}
		public static void load_glass(GrabbableObject _item)
		{
			if (yon.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false && lobbyid != 0uL && _item.itemProperties.name == "MagnifyingGlass")
			{
				if (_item.GetComponent<NetworkObject>() != null)
				{
					loaded_glass.Add(_item.GetComponent<NetworkObject>().NetworkObjectId.ToString());
				}
				else
				{
					loaded_glass.Add("nil");
					yon.mls.LogInfo("NetworkObject is null! the seed can't be loaded for this item");
				}
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "Start"), HarmonyPostfix]
		private static void pst6()
		{
			if (yon.cfg_saveseeds.Value == true && GameNetworkManager.Instance.isHostingGame == true && GameNetworkManager.Instance.disableSteam == false)
			{
				try
				{
					string temp = ES3.Load("4902.Minecraft_Scraps-1", GameNetworkManager.Instance.currentSaveFileName, "nil");
					yon.mls.LogInfo("loaded " + temp);
					string[] s = temp.Split(new char[]{'/'});
					if (s[0] != "nil" && s[0] != "" && s.Length == loaded_glass.Count)
					{
						seeds = "";
						for (int n = 0; n < loaded_glass.Count; n = n + 1)
						{
							seeds = seeds + loaded_glass[n] + "." + s[n] + "/";
						}
						yon.mls.LogInfo("current networkobjectids + saved seeds " + seeds);
					}
					loaded_glass = new List<string>();
				}
				catch (System.Exception error)
				{
					yon.mls.LogError("Error loading item types: " + error);
				}
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
		public string guid = yon.harmony.Id;
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