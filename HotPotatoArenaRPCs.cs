using RainMeadow;
using RWCustom;
using UnityEngine;

namespace Meadow_MiniGame_HotPotato
{
    public static class HotPotatoArenaRPCs
    {
        [RainMeadow.RPCMethod]
        public static void SyncRemix(RPCEvent rpcEvent, int bombTimer, OnlinePlayer bombHolder, bool isGameOver)
        {
            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                var potatoArena = (HotPotatoArena)arena.onlineArenaGameMode;
                // 确保bombTimer值有效
                if (bombTimer <= 0)
                {
                    HotPotatoArena.bombTimer = HotPotatoArena.initialBombTimer;
                    // Debug.LogWarning($"Received invalid bombTimer value: {bombTimer}, setting to default: {HotPotatoArena.initialBombTimer}");
                }
                else
                {
                    HotPotatoArena.bombTimer = bombTimer;
                }

                HotPotatoArena.bombHolder = bombHolder;
                potatoArena.IsGameOver = isGameOver;

            }
        }

        // 可以添加传递炸弹的RPC方法
        [RainMeadow.RPCMethod]
        public static void PassBomb(OnlinePlayer newHolder)
        {
            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                var potatoArena = (HotPotatoArena)arena.onlineArenaGameMode;
                // RainMeadow.RainMeadow.Debug($"Passing bomb to player: {newHolder.inLobbyId}");

                HotPotatoArena.nextBombTimer = Custom.IntClamp(HotPotatoArena.nextBombTimer % 40 - 5, 4, HotPotatoArena.initialBombTimer) * 40;
                HotPotatoArena.bombTimer = HotPotatoArena.nextBombTimer;
                HotPotatoArena.bombHolder = newHolder;

                // RainMeadow.RainMeadow.Debug($"Reset bomb timer to: {HotPotatoArena.bombTimer}");

                // 同步新的计时器状态给所有玩家
                foreach (var player in OnlineManager.players)
                {
                    if (player != null && !player.isMe)
                    {
                        player.InvokeOnceRPC(SyncRemix, HotPotatoArena.bombTimer, HotPotatoArena.bombHolder, potatoArena.IsGameOver);
                    }
                }


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
                // Debug.LogError("Failed to pass bomb: Invalid game mode or arena not found");
            }
        }
        public static void PassBomb_Local(OnlinePlayer newHolder)
        {

            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                var potatoArena = (HotPotatoArena)arena.onlineArenaGameMode;

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
                            potatoArena.ExplosionPlayer_Local(player);
                            return;
                        }
                    }
                }
            }
            else
            {
                // Debug.LogError("Failed to explode bomb: Invalid game mode or arena not found");
            }
        }
    }
}
