using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Kanata.Patches;
using UnityEngine;
using UnityEngine.Rendering;

namespace Kanata
{
	[BepInPlugin("4902.Katana", "Katana", "1.0.0")]
	public class kyu : BaseUnityPlugin
	{
		private readonly Harmony harmony = new Harmony("4902.Katana");

		public static ManualLogSource mls;

		private void Awake()
		{
			mls = BepInEx.Logging.Logger.CreateLogSource("Katana");
			mls.LogInfo("hei");
			harmony.PatchAll();
		}
	}
}
namespace Kanata.Patches
{
	[HarmonyPatch(typeof(GrabbableObject))]
	internal class go
	{
		private static AudioClip[] audio = new AudioClip[2];

		private static Transform str;

		[HarmonyPatch("Start")]
		private static void Postfix(GrabbableObject __instance)
		{
			if (((Object)__instance.itemProperties).name == "Shovel")
			{
				if (audio[0] == null)
				{
					string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
					string text1 = (path + "/4902-katana").Replace("\\", "/");
					string text2 = path + "\\4902-katana_icon.png";
					kyu.mls.LogMessage("Searching this filepath:" + text1);
					AssetBundle asset = AssetBundle.LoadFromFileAsync(text1).assetBundle;

					__instance.itemProperties.toolTips = new string[] {"Swing katana : [LMB]"};

					Transform tr = Object.Instantiate<Transform>(asset.LoadAsset<Item>("katanaitem.asset").spawnPrefab.GetComponentsInChildren<Transform>()[1]);
					Object.Instantiate<Transform>(tr).SetParent(__instance.transform);
					tr.SetParent(__instance.itemProperties.spawnPrefab.transform);
					Transform temp = __instance.GetComponentsInChildren<Transform>()[2];
					temp.localPosition = tr.localPosition = new Vector3(0f, 0f, -0.6f);
					temp.localRotation = tr.localRotation = new Quaternion(0.7071f, 0f, 0f, 0.7071f);
					temp.localScale = tr.localScale = new Vector3(0.4f, 0.4f, 0.4f);
					str = tr;
					__instance.itemProperties.spawnPrefab.GetComponentsInChildren<Transform>()[1].GetComponent<MeshFilter>().mesh = null;
					__instance.GetComponentsInChildren<Transform>()[1].GetComponent<MeshFilter>().mesh = null;

					Texture2D texture = new Texture2D(2, 2);
					ImageConversion.LoadImage(texture, File.ReadAllBytes(text2));
					__instance.itemProperties.itemIcon = Sprite.Create(texture, __instance.itemProperties.itemIcon.rect, __instance.itemProperties.itemIcon.pivot);

					audio = StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Knife").spawnPrefab.GetComponent<KnifeItem>().hitSFX;

					__instance.itemProperties.positionOffset = new Vector3(-0.19f, 0.03f, -0.44f);
					__instance.itemProperties.rotationOffset = new Vector3(-30f, 200f, -4f);
				}
				else if (__instance.GetComponentsInChildren<Transform>().Length < 12)
				{
					Object.Instantiate<Transform>(str).SetParent(__instance.transform);
					Transform temp = __instance.GetComponentsInChildren<Transform>()[2];
					temp.localPosition = new Vector3(0f, 0f, -0.6f);
					temp.localRotation = new Quaternion(0.7071f, 0f, 0f, 0.7071f);
					temp.localScale = new Vector3(0.4f, 0.4f, 0.4f);
					__instance.GetComponentsInChildren<Transform>()[1].GetComponent<MeshFilter>().mesh = null;
				}
				if (audio[0] != null)
				{
					BoxCollider box = __instance.GetComponent<BoxCollider>();
					box.center = new Vector3(0f, 0f, 0.4f);
					box.size = new Vector3(0.214f, 0.2f, 2f);

					__instance.GetComponent<Shovel>().hitSFX = audio;
				}
			}
		}
	}
	[HarmonyPatch(typeof(StartOfRound))]
	internal class sor
	{
		private static bool temp = false;

		[HarmonyPatch("Awake")]
		private static void Postfix()
		{
			if (temp == true) return; temp = true;
			StartOfRound.Instance.allItemsList.itemsList.First(_ => _.name == "Shovel").spawnPrefab.GetComponent<GrabbableObject>().itemProperties.itemName = "Katana";
		}
	}
	[HarmonyPatch(typeof(Terminal))]
	internal class t
	{
		private static bool temp = false;

		[HarmonyPatch("Awake")]
		private static void Postfix(Terminal __instance)
		{
			if (temp == true) return; temp = true;
			TerminalKeyword w = __instance.terminalNodes.allKeywords.First(_ => _.word == "shovel");
			w.word = "katana";
			TerminalNode n = w.defaultVerb.compatibleNouns.First(_ => _.result.terminalOptions[0].result.name == "buyShovel2").result;
			n.displayText = n.displayText.Replace("shovels", "katanas");
			n.terminalOptions[0].result.displayText = n.terminalOptions[0].result.displayText.Replace("shovels", "katanas");
		}
	}
}