using BindingFlags = System.Reflection.BindingFlags;
using GetField = System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using GameNetcodeStuff;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using MonoMod.RuntimeDetour;

namespace bunny
{
	[BepInPlugin("4902.No_Console_Spam", "No_Console_Spam", "1.0.0")]
	public class no_console_spam : BaseUnityPlugin
	{
		private static ConfigEntry<bool> yeet;
		private static ConfigEntry<uint>[] cfg = new ConfigEntry<uint>[38+1];
		private static ConfigEntry<string> cdlist;

		private static ConfigEntry<bool> kiwi_debug;

		private static uint cd_01; //average velocity
		private static uint cd_02; //targetable a
		private static uint cd_04; //level timer
		private static uint cd_06; //called teleport function
		private static uint cd_07; //company desk timer
		private static uint cd_08; //shower
		private static uint cd_09; //agent speed
		private static uint cd_10; //secondary use a-g
		private static uint cd_11; //player leave web
		private static uint cd_12; //zapgun
		private static uint cd_13; //walkie
		private static uint cd_14; //loud horn
		private static uint cd_15; //mouth dog
		private static uint cd_17; //hoarder bug
		private static uint cd_18; //random nav
		private static uint cd_19; //quicksand
		private static uint cd_20; //not targetable
		private static uint cd_21; //masked
		private static uint cd_22; //fox
		private static uint cd_23; //bush
		private static uint cd_25; //truck
		private static uint cd_26; //ropes
		private static uint cd_28; //grabinvalidated
		private static uint cd_30; //item meshes
		private static uint cd_31; //weedkiller
		private static uint cd_32; //spraypaint
		private static uint cd_33; //flashlight
		private static uint cd_34; //item being used
		private static uint cd_35; //masked elevator
		private static uint cd_36; //item spawning
		private static uint cd_37; //baboon
		private static uint cd_38; //kiwi

		private static System.Collections.Generic.List<string> r = new System.Collections.Generic.List<string>();

		private void Awake()
		{
			string s1 = "number of frames this string should be on cooldown for after being logged. set to 0 to disable the string completely";
			string s2 = "number of frames these strings should be on cooldown for after being logged. set to 0 to disable the strings completely";

			yeet = Config.Bind("#", "enabled", true, "whether this mod should be enabled or not\ndefault cooldown values are assuming 60 frames per second, with higher or lower fps you may want to adjust the values");
			cdlist  = Config.Bind("#", "cd_list", "{01,02,04,06,07,08,09,10,11,12,13,14,15,17,18,19,20,21,22,23,25,26,28,30,31,32,33,34,35,36,37,38}", "if you want to disable individual cooldowns, or if you want to see a variable in a string that the original string would've had (like cd_04), you can disable the cooldown related to that string with this option, which will prevent the transpiler/il from modifying those cooldowns related strings and will be the same as vanilla. to remove a cooldown remove its number from this configs string");
			cfg[1]  = Config.Bind("#", "01_cooldown", 180u, "[average velocity]\n"+s1+"\nconditions: while using the jetpack or while flower snakes are lifting the player\nfrequency: every frame\n(PlayerControllerB.Update)\n\"Average velocity: {0}\""); cd_01 = cfg[1].Value;
			cfg[2]  = Config.Bind("#", "02_cooldown", 480u, "[targetable a]\n"+s1+"\nconditions: roughly when certain enemies check if the player is targetable while they're in the ship\nfrequency: every frame (per enemy)\n(EnemyAI.PlayerIsTargetable)\n\"Targetable A\""); cd_02 = cfg[2].Value;
			cfg[4]  = Config.Bind("#", "04_cooldown", 60u,  "[level timer]\n"+s1+"\nconditions: during the xp increase/decrease animation after the end of a round\nfrequency: every frame for a short duration\n(HUDManager.SetPlayerLevelSmoothly)\n\"Level up timer: {0}\""); cd_04 = cfg[4].Value;
			cfg[6]  = Config.Bind("#", "06_cooldown", 180u, "[called teleport function]\n"+s1+"\nconditions: anytime TeleportPlayer() is called, most notably while the player is waiting to be respawned while in orbit it will log every frame\nfrequency: either infrequently or every frame\n(PlayerControllerB.TeleportPlayer)\n\"Called teleport function on (player)\""); cd_06 = cfg[6].Value;
			cfg[7]  = Config.Bind("#", "07_cooldown", 60u,  "[company desk timer]\n"+s2+"\nconditions: while host and while the desk door is open\nfrequency: every frame for a short duration\n(DepositItemsDesk.Update)\n\"Desk: Waiting to grab the items on the desk; {0}\"\n\"Desk: no objects on counter, waiting with door open; {0}\""); cd_07 = cfg[7].Value;
			cfg[8]  = Config.Bind("#", "08_cooldown", 600u, "[shower]\n"+s2+"\nconditions: while the shower is on and there is spray paint anywhere, and while the shower is on and spray paint is on a player\nfrequency: every frame\n(ShowerTrigger.Update)\n\"Shower is running with players inside!\"\n\"spray decal #{0} found as child of {1}\""); cd_08 = cfg[8].Value;
			cfg[9]  = Config.Bind("#", "09_cooldown", 180u, "[agent speed]\n"+s1+"\nconditions: while a giant was chasing a player and then lost sight of the player\nfrequency: every frame for maybe a short duration\n(ForestGiantAI.Update)\n\"agent speed: {0}\""); cd_09 = cfg[9].Value;
			cfg[10] = Config.Bind("#", "10_cooldown", 0u,   "[secondary use a-g]\n"+s2+"\nconditions: anytime the key that the item secondary use keybind is set to is pressed\nfrequency: up to 7 strings once per press\n(PlayerControllerB.ItemSecondaryUse_performed)\n\"secondary use A\"\n\"secondary use B\"\n\"secondary use C\"\n\"secondary use D\"\n\"secondary use E\"\n\"secondary use F\"\n\"secondary use G\""); cd_10 = cfg[10].Value;
			cfg[11] = Config.Bind("#", "11_cooldown", 1u,   "[player leave web]\n"+s1+"\nconditions: when a player leaves a web or other stuff like going through an entrance\nfrequency: once per web, so with 10 webs it would log 10 lines in 1 frame. cooldown doesn't need to be anything above 1\n(SandSpiderWebTrap.PlayerLeaveWeb)\n\"Player leave web called\""); cd_11 = cfg[11].Value;
			cfg[12] = Config.Bind("#", "12_cooldown", 240u, "[zapgun]\n"+s2+"\nconditions: activating the zap gun and or zapping something\nfrequency: some strings (multiple times) on each activation and 1 string logged every frame while zapping something\n(PatcherTool.ScanGun)\n\"Scan A\"\n\"Scan B\"\n\"Got shockable transform name :\"\n\"Zap gun light off!!!\"\n(PatcherTool.GunMeetsConditionsToShock)\n\"Target position: {0}\"\n\"Gun not meeting conditions to zap; {0}; {1}; {2}\"\n(PatcherTool.ItemActivate)\n\"Stop shocking gun\"\n\"Start scanning gun\"\n\"Use patcher tool\""); cd_12 = cfg[12].Value;
			cfg[13] = Config.Bind("#", "13_cooldown", 240u, "[walkie]\n"+s2+"\nconditions: using the walkie\nfrequency: some strings on each activation\n(WalkieTalkie.SetLocalClientSpeaking)\n\"Set local client speaking on walkie talkie: {0}\"\n(WalkieTalkie.SendWalkieTalkieStartTransmissionSFX)\n\"Walkie talkie A\"\n\"Walkie talkie #{0} {1} B\"\n\"is walkie being used: {0}\"\n\"Walkie talkie #{0} {1} C\"\n(WalkieTalkie.PlayerIsHoldingAnotherWalkieTalkie)\n\"False A\"\n\"False B\"\n\"False C\"\n\"{0}\""); cd_13 = cfg[13].Value;
			cfg[14] = Config.Bind("#", "14_cooldown", 300u, "[loud horn]\n"+s1+"\nconditions: holding the cord\nfrequency: every frame\n(ShipAlarmCord.HoldCordDown)\n\"HOLD horn local client called\""); cd_14 = cfg[14].Value;
			cfg[15] = Config.Bind("#", "15_cooldown", 480u, "[mouth dog]\n"+s2+"\nconditions: dog hearing noise\nfrequency: some strings initially and 1 string logged maybe every frame if hearing continuous noise\n(MouthDogAI.DetectNoise)\n\"dog '{0}': Heard noise! Distance: {1} meters\"\n(MouthDogAI.EnrageDogOnLocalClient)\n\"Mouth dog targetPos 1: {0}; distanceToNoise: {1}\"\n\"Mouth dog targetPos 2: {0}\"\n\"Dog lastheardnoisePosition: {0}\"\n(MouthDogAI.ReactToOtherDogHowl)\n\"Setting lastHeardNoisePosition to {0}\"\n(MouthDogAI.EndLungeClientRpc)\n\"Ending lunge\""); cd_15 = cfg[15].Value;
			cfg[17] = Config.Bind("#", "17_cooldown", 180u, "[hoarder bug]\n"+s2+"\nconditions: when a hoarder bug finds an item\nfrequency: every frame for maybe a short duration\n(HoarderBugAI.SetGoTowardsTargetObject)\n\": Setting target object and going towards it.\"\n\": i found an object but cannot reach it (or it has been taken by another bug):\""); cd_17 = cfg[17].Value;
			cfg[18] = Config.Bind("#", "18_cooldown", 300u, "[random nav]\n"+s1+"\nconditions: when some enemies search for a position on the map\nfrequency: usually low\n(RoundManager.GetRandomNavMeshPositionInRadius)\n\"Unable to get random nav mesh position in radius! Returning old pos\""); cd_18 = cfg[18].Value;
			cfg[19] = Config.Bind("#", "19_cooldown", 480u, "[quicksand]\n"+s2+"\nconditions: interacting with quicksand, can also be triggered by enemies\nfrequency: every frame for a short duration\n(QuicksandTrigger.OnTriggerStay)\n\"Set local player to sinking!\"\n(QuicksandTrigger.OnExit)\n\"Quicksand is not sinking local player!\"\n\"Quicksand is sinking local player!\"\n\"Quicksand is sinking local player! B\"\n\"Quicksand is sinking local player! C\""); cd_19 = cfg[19].Value;
			cfg[20] = Config.Bind("#", "20_cooldown", 180u, "[not targetable]\n"+s1+"\nconditions: player not being targetable while trying to be hit by an enemy (most commonly while invincable)\nfrequency: every frame for maybe a short duration\n(EnemyAI.MeetsStandardPlayerCollisionConditions)\n\"Player is not targetable\""); cd_20 = cfg[20].Value;
			cfg[21] = Config.Bind("#", "21_cooldown", 300u, "[masked]\n"+s2+"\nconditions: masked detecting noise\nfrequency: every few frames (per masked)\n(MaskedPlayerEnemy.DetectNoise)\n\"Noise heard relative loudness: {0}\"\n\"Can't hear noise reason A\"\n\"Can't hear noise reason B\""); cd_21 = cfg[21].Value;
			cfg[22] = Config.Bind("#", "22_cooldown", 600u, "[fox]\n"+s2+"\nconditions: fox existing during a round\nfrequency: every frame\n(BushWolfEnemy.Update)\n\"Fox spotted meter: {0}\"\n\"Fox A\"\n\"Fox B\"\n\"Fox C; {0}; {1}\"\n\"Fox D\"\n\"Fox E\"\n\"Fox F\"\n\"Fox G\"\n\"Fox H\"\n\"Fox I\"\n\"Fox J\"\n(BushWolfEnemy.GetBiggestWeedPatch)\n\"Bush wolf: No game objects found with spore tag; cancelling\"\n\"{0}: Mold spore {1} at {2} surrounded by {3}\"\n\"Bush wolf: Most surrounding spores is {0}\"\n\"Bush wolf: All spores found were lone spores; cancelling\""); cd_22 = cfg[22].Value;
			cfg[23] = Config.Bind("#", "23_cooldown", 180u, "[bush]\n"+s2+"\nconditions: using spray on bushes or loading a level\nfrequency: every frame for a short duration\n(MoldSpreadManager.DestroyMoldAtPosition)\n\"weeds found at pos {0}: {1}\"\n\"Index: {0}\"\n(MoldSpreadManager.GenerateMold)\n\"Growing back mold at index {0}\""); cd_23 = cfg[23].Value;
			cfg[25] = Config.Bind("#", "25_cooldown", 180u, "[truck]\n"+s2+"\nconditions: spawning truck, using magnet, collecting items with the truck, collide audio playing\nfrequency: some strings multiple times\n(VehicleController.CollectItemsInTruck)\n\"Collect items in truck A\"\n\"Collect items in truck B; {0}\"\n\"Collect items in truck C; {0}\"\n\"{0}; {1}; {2}\"\n\"Magneted? : {0}\"\n(VehicleController.PlayCollisionAudio)\n\"Play collision audio with type {0} A\""); cd_25 = cfg[25].Value;
			cfg[26] = Config.Bind("#", "26_cooldown", 300u, "[ropes]\n"+s1+"\nconditions: bought cruiser arriving\nfrequency: every frame for a short duration\n(ItemDropship.Update)\n\"Setting position of ropes\""); cd_26 = cfg[26].Value;
			cfg[28] = Config.Bind("#", "28_cooldown", 300u, "[grabinvalidated]\n"+s1+"\nconditions: waiting for a grabbed item to be validated\nfrequency: depends on the connection, if laggy then can be every frame\n(PlayerControllerB.GrabObject)\n\"grabInvalidated: {0}\""); cd_28 = cfg[28].Value;
			cfg[30] = Config.Bind("#", "30_cooldown", 0u,   "[item meshes]\n"+s1+"\nconditions: picking up or dropping certain items\nfrequency: once each time\n(GrabbableObject.EnableItemMeshes)\n\"DISABLING/ENABLING SKINNEDMESH:\""); cd_30 = cfg[30].Value;
			cfg[31] = Config.Bind("#", "31_cooldown", 600u, "[weedkiller]\n"+s1+"\nconditions: spraying weed killer\nfrequency: every frame\n(SprayPaintItem)\n\"Spraying, depleting tank\""); cd_31 = cfg[31].Value;
			cfg[32] = Config.Bind("#", "32_cooldown", 600u, "[spraypaint]\n"+s2+"\nconditions: spraying spray paint or weed killer\nfrequency: two lines each activation\n(SprayPaintItem)\n\"Start using spray\"\n\"Spray empty\"\n\"Spray not empty\"\n\"Stop using spray\""); cd_32 = cfg[32].Value;
			cfg[33] = Config.Bind("#", "33_cooldown", 480u, "[flashlight]\n"+s2+"\nconditions: another player using a flashlight\nfrequency: two lines each activation\n(FlashlightItem)\n\"Flashlight click. playerheldby null?: {0}\"\n\"Flashlight being disabled or enabled: {0}\""); cd_33 = cfg[33].Value;
			cfg[34] = Config.Bind("#", "34_cooldown", 480u, "[item being used]\n"+s1+"\nconditions: another player using an item\nfrequency: one line each activation\n(GrabbableObject)\n\"Is being used set to {0} by RPC\""); cd_34 = cfg[34].Value;
			cfg[35] = Config.Bind("#", "35_cooldown", 600u, "[masked elevator]\n"+s2+"\nconditions: masked in mineshaft not close to the elevator\nfrequency: every frame per masked\n(MaskedPlayerEnemy)\n\"goUp: {0}\"\n\"{0}, {1}\"\n\"goUp: {0}, elevatormovingdown: {1}\"\n\"{0}, {1}, {2}\""); cd_35 = cfg[35].Value;
			cfg[36] = Config.Bind("#", "36_cooldown", 0u,   "[item spawning]\n"+s2+"\nconditions: loading a save file or an item spawning/starting to fall\nfrequency: multiple lines per item\n(GrabbableObject)\n\"Item spawning: {0} ; item parent : {1}\"\n\"Item spawning: \" + itemProperties.itemName + \" ; item parent : null\"\n\"Start falling position: {0}\"\n\"global startposition falltoground for object {0}: {1}\"\n\"Item \" + itemProperties.itemName + \" landed on : \" + hitInfo.collider.name + \" / \" + hitInfo.transform.gameObject.name\n(StartOfRound)\n\"Item {0} #{1} spawn position: {2}\"\n\"Setting scrap value for item: {0}: {1}\"\n\"Loading item save data for item: {0}: {1}\""); cd_36 = cfg[36].Value;
			cfg[37] = Config.Bind("#", "37_cooldown", 600u, "[baboon hawk]\n"+s1+"\nconditions: baboon hawk seeing another enemy\nfrequency: every few frames\n(BaboonBirdAI)\n\"Threat type : {0}; threat level: {1} ; with distance: {2}\""); cd_37 = cfg[37].Value;
			cfg[38] = Config.Bind("#", "38_cooldown", 300u, "[kiwi]\n"+s2+"\nconditions: stealing kiwi egg, kiwi egg screaming, kiwi attacking the player with cruiser nearby\nfrequency: every frame for kiwi egg\n(GiantKiwiAI)\n\"dist: {0}\"\n(KiwiBabyItem)\n\"Baby bird distance to nest : {0}\"\n\"Counting scream timer; timer: {0}\""); cd_38 = cfg[38].Value;

			kiwi_debug = Config.Bind("K", "kiwi_debug", false, "[kiwi debug]\nwhether debug logs should be enabled for the giant kiwi enemy.\nif this config is false then the debugEnemyAI bool will be set to false when the kiwi spawns, disabling debug logs for the kiwi.\nsetting to true will disable this config and leave the debugEnemyAI bool unchanged.\nexample strings that will be toggled:\n\"Bird: Seeing visible threat: {0}; type: {1}\"\n\"GiantKiwi(Clone): Collided with player!\"");

			if (yeet.Value == true)
			{
				if (cdlist.Value.Contains("01") == true) IL.GameNetcodeStuff.PlayerControllerB.Update += pcb_update;
				if (cdlist.Value.Contains("02") == true) IL.EnemyAI.PlayerIsTargetable += enemy_target;
				if (cdlist.Value.Contains("04") == true) new ILHook((typeof(HUDManager).GetMethod("SetPlayerLevelSmoothly", BindingFlags.NonPublic | BindingFlags.Instance)).GetStateMachineTarget(), level);
				if (cdlist.Value.Contains("06") == true) IL.GameNetcodeStuff.PlayerControllerB.TeleportPlayer += pcb_teleport;
				if (cdlist.Value.Contains("07") == true) IL.DepositItemsDesk.Update += desk;
				if (cdlist.Value.Contains("08") == true) IL.ShowerTrigger.Update += shower;
				if (cdlist.Value.Contains("09") == true) IL.ForestGiantAI.Update += giant;
				if (cdlist.Value.Contains("10") == true) IL.GameNetcodeStuff.PlayerControllerB.ItemSecondaryUse_performed += pcb_secondary;
				if (cdlist.Value.Contains("11") == true) IL.SandSpiderWebTrap.PlayerLeaveWeb += spider;
				if (cdlist.Value.Contains("12") == true)
				{
					new ILHook((typeof(PatcherTool).GetMethod("ScanGun", BindingFlags.NonPublic | BindingFlags.Instance)).GetStateMachineTarget(), patch_1);
					IL.PatcherTool.GunMeetsConditionsToShock += patch_2;
					IL.PatcherTool.ItemActivate += patch_3;
				}
				if (cdlist.Value.Contains("13") == true)
				{
					IL.WalkieTalkie.SetLocalClientSpeaking += radio_1;
					IL.WalkieTalkie.SendWalkieTalkieStartTransmissionSFX += radio_2;
					IL.WalkieTalkie.PlayerIsHoldingAnotherWalkieTalkie += radio_3;
				}
				if (cdlist.Value.Contains("14") == true) IL.ShipAlarmCord.HoldCordDown += cord;
				if (cdlist.Value.Contains("15") == true)
				{
					IL.MouthDogAI.DetectNoise += dog_1;
					IL.MouthDogAI.EnrageDogOnLocalClient += dog_2;
					IL.MouthDogAI.ReactToOtherDogHowl += dog_3;
					IL.MouthDogAI.EndLungeClientRpc += dog_4;
				}
				if (cdlist.Value.Contains("17") == true) IL.HoarderBugAI.SetGoTowardsTargetObject += hoarder;
				if (cdlist.Value.Contains("18") == true) IL.RoundManager.GetRandomNavMeshPositionInRadius += random;
				if (cdlist.Value.Contains("19") == true)
				{
					IL.QuicksandTrigger.OnTriggerStay += sand_1;
					IL.QuicksandTrigger.OnExit += sand_2;
				}
				if (cdlist.Value.Contains("20") == true) IL.EnemyAI.MeetsStandardPlayerCollisionConditions += enemy_not_targetable;
				if (cdlist.Value.Contains("21") == true) IL.MaskedPlayerEnemy.DetectNoise += masked;
				if (cdlist.Value.Contains("22") == true)
				{
					IL.BushWolfEnemy.Update += bushwolf_update;
					IL.BushWolfEnemy.GetBiggestWeedPatch += bushwolf_big;
				}
				if (cdlist.Value.Contains("23") == true)
				{
					IL.MoldSpreadManager.DestroyMoldAtPosition += bush_destroy;
					IL.MoldSpreadManager.GenerateMold += bush_generate;
				}
				if (cdlist.Value.Contains("25") == true)
				{
					IL.VehicleController.CollectItemsInTruck += truck_collect;
					IL.VehicleController.PlayCollisionAudio += truck_audio;
				}
				if (cdlist.Value.Contains("26") == true) IL.ItemDropship.Update += ropes;
				if (cdlist.Value.Contains("28") == true) new ILHook((typeof(GameNetcodeStuff.PlayerControllerB).GetMethod("GrabObject", BindingFlags.NonPublic | BindingFlags.Instance)).GetStateMachineTarget(), grab);
				if (cdlist.Value.Contains("30") == true) IL.GrabbableObject.EnableItemMeshes += item_meshes;
				if (cdlist.Value.Contains("31") == true) IL.SprayPaintItem.LateUpdate += spray_1;
				if (cdlist.Value.Contains("32") == true) IL.SprayPaintItem.ItemActivate += spray_2;
				if (cdlist.Value.Contains("33") == true) IL.FlashlightItem.SwitchFlashlight += flashlight;
				if (cdlist.Value.Contains("34") == true) IL.GrabbableObject.ActivateItemClientRpc += item_used;
				if (cdlist.Value.Contains("35") == true) IL.MaskedPlayerEnemy.UseElevator += masked_elevator;
				if (cdlist.Value.Contains("36") == true)
				{
					IL.GrabbableObject.Start += item_spawn_1;
					IL.GrabbableObject.FallToGround += item_fall;
					IL.StartOfRound.LoadShipGrabbableItems += item_spawn_2;
				}
				if (cdlist.Value.Contains("37") == true) IL.BaboonBirdAI.DoLOSCheck += baboon;
				if (cdlist.Value.Contains("38") == true)
				{
					IL.GiantKiwiAI.NavigateTowardsTargetPlayer += kiwi_1;
					IL.KiwiBabyItem.Update += kiwi_2;
				}
				if (kiwi_debug.Value == false) On.GiantKiwiAI.Start += kiwi_3;

				//for (int n = 0; n < r.Count; n = n + 1) Debug.Log((n + 1) + " " + r[n] + "\"");
			}
		}

		//01 average velocity
		public static int _1;
		private static void pcb_update(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Average velocity: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_01 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<PlayerControllerB>>((self) =>
					{
						if ((Time.frameCount - _1) >= cd_01)
						{
							_1 = Time.frameCount;
							Debug.Log("Average velocity: " + self.averageVelocity + " [NCS:" + cd_01 + "-" + _1 + "]");
						}
					});
					break;
				}
			}
		}

		//02 targetable a
		public static int _2;
		private static void enemy_target(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Targetable A")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_02 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _2) >= cd_02)
						{
							_2 = Time.frameCount;
							Debug.Log("Targetable A [NCS:" + cd_02 + "-" + _2 + "]");
						}
					});
					break;
				}
			}
		}

		//04 level timer
		public static int _4;
		private static void level(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Level up timer: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 8;
					for (int s = 0; s < 8; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_04 == 0) continue;
					//c.Emit(OpCodes.Ldarg_0);
					//c.Emit(OpCodes.Ldfld, typeof(HUDManager).GetField("SetPlayerLevelSmoothly.timeAtStart", BindingFlags.NonPublic | BindingFlags.Instance));
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _4) >= cd_04)
						{
							_4 = Time.frameCount;
							Debug.Log("Level up timer: (timer) [NCS:" + cd_04 + "-" + _4 + "]");
						}
					});
					break;
				}
			}
		}

		//06 called teleport function
		public static int _6;
		private static void pcb_teleport(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Called teleport function on ")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_06 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<PlayerControllerB>>((self) =>
					{
						if ((Time.frameCount - _6) >= cd_06)
						{
							_6 = Time.frameCount;
							Debug.Log("Called teleport function on " + self.gameObject.name + " [NCS:" + cd_06 + "-" + _6 + "]");
						}
					});
					break;
				}
			}
		}

		//07 company desk timer
		public static int[] _7 = new int[2];
		private static void desk(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Desk: Waiting to grab the items on the desk; {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_07 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldfld, typeof(DepositItemsDesk).GetField("grabObjectsTimer", BindingFlags.NonPublic | BindingFlags.Instance));
					c.EmitDelegate<System.Action<float>>((timer) =>
					{
						if ((Time.frameCount - _7[0]) >= cd_07)
						{
							_7[0] = Time.frameCount;
							Debug.Log("Desk: Waiting to grab the items on the desk; " + timer + " [NCS:" + cd_07 + "-" + _7[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Desk: no objects on counter, waiting with door open; {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_07 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldfld, typeof(DepositItemsDesk).GetField("waitingWithDoorOpenTimer", BindingFlags.NonPublic | BindingFlags.Instance));
					c.EmitDelegate<System.Action<float>>((timer) =>
					{
						if ((Time.frameCount - _7[1]) >= cd_07)
						{
							_7[1] = Time.frameCount;
							Debug.Log("Desk: no objects on counter, waiting with door open; " + timer + " [NCS:" + cd_07 + "-" + _7[1] + "]");
						}
					});
				}
			}
		}

		//08 shower
		public static int[] _8 = new int[2];
		private static void shower(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Shower is running with players inside!")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_08 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _8[0]) >= cd_08)
						{
							_8[0] = Time.frameCount;
							Debug.Log("Shower is running with players inside! [NCS:" + cd_08 + "-" + _8[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "spray decal #{0} found as child of {1}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 11; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_08 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldfld, typeof(ShowerTrigger).GetField("cleanDecalIndex", BindingFlags.NonPublic | BindingFlags.Instance));
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldfld, typeof(ShowerTrigger).GetField("playersInShower", BindingFlags.NonPublic | BindingFlags.Instance));
					c.Emit(OpCodes.Ldloc_1);
					c.EmitDelegate<System.Action<int, System.Collections.Generic.List<PlayerControllerB>, int>>((num, players, index) =>
					{
						if ((Time.frameCount - _8[1]) >= cd_08)
						{
							_8[1] = Time.frameCount;
							Debug.Log("spray decal #" + num + " found as child of " + players[index] + " [NCS:" + cd_08 + "-" + _8[1] + "]");
						}
					});
				}
			}
		}

		//09 agent speed
		public static int _9;
		private static void giant(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "agent speed: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 7; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_09 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<EnemyAI>>((self) =>
					{
						if ((Time.frameCount - _9) >= cd_09)
						{
							_9 = Time.frameCount;
							Debug.Log("agent speed: " + self.agent.speed + " [NCS:" + cd_09 + "-" + _9 + "]");
						}
					});
					break;
				}
			}
		}

		//10 secondary use a-g
		public static int[] _10 = new int[7];
		private static void pcb_secondary(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "secondary use A")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_10 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _10[0]) >= cd_10)
						{
							_10[0] = Time.frameCount;
							Debug.Log("secondary use A [NCS:" + cd_10 + "-" + _10[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "secondary use B")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_10 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _10[1]) >= cd_10)
						{
							_10[1] = Time.frameCount;
							Debug.Log("secondary use B [NCS:" + cd_10 + "-" + _10[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "secondary use C")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_10 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _10[2]) >= cd_10)
						{
							_10[2] = Time.frameCount;
							Debug.Log("secondary use C [NCS:" + cd_10 + "-" + _10[2] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "secondary use D")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_10 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _10[3]) >= cd_10)
						{
							_10[3] = Time.frameCount;
							Debug.Log("secondary use D [NCS:" + cd_10 + "-" + _10[3] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "secondary use E")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_10 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _10[4]) >= cd_10)
						{
							_10[4] = Time.frameCount;
							Debug.Log("secondary use E [NCS:" + cd_10 + "-" + _10[4] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "secondary use F")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_10 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _10[5]) >= cd_10)
						{
							_10[5] = Time.frameCount;
							Debug.Log("secondary use F [NCS:" + cd_10 + "-" + _10[5] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "secondary use G")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_10 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _10[6]) >= cd_10)
						{
							_10[6] = Time.frameCount;
							Debug.Log("secondary use G [NCS:" + cd_10 + "-" + _10[6] + "]");
						}
					});
				}
			}
		}

		//11 player leave web
		public static int _11;
		private static void spider(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Player leave web called")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_11 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _11) >= cd_11)
						{
							_11 = Time.frameCount;
							Debug.Log("Player leave web called [NCS:" + cd_11 + "-" + _11 + "]");
						}
					});
					break;
				}
			}
		}

		//12 zapgun 1
		public static int[] _12 = new int[9];
		private static void patch_1(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Scan A")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_12 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _12[0]) >= cd_12)
						{
							_12[0] = Time.frameCount;
							Debug.Log("Scan A [NCS:" + cd_12 + "-" + _12[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Scan B")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_12 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _12[1]) >= cd_12)
						{
							_12[1] = Time.frameCount;
							Debug.Log("Scan B [NCS:" + cd_12 + "-" + _12[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Got shockable transform name : ")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 7;
					for (int s = 0; s < 7; s = s + 1)
					{
						if (s != 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					}
					if (cd_12 == 0) continue;
					//c.Emit(OpCodes.Ldloc_S, 4);
					c.EmitDelegate<System.Action<IShockableWithGun>>((component) =>
					{
						if ((Time.frameCount - _12[2]) >= cd_12)
						{
							_12[2] = Time.frameCount;
							Debug.Log("Got shockable transform name : " + component.GetShockableTransform().gameObject.name + " [NCS:" + cd_12 + "-" + _12[2] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Zap gun light off!!!")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_12 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _12[3]) >= cd_12)
						{
							_12[3] = Time.frameCount;
							Debug.Log("Zap gun light off!!! [NCS:" + cd_12 + "-" + _12[3] + "]");
						}
					});
				}
			}
		}

		//12 zapgun 2
		private static void patch_2(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Target position: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 5; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_12 == 0) continue;
					c.Emit(OpCodes.Ldarg_2);
					c.EmitDelegate<System.Action<Vector3>>((position) =>
					{
						if ((Time.frameCount - _12[4]) >= cd_12)
						{
							_12[4] = Time.frameCount;
							Debug.Log("Target position: " + position + " [NCS:" + cd_12 + "-" + _12[4] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Gun not meeting conditions to zap; {0}; {1}; {2}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 21; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_12 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldfld, typeof(PatcherTool).GetField("gunOverheat"));
					c.Emit(OpCodes.Ldloc_0); //position
					c.Emit(OpCodes.Ldarg_2); //target_position
					c.EmitDelegate<System.Action<float, Vector3, Vector3>>((num, position, target_position) =>
					{
						if ((Time.frameCount - _12[5]) >= cd_12)
						{
							_12[5] = Time.frameCount;
							Debug.Log("Gun not meeting conditions to zap; " + (num > 2f) + "; " + (Vector3.Distance(position, target_position) < 0.7f) + "; " + (Physics.Linecast(position, target_position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore)) + " [NCS:" + cd_12 + "-" + _12[5] + "]");
						}
					});
				}
			}
		}

		//12 zapgun 3
		private static void patch_3(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Stop shocking gun")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_12 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _12[6]) >= cd_12)
						{
							_12[6] = Time.frameCount;
							Debug.Log("Stop shocking gun [NCS:" + cd_12 + "-" + _12[6] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Start scanning gun")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_12 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _12[7]) >= cd_12)
						{
							_12[7] = Time.frameCount;
							Debug.Log("Start scanning gun [NCS:" + cd_12 + "-" + _12[7] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Use patcher tool")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_12 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _12[8]) >= cd_12)
						{
							_12[8] = Time.frameCount;
							Debug.Log("Use patcher tool [NCS:" + cd_12 + "-" + _12[8] + "]");
						}
					});
				}
			}
		}

		//13 walkie 1
		public static int[] _13 = new int[9];
		private static void radio_1(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Set local client speaking on walkie talkie: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 5; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_13 == 0) continue;
					c.Emit(OpCodes.Ldarg_1);
					c.EmitDelegate<System.Action<bool>>((bol) =>
					{
						if ((Time.frameCount - _13[0]) >= cd_13)
						{
							_13[0] = Time.frameCount;
							Debug.Log("Set local client speaking on walkie talkie: " + bol + " [NCS:" + cd_13 + "-" + _13[0] + "]");
						}
					});
					break;
				}
			}
		}

		//13 walkie 2
		private static void radio_2(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Walkie talkie A")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_13 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _13[1]) >= cd_13)
						{
							_13[1] = Time.frameCount;
							Debug.Log("Walkie talkie A [NCS:" + cd_13 + "-" + _13[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Walkie talkie #{0} {1} B")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 10;
					for (int s = 0; s < 10; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_13 == 0) continue;
					c.Emit(OpCodes.Ldsfld, typeof(WalkieTalkie).GetField("allWalkieTalkies"));
					c.Emit(OpCodes.Ldloc_0);
					c.EmitDelegate<System.Action<System.Collections.Generic.List<WalkieTalkie>, int>>((list, index) =>
					{
						if ((Time.frameCount - _13[2]) >= cd_13)
						{
							_13[2] = Time.frameCount;
							Debug.Log("Walkie talkie #" + index + " " + list[index].gameObject.name + " B [NCS:" + cd_13 + "-" + _13[2] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "is walkie being used: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 8;
					for (int s = 0; s < 8; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_13 == 0) continue;
					c.Emit(OpCodes.Ldsfld, typeof(WalkieTalkie).GetField("allWalkieTalkies"));
					c.Emit(OpCodes.Ldloc_0);
					c.EmitDelegate<System.Action<System.Collections.Generic.List<WalkieTalkie>, int>>((list, index) =>
					{
						if ((Time.frameCount - _13[3]) >= cd_13)
						{
							_13[3] = Time.frameCount;
							Debug.Log("is walkie being used: " + list[index].isBeingUsed + " [NCS:" + cd_13 + "-" + _13[3] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Walkie talkie #{0}  {1} C")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 10;
					for (int s = 0; s < 10; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_13 == 0) continue;
					c.Emit(OpCodes.Ldsfld, typeof(WalkieTalkie).GetField("allWalkieTalkies"));
					c.Emit(OpCodes.Ldloc_0);
					c.EmitDelegate<System.Action<System.Collections.Generic.List<WalkieTalkie>, int>>((list, index) =>
					{
						if ((Time.frameCount - _13[4]) >= cd_13)
						{
							_13[4] = Time.frameCount;
							Debug.Log("Walkie talkie #" + index + " " + list[index].gameObject.name + " C [NCS:" + cd_13 + "-" + _13[4] + "]");
						}
					});
				}
			}
		}

		//13 walkie 3
		private static void radio_3(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "False A")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_13 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _13[5]) >= cd_13)
						{
							_13[5] = Time.frameCount;
							Debug.Log("False A [NCS:" + cd_13 + "-" + _13[5] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "False B")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_13 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _13[6]) >= cd_13)
						{
							_13[6] = Time.frameCount;
							Debug.Log("False B [NCS:" + cd_13 + "-" + _13[6] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "False C")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_13 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _13[7]) >= cd_13)
						{
							_13[7] = Time.frameCount;
							Debug.Log("False C [NCS:" + cd_13 + "-" + _13[7] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "{0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 6;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_13 == 0) continue;
					c.Emit(OpCodes.Ldarg_1);
					c.EmitDelegate<System.Action<WalkieTalkie>>((self) =>
					{
						if ((Time.frameCount - _13[8]) >= cd_13)
						{
							_13[8] = Time.frameCount;
							Debug.Log(self.isPocketed + " [NCS:" + cd_13 + "-" + _13[8] + "]");
						}
					});
				}
			}
		}

		//14 loud horn
		public static int _14;
		private static void cord(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "HOLD horn local client called")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_14 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _14) >= cd_14)
						{
							_14 = Time.frameCount;
							Debug.Log("HOLD horn local client called [NCS:" + cd_14 + "-" + _14 + "]");
						}
					});
					break;
				}
			}
		}

		//15 mouth dog 1
		public static int[] _15 = new int[6];
		private static void dog_1(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "dog '{0}': Heard noise! Distance: {1} meters")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 8; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_15 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldloc_0);
					c.EmitDelegate<System.Action<MouthDogAI, float>>((self, num) =>
					{
						if ((Time.frameCount - _15[0]) >= cd_15)
						{
							_15[0] = Time.frameCount;
							Debug.Log("dog '" + self.gameObject.name + "': Heard noise! Distance: " + num + " meters [NCS:" + cd_15 + "-" + _15[0] + "]");
						}
					});
					break;
				}
			}
		}

		//15 mouth dog 2
		private static void dog_2(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Mouth dog targetPos 1: {0}; distanceToNoise: {1}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 7; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_15 == 0) continue;
					c.Emit(OpCodes.Ldarg_1);
					c.Emit(OpCodes.Ldarg_2);
					c.EmitDelegate<System.Action<Vector3, float>>((position, distance) =>
					{
						if ((Time.frameCount - _15[1]) >= cd_15)
						{
							_15[1] = Time.frameCount;
							Debug.Log("Mouth dog targetPos 1: " + position + "; distanceToNoise: " + distance + " [NCS:" + cd_15 + "-" + _15[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Mouth dog targetPos 2: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 5; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_15 == 0) continue;
					c.Emit(OpCodes.Ldarg_1);
					c.EmitDelegate<System.Action<Vector3>>((position) =>
					{
						if ((Time.frameCount - _15[2]) >= cd_15)
						{
							_15[2] = Time.frameCount;
							Debug.Log("Mouth dog targetPos 2: " + position + " [NCS:" + cd_15 + "-" + _15[2] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Dog lastheardnoisePosition: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_15 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldfld, typeof(MouthDogAI).GetField("lastHeardNoisePosition", BindingFlags.NonPublic | BindingFlags.Instance));
					c.EmitDelegate<System.Action<Vector3>>((position) =>
					{
						if ((Time.frameCount - _15[3]) >= cd_15)
						{
							_15[3] = Time.frameCount;
							Debug.Log("Dog lastheardnoisePosition: " + position + " [NCS:" + cd_15 + "-" + _15[3] + "]");
						}
					});
				}
			}
		}

		//15 mouth dog 3
		private static void dog_3(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Setting lastHeardNoisePosition to {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 5; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_15 == 0) continue;
					c.Emit(OpCodes.Ldarg_1);
					c.EmitDelegate<System.Action<Vector3>>((position) =>
					{
						if ((Time.frameCount - _15[4]) >= cd_15)
						{
							_15[4] = Time.frameCount;
							Debug.Log("Setting lastHeardNoisePosition to " + position + " [NCS:" + cd_15 + "-" + _15[4] + "]");
						}
					});
					break;
				}
			}
		}

		//15 mouth dog 4
		private static void dog_4(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Ending lunge")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_15 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _15[5]) >= cd_15)
						{
							_15[5] = Time.frameCount;
							Debug.Log("Ending lunge [NCS:" + cd_15 + "-" + _15[5] + "]");
						}
					});
					break;
				}
			}
		}

		//17 hoarder bug
		public static int[] _17 = new int[2];
		private static void hoarder(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == ": Setting target object and going towards it.")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 3;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[(n - 3) + s].OpCode = OpCodes.Nop;
					if (cd_17 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<HoarderBugAI>>((self) =>
					{
						if ((Time.frameCount - _17[0]) >= cd_17)
						{
							_17[0] = Time.frameCount;
							Debug.Log(self.gameObject.name + " : Setting target object and going towards it. [NCS:" + cd_17 + "-" + _17[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == ": i found an object but cannot reach it (or it has been taken by another bug): ")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 5;
					for (int s = 0; s < 8; s = s + 1) l.Instrs[(n - 3) + s].OpCode = OpCodes.Nop;
					if (cd_17 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldarg_1);
					c.EmitDelegate<System.Action<HoarderBugAI, GameObject>>((self, item) =>
					{
						if ((Time.frameCount - _17[1]) >= cd_17)
						{
							_17[1] = Time.frameCount;
							Debug.Log(self.gameObject.name + " : i found an object but cannot reach it (or it has been taken by another bug): " + item.name + " [NCS:" + cd_17 + "-" + _17[1] + "]");
						}
					});
				}
			}
		}

		//18 random nav
		public static int _18;
		private static void random(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Unable to get random nav mesh position in radius! Returning old pos")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_18 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _18) >= cd_18)
						{
							_18 = Time.frameCount;
							Debug.Log("Unable to get random nav mesh position in radius! Returning old pos [NCS:" + cd_18 + "-" + _18 + "]");
						}
					});
					break;
				}
			}
		}

		//19 quicksand 1
		public static int[] _19 = new int[5];
		private static void sand_1(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Set local player to sinking!")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_19 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _19[0]) >= cd_19)
						{
							_19[0] = Time.frameCount;
							Debug.Log("Set local player to sinking! [NCS:" + cd_19 + "-" + _19[0] + "]");
						}
					});
					break;
				}
			}
		}

		//19 quicksand 2
		private static void sand_2(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Quicksand is not sinking local player!")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_19 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _19[1]) >= cd_19)
						{
							_19[1] = Time.frameCount;
							Debug.Log("Quicksand is not sinking local player! [NCS:" + cd_19 + "-" + _19[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Quicksand is sinking local player!")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_19 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _19[2]) >= cd_19)
						{
							_19[2] = Time.frameCount;
							Debug.Log("Quicksand is sinking local player! [NCS:" + cd_19 + "-" + _19[2] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Quicksand is sinking local player! B")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_19 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _19[3]) >= cd_19)
						{
							_19[3] = Time.frameCount;
							Debug.Log("Quicksand is sinking local player! B [NCS:" + cd_19 + "-" + _19[3] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Quicksand is sinking local player! C")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_19 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _19[4]) >= cd_19)
						{
							_19[4] = Time.frameCount;
							Debug.Log("Quicksand is sinking local player! C [NCS:" + cd_19 + "-" + _19[4] + "]");
						}
					});
				}
			}
		}

		//20 not targetable
		public static int _20;
		private static void enemy_not_targetable(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Player is not targetable")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_20 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _20) >= cd_20)
						{
							_20 = Time.frameCount;
							Debug.Log("Player is not targetable [NCS:" + cd_20 + "-" + _20 + "]");
						}
					});
					break;
				}
			}
		}

		//21 masked
		public static int[] _21 = new int[3];
		private static void masked(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Noise heard relative loudness: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 5;
					for (int s = 0; s < 5; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_21 == 0) continue;
					c.Emit(OpCodes.Ldloc_1);
					c.EmitDelegate<System.Action<float>>((num) =>
					{
						if ((Time.frameCount - _21[0]) >= cd_21)
						{
							_21[0] = Time.frameCount;
							Debug.Log("Noise heard relative loudness: " + num + " [NCS:" + cd_21 + "-" + _21[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Can't hear noise reason A")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_21 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _21[1]) >= cd_21)
						{
							_21[1] = Time.frameCount;
							Debug.Log("Can't hear noise reason A [NCS:" + cd_21 + "-" + _21[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Can't hear noise reason B")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_21 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _21[2]) >= cd_21)
						{
							_21[2] = Time.frameCount;
							Debug.Log("Can't hear noise reason B [NCS:" + cd_21 + "-" + _21[2] + "]");
						}
					});
				}
			}
		}

		//22 fox 1
		public static int[] _22 = new int[15];
		private static void bushwolf_update(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Fox spotted meter: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 6;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<BushWolfEnemy>>((self) =>
					{
						if ((Time.frameCount - _22[0]) >= cd_22)
						{
							_22[0] = Time.frameCount;
							Debug.Log("Fox spotted meter: " + self.spottedMeter + " [NCS:" + cd_22 + "-" + _22[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Fox A")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[1]) >= cd_22)
						{
							_22[1] = Time.frameCount;
							Debug.Log("Fox A [NCS:" + cd_22 + "-" + _22[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Fox B")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[2]) >= cd_22)
						{
							_22[2] = Time.frameCount;
							Debug.Log("Fox B [NCS:" + cd_22 + "-" + _22[2] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Fox C; {0}; {1}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 7;
					for (int s = 0; s < 7; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.Emit(OpCodes.Ldloc, 14);
					c.Emit(OpCodes.Ldloc, 15);
					c.EmitDelegate<System.Action<float, float>>((distance, angle) =>
					{
						if ((Time.frameCount - _22[3]) >= cd_22)
						{
							_22[3] = Time.frameCount;
							Debug.Log("Fox C; " + distance + "; " + angle + " [NCS:" + cd_22 + "-" + _22[3] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Fox D")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[4]) >= cd_22)
						{
							_22[4] = Time.frameCount;
							Debug.Log("Fox D [NCS:" + cd_22 + "-" + _22[4] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Fox E")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[5]) >= cd_22)
						{
							_22[5] = Time.frameCount;
							Debug.Log("Fox E [NCS:" + cd_22 + "-" + _22[5] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Fox F")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[6]) >= cd_22)
						{
							_22[6] = Time.frameCount;
							Debug.Log("Fox F [NCS:" + cd_22 + "-" + _22[6] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Fox G")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[7]) >= cd_22)
						{
							_22[7] = Time.frameCount;
							Debug.Log("Fox G [NCS:" + cd_22 + "-" + _22[7] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Fox H")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[8]) >= cd_22)
						{
							_22[8] = Time.frameCount;
							Debug.Log("Fox H [NCS:" + cd_22 + "-" + _22[8] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Fox I")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[9]) >= cd_22)
						{
							_22[9] = Time.frameCount;
							Debug.Log("Fox I [NCS:" + cd_22 + "-" + _22[9] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Fox J")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[10]) >= cd_22)
						{
							_22[10] = Time.frameCount;
							Debug.Log("Fox J [NCS:" + cd_22 + "-" + _22[10] + "]");
						}
					});
				}
			}
		}

		//22 fox 2
		private static void bushwolf_big(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Bush wolf: No game objects found with spore tag; cancelling")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[11]) >= cd_22)
						{
							_22[11] = Time.frameCount;
							Debug.Log("Bush wolf: No game objects found with spore tag; cancelling [NCS:" + cd_22 + "-" + _22[11] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "{0}: Mold spore {1} at {2} surrounded by {3}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 32;
					for (int s = 0; s < 32; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.Emit(OpCodes.Ldloc, 6);
					c.Emit(OpCodes.Ldloc_0);
					c.Emit(OpCodes.Ldloc_3);
					c.EmitDelegate<System.Action<int, GameObject[], int>>((index, objects, num) =>
					{
						if ((Time.frameCount - _22[12]) >= cd_22)
						{
							_22[12] = Time.frameCount;
							Debug.Log(index + ": Mold spore " + objects[index].gameObject.name + " at " + objects[index].transform.position + " surrounded by " + num + " [NCS:" + cd_22 + "-" + _22[12] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Bush wolf: Most surrounding spores is {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 5;
					for (int s = 0; s < 5; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.Emit(OpCodes.Ldloc_2);
					c.EmitDelegate<System.Action<int>>((num) =>
					{
						if ((Time.frameCount - _22[13]) >= cd_22)
						{
							_22[13] = Time.frameCount;
							Debug.Log("Bush wolf: Most surrounding spores is " + num + " [NCS:" + cd_22 + "-" + _22[13] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Bush wolf: All spores found were lone spores; cancelling")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_22 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _22[14]) >= cd_22)
						{
							_22[14] = Time.frameCount;
							Debug.Log("Bush wolf: All spores found were lone spores; cancelling [NCS:" + cd_22 + "-" + _22[14] + "]");
						}
					});
				}
			}
		}

		//23 bush 1
		public static int[] _23 = new int[11];
		private static void bush_destroy(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "weeds found at pos {0}: {1}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 7;
					for (int s = 0; s < 7; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_23 == 0) continue;
					c.Emit(OpCodes.Ldarg_1);
					c.Emit(OpCodes.Ldloc_0);
					c.EmitDelegate<System.Action<Vector3, int>>((position, num) =>
					{
						if ((Time.frameCount - _23[0]) >= cd_23)
						{
							_23[0] = Time.frameCount;
							Debug.Log("weeds found at pos " + position + ": " + num + " [NCS:" + cd_23 + "-" + _23[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Index: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 5;
					for (int s = 0; s < 5; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_23 == 0) continue;
					c.Emit(OpCodes.Ldloc_2);
					c.EmitDelegate<System.Action<int>>((num) =>
					{
						if ((Time.frameCount - _23[1]) >= cd_23)
						{
							_23[1] = Time.frameCount;
							Debug.Log("Index: " + num + " [NCS:" + cd_23 + "-" + _23[1] + "]");
						}
					});
				}
			}
		}

		//23 bush 2
		private static void bush_generate(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Growing back mold at index {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 8;
					for (int s = 0; s < 8; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_23 == 0) continue;
					c.Emit(OpCodes.Ldloc, 12);
					c.Emit(OpCodes.Ldloc, 24);
					c.EmitDelegate<System.Action<System.Collections.Generic.List<MoldSpore>, int>>((spores, index) =>
					{
						if ((Time.frameCount - _23[10]) >= cd_23)
						{
							_23[10] = Time.frameCount;
							Debug.Log("Growing back mold at index " + spores[index].generationId + " [NCS:" + cd_23 + "-" + _23[10] + "]");
						}
					});
					break;
				}
			}
		}

		//25 truck 1
		public static int[] _25 = new int[6];
		private static void truck_collect(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Collect items in truck A")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_25 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _25[0]) >= cd_25)
						{
							_25[0] = Time.frameCount;
							Debug.Log("Collect items in truck A [NCS:" + cd_25 + "-" + _25[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Collect items in truck B; {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 7;
					for (int s = 0; s < 7; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_25 == 0) continue;
					c.Emit(OpCodes.Ldloc_0);
					c.EmitDelegate<System.Action<Collider[]>>((list) =>
					{
						if ((Time.frameCount - _25[1]) >= cd_25)
						{
							_25[1] = Time.frameCount;
							Debug.Log("Collect items in truck B; " + list.Length + " [NCS:" + cd_25 + "-" + _25[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Collect items in truck C; {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 7;
					for (int s = 0; s < 7; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_25 == 0) continue;
					c.Emit(OpCodes.Ldloc_1);
					c.EmitDelegate<System.Action<GrabbableObject>>((go) =>
					{
						if ((Time.frameCount - _25[2]) >= cd_25)
						{
							_25[2] = Time.frameCount;
							Debug.Log("Collect items in truck C; " + (go != null) + " [NCS:" + cd_25 + "-" + _25[2] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "{0}; {1}; {2}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 22;
					for (int s = 0; s < 22; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_25 == 0) continue;
					c.Emit(OpCodes.Ldloc_1);
					c.Emit(OpCodes.Ldloc_0);
					c.Emit(OpCodes.Ldloc_2);
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<GrabbableObject, Collider[], int, VehicleController>>((go, list, index, self) =>
					{
						if ((Time.frameCount - _25[3]) >= cd_25)
						{
							_25[3] = Time.frameCount;
							Debug.Log(!go.isHeld + "; " + !go.isHeldByEnemy + "; " + (list[index].transform.parent == self.transform) + " [NCS:" + cd_25 + "-" + _25[3] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Magneted? : {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 6;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_25 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<VehicleController>>((self) =>
					{
						if ((Time.frameCount - _25[4]) >= cd_25)
						{
							_25[4] = Time.frameCount;
							Debug.Log("Magneted? : " + self.magnetedToShip + " [NCS:" + cd_25 + "-" + _25[4] + "]");
						}
					});
				}
			}
		}

		//25 truck 2
		private static void truck_audio(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Play collision audio with type {0} A")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 5;
					for (int s = 0; s < 5; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_25 == 0) continue;
					c.Emit(OpCodes.Ldarg_2);
					c.EmitDelegate<System.Action<int>>((num) =>
					{
						if ((Time.frameCount - _25[5]) >= cd_25)
						{
							_25[5] = Time.frameCount;
							Debug.Log("Play collision audio with type " + num + " A [NCS:" + cd_25 + "-" + _25[5] + "]");
						}
					});
					break;
				}
			}
		}

		//26 ropes
		public static int _26;
		private static void ropes(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Setting position of ropes")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_26 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _26) >= cd_26)
						{
							_26 = Time.frameCount;
							Debug.Log("Setting position of ropes [NCS:" + cd_26 + "-" + _26 + "]");
						}
					});
					break;
				}
			}
		}

		//28 grabinvalidated
		public static int _28;
		private static void grab(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "grabInvalidated: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 6;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_28 == 0) continue;
					c.Emit(OpCodes.Ldloc_1);
					c.Emit(OpCodes.Ldfld, typeof(GameNetcodeStuff.PlayerControllerB).GetField("grabInvalidated", BindingFlags.NonPublic | BindingFlags.Instance));
					c.EmitDelegate<System.Action<bool>>((grabinvalidated) =>
					{
						if ((Time.frameCount - _28) >= cd_28)
						{
							_28 = Time.frameCount;
							Debug.Log("grabInvalidated: " + grabinvalidated + " [NCS:" + cd_28 + "-" + _28 + "]");
						}
					});
					break;
				}
			}
		}

		//30 item meshes
		public static int _30;
		private static void item_meshes(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "DISABLING/ENABLING SKINNEDMESH: ")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 8;
					for (int s = 0; s < 8; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_30 == 0) continue;
					c.Emit(OpCodes.Ldloc_1);
					c.Emit(OpCodes.Ldloc_3);
					c.EmitDelegate<System.Action<SkinnedMeshRenderer[], int>>((meshes, index) =>
					{
						if ((Time.frameCount - _30) >= cd_30)
						{
							_30 = Time.frameCount;
							Debug.Log("DISABLING/ENABLING SKINNEDMESH: " + meshes[index].gameObject.name + " [NCS:" + cd_30 + "-" + _30 + "]");
						}
					});
					break;
				}
			}
		}

		//31 weedkiller
		public static int _31;
		private static void spray_1(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Spraying, depleting tank")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_31 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _31) >= cd_31)
						{
							_31 = Time.frameCount;
							Debug.Log("Spraying, depleting tank [NCS:" + cd_31 + "-" + _31 + "]");
						}
					});
					break;
				}
			}
		}

		//32 spraypaint
		public static int[] _32 = new int[4];
		private static void spray_2(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Start using spray")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_32 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _32[0]) >= cd_32)
						{
							_32[0] = Time.frameCount;
							Debug.Log("Start using spray [NCS:" + cd_32 + "-" + _32[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Spray empty")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_32 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _32[1]) >= cd_32)
						{
							_32[1] = Time.frameCount;
							Debug.Log("Spray empty [NCS:" + cd_32 + "-" + _32[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Spray not empty")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_32 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _32[2]) >= cd_32)
						{
							_32[2] = Time.frameCount;
							Debug.Log("Spray not empty [NCS:" + cd_32 + "-" + _32[2] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Stop using spray")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 2;
					for (int s = 0; s < 2; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_32 == 0) continue;
					c.EmitDelegate<System.Action>(() =>
					{
						if ((Time.frameCount - _32[3]) >= cd_32)
						{
							_32[3] = Time.frameCount;
							Debug.Log("Stop using spray [NCS:" + cd_32 + "-" + _32[3] + "]");
						}
					});
				}
			}
		}

		//33 flashlight
		public static int[] _33 = new int[2];
		private static void flashlight(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Flashlight click. playerheldby null?: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 8;
					for (int s = 0; s < 8; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_33 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<FlashlightItem>>((self) =>
					{
						if ((Time.frameCount - _33[0]) >= cd_33)
						{
							_33[0] = Time.frameCount;
							Debug.Log("Flashlight click. playerheldby null?: " + (self.playerHeldBy != null) + " [NCS:" + cd_33 + "-" + _33[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Flashlight being disabled or enabled: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 5;
					for (int s = 0; s < 5 ; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_33 == 0) continue;
					c.Emit(OpCodes.Ldarg_1);
					c.EmitDelegate<System.Action<bool>>((bol) =>
					{
						if ((Time.frameCount - _33[1]) >= cd_33)
						{
							_33[1] = Time.frameCount;
							Debug.Log("Flashlight being disabled or enabled: " + bol + " [NCS:" + cd_33 + "-" + _33[1] + "]");
						}
					});
				}
			}
		}

		//34 item being used
		public static int _34;
		private static void item_used(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Is being used set to {0} by RPC")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 5;
					for (int s = 0; s < 5; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_34 == 0) continue;
					c.Emit(OpCodes.Ldarg_1);
					c.EmitDelegate<System.Action<bool>>((bol) =>
					{
						if ((Time.frameCount - _34) >= cd_34)
						{
							_34 = Time.frameCount;
							Debug.Log("Is being used set to " + bol + " by RPC [NCS:" + cd_34 + "-" + _34 + "]");
						}
					});
					break;
				}
			}
		}

		//35 masked elevator
		public static int[] _35 = new int[4];
		private static void masked_elevator(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "goUp: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 5;
					for (int s = 0; s < 5; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_35 == 0) continue;
					c.Emit(OpCodes.Ldarg_1);
					c.EmitDelegate<System.Action<bool>>((bol) =>
					{
						if ((Time.frameCount - _35[0]) >= cd_35)
						{
							_35[0] = Time.frameCount;
							Debug.Log("goUp: " + bol + " [NCS:" + cd_35 + "-" + _35[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "{0}, {1}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 19;
					for (int s = 0; s < 19; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_35 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldfld, typeof(MaskedPlayerEnemy).GetField("elevatorScript", BindingFlags.NonPublic | BindingFlags.Instance));
					c.EmitDelegate<System.Action<MineshaftElevatorController>>((elevator) =>
					{
						if ((Time.frameCount - _35[1]) >= cd_35)
						{
							_35[1] = Time.frameCount;
							Debug.Log(elevator.elevatorFinishedMoving + ", (bool) [NCS:" + cd_35 + "-" + _35[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "goUp: {0}, elevatormovingdown: {1}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 9;
					for (int s = 0; s < 9; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_35 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldfld, typeof(MaskedPlayerEnemy).GetField("elevatorScript", BindingFlags.NonPublic | BindingFlags.Instance));
					c.Emit(OpCodes.Ldarg_1);
					c.EmitDelegate<System.Action<MineshaftElevatorController, bool>>((elevator, bol) =>
					{
						if ((Time.frameCount - _35[2]) >= cd_35)
						{
							_35[2] = Time.frameCount;
							Debug.Log("goUp: " + bol + ", elevatormovingdown: " + elevator.elevatorMovingDown + " [NCS:" + cd_35 + "-" + _35[2] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "{0}, {1}, {2}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 24;
					for (int s = 0; s < 24; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_35 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldfld, typeof(MaskedPlayerEnemy).GetField("elevatorScript", BindingFlags.NonPublic | BindingFlags.Instance));
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldarg_1);
					c.EmitDelegate<System.Action<MineshaftElevatorController, MaskedPlayerEnemy, bool>>((elevator, self, bol) =>
					{
						if ((Time.frameCount - _35[3]) >= cd_35)
						{
							_35[3] = Time.frameCount;
							Debug.Log(elevator.elevatorDoorOpen + ", " + (Vector3.Distance(self.transform.position, elevator.elevatorInsidePoint.position) < 1f) + ", " + (elevator.elevatorMovingDown == bol) + " [NCS:" + cd_35 + "-" + _35[3] + "]");
						}
					});
				}
			}
		}

		//36 item spawning 1
		public static int[] _36 = new int[8];
		private static void item_spawn_1(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Item spawning: {0} ; item parent : {1}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 9;
					for (int s = 0; s < 9; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_36 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<GrabbableObject>>((item) =>
					{
						if ((Time.frameCount - _36[0]) >= cd_36)
						{
							_36[0] = Time.frameCount;
							Debug.Log("Item spawning: " + item.itemProperties.itemName + " ; item parent : " + item.transform.parent + " [NCS:" + cd_36 + "-" + _36[0] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Item spawning: ")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 7;
					for (int s = 0; s < 7; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_36 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<GrabbableObject>>((item) =>
					{
						if ((Time.frameCount - _36[1]) >= cd_36)
						{
							_36[1] = Time.frameCount;
							Debug.Log("Item spawning: " + item.itemProperties.itemName + " ; item parent : null [NCS:" + cd_36 + "-" + _36[1] + "]");
						}
					});
				}
			}
		}

		//36 item spawning 2
		private static void item_fall(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Start falling position: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 6;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_36 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<GrabbableObject>>((item) =>
					{
						if ((Time.frameCount - _36[2]) >= cd_36)
						{
							_36[2] = Time.frameCount;
							Debug.Log("Start falling position: " + item.startFallingPosition + " [NCS:" + cd_36 + "-" + _36[2] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "global startposition falltoground for object {0}: {1}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 8;
					for (int s = 0; s < 8; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_36 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldloc_0);
					c.EmitDelegate<System.Action<GrabbableObject, Vector3>>((item, startposition) =>
					{
						if ((Time.frameCount - _36[3]) >= cd_36)
						{
							_36[3] = Time.frameCount;
							Debug.Log("global startposition falltoground for object " + item.gameObject.name + ": " + startposition + " [NCS:" + cd_36 + "-" + _36[3] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Item ")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 31;
					for (int s = -4; s < 31; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_36 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldloca_S, 1);
					c.EmitDelegate<System.Action<GrabbableObject, RaycastHit>>((item, hitinfo) =>
					{
						if ((Time.frameCount - _36[4]) >= cd_36)
						{
							_36[4] = Time.frameCount;
							Debug.Log("Item " + item.itemProperties.itemName + " landed on : " + hitinfo.collider.name + " / " + hitinfo.transform.gameObject.name + " [NCS:" + cd_36 + "-" + _36[4] + "]");
						}
					});
				}
			}
		}

		//36 item spawning 3
		private static void item_spawn_2(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Item {0} #{1} spawn position: {2}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 17;
					for (int s = 0; s < 17; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_36 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldloc_1);
					c.Emit(OpCodes.Ldloc_2);
					c.Emit(OpCodes.Ldloc_S, 9);
					c.EmitDelegate<System.Action<StartOfRound, int[], int[], int>>((sor, array1, array2, z) =>
					{
						if ((Time.frameCount - _36[5]) >= cd_36)
						{
							_36[5] = Time.frameCount;
							Debug.Log("Item " + sor.allItemsList.itemsList[array1[z]].itemName + " #" + r + " spawn position: " + array2[z] + " [NCS:" + cd_36 + "-" + _36[5] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Setting scrap value for item: {0}: {1}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 10;
					for (int s = 0; s < 10; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_36 == 0) continue;
					c.Emit(OpCodes.Ldloc_0);
					c.Emit(OpCodes.Ldloc_S, 4);
					c.Emit(OpCodes.Ldloc_S, 7);
					c.EmitDelegate<System.Action<GrabbableObject, int[], int>>((item, array, z) =>
					{
						if ((Time.frameCount - _36[6]) >= cd_36)
						{
							_36[6] = Time.frameCount;
							Debug.Log("Setting scrap value for item: " + item.gameObject.name + ": " + array[z] + " [NCS:" + cd_36 + "-" + _36[6] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Loading item save data for item: {0}: {1}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 9;
					for (int s = 0; s < 9; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_36 == 0) continue;
					c.Emit(OpCodes.Ldloc_0);
					c.Emit(OpCodes.Ldloc_S, 5);
					c.Emit(OpCodes.Ldloc_S, 8);
					c.EmitDelegate<System.Action<GrabbableObject, int[], int>>((item, array, z) =>
					{
						if ((Time.frameCount - _36[7]) >= cd_36)
						{
							_36[7] = Time.frameCount;
							Debug.Log("Loading item save data for item: " + item.gameObject + ": " + array[z] + " [NCS:" + cd_36 + "-" + _36[7] + "]");
						}
					});
				}
			}
		}

		//37 baboon
		public static int _37;
		private static void baboon(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "Threat type : {0}; threat level: {1} ; with distance: {2}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 12;
					for (int s = 0; s < 12; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_37 == 0) continue;
					c.Emit(OpCodes.Ldloc_1);
					c.Emit(OpCodes.Ldloc, 16);
					c.EmitDelegate<System.Action<Threat, float>>((threat, z) =>
					{
						if ((Time.frameCount - _37) >= cd_37)
						{
							_37 = Time.frameCount;
							Debug.Log("Threat type : " + threat.type.ToString() + "; threat level: " + threat.threatLevel + " ; with distance: " + z + " [NCS:" + cd_37 + "-" + _37 + "]");
						}
					});
					break;
				}
			}
		}

		//38 kiwi 1
		public static int[] _38 = new int[3];
		private static void kiwi_1(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode == OpCodes.Ldstr && l.Instrs[n].Operand.ToString() == "dist: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 12;
					for (int s = 0; s < 12; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_38 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldloc_0);
					c.EmitDelegate<System.Action<GiantKiwiAI, VehicleController>>((kiwi, vehicle) =>
					{
						if ((Time.frameCount - _38[0]) >= cd_38)
						{
							_38[0] = Time.frameCount;
							Debug.Log("dist: " + Vector3.Distance(kiwi.targetPlayer.transform.position, vehicle.transform.position) + " [NCS:" + cd_38 + "-" + _38[0] + "]");
						}
					});
					break;
				}
			}
		}

		//37 kiwi 2
		private static void kiwi_2(ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue;
				if (l.Instrs[n].Operand.ToString() == "Baby bird distance to nest : {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 7;
					for (int s = 0; s < 7; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_38 == 0) continue;
					c.Emit(OpCodes.Ldloc_0);
					c.EmitDelegate<System.Action<float>>((z) =>
					{
						if ((Time.frameCount - _38[1]) >= cd_38)
						{
							_38[1] = Time.frameCount;
							Debug.Log("Baby bird distance to nest : " + z + " [NCS:" + cd_38 + "-" + _38[1] + "]");
						}
					});
				}
				else if (l.Instrs[n].Operand.ToString() == "Counting scream timer; timer: {0}")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + 6;
					for (int s = 0; s < 6; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_38 == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.Emit(OpCodes.Ldfld, typeof(KiwiBabyItem).GetField("screamTimer", BindingFlags.NonPublic | BindingFlags.Instance));
					c.EmitDelegate<System.Action<float>>((timer) =>
					{
						if ((Time.frameCount - _38[2]) >= cd_38)
						{
							_38[2] = Time.frameCount;
							Debug.Log("Counting scream timer; timer: " + timer + " [NCS:" + cd_38 + "-" + _38[2] + "]");
						}
					});
				}
			}
		}

		private static void kiwi_3(On.GiantKiwiAI.orig_Start original, GiantKiwiAI kiwi)
		{
			kiwi.debugEnemyAI = false;
			original(kiwi);
		}

		/*template
		public static int[] _?? = new int[];
		private static void (ILContext l)
		{
			ILCursor c = new ILCursor(l);
			for (int n = 0; n < l.Instrs.Count; n = n + 1)
			{
				if (l.Instrs[n].OpCode != OpCodes.Ldstr) continue; //== &&
				if (l.Instrs[n].Operand.ToString()r == "")
				{
					r.Add(l.Method.Name + " \"" + l.Instrs[n].Operand.ToString());
					c.Index = n + ?;
					for (int s = 0; s < ?; s = s + 1) l.Instrs[n + s].OpCode = OpCodes.Nop;
					if (cd_ == 0) continue;
					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<System.Action<>>((self) =>
					{
						if ((Time.frameCount - _[]) >= cd_)
						{
							_[] = Time.frameCount;
							Debug.Log("[NCS:" + cd_ + "-" + _[] + "]");
						}
					});
					//break;
				}
				else if (l.Instrs[n].Operand.ToString()r == "")
				{
				}
			}
		}
		*/
	}
}
