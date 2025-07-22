using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JollyCoop.JollyMenu;
using Menu;
using Menu.Remix.MixedUI;
using MonoMod;
using MonoMod.RuntimeDetour;
using RainMeadow;
using RainMeadow.UI.Pages;
using RWCustom;
using UnityEngine;
// using static Menu.SandboxSettingsInterface;

namespace Meadow_MiniGame_HotPotato.UI
{

    public class OnlineHotPotatoSettingsInterface : RectangularMenuObject, Menu.MultipleChoiceArray.IOwnMultipleChoiceArray
    {
        public PotatoArenaMenu potatoArenaMenu;
        public OnlineHotPotatoSettingsInterface(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            potatoArenaMenu = new PotatoArenaMenu();
            float xOffset = 60f; // 统一X轴偏移量变量

            // 处理炸弹计时器的按钮控件 - 居中布局
            potatoArenaMenu.TimerArray = new MultipleChoiceArray(menu, this, this,
                new Vector2(size.x * 0.5f - xOffset, size.y - 100),
                menu.Translate("Explosion Timer:"),
                "MAX_BOMB_TIMER",
                95f, 200, 6,
                textInBoxes: false,
                splitText: false);
            this.SafeAddSubobjects(potatoArenaMenu.TimerArray);

            // 处理炸弹减少时间的按钮控件 - 与计时器垂直对齐
            potatoArenaMenu.ReduceTimeArray = new BombScore(
                menu, this,
                menu.Translate("Bomb Reduce Time:"),
                "BOMB_REDUCE_TIME"
            );
            potatoArenaMenu.ReduceTimeArray.pos = new Vector2(size.x * 0.5f - xOffset, size.y - 150);
            this.SafeAddSubobjects(potatoArenaMenu.ReduceTimeArray);
        }

        // 实现 IOwnMultipleChoiceArray 接口所需的方法
        public int GetSelected(Menu.MultipleChoiceArray array)
        {
            if (array.IDString == "MAX_BOMB_TIMER")
            {
                return HotPotatoArena.bombData.bombTimerIndex;
            }
            return 0;
        }

        public void SetSelected(Menu.MultipleChoiceArray array, int i)
        {
            if (array.IDString == "MAX_BOMB_TIMER")
            {
                if (i != MiniGameHotPotato.MiniGameHotPotato.options.BombTimer.Value)
                {
                    HotPotatoArena.bombData.bombTimerIndex = i;
                    MiniGameHotPotato.MiniGameHotPotato.options.BombTimer.Value = i;
                    MiniGameHotPotato.MiniGameHotPotato.options._SaveConfigFile();
                }
            }
        }
        public string UpdateInfoText(Menu.Menu menu)
        {
            if (menu.selectedObject is MultipleChoiceArray.MultipleChoiceButton)
            {
                switch ((menu.selectedObject.owner as MultipleChoiceArray).IDString)
                {
                    case "MAX_BOMB_TIMER":
                        return GameTypeSetup.BombTimesInSecondsArray[(menu.selectedObject as MultipleChoiceArray.MultipleChoiceButton).index] + " " + menu.Translate("seconds to explode");
                }
            }

            if (menu.selectedObject is SymbolButton)
            {
                if ((menu.selectedObject as SymbolButton).signalText == "FILTER")
                {
                    return menu.Translate("Show only Hot Potato levels");
                }
                else if ((menu.selectedObject as SymbolButton).signalText == "CLEARFILTER")
                {
                    return menu.Translate("Showing all levels");
                }

            }
            return menu.Translate("Requires at least 2 players. 3-5 seconds after start,<LINE>a bomb randomly spawns on one player,<LINE>pass by touching others.<LINE>Explodes when timer hits zero, <LINE>then respawns until only one survivor remains!");
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
        }

        public void OnShutdown()
        {

            // Lobby lobby = OnlineManager.lobby;
        }
    }


    public class PotatoArenaMenu
    {
        public InteractiveMenuScene scene;
        // public static ConditionalWeakTable<MultiplayerMenu, PotatoArenaMenu> menuPotatoCWT = new ConditionalWeakTable<MultiplayerMenu, PotatoArenaMenu>();
        public MenuLabel versionLabel;
        public MultipleChoiceArray TimerArray;
        public BombScore ReduceTimeArray;
        public static void InitHook()
        {
            On.Menu.Menu.UpdateInfoText += Menu_UpdateInfoText;
        }

        private static string Menu_UpdateInfoText(On.Menu.Menu.orig_UpdateInfoText orig, Menu.Menu self)
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
    }
    public static class GameTypeSetup
    {
        public static int BombTimerIndex => MiniGameHotPotato.MiniGameHotPotato.options.BombTimer.Value;
        public static int BombReduceTime => MiniGameHotPotato.MiniGameHotPotato.options.BombReduceTime.Value;
        // public static int[] BombTimesInSecondsArray = new int[6] { 10, 30, 45, 60, 80, 99 };
        public static int[] BombTimesInSecondsArray = new int[6] { 5, 15, 25, 45, 85, 99 };
    }

}