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
            if (RainMeadow.RainMeadow.isArenaMode(out var arena) && MiniGameHotPotato.MiniGameHotPotato.isMyCoolGameMode(arena, out var potatoArena))
            {
                // 给新的炸弹持有者添加晕眩效果
                var game = (RWCustom.Custom.rainWorld?.processManager?.currentMainLoop as RainWorldGame);
                if (game == null) return;
                foreach (var abstractCreature in game.session.Players)
                {
                    if (abstractCreature != null &&
                        OnlinePhysicalObject.map.TryGetValue(abstractCreature, out var onlineObject) &&
                        onlineObject != null && onlineObject.owner == newHolder)
                    {
                        var player = abstractCreature.realizedCreature as Player;
                        if (player != null && player.room != null && player.playerState.alive)
                        {

                            HotPotatoArena.bombData.HandleBombTimer(reduceSecond: MiniGameHotPotato.MiniGameHotPotato.options.BombReduceTime.Value);

                            HotPotatoArena.bombData.bombHolder = newHolder;
                            HotPotatoArena.bombData.bombHolderCache = player;
                            if (HotPotatoArena.bombData.passCD <= 0)
                            {//给传递炸弹的时间加了一个小cd估计可以避免连续发声
                                player.room.PlaySound(SoundID.MENU_Add_Level, player.firstChunk, false, 1, 2);
                            }
                            HotPotatoArena.bombData.passCD = 10;
                            HotPotatoArena.bombData.bombPassed = true;

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
            if (RainMeadow.RainMeadow.isArenaMode(out var arena) && MiniGameHotPotato.MiniGameHotPotato.isMyCoolGameMode(arena, out var potatoArena))
            {
                var game = (RWCustom.Custom.rainWorld?.processManager?.currentMainLoop as RainWorldGame);
                if (game == null) return;
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
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
