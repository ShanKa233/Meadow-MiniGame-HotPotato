using RainMeadow;
using System.Text.RegularExpressions;
using Menu;
using UnityEngine;
using RWCustom;
using MoreSlugcats;
using Smoke;
using System.Collections.Generic;
using MiniGameHotPotato;
using BepInEx;

namespace Meadow_MiniGame_HotPotato
{
    public class HotPotatoArena : ExternalArenaGameMode
    {
        public static string arenaName = "Hot Potato";
        public static ArenaSetup.GameTypeID PotatoArena = new ArenaSetup.GameTypeID(arenaName, register: false); // dont register so we dont add to local arena

        public override int TimerDuration { get; set; }

        // 添加倒计时相关变量防止太早在游戏中加载内容导致后面进入房间的玩家无法读取
        private int countdownTimer = 3 * 40; // 3秒 * 40帧 = 120帧的倒计时
        private const int COUNTDOWN_START = 5 * 40; // 倒计时初始值

        public static BombGameData bombData;



        private FireSmoke bombHolderSmoke; // 炸弹持有者的烟雾效果

        // 处理炸弹持有者特效
        private void UpdateBombHolderEffects(ArenaGameSession session)
        {
            // 安全检查
            if (session == null || session.Players == null)
            {
                bombHolderSmoke?.Destroy();
                bombHolderSmoke = null;
                return;
            }

            if (bombData.bombHolder == null)
            {
                // 如果没有炸弹持有者，销毁特效
                bombHolderSmoke?.Destroy();
                bombHolderSmoke = null;
                return;
            }

            // 使用缓存获取持有者实例
            Player bombHolder = bombData.bombHolderCache;

            // 如果缓存无效，尝试更新缓存
            if (bombHolder == null || !bombHolder.playerState.alive || bombHolder.room == null)
            {
                // 不直接在这里更新缓存，因为UpdateBombHolderCache已经会被调用
                // 销毁特效
                bombHolderSmoke?.Destroy();
                bombHolderSmoke = null;
                return;
            }

            try
            {
                // 处理烟雾特效
                if (bombHolder.room.ViewedByAnyCamera(bombHolder.firstChunk.pos, 300f) && bombHolderSmoke != null)
                {
                    bombHolderSmoke.Update(false);

                    // 根据剩余时间改变颜色
                    Color smokeColor = Custom.HSL2RGB(
                        Custom.LerpMap(bombData.bombTimer, 20 * 40, 0, 144, 0) / 360f,
                        1f,
                        0.5f);

                    // 发射烟雾
                    bombHolderSmoke.EmitSmoke(
                        // bombHolder.bodyChunks[1].pos,
                        //改成尾巴根部冒烟,感觉更有味道(?)
                        (bombHolder.graphicsModule as PlayerGraphics).tail[(bombHolder.graphicsModule as PlayerGraphics).tail.Length - 1].pos,
                        Custom.RNV(),
                        smokeColor,
                        (int)Custom.LerpMap(bombData.bombTimer, 20 * 40, 0, 25f, 40f));
                }

                // 如果烟雾不存在或者玩家换了房间，重新创建烟雾
                if (bombHolder.room != null && (bombHolder.room != bombHolderSmoke?.room || bombHolderSmoke == null))
                {
                    bombHolderSmoke?.Destroy();
                    bombHolderSmoke = new FireSmoke(bombHolder.room);
                    bombHolder.room.AddObject(bombHolderSmoke);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"FireSmoke effect error: {e.Message}");
                bombHolderSmoke?.Destroy();
                bombHolderSmoke = null;
            }
        }

        public override bool IsExitsOpen(ArenaOnlineGameMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            return bombData.gameOver;
        }

        public override void InitAsCustomGameType(ArenaSetup.GameTypeSetup self)
        {
            self.foodScore = 1;                                                 // 食物得分
            self.survivalScore = 3;                                             // 生存得分
            self.spearHitScore = 0;                                             // 矛命中得分
            self.repeatSingleLevelForever = false;                              // 是否无限重复单个关卡
            self.savingAndLoadingSession = true;                                // 允许保存和加载会话
            self.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard; // 标准进入规则
            self.rainWhenOnePlayerLeft = false;                                 // 当只剩一个玩家时下雨
            self.levelItems = true;                                             // 启用关卡物品
            self.fliesSpawn = false;                                            // 禁止生成苍蝇
            self.saveCreatures = false;                                         // 不保存生物状态

            self.spearsHitPlayers = false;//禁止玩家用矛互相攻击
        }
        public override string AddCustomIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud hud)
        {
            if (hud.clientSettings.owner == OnlineManager.mePlayer)
            {
                return "Symbol_StunBomb";
            }
            else
            {
                return base.AddCustomIcon(arena, hud);
            }
        }
        public void InitGame()
        {

            bombData.initialBombTimer = GameTypeSetup.BombTimesInSecondsArray[GameTypeSetup.BombTimerIndex] * 40;//初始炸弹时间
            bombData.HandleBombTimer(reset: true);
            bombData.bombHolder = null;

            //用于刷新缓存的炸弹持有者
            bombData.bombPassed = false;
            bombData.bombHolderCache = null;
            // 重置游戏状态
            bombData.gameStarted = false;
            bombData.gameOver = false;
            // 重置传递CD
            bombData.passCD = 0;
            bombData.fristBombExplode = false;
        }
        public void ResetCountdown()
        {
            // 重置倒计时到初始值
            countdownTimer = COUNTDOWN_START;
        }

        public override string TimerText()
        {
            return "";
        }
        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }

        public override void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            base.ArenaSessionCtor(arena, orig, self, game);
            if (OnlineManager.lobby.isOwner)
            {
                //初始化游戏计时器呀之类的
                InitGame();
                ResetCountdown();
            }
        }
        public override void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, HUD.HUD self, ArenaGameSession session)
        {
            //添加炸弹计时器,最重要
            //包括音效的部分都是这里处理的
            var gameHUD = new GameHUD(self, session.game.cameras[0]);
            self.AddPart(gameHUD);
            //添加文字提示一般用于显示左下角地图名和音乐
            self.AddPart(new HUD.TextPrompt(self));
            // 如果允许聊天，添加聊天HUD
            if (MatchmakingManager.currentInstance.canSendChatMessages)
                self.AddPart(new ChatHud(self, session.game.cameras[0]));
            // 添加观战HUD    
            //观战HUD感觉没啥用
            self.AddPart(new SpectatorHud(self, session.game.cameras[0]));
            // 添加在线状态HUD
            self.AddPart(new OnlineHUD(self, session.game.cameras[0], arena));
        }

        public override void ArenaSessionNextLevel(ArenaOnlineGameMode arena, On.ArenaSitting.orig_NextLevel orig, ArenaSitting self, ProcessManager process)
        {
            base.ArenaSessionNextLevel(arena, orig, self, process);
        }
        // 处理游戏更新
        public override void ArenaSessionUpdate(ArenaOnlineGameMode arena, ArenaGameSession session)
        {
            //虽然原版的方法也没写啥,但是还是先调用一下base
            base.ArenaSessionUpdate(arena, session);

            // 安全检查
            if (session == null || arena == null)
            {
                return;
            }

            if (bombData.passCD > 0 && OnlineManager.lobby.isOwner)
            {
                bombData.passCD--;
            }
            // 处理游戏前的倒计时
            if (!bombData.gameStarted)
            {
                if (countdownTimer > 0)
                {
                    countdownTimer--;
                    return; // 倒计时期间不执行其他游戏逻辑
                }
                else
                {
                    if (OnlineManager.lobby.isOwner)
                    {
                        InitGame();
                        bombData.gameStarted = true;
                    }
                    ResetCountdown();
                }
            }

            //如果炸弹爆炸了,剩余玩家低于一定的值,而且没有结算就提前结算
            if (bombData.gameOver && bombData.fristBombExplode && !session.sessionEnded)
            {
                session.EndSession();
                return;
            }

            //如果当前是房主,则进行炸弹持有者选择
            if (OnlineManager.lobby.isOwner)
            {
                if (ShouldGameEnd(session))
                {
                    bombData.gameOver = true;
                    return;
                }
                else
                {
                    bombData.gameOver = false;
                }

                // 检查当前是否有炸弹持有者
                if ((bombData.bombHolder == null || bombData.bombHolder.hasLeft) && (!ShouldGameEnd(session)))
                {
                    RandomSelectBombHolder(session);
                }
                else
                {
                    // 使用新的方法检查炸弹持有者是否有效
                    if (!IsHolderValid(session) && !ShouldGameEnd(session))
                    {
                        bombData.bombHolder = null;
                        RandomSelectBombHolder(session);
                    }
                }
                //炸弹计时器
                if (bombData.bombHolder != null)
                {
                    if (bombData.bombTimer > 0)
                    {
                        // 如果有炸弹持有者，更新计时器
                        bombData.bombTimer--;
                    }
                    if (bombData.bombTimer <= 0)
                    {
                        BombExplosion(session);

                        if (ShouldGameEnd(session))
                        {
                            bombData.bombTimer = -1;
                            bombData.gameOver = true;
                        }
                        else
                        {
                            bombData.HandleBombTimer(reset:true);
                        }
                    }
                }
            }

            if (bombData.gameOver) return;
            // 更新炸弹持有者缓存
            UpdateBombHolderCache(session);
            // 处理炸弹持有者特效
            UpdateBombHolderEffects(session);
        }


        //检查缓存是否有效
        private bool IsValidCache()
        {
            if (bombData.bombHolderCache == null) return false;

            try
            {
                // 检查缓存的玩家是否仍然是当前的炸弹持有者
                if (OnlinePhysicalObject.map.TryGetValue(bombData.bombHolderCache.abstractCreature, out var onlineObject) &&
                    onlineObject != null && onlineObject.owner == bombData.bombHolder)
                {
                    // 检查玩家状态是否有效
                    return bombData.bombHolderCache.playerState.alive &&
                           (bombData.bombHolderCache.inShortcut || bombData.bombHolderCache.room != null);
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"检查缓存玩家错误: {e.Message}");
            }

            return false;
        }
        //更新缓存的炸弹持有者
        private void UpdateBombHolderCache(ArenaGameSession session)
        {
            if (session == null) return;
            if (!NeedResetBombHolderCache()) return;

            // 如果已有缓存的玩家，先检查它是否仍然是当前的炸弹持有者
            if (IsValidCache())
            {
                bombData.bombPassed = false;
                return;
            }

            // 如果缓存无效或检查失败，再遍历所有玩家
            foreach (var abstractCreature in session.Players)
            {
                if (abstractCreature == null) continue;

                try
                {
                    if (OnlinePhysicalObject.map.TryGetValue(abstractCreature, out var onlineObject) &&
                        onlineObject != null && onlineObject.owner == bombData.bombHolder)
                    {
                        var player = abstractCreature.realizedCreature as Player;
                        if (player != null && player.playerState.alive)
                        {
                            bombData.bombHolderCache = player;
                            bombData.bombPassed = false;
                            break;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"UpdateBombHolderCache error: {e.Message}");
                    continue;
                }
            }
        }

        //检查炸弹持有者是否有效
        private bool IsHolderValid(ArenaGameSession session)
        {
            // 先检查缓存是否有效
            if (IsValidCache())
            {
                return true;
            }

            // 如果缓存无效，检查当前炸弹持有者
            foreach (var abstractCreature in session.Players)
            {
                if (abstractCreature == null) continue;

                try
                {
                    if (OnlinePhysicalObject.map.TryGetValue(abstractCreature, out var onlineObject) &&
                        onlineObject != null && onlineObject.owner == bombData.bombHolder)
                    {
                        var player = abstractCreature.realizedCreature as Player;
                        if (player != null && player.playerState.alive)
                        {
                            if (player.inShortcut || player.room != null)
                            {
                                // 更新缓存
                                bombData.bombHolderCache = player;
                                return true;
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"检查炸弹持有者错误: {e.Message}");
                    continue;
                }
            }
            return false;
        }

        //触发炸弹爆炸,在此方法内同步爆炸效果到其他玩家
        public void BombExplosion(ArenaGameSession session)
        {
            if (bombData.bombHolder == null) return;
            if (!OnlineManager.lobby.isOwner) return;//只让房主处理爆炸
            // 本地处理爆炸
            // 获取炸弹持有者的Player实例
            foreach (var abstractCreature in session.Players)
            {
                if (OnlinePhysicalObject.map.TryGetValue(abstractCreature, out var onlineObject) &&
                    onlineObject.owner == bombData.bombHolder)
                {
                    var player = abstractCreature.realizedCreature as Player;
                    if (player != null && player.room != null && player.playerState.alive)
                    {
                        //如果触发过炸弹,满足直接结算的条件,当只剩下一个人的时候自动结算
                        if (!bombData.fristBombExplode) bombData.fristBombExplode = true;
                        ExplosionPlayer_Local(player);
                        foreach (var onlinePlayer in OnlineManager.players)
                        {
                            if (!onlinePlayer.isMe)
                            {
                                onlinePlayer.InvokeOnceRPC(HotPotatoArenaRPCs.ExplosionPlayer, bombData.bombHolder);
                            }
                        }
                        return;
                    }
                }
            }
        }
        //检查需不需要刷新缓存的炸弹持有者
        public bool NeedResetBombHolderCache()
        {
            // 基础检查
            if (bombData == null) return false;
            if (bombData.gameOver) return false;

            // 需要重置的情况：
            return bombData.bombPassed || // 炸弹被传递
                   bombData.bombHolderCache == null || // 缓存为空
                   bombData.bombHolder == null || // 没有当前持有者
                   (bombData.bombHolderCache != null && // 缓存的玩家已死亡或不在游戏中
                    (!bombData.bombHolderCache.playerState.alive ||
                     bombData.bombHolderCache.room == null)) ||
                   !IsValidCache(); // 额外检查缓存是否有效
        }

        //本地显示爆炸效果
        public void ExplosionPlayer_Local(Player player)
        {
            var room = player.room;
            var vector = player.bodyChunks[1].pos;
            room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, player.ShortCutColor()));
            room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            room.AddObject(new ExplosionSpikes(room, vector, 14, 30f, 9f, 7f, 170f, player.ShortCutColor()));
            room.AddObject(new ShockWave(vector, 330f, 0.045f, 5));
            room.AddObject(new Explosion(room, player, vector, 7, 250f, 2, 0, 20, 0f, player, 0.7f, 5f, 1f));
            room.PlaySound(SoundID.Bomb_Explode, vector, player.abstractCreature);

            for (int i = 0; i < 30; i++)
            {
                room.AddObject(new APieceOfSlug(vector, (Custom.RNV() + Vector2.up * 2).normalized * 40f * Random.value + player.mainBodyChunk.vel, player));
            }
            player.Die();
            player.Destroy();

            bombData.bombHolderCache = null;
        }

        // 获取存活玩家数量
        public int GetAlivePlayersCount(ArenaGameSession session)
        {
            int aliveCount = 0;
            foreach (var abstractCreature in session.Players)
            {
                var player = abstractCreature.realizedCreature as Player;
                if (player != null && player.playerState.alive)
                {
                    if (player.inShortcut || player.room != null)
                    {
                        aliveCount++;
                    }
                }
            }
            return aliveCount;
        }

        // 检查游戏是否应该结束
        public bool ShouldGameEnd(ArenaGameSession session)
        {
            int alivePlayers = session.PlayersStillActive(false, false);
            return alivePlayers < MiniGameHotPotato.MiniGameHotPotato.options.MinPlayersRequired.Value;
        }

        // 随机选择炸弹持有者
        public void RandomSelectBombHolder(ArenaGameSession session)
        {
            if (session == null || session.Players == null || !OnlineManager.lobby.isOwner)
                return;

            List<(OnlinePlayer onlinePlayer, Player player)> eligiblePlayers = new List<(OnlinePlayer, Player)>();

            // 查找所有存活的玩家
            foreach (var abstractCreature in session.Players)
            {
                if (abstractCreature == null) continue;

                try
                {
                    if (OnlinePhysicalObject.map.TryGetValue(abstractCreature, out var onlineObject) &&
                        onlineObject != null && onlineObject.owner != null)
                    {
                        var player = abstractCreature.realizedCreature as Player;
                        if (player != null && player.playerState.alive)
                        {
                            if (player.inShortcut || player.room != null)
                            {
                                eligiblePlayers.Add((onlineObject.owner, player));
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"选择炸弹持有者错误: {e.Message}");
                    continue;
                }
            }

            // 如果有存活的玩家，随机选择一个作为炸弹持有者
            if (eligiblePlayers.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, eligiblePlayers.Count);
                bombData.HandleBombTimer(reset: true);
                bombData.bombHolder = eligiblePlayers[randomIndex].onlinePlayer;
                bombData.bombHolderCache = eligiblePlayers[randomIndex].player; // 直接缓存Player实例

                // 传递炸弹的CD
                bombData.passCD = 10;
                // 击晕新持有者防止反复触发
                eligiblePlayers[randomIndex].player.Stun(40);

                // 同步到其他玩家
                foreach (var player in OnlineManager.players)
                {
                    if (!player.isMe)
                    {
                        player.InvokeOnceRPC(HotPotatoArenaRPCs.PassBomb, eligiblePlayers[randomIndex].onlinePlayer);
                    }
                }
            }
        }
    }
}