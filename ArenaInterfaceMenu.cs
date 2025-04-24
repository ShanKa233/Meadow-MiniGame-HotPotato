using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using JollyCoop.JollyMenu;
using Menu;
using MonoMod;
using MonoMod.RuntimeDetour;
using RainMeadow;
using RWCustom;
using UnityEngine;
// using static Menu.SandboxSettingsInterface;

namespace Meadow_MiniGame_HotPotato
{
    public class PotatoArenaMenu
    {
        public InteractiveMenuScene scene;
        public static ConditionalWeakTable<MultiplayerMenu, PotatoArenaMenu> menuPotatoCWT = new ConditionalWeakTable<MultiplayerMenu, PotatoArenaMenu>();
        public MenuLabel versionLabel;
        public MultipleChoiceArray TimerArray;

        // 添加数字调整变量
        private static Hook AreanaLobbyMenu_UpdateGameModeLabel_Hook;//用于hook
        private BombScore ReduceTimeArray;
        public static void InitHook()
        {
            On.Menu.ArenaSettingsInterface.GetSelected += ArenaSettingsInterface_GetSelected;
            On.Menu.ArenaSettingsInterface.SetSelected += ArenaSettingsInterface_SetSelected;

            Type arenaLobbyMenuType = typeof(RainMeadow.ArenaLobbyMenu);
            AreanaLobbyMenu_UpdateGameModeLabel_Hook = new Hook(
                               typeof(RainMeadow.ArenaLobbyMenu).GetMethod("UpdateGameModeLabel",
                                   BindingFlags.Instance | BindingFlags.NonPublic),
                               typeof(PotatoArenaMenu).GetMethod("ArenaLobbyMenu_UpdateGameModeLabel",
                                   BindingFlags.Static | BindingFlags.NonPublic)
                           );

            On.Menu.MultiplayerMenu.UpdateInfoText += MultiplayerMenu_UpdateInfoText;
        }


        //更新信息文本,在光标移上去的时候显示详情
        private static string MultiplayerMenu_UpdateInfoText(On.Menu.MultiplayerMenu.orig_UpdateInfoText orig, MultiplayerMenu self)
        {
            if (self.selectedObject is MultipleChoiceArray.MultipleChoiceButton)
            {
                switch ((self.selectedObject.owner as MultipleChoiceArray).IDString)
                {
                    case "MAX_BOMB_TIMER":
                        return GameTypeSetup.BombTimesInSecondsArray[(self.selectedObject as MultipleChoiceArray.MultipleChoiceButton).index] + " " + self.Translate("seconds to explode");
                }
            }

            if (self.selectedObject is SymbolButton)
            {
                if ((self.selectedObject as SymbolButton).signalText == "FILTER")
                {
                    return self.Translate("Show only Hot Potato levels");
                }
                else if ((self.selectedObject as SymbolButton).signalText == "CLEARFILTER")
                {
                    return self.Translate("Showing all levels");
                }

            }
            return orig(self);
        }


        private static void ArenaSettingsInterface_SetSelected(On.Menu.ArenaSettingsInterface.orig_SetSelected orig, ArenaSettingsInterface self, MultipleChoiceArray array, int i)
        {
            if (array.IDString == "MAX_BOMB_TIMER")
            {
                if (i != MiniGameHotPotato.MiniGameHotPotato.options.BombTimer.Value)
                {
                    HotPotatoArena.bombData.bombTimerIndex = i;
                    MiniGameHotPotato.MiniGameHotPotato.options.BombTimer.Value = i;
                    MiniGameHotPotato.MiniGameHotPotato.options._SaveConfigFile();
                }

                return;
            }
            orig(self, array, i);
        }


        private static int ArenaSettingsInterface_GetSelected(On.Menu.ArenaSettingsInterface.orig_GetSelected orig, ArenaSettingsInterface self, MultipleChoiceArray array)
        {
            if (array.IDString == "MAX_BOMB_TIMER")
            {
                return HotPotatoArena.bombData.bombTimerIndex;
            }
            return orig(self, array);
        }

        private static void ArenaLobbyMenu_UpdateGameModeLabel(Action<RainMeadow.ArenaLobbyMenu> orig, RainMeadow.ArenaLobbyMenu self)
        {
            // 调用原始方法
            orig(self);
            if (RainMeadow.RainMeadow.isArenaMode(out var arena) && ((ArenaOnlineGameMode)OnlineManager.lobby.gameMode).currentGameMode == HotPotatoArena.arenaName)
            {
                if (PotatoArenaMenu.menuPotatoCWT.TryGetValue(self, out var potatoArenaMenu))
                {
                    //处理背景
                    if (self.scene.depthIllustrations != null && self.scene.depthIllustrations.Count > 0)
                    {
                        potatoArenaMenu.scene.flatIllustrations[0].sprite.MoveInFrontOfOtherNode(self.scene.depthIllustrations[self.scene.depthIllustrations.Count - 1].sprite);
                    }
                    else if (self.scene.flatIllustrations != null && self.scene.flatIllustrations.Count > 0)
                    {
                        potatoArenaMenu.scene.flatIllustrations[0].sprite.MoveInFrontOfOtherNode(self.scene.flatIllustrations[0].sprite);
                    }

                    //更改可见度显示版本号
                    potatoArenaMenu.versionLabel.label.isVisible = true;
                    //处理部分强制关闭的按钮控件的内容
                    self.arenaSettingsInterface.spearsHitCheckbox.selectable = false;//禁止点击互相攻击按钮
                    self.arenaSettingsInterface.spearsHitCheckbox.buttonBehav.greyedOut = true;//灰掉互相攻击按钮
                    if (self.arenaSettingsInterface.spearsHitCheckbox.Checked)
                    {
                        self.arenaSettingsInterface.spearsHitCheckbox.Checked = false;//如果互相攻击按钮被勾选，则取消勾选
                    }
                    //处理炸弹计时器的按钮控件
                    if (potatoArenaMenu.TimerArray == null)
                    {
                        potatoArenaMenu.TimerArray = new MultipleChoiceArray(self.arenaSettingsInterface.menu, self.arenaSettingsInterface, self.arenaSettingsInterface
                        , self.arenaSettingsInterface.wildlifeArray.pos + new Vector2(0f, -50f)
                        , self.arenaSettingsInterface.menu.Translate("Explosion Timer:")
                        , "MAX_BOMB_TIMER"
                        , 120f, 340, 6
                        , textInBoxes: false
                        , splitText: false);
                        self.arenaSettingsInterface.subObjects.Add(potatoArenaMenu.TimerArray);
                    }
                    else
                    {

                        potatoArenaMenu.TimerArray.pos = self.arenaSettingsInterface.wildlifeArray.pos + new Vector2(0f, -50f);
                        if (OnlineManager.lobby.isOwner)
                        {
                            potatoArenaMenu.TimerArray.greyedOut = false;
                        }
                        else
                        {
                            potatoArenaMenu.TimerArray.greyedOut = true;
                        }
                    }
                    //处理炸弹减少时间的按钮控件
                    if (potatoArenaMenu.ReduceTimeArray == null)
                    {
                        potatoArenaMenu.ReduceTimeArray = new BombScore(
                            self.arenaSettingsInterface.menu, self.arenaSettingsInterface,
                            self.arenaSettingsInterface.menu.Translate("Bomb Reduce Time:"),
                            "BOMB_REDUCE_TIME"
                        );
                        self.arenaSettingsInterface.subObjects.Add(potatoArenaMenu.ReduceTimeArray);
                        potatoArenaMenu.ReduceTimeArray.pos = self.arenaSettingsInterface.wildlifeArray.pos + new Vector2(0f, -100f);
                    }
                    else
                    {
                        potatoArenaMenu.ReduceTimeArray.pos = self.arenaSettingsInterface.wildlifeArray.pos + new Vector2(0f, -100f);
                        if (OnlineManager.lobby.isOwner)
                        {
                            potatoArenaMenu.ReduceTimeArray.scoreDragger.buttonBehav.greyedOut = false;
                        }
                        else
                        {
                            potatoArenaMenu.ReduceTimeArray.scoreDragger.buttonBehav.greyedOut = true;
                        }
                    }
                }
            }
            else
            {
                if (PotatoArenaMenu.menuPotatoCWT.TryGetValue(self, out var potatoArenaMenu))
                {
                    if (self.scene.depthIllustrations != null && self.scene.depthIllustrations.Count > 0)
                    {
                        potatoArenaMenu.scene.flatIllustrations[0].sprite.MoveBehindOtherNode(self.scene.depthIllustrations[0].sprite);
                    }
                    else if (self.scene.flatIllustrations != null && self.scene.flatIllustrations.Count > 0)
                    {
                        potatoArenaMenu.scene.flatIllustrations[0].sprite.MoveBehindOtherNode(self.scene.flatIllustrations[0].sprite);
                    }
                    //更改可见度隐藏版本号
                    potatoArenaMenu.versionLabel.label.isVisible = false;

                    //处理炸弹计时器的按钮控件
                    if (potatoArenaMenu.TimerArray != null && !potatoArenaMenu.TimerArray.greyedOut)
                    {
                        potatoArenaMenu.TimerArray.pos = new Vector2(0f, -300f);
                        potatoArenaMenu.TimerArray.greyedOut = true;
                    }
                    //处理炸弹减少时间的按钮控件
                    if (potatoArenaMenu.ReduceTimeArray != null && !potatoArenaMenu.ReduceTimeArray.scoreDragger.buttonBehav.greyedOut)
                    {
                        potatoArenaMenu.ReduceTimeArray.pos = new Vector2(0f, -300f);
                        potatoArenaMenu.ReduceTimeArray.scoreDragger.buttonBehav.greyedOut = true;
                    }

                    if (OnlineManager.lobby.isOwner)
                    {
                        self.arenaSettingsInterface.spearsHitCheckbox.selectable = true;//允许点击互相攻击按钮
                        self.arenaSettingsInterface.spearsHitCheckbox.buttonBehav.greyedOut = false;//取消灰掉互相攻击按钮
                    }

                }
            }
        }



    }
    public static class GameTypeSetup
    {
        public static int BombTimerIndex => MiniGameHotPotato.MiniGameHotPotato.options.BombTimer.Value;
        public static int BombReduceTime => MiniGameHotPotato.MiniGameHotPotato.options.BombReduceTime.Value;
        // public static int[] BombTimesInSecondsArray = new int[6] { 10, 30, 45, 60, 80, 99 };
        public static int[] BombTimesInSecondsArray = new int[6] { 5, 15, 25, 45, 85, 99 };
    }

}