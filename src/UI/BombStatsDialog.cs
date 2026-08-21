using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;

namespace Meadow_MiniGame_HotPotato.UI
{
    // 赛后统计对话框:三列(传递/爆炸/存活)
    // 结构复刻Meadow的ArenaPostGameStatsDialog(StoredResults + ButtonScroller滚动列)
    public class BombStatsDialog : Dialog
    {
        public StoredResults[] storedResults;
        public SimplerButton closeButton;
        public AlignedMenuLabel postGameStatsLabel;
        public ArenaOnlineGameMode arenaMode;
        public float spacing = 20;

        public BombStatsDialog(ProcessManager manager, ArenaOnlineGameMode arena) : base("", new Vector2(1000, 460), manager)
        {
            arenaMode = arena;

            // 标题(位置公式与Meadow一致)
            postGameStatsLabel = new(this, pages[0], Translate("HOTPOTATO STATS"),
                new Vector2(pos.x + size.x * 0.5f, pos.y + size.y + 10), new Vector2(0, 0), true);
            postGameStatsLabel.label.anchorY = 0;

            // 关闭按钮(位置公式与Meadow一致)
            closeButton = new(this, pages[0], Translate("CLOSE"),
                new Vector2(roundedRect.pos.x + roundedRect.size.x - 80, roundedRect.pos.y - 40), new Vector2(80, 30));
            closeButton.OnClick += _ =>
            {
                manager.StopSideProcess(this);
                PlaySound(SoundID.MENU_Remove_Level);
            };

            // 控件必须显式加入subObjects才会被更新和绘制
            pages[0].subObjects.AddRange(new MenuObject[] { postGameStatsLabel, closeButton });

            // 三列统计:传递/爆炸/存活
            storedResults = new StoredResults[3];
            SetUpStoredResults();
        }

        public void SetUpStoredResults()
        {
            float totalOccupiedXSize = size.x - (storedResults.Length + 1) * spacing;
            float totalOccupiedYSize = size.y - spacing * 2;
            float storedResultXSize = totalOccupiedXSize / storedResults.Length;
            for (int i = 0; i < storedResults.Length; i++)
            {
                float posX = spacing * (i + 1) + storedResultXSize * i;
                string name = i == 0 ? Translate("PASS") : i == 1 ? Translate("EXPLODE") : Translate("SURVIVE");
                storedResults[i] = new StoredResults(this, roundedRect, new Vector2(posX, spacing), new Vector2(storedResultXSize, totalOccupiedYSize), name);
            }
            roundedRect.SafeAddSubobjects(storedResults);
        }

        public override void Update()
        {
            base.Update();
            for (int i = 0; i < storedResults.Length; i++)
                UpdateStoredResults(storedResults[i], GetStrings(storedResults[i], i));
        }

        // 每列的内容:玩家名+数值,按数值降序
        public string[] GetStrings(StoredResults storedResults, int i)
        {
            var bombData = HotPotatoArena.bombData;
            if (bombData == null || bombData.playerStats == null || bombData.playerStats.Count == 0)
                return new string[0];

            var stats = bombData.playerStats.Values;
            if (i == 0)
                return stats.OrderByDescending(x => x.passCount)
                    .Select(x => FormatLine(storedResults, PlayerName(x), x.passCount)).ToArray();
            if (i == 1)
                return stats.OrderByDescending(x => x.explodedCount)
                    .Select(x => FormatLine(storedResults, PlayerName(x), x.explodedCount)).ToArray();
            return stats.OrderByDescending(x => x.survivedRounds)
                .Select(x => FormatLine(storedResults, PlayerName(x), x.survivedRounds)).ToArray();
        }

        private string PlayerName(PlayerBombStats stats)
        {
            var onlinePlayer = OnlineManager.lobby?.PlayerFromId(stats.inLobbyId);
            return onlinePlayer != null && !string.IsNullOrEmpty(onlinePlayer.id.name)
                ? onlinePlayer.id.name
                : "Player " + stats.inLobbyId;
        }

        private string FormatLine(StoredResults storedResults, string name, int value)
        {
            return LabelTest.TrimText(name, storedResults.size.x - LabelTest.GetWidth(" - " + value) - 10, true) + " - " + value;
        }

        // 同步滚动列中的文本行,与Meadow一致
        public void UpdateStoredResults(StoredResults storedResults, string[] strings)
        {
            List<AlignedMenuLabel> menulabels = storedResults.scroller.GetSpecificButtons<AlignedMenuLabel>();
            for (int i = 0; i < menulabels.Count; i++)
            {
                if (strings.Length <= i)
                {
                    storedResults.scroller.RemoveButton(menulabels[i], true);
                    continue;
                }
                menulabels[i].text = strings[i];
            }
            int count = storedResults.scroller.GetSpecificButtons<AlignedMenuLabel>().Count;
            if (strings.Length == count) return;
            IEnumerable<string> newStrings = strings.Skip(count);
            foreach (string s in newStrings)
            {
                AlignedMenuLabel label = new(this, storedResults.scroller, s,
                    storedResults.scroller.GetIdealPosWithScrollForButton(storedResults.scroller.buttons.Count),
                    new Vector2(storedResults.scroller.size.x, 30), false);
                label.label.color = MenuColorEffect.rgbMediumGrey;
                storedResults.scroller.AddScrollObjects(label);
            }
        }

        // 单个统计列:标题+圆角框+滚动列表(与Meadow的StoredResults一致)
        public class StoredResults : RectangularMenuObject
        {
            public MenuLabel label;
            public RoundedRect roundedRect;
            public ButtonScroller scroller;
            public StoredResults(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, string name) : base(menu, owner, pos, size)
            {
                roundedRect = new RoundedRect(menu, this, Vector2.zero, size, true);
                scroller = new ButtonScroller(menu, this, Vector2.zero, new Vector2(size.x, size.y - 45), false, new Vector2(30, 20), -20)
                {
                    greyOutWhenNoScroll = true,
                    buttonHeight = 30,
                    buttonSpacing = 5,
                };
                scroller.CreateSideButtonLines();
                label = new MenuLabel(menu, this, name, new Vector2(0, scroller.size.y), new Vector2(size.x, 40), true);
                this.SafeAddSubobjects(roundedRect, scroller, label);
            }
        }
    }
}
