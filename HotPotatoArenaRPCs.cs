using RainMeadow;
using RWCustom;
using UnityEngine;

namespace Meadow_MiniGame_HotPotato
{
    public static class HotPotatoArenaRPCs
    {
        // 可以添加传递炸弹的RPC方法
        [RainMeadow.RPCMethod]
        public static void PassBomb(OnlinePlayer newHolder)
        {
            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                // 给新的炸弹持有者添加晕眩效果
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                foreach (var abstractCreature in game.session.Players)
                {
                    if (abstractCreature != null &&
                        OnlinePhysicalObject.map.TryGetValue(abstractCreature, out var onlineObject) &&
                        onlineObject != null && onlineObject.owner == newHolder)
                    {
                        var player = abstractCreature.realizedCreature as Player;
                        if (player != null && player.room != null && player.playerState.alive)
                        {
                            HotPotatoArena.bombData.bombHolderCache = player;
                            HotPotatoArena.bombData.bombPassed = true;

                            player.room.PlaySound(SoundID.MENU_Add_Level, player.firstChunk, false, 1, 2);
                            player.Stun(40); // 晕眩40tick
                            break;
                        }
                    }
                }
            }
        }
        // 添加炸弹爆炸的RPC方法
        [RainMeadow.RPCMethod]
        public static void ExplosionPlayer(OnlinePlayer bombHolder)
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
        }
    }
}
