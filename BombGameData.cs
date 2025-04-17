using System;
using RainMeadow;

namespace Meadow_MiniGame_HotPotato
{
    public class BombGameData : OnlineResource.ResourceData
    {

        public OnlinePlayer bombHolder;

        public int bombTimer = 30 * 40;//炸弹时间
        public int nextBombTimer = 30 * 40;//下次重置后的炸弹时间
        public int initialBombTimer = 30 * 40;//初始炸弹时间

        //开始先倒计时3秒,倒计时结束后开始游戏
        public bool gameStarted = false;
        public bool gameOver = false;
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
            public OnlinePlayer bombHolder;

            [OnlineField]
            public int bombTimer;
            [OnlineField]
            public int nextBombTimer;
            [OnlineField]
            public bool gameStarted;
            [OnlineField]
            public bool gameOver;
            public GameState(BombGameData bombData)
            {
                bombHolder = bombData.bombHolder;

                bombTimer = bombData.bombTimer;
                nextBombTimer = bombData.nextBombTimer;

                gameStarted = bombData.gameStarted;
                gameOver = bombData.gameOver;
            }


            public override Type GetDataType() => typeof(BombGameData);

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                BombGameData bombData = (BombGameData)data;

                bombData.bombHolder = bombHolder;
                bombData.bombTimer = bombTimer;
                bombData.nextBombTimer = nextBombTimer;
                bombData.gameStarted = gameStarted;
                bombData.gameOver = gameOver;
            }
        }
    }
}