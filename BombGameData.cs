using System;
using RainMeadow;
using System.Linq;
using System.Collections.Generic;

namespace Meadow_MiniGame_HotPotato
{
    public class BombGameData : OnlineResource.ResourceData
    {
        public OnlinePlayer bombHolder;

        public int bombTimer = 30 * 40;//炸弹时间
        public int nextBombTimer = 30 * 40;//下次重置后的炸弹时间
        internal int initialBombTimer = 30 * 40;//初始炸弹时间

        //开始先倒计时3秒,倒计时结束后开始游戏
        public bool gameStarted = false;
        public bool gameOver = false;
        public int passCD = 0;

        public bool bombPassed = false;
        public Player bombHolderCache;//用于缓存上个炸弹持有者
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
            [OnlineField]
            public int passCD;

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
                passCD = bombData.passCD;
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
                bombData.passCD = passCD;
            }
        }
    }
}