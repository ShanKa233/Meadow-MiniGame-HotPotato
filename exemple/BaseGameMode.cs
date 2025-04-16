using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Menu;
using MoreSlugcats;
using RainMeadow;
using UnityEngine;
namespace RainMeadow
{
    // 竞技场游戏模式的抽象基类，用于定义各种竞技场游戏的基本功能
    public abstract class ExternalArenaGameMode
    {
        // 计时器持续时间
        private int _timerDuration;

        // 抽象方法：控制出口是否开放
        public abstract bool IsExitsOpen(ArenaOnlineGameMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self);
        
        // 抽象方法：控制蝙蝠苍蝇的生成
        public abstract bool SpawnBatflies(FliesWorldAI self, int spawnRoom);

        // 抽象属性：计时器持续时间
        public abstract int TimerDuration { get; set; }

        // 竞技场会话初始化
        public virtual void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            arena.ResetAtSession_ctor();
        }

        // 处理下一关卡的切换
        public virtual void ArenaSessionNextLevel(ArenaOnlineGameMode arena, On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager process)
        {
            arena.ResetAtNextLevel();
        }

        // 初始化自定义游戏类型的设置
        public virtual void InitAsCustomGameType(ArenaSetup.GameTypeSetup self)
        {
            self.foodScore = 1;                     // 食物得分
            self.survivalScore = 0;                 // 生存得分
            self.spearHitScore = 0;                 // 矛命中得分
            self.repeatSingleLevelForever = false;  // 是否无限重复单个关卡
            self.savingAndLoadingSession = true;    // 允许保存和加载会话
            self.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard;  // 标准进入规则
            self.rainWhenOnePlayerLeft = true;      // 当只剩一个玩家时下雨
            self.levelItems = true;                 // 启用关卡物品
            self.fliesSpawn = true;                 // 允许生成苍蝇
            self.saveCreatures = false;             // 不保存生物状态
        }

        // 获取计时器显示文本
        public virtual string timertext()
        {
            return "";
        }

        // 设置计时器初始值
        public virtual int SetTimer(ArenaOnlineGameMode arena)
        {
            return arena.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

        // 重置游戏计时器
        public virtual void ResetGameTimer()
        {
            _timerDuration = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

        // 计时器方向（默认递减）
        public virtual int TimerDirection(ArenaOnlineGameMode arena, int timer)
        {
            return --timer;
        }

        // 处理击杀事件
        public virtual void Killing(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_Killing orig, ArenaGameSession self, Player player, Creature killedCrit, int playerIndex)
        {
        }

        // 处理矛命中事件
        public virtual void LandSpear(ArenaOnlineGameMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
        {
        }

        // 初始化多人游戏HUD
        public virtual void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, HUD.HUD self, ArenaGameSession session)
        {
            // 添加文本提示
            self.AddPart(new HUD.TextPrompt(self));
            
            // 如果允许聊天，添加聊天HUD
            if (MatchmakingManager.currentInstance.canSendChatMessages) 
                self.AddPart(new ChatHud(self, session.game.cameras[0]));
            
            // 添加观战HUD    
            self.AddPart(new SpectatorHud(self, session.game.cameras[0]));
            // 添加准备计时器
            self.AddPart(new ArenaPrepTimer(self, self.fContainers[0], arena, session));
            // 添加在线状态HUD
            self.AddPart(new OnlineHUD(self, session.game.cameras[0], arena));
        }

        // 生成竞技场生物
        public virtual void ArenaCreatureSpawner_SpawnCreatures(ArenaOnlineGameMode arena, On.ArenaCreatureSpawner.orig_SpawnArenaCreatures orig, RainWorldGame game, ArenaSetup.GameTypeSetup.WildLifeSetting wildLifeSetting, ref List<AbstractCreature> availableCreatures, ref MultiplayerUnlocks unlocks)
        {
        }

        // 计时器激活时是否暂停射击
        public virtual bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            return arena.countdownInitiatedHoldFire = false;
        }

        // 添加自定义图标
        public virtual string AddCustomIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud onlineHud)
        {
            return "";
        }

        // 生成玩家
        /// <summary>
        /// 生成玩家角色
        /// </summary>
        /// <param name="arena">竞技场模式实例</param>
        /// <param name="self">当前游戏会话</param>
        /// <param name="room">生成房间</param>
        /// <param name="suggestedDens">建议的生成点列表</param>
        public virtual void SpawnPlayer(ArenaOnlineGameMode arena, ArenaGameSession self, Room room, List<int> suggestedDens)
        {
            // 初始化两个玩家列表
            List<OnlinePlayer> list = new List<OnlinePlayer>();  // 最终玩家顺序列表
            List<OnlinePlayer> list2 = new List<OnlinePlayer>(); // 临时玩家列表

            // 遍历所有在线玩家，筛选出在竞技场中的玩家
            for (int j = 0; j < OnlineManager.players.Count; j++)
            {
                if (arena.arenaSittingOnlineOrder.Contains(OnlineManager.players[j].inLobbyId))
                {
                    list2.Add(OnlineManager.players[j]);
                }
            }

            // 随机打乱玩家顺序
            while (list2.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, list2.Count);
                list.Add(list2[index]);
                list2.RemoveAt(index);
            }

            // 获取总出口数并初始化出口分数数组
            int totalExits = self.game.world.GetAbstractRoom(0).exits;
            int[] exitScores = new int[totalExits];

            // 如果有建议的生成点，调整相应出口的分数
            if (suggestedDens != null)
            {
                for (int k = 0; k < suggestedDens.Count; k++)
                {
                    if (suggestedDens[k] >= 0 && suggestedDens[k] < exitScores.Length)
                    {
                        exitScores[suggestedDens[k]] -= 1000; // 降低建议生成点的分数
                    }
                }
            }

            // 随机选择一个出口作为生成点
            int randomExitIndex = UnityEngine.Random.Range(0, totalExits);
            float highestScore = float.MinValue;

            // 计算每个出口的分数，选择最佳生成点
            for (int currentExitIndex = 0; currentExitIndex < totalExits; currentExitIndex++)
            {
                float score = UnityEngine.Random.value - (float)exitScores[currentExitIndex] * 1000f;
                RWCustom.IntVector2 startTilePosition = room.ShortcutLeadingToNode(currentExitIndex).StartTile;

                // 考虑与其他出口的距离
                for (int otherExitIndex = 0; otherExitIndex < totalExits; otherExitIndex++)
                {
                    if (otherExitIndex != currentExitIndex && exitScores[otherExitIndex] > 0)
                    {
                        float distanceAdjustment = Mathf.Clamp(startTilePosition.FloatDist(room.ShortcutLeadingToNode(otherExitIndex).StartTile), 8f, 17f) * UnityEngine.Random.value;
                        score += distanceAdjustment;
                    }
                }

                // 更新最佳出口
                if (score > highestScore)
                {
                    randomExitIndex = currentExitIndex;
                    highestScore = score;
                }
            }

            // 调试信息：记录生成点信息
            RainMeadow.Debug("Trying to create an abstract creature");
            RainMeadow.Debug($"RANDOM EXIT INDEX: {randomExitIndex}");
            RainMeadow.Debug($"RANDOM START TILE INDEX: {room.ShortcutLeadingToNode(randomExitIndex).StartTile}");

            // 创建抽象玩家角色
            RainMeadow.sSpawningAvatar = true;
            AbstractCreature abstractCreature = new AbstractCreature(self.game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, 0));
            abstractCreature.pos.room = self.game.world.GetAbstractRoom(0).index;
            abstractCreature.pos.abstractNode = room.ShortcutLeadingToNode(randomExitIndex).destNode;

            // 注册角色到世界
            RainMeadow.Debug("assigned ac, registering");
            self.game.world.GetResource().ApoEnteringWorld(abstractCreature);
            RainMeadow.sSpawningAvatar = false;

            // 如果启用了MSC模组，设置摄像机跟随
            if (ModManager.MSC)
            {
                self.game.cameras[0].followAbstractCreature = abstractCreature;
            }

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

            // 实现角色
            RainMeadow.Debug("Arena: Realize Creature!");
            abstractCreature.Realize();

            // 创建快捷通道容器
            var shortCutVessel = new ShortcutHandler.ShortCutVessel(room.ShortcutLeadingToNode(randomExitIndex).DestTile, abstractCreature.realizedCreature, self.game.world.GetAbstractRoom(0), 0);
            shortCutVessel.entranceNode = abstractCreature.pos.abstractNode;
            shortCutVessel.room = self.game.world.GetAbstractRoom(abstractCreature.Room.name);

            // 添加角色到游戏会话
            self.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
            self.AddPlayer(abstractCreature);

            // 设置夜猫子角色的投掷技能
            if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Night)
            {
                (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
            }

            // 如果启用了MSC模组，处理特殊角色设置
            if (ModManager.MSC)
            {
                // 处理红色角色
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Red)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.75f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.5f);
                }

                // 处理黄色角色
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Yellow)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, 0.75f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.3f);
                }

                // 处理工匠角色
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.5f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, -1f);
                }

                // 处理小蛞蝓角色
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                {
                    (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
                }

                // 处理Sofanthiel角色
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

                    if (arena.lizardEvent == 99 && arena.painCatLizard)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Lizards, -1, 0, 1f);
                        AbstractCreature bringTheTrain = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate("Red Lizard"), null, room.GetWorldCoordinate(shortCutVessel.pos), shortCutVessel.room.world.game.GetNewID()); // Train too big :( 
                        room.abstractRoom.AddEntity(bringTheTrain);
                        bringTheTrain.RealizeInRoom();

                        self.room.world.GetResource().ApoEnteringWorld(bringTheTrain);
                        self.room.abstractRoom.GetResource()?.ApoEnteringRoom(bringTheTrain, bringTheTrain.pos);
                    }
                }

                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
                {
                    if (!arena.sainot) // ascendance saint
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 0;
                    }
                    else
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
                    }
                }
            }

            self.playersSpawned = true;
            arena.playerEnteredGame++;
            foreach (var player in arena.arenaSittingOnlineOrder)
            {
                var getPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(player);
                if (getPlayer != null)
                {
                    if (!getPlayer.isMe)
                    {
                        getPlayer.InvokeOnceRPC(ArenaRPCs.Arena_IncrementPlayersJoined);
                    }
                }
            }
            if (OnlineManager.lobby.isOwner)
            {
                arena.isInGame = true;
            }
        }

        // 更新竞技场会话
        public virtual void ArenaSessionUpdate(ArenaOnlineGameMode arena, ArenaGameSession session)
        {
        }
    }
}
