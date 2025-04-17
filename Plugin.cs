using BepInEx;
using IL;
using Meadow_MiniGame_HotPotato;
using RainMeadow;
using RWCustom;
using System;
using System.Security.Permissions;
using UnityEngine;

//#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace MiniGameHotPotato
{
    [BepInPlugin("ShanKa.MiniGameHotPotato", "MiniGameHotPotato", "0.1.0")]
    public partial class MiniGameHotPotato : BaseUnityPlugin
    {

        public static string modName = "MiniGameHotPotato";
        public static MiniGameHotPotato instance;
        private bool init;
        private bool fullyInit;
        private bool addedMod = false;

        // public static OnlineGameMode.OnlineGameModeType hotPotatoGameMode = new OnlineGameMode.OnlineGameModeType("HotPotato", true);
        public void OnEnable()
        {
            instance = this;

            On.RainWorld.OnModsInit += RainWorld_OnModsInit;

        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (init) return;
            init = true;

            try
            {

                // 设置游戏模式
                // 打算先做一个竞技场的版本先试试看, 如果可以的话再做大厅的版本
                // RainMeadow.OnlineGameMode.RegisterType(hotPotatoGameMode, typeof(HotPotatoGameMode), "Hot Potato!");
                OnlineResource.OnAvailable += OnlineResource_OnAvailable;

                On.Menu.MultiplayerMenu.ctor += MultiplayerMenu_ctor;
                On.Player.Collide += Player_Collide;

                fullyInit = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                fullyInit = false;
            }
        }

        private void OnlineResource_OnAvailable(OnlineResource resource)
        {
            HotPotatoArena.bombData = resource.AddData(new BombGameData());
        }

        private void Player_Collide(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            orig(self, otherObject, myChunk, otherChunk);
            if (RainMeadow.RainMeadow.isArenaMode(out var arena) && arena.onlineArenaGameMode is HotPotatoArena potatoArena)
            {
                //只让主机处理防止反复传递
                if (OnlineManager.lobby.isOwner)
                {
                    // 使用缓存系统检查自己是否是炸弹持有者
                    if (HotPotatoArena.bombData.bombHolderCache == self)
                    {
                        // 确保碰撞的是另一个玩家
                        if (self.Consious && self.stun <= 2 && otherObject is Player otherPlayer && otherPlayer != self)
                        {
                            // 确保另一个玩家活着且能被传递炸弹
                            if (otherPlayer.playerState.alive)
                            {
                                // 获取另一个玩家的OnlinePlayer实例
                                if (OnlinePhysicalObject.map.TryGetValue(otherPlayer.abstractCreature, out var otherOnlineObject) &&
                                    otherOnlineObject != null && otherOnlineObject.owner != null)
                                {
                                    // 更新炸弹计时器和持有者
                                    HotPotatoArena.bombData.nextBombTimer = Custom.IntClamp(HotPotatoArena.bombData.nextBombTimer / 40 - 5, 4, 30) * 40;
                                    HotPotatoArena.bombData.bombTimer = HotPotatoArena.bombData.nextBombTimer;
                                    HotPotatoArena.bombData.bombHolder = otherOnlineObject.owner;
                                    HotPotatoArena.bombData.bombHolderCache = otherPlayer; // 直接更新缓存
                                    // 击晕新持有者防止反复触发
                                    otherPlayer.Stun(60);

                                    // 同步到其他玩家
                                    foreach (var player in OnlineManager.players)
                                    {
                                        if (!player.isMe)
                                        {
                                            player.InvokeOnceRPC(HotPotatoArenaRPCs.PassBomb, otherOnlineObject.owner);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private void MultiplayerMenu_ctor(On.Menu.MultiplayerMenu.orig_ctor orig, Menu.MultiplayerMenu self, ProcessManager manager)
        {
            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                var myNewGamemode = new HotPotatoArena();
                if (!arena.registeredGameModes.ContainsKey(myNewGamemode))
                {
                    arena.registeredGameModes.Add(myNewGamemode, HotPotatoArena.PotatoArena.value);
                }
            }
            orig(self, manager);
        }
    }
}