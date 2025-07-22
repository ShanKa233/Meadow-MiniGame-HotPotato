using BepInEx;
using HarmonyLib;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RainMeadow.UI.Components;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RainMeadow.UI;


namespace RainMeadow
{
    /// <summary>
    /// 外部竞技场游戏模式的抽象基类，定义了所有游戏模式必须实现的基本功能
    /// </summary>
    public abstract class ExternalArenaGameMode
    {
        private int _timerDuration; // 游戏计时器持续时间

        /// <summary>
        /// 获取和设置游戏模式ID，默认为自由混战(FFA)模式
        /// </summary>
        public virtual ArenaSetup.GameTypeID GetGameModeId
        {
            get
            {
                return FFA.FFAMode;
            }
            set { GetGameModeId = value; }

        }

        /// <summary>
        /// 在会话结束时重置游戏状态
        /// </summary>
        public virtual void ResetOnSessionEnd()
        {

        }

        /// <summary>
        /// 确定出口是否开放的抽象方法
        /// </summary>
        public abstract bool IsExitsOpen(ArenaOnlineGameMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self);
        
        /// <summary>
        /// 确定是否生成蝙蝠的抽象方法
        /// </summary>
        public abstract bool SpawnBatflies(FliesWorldAI self, int spawnRoom);

        /// <summary>
        /// 游戏计时器持续时间的属性
        /// </summary>
        public abstract int TimerDuration { get; set; }

        /// <summary>
        /// 竞技场会话构造函数钩子，在创建新会话时调用
        /// </summary>
        public virtual void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            arena.ResetAtSession_ctor(); // 重置竞技场状态
        }

        /// <summary>
        /// 竞技场下一关卡钩子，在进入下一关卡时调用
        /// </summary>
        public virtual void ArenaSessionNextLevel(ArenaOnlineGameMode arena, On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager process)
        {
            arena.ResetAtNextLevel(); // 重置竞技场状态
        }

        /// <summary>
        /// 竞技场会话结束钩子，用于管理胜利条件
        /// 在原始列表排序后但在初始化覆盖层之前调用
        /// </summary>
        public virtual void ArenaSessionEnded(ArenaOnlineGameMode arena, On.ArenaSitting.orig_SessionEnded orig, ArenaSitting self, ArenaGameSession session, List<ArenaSitting.ArenaPlayer> list)
        {
            // 如果只有一名玩家，根据存活状态确定胜利
            if (list.Count == 1)
            {
                list[0].winner = list[0].alive;
            }
            // 如果有多名玩家，根据存活状态和分数确定胜利
            else if (list.Count > 1)
            {
                if (list[0].alive && !list[1].alive)
                {
                    list[0].winner = true;
                }
                else if (list[0].score > list[1].score)
                {
                    list[0].winner = true;
                }
            }
        }

        /// <summary>
        /// 初始化自定义游戏类型的设置
        /// </summary>
        public virtual void InitAsCustomGameType(ArenaSetup.GameTypeSetup self)
        {
            self.foodScore = 1;                        // 食物得分
            self.survivalScore = 0;                    // 生存得分
            self.spearHitScore = 0;                    // 矛命中得分
            self.repeatSingleLevelForever = false;     // 是否无限重复单一关卡
            self.savingAndLoadingSession = true;       // 是否保存和加载会话
            self.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard; // 巢穴进入规则
            self.rainWhenOnePlayerLeft = true;         // 当只剩一名玩家时是否下雨
            self.levelItems = true;                    // 是否生成关卡物品
            self.fliesSpawn = true;                    // 是否生成飞虫
            self.saveCreatures = false;                // 是否保存生物
        }

        /// <summary>
        /// 获取当前玩家角色的文本描述
        /// </summary>
        public string PlayingAsText()
        {
            var clientSettings = OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>();
            // 如果是MSC模组的特殊角色Sofanthiel
            if (ModManager.MSC && clientSettings.playingAs == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                return (OnlineManager.lobby.gameMode as ArenaOnlineGameMode)?.paincatName ?? SlugcatStats.getSlugcatName(clientSettings.playingAs);
            }
            // 如果是随机角色
            else if (clientSettings.playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat)
            {
                return SlugcatStats.getSlugcatName(clientSettings.randomPlayingAs);
            }
            // 其他常规角色
            else
            {
                return SlugcatStats.getSlugcatName(clientSettings.playingAs);
            }
        }

        /// <summary>
        /// 获取计时器显示文本
        /// </summary>
        public virtual string TimerText()
        {
            return "";
        }

        /// <summary>
        /// 设置竞技场计时器
        /// </summary>
        public virtual int SetTimer(ArenaOnlineGameMode arena)
        {
            return arena.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

        /// <summary>
        /// 重置游戏计时器
        /// </summary>
        public virtual void ResetGameTimer()
        {
            _timerDuration = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;

        }
        
        /// <summary>
        /// 计时器方向（递增或递减）
        /// </summary>
        public virtual int TimerDirection(ArenaOnlineGameMode arena, int timer)
        {
            return --timer; // 默认递减
        }

        /// <summary>
        /// 玩家击杀事件处理
        /// 注意：这在被害者端运行，而不是击杀者端！
        /// </summary>
        public virtual void Killing(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_Killing orig, ArenaGameSession self, Player player, Creature killedCrit, int playerIndex)
        {
        }
        
        /// <summary>
        /// 矛命中事件处理
        /// </summary>
        public virtual void LandSpear(ArenaOnlineGameMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
        {

        }
        
        /// <summary>
        /// 初始化多人游戏HUD
        /// </summary>
        public virtual void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, HUD.HUD self, ArenaGameSession session)
        {
            self.AddPart(new HUD.TextPrompt(self)); // 添加文本提示

            // 如果可以发送聊天消息，添加聊天HUD
            if (MatchmakingManager.currentInstance.canSendChatMessages)
                self.AddPart(new ChatHud(self, session.game.cameras[0]));

            self.AddPart(new SpectatorHud(self, session.game.cameras[0])); // 添加观战HUD
            self.AddPart(new ArenaPrepTimer(self, self.fContainers[0], arena, session)); // 添加准备计时器
            self.AddPart(new OnlineHUD(self, session.game.cameras[0], arena)); // 添加在线HUD
            self.AddPart(new Pointing(self)); // 添加指向功能
            self.AddPart(new ArenaSpawnLocationIndicator(self, session.game.cameras[0])); // 添加出生点指示器
            self.AddPart(new Watcher.CamoMeter(self, self.fContainers[1])); // 添加伪装计量表
            
            // 如果是Watcher模组且玩家使用Watcher角色，添加伪装计量表
            if (ModManager.Watcher && OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>().playingAs == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
            {
                RainMeadow.Debug("Adding Watcher Camo Meter");
                self.AddPart(new Watcher.CamoMeter(self, self.fContainers[1]));
            }
        }
        
        /// <summary>
        /// 竞技场生物生成处理
        /// </summary>
        public virtual void ArenaCreatureSpawner_SpawnCreatures(ArenaOnlineGameMode arena, On.ArenaCreatureSpawner.orig_SpawnArenaCreatures orig, RainWorldGame game, ArenaSetup.GameTypeSetup.WildLifeSetting wildLifeSetting, ref List<AbstractCreature> availableCreatures, ref MultiplayerUnlocks unlocks)
        {

        }

        /// <summary>
        /// 计时器激活时是否阻止射击
        /// </summary>
        public virtual bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            return arena.countdownInitiatedHoldFire = false;
        }

        /// <summary>
        /// 添加玩家图标
        /// </summary>
        public virtual string AddIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {
            return "";
        }

        /// <summary>
        /// 获取图标颜色
        /// </summary>
        public virtual Color IconColor(ArenaOnlineGameMode arena, OnlinePlayerDisplay display, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {
            // 调整颜色亮度，确保可见性
            Color.RGBToHSV(customization.SlugcatColor(), out var H, out var S, out var V);
            if (V < 0.8)
            {
                return Color.HSVToRGB(H, S, 0.8f);
            }
            return customization.SlugcatColor();
        }

        /// <summary>
        /// 获取竞技场在线界面列表项
        /// </summary>
        public virtual List<ListItem> ArenaOnlineInterfaceListItems(ArenaOnlineGameMode arena)
        {
            return null;
        }

        /// <summary>
        /// 生成玩家
        /// </summary>
        public virtual void SpawnPlayer(ArenaOnlineGameMode arena, ArenaGameSession self, Room room, List<int> suggestedDens)
        {
            // 创建玩家列表
            List<OnlinePlayer> list = new List<OnlinePlayer>();
            List<OnlinePlayer> list2 = new List<OnlinePlayer>();

            // 收集所有在竞技场中的在线玩家
            for (int j = 0; j < OnlineManager.players.Count; j++)
            {
                if (arena.arenaSittingOnlineOrder.Contains(OnlineManager.players[j].inLobbyId))
                {
                    list2.Add(OnlineManager.players[j]);
                }
            }

            // 随机排序玩家列表
            while (list2.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, list2.Count);
                list.Add(list2[index]);
                list2.RemoveAt(index);
            }

            // 计算出口分数，用于选择最佳出生点
            int totalExits = self.game.world.GetAbstractRoom(0).exits;
            int[] exitScores = new int[totalExits];
            if (suggestedDens != null)
            {
                for (int k = 0; k < suggestedDens.Count; k++)
                {
                    if (suggestedDens[k] >= 0 && suggestedDens[k] < exitScores.Length)
                    {
                        exitScores[suggestedDens[k]] -= 1000;
                    }
                }
            }

            // 选择随机出口作为出生点
            int randomExitIndex = UnityEngine.Random.Range(0, totalExits);
            float highestScore = float.MinValue;

            // 计算每个出口的分数，选择最高分的出口
            for (int currentExitIndex = 0; currentExitIndex < totalExits; currentExitIndex++)
            {
                float score = UnityEngine.Random.value - (float)exitScores[currentExitIndex] * 1000f;
                RWCustom.IntVector2 startTilePosition = room.ShortcutLeadingToNode(currentExitIndex).StartTile;

                // 根据与其他出口的距离调整分数
                for (int otherExitIndex = 0; otherExitIndex < totalExits; otherExitIndex++)
                {
                    if (otherExitIndex != currentExitIndex && exitScores[otherExitIndex] > 0)
                    {
                        float distanceAdjustment = Mathf.Clamp(startTilePosition.FloatDist(room.ShortcutLeadingToNode(otherExitIndex).StartTile), 8f, 17f) * UnityEngine.Random.value;
                        score += distanceAdjustment;
                    }
                }

                // 更新最高分出口
                if (score > highestScore)
                {
                    randomExitIndex = currentExitIndex;
                    highestScore = score;
                }
            }

            // 创建抽象生物（玩家）
            RainMeadow.Debug("Trying to create an abstract creature");
            RainMeadow.Debug($"RANDOM EXIT INDEX: {randomExitIndex}");
            RainMeadow.Debug($"RANDOM START TILE INDEX: {room.ShortcutLeadingToNode(randomExitIndex).StartTile}");
            RainMeadow.sSpawningAvatar = true;
            AbstractCreature abstractCreature = new AbstractCreature(self.game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, 0));
            abstractCreature.pos.room = self.game.world.GetAbstractRoom(0).index;
            abstractCreature.pos.abstractNode = room.ShortcutLeadingToNode(randomExitIndex).destNode;
            abstractCreature.Room.AddEntity(abstractCreature);

            RainMeadow.Debug("assigned ac, registering");

            // 注册生物进入世界
            self.game.world.GetResource().ApoEnteringWorld(abstractCreature);
            RainMeadow.sSpawningAvatar = false;

            // 设置相机跟随生物
            self.game.cameras[0].followAbstractCreature = abstractCreature;

            // 设置玩家状态
            if (abstractCreature.GetOnlineObject(out var oe) && oe.TryGetData<SlugcatCustomization>(out var customization))
            {
                abstractCreature.state = new PlayerState(abstractCreature, 0, customization.playingAs, isGhost: false);
            }
            else
            {
                RainMeadow.Error("Could not get online owner for spawned player!");
                abstractCreature.state = new PlayerState(abstractCreature, 0, self.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].playerClass, isGhost: false);
            }

            // 实例化生物
            RainMeadow.Debug("Arena: Realize Creature!");
            abstractCreature.Realize();
            var shortCutVessel = new ShortcutHandler.ShortCutVessel(room.ShortcutLeadingToNode(randomExitIndex).DestTile, abstractCreature.realizedCreature, self.game.world.GetAbstractRoom(0), 0);

            shortCutVessel.entranceNode = abstractCreature.pos.abstractNode;
            shortCutVessel.room = self.game.world.GetAbstractRoom(abstractCreature.Room.name);

            // 添加到等待大厅
            self.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
            self.AddPlayer(abstractCreature);
            
            // 角色特殊能力设置
            // 夜猫（Night）投掷技能设置
            if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Night)
            {
                (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
            }
            
            // MSC模组角色特殊设置
            if (ModManager.MSC)
            {
                // 猎人（Red）社区关系设置
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Red)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.75f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.5f);
                }

                // 僧侣（Yellow）社区关系设置
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Yellow)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, 0.75f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.3f);
                }

                // 工匠（Artificer）社区关系设置
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.5f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, -1f);
                }

                // 幼崽（Slugpup）投掷技能设置
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                {
                    (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
                }

                // Sofanthiel特殊设置
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                {
                    (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = arena.painCatThrowingSkill;
                    RainMeadow.Debug("ENOT THROWING SKILL " + (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill);
                    
                    // 如果投掷技能为0且启用了痛苦猫蛋，生成奇点炸弹
                    if ((abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill == 0 && arena.painCatEgg)
                    {
                        AbstractPhysicalObject bringThePain = new AbstractPhysicalObject(room.world, DLCSharedEnums.AbstractObjectType.SingularityBomb, null, abstractCreature.pos, shortCutVessel.room.world.game.GetNewID());
                        room.abstractRoom.AddEntity(bringThePain);
                        bringThePain.RealizeInRoom();

                        self.room.world.GetResource().ApoEnteringWorld(bringThePain);
                        self.room.abstractRoom.GetResource()?.ApoEnteringRoom(bringThePain, bringThePain.pos);
                    }

                    // 如果蜥蜴事件为99且启用了痛苦猫蜥蜴，生成红蜥蜴
                    if (arena.lizardEvent == 99 && arena.painCatLizard)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Lizards, -1, 0, 1f);
                        AbstractCreature bringTheTrain = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate("Red Lizard"), null, room.GetWorldCoordinate(shortCutVessel.pos), shortCutVessel.room.world.game.GetNewID()); // 火车太大了 :( 
                        room.abstractRoom.AddEntity(bringTheTrain);
                        bringTheTrain.RealizeInRoom();

                        self.room.world.GetResource().ApoEnteringWorld(bringTheTrain);
                        self.room.abstractRoom.GetResource()?.ApoEnteringRoom(bringTheTrain, bringTheTrain.pos);
                    }
                }

                // 圣徒（Saint）投掷技能设置
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
                {
                    if (!arena.sainot) // 升天圣徒
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 0;
                    }
                    else
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;

                    }
                }
            }

            // Watcher模组角色伪装设置
            if (ModManager.Watcher && (abstractCreature.realizedCreature as Player).SlugCatClass == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
            {
                (abstractCreature.realizedCreature as Player).enterIntoCamoDuration = 40;
            }

            // 设置玩家已生成标志
            self.playersSpawned = true;
            
            // 如果是房主，初始化游戏状态
            if (OnlineManager.lobby.isOwner)
            {
                arena.isInGame = true; // 用于游戏开始时准备好的玩家
                arena.leaveForNextLevel = false;
                
                // 为所有竞技场玩家添加统计数据
                foreach (var onlineArenaPlayer in arena.arenaSittingOnlineOrder)
                {
                    OnlinePlayer? getPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(onlineArenaPlayer);
                    if (getPlayer != null)
                    {
                        arena.CheckToAddPlayerStatsToDicts(getPlayer);
                    }
                }
                arena.playersLateWaitingInLobbyForNextRound.Clear();
                arena.hasPermissionToRejoin = false;
            }
        }

        /// <summary>
        /// 竞技场会话更新
        /// </summary>
        public virtual void ArenaSessionUpdate(ArenaOnlineGameMode arena, ArenaGameSession session)
        {

        }

        /// <summary>
        /// 玩家会话结果排序
        /// </summary>
        public virtual bool PlayerSessionResultSort(ArenaOnlineGameMode arena, On.ArenaSitting.orig_PlayerSessionResultSort orig, ArenaSitting self, ArenaSitting.ArenaPlayer A, ArenaSitting.ArenaPlayer B)
        {
            return orig(self, A, B);
        }

        /// <summary>
        /// 玩家坐姿结果排序
        /// </summary>
        public virtual bool PlayerSittingResultSort(ArenaOnlineGameMode arena, On.ArenaSitting.orig_PlayerSittingResultSort orig, ArenaSitting self, ArenaSitting.ArenaPlayer A, ArenaSitting.ArenaPlayer B)
        {
            RainMeadow.Debug($"PlayerSittingResultSort Player A: Score: {A.score} - Wins: {A.wins} - All Kills: {A.allKills.Count} - Deaths: {A.deaths}");
            RainMeadow.Debug($"PlayerSittingResultSort Player B: Score: {B.score} - Wins: {B.wins} - All Kills: {B.allKills.Count} - Deaths: {B.deaths}");

            return orig(self, A, B);
        }
        
        /// <summary>
        /// 玩家是否赢得彩虹
        /// </summary>
        public virtual bool DidPlayerWinRainbow(ArenaOnlineGameMode arena, OnlinePlayer player) => arena.reigningChamps.list.Contains(player.id);
        
        /// <summary>
        /// UI启用时的回调
        /// </summary>
        public virtual void OnUIEnabled(ArenaOnlineLobbyMenu menu)
        {

        }
        
        /// <summary>
        /// UI禁用时的回调
        /// </summary>
        public virtual void OnUIDisabled(ArenaOnlineLobbyMenu menu)
        {

        }
        
        /// <summary>
        /// UI更新时的回调
        /// </summary>
        public virtual void OnUIUpdate(ArenaOnlineLobbyMenu menu)
        {

        }
        
        /// <summary>
        /// UI关闭时的回调
        /// </summary>
        public virtual void OnUIShutDown(ArenaOnlineLobbyMenu menu)
        {

        }
        
        /// <summary>
        /// 获取玩家头像颜色
        /// </summary>
        public virtual Color GetPortraitColor(ArenaOnlineGameMode arena, OnlinePlayer? player, Color origPortraitColor) => origPortraitColor;
        
        /// <summary>
        /// 添加游戏模式信息对话框
        /// </summary>
        public virtual Dialog AddGameModeInfo(ArenaOnlineGameMode arena, Menu.Menu menu)
        {
            return new DialogNotify(menu.LongTranslate("This game mode doesnt have any info to give"), new Vector2(500f, 400f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
        }
        
        /// <summary>
        /// 添加游戏后统计信息对话框
        /// </summary>
        public virtual Dialog AddPostGameStatsFeed(ArenaOnlineGameMode arena, Menu.Menu menu)
        {
            return new ArenaPostGameStatsDialog(menu.manager, arena);
        }
    }
}
