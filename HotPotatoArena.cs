using RainMeadow;
using System.Text.RegularExpressions;
using Menu;
using UnityEngine;
using RWCustom;
using MoreSlugcats;
using Smoke;

namespace Meadow_MiniGame_HotPotato
{
    public class HotPotatoArena : ExternalArenaGameMode
    {
        public static string arenaName = "HotPotatoArena";
        public static ArenaSetup.GameTypeID PotatoArena = new ArenaSetup.GameTypeID(arenaName, register: false); // dont register so we dont add to local arena

        public override int TimerDuration { get; set; }


        public static int bombTimer; // 炸弹爆炸计时器
        public static int nextBombTimer; // 下次重置后的炸弹时间
        public static OnlinePlayer bombHolder = null; // 炸弹持有者
        public bool IsGameOver { get; set; } = false; // 游戏是否结束


        public const int initialBombTimer = 30 * 40; // 初始炸弹爆炸时间
        public const int minPlayersRequired = 2; // 最少需要的玩家数量


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

            if (HotPotatoArena.bombHolder == null)
            {
                // 如果没有炸弹持有者，销毁特效
                bombHolderSmoke?.Destroy();
                bombHolderSmoke = null;
                return;
            }

            // 获取炸弹持有者
            Player bombHolder = null;
            foreach (var abstractCreature in session.Players)
            {
                if (abstractCreature == null) continue;

                try
                {
                    if (OnlinePhysicalObject.map.TryGetValue(abstractCreature, out var onlineObject) &&
                        onlineObject != null && onlineObject.owner == HotPotatoArena.bombHolder)
                    {
                        bombHolder = abstractCreature.realizedCreature as Player;
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"UpdateBombHolderEffects error: {e.Message}");
                    continue;
                }
            }

            if (bombHolder == null || !bombHolder.playerState.alive || bombHolder.room == null)
            {
                // 炸弹持有者不存在或已死亡，销毁特效
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
                        Custom.LerpMap(bombTimer, 20 * 40, 0, 144, 0) / 360f,
                        1f,
                        0.5f);

                    // 发射烟雾
                    bombHolderSmoke.EmitSmoke(
                        bombHolder.firstChunk.pos,
                        Custom.RNV(),
                        smokeColor,
                        (int)Custom.LerpMap(bombTimer, 20 * 40, 0, 25f, 40f));
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
            return IsGameOver;
        }

        public override void InitAsCustomGameType(ArenaSetup.GameTypeSetup self)
        {
            self.foodScore = 1;                     // 食物得分
            self.survivalScore = 3;                 // 生存得分
            self.spearHitScore = 0;                 // 矛命中得分
            self.repeatSingleLevelForever = false;  // 是否无限重复单个关卡
            self.savingAndLoadingSession = true;    // 允许保存和加载会话
            self.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard;  // 标准进入规则
            self.rainWhenOnePlayerLeft = false;      // 当只剩一个玩家时下雨
            self.levelItems = true;                 // 启用关卡物品
            self.fliesSpawn = false;                 // 允许生成苍蝇
            self.saveCreatures = false;             // 不保存生物状态

            self.spearsHitPlayers = false;//禁止玩家用矛互相攻击
            // self.evilAI = false;//禁止邪恶AI
        }
        public void InitGame()
        {
            bombTimer = initialBombTimer;
            nextBombTimer = initialBombTimer;
            bombHolder = null;

            IsGameOver = false;
        }

        public override void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, HUD.HUD self, ArenaGameSession session)
        {
            //添加炸弹计时器,最重要
            self.AddPart(new BombTImer(self, self.fContainers[0], this));

            // 如果允许聊天，添加聊天HUD
            if (MatchmakingManager.currentInstance.canSendChatMessages) 
                self.AddPart(new ChatHud(self, session.game.cameras[0]));
            
            // 添加观战HUD    
            self.AddPart(new SpectatorHud(self, session.game.cameras[0]));
            // 添加在线状态HUD
            self.AddPart(new OnlineHUD(self, session.game.cameras[0], arena));
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

            InitGame();

            //主机选择炸弹持有者
            if (OnlineManager.lobby.isOwner)
            {
                // 随机选择一个玩家作为炸弹持有者
                if (OnlineManager.players.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, OnlineManager.players.Count);
                    HotPotatoArena.bombHolder = OnlineManager.players[randomIndex];

                    // 同步到其他玩家
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeOnceRPC(HotPotatoArenaRPCs.SyncRemix, bombTimer, bombHolder, IsGameOver);
                        }
                    }
                }
            }

            // 确保bombTimer不为0或负数
            if (bombTimer <= 0)
            {
                bombTimer = initialBombTimer;
            }

            UnityEngine.Debug.Log($"ArenaSessionCtor: bombTimer initialized to {bombTimer}");
        }
        public override void ArenaSessionUpdate(ArenaOnlineGameMode arena, ArenaGameSession session)
        {
            base.ArenaSessionUpdate(arena, session);

            // 安全检查
            if (session == null || arena == null)
            {
                return;
            }

            if (OnlineManager.lobby.isOwner)
            {
                // 检查当前是否有炸弹持有者
                if (bombHolder == null)
                {
                    // 如果没有炸弹持有者，随机选择一个玩家
                    if (OnlineManager.players != null && OnlineManager.players.Count > 0)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, OnlineManager.players.Count);
                        bombHolder = OnlineManager.players[randomIndex];

                        // 同步到其他玩家
                        foreach (var player in OnlineManager.players)
                        {
                            if (player != null && !player.isMe)
                            {
                                player.InvokeOnceRPC(HotPotatoArenaRPCs.SyncRemix, bombTimer, bombHolder, IsGameOver);
                            }
                        }
                    }
                }
            }
            if (bombHolder != null)
            {
                if (bombTimer >= 0)
                {
                    // 如果有炸弹持有者，更新计时器
                    bombTimer--;
                    UnityEngine.Debug.Log($"bombTimer: {bombTimer}, Bomb Holder: {HotPotatoArena.bombHolder.inLobbyId}");

                }
                if (bombTimer <= 0)
                {
                    BombExplosion(session);
                    // 检查爆炸后是否应该结束游戏
                    if (ShouldGameEnd(session))
                    {
                        IsGameOver = true;
                        bombTimer = -1;
                        return;
                    }
                    nextBombTimer = Custom.IntClamp(nextBombTimer % 40 - 5, 4, initialBombTimer) * 40;
                    bombTimer = nextBombTimer;
                }
            }
            // 处理炸弹持有者特效
            UpdateBombHolderEffects(session);
        }
        public void BombExplosion(ArenaGameSession session)
        {
            if (HotPotatoArena.bombHolder == null) return;

            // 本地处理爆炸
            // 获取炸弹持有者的Player实例
            foreach (var abstractCreature in session.Players)
            {
                if (OnlinePhysicalObject.map.TryGetValue(abstractCreature, out var onlineObject) &&
                    onlineObject.owner == HotPotatoArena.bombHolder)
                {
                    var player = abstractCreature.realizedCreature as Player;
                    if (player != null && player.room != null && player.playerState.alive)
                    {
                        var room = player.room;
                        var vector = player.bodyChunks[1].pos;
                        room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, player.ShortCutColor()));
                        room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                        room.AddObject(new ExplosionSpikes(room, vector, 14, 30f, 9f, 7f, 170f, player.ShortCutColor()));
                        room.AddObject(new ShockWave(vector, 330f, 0.045f, 5));
                        room.AddObject(new Explosion(room, player, vector, 7, 250f, 30f, 1, 280f, 0f, player, 0.7f, 160f, 1f));
                        room.PlaySound(SoundID.Bomb_Explode, vector, player.abstractCreature);

                        for (int i = 0; i < 30; i++)
                        {
                            room.AddObject(new APieceOfSlug(vector, (Custom.RNV() + Vector2.up * 2).normalized * 40f * Random.value + player.mainBodyChunk.vel, player));
                        }
                        player.Die();
                        player.Destroy();

                        // 同步爆炸效果到其他玩家
                        if (OnlineManager.lobby.isOwner)
                        {
                            foreach (var onlinePlayer in OnlineManager.players)
                            {
                                if (!onlinePlayer.isMe)
                                {
                                    onlinePlayer.InvokeOnceRPC(HotPotatoArenaRPCs.BombExplode, HotPotatoArena.bombHolder);
                                }
                            }
                        }

                        break;
                    }
                }
            }
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
                    aliveCount++;
                }
            }
            return aliveCount;
        }

        // 检查游戏是否应该结束
        private bool ShouldGameEnd(ArenaGameSession session)
        {
            int alivePlayers = GetAlivePlayersCount(session);
            return alivePlayers < minPlayersRequired;
        }
    }
}