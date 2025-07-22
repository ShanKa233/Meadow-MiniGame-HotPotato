using System;
using Menu;
using RainMeadow;
using RWCustom;
using UnityEngine;

namespace Meadow_MiniGame_HotPotato.UI
{
    // 自定义分数控制器基类
    public abstract class ScoreController : PositionedMenuObject
    {
        // 添加统一的上下限设置
        protected const int DEFAULT_MIN_SCORE = 0;
        protected const int DEFAULT_MAX_SCORE = 99;
        protected const int NEGATIVE_MIN_SCORE = -100;
        protected const int NEGATIVE_THRESHOLD = 60; // allowNegativeCounter超过此值时允许负分

        // 使用虚方法允许子类重写上下限
        protected virtual int MinScore(int allowNegativeCounter) => 
            (allowNegativeCounter > NEGATIVE_THRESHOLD) ? NEGATIVE_MIN_SCORE : DEFAULT_MIN_SCORE;
        
        protected virtual int MaxScore => DEFAULT_MAX_SCORE;

        public class ScoreDragger : ButtonTemplate
        {
            public RoundedRect roundedRect;
            public MenuLabel label;
            private bool held;
            public int lastY;
            public float savMouse;
            public int savScore;
            private int forgetClicked;
            public int yHeldCounter;
            private int allowNegativeCounter;
            private float flash;
            private float lastFlash;
            private float greyFade;
            private float lastGreyFade;
            // 添加私有字段用于冻结状态跟踪
            private bool freezeMenu = false;

            public override Color MyColor(float timeStacker)
            {
                if (buttonBehav.greyedOut)
                {
                    return HSLColor.Lerp(Menu.Menu.MenuColor(Menu.Menu.MenuColors.VeryDarkGrey), Menu.Menu.MenuColor(Menu.Menu.MenuColors.Black), black).rgb;
                }
                float a = Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker);
                a = Mathf.Max(a, Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
                HSLColor from = HSLColor.Lerp(Menu.Menu.MenuColor(Menu.Menu.MenuColors.DarkGrey), Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey), a);
                from = HSLColor.Lerp(from, Menu.Menu.MenuColor(Menu.Menu.MenuColors.Black), black);
                return HSLColor.Lerp(from, Menu.Menu.MenuColor(Menu.Menu.MenuColors.VeryDarkGrey), Mathf.Lerp(lastGreyFade, greyFade, timeStacker)).rgb;
            }

            public void UpdateScoreText()
            {
                label.text = (owner as ScoreController).Score.ToString();
            }

            public ScoreDragger(Menu.Menu menu, MenuObject owner, Vector2 pos)
                : base(menu, owner, pos, new Vector2(24f, 24f))
            {
                roundedRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, filled: true);
                subObjects.Add(roundedRect);
                label = new MenuLabel(menu, this, "", new Vector2(0f, 2f), new Vector2(24f, 20f), bigText: false);
                subObjects.Add(label);
            }

            // 辅助方法：应用分数限制
            private int ClampScore(int score, bool flag)
            {
                ScoreController controller = owner as ScoreController;
                return Custom.IntClamp(score, controller.MinScore(allowNegativeCounter), controller.MaxScore);
            }

            public override void Update()
            {
                base.Update();
                buttonBehav.Update();
                lastFlash = flash;
                lastGreyFade = greyFade;
                flash = Mathf.Max(0f, flash - 1f / 7f);
                
                // 使用本地freezeMenu字段
                greyFade = Custom.LerpAndTick(greyFade, (freezeMenu && !held) ? 1f : 0f, 0.05f, 0.025f);
                
                if (buttonBehav.clicked)
                {
                    forgetClicked++;
                }
                else
                {
                    forgetClicked = 0;
                }
                roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);
                roundedRect.addSize = new Vector2(4f, 4f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * (float)Math.PI)) * (buttonBehav.clicked ? Mathf.InverseLerp(7f, 14f, forgetClicked) : 1f);
                bool flag = false;
                int num = (owner as ScoreController).Score;
                if (held)
                {
                    int num2 = num;
                    if (menu.manager.menuesMouseMode)
                    {
                        num = savScore + (int)Custom.LerpMap(Futile.mousePosition.y, savMouse - 300f, savMouse + 300f, -100f, 100f);
                        if (num < 0)
                        {
                            flag = true;
                        }
                        num = ClampScore(num, flag);
                    }
                    else
                    {
                        int y = menu.NonMouseInputDisregardingFreeze.y;
                        if (y != lastY || (yHeldCounter > 20 && yHeldCounter % ((yHeldCounter > 60) ? 2 : 4) == 0))
                        {
                            num += y * ((yHeldCounter <= 60) ? 1 : 2);
                        }
                        if (y != 0)
                        {
                            yHeldCounter++;
                        }
                        else
                        {
                            yHeldCounter = 0;
                        }
                        if (num < 0)
                        {
                            flag = true;
                        }
                        num = ClampScore(num, flag);
                        lastY = y;
                    }
                    if (num != num2)
                    {
                        flash = 1f;
                        menu.PlaySound(SoundID.MENU_Scroll_Tick);
                        buttonBehav.sizeBump = Mathf.Min(2.5f, buttonBehav.sizeBump + 1f);
                    }
                }
                else
                {
                    lastY = 0;
                    yHeldCounter = 0;
                }
                if (menu.manager.menuesMouseMode && MouseOver)
                {
                    int num3 = num;
                    num -= menu.mouseScrollWheelMovement;
                    if (num < 0)
                    {
                        flag = true;
                    }
                    num = ClampScore(num, flag);
                    if (num != num3)
                    {
                        flash = 1f;
                        menu.PlaySound(SoundID.MENU_Scroll_Tick);
                        buttonBehav.sizeBump = Mathf.Min(2.5f, buttonBehav.sizeBump + 1f);
                        savScore = num;
                    }
                }
                if (held && !menu.HoldButtonDisregardingFreeze)
                {
                    // 使用本地freezeMenu字段
                    freezeMenu = false;
                    held = false;
                }
                else if (!held && Selected && menu.pressButton)
                {
                    // 使用本地freezeMenu字段
                    freezeMenu = true;
                    savMouse = Futile.mousePosition.y;
                    savScore = (owner as ScoreController).Score;
                    held = true;
                }
                (owner as ScoreController).Score = num;
                if (num < 0)
                {
                    allowNegativeCounter = 120;
                }
                else
                {
                    allowNegativeCounter = Custom.IntClamp(allowNegativeCounter + ((!flag) ? (-1) : (menu.manager.menuesMouseMode ? 1 : 3)), 0, 120);
                }
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                float num = 0.5f - 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * (float)Math.PI * 2f);
                num *= buttonBehav.sizeBump;
                Color color = Color.Lerp(Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
                for (int i = 0; i < 9; i++)
                {
                    roundedRect.sprites[i].color = color;
                }
                if (owner is LockedScore)
                {
                    label.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey);
                }
                else
                {
                    color = ((!held) ? Color.Lerp(base.MyColor(timeStacker), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey), Mathf.Max(num, Mathf.Lerp(lastGreyFade, greyFade, timeStacker))) : Color.Lerp(Color.Lerp(Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey), num), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White), Mathf.Lerp(lastFlash, flash, timeStacker)));
                    label.label.color = color;
                }
                color = ((!held) ? MyColor(timeStacker) : Color.Lerp(Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey), Mathf.Lerp(lastFlash, flash, timeStacker)));
                for (int j = 9; j < 17; j++)
                {
                    roundedRect.sprites[j].color = color;
                }
            }

            public override void Clicked()
            {
            }
        }

        public ScoreDragger scoreDragger;

        // 获取游戏的炸弹时间设置
        public int[] BombTimesInSecondsArray => Meadow_MiniGame_HotPotato.UI.GameTypeSetup.BombTimesInSecondsArray;

        public virtual string DescriptorString => "";

        public virtual int Score
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public ScoreController(Menu.Menu menu, MenuObject owner)
            : base(menu, owner, default(Vector2))
        {
            scoreDragger = new ScoreDragger(menu, this, new Vector2(0f, 0f));
            subObjects.Add(scoreDragger);
        }
    }

    // 炸弹设置控制器
    public class BombScore : ScoreController
    {
        public MenuLabel descriptor;
        private string idString;
        // 可以重写炸弹减少时间的上下限
        protected override int MinScore(int allowNegativeCounter) => 0; // 最小0秒
        protected override int MaxScore => 99; // 最大99秒

        public override string DescriptorString => menu.Translate("Bomb Reduce Time:");

        public override int Score
        {
            get
            {
                return HotPotatoArena.bombData.bombReduceTime;
            }
            set
            {
                HotPotatoArena.bombData.bombReduceTime = value;
                MiniGameHotPotato.MiniGameHotPotato.options.BombReduceTime.Value = value;
                MiniGameHotPotato.MiniGameHotPotato.options._SaveConfigFile();
                scoreDragger.UpdateScoreText();
            }
        }

        public BombScore(Menu.Menu menu, MenuObject owner, string description, string idString)
            : base(menu, owner)
        {
            this.idString = idString;
            descriptor = new MenuLabel(menu, this, description, new Vector2(-120f, 0f), new Vector2(120f, 30f), bigText: false);
            subObjects.Add(descriptor);
            
            scoreDragger.UpdateScoreText();
        }
    }

    // 锁定的分数控制器（只读）
    public class LockedScore : ScoreController
    {
        private string idString;
        private int value;
        
        public override int Score
        {
            get => value;
            set { /* 锁定，不允许修改 */ }
        }
        
        public LockedScore(Menu.Menu menu, MenuObject owner, string idString, int value)
            : base(menu, owner)
        {
            this.idString = idString;
            this.value = value;
            scoreDragger.buttonBehav.greyedOut = true;
            scoreDragger.UpdateScoreText();
        }
    }
} 