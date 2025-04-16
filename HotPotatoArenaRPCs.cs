using RainMeadow;
using UnityEngine;

namespace Meadow_MiniGame_HotPotato
{
    public static class HotPotatoArenaRPCs
    {
        [RainMeadow.RPCMethod]
        public static void SyncRemix(RPCEvent rpcEvent, int bombTimer, OnlinePlayer bombHolder, bool isGameOver)
        {
            if(RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                var potatoArena = (HotPotatoArena)arena.onlineArenaGameMode;
                // 确保bombTimer值有效
                if (bombTimer <= 0)
                {
                    HotPotatoArena.bombTimer = HotPotatoArena.initialBombTimer;
                    Debug.LogWarning($"Received invalid bombTimer value: {bombTimer}, setting to default: {HotPotatoArena.initialBombTimer}");
                }
                else
                {
                    HotPotatoArena.bombTimer = bombTimer;
                }
                
                potatoArena.potatoData.bombHolder = bombHolder;
                potatoArena.IsGameOver = isGameOver;
                
                Debug.Log($"SyncRemix: bombTimer synchronized to {HotPotatoArena.bombTimer}");
            }
        }

        // 可以添加传递炸弹的RPC方法
        [RainMeadow.RPCMethod]
        public static void PassBomb(OnlinePlayer newHolder)
        {
            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
                var potatoArena = (HotPotatoArena)arena.onlineArenaGameMode;
                Debug.Log($"Passing bomb to player: {newHolder.inLobbyId}");
                
                // 在传递炸弹时重置爆炸时间
                if (OnlineManager.lobby.isOwner)
                {
                    // 确保nextBombTimer值有效
                    if (HotPotatoArena.bombTimer <= 0)
                    {
                        HotPotatoArena.nextBombTimer = HotPotatoArena.initialBombTimer;
                    }
                    
                    HotPotatoArena.bombTimer = HotPotatoArena.nextBombTimer;
                    Debug.Log($"Reset bomb timer to: {HotPotatoArena.bombTimer}");
                    
                    // 同步新的计时器状态给所有玩家
                    foreach (var player in OnlineManager.players)
                    {
                        if (player != null && !player.isMe)
                        {
                            player.InvokeOnceRPC(SyncRemix, HotPotatoArena.bombTimer, newHolder, potatoArena.IsGameOver);
                        }
                    }
                }
                
                potatoArena.potatoData.bombHolder = newHolder;

                // 给新的炸弹持有者添加晕眩效果
                foreach (var abstractCreature in game.session.Players)
                {
                    if (abstractCreature != null && 
                        OnlinePhysicalObject.map.TryGetValue(abstractCreature, out var onlineObject) &&
                        onlineObject != null && onlineObject.owner == newHolder)
                    {
                        var player = abstractCreature.realizedCreature as Player;
                        if (player != null && player.room != null && player.playerState.alive)
                        {
                            player.Stun(60); // 晕眩60tick
                            break;
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Failed to pass bomb: Invalid game mode or arena not found");
            }
        }

        // 添加炸弹爆炸的RPC方法
        [RainMeadow.RPCMethod]
        public static void BombExplode(OnlinePlayer bombHolder)
        {
            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
                var potatoArena = (HotPotatoArena)arena.onlineArenaGameMode;
                
                // 找到对应的玩家并引爆
                foreach (var abstractCreature in game.session.Players)
                {
                    if (OnlinePhysicalObject.map.TryGetValue(abstractCreature, out var onlineObject) &&
                        onlineObject.owner == bombHolder)
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
                                room.AddObject(new APieceOfSlug(vector, (RWCustom.Custom.RNV() + Vector2.up * 2).normalized * 40f * Random.value + player.mainBodyChunk.vel, player));
                            }
                            player.Die();
                            player.Destroy();
                            break;
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Failed to explode bomb: Invalid game mode or arena not found");
            }
        }
    }
}
