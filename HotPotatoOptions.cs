using Menu.Remix.MixedUI;
using Menu.Remix;
using UnityEngine;
using System;
using RainMeadow;

namespace Meadow_MiniGame_HotPotato
{
    // 烫手土豆游戏配置类
    public class HotPotatoOptions : OptionInterface
    {
        // 只保留最小玩家数量设置
        public readonly Configurable<int> MinPlayersRequired; // 最少需要的玩家数量

        private UIelement[] HotPotatoSettings;

        public HotPotatoOptions(MiniGameHotPotato.MiniGameHotPotato instance)
        {
            // 初始化游戏核心设置
            MinPlayersRequired = config.Bind("HotPotatoMinPlayers", 2);
        }

        public override void Initialize()
        {
            try
            {
                // 创建配置选项卡
                OpTab hotPotatoTab = new OpTab(this, "烫手山芋");
                Tabs = new OpTab[1] { hotPotatoTab };
                
                // 创建UI元素
                HotPotatoSettings = new UIelement[]
                {
                    // 标题
                    new OpLabel(10f, 550f, "烫手山芋", bigText: true),
                    
                    // 游戏核心设置区域
                    new OpLabel(10f, 510f, "游戏核心设置", bigText: false),
                    
                    // 最少玩家数量
                    new OpLabel(10f, 480f, "最少需要的玩家数量", bigText: false),
                    new OpTextBox(MinPlayersRequired, new Vector2(10f, 455f), 160f)
                    {
                        accept = OpTextBox.Accept.Int
                    }
                };
                
                // 将元素添加到选项卡
                hotPotatoTab.AddItems(HotPotatoSettings);
            }
            catch (Exception ex)
            {
                RainMeadow.RainMeadow.Error("错误：打开烫手土豆选项菜单时出错" + ex);
            }
        }

        public override void Update()
        {
            // 如果需要实时更新某些选项，可以在这里实现
        }
    }
} 