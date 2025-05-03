using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Unity.Netcode;

namespace cake
{
	[BepInPlugin("4902.Vanilla_Belt_Bag", "Vanilla_Belt_Bag", "1.0.0")]
	public class v : BaseUnityPlugin
	{
		public static readonly Harmony harmony = new Harmony("4902.Vanilla_Belt_Bag");

		public static ManualLogSource mls;

		public static ConfigEntry<int> cfg_display;
		public static ConfigEntry<float> cfg_delay;
		public static ConfigEntry<string> cfg_text1;
		public static ConfigEntry<string> cfg_text2;
		//public static ConfigEntry<bool> cfg_remove;

		private void Awake()
		{
			cfg_display = Config.Bind("Vanilla", "display", 0, "[Display]\ndetermines what text display tips are shown.\n0 = both disabled.\n1 = display text 1 only.\n2 = display text 2 only.\n3 = both enabled.");
			cfg_delay = Config.Bind("Vanilla", "seconds", 3f, "[Display cooldown]\ncooldown in seconds for the display text being displayed.");
			cfg_text1 = Config.Bind("Vanilla", "text1", "they tried to put scrap!!!", "[Display text 1]\ndisplayed when scrap is prevented from being collected with a belt bag. scrap being collected with a belt bag is only prevented while you're the host.");
			cfg_text2 = Config.Bind("Vanilla", "text2", "they put scrap!!!", "[Display text 2]\ndisplayed when scrap is collected with a belt bag, regardless of if you're host or client.");
			//cfg_remove = Config.Bind("Vanilla", "remove", false, "[Remove collected scrap]\ntakes scrap out of the belt bag 1 second after being collected, regardless of if you're host or client.\n(this config may not work if the host has mods that prevent interacting with the belt bag if you're dead or too far away etc).");

			mls = BepInEx.Logging.Logger.CreateLogSource("Vanilla");
			mls.LogInfo("Vanilla belt bag loaded!");
			harmony.PatchAll(typeof(cake.vanilla));
		}
	}
	public class vanilla
	{
		[HarmonyPatch(typeof(BeltBagItem), "TryAddObjectToBagServerRpc"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn1(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if (l[n].ToString() == "call bool Unity.Netcode.NetworkObjectReference::TryGet(Unity.Netcode.NetworkObject& networkObject, Unity.Netcode.NetworkManager networkManager)")
				{
					yield return new CodeInstruction(OpCodes.Ldloc_0);
					yield return new CodeInstruction(OpCodes.Ldarg_2);
					yield return new CodeInstruction(OpCodes.Call, typeof(cake.vanilla).GetMethod("return_bool"));
				}
				//v.mls.LogInfo(l[n].ToString());
			}
		}
		public static bool return_bool(bool r, NetworkObject no, int player)
		{
			if (no != null)
			{
				GrabbableObject go = no.GetComponent<GrabbableObject>();
				if (go != null && go.itemProperties != null && go.itemProperties.isScrap == true)
				{
					if (v.cfg_display.Value == 1 || v.cfg_display.Value == 3) display_warning(true, v.cfg_text1.Value, player);
					return false;
				}
			}
			return r;
		}

		[HarmonyPatch(typeof(BeltBagItem), "TryAddObjectToBagClientRpc"), HarmonyPostfix]
		private static void pst1(ref NetworkObjectReference netObjectRef, ref int playerWhoAdded)
		{
			if (v.cfg_display.Value == 2 || v.cfg_display.Value == 3)
			{
				NetworkObject no;
				if (netObjectRef.TryGet(out no))
				{
					GrabbableObject go = no.GetComponent<GrabbableObject>();
					if (go != null && go.itemProperties != null && go.itemProperties.isScrap == true)
					{
						display_warning(false, v.cfg_text2.Value, playerWhoAdded);
					}
				}
			}
		}

		private static float last = 0f;

		private static void display_warning(bool tried = true, string text = "temp", int player = 0)
		{
			if (Time.realtimeSinceStartup > last)
			{
				last = Time.realtimeSinceStartup + v.cfg_delay.Value;
				if (StartOfRound.Instance != null && StartOfRound.Instance.allPlayerScripts.Length > player && StartOfRound.Instance.allPlayerScripts[player] != null && HUDManager.Instance != null)
				{
					string player_name = StartOfRound.Instance.allPlayerScripts[player].playerUsername;
					v.mls.LogInfo("player " + player_name + (tried == true ? " tried collecting" : " collected") + " scrap!!");
					HUDManager.Instance.DisplayTip(text, player_name, true);
				}
			}
		}

		/*public static float remove_delay = 1f;

		[HarmonyPatch(typeof(BeltBagItem), "PutObjectInBagLocalClient"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn2(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				if (l[n].ToString() == "call System.Collections.IEnumerator BeltBagItem::putObjectInBagAnimation(GrabbableObject gObject)")
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, typeof(cake.vanilla).GetMethod("remove_scrap"));
				}
				else
				{
					yield return l[n];
				}
				//v.mls.LogInfo(l[n].ToString());
			}
		}
		public IEnumerator remove_scrap(GrabbableObject go, BeltBagItem instance)
		{
			if (go == null || instance == null) yield break;
			FieldInfo placing = typeof(BeltBagItem).GetField("placingItemsInBag", BindingFlags.NonPublic | BindingFlags.Instance);
			//putObjectInBagAnimation
			float time = 0f;
			Vector3 startingPosition = go.transform.position;
			go.EnablePhysics(enable: false);
			go.transform.SetParent(null);
			go.startFallingPosition = go.transform.position;
			go.targetFloorPosition = go.transform.position;
			placing.SetValue(instance, (int)placing.GetValue(instance) + 1);
			while (time < 1f)
			{
				if (return_item_index(instance, go) == -1) break;
				time = time + Time.deltaTime * 14f;
				go.targetFloorPosition = Vector3.Lerp(startingPosition, instance.transform.position, time / 1f);
				yield return null;
			}
			if (instance == null) yield break;
			placing.SetValue(instance, (int)placing.GetValue(instance) - 1);
			if (return_item_index(instance, go) != -1)
			{
				go.targetFloorPosition = new Vector3(3000f, -400f, 3000f);
				go.startFallingPosition = new Vector3(3000f, -400f, 3000f);
			}
			if (v.cfg_remove.Value == true && go != null && go.itemProperties != null && go.itemProperties.isScrap == true)
			{
				yield return new WaitForSeconds(remove_delay);
				int num = return_item_index(instance, go);
				if (num != -1) //instance.insideAnotherBeltBag == null)
				{
					instance.RemoveObjectFromBag(num);
					v.mls.LogInfo("removed collected scrap" + (instance.playerHeldBy != null ? " (playerHeldBy " + instance.playerHeldBy.playerUsername + ")" : ""));
				}
			}
		}
		private static int return_item_index(BeltBagItem instance, GrabbableObject go)
		{
			if (go != null && instance != null)
			{
				int num = instance.objectsInBag.FindIndex(_ => _ == go);
				if (num != -1 && instance.objectsInBag.Count > num && instance.objectsInBag[num] != null)
				{
					return num;
				}
			}
			return -1;
		}*/

		[HarmonyPatch(typeof(BeltBagItem), "TryAddObjectToBag"), HarmonyPrefix]
		private static bool pre1(ref GrabbableObject gObject)
		{
			if (gObject != null && gObject.itemProperties != null && gObject.itemProperties.isScrap == true) return false;
			return true;
		}
	}
}