
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ArenaBehaviors;
using HUD;
using Menu;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using On;
using On.ArenaBehaviors;
using RainMeadow.UI;
using RWCustom;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
	// Token: 0x02000030 RID: 48
	[NullableContext(1)]
	[Nullable(0)]
	public abstract class ExternalArenaGameMode
	{
		// Token: 0x17000048 RID: 72
		// (get) Token: 0x060003A0 RID: 928
		// (set) Token: 0x060003A1 RID: 929
		public abstract ArenaSetup.GameTypeID GetGameModeId { get; set; }

		// Token: 0x060003A2 RID: 930 RVA: 0x00031416 File Offset: 0x0002F616
		public virtual void ResetOnSessionEnd()
		{
		}

		// Token: 0x060003A3 RID: 931
		public abstract bool IsExitsOpen(ArenaOnlineGameMode arena, ExitManager.orig_ExitsOpen orig, ExitManager self);

		// Token: 0x060003A4 RID: 932
		public abstract bool SpawnBatflies(FliesWorldAI self, int spawnRoom);

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x060003A5 RID: 933
		// (set) Token: 0x060003A6 RID: 934
		public abstract int TimerDuration { get; set; }

		// Token: 0x060003A7 RID: 935 RVA: 0x00031419 File Offset: 0x0002F619
		public virtual void ArenaSessionCtor(ArenaOnlineGameMode arena, ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
		{
			arena.session = self;
			arena.ResetAtSession_ctor();
		}

		// Token: 0x060003A8 RID: 936 RVA: 0x0003142A File Offset: 0x0002F62A
		public virtual void ArenaSessionNextLevel(ArenaOnlineGameMode arena, ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager process)
		{
			arena.ResetAtNextLevel();
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x00031434 File Offset: 0x0002F634
		public virtual void ArenaSessionEnded(ArenaOnlineGameMode arena, ArenaSitting.orig_SessionEnded orig, ArenaSitting self, ArenaGameSession session, List<ArenaSitting.ArenaPlayer> list)
		{
			bool flag = list.Count == 1;
			if (flag)
			{
				list[0].winner = list[0].alive;
			}
			else
			{
				bool flag2 = list.Count > 1;
				if (flag2)
				{
					bool flag3 = list[0].alive && !list[1].alive;
					if (flag3)
					{
						list[0].winner = true;
					}
					else
					{
						bool flag4 = list[0].score > list[1].score;
						if (flag4)
						{
							list[0].winner = true;
						}
					}
				}
			}
		}

		// Token: 0x060003AA RID: 938 RVA: 0x000314E8 File Offset: 0x0002F6E8
		public virtual void InitAsCustomGameType(ArenaSetup.GameTypeSetup self)
		{
			self.foodScore = 1;
			self.survivalScore = 0;
			self.spearHitScore = 0;
			self.repeatSingleLevelForever = false;
			self.savingAndLoadingSession = true;
			self.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard;
			self.rainWhenOnePlayerLeft = true;
			self.levelItems = true;
			self.fliesSpawn = true;
			self.saveCreatures = false;
		}

		// Token: 0x060003AB RID: 939 RVA: 0x00031548 File Offset: 0x0002F748
		public string PlayingAsText()
		{
			ArenaClientSettings clientSettings = OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>();
			bool flag = ModManager.MSC && clientSettings.playingAs == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel;
			string result;
			if (flag)
			{
				ArenaOnlineGameMode arenaOnlineGameMode = OnlineManager.lobby.gameMode as ArenaOnlineGameMode;
				result = (((arenaOnlineGameMode != null) ? arenaOnlineGameMode.paincatName : null) ?? SlugcatStats.getSlugcatName(clientSettings.playingAs));
			}
			else
			{
				bool flag2 = clientSettings.playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat;
				if (flag2)
				{
					result = SlugcatStats.getSlugcatName(clientSettings.randomPlayingAs);
				}
				else
				{
					result = SlugcatStats.getSlugcatName(clientSettings.playingAs);
				}
			}
			return result;
		}

		// Token: 0x060003AC RID: 940 RVA: 0x000315F0 File Offset: 0x0002F7F0
		public virtual string TimerText()
		{
			return "";
		}

		// Token: 0x060003AD RID: 941 RVA: 0x00031608 File Offset: 0x0002F808
		public virtual int SetTimer(ArenaOnlineGameMode arena)
		{
			return arena.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
		}

		// Token: 0x060003AE RID: 942 RVA: 0x00031632 File Offset: 0x0002F832
		public virtual void ResetGameTimer()
		{
			this._timerDuration = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
		}

		// Token: 0x060003AF RID: 943 RVA: 0x0003164C File Offset: 0x0002F84C
		public virtual int TimerDirection(ArenaOnlineGameMode arena, int timer)
		{
			return --timer;
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x00031664 File Offset: 0x0002F864
		public virtual void Killing(ArenaOnlineGameMode arena, ArenaGameSession.orig_Killing orig, ArenaGameSession self, Player player, Creature killedCrit, int playerIndex)
		{
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x00031667 File Offset: 0x0002F867
		public virtual void LandSpear(ArenaOnlineGameMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
		{
		}

		// Token: 0x060003B2 RID: 946 RVA: 0x0003166C File Offset: 0x0002F86C
		public virtual void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, HUD self, ArenaGameSession session)
		{
			self.AddPart(new TextPrompt(self));
			bool canSendChatMessages = MatchmakingManager.currentInstance.canSendChatMessages;
			if (canSendChatMessages)
			{
				self.AddPart(new ChatHud(self, session.game.cameras[0]));
			}
			self.AddPart(new SpectatorHud(self, session.game.cameras[0]));
			self.AddPart(new ArenaPrepTimer(self, self.fContainers[0], arena, session));
			self.AddPart(new OnlineHUD(self, session.game.cameras[0], arena));
			self.AddPart(new Pointing(self));
			self.AddPart(new ArenaSpawnLocationIndicator(self, session.game.cameras[0]));
			self.AddPart(new CamoMeter(self, null, self.fContainers[1]));
			bool flag = ModManager.Watcher && OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>().playingAs == WatcherEnums.SlugcatStatsName.Watcher;
			if (flag)
			{
				RainMeadow.Debug("Adding Watcher Camo Meter", "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "HUD_InitMultiplayerHud");
				self.AddPart(new CamoMeter(self, null, self.fContainers[1]));
			}
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x0003179B File Offset: 0x0002F99B
		public virtual void ArenaCreatureSpawner_SpawnCreatures(ArenaOnlineGameMode arena, ArenaCreatureSpawner.orig_SpawnArenaCreatures orig, RainWorldGame game, ArenaSetup.GameTypeSetup.WildLifeSetting wildLifeSetting, ref List<AbstractCreature> availableCreatures, ref MultiplayerUnlocks unlocks)
		{
		}

		// Token: 0x060003B4 RID: 948 RVA: 0x000317A0 File Offset: 0x0002F9A0
		public virtual bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
		{
			return arena.countdownInitiatedHoldFire = false;
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x000317BC File Offset: 0x0002F9BC
		public virtual string AddIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
		{
			return "";
		}

		// Token: 0x060003B6 RID: 950 RVA: 0x000317D4 File Offset: 0x0002F9D4
		public virtual Color IconColor(ArenaOnlineGameMode arena, OnlinePlayerDisplay display, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
		{
			float H;
			float S;
			float V;
			Color.RGBToHSV(customization.SlugcatColor(), out H, out S, out V);
			bool flag = (double)V < 0.8;
			Color result;
			if (flag)
			{
				result = Color.HSVToRGB(H, S, 0.8f);
			}
			else
			{
				result = customization.SlugcatColor();
			}
			return result;
		}

		// Token: 0x060003B7 RID: 951 RVA: 0x00031824 File Offset: 0x0002FA24
		public virtual List<ListItem> ArenaOnlineInterfaceListItems(ArenaOnlineGameMode arena)
		{
			return null;
		}

		// Token: 0x060003B8 RID: 952 RVA: 0x00031838 File Offset: 0x0002FA38
		public void SpawnTransferableCreature(ArenaOnlineGameMode arena, ArenaGameSession self, Room room, int randomExitIndex, CreatureTemplate.Type templateType)
		{
			AbstractCreature abstractCreature = new AbstractCreature(self.game.world, StaticWorld.GetCreatureTemplate(templateType), null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, 0));
			abstractCreature.pos.room = self.game.world.GetAbstractRoom(0).index;
			abstractCreature.pos.abstractNode = room.ShortcutLeadingToNode(randomExitIndex).destNode;
			abstractCreature.Room.AddEntity(abstractCreature);
			abstractCreature.RealizeInRoom();
			self.game.world.GetResource().ApoEnteringWorld(abstractCreature);
		}

		// Token: 0x060003B9 RID: 953 RVA: 0x000318D4 File Offset: 0x0002FAD4
		public void SpawnNonTransferableCreature(ArenaOnlineGameMode arena, ArenaGameSession self, Room room, int randomExitIndex, CreatureTemplate.Type templateType)
		{
			RainMeadow.Debug("Trying to create an abstract creature", "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "SpawnNonTransferableCreature");
			RainMeadow.Debug(string.Format("RANDOM EXIT INDEX: {0}", randomExitIndex), "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "SpawnNonTransferableCreature");
			RainMeadow.Debug(string.Format("RANDOM START TILE INDEX: {0}", room.ShortcutLeadingToNode(randomExitIndex).StartTile), "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "SpawnNonTransferableCreature");
			RainMeadow.sSpawningAvatar = true;
			AbstractCreature abstractCreature = new AbstractCreature(self.game.world, StaticWorld.GetCreatureTemplate(templateType), null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, 0));
			abstractCreature.pos.room = self.game.world.GetAbstractRoom(0).index;
			abstractCreature.pos.abstractNode = room.ShortcutLeadingToNode(randomExitIndex).destNode;
			abstractCreature.Room.AddEntity(abstractCreature);
			RainMeadow.Debug("assigned ac, registering", "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "SpawnNonTransferableCreature");
			self.game.world.GetResource().ApoEnteringWorld(abstractCreature);
			RainMeadow.sSpawningAvatar = false;
			self.game.cameras[0].followAbstractCreature = abstractCreature;
			OnlinePhysicalObject oe;
			SlugcatCustomization customization;
			bool flag = abstractCreature.GetOnlineObject(out oe) && oe.TryGetData<SlugcatCustomization>(out customization);
			if (flag)
			{
				abstractCreature.state = new PlayerState(abstractCreature, 0, customization.playingAs, false);
			}
			else
			{
				RainMeadow.Error("Could not get online owner for spawned player!", "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "SpawnNonTransferableCreature");
				abstractCreature.state = new PlayerState(abstractCreature, 0, self.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].playerClass, false);
			}
			RainMeadow.Debug("Arena: Realize Creature!", "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "SpawnNonTransferableCreature");
			abstractCreature.Realize();
			ShortcutHandler.ShortCutVessel shortCutVessel = new ShortcutHandler.ShortCutVessel(room.ShortcutLeadingToNode(randomExitIndex).DestTile, abstractCreature.realizedCreature, self.game.world.GetAbstractRoom(0), 0);
			shortCutVessel.entranceNode = abstractCreature.pos.abstractNode;
			shortCutVessel.room = self.game.world.GetAbstractRoom(abstractCreature.Room.name);
			self.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
			self.AddPlayer(abstractCreature);
			bool flag2 = !(abstractCreature.realizedCreature is Player);
			if (!flag2)
			{
				bool flag3 = (abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Night;
				if (flag3)
				{
					(abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
				}
				bool msc = ModManager.MSC;
				if (msc)
				{
					bool flag4 = (abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Red;
					if (flag4)
					{
						self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.75f);
						self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.5f);
					}
					bool flag5 = (abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Yellow;
					if (flag5)
					{
						self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, 0.75f);
						self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.3f);
					}
					bool flag6 = (abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer;
					if (flag6)
					{
						self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.5f);
						self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, -1f);
					}
					bool flag7 = (abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup;
					if (flag7)
					{
						(abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
					}
					bool flag8 = (abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel;
					if (flag8)
					{
						(abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = arena.painCatThrowingSkill;
						RainMeadow.Debug("ENOT THROWING SKILL " + (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill.ToString(), "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "SpawnNonTransferableCreature");
						bool flag9 = (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill == 0 && arena.painCatEgg;
						if (flag9)
						{
							AbstractPhysicalObject bringThePain = new AbstractPhysicalObject(room.world, DLCSharedEnums.AbstractObjectType.SingularityBomb, null, abstractCreature.pos, shortCutVessel.room.world.game.GetNewID());
							room.abstractRoom.AddEntity(bringThePain);
							bringThePain.RealizeInRoom();
							self.room.world.GetResource().ApoEnteringWorld(bringThePain);
							RoomSession resource = self.room.abstractRoom.GetResource();
							if (resource != null)
							{
								resource.ApoEnteringRoom(bringThePain, bringThePain.pos);
							}
						}
						bool flag10 = arena.lizardEvent == 99 && arena.painCatLizard;
						if (flag10)
						{
							self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Lizards, -1, 0, 1f);
							AbstractCreature bringTheTrain = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate("Red Lizard"), null, room.GetWorldCoordinate(shortCutVessel.pos), shortCutVessel.room.world.game.GetNewID());
							room.abstractRoom.AddEntity(bringTheTrain);
							bringTheTrain.Realize();
							bringTheTrain.realizedCreature.PlaceInRoom(room);
							self.room.world.GetResource().ApoEnteringWorld(bringTheTrain);
							RoomSession resource2 = self.room.abstractRoom.GetResource();
							if (resource2 != null)
							{
								resource2.ApoEnteringRoom(bringTheTrain, bringTheTrain.pos);
							}
						}
					}
					bool flag11 = (abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint;
					if (flag11)
					{
						bool flag12 = !arena.sainot;
						if (flag12)
						{
							(abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 0;
						}
						else
						{
							(abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
						}
					}
				}
				bool flag13 = ModManager.Watcher && (abstractCreature.realizedCreature as Player).SlugCatClass == WatcherEnums.SlugcatStatsName.Watcher;
				if (flag13)
				{
					(abstractCreature.realizedCreature as Player).enterIntoCamoDuration = 40;
				}
			}
		}

		// Token: 0x060003BA RID: 954 RVA: 0x00031F3C File Offset: 0x0003013C
		public virtual void SpawnPlayer(ArenaOnlineGameMode arena, ArenaGameSession self, Room room, List<int> suggestedDens)
		{
			List<OnlinePlayer> list = new List<OnlinePlayer>();
			List<OnlinePlayer> list2 = new List<OnlinePlayer>();
			for (int i = 0; i < OnlineManager.players.Count; i++)
			{
				bool flag = arena.arenaSittingOnlineOrder.Contains(OnlineManager.players[i].inLobbyId);
				if (flag)
				{
					list2.Add(OnlineManager.players[i]);
				}
			}
			while (list2.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, list2.Count);
				list.Add(list2[index]);
				list2.RemoveAt(index);
			}
			int totalExits = self.game.world.GetAbstractRoom(0).exits;
			int[] exitScores = new int[totalExits];
			bool flag2 = suggestedDens != null;
			if (flag2)
			{
				for (int j = 0; j < suggestedDens.Count; j++)
				{
					bool flag3 = suggestedDens[j] >= 0 && suggestedDens[j] < exitScores.Length;
					if (flag3)
					{
						exitScores[suggestedDens[j]] -= 1000;
					}
				}
			}
			int randomExitIndex = UnityEngine.Random.Range(0, totalExits);
			float highestScore = float.MinValue;
			for (int currentExitIndex = 0; currentExitIndex < totalExits; currentExitIndex++)
			{
				float score = UnityEngine.Random.value - (float)exitScores[currentExitIndex] * 1000f;
				IntVector2 startTilePosition = room.ShortcutLeadingToNode(currentExitIndex).StartTile;
				for (int otherExitIndex = 0; otherExitIndex < totalExits; otherExitIndex++)
				{
					bool flag4 = otherExitIndex != currentExitIndex && exitScores[otherExitIndex] > 0;
					if (flag4)
					{
						float distanceAdjustment = Mathf.Clamp(startTilePosition.FloatDist(room.ShortcutLeadingToNode(otherExitIndex).StartTile), 8f, 17f) * UnityEngine.Random.value;
						score += distanceAdjustment;
					}
				}
				bool flag5 = score > highestScore;
				if (flag5)
				{
					randomExitIndex = currentExitIndex;
					highestScore = score;
				}
			}
			bool flag6 = ArenaHelpers.GetArenaClientSettings(OnlineManager.mePlayer).playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator;
			if (flag6)
			{
				RainMeadow.Debug("Player spawned as overseer", "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "SpawnPlayer");
				bool enableOverseer = arena.enableOverseer;
				if (enableOverseer)
				{
					this.SpawnTransferableCreature(arena, self, room, randomExitIndex, CreatureTemplate.Type.Overseer);
				}
			}
			else
			{
				this.SpawnNonTransferableCreature(arena, self, room, randomExitIndex, CreatureTemplate.Type.Slugcat);
			}
			self.playersSpawned = true;
			bool isOwner = OnlineManager.lobby.isOwner;
			if (isOwner)
			{
				arena.isInGame = true;
				arena.leaveForNextLevel = false;
				foreach (ushort onlineArenaPlayer in arena.arenaSittingOnlineOrder)
				{
					OnlinePlayer getPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(onlineArenaPlayer);
					bool flag7 = getPlayer != null;
					if (flag7)
					{
						arena.CheckToAddPlayerStatsToDicts(getPlayer);
					}
				}
				arena.playersLateWaitingInLobbyForNextRound.Clear();
				arena.hasPermissionToRejoin = false;
			}
		}

		// Token: 0x060003BB RID: 955 RVA: 0x00032248 File Offset: 0x00030448
		public virtual void ArenaSessionUpdate(ArenaGameSession.orig_Update orig, ArenaGameSession self, ArenaOnlineGameMode arena)
		{
			ArenaClientSettings arenaClientSettings = ArenaHelpers.GetArenaClientSettings(OnlineManager.lobby.owner);
			bool isOwnerOverseer = ((arenaClientSettings != null) ? arenaClientSettings.playingAs : null) == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator;
			bool flag = arena.countdownInitiatedHoldFire && isOwnerOverseer;
			if (flag)
			{
				self.endSessionCounter = 30;
			}
			orig.Invoke(self);
			bool flag2 = arena.currentLobbyOwner != OnlineManager.lobby.owner;
			if (flag2)
			{
				self.game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerResults);
				arena.currentLobbyOwner = OnlineManager.lobby.owner;
			}
			int activePlayerCountWithOverseers = (from id in arena.arenaSittingOnlineOrder
			select ArenaHelpers.FindOnlinePlayerByLobbyId(id) into player
			where player != null
			select ArenaHelpers.GetArenaClientSettings(player) into settings
			where settings != null
			select settings).Count((ArenaClientSettings settings) => settings.playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineOverseerSpectator);
			bool flag3 = self.Players.Count + activePlayerCountWithOverseers != arena.arenaSittingOnlineOrder.Count;
			if (flag3)
			{
				List<AbstractCreature> extraPlayers = self.Players.Skip(arena.arenaSittingOnlineOrder.Count).ToList<AbstractCreature>();
				self.Players.RemoveAll((AbstractCreature p) => extraPlayers.Contains(p));
				foreach (OnlineEntity.EntityId playerAvatar in from kv in OnlineManager.lobby.playerAvatars
				select kv.Value)
				{
					bool flag4 = playerAvatar.type == 0;
					if (!flag4)
					{
						OnlinePhysicalObject opo = playerAvatar.FindEntity(true) as OnlinePhysicalObject;
						if (opo == null)
						{
							goto IL_235;
						}
						AbstractCreature ac = opo.apo as AbstractCreature;
						if (ac == null)
						{
							goto IL_235;
						}
						bool flag5 = !self.Players.Contains(ac);
						IL_236:
						bool flag6 = flag5;
						if (flag6)
						{
							self.Players.Add(ac);
						}
						continue;
						IL_235:
						flag5 = false;
						goto IL_236;
					}
				}
			}
			bool isOwner = OnlineManager.lobby.isOwner;
			if (isOwner)
			{
				arena.playersEqualToOnlineSitting = (self.Players.Count + activePlayerCountWithOverseers == arena.arenaSittingOnlineOrder.Count);
			}
			bool flag7 = !self.sessionEnded;
			if (flag7)
			{
				foreach (ArenaSitting.ArenaPlayer s in self.arenaSitting.players)
				{
					OnlinePlayer os = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, s.playerNumber);
					for (int i = 0; i < self.Players.Count; i++)
					{
						OnlinePhysicalObject onlineC;
						bool flag8 = OnlinePhysicalObject.map.TryGetValue(self.Players[i], out onlineC);
						if (flag8)
						{
							bool flag9 = onlineC.owner == os && self.Players[i].realizedCreature != null && !self.Players[i].realizedCreature.State.dead;
							if (flag9)
							{
								s.timeAlive++;
							}
						}
						else
						{
							bool alive = self.Players[i].state.alive;
							if (alive)
							{
								self.Players[i].Die();
							}
						}
					}
				}
			}
		}

		// Token: 0x060003BC RID: 956 RVA: 0x00032664 File Offset: 0x00030864
		public virtual bool PlayerSessionResultSort(ArenaOnlineGameMode arena, ArenaSitting.orig_PlayerSessionResultSort orig, ArenaSitting self, ArenaSitting.ArenaPlayer A, ArenaSitting.ArenaPlayer B)
		{
			return orig.Invoke(self, A, B);
		}

		// Token: 0x060003BD RID: 957 RVA: 0x00032684 File Offset: 0x00030884
		public virtual bool PlayerSittingResultSort(ArenaOnlineGameMode arena, ArenaSitting.orig_PlayerSittingResultSort orig, ArenaSitting self, ArenaSitting.ArenaPlayer A, ArenaSitting.ArenaPlayer B)
		{
			RainMeadow.Debug(string.Format("PlayerSittingResultSort Player A: Score: {0} - Wins: {1} - All Kills: {2} - Deaths: {3}", new object[]
			{
				A.score,
				A.wins,
				A.allKills.Count,
				A.deaths
			}), "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "PlayerSittingResultSort");
			RainMeadow.Debug(string.Format("PlayerSittingResultSort Player B: Score: {0} - Wins: {1} - All Kills: {2} - Deaths: {3}", new object[]
			{
				B.score,
				B.wins,
				B.allKills.Count,
				B.deaths
			}), "/Arena/ArenaOnlineGameModes/BaseGameMode.cs", "PlayerSittingResultSort");
			return orig.Invoke(self, A, B);
		}

		// Token: 0x060003BE RID: 958 RVA: 0x00032763 File Offset: 0x00030963
		public virtual bool DidPlayerWinRainbow(ArenaOnlineGameMode arena, OnlinePlayer player)
		{
			return arena.reigningChamps.list.Contains(player.id);
		}

		// Token: 0x060003BF RID: 959 RVA: 0x0003277B File Offset: 0x0003097B
		public virtual void OnUIEnabled(ArenaOnlineLobbyMenu menu)
		{
		}

		// Token: 0x060003C0 RID: 960 RVA: 0x0003277E File Offset: 0x0003097E
		public virtual void OnUIDisabled(ArenaOnlineLobbyMenu menu)
		{
		}

		// Token: 0x060003C1 RID: 961 RVA: 0x00032781 File Offset: 0x00030981
		public virtual void OnUIUpdate(ArenaOnlineLobbyMenu menu)
		{
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x00032784 File Offset: 0x00030984
		public virtual void OnUIShutDown(ArenaOnlineLobbyMenu menu)
		{
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x00032787 File Offset: 0x00030987
		public virtual Color GetPortraitColor(ArenaOnlineGameMode arena, [Nullable(2)] OnlinePlayer player, Color origPortraitColor)
		{
			return origPortraitColor;
		}

		// Token: 0x060003C4 RID: 964 RVA: 0x0003278C File Offset: 0x0003098C
		public virtual Dialog AddGameModeInfo(ArenaOnlineGameMode arena, Menu menu)
		{
			return new DialogNotify(menu.LongTranslate("This game mode doesnt have any info to give"), new Vector2(500f, 400f), menu.manager, delegate()
			{
				menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
			});
		}

		// Token: 0x060003C5 RID: 965 RVA: 0x000327E8 File Offset: 0x000309E8
		public virtual Dialog AddPostGameStatsFeed(ArenaOnlineGameMode arena, Menu menu)
		{
			return new ArenaPostGameStatsDialog(menu.manager, arena);
		}

		// Token: 0x040001AF RID: 431
		private int _timerDuration;
	}
}