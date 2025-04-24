using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Menu;
using NUnit.Framework.Constraints;
using UnityEngine;
using static Menu.LevelSelector;
using HUD;
using RWCustom;

namespace Meadow_MiniGame_HotPotato
{
    public class PotatoPlaylist
    {
        public static ConditionalWeakTable<Menu.LevelSelector.AllLevelsSelectionList, PotatoPlaylist> playlistcwt = new ConditionalWeakTable<Menu.LevelSelector.AllLevelsSelectionList, PotatoPlaylist>();
        private LevelSelector.AllLevelsSelectionList self;
        public SymbolButton filterButton;
        private List<LevelItem> filteredLevelItems = new List<LevelItem>();
        private bool isFiltered;
        private string currentFilter = "";

        public PotatoPlaylist(LevelSelector.AllLevelsSelectionList self)
        {
            this.self = self;
            this.filteredLevelItems = new List<LevelItem>();
        }

        private void DebugLogStatus(string message)
        {
            try
            {
                UnityEngine.Debug.Log($"[PotatoPlaylist] {message}");
            }
            catch
            {
                // 静默处理异常
            }
        }

        public static void InitHook()
        {
            On.Menu.LevelSelector.AllLevelsSelectionList.ctor += AddNewButton;
            On.Menu.LevelSelector.AllLevelsSelectionList.Singal += FilterButtonClick;
            On.Menu.LevelSelector.AllLevelsSelectionList.ItemClicked += ItemClicked;
        }




        private static void ItemClicked(On.Menu.LevelSelector.AllLevelsSelectionList.orig_ItemClicked orig, AllLevelsSelectionList self, int index)
        {
            if (playlistcwt.TryGetValue(self, out PotatoPlaylist playlist))
            {

                if (playlist.isFiltered && index < playlist.filteredLevelItems.Count)
                {
                    string actualLevelName = playlist.filteredLevelItems[index].name;
                    (playlist.self.owner as LevelSelector).LevelToPlaylist(actualLevelName);
                    return;
                }
                // else if (!playlist.isFiltered && index < playlist.self.AllLevelsList.Count)
                // {
                //     (playlist.self.owner as LevelSelector).LevelToPlaylist(playlist.self.AllLevelsList[index]);
                // }
            }

            orig(self, index);
        }



        private static void FilterButtonClick(On.Menu.LevelSelector.AllLevelsSelectionList.orig_Singal orig, LevelSelector.AllLevelsSelectionList self, MenuObject sender, string message)
        {
            if (playlistcwt.TryGetValue(self, out PotatoPlaylist playlist))
            {
                if (message == "FILTER")
                {
                    // 如果已经过滤，则取消过滤
                    if (playlist.isFiltered)
                    {
                        playlist.ClearFilter();
                    }
                    // 否则应用过滤
                    else
                    {
                        // 简单实现：使用"PV"作为默认过滤词

                        //TODO:之后可能改成POTATO
                        string keyword = "POTATO";
                        playlist.ApplyFilter(keyword, null);
                    }
                    return;
                }
                else if (message == "CLEARFILTER")
                {
                    playlist.ClearFilter();
                    return;
                }
            }
            orig(self, sender, message);
        }



        public void ApplyFilter(string keyword, PotatoPlaylist playlist)
        {
            // 忽略 playlist 参数，始终使用 this
            // DebugLogStatus($"开始应用过滤: {keyword}, 项目数量: {self.levelItems.Count}");

            if (isFiltered)
            {
                ClearFilter();
            }

            if (string.IsNullOrEmpty(keyword))
            {
                return;
            }

            isFiltered = true;
            currentFilter = keyword;

            filteredLevelItems.Clear();

            // 先创建要保留的项目列表
            for (int i = 0; i < self.levelItems.Count; i++)
            {
                if (self.levelItems[i].name.ToLower().Contains(keyword.ToLower()))
                {
                    filteredLevelItems.Add(self.levelItems[i]);
                }
            }

            // 完全移除所有项目
            for (int i = self.levelItems.Count - 1; i >= 0; i--)
            {
                LevelItem item = self.levelItems[i];
                self.RemoveSubObject(item);
                // 确保精灵被移除
                item.RemoveSprites();
            }

            // 清空列表
            self.levelItems.Clear();

            // 重新添加匹配的项目
            foreach (LevelItem item in filteredLevelItems)
            {
                // 创建新的项目而不是重用旧项目
                LevelItem newItem = new LevelItem(self.menu, self, item.name);
                self.AddLevelItem(newItem);
            }

            // 重置滚动位置到顶部
            self.ScrollPos = 0;
            self.floatScrollPos = 0;
            self.floatScrollVel = 0;

            self.ConstrainScroll();
            filterButton.buttonBehav.greyedOut = false;

            // 确保按钮图标可见
            if (filterButton.symbolSprite != null)
            {
                filterButton.symbolSprite.alpha = 1f;
            }

            // 更新按钮图标，表示过滤已激活
            filterButton.UpdateSymbol("illustrations/Potato_Symbol_Clear_All");
            filterButton.signalText = "CLEARFILTER";

            self.menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

            // DebugLogStatus($"过滤完成, 匹配项目: {filteredLevelItems.Count}, 显示项目: {self.levelItems.Count}");
        }

        public void ClearFilter()
        {
            DebugLogStatus($"开始清除过滤，当前项目数量: {self.levelItems.Count}");

            if (!isFiltered)
            {
                return;
            }

            isFiltered = false;
            currentFilter = "";

            // 完全移除所有项目
            for (int i = self.levelItems.Count - 1; i >= 0; i--)
            {
                LevelItem item = self.levelItems[i];
                self.RemoveSubObject(item);
                // 确保精灵被移除
                item.RemoveSprites();
            }

            // 清空列表
            self.levelItems.Clear();

            // 重新添加所有原始项目
            for (int i = 0; i < self.AllLevelsList.Count; i++)
            {
                LevelItem newItem = new LevelItem(self.menu, self, self.AllLevelsList[i]);
                self.AddLevelItem(newItem);
            }

            // 添加分隔符
            for (int j = 0; j < self.levelItems.Count - 1; j++)
            {
                if ((self.owner as LevelSelector).GetMultiplayerMenu.multiplayerUnlocks.LevelListSortNumber(self.AllLevelsList[j]) != (self.owner as LevelSelector).GetMultiplayerMenu.multiplayerUnlocks.LevelListSortNumber(self.AllLevelsList[j + 1]))
                {
                    self.levelItems[j].AddDividers(self.levelItems[j + 1]);
                }
            }

            // 重置滚动位置到顶部
            self.ScrollPos = 0;
            self.floatScrollPos = 0;
            self.floatScrollVel = 0;

            self.ConstrainScroll();

            // 确保按钮状态重置
            filterButton.buttonBehav.greyedOut = false;

            // 确保按钮图标可见
            if (filterButton.symbolSprite != null)
            {
                filterButton.symbolSprite.alpha = 1f;
            }

            // 恢复按钮图标
            filterButton.UpdateSymbol("illustrations/Potato_Symbol_Show_Thumbs");
            filterButton.signalText = "FILTER";

            self.menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

            // DebugLogStatus($"清除过滤完成，项目数量: {self.levelItems.Count}");
        }



        private static void AddNewButton(On.Menu.LevelSelector.AllLevelsSelectionList.orig_ctor orig, LevelSelector.AllLevelsSelectionList self, Menu.Menu menu, LevelSelector owner, Vector2 pos, bool shortList)
        {
            orig(self, menu, owner, pos, shortList);
            var playlist = playlistcwt.GetValue(self, (_) => new PotatoPlaylist(self));

            // 创建过滤按钮
            playlist.filterButton = new SymbolButton(menu, self, "illustrations/Potato_Symbol_Show_Thumbs", "FILTER", self.sideButtons[0].pos + new Vector2(0f, 30f));
            self.subObjects.Add(playlist.filterButton);
        }

    }
}