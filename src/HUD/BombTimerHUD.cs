using System;
using System.Linq;
using System.Windows.Forms;
using RainMeadow;
using RWCustom;
using UnityEngine;
using HUD;

namespace Meadow_MiniGame_HotPotato.HUDStuff
{
    public class BombTimerHUD : HudPart
    {
        static readonly float textRectSize = 25f;
        RoomCamera cam;

        FLabel digiTen_1;//1=smaller
        FLabel digiTen_2;//2=bigger

        FLabel digiSingle_1;
        FLabel digiSingle_2;

        int counter;
        public int Counter => counter;
        int lastCounter;
        int syncGoalCounter;
        float smoothCounter;

        #region display
        bool reval;

        int lastIndex = -1;
        bool scrollDigiTen;

        float alpha;
        float lastSoundIndex;

        public BombTimerHUD(HUD.HUD hud, RoomCamera roomCamera) : base(hud)
        {
            cam = roomCamera;

            counter = 99 * 30;
            lastCounter = counter;
            lastSoundIndex = float.MaxValue;

            digiTen_1 = GetNewDigiLabel();
            hud.fContainers[0].AddChild(digiTen_1);

            digiTen_2 = GetNewDigiLabel();
            hud.fContainers[0].AddChild(digiTen_2);

            digiSingle_1 = GetNewDigiLabel();
            hud.fContainers[0].AddChild(digiSingle_1);

            digiSingle_2 = GetNewDigiLabel();
            hud.fContainers[0].AddChild(digiSingle_2);
        }

        #endregion

        public override void Update()
        {
            base.Update();
            if (HotPotatoArena.bombData != null && !HotPotatoArena.bombData.gameOver)
            {
                SyncCounter(HotPotatoArena.bombData.bombTimer);
            }
            else
            {
                StopTimer(true);
            }

            if (reval)
            {
                lastCounter = counter;
                counter -= InternalGetTickStep(counter, syncGoalCounter);
                syncGoalCounter--;
            }

            //if (Input.GetKeyDown(KeyCode.N))
            //{
            //    ResetCounter(20 * 40);
            //    StartTimer();
            //}
        }
        Color GetGoalColor(float index)
        {
            if (index > 15)
                return Color.white;
            else if (index <= 15 && index > 8)
                return Color.Lerp(Color.yellow, Color.white, (index - 8f) / (15f - 8f));
            else if (index <= 8 && index > 3)
                return Color.Lerp(Color.red, Color.yellow, (index - 3f) / (8f - 3f));
            else
                return Color.Lerp(Color.red, Color.white, Mathf.Sin(Time.time * Mathf.Pow((1f - index / 3f), 2f) * 4f));
        }

        void InternalUpdateTextCol(float floatIndex)
        {
            if (!reval)
                return;

            Color goalColor = GetGoalColor(floatIndex);
            digiSingle_1.color = goalColor;
            digiSingle_2.color = goalColor;
            digiTen_1.color = goalColor;
            digiTen_2.color = goalColor;
        }
        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            smoothCounter = Mathf.Lerp(lastCounter / 40f, counter / 40f, timeStacker);
            UpdateLabels(Mathf.Max(0f, smoothCounter), timeStacker);
            if (smoothCounter <= 0f)
                StopTimer(true);
        }

        public override void ClearSprites()
        {
            digiTen_1.isVisible = false;
            digiTen_1.RemoveFromContainer();

            digiTen_2.isVisible = false;
            digiTen_2.RemoveFromContainer();

            digiSingle_1.isVisible = false;
            digiSingle_1.RemoveFromContainer();

            digiSingle_2.isVisible = false;
            digiSingle_2.RemoveFromContainer();
        }

        void UpdateLabels(float floatIndex, float timeStacker)
        {
            int intIndex = Mathf.FloorToInt(floatIndex);
            if (intIndex != lastIndex)
            {
                lastIndex = intIndex;

                InternalUpdateTextAndScrollMode(intIndex, lastIndex);
            }

            float freq = InternalGetFreq(floatIndex);
            if ((lastSoundIndex - floatIndex) >= (1f / freq))
            {
                lastSoundIndex = floatIndex;
                InternalPlaySound();
            }

            InternalUpdateScroll(intIndex, lastIndex, floatIndex, timeStacker);
            InternalUpdateTextCol(floatIndex);
        }

        void InternalPlaySound(bool forcePlay = false)
        {
            if (!reval && !forcePlay)
                return;

            // 使用缓存获取炸弹持有者
            Player bombHolder = null;
            if (HotPotatoArena.bombData != null)
            {
                bombHolder = HotPotatoArena.bombData.bombHolderCache;
            }

            // 如果找到炸弹持有者，从其位置播放声音
            if (bombHolder != null && bombHolder.room != null)
            {
                cam.room.PlaySound(SoundID.Gate_Clamp_Lock, bombHolder.mainBodyChunk, false, 1f, 40f + UnityEngine.Random.value);
            }
        }

        float InternalGetFreq(float index)
        {
            //return 1;

            if (index > 10)
                return 1;
            if (index > 6)
                return 2;
            if (index > 3)
                return 4;
            return 6;
        }

        int InternalGetTickStep(int localCounter, int goalCounter)
        {
            if (localCounter > goalCounter)
            {
                int diff = localCounter - goalCounter;
                // 动态计算步进值：差值越大，步进越大
                // 使用对数函数使得增长更平滑
                int step = (int)Mathf.Ceil(Mathf.Log(diff + 1, 2));
                return Mathf.Clamp(step, 2, 10); // 限制最小2，最大10
            }
            else if (localCounter < goalCounter)
            {
                // 如果本地时间小于目标时间（比如时间被重置），直接跳转
                return goalCounter - localCounter;
            }
            else
            {
                return 1;
            }
        }

        void InternalUpdateScroll(int index, int lastIndex, float floatIndex, float timeStacker)
        {
            alpha = Mathf.Lerp(alpha, reval ? 1f : 0f, 0.15f);

            Vector2 anchor = new Vector2(Custom.rainWorld.options.ScreenSize.x / 2f, Custom.rainWorld.options.ScreenSize.y - 80f);
            int uppderIndex = index + 1;
            float t = floatIndex - (float)index;
            float reverseT = 1f - t;
            reverseT = CubicBezier(1f, 0f, 1f, 0f, reverseT);

            #region single
            if (index >= 10)
                digiSingle_1.x = anchor.x + (textRectSize * 0.6f);
            else
                digiSingle_1.x = anchor.x;
            digiSingle_1.y = anchor.y + Mathf.Cos(reverseT * Mathf.PI / 2f) * textRectSize;
            digiSingle_1.scaleY = Mathf.Sin(reverseT * Mathf.PI / 2f) * 2;
            digiSingle_1.alpha = Mathf.Sin(reverseT * Mathf.PI / 2f) * alpha;

            if (uppderIndex >= 10)
                digiSingle_2.x = anchor.x + (textRectSize * 0.6f);
            else
                digiSingle_2.x = anchor.x;
            digiSingle_2.y = anchor.y + Mathf.Cos((reverseT + 1) * Mathf.PI / 2f) * textRectSize;
            digiSingle_2.scaleY = Mathf.Sin((reverseT + 1) * Mathf.PI / 2f) * 2;
            digiSingle_2.alpha = Mathf.Sin((reverseT + 1) * Mathf.PI / 2f) * alpha;
            #endregion

            if (!scrollDigiTen)//强制置1来阻止10位数滚动
                reverseT = 1f;

            #region ten
            digiTen_1.x = anchor.x - (textRectSize * 0.6f);
            digiTen_1.y = anchor.y + Mathf.Cos(reverseT * Mathf.PI / 2f) * textRectSize;
            digiTen_1.scaleY = Mathf.Sin(reverseT * Mathf.PI / 2f) * 2;
            digiTen_1.alpha = Mathf.Sin(reverseT * Mathf.PI / 2f) * alpha;

            digiTen_2.x = anchor.x - (textRectSize * 0.6f);
            digiTen_2.y = anchor.y + Mathf.Cos((reverseT + 1) * Mathf.PI / 2f) * textRectSize;
            digiTen_2.scaleY = Mathf.Sin((reverseT + 1) * Mathf.PI / 2f) * 2;
            digiTen_2.alpha = Mathf.Sin((reverseT + 1) * Mathf.PI / 2f) * alpha;
            #endregion
        }

        void InternalUpdateTextAndScrollMode(int index, int lastIndex)
        {
            string currentText = index.ToString();
            string upperText = (index + 1).ToString();
            string lastText = lastIndex.ToString();

            if (currentText.Length > 1)
                digiTen_1.text = currentText[0].ToString();
            else
                digiTen_1.text = "";
            digiSingle_1.text = currentText.Last().ToString();


            if (upperText.Length > 1)
                digiTen_2.text = upperText[0].ToString();
            else
                digiTen_2.text = "";
            digiSingle_2.text = upperText.Last().ToString();

            scrollDigiTen = false;
            if (currentText.Length != upperText.Length)
                scrollDigiTen = true;
            else
            {
                if (currentText.Length == 2 && upperText[0] != currentText[0])
                    scrollDigiTen = true;
            }
        }

        FLabel GetNewDigiLabel()
        {
            return new FLabel(Custom.GetDisplayFont(), "")
            {
                isVisible = true,
                scaleX = 2f,
                scaleY = 2f,
                alpha = 0f
            };
        }

        float CubicBezier(float ax, float ay, float bx, float by, float t)
        {
            //see http://yisibl.github.io/cubic-bezier
            Vector2 a = Vector2.zero;
            Vector2 a1 = new Vector2(ax, ay);
            Vector2 b1 = new Vector2(bx, by);
            Vector2 b = Vector2.one;

            Vector2 c1 = Vector2.Lerp(a, a1, t);
            Vector2 c2 = Vector2.Lerp(b1, b, t);

            return Vector2.Lerp(c1, c2, t).y;
        }

        /// <summary>
        /// 重置计数器，并不会自动开启计时器
        /// </summary>
        /// <param name="startCounter"></param>
        public void ResetCounter(int startCounter)
        {
            counter = startCounter;
            lastCounter = counter;
            lastSoundIndex = float.MaxValue;
            syncGoalCounter = startCounter;
        }

        /// <summary>
        /// 同步并校准当前计时器的值
        /// </summary>
        /// <param name="currentCounter"></param>
        public void SyncCounter(int currentCounter)
        {
            if (!reval)
                StartTimer();

            if (currentCounter == syncGoalCounter)
                return;

            // 如果是时间重置（新时间大于当前时间）或者差值过大，直接同步
            if (Mathf.Abs(counter - currentCounter) > 40)
            {

                counter = currentCounter;
                lastCounter = counter;
                //用于重置声音,当超值过大重置一次来让声音重新播放
                lastSoundIndex = float.MaxValue;
                syncGoalCounter = currentCounter;
                return;
            }
            if (currentCounter > counter)
            {
                counter = currentCounter;
                lastCounter = counter;
                syncGoalCounter = currentCounter;
                return;
            }

            syncGoalCounter = currentCounter;
        }

        /// <summary>
        /// 开启计时器，显示计时器的同时开始计时，并不会自动重置计时器的值
        /// </summary>
        public void StartTimer()
        {
            if (reval)
                return;
            reval = true;
            syncGoalCounter = counter;
        }


        /// <summary>
        /// 关闭计时器，隐藏计时器的同时停止计时，并不会自动重置计时器的值
        /// </summary>
        public void StopTimer(bool playSound = false)
        {
            if (!reval)
                return;
            reval = false;
            if (playSound) InternalPlaySound(true);
        }
    }
}