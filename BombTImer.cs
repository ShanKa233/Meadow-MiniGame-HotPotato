using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Meadow_MiniGame_HotPotato
{
    public class BombTImer : HUD.HudPart
    {
        private FLabel timerLabel;  // 显示时间的标签
        private FLabel modeLabel;   // 显示模式的标签
        private Vector2 pos, lastPos;  // 标签的位置
        public HotPotatoArena hpam;  // 游戏模式
        private Player? player;  // 当前玩家
        private float lastSoundTime;  // 上次播放声音的时间
        private float lastBeepValue;  // 上次计时的数值

        // 构造函数：初始化计时器
        public BombTImer(HUD.HUD hud, FContainer fContainer, HotPotatoArena tgm) : base(hud)
        {
            // 初始化时间标签
            timerLabel = new FLabel(Custom.GetFont(), FormatTime(0))
            {
                scale = 2.4f,
                alignment = FLabelAlignment.Center
            };
            // 设置标签位置
            pos = new Vector2(hud.rainWorld.options.ScreenSize.x/2, hud.rainWorld.options.ScreenSize.y - 60f);
            lastPos = pos;
            timerLabel.SetPosition(DrawPos(1f));

            // 将标签添加到容器中
            fContainer.AddChild(timerLabel);
            this.hpam = tgm;
            
            // 初始化音效变量
            lastSoundTime = float.MaxValue;
            lastBeepValue = float.MaxValue;
        }

        // 计算标签的绘制位置
        public Vector2 DrawPos(float timeStacker)
        {
            return Vector2.Lerp(lastPos, pos, timeStacker);  // 使用线性插值计算当前位置
        }

        // 更新计时器状态
        public override void Update()
        {
            base.Update();
            player = hud.owner as Player;
            if (player == null) return;

            // 播放计时音效
            float currentTime = HotPotatoArena.bombTimer/40f;
            if (ShouldPlaySound(currentTime))
            {
                PlayTimerSound();
                lastSoundTime = currentTime;
            }
        }

        // 判断是否应该播放音效
        private bool ShouldPlaySound(float currentTime)
        {
            if (currentTime <= 0 || !timerLabel.isVisible) return false;
            
            float freq = GetSoundFrequency(currentTime);
            return (lastSoundTime - currentTime) >= (1f / freq);
        }

        // 获取音效频率（根据剩余时间调整）
        private float GetSoundFrequency(float timeRemaining)
        {
            if (timeRemaining > 10)
                return 1f;
            if (timeRemaining > 6)
                return 2f;
            if (timeRemaining > 3)
                return 4f;
            return 6f;
        }

        // 播放计时音效
        private void PlayTimerSound()
        {
            if (player != null && player.room != null)
            {
                // 使用游戏中已有的声音ID
                player.room.PlaySound(SoundID.Gate_Clamp_Lock, player.mainBodyChunk, false, 0.8f, 1f + Random.value * 0.2f);
            }
        }

        // 绘制计时器
        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (HotPotatoArena.bombTimer <0)
            {
                timerLabel.isVisible = false;
                return;
            }
            
            timerLabel.isVisible = true;
            timerLabel.text = FormatTime(HotPotatoArena.bombTimer/40f);

            // 当剩余时间小于10秒时闪烁
            if (HotPotatoArena.bombTimer/40f < 10)
            {
                timerLabel.alpha = Mathf.PingPong(Time.time * 2, 1);
                // 设置颜色
                timerLabel.color = GetTimerColor(HotPotatoArena.bombTimer/40f);
            }
            else
            {
                timerLabel.alpha = 1;
                timerLabel.color = Color.white;
            }
        }

        // 根据剩余时间获取颜色
        private Color GetTimerColor(float timeRemaining)
        {
            if (timeRemaining > 10)
                return Color.white;
            else if (timeRemaining <= 10 && timeRemaining > 5)
                return Color.Lerp(Color.yellow, Color.white, (timeRemaining - 5f) / 5f);
            else if (timeRemaining <= 5 && timeRemaining > 2)
                return Color.Lerp(Color.red, Color.yellow, (timeRemaining - 2f) / 3f);
            else
                return Color.Lerp(Color.red, Color.white, Mathf.Sin(Time.time * Mathf.Pow((1f - timeRemaining / 2f), 2f) * 4f));
        }

        // 将时间格式化为 MM:SS:MMM 格式
        public static string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            int milliseconds = Mathf.FloorToInt((time % 1) * 1000);

            return $"{minutes:D2}:{seconds:D2}:{milliseconds:D2}";
        }

        // 清理计时器资源
        public override void ClearSprites()
        {
            base.ClearSprites();
            timerLabel.RemoveFromContainer();  // 移除时间标签
        }
    }
}