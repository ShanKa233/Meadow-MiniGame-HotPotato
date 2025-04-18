using Kittehface.Framework20;
using Menu;
using Menu.Remix;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;
namespace RainMeadow
{
    // 竞技场大厅菜单类，用于管理多人在线游戏的大厅界面
    public class ArenaLobbyMenu : MultiplayerMenu
    {
        // 获取当前竞技场在线游戏模式的引用
        private ArenaOnlineGameMode arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;

        // 布局相关的静态变量
        private static float num = 120f;    // 基本宽度单位
        private static float num2 = 0f;     // 间距偏移量
        private static float num3 = num - num2; // 实际使用的宽度

        // UI元素
        public MenuLabel totalClientsReadiedUpOnPage;  // 显示已准备就绪的玩家数量的标签
        public MenuLabel currentLevelProgression;      // 显示当前关卡进程的标签
        public MenuLabel displayCurrentGameMode;       // 显示当前游戏模式的标签

        // 导航按钮及相关变量
        private SimplerSymbolButton viewNextPlayer, viewPrevPlayer, colorConfigButton; // 查看下一个/上一个玩家的按钮，以及颜色配置按钮
        private int holdPlayerPosition;     // 用于导航时保存的玩家位置
        private int currentPlayerPosition;  // 当前显示的玩家位置
        private bool initiatedStartGameForClient; // 是否已为客户端初始化开始游戏流程
        
        // 游戏数据
        public List<SlugcatStats.Name> allSlugs; // 所有可选的蛞蝓猫
        public Dictionary<string, bool> playersReadiedUp = new Dictionary<string, bool>(); // 记录哪些玩家已准备好

        // 获取屏幕宽度的属性
        int ScreenWidth => (int)manager.rainWorld.options.ScreenSize.x; // 参考值为1360

        // UI控件数组
        public SimplerButton[] usernameButtons; // 玩家用户名按钮数组
        public SimplerButton forceReady;        // 强制准备按钮
        private string forceReadyText = "FORCE READY"; // 强制准备按钮的文本

        // 标记哪些UI元素已创建
        public bool meUsernameButtonCreated = false; // 当前玩家的用户名按钮是否已创建
        public bool meClassButtonCreated = false;    // 当前玩家的职业按钮是否已创建

        public ArenaOnlinePlayerJoinButton[] classButtons; // 玩家职业选择按钮数组
        private bool flushArenaSittingForWaitingClients = false; // 是否需要为等待的客户端刷新竞技场会话

        // 构造函数，初始化竞技场大厅菜单
        public ArenaLobbyMenu(ProcessManager manager) : base(manager)
        {
            ID = OnlineManager.lobby.gameMode.MenuProcessId();
            RainMeadow.DebugMe();

            if (OnlineManager.lobby == null) throw new InvalidOperationException("lobby is null");

            if (OnlineManager.lobby.isOwner)
            {
                ArenaHelpers.ResetOnReturnToMenu(arena, this); // 如果是房主，重置菜单状态
                arena.ResetForceReadyCountDown(); // 重置强制准备倒计时
            }

            allSlugs = ArenaHelpers.AllSlugcats(); // 获取所有可选的蛞蝓猫
            holdPlayerPosition = 3; // 设置导航时使用的玩家位置
            ArenaHelpers.ResetReadyUpLogic(arena, this); // 重置准备逻辑

            OverrideMultiplayerMenu(); // 覆盖多人游戏菜单的默认设置
            BindSettings(); // 绑定设置
            BuildLayout(); // 构建布局

            MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived; // 订阅玩家列表接收事件
            initiatedStartGameForClient = false; // 初始化客户端开始游戏标志
            if (arena.currentGameMode == "" || arena.currentGameMode is null)
            {
                arena.currentGameMode = Competitive.CompetitiveMode.value; // 如果当前游戏模式为空，设置为竞技模式
            }
        }

        // 移除多余的竞技场对象
        void RemoveExcessArenaObjects()
        {
            if (OnlineManager.lobby.isOwner)
            {
                arena.playList.Clear(); // 如果是房主，清空播放列表
            }
            if (this.playerClassButtons != null && this.playerClassButtons.Length > 0)
            {
                for (int i = this.playerClassButtons.Length - 1; i >= 0; i--)
                {
                    this.playerClassButtons[i].RemoveSprites(); // 移除角色选择按钮的精灵
                    this.pages[0].RecursiveRemoveSelectables(playerClassButtons[i]); // 从页面中递归移除可选择元素
                }
            }

            if (this.playerJoinButtons != null && this.playerJoinButtons.Length > 0)
            {
                for (int i = this.playerJoinButtons.Length - 1; i >= 0; i--)
                {
                    this.playerJoinButtons[i].RemoveSprites(); // 移除加入游戏按钮的精灵
                    this.pages[0].RecursiveRemoveSelectables(playerJoinButtons[i]); // 从页面中递归移除可选择元素
                }
            }
            if (this.resumeButton != null)
            {
                this.resumeButton.RemoveSprites(); // 移除继续按钮的精灵
                this.pages[0].RecursiveRemoveSelectables(this.resumeButton); // 从页面中移除继续按钮
            }

            if (this.levelSelector != null && this.levelSelector.levelsPlaylist != null)
            {
                if (!OnlineManager.lobby.isOwner)
                {
                    for (int i = this.levelSelector.levelsPlaylist.levelItems.Count - 1; i >= 0; i--)
                    {
                        this.GetGameTypeSetup.playList.RemoveAt(this.GetGameTypeSetup.playList.Count - 1); // 移除游戏类型设置中的播放列表项
                        this.levelSelector.levelsPlaylist.RemoveLevelItem(new Menu.LevelSelector.LevelItem(this, this.levelSelector.levelsPlaylist, this.levelSelector.levelsPlaylist.levelItems[i].name)); // 从级别选择器中移除级别项
                        this.levelSelector.levelsPlaylist.ScrollPos = this.levelSelector.levelsPlaylist.LastPossibleScroll; // 设置滚动位置
                        this.levelSelector.levelsPlaylist.ConstrainScroll(); // 约束滚动范围
                    }
                }
                else
                {
                    arena.playList = this.GetGameTypeSetup.playList; // 如果是房主，设置竞技场的播放列表
                }
            }
        }

        // 覆盖多人游戏菜单的默认设置
        void OverrideMultiplayerMenu()
        {
            RemoveExcessArenaObjects(); // 移除多余的竞技场对象

            this.currentGameType = this.nextGameType = ArenaSetup.GameTypeID.Competitive; // 设置当前和下一个游戏类型为竞技模式
            this.nextButton.signalText = "NEXTONLINEGAME"; // 设置下一个按钮的信号文本
            this.prevButton.signalText = "PREVONLINEGAME"; // 设置上一个按钮的信号文本

            this.backButton.signalText = "BACKTOLOBBY"; // 设置返回按钮的信号文本
            this.playButton.signalText = "STARTARENAONLINEGAME"; // 设置开始游戏按钮的信号文本
        }

        // 绑定设置
        private void BindSettings()
        {
            arena.avatarSettings.eyeColor = RainMeadow.rainMeadowOptions.EyeColor.Value; // 绑定眼睛颜色设置
            arena.avatarSettings.bodyColor = RainMeadow.rainMeadowOptions.BodyColor.Value; // 绑定身体颜色设置
            arena.avatarSettings.playingAs = SlugcatStats.Name.White; // 设置默认角色为白色蛞蝓猫
        }

        // 构建布局
        void BuildLayout()
        {
            BuildPlayerSlots(); // 构建玩家槽位
            AddAbovePlayText(); // 添加播放按钮上方的文本
            if (OnlineManager.lobby.isOwner)
            {
                AddForceReadyUp(); // 如果是房主，添加强制准备按钮
            }

            if (this.levelSelector != null && this.levelSelector.levelsPlaylist != null)
            {
                if (!OnlineManager.lobby.isOwner)
                {
                    foreach (var level in arena.playList)
                    {
                        this.GetGameTypeSetup.playList.Add(level); // 将竞技场播放列表中的关卡添加到游戏类型设置的播放列表中
                        this.levelSelector.levelsPlaylist.AddLevelItem(new Menu.LevelSelector.LevelItem(this, this.levelSelector.levelsPlaylist, level)); // 添加关卡项到关卡选择器
                        this.levelSelector.levelsPlaylist.ScrollPos = this.levelSelector.levelsPlaylist.LastPossibleScroll; // 设置滚动位置到最后
                        this.levelSelector.levelsPlaylist.ConstrainScroll(); // 约束滚动范围
                    }
                }
            }

            if (usernameButtons != null)
            {
                // 创建显示当前游戏模式的标签
                this.displayCurrentGameMode = new MenuLabel(this, pages[0], this.Translate($"Current Mode:") + " " + Utils.Translate(arena.currentGameMode ?? ""), new Vector2(this.usernameButtons[0].pos.x, usernameButtons[0].pos.y + 200f), new Vector2(10f, 10f), true);
                this.displayCurrentGameMode.label.alignment = FLabelAlignment.Left;
                this.pages[0].subObjects.Add(displayCurrentGameMode);
                
                // 创建显示已准备玩家数量的标签
                this.totalClientsReadiedUpOnPage = new MenuLabel(this, pages[0], this.Translate($"Ready:") + " " + arena.playersReadiedUp.list.Count + "/" + OnlineManager.players.Count, new Vector2(displayCurrentGameMode.pos.x, usernameButtons[0].pos.y + 170f), new Vector2(10f, 10f), false);
                this.totalClientsReadiedUpOnPage.label.alignment = FLabelAlignment.Left;
                this.pages[0].subObjects.Add(totalClientsReadiedUpOnPage);

                // 创建显示播放列表进度的标签
                this.currentLevelProgression = new MenuLabel(this, pages[0], this.Translate($"Playlist Progress:") + " " + arena.currentLevel + "/" + arena.totalLevelCount, new Vector2(displayCurrentGameMode.pos.x, usernameButtons[0].pos.y + 150f), new Vector2(10f, 10f), false);
                this.currentLevelProgression.label.alignment = FLabelAlignment.Left;
                this.pages[0].subObjects.Add(currentLevelProgression);
                
                // 如果MMF模组已安装，添加颜色配置按钮
                if (ModManager.MMF)
                {
                    colorConfigButton = new(this, pages[0], "Kill_Slugcat", "", usernameButtons[0].pos + new Vector2(-44, 0f));
                    colorConfigButton.OnClick += (_) =>
                    {
                        manager.ShowDialog(new ColorSlugcatDialog(manager, arena.avatarSettings.playingAs, () => { })); // 显示颜色配置对话框
                    };
                    pages[0].subObjects.Add(colorConfigButton);
                    MutualHorizontalButtonBind(colorConfigButton, usernameButtons[0]); // 将颜色配置按钮与用户名按钮绑定
                }
            }
        }

        // 创建按钮的辅助方法
        SimplerButton CreateButton(string text, Vector2 pos, Vector2 size, Action<SimplerButton>? clicked = null, Page? page = null)
        {
            page ??= pages[0]; // 如果未指定页面，使用第一页
            var b = new SimplerButton(this, page, text, pos, size); // 创建简单按钮
            if (clicked != null) b.OnClick += clicked; // 如果提供了点击回调，添加到按钮的点击事件
            page.subObjects.Add(b); // 将按钮添加到页面的子对象列表
            return b; // 返回创建的按钮
        }

        // 构建玩家槽位
        void BuildPlayerSlots()
        {
            AddClassButtons(); // 添加角色选择按钮
            AddUsernames(); // 添加用户名显示

            this.GetArenaSetup.playersJoined[0] = true; // 设置房主已加入游戏

            if (OnlineManager.players.Count > 4)
            {
                HandleLobbyProfileOverflow(); // 如果玩家数量超过4个，处理大厅个人资料溢出
            }
        }

        // 添加播放按钮上方的文本
        void AddAbovePlayText()
        {
            this.abovePlayButtonLabel = new MenuLabel(this, pages[0], "", this.playButton.pos + new Vector2((0f - this.playButton.size.x) / 2f + 0.01f, 50.01f), new Vector2(this.playButton.size.x, 20f), bigText: false);
            this.abovePlayButtonLabel.label.alignment = FLabelAlignment.Left;
            this.abovePlayButtonLabel.label.color = MenuRGB(MenuColors.DarkGrey);
            pages[0].subObjects.Add(this.abovePlayButtonLabel);
            if (manager.rainWorld.options.ScreenSize.x < 1280f)
            {
                this.abovePlayButtonLabel.label.alignment = FLabelAlignment.Right; // 在小屏幕上调整对齐方式
                this.abovePlayButtonLabel.pos.x = this.playButton.pos.x + 55f; // 调整位置
            }
        }

        // 初始化新的在线游戏会话
        private void InitializeNewOnlineSitting()
        {
            manager.arenaSitting = new ArenaSitting(this.GetGameTypeSetup, this.multiplayerUnlocks); // 创建新的竞技场会话

            manager.arenaSitting.levelPlaylist = new List<string>(); // 初始化关卡播放列表

            if (this.GetGameTypeSetup.shufflePlaylist)
            {
                // 如果设置了随机播放，随机排序关卡列表
                List<string> list2 = new List<string>();
                for (int l = 0; l < this.GetGameTypeSetup.playList.Count; l++)
                {
                    list2.Add(this.GetGameTypeSetup.playList[l]);
                }

                while (list2.Count > 0)
                {
                    int index2 = UnityEngine.Random.Range(0, list2.Count); // 随机选择一个关卡
                    for (int m = 0; m < this.GetGameTypeSetup.levelRepeats; m++)
                    {
                        manager.arenaSitting.levelPlaylist.Add(list2[index2]); // 根据重复次数添加到播放列表
                    }
                    list2.RemoveAt(index2); // 从临时列表中移除已添加的关卡
                }
            }
            else
            {
                // 如果不随机播放，按顺序添加关卡
                for (int n = 0; n < this.GetGameTypeSetup.playList.Count; n++)
                {
                    for (int num = 0; num < this.GetGameTypeSetup.levelRepeats; num++)
                    {
                        manager.arenaSitting.levelPlaylist.Add(this.GetGameTypeSetup.playList[n]); // 根据重复次数添加到播放列表
                    }
                }
            }

            // 房主决定播放列表
            if (OnlineManager.lobby.isOwner)
            {
                arena.playList = manager.arenaSitting.levelPlaylist; // 设置竞技场的播放列表

                // 确保所有玩家都在在线顺序列表中
                for (int i = 0; i < OnlineManager.players.Count; i++)
                {
                    if (!arena.arenaSittingOnlineOrder.Contains(OnlineManager.players[i].inLobbyId))
                    {
                        arena.arenaSittingOnlineOrder.Add(OnlineManager.players[i].inLobbyId);
                    }
                }
                arena.totalLevelCount = manager.arenaSitting.levelPlaylist.Count; // 设置总关卡数
            }
            // 客户端获取播放列表
            else
            {
                manager.arenaSitting.levelPlaylist = arena.playList; // 从竞技场获取播放列表
            }

            ArenaHelpers.SetProfileColor(arena); // 设置个人资料颜色
            if (arena.registeredGameModes.Values.Contains(arena.currentGameMode))
            {
                // 查找当前游戏模式并设置
                arena.onlineArenaGameMode = arena.registeredGameModes.FirstOrDefault(kvp => kvp.Value == arena.currentGameMode).Key;
                RainMeadow.Debug("Playing GameMode: " + arena.onlineArenaGameMode);
            }
            else
            {
                // 如果找不到当前游戏模式，回退到竞技模式
                RainMeadow.Error("Could not find gamemode in list! Setting to Competitive as a fallback");
                arena.onlineArenaGameMode = arena.registeredGameModes.FirstOrDefault(kvp => kvp.Value == Competitive.CompetitiveMode.value).Key;
            }
            arena.onlineArenaGameMode.InitAsCustomGameType(this.GetGameTypeSetup); // 初始化自定义游戏类型
        }

        // 开始游戏
        private void StartGame()
        {
            RainMeadow.DebugMe(); // 调试日志

            // 如果已在游戏中但当前玩家未准备好，直接返回
            if (arena.isInGame && !arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id))
            {
                return;
            }

            // 如果大厅不存在或不活跃，直接返回
            if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;

            // 如果是房主且没有选择关卡，直接返回
            if (OnlineManager.lobby.isOwner && this.GetGameTypeSetup.playList != null && this.GetGameTypeSetup.playList.Count == 0)
            {
                return; // 不要做傻事
            }

            // 如果游戏已开始但不是所有玩家都准备好，直接返回
            if (arena.playersReadiedUp.list.Count != OnlineManager.players.Count && arena.isInGame)
            {
                return;
            }

            // 处理准备逻辑
            if (!arena.allPlayersReadyLockLobby)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    // 如果是房主且未准备好，标记为准备好
                    if (!arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id))
                    {
                        arena.playersReadiedUp.list.Add(OnlineManager.mePlayer.id);
                    }
                }
                else
                {
                    // 如果是客户端，通知房主已准备好
                    if (OnlineManager.players.Count > 1 && !arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id))
                    {
                        OnlineManager.lobby.owner.InvokeRPC(ArenaRPCs.Arena_NotifyLobbyReadyUp, OnlineManager.mePlayer);
                        this.playButton.menuLabel.text = this.Translate("Waiting for others..."); // 更新按钮文本
                        this.playButton.inactive = true; // 禁用按钮
                        this.playButton.buttonBehav.greyedOut = true; // 使按钮变灰
                    }
                }
                return;
            }

            // 如果不是房主且游戏未开始，直接返回
            if (!OnlineManager.lobby.isOwner && !arena.isInGame)
            {
                return;
            }
            
            // 设置角色颜色并初始化游戏
            arena.avatarSettings.currentColors = GetPersonalColors(arena.avatarSettings.playingAs);
            InitializeNewOnlineSitting(); // 初始化新的在线游戏会话
            ArenaHelpers.SetupOnlineArenaStting(arena, this.manager); // 设置在线竞技场会话
            this.manager.rainWorld.progression.ClearOutSaveStateFromMemory(); // 清除内存中的保存状态
            
            // 临时设置用户输入
            UserInput.SetUserCount(OnlineManager.players.Count); // 设置用户数量
            UserInput.SetForceDisconnectControllers(forceDisconnect: false); // 设置不强制断开控制器
            this.PlaySound(SoundID.MENU_Start_New_Game); // 播放开始新游戏音效
            this.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game); // 请求切换到游戏进程
        }
        // 图形更新
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (colorConfigButton != null)
            {
                // 根据是否启用自定义颜色设置颜色配置按钮的透明度
                colorConfigButton.symbolSprite.alpha = this.IsCustomColorEnabled(arena.avatarSettings.playingAs) ? 1 : 0.2f;
            }
        }
        // 更新逻辑
        public override void Update()
        {
            base.Update();

            if (OnlineManager.lobby == null) return; // 如果大厅不存在，直接返回

            if (OnlineManager.lobby.isOwner)
            {
                if (this.forceReady != null)
                {
                    if (arena.forceReadyCountdownTimer > 0)
                    {
                        // 如果强制准备倒计时大于0，禁用按钮并显示倒计时
                        this.forceReady.buttonBehav.greyedOut = true;
                        this.forceReady.menuLabel.text = forceReadyText + $" ({arena.forceReadyCountdownTimer})";
                    }
                    else
                    {
                        // 倒计时结束，恢复按钮文本
                        this.forceReady.menuLabel.text = forceReadyText;
                    }

                    if (arena.playersReadiedUp != null && arena.playersReadiedUp.list != null && arena.forceReadyCountdownTimer <= 0)
                    {
                        // 如果所有玩家都准备好了，禁用强制准备按钮
                        this.forceReady.buttonBehav.greyedOut = OnlineManager.players.Count == arena.playersReadiedUp.list.Count;
                    }
                }
            }

            // 更新UI标签
            if (this.totalClientsReadiedUpOnPage != null)
            {
                UpdateReadyUpLabel(); // 更新准备状态标签
            }

            if (this.currentLevelProgression != null)
            {
                UpdateLevelCounter(); // 更新关卡计数器
            }

            if (this.displayCurrentGameMode != null)
            {
                UpdateGameModeLabel(); // 更新游戏模式标签
            }

            // 客户端准备好且游戏开始时，启动游戏
            if (arena.allPlayersReadyLockLobby && arena.isInGame && arena.arenaSittingOnlineOrder.Contains(OnlineManager.mePlayer.inLobbyId) && !OnlineManager.lobby.isOwner && !initiatedStartGameForClient && arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id))  // 是时候开始了
            {
                this.StartGame(); // 开始游戏
                initiatedStartGameForClient = true; // 标记已为客户端初始化游戏
            }

            if (this.playButton != null)
            {
                // 根据不同情况更新开始游戏按钮的状态和文本

                if (arena.playersReadiedUp.list.Count == 0 && arena.returnToLobby)
                {
                    // 如果没有玩家准备好且返回大厅，显示"准备好了吗？"
                    this.playButton.menuLabel.text = this.Translate("READY?");
                    this.playButton.inactive = false;
                }

                if (OnlineManager.players.Count == 1)
                {
                    // 如果只有一个玩家，显示"等待其他玩家"
                    this.playButton.menuLabel.text = this.Translate("WAIT FOR OTHERS");
                }

                if (this.GetGameTypeSetup.playList.Count == 0 && OnlineManager.lobby.isOwner)
                {
                    // 如果是房主且没有选择关卡，禁用开始按钮
                    this.playButton.buttonBehav.greyedOut = true;
                }

                if (this.GetGameTypeSetup.playList.Count * this.GetGameTypeSetup.levelRepeats >= 0)
                {
                    if (this.abovePlayButtonLabel != null)
                    {
                        // 移除开始按钮上方的标签
                        this.abovePlayButtonLabel.RemoveSprites();
                        this.pages[0].RemoveSubObject(this.abovePlayButtonLabel);
                    }
                    if (!OnlineManager.lobby.isOwner) // 允许客户端在未选择地图时准备
                    {
                        this.playButton.buttonBehav.greyedOut = false;
                    }
                }

                if (arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id) && OnlineManager.players.Count > 1)
                {
                    // 如果当前玩家已准备好且有多个玩家，禁用开始按钮
                    this.playButton.inactive = true;
                }

                if (arena.playersReadiedUp.list.Count == OnlineManager.players.Count)
                {
                    // 如果所有玩家都准备好了，锁定大厅
                    arena.allPlayersReadyLockLobby = true;

                    if (OnlineManager.players.Count == 1)
                    {
                        // 如果只有一个玩家，显示"大厅将锁定"
                        this.playButton.menuLabel.text = this.Translate("LOBBY WILL LOCK"); // 你确定要进入主机模式吗？没有人可以加入你
                    }
                    else
                    {
                        if (OnlineManager.lobby.isOwner)
                        {
                            // 如果是房主，显示"进入"
                            this.playButton.menuLabel.text = this.Translate("ENTER");
                            this.playButton.inactive = false;
                        }
                    }
                }

                // 客户端逻辑
                if (!OnlineManager.lobby.isOwner)
                {
                    if (arena.isInGame)
                    {
                        // 如果游戏已开始，禁用开始按钮
                        this.playButton.inactive = true;
                        if (!arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id)) // 你来晚了
                        {
                            this.playButton.menuLabel.text = this.Translate("GAME IN SESSION");
                        }
                    }
                    if (!arena.isInGame && !arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id))
                    {
                        // 如果游戏未开始且当前玩家未准备好，显示"准备好了吗？"
                        this.playButton.menuLabel.text = this.Translate("READY?");
                        this.playButton.inactive = false;
                    }
                    if (!arena.isInGame && arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id) && arena.playersReadiedUp.list.Count != OnlineManager.players.Count)
                    {
                        // 如果游戏未开始且当前玩家已准备好但不是所有玩家都准备好，显示"等待其他玩家..."
                        this.playButton.menuLabel.text = this.Translate("Waiting for others...");
                        this.playButton.inactive = true;
                    }

                    if (!arena.isInGame && arena.playersReadiedUp.list.Contains(OnlineManager.mePlayer.id) && arena.playersReadiedUp.list.Count == OnlineManager.players.Count)
                    {
                        // 如果游戏未开始且所有玩家都准备好了，显示"等待房主..."
                        this.playButton.menuLabel.text = this.Translate("Waiting for host...");
                        this.playButton.inactive = true;
                    }
                }

                if (arena.returnToLobby && !flushArenaSittingForWaitingClients) // 返回大厅，重置所有内容
                {
                    ArenaHelpers.ResetReadyUpLogic(arena, this); // 重置准备逻辑
                    flushArenaSittingForWaitingClients = true; // 标记已为等待的客户端刷新竞技场会话
                }
            }
        }
        // 关闭进程
        public override void ShutDownProcess()
        {
            RainMeadow.DebugMe(); // 调试日志
            manager.rainWorld.progression.SaveProgression(true, true); // 保存游戏进度
            if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
            {
                OnlineManager.LeaveLobby(); // 如果下一个进程不是游戏，离开大厅
            }
            base.ShutDownProcess(); // 调用基类方法
        }

        // 处理菜单信号
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "BACKTOLOBBY")
            {
                // 返回大厅信号
                manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu); // 请求切换到大厅选择菜单
                base.PlaySound(SoundID.MENU_Switch_Page_Out); // 播放页面切换音效
            }
            if (message == "STARTARENAONLINEGAME")
            {
                // 开始竞技场在线游戏信号
                StartGame(); // 开始游戏
            }

            if (message == "INFO" && infoWindow != null)
            {
                // 信息窗口信号
                infoWindow.label.text = Regex.Replace(this.Translate("Welcome to Arena Online!<LINE>All players must ready up to begin."), "<LINE>", "\r\n"); // 设置信息窗口文本
            }
            if (OnlineManager.lobby.isOwner)
            {
                if (message == "NEXTONLINEGAME")
                {
                    // 下一个在线游戏信号（仅限房主）
                    var gameModesList = arena.registeredGameModes.ToList(); // 获取已注册游戏模式列表

                    // 查找当前游戏模式的索引
                    var currentModeIndex = gameModesList.FindIndex(kvp => kvp.Value == arena.currentGameMode);

                    // 获取列表中的下一个模式，如果到达末尾则回到第一个模式
                    var nextModeIndex = (currentModeIndex + 1) % gameModesList.Count;

                    // 更新当前游戏模式
                    arena.onlineArenaGameMode = gameModesList[nextModeIndex].Key;
                    arena.currentGameMode = gameModesList[nextModeIndex].Value;
                }

                if (message == "PREVONLINEGAME")
                {
                    // 上一个在线游戏信号（仅限房主）
                    var gameModesList = arena.registeredGameModes.ToList(); // 获取已注册游戏模式列表

                    // 查找当前游戏模式的索引
                    int currentModeIndex = gameModesList.FindIndex(kvp => kvp.Value == arena.currentGameMode);

                    // 处理位于列表开头的情况
                    if (currentModeIndex > 0)
                    {
                        // 获取列表中的上一个模式
                        int prevModeIndex = currentModeIndex - 1;
                        arena.onlineArenaGameMode = gameModesList[prevModeIndex].Key;
                        arena.currentGameMode = gameModesList[prevModeIndex].Value;
                    }
                    else
                    {
                        // 处理已经位于开头的情况
                        // 你可能想在这里绕回到最后一个模式
                        arena.onlineArenaGameMode = gameModesList[gameModesList.Count - 1].Key;
                        arena.currentGameMode = gameModesList[gameModesList.Count - 1].Value;
                    }
                }
            }
        }

        // 玩家列表接收事件处理
        private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
        {
            if (RainMeadow.isArenaMode(out var _))
            {
                RainMeadow.Debug(players); // 调试输出玩家信息
                if (usernameButtons != null)
                {
                    // 移除现有的用户名按钮
                    for (int i = usernameButtons.Length - 1; i >= 0; i--)
                    {
                        if (usernameButtons[i] != null)
                        {
                            var playerbtn = usernameButtons[i];
                            playerbtn.RemoveSprites(); // 移除精灵
                            this.pages[0].RemoveSubObject(playerbtn); // 从页面中移除
                            meUsernameButtonCreated = false; // 重置创建标志
                        }
                    }
                }

                if (classButtons != null)
                {
                    // 移除现有的职业按钮
                    for (int i = classButtons.Length - 1; i >= 0; i--)
                    {
                        if (classButtons[i] != null)
                        {
                            if (OnlineManager.lobby.isOwner) // 检查踢出按钮是否为空
                            {
                                if (classButtons[i].kickButton != null)
                                {
                                    classButtons[i].kickButton.RemoveSprites(); // 移除踢出按钮精灵
                                    this.pages[0].RemoveSubObject(classButtons[i].kickButton); // 从页面中移除踢出按钮
                                }
                            }
                            classButtons[i].RemoveSprites(); // 移除精灵
                            this.pages[0].RemoveSubObject(classButtons[i]); // 从页面中移除
                            meClassButtonCreated = false; // 重置创建标志
                        }
                    }
                }

                // 清理已离开玩家的准备状态
                List<MeadowPlayerId> keysToRemove = new List<MeadowPlayerId>();
                for (int i = 0; i < arena.playersReadiedUp.list.Count; i++)
                {
                    if (!OnlineManager.players.Any(player => player.id.Equals(arena.playersReadiedUp.list[i])))
                    {
                        RainMeadow.Debug($"Removing player: {arena.playersReadiedUp.list[i]} who left from readyUpDictionary");
                        keysToRemove.Add(arena.playersReadiedUp.list[i]);
                    }
                }

                // 从准备列表中移除已离开的玩家
                for (int j = 0; j < keysToRemove.Count; j++)
                {
                    arena.playersReadiedUp.list.Remove(keysToRemove[j]);
                }

                // 处理每个在线玩家
                foreach (var player in OnlineManager.players)
                {
                    if (arena.playersInLobbyChoosingSlugs.TryGetValue(player.id.ToString(), out var existingValue))
                    {
                        RainMeadow.Debug("Player already exists in slug dictionary"); // 玩家已存在于蛞蝓猫选择字典中
                    }
                    else
                    {
                        // 键不存在，如果需要可以添加
                        if (!OnlineManager.lobby.isOwner)
                        {
                            OnlineManager.lobby.owner.InvokeOnceRPC(ArenaRPCs.Arena_NotifyClassChange, player, 0); // 默认选择第一个蛞蝓猫
                        }
                    }

                    if (arena.playersReadiedUp.list.Contains(player.id))
                    {
                        RainMeadow.Debug($"Player {player.id.name} is readied up"); // 玩家已准备好
                    }
                }
                
                // 重新创建UI元素
                AddUsernames(); // 添加用户名
                AddClassButtons(); // 添加职业按钮
                HandleLobbyProfileOverflow(); // 处理大厅个人资料溢出
                
                if (OnlineManager.lobby.isOwner)
                {
                    arena.ResetForceReadyCountDownShort(); // 重置强制准备倒计时（短）
                }

                if (this != null)
                {
                    ArenaHelpers.ResetReadyUpLogic(arena, this); // 重置准备逻辑
                }
            }
        }

        // 获取个人颜色
        private List<Color> GetPersonalColors(SlugcatStats.Name id)
        {
            // 如果启用了自定义颜色，使用HSL颜色转换为RGB；否则使用默认的身体部分颜色
            return [.. this.IsCustomColorEnabled(id) ? this.GetMenuHSLs(id).Select(ColorHelpers.HSL2RGB) : PlayerGraphics.DefaultBodyPartColorHex(id).Select(Custom.hexToColor)];
        }

        // 添加职业按钮
        private void AddClassButtons()
        {
            classButtons = new ArenaOnlinePlayerJoinButton[OnlineManager.players.Count]; // 初始化职业按钮数组
            bool foundMe = false;
            // 检查是否找到当前玩家
            for (int i = 0; i < OnlineManager.players.Count; i++)
            {
                if (OnlineManager.players[i].isMe)
                {
                    foundMe = true;
                    break;
                }
            }
            if (!meClassButtonCreated && foundMe)
            {
                // 为当前玩家创建职业按钮
                classButtons[0] = new ArenaOnlinePlayerJoinButton(this, pages[0], new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f), 40f), 0);
                classButtons[0].buttonBehav.greyedOut = false;
                classButtons[0].readyForCombat = true;
                classButtons[0].profileIdentifier = OnlineManager.mePlayer;
                int currentColorIndex;
                // 检查玩家是否已选择角色
                if (arena.playersInLobbyChoosingSlugs.TryGetValue(OnlineManager.mePlayer.id.ToString(), out var existingValue))
                {
                    // 玩家已存在于字典中，获取当前索引
                    currentColorIndex = arena.playersInLobbyChoosingSlugs[OnlineManager.mePlayer.id.ToString()];
                    RainMeadow.Debug("Player already exists in dictionary");
                    RainMeadow.Debug("Current index" + currentColorIndex);
                    classButtons[0].portrait.fileName = ArenaImage(allSlugs[currentColorIndex], currentColorIndex); // 设置角色肖像
                    classButtons[0].portrait.LoadFile(); // 加载文件
                    classButtons[0].portrait.sprite.SetElementByName(classButtons[0].portrait.fileName); // 设置精灵元素
                }
                else
                {
                    // 玩家不存在于字典中，设置默认值
                    RainMeadow.Debug("Player did NOT exist in dictionary");
                    currentColorIndex = 0;
                    if (!OnlineManager.lobby.isOwner)
                    {
                        // 如果不是房主，通知房主角色变更
                        OnlineManager.lobby.owner.InvokeOnceRPC(ArenaRPCs.Arena_NotifyClassChange, OnlineManager.mePlayer, currentColorIndex);
                    }
                    else
                    {
                        // 如果是房主，直接更新字典
                        arena.playersInLobbyChoosingSlugs[OnlineManager.mePlayer.id.ToString()] = currentColorIndex;
                    }
                }

                // 设置职业按钮点击事件
                classButtons[0].OnClick += (_) =>
                {
                    // 切换到下一个角色
                    currentColorIndex = (currentColorIndex + 1) % allSlugs.Count;
                    allSlugs[currentColorIndex] = allSlugs[currentColorIndex];
                    classButtons[0].portrait.fileName = ArenaImage(allSlugs[currentColorIndex], currentColorIndex); // 更新角色肖像
                    classButtons[0].portrait.LoadFile(); // 重新加载文件
                    classButtons[0].portrait.sprite.SetElementByName(classButtons[0].portrait.fileName); // 更新精灵元素
                    PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); // 播放按钮音效

                    // 更新角色设置
                    arena.avatarSettings.playingAs = allSlugs[currentColorIndex];
                    arena.arenaClientSettings.playingAs = arena.avatarSettings.playingAs;

                    // 通知其他玩家角色变更
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeRPC(ArenaRPCs.Arena_NotifyClassChange, OnlineManager.mePlayer, currentColorIndex);
                        }
                    }
                    if (OnlineManager.lobby.isOwner)
                    {
                        // 如果是房主，更新字典
                        arena.playersInLobbyChoosingSlugs[OnlineManager.mePlayer.id.ToString()] = currentColorIndex;
                    }
                };
                pages[0].subObjects.Add(classButtons[0]); // 将按钮添加到页面
                arena.avatarSettings.playingAs = allSlugs[currentColorIndex]; // 设置当前角色
                arena.arenaClientSettings.playingAs = arena.avatarSettings.playingAs; // 更新客户端设置
                meClassButtonCreated = true; // 标记当前玩家的职业按钮已创建
            }
            for (int i = 0; i < OnlineManager.players.Count; i++) // 跳过房主
            {
                int localIndex = i;
                // 不能为零。
                // 必须考虑玩家0
                // 不能超过玩家数量

                if (MatchmakingManager.currentDomain == MatchmakingManager.MatchMakingDomain.Steam && i == 0)
                {
                    continue; // 我们已经处理了[0]
                }

                if (MatchmakingManager.currentDomain == MatchmakingManager.MatchMakingDomain.LAN) // 处理当前玩家位置
                {
                    if (OnlineManager.players[i] == OnlineManager.lobby.owner)
                    {
                        localIndex = OnlineManager.players.IndexOf(OnlineManager.mePlayer);
                    }

                    if (OnlineManager.players[i].isMe)
                    {
                        continue; // 我们已经在[0]处理了当前玩家
                    }
                }

                if (i > holdPlayerPosition)
                {
                    break; // 超出保留的玩家位置范围
                }

                // 创建其他玩家的职业按钮
                classButtons[localIndex] = new ArenaOnlinePlayerJoinButton(this, pages[0], new Vector2(600f + localIndex * num3, 500f) + new Vector2(106f, -20f) + new Vector2((num - 120f) / 2f, 0f) - new Vector2((num3 - 120f) * classButtons.Length, 40f), localIndex);
                classButtons[localIndex].portraitBlack = Custom.LerpAndTick(classButtons[localIndex].portraitBlack, 1f, 0.06f, 0.05f); // 设置肖像黑色过渡
                classButtons[localIndex].profileIdentifier = (localIndex == OnlineManager.players.IndexOf(OnlineManager.mePlayer) ? OnlineManager.lobby.owner : OnlineManager.players[localIndex]); // 设置个人资料标识符
                classButtons[localIndex].readyForCombat = arena.playersReadiedUp.list.Contains(classButtons[localIndex].profileIdentifier.id); // 设置是否准备好战斗
                classButtons[localIndex].buttonBehav.greyedOut =
                    (arena != null && arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(classButtons[localIndex].profileIdentifier.id))
                    ? false // 让冠军轮廓边框发光
                    : true;

                // 获取玩家选择的角色
                if (!arena.playersInLobbyChoosingSlugs.TryGetValue(OnlineManager.players[i].id.ToString(), out var currentColorIndexOther))
                {
                    currentColorIndexOther = 0; // 默认为第一个角色
                }
                else
                {
                    currentColorIndexOther = arena.playersInLobbyChoosingSlugs[OnlineManager.players[i].id.ToString()]; // 获取已选择的角色
                }
                // 设置角色肖像
                classButtons[localIndex].portrait.fileName = ArenaImage(allSlugs[currentColorIndexOther], currentColorIndexOther);
                classButtons[localIndex].portrait.LoadFile();
                classButtons[localIndex].portrait.sprite.SetElementByName(classButtons[localIndex].portrait.fileName);
                pages[0].subObjects.Add(classButtons[localIndex]); // 将按钮添加到页面

                if (OnlineManager.lobby.isOwner)
                {
                    // 如果是房主，添加踢出按钮
                    classButtons[localIndex].kickButton = new SimplerSymbolButton(this, this.pages[0], "Menu_Symbol_Clear_All", "KICKPLAYER", new Vector2(classButtons[localIndex].pos.x + 40f, classButtons[localIndex].pos.y + 110f));
                    classButtons[localIndex].kickButton.OnClick += (_) =>
                    {
                        RainMeadow.Debug($"Kicked User: {classButtons[localIndex].profileIdentifier}");
                        BanHammer.BanUser(classButtons[localIndex].profileIdentifier); // 封禁用户
                    };
                    this.pages[0].subObjects.Add(classButtons[localIndex].kickButton); // 将踢出按钮添加到页面
                }
            }
        }

        // 添加用户名按钮
        private void AddUsernames()
        {
            usernameButtons = new SimplerButton[OnlineManager.players.Count]; // 初始化用户名按钮数组
            bool foundMe = false;
            // 检查是否找到当前玩家
            for (int i = 0; i < OnlineManager.players.Count; i++)
            {
                if (OnlineManager.players[i].isMe)
                {
                    foundMe = true;
                    break;
                }
            }
            if (!meUsernameButtonCreated && foundMe)
            {
                // 将"isMe"玩家分配给索引0
                usernameButtons[0] = new SimplerButton(this, pages[0], OnlineManager.mePlayer.id.name, new Vector2(600f + 0 * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * usernameButtons.Length, 40f), new Vector2(num - 20f, 30f));
                usernameButtons[0].OnClick += (_) =>
                {
                    OnlineManager.mePlayer.id.OpenProfileLink(); // 打开"isMe"玩家的个人资料链接
                };
                usernameButtons[0].buttonBehav.greyedOut = false; // 启用按钮
                pages[0].subObjects.Add(usernameButtons[0]); // 将按钮添加到页面
                meUsernameButtonCreated = true; // 标记当前玩家的用户名按钮已创建
            }

            for (int i = 0; i < OnlineManager.players.Count; i++)
            {
                int buttonIndex = i;
                if (MatchmakingManager.currentDomain == MatchmakingManager.MatchMakingDomain.Steam && i == 0)
                {
                    continue; // 我们已经处理了[0]
                }

                if (MatchmakingManager.currentDomain == MatchmakingManager.MatchMakingDomain.LAN) // 处理当前玩家位置
                {
                    if (OnlineManager.players[i] == OnlineManager.lobby.owner)
                    {
                        buttonIndex = OnlineManager.players.IndexOf(OnlineManager.mePlayer);
                    }

                    if (OnlineManager.players[i].isMe)
                    {
                        continue; // 我们已经在[0]处理了当前玩家
                    }
                }
                if (i > holdPlayerPosition)
                {
                    break; // 超出保留的玩家位置范围
                }
                // 从索引1开始放置玩家
                // 使用buttonIndex将非"isMe"玩家分配给下一个可用索引
                // 为其他玩家创建按钮
                var player = OnlineManager.players[i];

                usernameButtons[buttonIndex] = new SimplerButton(this, pages[0], player.id.name, new Vector2(600f + buttonIndex * num3, 500f) + new Vector2(106f, -60f) - new Vector2((num3 - 120f) * usernameButtons.Length, 40f), new Vector2(num - 20f, 30f));
                usernameButtons[buttonIndex].OnClick += (_) => ArenaHelpers.FindOnlinePlayerByStringUsername(usernameButtons[buttonIndex].menuLabel.text).id.OpenProfileLink(); // 打开其他玩家的个人资料链接

                usernameButtons[buttonIndex].buttonBehav.greyedOut = false; // 启用按钮
                pages[0].subObjects.Add(usernameButtons[buttonIndex]); // 将按钮添加到页面
            }
        }

        // 更新准备状态标签
        private void UpdateReadyUpLabel()
        {
            this.totalClientsReadiedUpOnPage.text = this.Translate("Ready:") + " " + arena.playersReadiedUp.list.Count + "/" + OnlineManager.players.Count;
        }

        // 更新关卡计数器
        private void UpdateLevelCounter()
        {
            this.currentLevelProgression.text = this.Translate("Playlist Progress:") + " " + arena.currentLevel + "/" + arena.totalLevelCount;
        }
        
        // 更新游戏模式标签
        private void UpdateGameModeLabel()
        {
            this.displayCurrentGameMode.text = Translate("Current Mode:") + " " + Utils.Translate(arena.currentGameMode);
        }

        // 处理大厅个人资料溢出
        private void HandleLobbyProfileOverflow()
        {
            // 我等不及这次大修了
            if (viewNextPlayer != null)
            {
                viewNextPlayer.RemoveSprites(); // 移除下一个玩家按钮的精灵
                pages[0].RemoveSubObject(viewNextPlayer); // 从页面中移除按钮
            }

            if (viewPrevPlayer != null)
            {
                viewPrevPlayer.RemoveSprites(); // 移除上一个玩家按钮的精灵
                pages[0].RemoveSubObject(viewPrevPlayer); // 从页面中移除按钮
            }

            if (OnlineManager.players.Count <= 4)
            {
                return; // 如果玩家数量不超过4个，不需要处理溢出
            }

            currentPlayerPosition = holdPlayerPosition; // 初始化当前玩家位置

            // 创建查看下一个玩家的按钮
            viewNextPlayer = new SimplerSymbolButton(this, pages[0], "Menu_Symbol_Arrow", "VIEWNEXT", new Vector2(classButtons[holdPlayerPosition].pos.x + 120f, classButtons[holdPlayerPosition].pos.y + 60));
            viewNextPlayer.symbolSprite.rotation = 90; // 设置箭头旋转方向（向下）
            if (currentPlayerPosition == OnlineManager.players.Count - 1)
            {
                viewNextPlayer.buttonBehav.greyedOut = true; // 如果已经是最后一个玩家，禁用按钮
            }

            // 设置查看下一个玩家按钮的点击事件
            viewNextPlayer.OnClick += (_) =>
            {
                currentPlayerPosition++; // 增加当前玩家位置
                if (viewPrevPlayer != null && viewPrevPlayer.buttonBehav.greyedOut)
                {
                    viewPrevPlayer.buttonBehav.greyedOut = false; // 启用查看上一个玩家按钮
                }

                usernameButtons[holdPlayerPosition].menuLabel.text = OnlineManager.players[currentPlayerPosition].id.name; // 当前变为下一个

                int localIndex = currentPlayerPosition;
                if (OnlineManager.lobby.isOwner)
                {
                    // 如果是房主，重置踢出按钮的订阅
                    classButtons[holdPlayerPosition].kickButton.ResetSubscriptions();
                    classButtons[holdPlayerPosition].kickButton.OnClick += (_) =>
                    {
                        RainMeadow.Debug($"Kicking player {OnlineManager.players[localIndex]} at index {localIndex}");
                        BanHammer.BanUser(OnlineManager.players[localIndex]); // 封禁用户
                    };
                }
                // 获取玩家选择的角色
                if (!arena.playersInLobbyChoosingSlugs.TryGetValue(OnlineManager.players[currentPlayerPosition].id.ToString(), out var currentColorIndexOther))
                {
                    currentColorIndexOther = 0; // 默认为第一个角色
                }
                // 更新职业按钮信息
                classButtons[holdPlayerPosition].profileIdentifier = OnlineManager.players[currentPlayerPosition];
                classButtons[holdPlayerPosition].portrait.fileName = ArenaImage(allSlugs[currentColorIndexOther], currentColorIndexOther);
                classButtons[holdPlayerPosition].portrait.LoadFile();
                classButtons[holdPlayerPosition].portrait.sprite.SetElementByName(classButtons[holdPlayerPosition].portrait.fileName);
                usernameButtons[holdPlayerPosition].menuLabel.text = OnlineManager.players[currentPlayerPosition].id.name;

                try
                {
                    // 更新准备状态
                    classButtons[holdPlayerPosition].readyForCombat = arena.playersReadiedUp.list.Contains(OnlineManager.players[currentPlayerPosition].id);
                }
                catch
                {
                    classButtons[holdPlayerPosition].readyForCombat = false;
                }

                if (currentPlayerPosition + 1 >= OnlineManager.players.Count)
                {
                    // 如果到达扩展列表的末尾，禁用下一个按钮
                    RainMeadow.Debug("End of extended list: " + currentPlayerPosition);
                    viewNextPlayer.buttonBehav.greyedOut = true;
                    return;
                }
                else
                {
                    return;
                }
            };

            // 创建查看上一个玩家的按钮
            viewPrevPlayer = new SimplerSymbolButton(this, pages[0], "Menu_Symbol_Arrow", "VIEWPREV", new Vector2(classButtons[holdPlayerPosition].pos.x + 120f, classButtons[holdPlayerPosition].pos.y + 20));
            viewPrevPlayer.symbolSprite.rotation = 270; // 设置箭头旋转方向（向上）
            if (currentPlayerPosition <= holdPlayerPosition)
            {
                viewPrevPlayer.buttonBehav.greyedOut = true; // 如果已经是第一个玩家，禁用按钮
            }
            
            // 设置查看上一个玩家按钮的点击事件
            viewPrevPlayer.OnClick += (_) =>
            {
                currentPlayerPosition--; // 减少当前玩家位置
                if (viewNextPlayer != null && viewNextPlayer.buttonBehav.greyedOut)
                {
                    viewNextPlayer.buttonBehav.greyedOut = false; // 启用查看下一个玩家按钮
                }

                usernameButtons[holdPlayerPosition].menuLabel.text = OnlineManager.players[currentPlayerPosition].id.name; // 当前变为上一个

                int localIndex = currentPlayerPosition;
                if (OnlineManager.lobby.isOwner)
                {
                    // 如果是房主，重置踢出按钮的订阅
                    classButtons[holdPlayerPosition].kickButton.ResetSubscriptions();
                    classButtons[holdPlayerPosition].kickButton.OnClick += (_) =>
                    {
                        RainMeadow.Debug($"Kicking player {OnlineManager.players[localIndex]} at index {localIndex}");
                        BanHammer.BanUser(OnlineManager.players[localIndex]); // 封禁用户
                    };
                }
                // 获取玩家选择的角色
                if (!arena.playersInLobbyChoosingSlugs.TryGetValue(OnlineManager.players[currentPlayerPosition].id.ToString(), out var currentColorIndexOther))
                {
                    currentColorIndexOther = 0; // 默认为第一个角色
                }
                // 更新职业按钮信息
                classButtons[holdPlayerPosition].profileIdentifier = OnlineManager.players[currentPlayerPosition];
                classButtons[holdPlayerPosition].portrait.fileName = ArenaImage(allSlugs[currentColorIndexOther], currentColorIndexOther);
                classButtons[holdPlayerPosition].portrait.LoadFile();
                classButtons[holdPlayerPosition].portrait.sprite.SetElementByName(classButtons[holdPlayerPosition].portrait.fileName);
                usernameButtons[holdPlayerPosition].menuLabel.text = OnlineManager.players[currentPlayerPosition].id.name;

                try
                {
                    // 更新准备状态
                    classButtons[holdPlayerPosition].readyForCombat = arena.playersReadiedUp.list.Contains(OnlineManager.players[currentPlayerPosition].id);
                }
                catch
                {
                    classButtons[holdPlayerPosition].readyForCombat = false;
                }

                if (currentPlayerPosition <= holdPlayerPosition)
                {
                    // 如果到达扩展列表的开头，禁用上一个按钮
                    RainMeadow.Debug("Beginning of extended list: " + currentPlayerPosition);
                    viewPrevPlayer.buttonBehav.greyedOut = true;
                    return;
                }
                else
                {
                    return;
                }
            };
            
            // 将按钮添加到页面
            this.pages[0].subObjects.Add(viewNextPlayer);
            this.pages[0].subObjects.Add(viewPrevPlayer);
        }

        // 添加强制准备按钮
        private void AddForceReadyUp()
        {
            // 定义强制准备按钮的点击事件
            Action<SimplerButton> forceReadyClick = (_) =>
            {
                for (int i = 0; i < OnlineManager.players.Count; i++)
                {
                    var player = OnlineManager.players[i];
                    if (player.isMe)
                    {
                        this.playButton.Clicked(); // 如果是当前玩家，直接点击开始按钮
                        continue;
                    }
                    if (!arena.playersReadiedUp.list.Contains(player.id))
                    {
                        player.InvokeOnceRPC(ArenaRPCs.Arena_ForceReadyUp); // 强制其他玩家准备
                    }
                }
                arena.ResetForceReadyCountDownShort(); // 重置强制准备倒计时（短）
            };
            
            // 创建强制准备按钮
            this.forceReady = CreateButton(this.Translate(forceReadyText), new Vector2(this.playButton.pos.x - 130f, this.playButton.pos.y), this.playButton.size, forceReadyClick);
        }
    }
}
