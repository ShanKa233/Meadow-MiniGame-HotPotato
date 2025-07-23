using BepInEx;
using IL;
using Meadow_MiniGame_HotPotato;
using MoreSlugcats;
using RainMeadow;
using RainMeadow.UI;
using RWCustom;
using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Permissions;
using UnityEngine;
using Meadow_MiniGame_HotPotato.UI;

//#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace MiniGameHotPotato
{
    [BepInPlugin(modID, modeName, version)]
    public partial class MiniGameHotPotato : BaseUnityPlugin
    {
        public const string modID = "ShanKa.MiniGameHotPotato";
        public const string modeName = "MiniGameHotPotato";
        public const string version = "0.1.31";
        public static MiniGameHotPotato instance;
        public static HotPotatoOptions options;
        private bool init;
        private bool fullyInit;
        private bool addedMod = false;

        // 添加配置选项

        // public static OnlineGameMode.OnlineGameModeType hotPotatoGameMode = new OnlineGameMode.OnlineGameModeType("HotPotato", true);

        public static bool isHotPotatoGameMode(ArenaOnlineGameMode arena, out HotPotatoArena tb)
        {
            tb = null;
            if (arena.currentGameMode == HotPotatoArena.PotatoArena.value)
            {
                tb = (arena.registeredGameModes.FirstOrDefault(x => x.Key == HotPotatoArena.PotatoArena.value).Value as HotPotatoArena);
                return true;
            }
            return false;
        }
        public void OnEnable()
        {
            instance = this;
            options = new HotPotatoOptions(this);

            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (init) return;
            init = true;

            try
            {
                // 添加配置选项到机器连接器
                MachineConnector.SetRegisteredOI(modID, options);

                On.Menu.Menu.ctor += Menu_ctor;

                //用来在切换模式时改变背景图
                HotPotatoScenes.InitHook();
                PotatoArenaMenu.InitHook();
                PotatoPlaylist.InitHook();//用来增加按钮提供地图预设

                OnlineResource.OnAvailable += OnlineResource_OnAvailable;

                // On.Menu.MultiplayerMenu.ctor += MultiplayerMenu_ctor;//老的游戏模式注册内容

                //处理传递炸弹的碰撞事件
                On.Player.Collide += Player_Collide;
                //炸弹进入捷径的时候封锁捷径,并且把封锁信息同步给竞技场
                On.Creature.SuckedIntoShortCut += Creature_SuckedIntoShortCut;
                //处理玩家无法进入被烈焰封锁的洞穴的效果
                On.Player.Update += Player_Update;


                LoadPotatoIcon();

                fullyInit = true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                fullyInit = false;
            }
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {

            if (RainMeadow.RainMeadow.isArenaMode(out var arena)
                && isHotPotatoGameMode(arena, out var potatoArena))
            {
                if (potatoArena.blockedShortCut != null && self != HotPotatoArena.bombData.bombHolderCache)
                {
                    // 在更新前检查玩家是否尝试进入被锁定的捷径
                    if (self.enteringShortCut.HasValue)
                    {
                        Room room = self.room;
                        IntVector2 shortcutPos = self.enteringShortCut.Value;
                        ShortcutData shortcut = room.shortcutData(shortcutPos);

                        if (shortcut.StartTile == potatoArena.blockedShortCut.startTile ||
                            shortcut.StartTile == potatoArena.blockedShortCut.destTile)
                        {
                            // 取消进入捷径
                            self.enteringShortCut = null;

                            // 弹出管道的音效
                            room.PlaySound(SoundID.Spear_Bounce_Off_Wall, self.mainBodyChunk);

                            // 给玩家弹出管道
                            IntVector2 direction = room.ShorcutEntranceHoleDirection(shortcutPos);
                            for (int i = 0; i < self.bodyChunks.Length; i++)
                            {
                                self.bodyChunks[i].vel += RWCustom.Custom.IntVector2ToVector2(direction) * 20f;
                            }
                        }
                    }
                }
            }

            // 调用原始更新方法
            orig(self, eu);
        }
        private void Creature_SuckedIntoShortCut(On.Creature.orig_SuckedIntoShortCut orig, Creature self, IntVector2 entrancePos, bool carriedByOther)
        {

            if (RainMeadow.RainMeadow.isArenaMode(out var arena)
                && isHotPotatoGameMode(arena, out var potatoArena)
                && self is Player player
                && player == HotPotatoArena.bombData.bombHolderCache)
            {
                // 这是玩家进入捷径
                Room room = self.room;
                ShortcutData shortcut = room.shortcutData(entrancePos);

                // 获取起点和终点信息
                IntVector2 startTile = entrancePos;
                IntVector2 destTile = shortcut.DestTile;
                IntVector2 startDir = room.ShorcutEntranceHoleDirection(startTile);
                IntVector2 destDir = room.ShorcutEntranceHoleDirection(destTile);

                potatoArena.blockedShortCut = new ShortCutBlocker(shortcut, startTile, destTile, startDir, destDir, room);
                // 处理炸弹持有者进入管道的情况
            }
            orig(self, entrancePos, carriedByOther);
        }


        private void Menu_ctor(On.Menu.Menu.orig_ctor orig, Menu.Menu self, ProcessManager manager, ProcessManager.ProcessID ID)
        {
            orig(self, manager, ID);
            if (self is ArenaOnlineLobbyMenu)
            {
                AddNewMode();
            }
        }

        private void AddNewMode()
        {

            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                arena.AddExternalGameModes(HotPotatoArena.PotatoArena, new HotPotatoArena());
            }
        }

        private void LoadPotatoIcon()
        {
            Futile.atlasManager.LoadImage("illustrations/Potato_Symbol_Show_Thumbs");
            Futile.atlasManager.LoadImage("illustrations/Potato_Symbol_Clear_All");
        }
        private void OnlineResource_OnAvailable(OnlineResource resource)
        {
            HotPotatoArena.bombData = resource.AddData(new BombGameData());
        }


        private void Player_Collide(On.Player.orig_Collide orig, Player self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            orig(self, otherObject, myChunk, otherChunk);
            if (RainMeadow.RainMeadow.isArenaMode(out var arena) && isHotPotatoGameMode(arena, out var potatoArena))
            {

                //如果炸弹是这个玩家而且CD小于0
                if (HotPotatoArena.bombData.bombHolder != null
                && HotPotatoArena.bombData.bombHolder.isMe//这个机子是炸弹的端口
                && HotPotatoArena.bombData.bombHolderCache == self//这个碰撞的角色是炸弹的持有者
                && HotPotatoArena.bombData.passCD <= 0)//传炸弹的CD小于0
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
                                HotPotatoArena.bombData.HandleBombTimer(reduceSecond: options.BombReduceTime.Value);
                                HotPotatoArena.bombData.bombHolder = otherOnlineObject.owner;
                                HotPotatoArena.bombData.bombHolderCache = otherPlayer; // 直接更新缓存

                                // 传递炸弹的音效
                                if (HotPotatoArena.bombData.passCD <= 0)
                                {
                                    otherPlayer.room.PlaySound(SoundID.MENU_Add_Level, otherPlayer.firstChunk, false, 1, 2);
                                }
                                // 传递炸弹的CD
                                HotPotatoArena.bombData.passCD = 10;
                                // 击晕新持有者防止反复触发
                                otherPlayer.Stun(40);

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
        private void MultiplayerMenu_ctor(On.Menu.MultiplayerMenu.orig_ctor orig, Menu.MultiplayerMenu self, ProcessManager manager)
        {
            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                if (!arena.registeredGameModes.ContainsKey(HotPotatoArena.PotatoArena.value))
                {
                    arena.registeredGameModes.Add(HotPotatoArena.PotatoArena.value, new HotPotatoArena());
                }
            }
            orig(self, manager);
        }
    }
}