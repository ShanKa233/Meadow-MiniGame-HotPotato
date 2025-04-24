using System;
using RainMeadow;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Meadow_MiniGame_HotPotato
{
    public class BombGameData : OnlineResource.ResourceData
    {
        public OnlinePlayer bombHolder;

        public int bombTimer;//炸弹时间
        public int nextBombTimer;//下次重置后的炸弹时间
        internal int initialBombTimer = GameTypeSetup.BombTimesInSecondsArray[GameTypeSetup.BombTimerIndex] * 40;//初始炸弹时间

        //开始先倒计时3秒,倒计时结束后开始游戏
        public bool gameStarted = false;
        public bool fristBombExplode = false;
        public bool gameOver = false;

        public int bombTimerIndex = GameTypeSetup.BombTimerIndex;//炸弹时间下标
        public int bombReduceTime = GameTypeSetup.BombReduceTime;//炸弹减少时间(秒)


        public int passCD = 0;

        public bool bombPassed = false;
        public Player bombHolderCache;//用于缓存上个炸弹持有者


        public void HandleBombTimer(bool reset = false, int reduceSecond = 0, ArenaGameSession session = null)
        {
            // 只有主机可以处理
            if (!OnlineManager.lobby.isOwner) return;

            if (reset)
            {
                // 重置为初始时间
                if (session != null && session.room?.abstractRoom?.name?.Contains("80s") == true)
                {
                    nextBombTimer = Mathf.Max(80 * 40, initialBombTimer);
                }
                else
                {
                    nextBombTimer = initialBombTimer;
                }
                bombTimer = nextBombTimer;
            }
            else if (reduceSecond > 0)
            {
                // 减少指定时间
                nextBombTimer = Mathf.Max(4 * 40, nextBombTimer - reduceSecond * 40);
                bombTimer = nextBombTimer;
            }
        }
        public BombGameData()
        {
        }

        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new GameState(this);
        }

        public class GameState : ResourceDataState
        {
            [OnlineField(nullable = true)]
            RainMeadow.Generics.DynamicUnorderedUshorts holderIds;

            [OnlineField]
            public int bombTimer;
            [OnlineField]
            public int nextBombTimer;
            [OnlineField]
            public bool gameStarted;
            [OnlineField]
            public bool gameOver;
            // [OnlineField]
            // public int passCD;
            [OnlineField]
            public bool fristBombExplode;

            //用于同步主机的时间设置
            [OnlineField]
            public int bombTimerIndex;
            [OnlineField]
            public int bombReduceTime;

            public GameState() { }

            public GameState(BombGameData bombData)
            {
                holderIds = new(new List<ushort> {
                    bombData.bombHolder?.inLobbyId ?? 0
                });

                bombTimer = bombData.bombTimer;
                nextBombTimer = bombData.nextBombTimer;
                gameStarted = bombData.gameStarted;
                gameOver = bombData.gameOver;
                // passCD = bombData.passCD;
                fristBombExplode = bombData.fristBombExplode;

                bombTimerIndex = bombData.bombTimerIndex;
                bombReduceTime = bombData.bombReduceTime;
            }

            public override Type GetDataType() => typeof(BombGameData);

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                BombGameData bombData = (BombGameData)data;

                bombData.bombHolder = holderIds.list.Any() && holderIds.list[0] != 0
                    ? OnlineManager.lobby.PlayerFromId(holderIds.list[0])
                    : null;

                bombData.bombTimer = bombTimer;
                bombData.nextBombTimer = nextBombTimer;
                bombData.gameStarted = gameStarted;
                bombData.gameOver = gameOver;
                // bombData.passCD = passCD;
                bombData.fristBombExplode = fristBombExplode;

                bombData.bombTimerIndex = bombTimerIndex;
                bombData.bombReduceTime = bombReduceTime;
            }
        }
    }
}