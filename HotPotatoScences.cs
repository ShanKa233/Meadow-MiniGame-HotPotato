using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RainMeadow;
using UnityEngine;

namespace Meadow_MiniGame_HotPotato
{
    public class HotPotatoScenes
    {
        public static MenuScene.SceneID potatoBackground = new MenuScene.SceneID("potatoBackground", true);
        private static Hook AreanaLobbyMenu_UpdateGameModeLabel_Hook;
        public static ConditionalWeakTable<Menu.MultiplayerMenu, MenuScene> menuPotatoCWT = new ConditionalWeakTable<Menu.MultiplayerMenu, MenuScene>();
        public static ConditionalWeakTable<Menu.MultiplayerMenu, MenuLabel> menuPotatoCWT_version = new ConditionalWeakTable<Menu.MultiplayerMenu, MenuLabel>();
        public static void InitHook()
        {

            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;

            Type arenaLobbyMenuType = typeof(RainMeadow.ArenaLobbyMenu);
            AreanaLobbyMenu_UpdateGameModeLabel_Hook = new Hook(
                               typeof(RainMeadow.ArenaLobbyMenu).GetMethod("UpdateGameModeLabel",
                                   BindingFlags.Instance | BindingFlags.NonPublic),
                               typeof(HotPotatoScenes).GetMethod("ArenaLobbyMenu_UpdateGameModeLabel",
                                   BindingFlags.Static | BindingFlags.NonPublic)
                           );

            IL.Menu.MultiplayerMenu.ctor += MultiplayerMenu_ctor;

        }

        private static void MultiplayerMenu_ctor(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(
                MoveType.After,
                i => i.Match(OpCodes.Ldfld),
                i => i.MatchLdcI4(0),
                i => i.Match(OpCodes.Callvirt),
                i => i.Match(OpCodes.Ldfld),
                i => i.MatchLdarg(0),
                i => i.Match(OpCodes.Ldfld),
                i => i.Match(OpCodes.Callvirt)
            ))
            {
                cursor.Emit(OpCodes.Ldarg, 0);
                cursor.EmitDelegate<Action<Menu.MultiplayerMenu>>(menu =>
                {

                    if (menu is ArenaLobbyMenu)
                    {
                        //添加背景
                        var potatoScene = new InteractiveMenuScene(menu, menu.pages[0], HotPotatoScenes.potatoBackground);
                        // potatoScene.myContainer.alpha = 0;
                        menuPotatoCWT.Add(menu, potatoScene);
                        menu.pages[0].subObjects.Add(potatoScene);
                        //添加版本显示
                        MenuLabel displayCurrentGameMode = new MenuLabel(menu, menu.pages[0], "potato v" + MiniGameHotPotato.MiniGameHotPotato.version, new Vector2(10, 20f), new Vector2(10f, 10f), true);
                        displayCurrentGameMode.label.alignment = FLabelAlignment.Left;
                        menuPotatoCWT_version.Add(menu, displayCurrentGameMode);
                        menu.pages[0].subObjects.Add(displayCurrentGameMode);

                    }

                });
            }
        }

        // 钩子方法

        private static void ArenaLobbyMenu_UpdateGameModeLabel(Action<RainMeadow.ArenaLobbyMenu> orig, RainMeadow.ArenaLobbyMenu self)
        {
            // 调用原始方法
            orig(self);
            if (RainMeadow.RainMeadow.isArenaMode(out var arena) && ((ArenaOnlineGameMode)OnlineManager.lobby.gameMode).currentGameMode == HotPotatoArena.arenaName)
            {
                if (menuPotatoCWT.TryGetValue(self, out var potatoScene))
                {

                    if (self.scene.depthIllustrations != null && self.scene.depthIllustrations.Count > 0)
                    {
                        potatoScene.flatIllustrations[0].sprite.MoveInFrontOfOtherNode(self.scene.depthIllustrations[self.scene.depthIllustrations.Count - 1].sprite);
                    }
                    else if (self.scene.flatIllustrations != null && self.scene.flatIllustrations.Count > 0)
                    {
                        potatoScene.flatIllustrations[0].sprite.MoveInFrontOfOtherNode(self.scene.flatIllustrations[0].sprite);
                    }
                    //更改可见度显示版本号
                    if (menuPotatoCWT_version.TryGetValue(self, out var menuLabel))
                    {
                        menuLabel.label.isVisible = true;
                    }
                    self.arenaSettingsInterface.spearsHitCheckbox.selectable = false;//禁止点击互相攻击按钮
                    self.arenaSettingsInterface.spearsHitCheckbox.buttonBehav.greyedOut = true;//灰掉互相攻击按钮
                    if (self.arenaSettingsInterface.spearsHitCheckbox.Checked)
                    {
                        self.arenaSettingsInterface.spearsHitCheckbox.Checked = false;//如果互相攻击按钮被勾选，则取消勾选
                    }
                }
            }
            else
            {
                if (menuPotatoCWT.TryGetValue(self, out var potatoScene))
                {
                    if (self.scene.depthIllustrations != null && self.scene.depthIllustrations.Count > 0)
                    {
                        potatoScene.flatIllustrations[0].sprite.MoveBehindOtherNode(self.scene.depthIllustrations[0].sprite);
                    }
                    else if (self.scene.flatIllustrations != null && self.scene.flatIllustrations.Count > 0)
                    {
                        potatoScene.flatIllustrations[0].sprite.MoveBehindOtherNode(self.scene.flatIllustrations[0].sprite);
                    }
                    //更改可见度隐藏版本号
                    if (menuPotatoCWT_version.TryGetValue(self, out var menuLabel))
                    {
                        menuLabel.label.isVisible = false;
                    }
                    if (OnlineManager.lobby.isOwner)
                    {
                        self.arenaSettingsInterface.spearsHitCheckbox.selectable = true;//允许点击互相攻击按钮
                        self.arenaSettingsInterface.spearsHitCheckbox.buttonBehav.greyedOut = false;//取消灰掉互相攻击按钮
                    }
                }
            }
        }

        private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig(self);
            if (self.sceneID == potatoBackground)
            {
                BuildPotatoBackground(self);
            }
        }
        public static void BuildPotatoBackground(MenuScene self)
        {
            string sceneFolder = "Scenes" + Path.DirectorySeparatorChar + "Potato Scene";
            self.AddIllustration(new MenuIllustration(self.menu, self, sceneFolder, "Potato BackGround - Flat", new Vector2(683, 384), false, true));
            // if (!self.flatMode)
            // {
            //     AddIllustration(new MenuDepthIllustration(menu, this, sceneFolder, "Gourmand Background - 4", new Vector2(0f, 0f), 3.6f, MenuDepthIllustration.MenuShader.Normal));
            //     AddIllustration(new MenuDepthIllustration(menu, this, sceneFolder, "Gourmand Pipes - 3", new Vector2(0f, 0f), 3.2f, MenuDepthIllustration.MenuShader.Normal));
            //     AddIllustration(new MenuDepthIllustration(menu, this, sceneFolder, "Gourmand Bg - 2", new Vector2(0f, 0f), 3.1f, MenuDepthIllustration.MenuShader.Normal));
            //     if (owner is SlugcatSelectMenu.SlugcatPage)
            //     {
            //         (owner as SlugcatSelectMenu.SlugcatPage).AddGlow();
            //     }
            //     if (!UseSlugcatUnlocked(MoreSlugcatsEnums.SlugcatStatsName.Gourmand))
            //     {
            //         AddIllustration(new MenuDepthIllustration(menu, this, sceneFolder, "Gourmand Slugcat - 1 - Dark", new Vector2(0f, 0f), 2.7f, MenuDepthIllustration.MenuShader.Basic));
            //     }
            //     else
            //     {
            //         AddIllustration(new MenuDepthIllustration(menu, this, sceneFolder, "Gourmand Slugcat - 1", new Vector2(0f, 0f), 2.7f, MenuDepthIllustration.MenuShader.Basic));
            //     }
            //     AddIllustration(new MenuDepthIllustration(menu, this, sceneFolder, "Gourmand Fg - 0", new Vector2(0f, 0f), 2.1f, MenuDepthIllustration.MenuShader.Normal));
            //     (this as InteractiveMenuScene).idleDepths.Add(3.6f);
            //     (this as InteractiveMenuScene).idleDepths.Add(2.8f);
            //     (this as InteractiveMenuScene).idleDepths.Add(2.7f);
            //     (this as InteractiveMenuScene).idleDepths.Add(2.6f);
            //     (this as InteractiveMenuScene).idleDepths.Add(1.5f);
            // }
            // else if (!UseSlugcatUnlocked(MoreSlugcatsEnums.SlugcatStatsName.Gourmand))
            // {
            //     AddIllustration(new MenuIllustration(menu, this, sceneFolder, "Slugcat - Gourmand Dark - Flat", new Vector2(683f, 384f), crispPixels: false, anchorCenter: true));
            // }
            // else
            // {
            //     AddIllustration(new MenuIllustration(menu, this, sceneFolder, "Slugcat - Gourmand - Flat", new Vector2(683f, 384f), crispPixels: false, anchorCenter: true));
            // }
        }

    }
}