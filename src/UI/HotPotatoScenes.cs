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

namespace Meadow_MiniGame_HotPotato.UI
{
    public class HotPotatoScenes
    {
        public static MenuScene.SceneID potatoBackground = new MenuScene.SceneID("potatoBackground", true);

        public static void InitHook()
        {
            // 增加一个cg
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
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