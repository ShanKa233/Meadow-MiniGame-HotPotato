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
        public readonly Configurable<int> MinPlayersRequired; // 最少需要的玩家数量,少于这个数量游戏结束
        public readonly Configurable<int> BombTimer; // 炸弹计时器
        public readonly Configurable<int> BombReduceTime; // 每次传递炸弹后减少的时间(秒)

        private UIelement[] HotPotatoSettings;

        public HotPotatoOptions(MiniGameHotPotato.MiniGameHotPotato instance)
        {
            // 初始化游戏核心设置
            MinPlayersRequired = config.Bind("HotPotatoMinPlayers", 2);
            BombTimer = config.Bind("HotPotatoBombTimer", 2);
            //每次炸弹传递的减少时间
            BombReduceTime = config.Bind("HotPotatoBombReduceTime", 5);
        }

        public override void Initialize()
        {
            try
            {
                // 创建配置选项卡
                OpTab hotPotatoTab = new OpTab(this, "HotPotato");
                Tabs = new OpTab[1] { hotPotatoTab };
                
                // 创建UI元素
                HotPotatoSettings = new UIelement[]
                {
                    // 标题
                    new OpLabel(10f, 550f, Translate("Hot Potato"), bigText: true),
                    
                    // 游戏核心设置区域
                    new OpLabel(10f, 510f, Translate("Hot Potato Core Settings"), bigText: false),
                    
                    // 最少玩家数量
                    new OpLabel(10f, 480f, Translate("Minimum Players Required to Start"), bigText: false),
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
                RainMeadow.RainMeadow.Error("Error: Failed to open Hot Potato options menu: " + ex);
            }
        }

        public override void Update()
        {
            // 如果需要实时更新某些选项，可以在这里实现
        }
    }
} 