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
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

namespace baldatro
{
	[BepInPlugin("4902.Save_Utils", "Save_Utils", "1.0.0")]
	public class su : BaseUnityPlugin
	{
		public static readonly Harmony harmony = new Harmony("4902.Save_Utils");

		public static ManualLogSource mls;

		public static ConfigEntry<int>    temp1_keybind_mode;
		public static ConfigEntry<string> temp1_keybind_one;
		public static ConfigEntry<string> temp1_keybind_two;
		public static ConfigEntry<bool>   temp1_keybind_prompt;
		public static ConfigEntry<int>    temp2_backup_mode;
		public static ConfigEntry<int>    temp2_backup_number;
		public static ConfigEntry<bool>   temp2_backup_data;
		public static ConfigEntry<bool>   temp2_backup_mod;
		public static ConfigEntry<bool>   temp3_delete_delete;
		public static ConfigEntry<bool>   temp3_disable_splash;
		public static ConfigEntry<int>    temp3_debug;

		public static cfg_int    cfg1_keybind_mode = new cfg_int();    //1
		public static cfg_string cfg1_keybind_one = new cfg_string();  //None,None
		public static cfg_string cfg1_keybind_two = new cfg_string();  //None,None
		public static cfg_bool   cfg1_keybind_prompt = new cfg_bool(); //true
		public static cfg_int    cfg2_backup_mode = new cfg_int();     //3
		public static cfg_int    cfg2_backup_number = new cfg_int();   //3
		public static cfg_bool   cfg2_backup_data = new cfg_bool();    //true
		public static cfg_bool   cfg2_backup_mod = new cfg_bool();     //true
		public static cfg_bool   cfg3_delete_delete = new cfg_bool();  //true
		public static cfg_bool   cfg3_disable_splash = new cfg_bool(); //false
		public static cfg_int    cfg3_debug = new cfg_int();           //4

		private void Awake()
		{
			temp1_keybind_mode   = Config.Bind("1/KEYBINDS", "keybind_mode", 1, "[Keybind mode]\ncontrols what the keybinds do when triggered or disables them.\nSaveGame() gets called by Disconnect() and AutoSaveShipData() even if you're not the host or you disconnected during a round, however in either case not much is actually saved.\n1 = keybind_one will disconnect With SaveGame() being called, keybind_two will disconnect Without SaveGame() being called.\n2 = keybind_one will Enable or Disable (toggle) SaveGame() from being called by Disconnect() (leaving the lobby through a keybind or pause menu) and AutoSaveShipData() (end of round ship auto save), keybind_two will disconnect. the toggle value is displayed when triggering keybind_one, and will only be changed when toggled again or by restarting lethal company.\n3 = disables keybinds."); cfg1_keybind_mode.Value = temp1_keybind_mode.Value;
			temp1_keybind_one    = Config.Bind("1/KEYBINDS", "keybind_one", "None,None", "[Keybind one]\nlist of keyboard keys that need to be pressed/held to trigger the keybind (1-10).\nthe first (left most) key in the list is the only key that needs to be pressed, while all other keys need to be held.\nexample keybinds: \"Enter,K,CapsLock\" \"Enter,W,S,LeftCtrl\" \"Backquote\". list of valid keys: https://web.archive.org/web/20250305185947/https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.Key.html"); cfg1_keybind_one.Value = temp1_keybind_one.Value;
			temp1_keybind_two    = Config.Bind("1/KEYBINDS", "keybind_two", "None,None", "[Keybind two]\nlist of keyboard keys that need to be pressed/held to trigger the keybind (1-10).\nthe first (left most) key in the list is the only key that needs to be pressed, while all other keys need to be held.\nexample keybinds: \"Enter,K,CapsLock\" \"Enter,W,S,LeftCtrl\" \"Backquote\". list of valid keys: https://web.archive.org/web/20250305185947/https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.Key.html"); cfg1_keybind_two.Value = temp1_keybind_two.Value;
			temp1_keybind_prompt = Config.Bind("1/KEYBINDS", "keybind_prompt", true, "[Keybind prompt]\ndisplays a cancel or confirm screen when pressing a keybind to leave the lobby, with the text showing if you're leaving with or without saving."); cfg1_keybind_prompt.Value = temp1_keybind_prompt.Value;
			temp2_backup_mode    = Config.Bind("2/BACKUPS",  "backup_mode", 3, "[Backup mode]\ncontrols whether backups are created before loading, before saving, both, or disabled. backups won't be created while playing in lan.\n1 = backups are only created before loading the save file. (before the SampleSceneRelay scene is loaded).\n2 = backups are only created at the start of SaveGame() (which is called when leaving the lobby or when the ship auto saves after a round). LCSaveFile backup will be skipped if you're not the host. if a keybind skips calling SaveGame() then the backups won't be made.\n3 = backups are created before loading and before saving.\n4 = disable backups."); cfg2_backup_mode.Value = temp2_backup_mode.Value;
			temp2_backup_number  = Config.Bind("2/BACKUPS",  "backup_number", 3, "[Backup number]\nmaximum number of backups per save file (1-100).\nwhen a backup being created would exceed the maximum number then the earliest/lowest backup for that save file will be deleted after the new backup is created.\nthe only files that can be deleted are files named \"(currentSaveFileName/generalSaveDataName)_SaveUtilsBackup-#\". anything else won't be considered a backup file.\nfor example renaming \"LCSaveFile1_SaveUtilsBackup-49\" to \"LCSaveFile1_SaveUtilsBackup-49test\" will make it not count towards the maximum number of backups for LCSaveFile1 and won't be deleted."); cfg2_backup_number.Value = temp2_backup_number.Value;
			temp2_backup_data    = Config.Bind("2/BACKUPS",  "backup_general_save_data", true, "[Backup general save data]\nwhether a backup of LCGeneralSaveData is created after a LCSaveFile backup would be created.\nLCGeneralSaveData gets saved to when you're the host and when you're not the host (for data like player rank xp, settings, etc), so a backup will also be made when you're not the host."); cfg2_backup_data.Value = temp2_backup_data.Value;
			temp2_backup_mod     = Config.Bind("2/BACKUPS",  "only_backup_modified_saves", true, "[Only backup modified saves]\nwhether to only create a backup of LCSaveFile when the last modified times of that save file and the current highest backup of that save file are different, so a backup won't be made if it would be identical to the previous backup for that save file.\nthe times are from System.IO.File.GetLastWriteTime()."); cfg2_backup_mod.Value = temp2_backup_mod.Value;
			temp3_delete_delete  = Config.Bind("3/MISC",     "prevent_deletion", true, "[Prevent save deletion]\nremoves calls to ES3.DeleteFile() (notably when an error happens while comparing the game version of the save file to the current version). except for when manually selecting delete save file in the main menu."); cfg3_delete_delete.Value = temp3_delete_delete.Value;
			temp3_disable_splash = Config.Bind("3/MISC",     "disable_splash", false, "[Disable splash screen]\ndisables the splash screen shown before selecting online/lan."); cfg3_disable_splash.Value = temp3_disable_splash.Value;
			temp3_debug          = Config.Bind("3/MISC",     "debug", 4, "[Debugging]\n1 = enables logging keybind update conditions (every second), keybind config values.\n2 = enables logging backup file location directory path, each file name in the directory/folder, how long a backup took to be created/deleted.\n3 = enable debugging logs for keybinds and backups.\n4 = disable debugging logs."); cfg3_debug.Value = temp3_debug.Value;

			mls = BepInEx.Logging.Logger.CreateLogSource("Save Utils");
			mls.LogInfo("Nope!");
			if (save_utils.check_configs() == false)
			{
				mls.LogFatal("some config values were invalid, skipping harmony patch (save utils will be disabled)");
				return;
			}
			harmony.PatchAll(typeof(baldatro.save_utils));
			if (cfg3_disable_splash.Value == true)
			{
				save_utils.start_splash_timer();
			}

			}}public class cfg_bool{public bool Value{get;set;
			}}public class cfg_int{public int Value{get;set;
			}}public class cfg_string{public string Value{get;set;
		}
	}
	public static class save_utils
	{
		public static bool check_configs()
		{
			List<string> temp = new List<string>();

			if ((su.cfg1_keybind_mode.Value >= 1 && su.cfg1_keybind_mode.Value <= 3) == false) temp.Add("keybind_mode must be within 1-3 [keybind_mode:" + su.cfg1_keybind_mode.Value + "]");

			if (su.cfg1_keybind_mode.Value != 3 && check_keybinds(config: su.cfg1_keybind_one.Value, list: ref keybind_one, name: "keybind_one") == false) temp.Add("keybind_one wasn't created [keybind_one:" + su.cfg1_keybind_one.Value + "]");

			if (su.cfg1_keybind_mode.Value != 3 && check_keybinds(config: su.cfg1_keybind_two.Value, list: ref keybind_two, name: "keybind_two") == false) temp.Add("keybind_two wasn't created [keybind_two:" + su.cfg1_keybind_two.Value + "]");

			if ((su.cfg2_backup_mode.Value >= 1 && su.cfg2_backup_mode.Value <= 4) == false) temp.Add("backup_mode must be within 1-4 [backup_mode:" + su.cfg2_backup_mode.Value + "]");

			if ((su.cfg2_backup_number.Value >= 1 && su.cfg2_backup_number.Value <= 100) == false) temp.Add("backup_number must be within 1-100 [backup_number:" + su.cfg2_backup_number.Value + "]");

			if ((su.cfg3_debug.Value >= 1 && su.cfg3_debug.Value <= 4) == false) temp.Add("debug number must be within 1-4 [debug:" + su.cfg3_debug.Value + "]");

			if (temp != null && temp.Count > 0)
			{
				foreach (string text in temp)
				{
					su.mls.LogError(text);
				}
				return false;
			}
			return true;
		}
		private static bool check_keybinds(string config, ref List<Key> list, string name)
		{
			try
			{
				List<Key> temp_list = new List<Key>();
				bool temp = true;
				if (string.IsNullOrEmpty(config) == false)
				{
					string[] keys = config.Replace(" ", "").Replace("\"", "").Split(new char[]{','});
					if ((keys.Length >= 1 && keys.Length <= 5) == false)
					{
						temp = false;
						su.mls.LogError(name + " must be within 1-10 keys [keys.Length:" + keys.Length + "]");
					}
					foreach (string key in keys)
					{
						try
						{
							if (string.IsNullOrEmpty(key) == false)
							{
								Key k = (Key)System.Enum.Parse(typeof(Key), key);
								if (System.Enum.IsDefined(typeof(Key), k) == true)
								{
									if (k == Key.None)
									{
										su.mls.LogWarning(name + " will not be triggerable since it contains a None key");
									}
									temp_list.Add(k);
								}
								else
								{
									temp = false;
									su.mls.LogError(name + " contains an invalid key [Invalid:" + key + "]");
								}
							}
							else
							{
								temp = false;
								su.mls.LogError(name + " contains an empty or null key [Invalid:" + key + "][keys.ToString():" + System.String.Join(",", keys) + "]");
							}
						}
						catch (System.ArgumentException error)
						{
							temp = false;
							su.mls.LogError(name + " contains an invalid key [Invalid:" + error + "]");
						}
					}
					if ((temp_list != null && temp_list.Count > 0) == false)
					{
						temp = false;
						su.mls.LogError(name + " temp_list was null or empty");
					}
				}
				else
				{
					temp = false;
					su.mls.LogError(name + " was null or empty");
				}
				if (temp == true)
				{
					list = temp_list;
					return true;
				}
				return false;
			}
			catch (System.Exception error)
			{
				su.mls.LogError(name + " error, " + error);
				return false;
			}
			//return false;
		}

		public static List<Key> keybind_one;

		public static List<Key> keybind_two;

		private static bool saving = true;

		private static bool each_pressed = true;

		private static bool pressed_keybind = false;

		private static QuickMenuManager menu;

		private static bool created_saved_prompts = false;

		private static GameObject saved_leave_with_saving_prompt;

		private static GameObject saved_leave_without_saving_prompt;

		private static GameObject leave_with_saving_prompt;

		private static GameObject leave_without_saving_prompt;

		private static PlayerControllerB lpc;

		private static float keybind_debug_time = 0f;

//		// keybinds //
		[HarmonyPatch(typeof(StartOfRound), "Update"), HarmonyPrefix]
		private static void pre1(StartOfRound __instance)
		{
			if (su.cfg1_keybind_mode.Value != 1 && su.cfg1_keybind_mode.Value != 2) return;
			lpc = GameNetworkManager.Instance.localPlayerController;
			if (StartOfRound.Instance != null && __instance != null && GameNetworkManager.Instance != null && lpc != null && lpc.inTerminalMenu == false && lpc.isTypingChat == false && lpc.quickMenuManager != null && lpc.quickMenuManager.isMenuOpen == false && lpc.inSpecialMenu == false && lpc.justConnected == false && HUDManager.Instance != null)
			{
				if ((su.cfg3_debug.Value == 1 || su.cfg3_debug.Value == 3) && Time.realtimeSinceStartup > keybind_debug_time)
				{
					keybind_debug_time = Time.realtimeSinceStartup + 1f;
					su.mls.LogDebug("keybind update conditions true");
				}
				pressed_keybind = false;
				if (keybind_one != null && keybind_one.Count > 0)
				{
					if (Keyboard.current[keybind_one[0]].wasPressedThisFrame == true)
					{
						each_pressed = true;
						foreach (Key k in keybind_one)
						{
							if (Keyboard.current[k].isPressed == false)
							{
								each_pressed = false;
								break;
							}
						}
						if (each_pressed == true)
						{
							su.mls.LogInfo("pressed keybind one [keybind_one:" + su.cfg1_keybind_one.Value + "][keybind_mode:" + su.cfg1_keybind_mode.Value + "]");
							pressed_keybind = true;
							if (su.cfg1_keybind_mode.Value == 1)
							{
								if (su.cfg1_keybind_prompt.Value == true)
								{
									if (menu == null || leave_with_saving_prompt == null)
									{
										su.mls.LogError("keybind error" + (menu == null ? ", QuickMenuManager reference was null" : "") + (leave_with_saving_prompt == null ? ", custom leave_with_saving_prompt gameobject was null" : ""));
										return;
									}
									menu.OpenQuickMenu();
									if (menu.isMenuOpen == true)
									{
										menu.CloseQuickMenuPanels();
										menu.mainButtonsPanel.SetActive(false);
										menu.playerListPanel.SetActive(false);
										menu.debugMenuUI.SetActive(false);
										leave_with_saving_prompt.SetActive(true);
										su.mls.LogInfo("enabled leave_with_saving_prompt");
									}
								}
								else
								{
									su.mls.LogMessage("disconnecting with saving");
									saving = true;
									GameNetworkManager.Instance.Disconnect();
								}
							}
							else
							{
								saving = !saving;
								su.mls.LogMessage((saving == true ? "enabled" : "disabled") + " saving");
								HUDManager.Instance.DisplayTip("Saving", (saving == true ? "ENABLED" : "DISABLED"), !saving);
							}
						}
					}
				}
				if (pressed_keybind == false && keybind_two != null && keybind_two.Count > 0)
				{
					if (Keyboard.current[keybind_two[0]].wasPressedThisFrame == true)
					{
						each_pressed = true;
						foreach (Key k in keybind_two)
						{
							if (Keyboard.current[k].isPressed == false)
							{
								each_pressed = false;
								break;
							}
						}
						if (each_pressed == true)
						{
							su.mls.LogInfo("pressed keybind two [keybind_two:" + su.cfg1_keybind_two.Value + "][keybind_mode:" + su.cfg1_keybind_mode.Value + "]");
							//pressed_keybind = true;
							if (su.cfg1_keybind_mode.Value == 1)
							{
								if (su.cfg1_keybind_prompt.Value == true)
								{
									if (menu == null || leave_without_saving_prompt == null)
									{
										su.mls.LogError("keybind error" + (menu == null ? ", QuickMenuManager reference was null" : "") + (leave_without_saving_prompt == null ? ", custom leave_without_saving_prompt gameobject was null" : ""));
										return;
									}
									menu.OpenQuickMenu();
									if (menu.isMenuOpen == true)
									{
										menu.CloseQuickMenuPanels();
										menu.mainButtonsPanel.SetActive(false);
										menu.playerListPanel.SetActive(false);
										menu.debugMenuUI.SetActive(false);
										leave_without_saving_prompt.SetActive(true);
										su.mls.LogInfo("enabled leave_without_saving_prompt");
									}
								}
								else
								{
									su.mls.LogMessage("disconnecting without saving");
									saving = false;
									GameNetworkManager.Instance.Disconnect();
								}
							}
							else
							{
								if (su.cfg1_keybind_prompt.Value == true)
								{
									if (menu == null || leave_with_saving_prompt == null || leave_without_saving_prompt == null)
									{
										su.mls.LogError("keybind error" + (menu == null ? ", QuickMenuManager reference was null" : "") + (leave_with_saving_prompt == null ? ", custom leave_with_saving_prompt gameobject was null" : "") + (leave_without_saving_prompt == null ? ", custom leave_without_saving_prompt gameobject was null" : ""));
										return;
									}
									menu.OpenQuickMenu();
									if (menu.isMenuOpen == true)
									{
										menu.CloseQuickMenuPanels();
										menu.mainButtonsPanel.SetActive(false);
										menu.playerListPanel.SetActive(false);
										menu.debugMenuUI.SetActive(false);
										if (saving == true)
										{
											leave_with_saving_prompt.SetActive(true);
											su.mls.LogInfo("enabled leave_with_saving_prompt");
										}
										else
										{
											leave_without_saving_prompt.SetActive(true);
											su.mls.LogInfo("enabled leave_without_saving_prompt");
										}
									}
								}
								else
								{
									su.mls.LogMessage("disconnecting with saving " + (saving == true ? "enabled" : "disabled"));
									GameNetworkManager.Instance.Disconnect();
								}
							}
						}
					}
				}
			}
			else if ((su.cfg3_debug.Value == 1 || su.cfg3_debug.Value == 3) && Time.realtimeSinceStartup > keybind_debug_time)
			{
				keybind_debug_time = Time.realtimeSinceStartup + 1f;
				su.mls.LogDebug("keybind update conditions false [" + (int)Time.realtimeSinceStartup + (StartOfRound.Instance == null ? ",SOR1:Null" : "") + (__instance == null ? ",SOR2:Null" : "") + (GameNetworkManager.Instance == null ? ",GNM:Null" : "") + (lpc == null ? ",LocalPlayerController:Null" : ((lpc.inTerminalMenu == true ? ",InTerminalMenu:True" : "") + (lpc.isTypingChat == true ? ",IsTypingChat:True" : "") + (lpc.quickMenuManager == null ? ",QuickMenuManager:Null" : (lpc.quickMenuManager.isMenuOpen == true ? ",IsMenuOpen:True" : "")) + (lpc.inSpecialMenu == true ? ",InSpecialMenu:True" : "") + (lpc.justConnected == true ? ",JustConnected:True" : ""))) + (HUDManager.Instance == null ? ",HM:Null" : "") + "]");
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "Awake"), HarmonyPrefix]
		private static void pre2()
		{
			if (su.cfg1_keybind_mode.Value == 1) saving = true;
			if (su.cfg3_debug.Value == 1 || su.cfg3_debug.Value == 3) su.mls.LogDebug("keybind config values [ConfigKeybindMode:" + su.cfg1_keybind_mode.Value + ",ConfigKeybindOne:" + su.cfg1_keybind_one.Value + ",ConfigKeybindTwo:" + su.cfg1_keybind_two.Value + ",KeybindOneNullOrEmpty:" + ((keybind_one != null && keybind_one.Count > 0) == false) + ",KeybindTwoNullOrEmpty:" + ((keybind_two != null && keybind_two.Count > 0) == false) + "]");
		}
		[HarmonyPatch(typeof(QuickMenuManager), "Start"), HarmonyPrefix]
		private static void pre3(QuickMenuManager __instance)
		{
			if (su.cfg1_keybind_prompt.Value == true)
			{
				menu = __instance;
				if (menu != null && menu.leaveGameConfirmPanel != null && menu.leaveGameConfirmPanel.transform != null && menu.leaveGameConfirmPanel.transform.parent != null)
				{
					try
					{
						if (saved_leave_with_saving_prompt == null || saved_leave_without_saving_prompt == null)
						{
							if (created_saved_prompts == false)
							{
								created_saved_prompts = true;

								GameObject go1 = Object.Instantiate<GameObject>(menu.leaveGameConfirmPanel);
								go1.name = "saved_leave_with_saving_prompt";
								go1.SetActive(false);
								TMPro.TextMeshProUGUI text1 = go1.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).First(_ => _.name == "Text (TMP) (1)");
								text1.text = "Would you like to save and leave the game?";
								text1.enabled = true;
								Object.DontDestroyOnLoad(go1);
								saved_leave_with_saving_prompt = go1;

								GameObject go2 = Object.Instantiate<GameObject>(menu.leaveGameConfirmPanel);
								go2.name = "saved_leave_without_saving_prompt";
								go2.SetActive(false);
								TMPro.TextMeshProUGUI text2 = go2.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).First(_ => _.name == "Text (TMP) (1)");
								text2.text = "Would you like to leave the game without saving?";
								text2.enabled = true;
								Object.DontDestroyOnLoad(go2);
								saved_leave_without_saving_prompt = go2;
							}
							else
							{
								su.mls.LogError("saved keybind prompts did not finish being created");
							}
						}
						if (saved_leave_with_saving_prompt != null && saved_leave_without_saving_prompt != null)
						{
							leave_with_saving_prompt = Object.Instantiate<GameObject>(saved_leave_with_saving_prompt, menu.leaveGameConfirmPanel.transform.parent);
							leave_with_saving_prompt.name = "leave_with_saving_prompt";
							leave_with_saving_prompt.SetActive(false);
							leave_without_saving_prompt = Object.Instantiate<GameObject>(saved_leave_without_saving_prompt, menu.leaveGameConfirmPanel.transform.parent);
							leave_without_saving_prompt.name = "leave_without_saving_prompt";
							leave_without_saving_prompt.SetActive(false);

							UnityEngine.UI.Button temp1 = leave_with_saving_prompt.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(_ => _.name == "Quit (1)"); if (temp1 != null) { temp1.onClick = new UnityEngine.UI.Button.ButtonClickedEvent(); temp1.onClick.AddListener(new UnityAction(confirm_leave_with_saving)); } else { su.mls.LogError("couldn't find component 1 for leave_with_saving_prompt"); }
							UnityEngine.UI.Button temp2 = leave_with_saving_prompt.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(_ => _.name == "Quit (2)"); if (temp2 != null) { temp2.onClick = new UnityEngine.UI.Button.ButtonClickedEvent(); temp2.onClick.AddListener(new UnityAction(cancel_keybind_prompt)); } else { su.mls.LogError("couldn't find component 2 for leave_with_saving_prompt"); }
							UnityEngine.UI.Button temp3 = leave_without_saving_prompt.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(_ => _.name == "Quit (1)"); if (temp3 != null) { temp3.onClick = new UnityEngine.UI.Button.ButtonClickedEvent(); temp3.onClick.AddListener(new UnityAction(confirm_leave_without_saving)); } else { su.mls.LogError("couldn't find component 1 for leave_without_saving_prompt"); }
							UnityEngine.UI.Button temp4 = leave_without_saving_prompt.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(_ => _.name == "Quit (2)"); if (temp4 != null) { temp4.onClick = new UnityEngine.UI.Button.ButtonClickedEvent(); temp4.onClick.AddListener(new UnityAction(cancel_keybind_prompt)); } else { su.mls.LogError("couldn't find component 2 for leave_without_saving_prompt"); }
						}
					}
					catch (System.Exception error)
					{
						su.mls.LogError("error creating keybind prompts, " + error);
					}
				}
				else
				{
					su.mls.LogError("couldn't create keybind prompts" + (menu == null ? ", QuickMenuManager reference was null" : (menu.leaveGameConfirmPanel == null ? ", QuickMenuManager.leaveGameConfirmPanel was null" : (menu.leaveGameConfirmPanel.transform == null ? ", QuickMenuManager.leaveGameConfirmPanel.transform was null" : (menu.leaveGameConfirmPanel.transform.parent == null ? ", QuickMenuManager.leaveGameConfirmPanel.transform.parent was null" : "")))));
				}
			}
		}
		[HarmonyPatch(typeof(QuickMenuManager), "CloseQuickMenuPanels"), HarmonyPrefix]
		private static void pre4()
		{
			if (su.cfg1_keybind_prompt.Value == true)
			{
				if (leave_with_saving_prompt != null) leave_with_saving_prompt.SetActive(false);
				if (leave_without_saving_prompt != null) leave_without_saving_prompt.SetActive(false);
			}
		}
		public static void confirm_leave_with_saving()
		{
			su.mls.LogMessage("disconnecting with saving");
			saving = true;
			GameNetworkManager.Instance.Disconnect();
		}
		public static void confirm_leave_without_saving()
		{
			su.mls.LogMessage("disconnecting without saving");
			saving = false;
			GameNetworkManager.Instance.Disconnect();
		}
		public static void cancel_keybind_prompt()
		{
			su.mls.LogInfo("cancelled keybind prompt");
			menu.CloseQuickMenu();
		}
		[HarmonyPatch(typeof(GameNetworkManager), "Disconnect"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn1(IEnumerable<CodeInstruction> Instrs)
		{
			if (su.cfg1_keybind_mode.Value == 1 || su.cfg1_keybind_mode.Value == 2)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					if (l[n].ToString() == "call void GameNetworkManager::SaveGame()")
					{
						yield return new CodeInstruction(OpCodes.Call, typeof(save_utils).GetMethod("pre_save_game"));
					}
					else
					{
						yield return l[n];
					}
					//su.mls.LogInfo(l[n].ToString());
				}
			}
		}
		[HarmonyPatch(typeof(StartOfRound), "AutoSaveShipData"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn2(IEnumerable<CodeInstruction> Instrs)
		{
			if (su.cfg1_keybind_mode.Value == 2)
			{
				var l = new List<CodeInstruction>(Instrs);
				for (int n = 0; n < l.Count; n = n + 1)
				{
					if (l[n].ToString() == "callvirt void GameNetworkManager::SaveGame()")
					{
						yield return new CodeInstruction(OpCodes.Call, typeof(save_utils).GetMethod("pre_save_game"));
					}
					else
					{
						yield return l[n];
					}
					//su.mls.LogInfo(l[n].ToString());
				}
			}
		}
		public static void pre_save_game(GameNetworkManager instance)
		{
			if (saving == true)
			{
				su.mls.LogInfo("calling SaveGame()");
				instance.SaveGame();
			}
			else
			{
				su.mls.LogInfo("skipped calling SaveGame()");
			}
		}

//		// backups //
		private static float last_save_time = 0f;

		private static float last_data_time = 0f;

		private static System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

		[HarmonyPatch(typeof(GameNetworkManager), "SaveGame"), HarmonyPrefix, HarmonyPriority(Priority.First)]
		private static void pre5(GameNetworkManager __instance)
		{
			if (su.cfg2_backup_mode.Value == 2 || su.cfg2_backup_mode.Value == 3)
			{
				su.mls.LogInfo("calling create_backup() before saving");
				try { create_backup(data: false, check_host: true); if (su.cfg2_backup_data.Value == true) { create_backup(data: true); } } catch (System.Exception error) { su.mls.LogError("error creating backup before saving, " + error); }
				timer.Stop();
			}
		}
		[HarmonyPatch(typeof(MenuManager), "delayedStartScene", MethodType.Enumerator), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn3(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			for (int n = 0; n < l.Count; n = n + 1)
			{
				yield return l[n];
				if ((su.cfg2_backup_mode.Value == 1 || su.cfg2_backup_mode.Value == 3) && l[n].ToString() == "callvirt virtual void TMPro.TMP_Text::set_text(string value)")
				{
					yield return new CodeInstruction(OpCodes.Call, typeof(save_utils).GetMethod("call_create_backup"));
				}
				//su.mls.LogInfo(l[n].ToString());
			}
		}
		public static void call_create_backup()
		{
			if (su.cfg2_backup_mode.Value == 1 || su.cfg2_backup_mode.Value == 3)
			{
				su.mls.LogInfo("calling create_backup() before loading");
				try { create_backup(data: false, check_host: false); if (su.cfg2_backup_data.Value == true) { create_backup(data: true); } } catch (System.Exception error) { su.mls.LogError("error creating backup before loading, " + error); }
				timer.Stop();
			}
		}
		private static void create_backup(bool data = false, bool check_host = false)
		{
			if (data == false && (Time.realtimeSinceStartup > last_save_time) == false)
			{
				su.mls.LogError("create_backup(data: false) was called within 2 seconds of the last time it was called");
				return;
			}
			if (data == true && (Time.realtimeSinceStartup > last_data_time) == false)
			{
				su.mls.LogError("create_backup(data: true) was called within 2 seconds of the last time it was called");
				return;
			}
			if (data == false) last_save_time = Time.realtimeSinceStartup + 2f;
			if (data == true) last_data_time = Time.realtimeSinceStartup + 2f;
			timer.Restart();
			if (GameNetworkManager.Instance != null)
			{
				string current = (data == false ? GameNetworkManager.Instance.currentSaveFileName : GameNetworkManager.generalSaveDataName);
				if (GameNetworkManager.Instance.disableSteam == true)
				{
					su.mls.LogInfo("skipped creating backup of " + current + ", disableSteam was true (playing in lan)");
					return;
				}
				if (check_host == true && data == false && GameNetworkManager.Instance.isHostingGame == false)
				{
					su.mls.LogInfo("skipped creating backup of " + current + ", isHostingGame was false");
					return;
				}
				if (current == GameNetworkManager.LCchallengeFileName || current == "LCChallengeFile")
				{
					su.mls.LogInfo("skipped creating backup of LCChallengeFile");
					return;
				}
				su.mls.LogInfo("creating backup of " + current);
				string directory = ((string)typeof(ES3Internal.ES3IO).GetField("persistentDataPath", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null)).Replace("\\", "/");
				if (su.cfg3_debug.Value == 2 || su.cfg3_debug.Value == 3) su.mls.LogDebug("directory " + directory);
				if (ES3.FileExists(current) == true && ES3Internal.ES3IO.FileExists(directory + "/" + current) == true)
				{
					string[] files = ES3.GetFiles();
					if (files != null && files.Length > 0 && files.Length < 492 && files.Contains(current) == true && files.Contains("LCGeneralSaveData") == true)
					{
						string name = current + "_SaveUtilsBackup-"; //LCSaveFile#_SaveUtilsBackup-# //LCGeneralSaveData_SaveUtilsBackup-#
						int lowest = int.MaxValue;
						int highest = 0;
						int total = 0;
						bool first = true;
						foreach (string file in files)
						{
							if (su.cfg3_debug.Value == 2 || su.cfg3_debug.Value == 3) su.mls.LogDebug("file " + file + " starts with \"" + name + "\"? " + file.StartsWith(name));
							if (file.StartsWith(name) == true)
							{
								int n;
								if (string.IsNullOrEmpty(file.Replace(name, "")) == false && file.Replace(name, "").Contains("-") == false && int.TryParse(file.Replace(name, ""), out n) == true)
								{
									total = total + 1;
									first = false;
									if (n < lowest) lowest = n;
									if (n > highest) highest = n;
								}
								else
								{
									su.mls.LogError("couldn't parse backup number from " + file);
								}
							}
						}
						if (((lowest < int.MaxValue && highest > 0) == true || first == true) && total < 123)
						{
							string _old = name + lowest;
							string _new = name + (highest + 1);
							if (ES3.FileExists(_new) == false && ES3Internal.ES3IO.FileExists(directory + "/" + _new) == false)
							{
								if (su.cfg2_backup_mod.Value == true && first == false && data == false)
								{
									string _current = name + highest;
									if (ES3.FileExists(_current) == true && ES3Internal.ES3IO.FileExists(directory + "/" + _current) == true)
									{
										System.DateTime time1 = System.IO.File.GetLastWriteTime(directory + "/" + current);
										System.DateTime time2 = System.IO.File.GetLastWriteTime(directory + "/" + _current);
										if (su.cfg3_debug.Value == 2 || su.cfg3_debug.Value == 3) su.mls.LogDebug(current + " " + time1 + ", " + _current + " " + time2);
										if (time1 == time2)
										{
											su.mls.LogInfo("skipped creating " + _new + ", last modified time of " + current + " was equal to the last modified time of " + _current);
											return;
										}
									}
									else
									{
										su.mls.LogError("couldn't compare last modified times, " + _current + " doesn't exist");
									}
								}
								su.mls.LogInfo("creating copy of " + current + " with file name " + _new);
								ES3Internal.ES3IO.CopyFile(directory + "/" + current, directory + "/" + _new);
								su.mls.LogMessage("created new backup");
								if ((total + 1) > su.cfg2_backup_number.Value)
								{
									if (ES3.FileExists(_old) == true && ES3Internal.ES3IO.FileExists(directory + "/" + _old) == true)
									{
										su.mls.LogInfo("deleting lowest copy of " + current + " with file name " + _old + ", total was " + (total + 1) + "/" + su.cfg2_backup_number.Value);
										ES3Internal.ES3IO.DeleteFile(directory + "/" + _old);
										su.mls.LogMessage("deleted lowest backup");
									}
									else
									{
										su.mls.LogError("couldn't delete lowest backup, " + _old + " doesn't exist");
									}
								}
								timer.Stop();
								if (su.cfg3_debug.Value == 2 || su.cfg3_debug.Value == 3) su.mls.LogDebug("created/deleted backups of " + current + " in " + (decimal)timer.Elapsed.TotalSeconds + " seconds"); //won't exceed max decimal value. TotalSeconds = (long casted to double)_ticks * 1E-07
							}
							else
							{
								su.mls.LogError("couldn't create backup, " + _new + " already exists");
							}
						}
						else
						{
							su.mls.LogError("couldn't create backup" + ((lowest < int.MaxValue) == false ? ", lowest wasn't set" : "") + ((highest > 0) == false ? ", highest wasn't set" : "") + (first == false ? ", first was false" : ""));
						}
					}
					else
					{
						su.mls.LogError("couldn't create backup" + (files == null ? ", files was null" : ((files.Length > 0) == false ? ", files.Length wasn't > 0" : "") + ((files.Length < 492) == false ? ", files.Length wasn't < 492" : "")) + (files.Contains(current) == false ? (", directory doesn't contain " + current) : "") + (files.Contains("LCGeneralSaveData") == false ? ", directory doesn't contain LCGeneralSaveData" : ""));
					}
				}
				else
				{
					su.mls.LogError("couldn't create backup, " + current + " doesn't exist");
				}
			}
			else
			{
				su.mls.LogError("couldn't create backup, GameNetworkManager was null");
			}
		}

//		// delete delete //
		[HarmonyPatch(typeof(MenuManager), "Start"), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn4(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			if (su.cfg3_delete_delete.Value == true)
			{
				for (int n = 0; n < l.Count; n = n + 1)
				{
					if (n > 1 && l[n].ToString() == "call static void ES3::DeleteFile(string filePath)")
					{
						l[n - 1].opcode = OpCodes.Nop;
						l[n].opcode = OpCodes.Nop;
						break;
					}
					//su.mls.LogInfo(l[n].ToString());
				}
			}
			return l;
		}
		[HarmonyPatch(typeof(PreInitSceneScript), "restartGameDueToCorruptedFile", MethodType.Enumerator), HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> trn5(IEnumerable<CodeInstruction> Instrs)
		{
			var l = new List<CodeInstruction>(Instrs);
			if (su.cfg3_delete_delete.Value == true)
			{
				for (int n = 0; n < l.Count; n = n + 1)
				{
					if (n > 1 && l[n].ToString() == "call static void ES3::DeleteFile(string filePath)")
					{
						l[n - 1].opcode = OpCodes.Nop;
						l[n].opcode = OpCodes.Nop;
					}
					//su.mls.LogInfo(l[n].ToString());
				}
			}
			return l;
		}

//		// disable splash screen //
		private static bool end = false;

		public static async void start_splash_timer()
		{
			su.mls.LogInfo("splash timer start");
			await Task.Run(() => splash_timer(40000));
			su.mls.LogInfo("splash timer end");
		}
		private static async Task splash_timer(int rem)
		{
			while (end == false && rem > 0)
			{
				SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
				await Task.Delay(1);
				rem = rem - 1;
			}
		}
		[HarmonyPatch(typeof(IngamePlayerSettings), "Awake"), HarmonyPrefix, HarmonyPriority(Priority.First)]
		private static void pre6()
		{
			end = true;
			if (su.cfg3_disable_splash.Value == true) su.mls.LogInfo("ending splash timer");
		}
	}
}
