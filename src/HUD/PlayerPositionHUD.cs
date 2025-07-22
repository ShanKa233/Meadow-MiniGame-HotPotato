using System.Collections.Generic;
using HUD;
using RainMeadow;
using UnityEngine;
using RWCustom;
using System;
using NUnit.Framework;

namespace Meadow_MiniGame_HotPotato.HUDStuff
{
    public class PlayerPositionHUD : HudPart
    {
        private readonly RoomCamera cam;
        private readonly List<PlayerIndicator> indicators = new List<PlayerIndicator>();
        
        // 配置参数
        private readonly Color bombHolderColor = new Color(1f, 0.5f, 0.2f);
        private bool canShowIndicator = true; // 始终允许显示
        private int standStillCounter;

        // 玩家指示器类，模仿JollyOffRoom
        private class PlayerIndicator
        {
            // 图标和背景
            private List<FSprite> sprites;
            
            // 位置参数
            private Vector2 playerPos;
            private Vector2 lastPlayerPos;
            private Vector2 drawPos;
            private Vector2 lastDrawPos;
            private Vector2 roomPos;
            
            // 显示参数
            private float alpha = 0f;
            private float lastAlpha = 0f;
            private bool hidden = false; // 默认显示
            private float scale = 1f;
            
            // 屏幕边缘参数
            private const int screenEdge = 25; // 与JollyOffRoom一致
            private float screenSizeX;
            private float screenSizeY;
            private Vector2 middleScreen;
            private Vector2 rectangleSize;
            private float diagScale;
            
            // 引用
            private HUD.HUD hud;
            private RoomCamera camera;
            
            // 玩家信息
            private bool isHudOwner;
            private int playerIndex;
            
            public PlayerIndicator(HUD.HUD hud, RoomCamera camera)
            {
                this.hud = hud;
                this.camera = camera;
                
                // 初始化精灵列表
                sprites = new List<FSprite>();
                
                // 模仿JollyOffRoom的精灵初始化
                sprites.Add(new FSprite("Kill_Slugcat") {
                    scale = scale
                });
                sprites.Add(new FSprite("Futile_White") {
                    shader = hud.rainWorld.Shaders["FlatLight"],
                    alpha = 0f,
                    x = -1000f
                });
                
                // 默认颜色和初始状态
                for (int i = 0; i < sprites.Count; i++)
                {
                    sprites[i].alpha = 0.8f;
                    hud.fContainers[1].AddChild(sprites[i]);
                    sprites[i].isVisible = true;
                }
                
                // 初始化位置数据
                playerPos = new Vector2(-1000f, -1000f);
                lastPlayerPos = playerPos;
                drawPos = playerPos;
                lastDrawPos = drawPos;
                
                // 获取屏幕尺寸信息 - 与JollyOffRoom一致
                screenSizeX = hud.rainWorld.options.ScreenSize.x;
                screenSizeY = hud.rainWorld.options.ScreenSize.y;
                middleScreen = new Vector2(screenSizeX / 2f, screenSizeY / 2f);
                rectangleSize = new Vector2(screenSizeX - (float)(2 * screenEdge), screenSizeY - (float)(2 * screenEdge));
                diagScale = Mathf.Abs(Vector2.Distance(Vector2.zero, middleScreen));
            }
            
            public void Update(Player player, bool isHudOwner, bool showIndicator, int index)
            {
                // 更新状态
                this.isHudOwner = isHudOwner;
                this.playerIndex = index;
                
                // 更新历史位置
                lastPlayerPos = playerPos;
                lastDrawPos = drawPos;
                lastAlpha = alpha;
                
                // 如果玩家无效或是HUD所有者，则隐藏
                if (player == null)
                {
                    hidden = true;
                    return;
                }
                
                if (isHudOwner)
                {
                    hidden = true;
                    return;
                }
                
                // 检查玩家是否在同一房间
                bool inSameRoom = (player.room == camera.room);
                if (!inSameRoom || player.inShortcut)
                {
                    hidden = true;
                    return;
                }
                
                // 获取玩家位置
                playerPos = player.mainBodyChunk.pos;
                
                // 获取摄像机位置
                roomPos = camera.pos;
                
                // 更新绘制位置 - 使用相对于摄像机的位置
                drawPos = playerPos - roomPos;
                
                // 检查玩家是否在屏幕上
                float left = middleScreen.x - rectangleSize.x / 2f;
                float right = middleScreen.x + rectangleSize.x / 2f;
                float bottom = middleScreen.y - rectangleSize.y / 2f;
                float top = middleScreen.y + rectangleSize.y / 2f;
                
                bool onScreen = (left < drawPos.x && drawPos.x < right && 
                                bottom < drawPos.y && drawPos.y < top);
                
                // 如果玩家在屏幕上且显示条件允许，则显示
                if (onScreen || !showIndicator)
                {
                    hidden = true;
                    return;
                }
                else
                {
                    hidden = false;
                }

                // 按照JollyOffRoom的缩放方式计算
                float distToCenter = Mathf.Abs(Vector2.Distance(drawPos, middleScreen));
                scale = Mathf.Lerp(0.65f, 1.65f, Mathf.Pow(diagScale / distToCenter, 1.2f));
                
                // 完全按照JollyOffRoom的边界检测和处理
                if (left < drawPos.x && drawPos.x < right && bottom < drawPos.y && drawPos.y < top)
                {
                    float distToLeft = Mathf.Abs(drawPos.x - left);
                    float distToRight = Mathf.Abs(drawPos.x - right);
                    float distToBottom = Mathf.Abs(drawPos.y - bottom);
                    float distToTop = Mathf.Abs(drawPos.y - top);
                    
                    // 获取最小距离
                    float smallestDistance = GetSmallestNumber(distToBottom, distToLeft, distToRight, distToTop);
                    
                    // 确定应该固定到哪个边缘
                    if (AreClose(smallestDistance, distToLeft))
                    {
                        drawPos.x = left;
                    }
                    else if (AreClose(smallestDistance, distToRight))
                    {
                        drawPos.x = right;
                    }
                    else if (AreClose(smallestDistance, distToBottom))
                    {
                        drawPos.y = bottom;
                    }
                    else
                    {
                        drawPos.y = top;
                    }
                }
                
                // 确保在屏幕范围内
                drawPos.x = Mathf.Clamp(drawPos.x, screenEdge, screenSizeX - screenEdge);
                drawPos.y = Mathf.Clamp(drawPos.y, screenEdge, screenSizeY - screenEdge);
                
                // 更新透明度目标值 - 与JollyOffRoom一致
                alpha = hidden ? 0f : 0.85f;
            }
            
            public void Draw(float timeStacker)
            {
                // 如果隐藏则不绘制
                if (hidden)
                {
                    for (int i = 0; i < sprites.Count; i++)
                    {
                        sprites[i].isVisible = false;
                    }
                    return;
                }
                
                // 使用JollyOffRoom的随机闪烁
                if (UnityEngine.Random.value > Mathf.InverseLerp(0.5f, 0.75f, alpha))
                {
                    for (int i = 0; i < sprites.Count; i++)
                    {
                        sprites[i].isVisible = false;
                    }
                    return;
                }
                
                // 计算当前位置和透明度 - 与JollyOffRoom一致
                float currentAlpha = Mathf.SmoothStep(lastAlpha, alpha, timeStacker);
                Vector2 currentPos = Vector2.Lerp(lastDrawPos, drawPos, timeStacker);
                
                // 更新所有精灵
                for (int i = 0; i < sprites.Count; i++)
                {
                    sprites[i].isVisible = true;
                    sprites[i].x = currentPos.x;
                    sprites[i].y = currentPos.y;
                    sprites[i].scale = scale;
                }
                
                // 主图标设置
                sprites[0].alpha = Mathf.Lerp(sprites[0].alpha, currentAlpha, timeStacker * 0.5f);
                
                // 背景光晕设置 - 与JollyOffRoom一致
                sprites[1].scale = Mathf.Lerp(80f, 110f, 1f) / 16f;
                sprites[1].alpha = Mathf.Lerp(sprites[1].alpha, 0.15f * Mathf.Pow(currentAlpha, 2f), timeStacker);
            }
            
            // 查找最小值 - 与JollyOffRoom的GetSmallestNumber一致
            private float GetSmallestNumber(float a, float b, float c, float d)
            {
                return Mathf.Min(a, Mathf.Min(b, Mathf.Min(c, d)));
            }
            
            // 浮点数近似相等 - 与JollyOffRoom的AreClose一致
            private bool AreClose(float a, float b)
            {
                return (double)Mathf.Abs(a - b) <= 0.01;
            }
            
            // 更新颜色
            public void UpdateColor(Color color, Color bombHolderColor)
            {
                // 设置颜色
                Color spriteColor = color;
                
                for (int i = 0; i < sprites.Count; i++)
                {
                    sprites[i].color = spriteColor;
                }
            }
            
            // 清理资源
            public void ClearSprites()
            {
                for (int i = 0; i < sprites.Count; i++)
                {
                    sprites[i].isVisible = false;
                    sprites[i].RemoveFromContainer();
                }
                sprites.Clear();
            }
        }
        
        public PlayerPositionHUD(HUD.HUD hud, RoomCamera roomCamera) : base(hud)
        {
            cam = roomCamera;
        }
        
        public override void Update()
        {
            base.Update();
            
            // 强制启用指示器显示
            // canShowIndicator = true;
            UpdateIndicatorState();
            
            // 获取当前游戏会话
            ArenaGameSession gameSession = GetGameSession();
            if (gameSession == null) return;
            
            // 确保指示器数量与玩家数量匹配
            EnsureIndicators(gameSession.Players.Count);
            
            // 更新所有玩家位置指示器
            UpdateIndicators(gameSession);
        }
        
        private void UpdateIndicatorState()
        {
            // 获取HUD所有者（玩家）
            var player = hud?.owner as Player;
            if (player == null) return;
            
            
            // 如果需要使用原始条件，可以取消下面的注释
            if (!player.input[0].AnyInput)
            {
                standStillCounter++;
            }
            else
            {
                standStillCounter = 0;
            }
            
            canShowIndicator = standStillCounter >= 80 || player.input[0].mp;
            // 检查是否持有炸弹
            bool ownerHasBomb = (player == HotPotatoArena.bombData?.bombHolderCache);
            
            // 判断是否显示指示器
        }
        
        private void EnsureIndicators(int playerCount)
        {
            // 如果需要更多指示器，创建新的
            while (indicators.Count < playerCount)
            {
                PlayerIndicator indicator = new PlayerIndicator(hud, cam);
                indicators.Add(indicator);
            }
            
            // 如果指示器过多，删除多余的
            while (indicators.Count > playerCount)
            {
                int lastIndex = indicators.Count - 1;
                indicators[lastIndex].ClearSprites();
                indicators.RemoveAt(lastIndex);
            }
        }
        
        private void UpdateIndicators(ArenaGameSession gameSession)
        {
            if (gameSession == null) return;
            
            int index = 0;
            Player hudOwner = hud.owner as Player;
            
            foreach (var abstractCreature in gameSession.Players)
            {
                if (index >= indicators.Count) break;
                
                // 获取玩家实例
                Player player = abstractCreature.realizedCreature as Player;
                if (player == null)
                {
                    index++;
                    continue;
                }
                
                // 检查是否是HUD所有者
                bool isHudOwner = (player == hudOwner);
                
                // 更新指示器
                indicators[index].Update(
                    player,             // 玩家实例
                    isHudOwner,         // 是否为HUD所有者
                    canShowIndicator,   // 是否允许显示指示器
                    index               // 玩家索引
                );
                
                // 更新颜色
                indicators[index].UpdateColor(
                    player.ShortCutColor(),  // 玩家颜色
                    bombHolderColor          // 炸弹持有者颜色
                );
                
                index++;
            }
        }
        
        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            
            // 绘制所有玩家指示器
            for (int i = 0; i < indicators.Count; i++)
            {
                indicators[i].Draw(timeStacker);
            }
        }
        
        private ArenaGameSession GetGameSession()
        {
            if (Custom.rainWorld?.processManager?.currentMainLoop is RainWorldGame game)
            {
                if (game.session is ArenaGameSession arenaSession)
                {
                    return arenaSession;
                }
            }
            return null;
        }
        
        public override void ClearSprites()
        {
            // 清理所有玩家指示器
            foreach (var indicator in indicators)
            {
                indicator.ClearSprites();
            }
            indicators.Clear();
        }
    }
}