using System;
using UnityEngine;
using RWCustom;
using RainMeadow;
using Smoke;

namespace Meadow_MiniGame_HotPotato
{
    public class ShortCutBlocker
    {
        public ShortcutData shortcut;
        public IntVector2 startTile;
        public IntVector2 destTile;
        public IntVector2 startDir;
        public IntVector2 destDir;
        // 被禁止的管道信息
        public int bannedLifeTime = 120; // 默认120帧(3秒)的禁止时间
        // 烟雾特效
        public FireSmoke startSmoke;
        public FireSmoke endSmoke;
        // 当前房间
        public Room room;

        public ShortCutBlocker(ShortcutData shortcut, IntVector2 startTile, IntVector2 destTile, IntVector2 startDir, IntVector2 destDir, Room room)
        {
            this.shortcut = shortcut;
            this.startTile = startTile;
            this.destTile = destTile;
            this.startDir = startDir;
            this.destDir = destDir;
            this.room = room;
        }

    }
    public partial class HotPotatoArena
    {
        public ShortCutBlocker blockedShortCut;
        public void UpdateShortCutBlocker(ArenaGameSession session)
        {
            if (bombData.bombHolderCache == null) return;
            if (bombData.bombHolderCache.dead) return;

            if (blockedShortCut != null)
            {
                if (!bombData.bombHolderCache.inShortcut) blockedShortCut.bannedLifeTime--;
                if (blockedShortCut.bannedLifeTime <= 0)
                {
                    // 销毁烟雾特效
                    blockedShortCut.startSmoke?.Destroy();
                    blockedShortCut.endSmoke?.Destroy();
                    blockedShortCut = null;
                }
                else
                {
                    // 更新管道锁烟雾效果
                    UpdateShortCutBlockerSmoke(session);
                }
            }
        }

        // 处理管道锁的烟雾效果
        public void UpdateShortCutBlockerSmoke(ArenaGameSession session)
        {
            if (blockedShortCut == null || session == null || blockedShortCut.room == null) return;

            // 处理起点和终点的烟雾效果
            UpdateSmokeEffect(isStart: true);
            UpdateSmokeEffect(isStart: false);
        }

        private void UpdateSmokeEffect(bool isStart)
        {
            // 获取对应的烟雾引用和位置信息
            ref FireSmoke smoke = ref (isStart ? ref blockedShortCut.startSmoke : ref blockedShortCut.endSmoke);
            IntVector2 tile = isStart ? blockedShortCut.startTile : blockedShortCut.destTile;
            IntVector2 dir = isStart ? blockedShortCut.startDir : blockedShortCut.destDir;
            Room room = blockedShortCut.room;

            // 计算位置
            Vector2 pos = new Vector2(
                tile.x * 20 + 10 + dir.x * 5,
                tile.y * 20 + 10 + dir.y * 5
            );

            // 创建或更新烟雾
            if (smoke == null || smoke.room != room)
            {
                smoke?.Destroy();
                smoke = new FireSmoke(room);
                room.AddObject(smoke);
            }

            // 更新烟雾效果
            if (smoke != null)
            {
                smoke.Update(false);

                // 计算烟雾颜色和方向
                Color smokeColor = Custom.HSL2RGB(
                    Custom.LerpMap(blockedShortCut.bannedLifeTime, 120, 0, 0, 20) / 360f,
                    0.8f,
                    0.5f);

                Vector2 smokeVel = Custom.RNV() * 0.4f + dir.ToVector2() * 30;

                // 发射烟雾
                smoke.EmitSmoke(pos, smokeVel, smokeColor, 15);
            }
        }
    }
}