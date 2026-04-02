using UInt32 = System.UInt32;
using UInt64 = System.UInt64;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;

namespace random_moons
{
	[BepInPlugin("4902.Random_Moons", "Random_Moons", "1.0.0")]
	public class c : BaseUnityPlugin
	{
		private readonly Harmony harmony = new Harmony("4902.Random_Moons");

		public static ManualLogSource mls;

		public static ConfigEntry<bool>   temp_ltn;
		public static ConfigEntry<bool>   temp_ltw;
		public static ConfigEntry<bool>   temp_wet;
		public static ConfigEntry<bool>   temp_rld;
		public static ConfigEntry<bool>   temp_psm;
		public static ConfigEntry<bool>   temp_mdn;
		public static ConfigEntry<bool>   temp_slm;
		public static ConfigEntry<bool>   temp_mcc;
		public static ConfigEntry<bool>   temp_mcm;
		public static ConfigEntry<bool>   temp_cmm;
		public static ConfigEntry<int>    temp_dsc;
		public static ConfigEntry<int>    temp_fl1;
		public static ConfigEntry<string> temp_fl2;
		public static ConfigEntry<bool>   temp_key;
		public static ConfigEntry<int>    temp_kcd;
		public static ConfigEntry<int>    temp_kcr;
		public static ConfigEntry<int>    temp_inf;
		public static ConfigEntry<bool>   temp_vcs;
		public static ConfigEntry<bool>   temp_htr;
		public static ConfigEntry<bool>   temp_rms;
		public static ConfigEntry<bool>   temp_pri;
		public static ConfigEntry<bool>   temp_tst;

		public static cfg_bool   cfg_ltn = new cfg_bool();   //false print name <= 1
		public static cfg_bool   cfg_ltw = new cfg_bool();   //false print weather <= 1
		public static cfg_bool   cfg_wet = new cfg_bool();   //true  print weather at all
		public static cfg_bool   cfg_rld = new cfg_bool();   //true  random level description
		public static cfg_bool   cfg_psm = new cfg_bool();   //true  prevent same moon
		public static cfg_bool   cfg_mdn = new cfg_bool();   //true  moon default name
		public static cfg_bool   cfg_slm = new cfg_bool();   //true  save/load moon
		public static cfg_bool   cfg_mcc = new cfg_bool();   //true  moon color chat
		public static cfg_bool   cfg_mcm = new cfg_bool();   //true  moon color monitor
		public static cfg_bool   cfg_cmm = new cfg_bool();   //true  challenge moon modifiers
		public static cfg_int    cfg_dsc = new cfg_int();    //diff  different, same, company
		public static cfg_int    cfg_fl1 = new cfg_int();    //off   filter controls
		public static cfg_string cfg_fl2 = new cfg_string(); //stuff filter list
		public static cfg_bool   cfg_key = new cfg_bool();   //true  key (11/493/256/26/350/8/65/14)(moon/name/color/text/cost/seed/rush/enemy) D14 Owu-97 1234 1234 Uwu-24
		public static cfg_int    cfg_kcd = new cfg_int();    //1000  key cost/credit default
		public static cfg_int    cfg_kcr = new cfg_int();    //90    key credit/cost random
		public static cfg_int    cfg_inf = new cfg_int();    //40    infestation percentage
		public static cfg_bool   cfg_vcs = new cfg_bool();   //false vanilla challenge spawning
		public static cfg_bool   cfg_htr = new cfg_bool();   //false hitori
		public static cfg_bool   cfg_rms = new cfg_bool();   //false reoccurring randomMapSeeds
		public static cfg_bool   cfg_pri = new cfg_bool();   //false print to console
		public static cfg_bool   cfg_tst = new cfg_bool();   //false test

		public static bool chosen = false;

		public static bool chosen_moon = false;

		public static int real = -1;

		public static string moon_n = "name";

		public static string moon_c = "ffffff";

		public static string moon_d = "description";

		public static uint key = 0;

		public static string keys = "";

		public static string start_keys = "";

		public static bool hitori = false;

		private void Awake()
		{
			temp_ltn = Config.Bind("1", "-print", false, "[Print moon name]\nprints the moon name if there are <= 1 players"); cfg_ltn.Value = temp_ltn.Value;
			temp_ltw = Config.Bind("1", "--print", false, "[Print moon weather]\nprints the moon weather if there are <= 1 players"); cfg_ltw.Value = temp_ltw.Value;
			temp_wet = Config.Bind("1", "---print", true, "[Print moon weather]\nprints the weather at all"); cfg_wet.Value = temp_wet.Value;
			temp_rld = Config.Bind("1", "random", true, "[Random description]\nshould the description on the monitor display a random levels description.\ntrue, display any levels description, unrelated to the actual orbited level\nfalse, display unknown"); cfg_rld.Value = temp_rld.Value;
			temp_psm = Config.Bind("1", "prevent", true, "[Prevent same moon]\nprevents routing to the same level as the currently orbited level.\nif current challenge moon is Moon-11 and its level is Adamance, rerouting would or would not be able to pick Adamance again"); cfg_psm.Value = temp_psm.Value;
			temp_mdn = Config.Bind("1", "default", true, "[Default moon name]\ndisplay the levels default name instead of its challenge moon name when landing.\ntrue would display 'CELESTIAL BODY: 20 Adamance', false would display 'CELESTIAL BODY: Moon-11'"); cfg_mdn.Value = temp_mdn.Value;
			temp_slm = Config.Bind("1", "save/load", true, "[Save / Load]\nsave and load the current challenge moon being orbited and the previous 10 moons.\ntrue, when rejoining the orbited moon will be the same challenge moon as it was previously\nfalse, if challenge moon was Moon-11 and level was Adamance then when rejoining the orbited moon will be non-challenge Adamance"); cfg_slm.Value = temp_slm.Value;
			temp_mcc = Config.Bind("1", "-color", true, "[Printed moon color]\nshould the moon name printed to chat have color"); cfg_mcc.Value = temp_mcc.Value;
			temp_mcm = Config.Bind("1", "--color", true, "[Monitor moon color]\nshould the moon name on the monitor have color"); cfg_mcm.Value = temp_mcm.Value;
			temp_cmm = Config.Bind("1", "modifiers", true, "[Challenge moon modifiers]\nshould challenge moon modifiers be enabled for challenge moons.\ntype 'randomizer modifiers' in the terminal to toggle this config in-game. it will be resynced to other players the next time the level is changed"); cfg_cmm.Value = temp_cmm.Value;
			temp_dsc = Config.Bind("1", "option", 1, "[Seed / Route options]\n1 = different seed each day (like vanilla gameplay)\n2 = same seed for the same challenge moon (like weekly challenge moons)(regardless of the reoccurring randomMapSeeds config)\n3 = route to company when going to orbit from a challenge moon (to play a different level each day)"); cfg_dsc.Value = temp_dsc.Value;
			temp_fl1 = Config.Bind("2", "filter", 1, "[Filtered moons]\nchanges which moons are routable with the randomizer.\n1 = disabled, all moons are routable\n2 = blacklist, moons in the list are not routable\n3 = whitelist, only moons in the list are routable"); cfg_fl1.Value = temp_fl1.Value;
			temp_fl2 = Config.Bind("2", "list", "[20 Adamance], [68 Artifice], [5 Embrion]", "[Filter list]\nlist of moons that are blacklisted/whitelisted.\ntype 'randomizer info' in the terminal to see all of the moons (including modded)(they will be displayed in the terminal and logged to the console), those are the exact moon names that would be put in this list. the moon names inside the brackets [] are case sensitive so the positions of the characters/numbers and uppercase/lowercase need to be exactly the same.\nGordion and Liquidation are already filtered so those don't need to be added.\nif there are no available moons it will route to gordion"); cfg_fl2.Value = temp_fl2.Value;
			temp_key = Config.Bind("3", "key", true, "[Direct routing]\nwhether direct routing should be enabled/disabled.\ntype 'randomizer key' in the terminal to enter the key/id of a moon to route to that moon. also shows the previous 10 moons with their key/name/cost.\nrouting directly to a moon will cost a random amount of credits based on the key. (default_credits +/- (random_credits * 10)). minimum cost is 100.\nif the number of levels changes (vanilla moon being added/removed or adding/removing a modded moon) then the level that a key routes to will be different than before.\nthe filtered moons/prevent same moon configs don't affect moons that are routed to directly.\nfalse will disable the key terminal command, won't show the key when routing to a random moon, won't save moons to the previous 10 moons list"); cfg_key.Value = temp_key.Value;
			temp_kcd = Config.Bind("3", "default_credits", 1000, "[Default credits]\nthe default starting amount of credits that a moon will cost"); cfg_kcd.Value = temp_kcd.Value;
			temp_kcr = Config.Bind("3", "random_credits", 90, "[Random credits]\nthe maximum amount of credits that the default cost can be raised/lowered by. (40% raise, 60% lower)\n(multiplied by 10, so a config value of 90 would be 900 credits)"); cfg_kcr.Value = temp_kcr.Value;
			temp_inf = Config.Bind("4", "percentage", 40, "[Infestation percentage]\npercentage that an infestation of a random enemy type from the current levels list of spawnable enemies will occur, if challenge moon modifiers are enabled.\nonly the infestation enemy type will be spawned until it reaches its usual spawn cap (for example a jester infestation will spawn 1 jester, then other enemies can be spawned).\nthe actual percentage of an infestation occurring would be lower than this config since an infestation of an enemy type with a max spawn cap of 1 would be the same as having no infestation"); cfg_inf.Value = temp_inf.Value;
			temp_vcs = Config.Bind("4", "modify", true, "[Vanilla challenge spawning]\nwhether this mods changes to the indoor enemy spawning behaviour on challenge moons should also be applied to the vanilla weekly challenge moons"); cfg_vcs.Value = temp_vcs.Value;
			temp_htr = Config.Bind("5", "multiplayer", true, "[Multiplayer]\ndisabling this config will disable printing moon name/weather and auto start if there are > 1 players"); cfg_htr.Value = temp_htr.Value;
			temp_rms = Config.Bind("5", "reoccurring", false, "[Reoccurring randomMapSeeds]\nwhen routing to a new challenge moon in a save the randomMapSeed during the first day on that moon will be determined by the moons key. when rerouting to that challenge moon (with direct routing) the randomMapSeed during the first day on that moon will instead be generated randomly because that moon had already been routed to during the save.\nthis config if enabled will make the randomMapSeed during the first day after routing to a challenge moon always be based on its key. otherwise a key would only determine a randomMapSeed once per save"); cfg_rms.Value = temp_rms.Value;
			temp_pri = Config.Bind("5", "print", false, "[Print to console]\nprint statements in console for debugging"); cfg_pri.Value = temp_pri.Value;
			temp_tst = Config.Bind("5", "test", false, "[test config]\ntest config"); cfg_tst.Value = temp_tst.Value;

			hitori = !cfg_htr.Value;

			mls = BepInEx.Logging.Logger.CreateLogSource("Random Moons");
			harmony.PatchAll();

			}}public class cfg_bool{public bool Value{get;set;
			}}public class cfg_int{public int Value{get;set;
			}}public class cfg_string{public string Value{get;set;
		}
	}
	[HarmonyPatch(typeof(Terminal))]
	internal class rm_t
	{
		public static TerminalNode _node;
		public static TerminalKeyword _word;
		public static TerminalKeyword _modifier;
		public static TerminalKeyword _key;
		public static TerminalNode _key_select;
		public static TerminalNode _key_confirm;
		public static TerminalNode _error;
		public static TerminalNode _cancel;

		public static uint typed_key = 0;

		public static string text3 = (c.cfg_cmm.Value == true ? " challenge " : " ");
		public static string text1 = "Route the autopilot to a random" + text3 + "moon.\n\nPlease CONFIRM or DENY.\n\n";
		public static string text2 = "Randomizer\n----------------------\n\n";

		public static string gold_key;
		public static TerminalKeyword route;

		[HarmonyPatch("Awake"), HarmonyPostfix]
		private static void pst1(Terminal __instance)
		{
print("1.0");		if (_node != null) return;
print("1.1");		gold_key = __instance.terminalNodes.allKeywords.First(_ => _.name == "Moons").specialKeywordResult.displayText;
			__instance.terminalNodes.allKeywords.First(_ => _.name == "Moons").specialKeywordResult.displayText += "* Randomizer   //   Random" + text3 + "moons\n\n";
			route = __instance.terminalNodes.allKeywords.First(_ => _.name == "Route");
			TerminalKeyword info = __instance.terminalNodes.allKeywords.First(_ => _.name == "Info");
			_node = new TerminalNode {
				name = "randomizer_select",
				displayText = text1,
				clearPreviousText = true,
				overrideOptions = true,
				terminalOptions = new CompatibleNoun[] {
					new CompatibleNoun (
						new TerminalKeyword { //noun
							word = "deny",
						},
						new TerminalNode { //result
							displayText = "You have cancelled the order.\n\n",
						}
					),
					new CompatibleNoun (
						new TerminalKeyword { //noun
							word = "confirm",
						},
						new TerminalNode { //result
							name = "randomizer_confirm",
							displayText = "Routing autopilot to [name].\n" + (c.cfg_key.Value == true ? "Key is [key].\n\n" : "\n") + "Good luck.\n\n",
							clearPreviousText = true,
						}
					),
				},
			};
			_word = new TerminalKeyword {
				word = "randomizer",
				specialKeywordResult = _node,
				defaultVerb = route,
			};
			_modifier = new TerminalKeyword {
				word = "modifiers",
				isVerb = true,
				compatibleNouns = new CompatibleNoun[] {
					new CompatibleNoun (
						_word, //noun
						new TerminalNode { //result
							name = "randomizer_modifier",
							displayText = "temp\n\n",
							clearPreviousText = true,
						}
					),
				},
			};
			_key = new TerminalKeyword {
				word = "key",
				isVerb = true,
				compatibleNouns = new CompatibleNoun[] {
					new CompatibleNoun (
						_word, //noun
						new TerminalNode { //result
							name = "randomizer_key_prompt",
							displayText = "Enter an eight digit key.\n\n",
							clearPreviousText = true,
							maxCharactersToType = 9,
							terminalOptions = new CompatibleNoun[] {
								new CompatibleNoun (
									new TerminalKeyword {}, //noun
									new TerminalNode { //result
										name = "randomizer_key_select",
										displayText = "select\n\n",
										clearPreviousText = true,
										overrideOptions = true,
										terminalOptions = new CompatibleNoun[] {
											new CompatibleNoun (
												new TerminalKeyword { //noun
													word = "deny",
												},
												new TerminalNode { //result
													displayText = "You have cancelled the order.\n\n",
												}
											),
											new CompatibleNoun (
												new TerminalKeyword { //noun
													word = "confirm",
												},
												new TerminalNode { //result
													name = "randomizer_key_confirm",
													displayText = "confirm\n\n",
													clearPreviousText = true,
													buyRerouteToMoon = -1,
													itemCost = 0,
												}
											),
										},
									}
								),
							},
							acceptAnything = true,
						}
					),
				},
			};
			_key_select = _key.compatibleNouns[0].result.terminalOptions[0].result;
			_key_confirm = _key_select.terminalOptions[1].result;
			_error = new TerminalNode {
				displayText = "Error\n\n",
				clearPreviousText = true,
				playSyncedClip = 1,
			};
			_cancel = new TerminalNode {
				displayText = "[Cancelled.]\n\n",
				clearPreviousText = true,
			};
			__instance.terminalNodes.allKeywords = __instance.terminalNodes.allKeywords.AddToArray(_word);
			__instance.terminalNodes.allKeywords = __instance.terminalNodes.allKeywords.AddToArray(_modifier);
			if (c.cfg_key.Value == true) __instance.terminalNodes.allKeywords = __instance.terminalNodes.allKeywords.AddToArray(_key);
			route.compatibleNouns = route.compatibleNouns.AddToArray(new CompatibleNoun (_word, _node));
			info.compatibleNouns = info.compatibleNouns.AddToArray(new CompatibleNoun (_word, new TerminalNode {name = "randomizer_info", displayText = text2, clearPreviousText = true}));
		}
		[HarmonyPatch("OnSubmit"), HarmonyPrefix]
		private static void pre1(Terminal __instance)
		{
			if (__instance.terminalInUse == true && __instance.currentNode.name == "randomizer_key_prompt")
			{
print("2.1");			if (__instance.textAdded != 0)
				{
print("2.2");				string hex = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded).ToLower().Replace(" ", "");
					if (hex.Length <= 8 && hex.All("0123456789abcdef".Contains) && hex.Replace("0", "") != "")
					{
print("2.3");					uint k = System.Convert.ToUInt32(hex, 16);
						Shion cr1 = new Shion(k + 350);
						_key_confirm.itemCost = c.cfg_kcd.Value - ((cr1.next32mm(0, c.cfg_kcr.Value + 1) * 10) * (cr1.next32mm(0, 10) < 4 ? -1 : 1));
						if (_key_confirm.itemCost < 100) _key_confirm.itemCost = (c.cfg_tst.Value == false ? 100 : 0);
						rm_gnm.k = k;
						_key_confirm.displayText = GameNetworkManager.Instance.GetNameForWeekNumber();
						_key_select.displayText = (StartOfRound.Instance.connectedPlayersAmount + 1 > 1 ? "! Ship will automatically start !\n\n" : "") + "The cost to route to " + _key_confirm.displayText + " is $" + _key_confirm.itemCost + ".\n\nPlease CONFIRM or DENY.\n\n";
						if (GameNetworkManager.Instance.isHostingGame == true)
						{
print("2.4");						Shion cr2 = new Shion(k + 11);
							int level = 3;
							while (level == 3 || level == 11)
							{
								level = cr2.next32mm(0, StartOfRound.Instance.levels.Length);
							}
							_key_confirm.buyRerouteToMoon = level;
							typed_key = k;
						}
						else
						{
print("2.5");						_key_confirm.buyRerouteToMoon = -1;
							_key_confirm.itemCost = 0;
						}
					}
					else
					{
print("2.6");					_error.displayText = __instance.currentNode.displayText + "[Invalid hexidecimal key.]\nKey characters must be (0123456789ABCDEF), cannot be only 0's, cannot exceed eight digits.\n\n";
						__instance.LoadNewNode(_error);
						__instance.textAdded = 0;
					}
				}
				else
				{
print("2.7");				_cancel.displayText = __instance.currentNode.displayText + "[Cancelled.]\n\n";
					__instance.LoadNewNode(_cancel);
					__instance.textAdded = 0;
				}
			}
		}
		[HarmonyPatch("LoadNewNodeIfAffordable"), HarmonyPrefix]
		private static void pre2(Terminal __instance, ref TerminalNode node, ref int ___groupCredits)
		{
			if (node.name == "randomizer_key_confirm" && GameNetworkManager.Instance.isHostingGame == true && __instance.useCreditsCooldown == false && ___groupCredits >= _key_confirm.itemCost && _key_confirm.buyRerouteToMoon != -1 && node.isConfirmationNode == false)
			{
print("3.1");			StartOfRound start = StartOfRound.Instance;
				if (start != null && start.inShipPhase == true && start.travellingToNewLevel == false && start.isChallengeFile == false && start.levels[_key_confirm.buyRerouteToMoon] != start.currentLevel)
				{
print("3.2");				_key_confirm.displayText = "Routing autopilot to " + _key_confirm.displayText + ".\nYour new balance is $" + (___groupCredits - _key_confirm.itemCost) + ".\n\n" + (new Shion().next32mm(0, 2) == 1 ? "Good luck.\n\n" : "Please enjoy your flight.\n\n");
					c.key = typed_key;
					c.chosen = true;
					rm_gnm.k = c.key;
					string name = GameNetworkManager.Instance.GetNameForWeekNumber();
					if (c.keys.Contains(name) == false)
					{
print("3.3");					string hex = c.key.ToString("X8").Insert(4, " ");
						c.mls.LogMessage("Key " + hex);
						Shion cr = new Shion(c.key + 350);
						int cost = c.cfg_kcd.Value - ((cr.next32mm(0, c.cfg_kcr.Value + 1) * 10) * (cr.next32mm(0, 10) < 4 ? -1 : 1));
						if (cost < 100) cost = (c.cfg_tst.Value == false ? 100 : 0);
						c.keys = c.keys + hex + " " + name.PadRight(10, ' ') + " $" + cost + "\n";
						if ((c.keys.Length - c.keys.Replace("\n", "").Length) > 10) c.keys = c.keys.Substring(c.keys.IndexOf("\n") + 1);
					}
				}
			}
		}
		[HarmonyPatch("LoadNewNode"), HarmonyPrefix]
		private static void pre3(Terminal __instance, ref TerminalNode node)
		{
			if (node.name == "randomizer_select")
			{
print("4.1");			if (StartOfRound.Instance.connectedPlayersAmount + 1 > 1)
				{
print("4.2");				node.displayText = "! Ship will automatically start !\n\n" + text1;
				}
				else
				{
print("4.3");				node.displayText = text1;
				}
			}
			else if (node.name == "randomizer_info")
			{
print("4.4");			string s = "";
				for (int n = 0; n < StartOfRound.Instance.levels.Length; n = n + 1)
				{
					if (s != "") s = s + ", ";
					s = s + "[" + StartOfRound.Instance.levels[n].PlanetName + "]";
				}
				c.mls.LogMessage(s);
				node.displayText = text2 + (c.cfg_key.Value == true ? ">Key\nTo route directly to a moon with an entered key.\n\n" : "") + ">Modifiers\nTo toggle the challenge moon modifiers config.\n\nMoons List: " + s + "\n\n";
			}
			else if (node.name == "randomizer_modifier" && GameNetworkManager.Instance.isHostingGame == true && StartOfRound.Instance.inShipPhase == true)
			{
print("4.5");			c.cfg_cmm.Value = !c.cfg_cmm.Value;
				c.temp_cmm.Value = !c.temp_cmm.Value;
				if (c.cfg_cmm.Value != c.temp_cmm.Value) c.mls.LogError("challenge moon modifiers config is not the same as itself. (" + c.cfg_cmm.Value + " & " + c.temp_cmm.Value + ")");
				text3 = (c.cfg_cmm.Value == true ? " challenge " : " ");
				text1 = "Route the autopilot to a random" + text3 + "moon.\n\nPlease CONFIRM or DENY.\n\n";
				Object.FindAnyObjectByType<Terminal>(FindObjectsInactive.Include).terminalNodes.allKeywords.First(_ => _.name == "Moons").specialKeywordResult.displayText = gold_key + "* Randomizer   //   Random" + text3 + "moons\n\n";
				node.displayText = "Challenge moon modifiers set from " + (!c.cfg_cmm.Value).ToString().ToLower() + " to " + c.cfg_cmm.Value.ToString().ToLower() + ".\n\n";
			}
			else if (node.name == "randomizer_key_prompt")
			{
print("4.6");			if (c.keys != "")
				{
print("4.7");				node.displayText = "Enter an eight digit key. Press enter with nothing typed to cancel.\n\nKeys of the previous 10 moons:\n" + c.keys + "\n";
				}
				else
				{
print("4.8");				node.displayText = "Enter an eight digit key. Press enter with nothing typed to cancel.\n\n";
				}
			}
		}
		[HarmonyPatch("LoadNewNode"), HarmonyPostfix]
		private static void pst2(Terminal __instance, ref TerminalNode node, ref int ___groupCredits)
		{
			if (node.name == "randomizer_confirm")
			{
print("5.1");			if (GameNetworkManager.Instance.isHostingGame == true)
				{
print("5.2");				StartOfRound start = StartOfRound.Instance;
					if (start != null && start.inShipPhase == true && start.travellingToNewLevel == false)
					{
print("5.3");					if (start.isChallengeFile == false)
						{
print("5.4");						c.chosen = true;
							start.ChangeLevelServerRpc(moons.list(c.cfg_psm.Value, false, true), ___groupCredits);
						}
						else
						{
print("5.5");						__instance.LoadNewNode(__instance.terminalNodes.specialNodes[24]);
						}
					}
					else
					{
print("5.6");					__instance.LoadNewNode(__instance.terminalNodes.specialNodes[3]);
					}
				}
				else
				{
print("5.7");				_error.displayText = "[Unable to use randomizer while not host.]\n\n";
					__instance.LoadNewNode(_error);
				}
			}
			else if (node.name == "randomizer_modifier" && GameNetworkManager.Instance.isHostingGame == false)
			{
print("5.8");			_error.displayText = "[Unable to use randomizer while not host.]\n\n";
				__instance.LoadNewNode(_error);
			}
			else if (node.name == "randomizer_modifier" && StartOfRound.Instance.inShipPhase == false)
			{
print("5.9");			_error.displayText = "[Unable to toggle modifiers. The ship must be in orbit. Modifiers are currently " + c.cfg_cmm.Value.ToString().ToLower() + ".]\n\n";
				__instance.LoadNewNode(_error);
			}
			else if (node.name == "randomizer_key_confirm" && GameNetworkManager.Instance.isHostingGame == false)
			{
print("5.10");			_error.displayText = "[Unable to use randomizer while not host.]\n\n";
				__instance.LoadNewNode(_error);
			}
			else if (node.name == "MoonsCatalogue")
			{
				add(__instance);
			}
		}
		private static async void add(Terminal __instance)
		{
print("6.0");		await Task.Delay(1);
			string text = __instance.currentText;
			if (text.Contains("PREVIEW:") == true || text.Contains("\u2554") == true)
			{
print("6.1");			string str = (text.Contains("\u2554") == true ? (text.Contains("PREVIEW:") == true ? "\n\n u---" : " u---") : "* u___");
				//text = text.Insert(Regex.Matches(text, "_\n").Cast<Match>().Select(_ => _.Index).ToList()[0] + 1, "\n\n* Randomizer   //   Random" + text3 + "moons");
				text = text.Insert(Regex.Matches(text, ("\n" + str.Split('u')[1])).Cast<Match>().Select(_ => _.Index).ToList().Last(), str.Split('u')[0] + "Randomizer   //   Random" + text3 + "moons\n");
				__instance.currentText = text;
				__instance.screenText.text = text;
			}
		}
		private static void print(string _) { if (c.cfg_pri.Value == true) c.mls.LogInfo("Terminal:" + _); }
	}
	[HarmonyPatch(typeof(StartOfRound))]
	internal class rm_sor
	{
		[HarmonyPatch("Awake"), HarmonyPostfix]
		private static void pst1()
		{
print("1.0");		rm_gnm.reset_local_variables("StartOfRound.Awake");
		}
		[HarmonyPatch("Start"), HarmonyPostfix]
		private static void pst2(StartOfRound __instance)
		{
print("2.0");		c.chosen = false;
			c.real = (StartOfRound.Instance.isChallengeFile == true ? 1 : 0);
			if (c.cfg_slm.Value == true && c.real != 1 && GameNetworkManager.Instance.isHostingGame == true)
			{
print("2.1");			try
				{
					string save = GameNetworkManager.Instance.currentSaveFileName;
					c.chosen_moon = ES3.Load("4902.Random_Moons-1", save, false);
					c.key = ES3.Load("4902.Random_Moons-5", save, 0u);
					c.keys = ES3.Load("4902.Random_Moons-6", save, "");
					c.start_keys = c.keys;
					if (c.chosen_moon == true)
					{
print("2.2");					c.moon_n = ES3.Load("4902.Random_Moons-2", save, "name");
						c.moon_c = ES3.Load("4902.Random_Moons-3", save, "ffffff");
						c.moon_d = ES3.Load("4902.Random_Moons-4", save, "description");
						__instance.screenLevelDescription.text = moons.create_description(c.moon_n, c.moon_c, c.moon_d, true);
					}
				}
				catch (System.Exception error)
				{
					c.mls.LogError("Error while trying to load game values when connecting as host: " + error);
				}
			}
		}
		[HarmonyPatch("ChangeLevel"), HarmonyPostfix]
		private static void pst3()
		{
print("3.0");		if (c.real == 0)
			{
print("3.1");			rm_pcb.stars = new string[] {"name", "ffffff", "description"};
				rm_pcb.presets = new bool[] {false, false, false};
				if (c.chosen == true)
				{
print("3.2");				c.moon_n = GameNetworkManager.Instance.GetNameForWeekNumber();
					c.moon_c = ColorUtility.ToHtmlStringRGB(moons.color(0f, 1f, 0.8f, 0.8f, 1f, 1f));
					c.moon_d = StartOfRound.Instance.levels[moons.list(false, true)].LevelDescription;
					if (!(c.hitori == true && NetworkManager.Singleton.ConnectedClients.Count > 1) && (c.cfg_ltn.Value == true || StartOfRound.Instance.connectedPlayersAmount + 1 > 1))
					{
print("3.3");					string color = (c.cfg_mcc.Value == true ? c.moon_c : "FFFF00");
						string text = (c.key == rm_t.typed_key ? "" : " random");
						HUDManager.Instance.AddTextToChatOnServer("</color><color=#FF0000>Routing to" + text + rm_t.text3 + "moon:</color>\n<size=14><color=#" + color + ">" + c.moon_n + "</color></size>", -1);
						c.mls.LogMessage("Routing to" + text + rm_t.text3 + "moon " + c.moon_n + "(" + c.moon_c + ")");
					}
					if (StartOfRound.Instance.connectedPlayersAmount + 1 > 1)
					{
print("3.4");					rm_pcb.host_send_all();
					}
				}
				else
				{
print("3.5");				c.key = 0;
				}
				c.chosen_moon = c.chosen;
			}
		}
		private static bool temp1 = false;
		[HarmonyPatch("ArriveAtLevel"), HarmonyPrefix]
		private static void pre1()
		{
print("4.0");		if (c.chosen == true)
			{
print("4.1");			temp1 = true;
			}
			c.chosen = false;
		}
		[HarmonyPatch("ArriveAtLevel"), HarmonyPostfix]
		private static void pst4(StartOfRound __instance)
		{
			arrive(__instance);
		}
		private static async void arrive(StartOfRound __instance)
		{
print("5.0");		if (temp1 == true)
			{
print("5.1");			temp1 = false;
				__instance.screenLevelDescription.text = moons.create_description(c.moon_n, c.moon_c, c.moon_d, true);
				if (!(c.hitori == true && NetworkManager.Singleton.ConnectedClients.Count > 1) && (StartOfRound.Instance.connectedPlayersAmount + 1 > 1))
				{
print("5.2");				StartMatchLever lever = Object.FindAnyObjectByType<StartMatchLever>(FindObjectsInactive.Include);
					await Task.Delay(250);
					lever.PlayLeverPullEffectsServerRpc(true);
					await Task.Delay(1500);
					lever.StartGame();
				}
			}
			else if (rm_pcb.presets[0] == true)
			{
print("5.3");			__instance.screenLevelDescription.text = moons.create_description(rm_pcb.stars[0], rm_pcb.stars[1], rm_pcb.stars[2], true);
				rm_pcb.presets[0] = false;
			}
		}
		[HarmonyPatch("StartGame"), HarmonyPrefix]
		private static void pre2(StartOfRound __instance)
		{
print("6.0");		if (c.chosen_moon == true)
			{
print("6.1");			__instance.overrideRandomSeed = true;
				if (c.key != 0 && (c.cfg_dsc.Value == 2 || c.cfg_rms.Value == true || c.start_keys.Contains(c.key.ToString("X8").Insert(4, " ")) == false))
				{
print("6.2");				__instance.overrideSeedNumber = new Shion(c.key + 8).next32mm(1, 100000000);
				}
				else
				{
print("6.3");				__instance.overrideSeedNumber = new Shion().next32mm(1, 100000000);
				}
			}
		}
		[HarmonyPatch("StartGame"), HarmonyPostfix]
		private static void pst5(StartOfRound __instance)
		{
print("7.0");		if (c.chosen_moon == true)
			{
print("7.1");			__instance.overrideRandomSeed = false;
				if (c.cfg_cmm.Value == true)
				{
print("7.2");				__instance.isChallengeFile = true;
				}
				if (!(c.hitori == true && NetworkManager.Singleton.ConnectedClients.Count > 1) && (c.cfg_wet.Value == true && (c.cfg_ltw.Value == true || StartOfRound.Instance.connectedPlayersAmount + 1 > 1)))
				{
print("7.3");				string weather = ((__instance.currentLevel.currentWeather == LevelWeatherType.None) ? "none" : ("Weather: " + __instance.currentLevel.currentWeather));
					if (weather != "none")
					{
print("7.4");					HUDManager.Instance.AddTextToChatOnServer("</color><color=#C0C0C0>" + weather + "</color>", -1);
						c.mls.LogMessage(weather);
					}
				}
			}
		}
		[HarmonyPatch("ShipLeave"), HarmonyPrefix]
		private static void pre3()
		{
print("8.0");		if (c.cfg_dsc.Value != 2)
			{
print("8.1");			c.key = 0u;
			}
			c.start_keys = c.keys;
			if (c.cfg_cmm.Value == true && GameNetworkManager.Instance.isHostingGame == true && c.real != 1)
			{
print("8.2");			StartOfRound.Instance.isChallengeFile = false;
			}
		}
		[HarmonyPatch("PassTimeToNextDay"), HarmonyPostfix]
		private static void pst6(StartOfRound __instance)
		{
print("9.0");		if (c.chosen_moon == true)
			{
print("9.1");			__instance.screenLevelDescription.text = moons.create_description(c.moon_n, c.moon_c, c.moon_d, true);
			}
			else if (rm_pcb.presets[2] == true)
			{
print("9.2");			__instance.screenLevelDescription.text = moons.create_description(rm_pcb.stars[0], rm_pcb.stars[1], rm_pcb.stars[2], true);
				rm_pcb.presets = new bool[] {false, false, false};
			}
		}
		private static bool temp2 = false;
		[HarmonyPatch("SetShipReadyToLand"), HarmonyPrefix]
		private static void pre4(StartOfRound __instance)
		{
print("10.0");		if (c.cfg_dsc.Value == 3 && c.chosen_moon == true && __instance.travellingToNewLevel == false && c.real != 1 && GameNetworkManager.Instance.isHostingGame == true)
			{
print("10.1");			temp2 = true;
				c.chosen_moon = false;
				__instance.currentLevel = __instance.levels[3];
				__instance.currentLevelID = 3;
				TimeOfDay.Instance.currentLevel = __instance.currentLevel;
				RoundManager.Instance.currentLevel = __instance.currentLevel;
			}
		}
		[HarmonyPatch("SetShipReadyToLand"), HarmonyPostfix]
		private static void pst7(StartOfRound __instance)
		{
print("11.0");		if (c.cfg_dsc.Value == 3 && temp2 == true && __instance.inShipPhase == true && __instance.travellingToNewLevel == false && c.real != 1 && GameNetworkManager.Instance.isHostingGame == true)
			{
print("11.1");			temp2 = false;
				__instance.ChangeLevelServerRpc(3, Object.FindAnyObjectByType<Terminal>(FindObjectsInactive.Include).groupCredits);
			}
		}
		[HarmonyPatch("OnDisable"), HarmonyPrefix]
		private static void pre5()
		{
print("12.0");		rm_gnm.reset_local_variables("StartOfRound.OnDisable");
			if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
			{
print("12.1");			try { NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("4902.Random_Moons-Host"); NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("4902.Random_Moons-Client"); } catch (System.Exception error) { c.mls.LogError(error); }
			}
		}
		private static void print(string _) { if (c.cfg_pri.Value == true) c.mls.LogInfo("StartOfRound:" + _); }
	}
	[HarmonyPatch(typeof(RoundManager))]
	internal class rm_rm
	{
		[HarmonyPatch("SetChallengeFileRandomModifiers")]
		private static void Postfix(RoundManager __instance)
		{
print("1.0");		if (c.cfg_cmm.Value == true && c.chosen_moon == true && c.real != 1 && StartOfRound.Instance.connectedPlayersAmount + 1 > 1 && GameNetworkManager.Instance.isHostingGame == true)
			{
print("1.1");			__instance.increasedMapPropSpawnRateIndex = -1;
			}
		}
		[HarmonyPatch("RefreshEnemiesList"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn4(IEnumerable<CodeInstruction> Instrs)
		{
			List<CodeInstruction> l = Instrs.ToList();
			for (int n = 0; n < l.Count; n = n + 1)
			{
				if (l[n].opcode == OpCodes.Stfld && l[n].operand.ToString() == "System.Int32 enemyRushIndex" && n > 3 && l[n - 3].opcode == OpCodes.Stfld && l[n - 3].operand.ToString() == "System.Single currentMaxOutsidePower")
				{
					l[n - 2].opcode = OpCodes.Nop;
					l[n - 1].opcode = OpCodes.Nop;
					l[n].opcode = OpCodes.Nop;
					break;
				}
			}
			return l;
		}
		[HarmonyPatch("RefreshEnemiesList")]
		private static void Prefix(RoundManager __instance)
		{
print("2.0");		if (StartOfRound.Instance.isChallengeFile == true)
			{
print("2.1");			if (c.real != 1)
				{
print("2.2");				if (c.cfg_cmm.Value == true && c.chosen_moon == true)
					{
print("2.3");					bool temp = (c.key != 0 && (c.cfg_dsc.Value == 2 || c.cfg_rms.Value == true || c.start_keys.Contains(c.key.ToString("X8").Insert(4, " ")) == false));
						Shion cr1 = (temp == true ? new Shion(c.key + 65) : new Shion());
						Shion cr2 = (temp == true ? new Shion(c.key + 14) : new Shion());
						typeof(RoundManager).GetField("enemyRushIndex", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, (cr1.next32mm(0, 100) < c.cfg_inf.Value ? cr2.next32mm(0, __instance.currentLevel.Enemies.Count) : -1));
						try_enabling_fog(__instance);
					}
				}
				else if (c.cfg_vcs.Value == true)
				{
print("2.4");				Shion cr1 = new Shion((UInt64)(StartOfRound.Instance.randomMapSeed + 65));
					Shion cr2 = new Shion((UInt64)(StartOfRound.Instance.randomMapSeed + 14));
					typeof(RoundManager).GetField("enemyRushIndex", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, (cr1.next32mm(0, 100) < c.cfg_inf.Value ? cr2.next32mm(0, __instance.currentLevel.Enemies.Count) : -1));
				}
				else
				{
print("2.5");				typeof(RoundManager).GetField("enemyRushIndex", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, -1);
				}
			}
		}
		private static void try_enabling_fog(RoundManager __instance)
		{
			//default
			System.DateTime date = new System.DateTime(System.DateTime.Now.Year, 10, 23);
			bool num = System.DateTime.Today == date;
			System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 5781);
			if (__instance.indoorFog != null && __instance.indoorFog.gameObject != null)
			{
				if ((!num && random.Next(0, 210) < 3) || random.Next(0, 1000) < 15)
				{
					__instance.indoorFog.gameObject.SetActive(random.Next(0, 100) < 20);
				}
				else
				{
					__instance.indoorFog.gameObject.SetActive(random.Next(0, 150) < 3);
				}
				if (__instance.indoorFog.gameObject.activeSelf == true) c.mls.LogInfo("indoorFog active");
			}
		}
		[HarmonyPatch("PlotOutEnemiesForNextHour"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn2(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				if (n < (l.Count - 1) && l[n + 1].ToString() == "ldfld int RoundManager::enemyRushIndex")
				{
					yield return new CodeInstruction(OpCodes.Ldloc_2);
					yield return new CodeInstruction(OpCodes.Call, typeof(rm_rm).GetMethod("set_spawn_rate_num")); //(should be "num? += 2" inside if enemyRushIndex != -1)
					yield return new CodeInstruction(OpCodes.Stloc_2);
				}
				yield return l[n];
				//yon.mls.LogInfo(l[n].ToString());
			}
		}
		public static int set_spawn_rate_num(int num)
		{
			if ((int)(typeof(RoundManager).GetField("enemyRushIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(RoundManager.Instance)) == -1 && StartOfRound.Instance.isChallengeFile == true)
			{
				if ((c.cfg_cmm.Value == true && c.chosen_moon == true && c.real != 1) || (c.cfg_vcs.Value == true && c.real == 1))
				{
					return num + 2;
				}
			}
			return num;
		}
		[HarmonyPatch("AssignRandomEnemyToVent"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn3(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if (l[n].opcode == OpCodes.Ldstr && l[n].operand.ToString() == "Probability: {0}; enemy type: {1}")
				{
					yield return new CodeInstruction(OpCodes.Ldloc_S, 9);
					yield return new CodeInstruction(OpCodes.Call, typeof(rm_rm).GetMethod("set_spawn_probability_num")); //(should be "SpawnProbabilities.Add(num?)" after debug string)
					yield return new CodeInstruction(OpCodes.Stloc_S, 9);
					yield return new CodeInstruction(OpCodes.Ldstr, "Probability: {0}; enemy type: {1}");
				}
				//yon.mls.LogInfo(l[n].ToString());
			}
		}
		public static int set_spawn_probability_num(string temp, int num)
		{
			if ((int)(typeof(RoundManager).GetField("enemyRushIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(RoundManager.Instance)) == -1 && StartOfRound.Instance.isChallengeFile == true)
			{
				if ((c.cfg_cmm.Value == true && c.chosen_moon == true && c.real != 1) || (c.cfg_vcs.Value == true && c.real == 1))
				{
					return 1;
				}
			}
			return num;
		}
		private static void print(string _) { if (c.cfg_pri.Value == true) c.mls.LogInfo("RoundManager:" + _); }
	}
	[HarmonyPatch(typeof(OutOfBoundsTrigger))]
	internal class rm_ote
	{
		[HarmonyPatch("OnTriggerEnter"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn4(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> list = instructions.ToList();
			for (int n = 0; n < list.Count; n = n + 1)
			{
				if (list[n].opcode == OpCodes.Ldfld && list[n].operand != null && list[n].operand.ToString() == "System.Boolean isChallengeFile")
				{
					list[n - 1].opcode = OpCodes.Nop;
					list[n].opcode = OpCodes.Nop;
					list[n + 1].opcode = OpCodes.Nop;
					break;
				}
			}
			return list;
		}
	}
	[HarmonyPatch(typeof(UnityEngine.Animator))]
	internal class rm_uea
	{
		[HarmonyPatch("SetTrigger", new System.Type[] {typeof(string)})]
		private static void Postfix(ref string name)
		{
			wait(name);
		}
		private static async void wait(string name)
		{
			if (name == "introAnimation" && (c.chosen_moon == true || rm_pcb.presets[1] == true))
			{
print("1.1");			await Task.Delay(10);
				string moon = (c.cfg_mdn.Value == true ? StartOfRound.Instance.currentLevel.PlanetName : (c.chosen_moon == true ? c.moon_n : rm_pcb.stars[0]));
				HUDManager.Instance.planetInfoHeaderText.text = "CELESTIAL BODY: " + moon;
				if (rm_pcb.presets[1] == true)
				{
print("1.2");				rm_pcb.presets = new bool[] {false, false, rm_pcb.presets[2]};
				}
			}
		}
		private static void print(string _) { if (c.cfg_pri.Value == true) c.mls.LogInfo("UnityEngine.Animator:" + _); }
	}
	[HarmonyPatch(typeof(GameNetworkManager))]
	internal class rm_gnm
	{
		public static uint k = 0;

		[HarmonyPatch("GetWeekNumber"), HarmonyPrefix]
		private static bool pre1(ref int __result)
		{
			if (k != 0)
			{
print("1.1");			__result = (new Shion(k + 493).next32mm(0, 100000000) - 51016);
				k = 0;
				return false;
			}
			if (c.chosen == true || c.chosen_moon == true)
			{
print("1.2");			__result = (new Shion(c.key + 493).next32mm(0, 100000000) - 51016);
				return false;
			}
			return true;
		}
		[HarmonyPatch("SaveGame"), HarmonyPostfix]
		private static void pst1(GameNetworkManager __instance)
		{
print("2.0");		if (c.cfg_slm.Value == true && c.real != 1 && StartOfRound.Instance.inShipPhase == true && __instance.isHostingGame == true)
			{
				try
				{
					StartOfRound start = StartOfRound.Instance;
					if (start != null)
					{
print("2.1");					ES3.Save("4902.Random_Moons-1", c.chosen_moon, __instance.currentSaveFileName);
						ES3.Save("4902.Random_Moons-5", c.key, __instance.currentSaveFileName);
						ES3.Save("4902.Random_Moons-6", c.keys, __instance.currentSaveFileName);
						if (c.chosen_moon == true)
						{
print("2.2");						ES3.Save("4902.Random_Moons-2", c.moon_n, __instance.currentSaveFileName);
							ES3.Save("4902.Random_Moons-3", c.moon_c, __instance.currentSaveFileName);
							ES3.Save("4902.Random_Moons-4", c.moon_d, __instance.currentSaveFileName);
						}
					}
				}
				catch (System.Exception error)
				{
					c.mls.LogError("Error while trying to save game values when disconnecting as host: " + error);
				}
			}
		}
		[HarmonyPatch("Disconnect"), HarmonyPostfix]
		private static void pst2()
		{
print("3.0");		reset_local_variables("GameNetworkManager.Disconnect");
		}
		[HarmonyPatch("ResetSavedGameValues"), HarmonyPrefix]
		private static void pre2(GameNetworkManager __instance)
		{
print("4.0");		c.chosen = false;
			c.chosen_moon = false;
			c.key = 0;
			c.keys = "";
			c.start_keys = "";
			rm_t.typed_key = 0;
			rm_pcb.stars = new string[] {"name", "ffffff", "description"};
			rm_pcb.presets = new bool[] {false, false, false};
			if (__instance.isHostingGame == true)
			{
print("4.1");			if (ES3.KeyExists("4902.Random_Moons-1", __instance.currentSaveFileName) == true) ES3.DeleteKey("4902.Random_Moons-1", __instance.currentSaveFileName);
				if (ES3.KeyExists("4902.Random_Moons-2", __instance.currentSaveFileName) == true) ES3.DeleteKey("4902.Random_Moons-2", __instance.currentSaveFileName);
				if (ES3.KeyExists("4902.Random_Moons-3", __instance.currentSaveFileName) == true) ES3.DeleteKey("4902.Random_Moons-3", __instance.currentSaveFileName);
				if (ES3.KeyExists("4902.Random_Moons-4", __instance.currentSaveFileName) == true) ES3.DeleteKey("4902.Random_Moons-4", __instance.currentSaveFileName);
				if (ES3.KeyExists("4902.Random_Moons-5", __instance.currentSaveFileName) == true) ES3.DeleteKey("4902.Random_Moons-5", __instance.currentSaveFileName);
				if (ES3.KeyExists("4902.Random_Moons-6", __instance.currentSaveFileName) == true) ES3.DeleteKey("4902.Random_Moons-6", __instance.currentSaveFileName);
			}
		}
		public static void reset_local_variables(string s)
		{
			c.chosen = false;
			c.chosen_moon = false;
			c.real = -1;
			c.key = 0;
			c.keys = "";
			c.start_keys = "";
			rm_t.typed_key = 0;
			rm_pcb.stars = new string[] {"name", "ffffff", "description"};
			rm_pcb.presets = new bool[] {false, false, false};
			rm_pcb.sync = false;
			c.mls.LogInfo("reset local variables (" + s + ")");
		}
		private static void print(string _) { if (c.cfg_pri.Value == true) c.mls.LogInfo("GameNetworkManager:" + _); }
	}
	[HarmonyPatch(typeof(PlayerControllerB))]
	internal class rm_pcb
	{
		public static string[] stars = {"name", "ffffff", "description"}; //strings>strs>stars

		public static bool[] presets = {false, false, false}; //pre set the stars in the voids

		public static bool sync = false;

		private static bool overwrite = false;

		[HarmonyPatch("ConnectClientToPlayerObject")]
		private static void Postfix()
		{
print("1.0");		if (overwrite == true)
			{
print("1.1");			rm_t.text3 = (c.cfg_cmm.Value == true ? " challenge " : " ");
				rm_t.text1 = "Route the autopilot to a random" + rm_t.text3 + "moon.\n\nPlease CONFIRM or DENY.\n\n";
				Object.FindAnyObjectByType<Terminal>(FindObjectsInactive.Include).terminalNodes.allKeywords.First(_ => _.name == "Moons").specialKeywordResult.displayText = rm_t.gold_key + "* Randomizer   //   Random" + rm_t.text3 + "moons\n\n";
				overwrite = false;
			}
			if (sync == false)
			{
print("1.2");			if (NetworkManager.Singleton.IsHost == true)
				{
print("1.3");				NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("4902.Random_Moons-Host", host_receive);
				}
				else
				{
print("1.4");				c.mls.LogInfo("requesting message from host");
					NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("4902.Random_Moons-Client", client_receive);
					FastBufferWriter w = new FastBufferWriter(0, Unity.Collections.Allocator.Temp);
					NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Random_Moons-Host", NetworkManager.ServerClientId, w);
					w.Dispose();
				}
				sync = true;
			}
		}
		private static void host_receive(ulong id, FastBufferReader r)
		{
print("2.0");		if (NetworkManager.Singleton.IsHost == true)
			{
print("2.1");			try
				{
					c.mls.LogInfo("received request from client");
					string message = (c.chosen_moon == true && StartOfRound.Instance.inShipPhase == true ? "1^" : "0^") + c.cfg_cmm.Value.ToString().ToLower() + "^" + c.moon_n + "^" + c.moon_c + "^" + c.moon_d.Replace("^", "(caret)");
					c.mls.LogInfo("sending message " + message);
					FastBufferWriter w = new FastBufferWriter(FastBufferWriter.GetWriteSize(message), Unity.Collections.Allocator.Temp);
					w.WriteValueSafe(message);
					NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("4902.Random_Moons-Client", id, w);
					w.Dispose();
				}
				catch (System.Exception error)
				{
					c.mls.LogError("Error writing strings while syncing: " + error);
				}
			}
		}
		private static void client_receive(ulong id, FastBufferReader r)
		{
print("3.0");		if (NetworkManager.Singleton.IsClient == true)
			{
print("3.1");			try
				{
					string message;
					r.ReadValueSafe(out message, false);
					c.mls.LogInfo("client received message: " + message);
					int n = 4;
					if ((message.Length - message.Replace("^", "").Length) == n)
					{
print("3.2");					string[] s = message.Split(new char[]{'^'}, n + 1);
						stars = new string[] {s[2], s[3], s[4]};
						if (s[0] == "1")
						{
print("3.3");						if (StartOfRound.Instance.travellingToNewLevel == true)
							{
print("3.4");							presets = new bool[] {true, true, true};
							}
							else
							{
print("3.5");							StartOfRound.Instance.screenLevelDescription.text = moons.create_description(stars[0], stars[1], stars[2], true);
								presets = new bool[] {false, true, true};
							}
						}
						rm_t.text3 = (s[1] == "true" ? " challenge " : " ");
						rm_t.text1 = "Route the autopilot to a random" + rm_t.text3 + "moon.\n\nPlease CONFIRM or DENY.\n\n";
						Object.FindAnyObjectByType<Terminal>(FindObjectsInactive.Include).terminalNodes.allKeywords.First(_ => _.name == "Moons").specialKeywordResult.displayText = rm_t.gold_key + "* Randomizer   //   Random" + rm_t.text3 + "moons\n\n";
						overwrite = true;
					}
					else
					{
print("3.6");					c.mls.LogError("received message was not what was expected. wasn't able to sync variables with host. (are the mod versions not the same?)");
						c.mls.LogError("found " + (message.Length - message.Replace("^", "").Length) + "/" + n + " ^ in message " + message);
					}
				}
				catch (System.Exception error)
				{
					c.mls.LogError("Error reading strings while syncing: " + error);
				}
			}
		}
		public static void host_send_all()
		{
print("4.0");		if (NetworkManager.Singleton.IsHost == true && c.chosen == true)
			{
print("4.1");			try
				{
					string message = "1^" + c.cfg_cmm.Value.ToString().ToLower() + "^" + c.moon_n + "^" + c.moon_c + "^" + c.moon_d;
					c.mls.LogInfo("sending message to all clients " + message);
					FastBufferWriter w = new FastBufferWriter(FastBufferWriter.GetWriteSize(message), Unity.Collections.Allocator.Temp);
					w.WriteValueSafe(message);
					NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("4902.Random_Moons-Client", w);
					w.Dispose();
				}
				catch (System.Exception error)
				{
					c.mls.LogError("Error writing strings while syncing to all clients: " + error);
				}
			}
		}
		private static void print(string _) { if (c.cfg_pri.Value == true) c.mls.LogInfo("PlayerControllerB:" + _); }
	}
	internal class moons
	{
		public static int list(bool compare_previous = false, bool misc = false, bool display_key = false)
		{
			int level = 3;
			uint k = 0;
			Shion cr = new Shion(c.key + 26);
			for (int n = 1; n <= 4000; n = n + 1)
			{
				if (level == 3 || level == 11 || (compare_previous == true && level == StartOfRound.Instance.currentLevelID) || (c.cfg_fl1.Value != 1 && misc == false && c.cfg_fl2.Value.Contains(StartOfRound.Instance.levels[level].PlanetName) == (c.cfg_fl1.Value == 2 ? true : false)))
				{
					if (misc == false)
					{
						k = new Shion().next32mm(min: 1, max: UInt32.MaxValue - 111111111, unsigned: true);
						level = new Shion(k + 11).next32mm(0, StartOfRound.Instance.levels.Length);
					}
					else
					{
						level = cr.next32mm(0, StartOfRound.Instance.levels.Length);
					}
				}
				else
				{
					break;
				}
				if (n == 4000)
				{
					c.mls.LogError("couldn't find a valid level, routing to gordion");
					c.chosen = false;
					level = 3;
					k = 0;
					break;
				}
			}
			if (k != 0 && misc == false) c.key = k;
			if (display_key == true && GameNetworkManager.Instance.isHostingGame == true)
			{
				Terminal terminal = Object.FindAnyObjectByType<Terminal>(FindObjectsInactive.Include);
				rm_gnm.k = c.key;
				string name = GameNetworkManager.Instance.GetNameForWeekNumber();
				string s = terminal.screenText.text.Replace("[name]", name);
				if (c.cfg_key.Value == true)
				{
					string hex = c.key.ToString("X8").Insert(4, " ");
					s = s.Replace("[key]", hex);
					c.mls.LogMessage("Key " + hex);
					if (c.keys.Contains(name) == false)
					{
						Shion cr2 = new Shion(c.key + 350);
						int cost = c.cfg_kcd.Value - ((cr2.next32mm(0, c.cfg_kcr.Value + 1) * 10) * (cr2.next32mm(0, 10) < 4 ? -1 : 1));
						if (cost < 100) cost = (c.cfg_tst.Value == false ? 100 : 0);
						c.keys = c.keys + hex + " " + name.PadRight(10, ' ') + " $" + cost + "\n";
						if ((c.keys.Length - c.keys.Replace("\n", "").Length) > 10) c.keys = c.keys.Substring(c.keys.IndexOf("\n") + 1);
					}
				}
				terminal.currentText = s;
				terminal.screenText.text = s;
			}
			return level;
		}
		public static string create_description(string _n, string _c, string _d, bool disable_video = false)
		{
			_n = (c.cfg_mcm.Value == true ? "Orbiting: </color><color=#" + _c + ">" + _n + "</color>\n" : "Orbiting: " + _n + "\n");
			if (c.cfg_rld.Value == false) _d = "POPULATION: Unknown\nCONDITIONS: Unknown\nFAUNA: Unknown";
			if (disable_video == true)
			{
				StartOfRound.Instance.screenLevelVideoReel.enabled = false;
				StartOfRound.Instance.screenLevelVideoReel.gameObject.SetActive(false);
			}
			return _n + _d;
		}
		public static Color color(float h_min, float h_max, float s_min, float s_max, float v_min, float v_max)
		{
			Shion cr = new Shion(c.key + 256);
			float h = Mathf.Lerp(h_min, h_max, (float)cr.next01());
			float s = Mathf.Lerp(s_min, s_max, (float)cr.next01());
			float v = Mathf.Lerp(v_min, v_max, (float)cr.next01());
			Color r = Color.HSVToRGB(h, s, v, true);
			r.a = 1f;
			return r;
		}
	}
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
