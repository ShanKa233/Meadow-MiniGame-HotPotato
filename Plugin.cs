using BepInEx;
using IL;
using Meadow_MiniGame_HotPotato;
using RainMeadow;
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

                On.Menu.MultiplayerMenu.ctor += MultiplayerMenu_ctor;
                On.Player.Collide += Player_Collide;
                // On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
                fullyInit = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                fullyInit = false;
            }
        }

        private void Player_Collide(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            orig(self, otherObject, myChunk, otherChunk);
            if (RainMeadow.RainMeadow.isArenaMode(out var arena) && arena.onlineArenaGameMode is HotPotatoArena potatoArena)
            {
                // 确保碰撞的是另一个玩家
                if (self.Consious&&otherObject is Player otherPlayer && otherPlayer != self)
                {
                    // 检查自己是否是炸弹持有者
                    if (OnlinePhysicalObject.map.TryGetValue(self.abstractCreature, out var myOnlineObject) &&
                        myOnlineObject.owner == potatoArena.potatoData.bombHolder)
                    {
                        // 获取另一个玩家的OnlinePlayer实例
                        if (OnlinePhysicalObject.map.TryGetValue(otherPlayer.abstractCreature, out var otherOnlineObject))
                        {
                            // 确保两个玩家都活着
                            if (self.playerState.alive && otherPlayer.playerState.alive)
                            {
                                // 传递炸弹给新玩家
                                foreach (var player in OnlineManager.players)
                                {
                                    if (!player.isMe)
                                    {
                                        player.InvokeOnceRPC(HotPotatoArenaRPCs.PassBomb, otherOnlineObject.owner);
                                    }
                                }
                                potatoArena.potatoData.bombHolder = otherOnlineObject.owner;
                            }
                        }
                    }
                }
            }
        }
        private void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            if (RainMeadow.RainMeadow.isArenaMode(out var arena) && arena.onlineArenaGameMode is HotPotatoArena)
            {
                var potatoArena = (HotPotatoArena)arena.onlineArenaGameMode;

                self.AddPart(new BombTImer(self, self.fContainers[0], potatoArena));
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