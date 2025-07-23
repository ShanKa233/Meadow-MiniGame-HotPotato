using Meadow_MiniGame_HotPotato.UI;
using Menu;
using RainMeadow;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using UnityEngine;

namespace Meadow_MiniGame_HotPotato
{
    public partial class HotPotatoArena : ExternalArenaGameMode
    {
        public override Dialog AddGameModeInfo(ArenaOnlineGameMode arena, Menu.Menu menu)
        {
            return new DialogNotify(menu.LongTranslate("Requires at least 2 players. 3-5 seconds after start,<LINE>a bomb randomly spawns on one player,<LINE>pass by touching others.<LINE>Explodes when timer hits zero, <LINE>then respawns until only one survivor remains!"), new Vector2(500f, 400f), menu.manager, delegate
            {
                menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            });
        }


        public TabContainer.Tab? myTab;
        private OnlineHotPotatoSettingsInterface? myInterface;
        // public OnlineTeamBattleSettingsInterface? myHotPotatoSettingInterface;
        public override void OnUIEnabled(ArenaOnlineLobbyMenu menu)
        {

            base.OnUIEnabled(menu);
            // 在主大厅页面的Tab容器中添加"热土豆设置"标签页

            if (myTab == null)
            {
                myTab = menu.arenaMainLobbyPage.tabContainer.AddTab(menu.Translate("Hot Potato"));
                myTab.AddObjects(myInterface = new OnlineHotPotatoSettingsInterface(myTab.menu, myTab, new(0, 0), menu.arenaMainLobbyPage.tabContainer.size));


                var potatoArenaMenu = myInterface.potatoArenaMenu;


                //添加背景
                var potatoScene = new InteractiveMenuScene(menu, menu.pages[0], HotPotatoScenes.potatoBackground);
                potatoArenaMenu.scene = potatoScene;
                menu.pages[0].subObjects.Add(potatoScene);
                //添加版本显示
                MenuLabel displayCurrentGameMode = new MenuLabel(menu, menu.pages[0], "potato v" + MiniGameHotPotato.MiniGameHotPotato.version, new Vector2(10, 20f), new Vector2(10f, 10f), true);
                displayCurrentGameMode.label.alignment = FLabelAlignment.Left;
                potatoArenaMenu.versionLabel = displayCurrentGameMode;
                menu.pages[0].subObjects.Add(displayCurrentGameMode);

                potatoArenaMenu.scene.flatIllustrations[0].sprite.isVisible = true;
                //处理背景
                if (menu.scene.depthIllustrations != null && menu.scene.depthIllustrations.Count > 0)
                {
                    potatoArenaMenu.scene.flatIllustrations[0].sprite.MoveInFrontOfOtherNode(menu.scene.depthIllustrations[menu.scene.depthIllustrations.Count - 1].sprite);
                }
                else if (menu.scene.flatIllustrations != null && menu.scene.flatIllustrations.Count > 0)
                {
                    potatoArenaMenu.scene.flatIllustrations[0].sprite.MoveInFrontOfOtherNode(menu.scene.flatIllustrations[0].sprite);
                }
                else
                {
                    potatoArenaMenu.scene.flatIllustrations[0].sprite.isVisible = false;
                }


                //更改可见度显示版本号
                potatoArenaMenu.versionLabel.label.isVisible = true;
                //处理部分强制关闭的按钮控件的内容
                menu.arenaMainLobbyPage.arenaSettingsInterface.spearsHitCheckbox.selectable = false;//禁止点击互相攻击按钮
                menu.arenaMainLobbyPage.arenaSettingsInterface.spearsHitCheckbox.buttonBehav.greyedOut = true;//灰掉互相攻击按钮
                if (menu.arenaMainLobbyPage.arenaSettingsInterface.spearsHitCheckbox.Checked)
                {
                    menu.arenaMainLobbyPage.arenaSettingsInterface.spearsHitCheckbox.Checked = false;//如果互相攻击按钮被勾选，则取消勾选
                }
            }
        }

        public override void OnUIDisabled(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIDisabled(menu);

            var potatoArenaMenu = myInterface.potatoArenaMenu;
            if (menu.scene.depthIllustrations != null && menu.scene.depthIllustrations.Count > 0)
            {
                potatoArenaMenu.scene.flatIllustrations[0].sprite.MoveBehindOtherNode(menu.scene.depthIllustrations[0].sprite);
            }
            else if (menu.scene.flatIllustrations != null && menu.scene.flatIllustrations.Count > 0)
            {
                potatoArenaMenu.scene.flatIllustrations[0].sprite.MoveBehindOtherNode(menu.scene.flatIllustrations[0].sprite);
            }
            potatoArenaMenu.scene.flatIllustrations[0].sprite.isVisible = false;
            //更改可见度隐藏版本号
            potatoArenaMenu.versionLabel.label.isVisible = false;

            if (OnlineManager.lobby.isOwner)
            {
                menu.arenaMainLobbyPage.arenaSettingsInterface.spearsHitCheckbox.selectable = true;//允许点击互相攻击按钮
                menu.arenaMainLobbyPage.arenaSettingsInterface.spearsHitCheckbox.buttonBehav.greyedOut = false;//取消灰掉互相攻击按钮
            }
            // 关闭热土豆设置界面，释放资源
            myInterface?.OnShutdown();
            // 移除"热土豆设置"标签页
            if (myTab != null) menu.arenaMainLobbyPage.tabContainer.RemoveTab(myTab);
            myTab = null;
        }

        public override void OnUIShutDown(ArenaOnlineLobbyMenu menu)
        {
            base.OnUIShutDown(menu);
            myInterface?.OnShutdown();
        }
    }
}