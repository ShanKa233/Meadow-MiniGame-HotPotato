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


        public static void InitHook()
        {
            // 增加一个cg
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;

            // 在菜单背景添加后离开添加背景
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
                        var potatoArenaMenu = PotatoArenaMenu.menuPotatoCWT.GetValue(menu, (menu) => new PotatoArenaMenu());


                        //添加背景
                        var potatoScene = new InteractiveMenuScene(menu, menu.pages[0], HotPotatoScenes.potatoBackground);
                        potatoArenaMenu.scene = potatoScene;
                        menu.pages[0].subObjects.Add(potatoScene);
                        //添加版本显示
                        MenuLabel displayCurrentGameMode = new MenuLabel(menu, menu.pages[0], "potato v" + MiniGameHotPotato.MiniGameHotPotato.version, new Vector2(10, 20f), new Vector2(10f, 10f), true);
                        displayCurrentGameMode.label.alignment = FLabelAlignment.Left;
                        potatoArenaMenu.versionLabel = displayCurrentGameMode;
                        menu.pages[0].subObjects.Add(displayCurrentGameMode);

                    }

                });
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
        }
    }
}