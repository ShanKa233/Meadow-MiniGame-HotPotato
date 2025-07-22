using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;
using Menu.Remix;
using System;


namespace RainMeadow.UI.Pages;

/// <summary>
/// 竞技场主大厅页面，用于显示游戏模式选择、玩家列表和聊天功能
/// </summary>
public class ArenaMainLobbyPage : PositionedMenuObject
{
    // 准备和开始按钮
    public SimplerButton readyButton;
    public SimplerButton? startButton;
    // 竞技场信息和统计按钮
    public SimplerSymbolButton arenaInfoButton, arenaGameStatsButton;
    // 显示当前游戏模式、准备玩家数量和播放列表进度的标签
    public MenuLabel activeGameModeLabel, readyPlayerCounterLabel, playlistProgressLabel;
    // 聊天和大厅状态分隔线
    public FSprite chatLobbyStateDivider;
    // 标签容器（用于切换不同功能页）
    public TabContainer tabContainer;
    // 关卡选择器
    public ArenaLevelSelector levelSelector;
    // 聊天菜单框
    public ChatMenuBox chatMenuBox;
    // 竞技场设置界面
    public OnlineArenaSettingsInferface arenaSettingsInterface;
    // 蛞蝓猫能力界面（可选）
    public OnlineSlugcatAbilitiesInterface? slugcatAbilitiesInterface;
    
    // 玩家显示器
    public PlayerDisplayer? playerDisplayer;
    // 蛞蝓猫对话框
    public Dialog? slugcatDialog;
    // 外部标签容器
    public TabContainer.Tab? externalTabContainer;

    // 对话框
    public Dialog? dialog;
    // 痛苦猫索引和按住蛞蝓猫按钮计数器
    public int painCatIndex, holdSlugcatBtnCounter;
    // 当前竞技场游戏模式的快捷访问
    private ArenaOnlineGameMode Arena => (ArenaOnlineGameMode)OnlineManager.lobby.gameMode;
    // 竞技场菜单的快捷访问
    public ArenaOnlineLobbyMenu? ArenaMenu => menu as ArenaOnlineLobbyMenu;

    /// <summary>
    /// 构造函数，初始化竞技场大厅页面
    /// </summary>
    public ArenaMainLobbyPage(Menu.Menu menu, MenuObject owner, Vector2 pos, string painCatName, int painCatIndex) : base(menu, owner, pos)
    {
        this.painCatIndex = painCatIndex;
        var scugslotsHint = UnityEngine.Random.Range(0, 21);

        // 初始化准备按钮
        readyButton = new SimplerButton(menu, this, Utils.Translate("READY?"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
        readyButton.OnClick += btn =>
        {
            if (!RainMeadow.isArenaMode(out var _)) return;
            Arena.arenaClientSettings.ready = !Arena.arenaClientSettings.ready;
        };
        // 初始化游戏统计按钮
        arenaGameStatsButton = new(menu, this, "Multiplayer_Bones", "", new(readyButton.pos.x + readyButton.size.x + 10, readyButton.pos.y))
        {
            size = new(30, 30)
        };
        arenaGameStatsButton.roundedRect.size = arenaGameStatsButton.size;
        arenaGameStatsButton.OnClick += _ => OpenGameStatsDialog();
        readyButton.description = Utils.Translate(scugslotsHint == 20 ? SlugcatSelector.slugcatSelectorHints[UnityEngine.Random.Range(0, SlugcatSelector.slugcatSelectorHints.Count)]: "Ready up to join the host when the match begins");
        
        // 初始化聊天菜单框
        chatMenuBox = new(menu, this, new(100f, 125f), new(300, 425));
        chatMenuBox.roundedRect.size.y = 475f;

        // 设置标签位置
        float chatRectPosSizeY = chatMenuBox.pos.y + chatMenuBox.roundedRect.size.y;
        activeGameModeLabel = new MenuLabel(menu, this, "", new Vector2(chatMenuBox.pos.x + chatMenuBox.size.x / 2, chatRectPosSizeY - 15), Vector2.zero, false);
        readyPlayerCounterLabel = new MenuLabel(menu, this, "", new Vector2(chatMenuBox.pos.x + chatMenuBox.size.x / 4, chatRectPosSizeY - 35), Vector2.zero, false);
        playlistProgressLabel = new MenuLabel(menu, this, "", new Vector2(chatMenuBox.pos.x + chatMenuBox.size.x / 4 * 3 - 20, chatRectPosSizeY - 35), Vector2.zero, false);

        // 初始化聊天和大厅状态分隔线
        chatLobbyStateDivider = new FSprite("pixel")
        {
            color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey),
            scaleX = chatMenuBox.size.x - 40,
            scaleY = 2,
        };
        Container.AddChild(chatLobbyStateDivider);

        // 构建玩家显示
        BuildPlayerDisplay();
        // 注册玩家列表接收事件
        MatchmakingManager.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;
        // 初始化竞技场信息按钮
        arenaInfoButton = new(menu, this, "Meadow_Menu_SmallQuestionMark", "", new Vector2(chatMenuBox.pos.x + chatMenuBox.size.x / 2 - 12, playerDisplayer!.pos.y + playerDisplayer.scrollUpButton!.pos.y), "");
        arenaInfoButton.OnClick += _ => OpenInfoDialog();

        // 初始化标签容器
        tabContainer = new TabContainer(menu, this, new Vector2(470f, 125f), new Vector2(450, 475));
        TabContainer.Tab playListTab = tabContainer.AddTab(menu.Translate("Arena Playlist")),
            matchSettingsTab = tabContainer.AddTab(menu.Translate("Match Settings"));

        // 添加关卡选择器到播放列表标签
        playListTab.AddObjects(levelSelector = new ArenaLevelSelector(menu, playListTab, new Vector2(65f, 7.5f)));

        // 如果有MSC或Watcher模组，添加蛞蝓猫能力标签
        if (ModManager.MSC || ModManager.Watcher)
        {
            TabContainer.Tab slugabilitiesTab = tabContainer.AddTab(menu.Translate("Slugcat Abilities"));
            slugcatAbilitiesInterface = new OnlineSlugcatAbilitiesInterface(menu, slugabilitiesTab, new Vector2(360f, 380f), new Vector2(0f, 50f), menu.Translate(painCatName));
            slugcatAbilitiesInterface.CallForSync();
            slugabilitiesTab.AddObjects(slugcatAbilitiesInterface);
        }
        
        // 初始化竞技场设置界面
        arenaSettingsInterface = new OnlineArenaSettingsInferface(menu, matchSettingsTab, new Vector2(120f, 0f), Arena.currentGameMode, [.. Arena.registeredGameModes.Keys.Select(v => new ListItem(v, menu.Translate(v)))]);
        arenaSettingsInterface.CallForSync();
        matchSettingsTab.AddObjects(arenaSettingsInterface);

        // 添加所有子对象
        this.SafeAddSubobjects(readyButton, tabContainer, activeGameModeLabel, readyPlayerCounterLabel, playlistProgressLabel, chatMenuBox, arenaInfoButton, arenaGameStatsButton);
    }

    /// <summary>
    /// 构建玩家显示界面
    /// </summary>
    public void BuildPlayerDisplay()
    {
        playerDisplayer = new PlayerDisplayer(menu, this, new Vector2(960f, 130f), [.. OnlineManager.players.OrderByDescending(x => x.isMe)], GetPlayerButton, 4, ArenaPlayerBox.DefaultSize.x, new(ArenaPlayerBox.DefaultSize.y, 0), new(ArenaPlayerSmallBox.DefaultSize.y, 10));
        subObjects.Add(playerDisplayer);
        playerDisplayer.CallForRefresh();
    }

    /// <summary>
    /// 当接收到在线玩家列表时更新玩家显示
    /// </summary>
    public void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
    {
        RainMeadow.DebugMe();
        playerDisplayer?.UpdatePlayerList([.. OnlineManager.players.OrderByDescending(x => x.isMe)]);
    }
    
    /// <summary>
    /// 获取玩家按钮（大或小显示）
    /// </summary>
    public ButtonScroller.IPartOfButtonScroller GetPlayerButton(PlayerDisplayer playerDisplay, bool isLargeDisplay, OnlinePlayer player, Vector2 pos)
    {
        if (isLargeDisplay)
        {
            // 创建大型玩家框
            ArenaPlayerBox playerBox = new(menu, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos); //buttons init prevents kick button if isMe
            playerBox.slugcatButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
            return playerBox;
        }
        // 创建小型玩家框
        ArenaPlayerSmallBox playerSmallBox = new(menu, playerDisplay, player, OnlineManager.lobby?.isOwner == true, pos);
        playerSmallBox.playerButton.TryBind(playerDisplay.scrollSlider, true, false, false, false);
        return playerSmallBox;
    }
    
    /// <summary>
    /// 打开游戏模式信息对话框
    /// </summary>
    public void OpenInfoDialog()
    {
        if (!RainMeadow.isArenaMode(out _)) return;
        menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        dialog = Arena.externalArenaGameMode?.AddGameModeInfo(Arena, menu);
        menu.manager.ShowDialog(dialog);
    }
    
    /// <summary>
    /// 打开游戏统计对话框
    /// </summary>
    public void OpenGameStatsDialog()
    {
        if (!RainMeadow.isArenaMode(out _)) return;
        menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        dialog = Arena.externalArenaGameMode?.AddPostGameStatsFeed(Arena, menu);
        menu.manager.ShowDialog(dialog);
    }
    
    /// <summary>
    /// 打开蛞蝓猫颜色配置对话框
    /// </summary>
    public void OpenColorConfig(SlugcatStats.Name? slugcat)
    {
        if (!ModManager.MMF)
        {
            menu.PlaySound(SoundID.MENU_Checkbox_Uncheck);
            dialog = new DialogNotify(menu.LongTranslate("You cant color without Remix on!"), new Vector2(500f, 200f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
            menu.manager.ShowDialog(dialog);
            return;
        }

        menu.PlaySound(SoundID.MENU_Checkbox_Check);
        dialog = new ColorMultipleSlugcatsDialog(menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); }, ArenaHelpers.allSlugcats, slugcat);
        menu.manager.ShowDialog(dialog);
    }
    
    /// <summary>
    /// 保存界面选项设置
    /// </summary>
    public void SaveInterfaceOptions()
    {
        // 保存竞技场设置
        RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value = arenaSettingsInterface.countdownTimerTextBox.valueInt;
        RainMeadow.rainMeadowOptions.ArenaItemSteal.Value = arenaSettingsInterface.stealItemCheckBox.Checked;
        RainMeadow.rainMeadowOptions.ArenaAllowMidJoin.Value = arenaSettingsInterface.allowMidGameJoinCheckbox.Checked;
        
        // 如果有蛞蝓猫能力界面，保存相关设置
        if (slugcatAbilitiesInterface != null)
        {
            if (ModManager.MSC)
            {
                // 保存MSC模组相关设置
                RainMeadow.rainMeadowOptions.BlockMaul.Value = slugcatAbilitiesInterface.blockMaulCheckBox.Checked;
                RainMeadow.rainMeadowOptions.BlockArtiStun.Value = slugcatAbilitiesInterface.blockArtiStunCheckBox.Checked;
                RainMeadow.rainMeadowOptions.ArenaSAINOT.Value = slugcatAbilitiesInterface.sainotCheckBox.Checked;
                RainMeadow.rainMeadowOptions.PainCatEgg.Value = slugcatAbilitiesInterface.painCatEggCheckBox.Checked;
                RainMeadow.rainMeadowOptions.PainCatThrows.Value = slugcatAbilitiesInterface.painCatThrowsCheckBox.Checked;
                RainMeadow.rainMeadowOptions.PainCatLizard.Value = slugcatAbilitiesInterface.painCatLizardCheckBox.Checked;
                RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value = slugcatAbilitiesInterface.saintAscendDurationTimerTextBox.valueInt;
            }
            if (ModManager.Watcher)
            {
                // 保存Watcher模组相关设置
                RainMeadow.rainMeadowOptions.ArenaWatcherCamoTimer.Value = slugcatAbilitiesInterface.watcherCamoLimitLabelTextBox.valueInt;
            }
        }
    }
    
    /// <summary>
    /// 更新玩家按钮状态
    /// </summary>
    public void UpdatePlayerButtons(ButtonScroller.IPartOfButtonScroller button)
    {
        if (button is ArenaPlayerBox playerBox)
        {
            // 获取玩家客户端设置
            ArenaClientSettings? clientSettings = ArenaHelpers.GetArenaClientSettings(playerBox.profileIdentifier);
            bool slugSlots = clientSettings?.gotSlugcat == true;

            // 根据玩家选择的角色更新显示
            if (ModManager.MSC && clientSettings?.playingAs == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                if (playerBox.profileIdentifier.isMe)
                    playerBox.slugcatButton.LoadNewSlugcat(clientSettings?.playingAs, painCatIndex, false);

                else if (playerBox.slugcatButton.slugcat != clientSettings?.playingAs)
                    playerBox.slugcatButton.LoadNewSlugcat(clientSettings?.playingAs, UnityEngine.Random.Range(0, 5), false);
            }
            else playerBox.slugcatButton.LoadNewSlugcat(clientSettings?.playingAs, clientSettings != null && clientSettings.slugcatColor != Color.black, false);

            // 更新玩家状态文本
            playerBox.ToggleTextOverlay("Got All<LINE>ScugSlots!!", slugSlots);
            if (clientSettings?.ready == true) playerBox.ToggleTextOverlay(Arena.isInGame && Arena.allowJoiningMidRound ? "Joining<LINE>soon!" : "Ready!", true);
            if (clientSettings?.selectingSlugcat == true) playerBox.ToggleTextOverlay("Selecting<LINE>Slugcat", true);
            if (Arena.arenaSittingOnlineOrder.Contains(playerBox.profileIdentifier.inLobbyId) && Arena.isInGame) playerBox.ToggleTextOverlay("In Game!", true);

            // 设置角色颜色
            Color color = playerBox.slugcatButton.isColored && clientSettings != null ? clientSettings.slugcatColor : Color.white;
            if (Arena.externalArenaGameMode != null) color = Arena.externalArenaGameMode.GetPortraitColor(Arena, playerBox.profileIdentifier, color);
            playerBox.slugcatButton.portraitColor = color;

            // 设置彩虹效果
            playerBox.showRainbow = Arena.externalArenaGameMode?.DidPlayerWinRainbow(Arena, playerBox.profileIdentifier) == true || slugSlots;
        }
        // 更新小型玩家框
        if (button is ArenaPlayerSmallBox smallPlayerBox)
            smallPlayerBox.slugcatButton.slug = ArenaHelpers.GetArenaClientSettings(smallPlayerBox.profileIdentifier)?.playingAs;
    }
    
    /// <summary>
    /// 更新比赛按钮状态
    /// </summary>
    public void UpdateMatchButtons()
    {
        // 更新准备按钮状态
        readyButton.buttonBehav.greyedOut = (!Arena.allowJoiningMidRound && Arena.arenaClientSettings.ready) || (OnlineManager.lobby.isOwner && Arena.initiateLobbyCountdown);
        readyButton.menuLabel.text = menu.Translate(Arena.arenaClientSettings.ready ? !Arena.allowJoiningMidRound ? "WAITING" : "UNREADY" : "READY?");

        if (startButton == null) return;

        // 更新开始按钮状态
        startButton.buttonBehav.greyedOut = !Arena.arenaClientSettings.ready || levelSelector.SelectedPlayList.Count == 0 || Arena.initiateLobbyCountdown;
        startButton.menuLabel.text = Arena.initiateLobbyCountdown ? menu.Translate(Arena.lobbyCountDown.ToString()) : menu.Translate("START MATCH!");
        startButton.signalText = "START_MATCH";
    }
    
    /// <summary>
    /// 处理菜单信号
    /// </summary>
    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        // 处理角色更换信号
        if (message == "CHANGE_SLUGCAT")
            ArenaMenu?.GoToChangeCharacter();
        // 处理角色颜色配置信号
        if (message == "COLOR_SLUGCAT")
        {
            SlugcatStats.Name? slug = sender?.owner is ArenaPlayerBox playerBox ? playerBox.slugcatButton.slugcat : sender?.owner is ArenaPlayerSmallBox smallPlayerBox ? smallPlayerBox.slugcatButton.slug : null;
            OpenColorConfig(slug);
        }
        // 处理开始比赛信号
        if (message == "START_MATCH")
            ArenaMenu?.StartGame();
    }
    
    /// <summary>
    /// 更新方法，每帧调用
    /// </summary>
    public override void Update()
    {
        base.Update();
        // 处理长按蛞蝓猫按钮逻辑
        if (menu.holdButton && menu.lastHoldButton && menu.selectedObject != null)
        {
            if (menu.selectedObject.Selected && ((menu.selectedObject is SimpleButton btn && btn.signalText == "CHANGE_SLUGCAT") || (menu.selectedObject is SlugcatColorableButton col && col.signalText == "CHANGE_SLUGCAT")))
                holdSlugcatBtnCounter = Mathf.Max(holdSlugcatBtnCounter, 0);
            else holdSlugcatBtnCounter = -1;
        }
        else holdSlugcatBtnCounter = -1;
        if (holdSlugcatBtnCounter >= 0) holdSlugcatBtnCounter++;
        if (holdSlugcatBtnCounter >= 40)
        {
            ArenaMenu?.GoToSlugcatSelector();
            holdSlugcatBtnCounter = -1;
        }

        if (!RainMeadow.isArenaMode(out _)) return;

        // 更新玩家颜色
        ChatLogManager.UpdatePlayerColors();
        // 更新玩家按钮
        if (playerDisplayer != null)
        {
            foreach (ButtonScroller.IPartOfButtonScroller button in playerDisplayer.buttons)
                UpdatePlayerButtons(button);
        }

        // 更新标签文本
        activeGameModeLabel.text = LabelTest.TrimText($"{menu.Translate("Current Mode:")} {menu.Translate(Arena./currentGameMode)}", chatMenuBox.size.x - 10, true);
        readyPlayerCounterLabel.text = $"{menu.Translate("Ready:")} {ArenaHelpers.GetReadiedPlayerCount(OnlineManager.players)}/{OnlineManager.players.Count}";
        int amtOfRooms = ArenaMenu?.GetGameTypeSetup?.playList != null ? ArenaMenu.GetGameTypeSetup.playList.Count : 0,
            amtOfRoomsRepeat = arenaSettingsInterface?.roomRepeatArray != null ? arenaSettingsInterface.roomRepeatArray.CheckedButton + 1 : 0;
        playlistProgressLabel.text = $"{menu.Translate("Playlist Progress:")} {Arena.currentLevel}/{(Arena.isInGame ? Arena.totalLevelCount : (amtOfRooms * amtOfRoomsRepeat))}";

        // 房主特有逻辑
        if (OnlineManager.lobby.isOwner)
        {
            if (menu.manager.upcomingProcess == null) levelSelector.LoadNewPlaylist(Arena.playList, false); //dont replace playlist when starting game
            // 创建开始按钮（仅房主可见）
            if (startButton is null)
            {
                startButton = new SimplerButton(menu, this, menu.Translate("START MATCH!"), new Vector2(936f, 50f), new Vector2(110f, 30f))
                {
                    signalText = "START_MATCH"
                };
                subObjects.Add(startButton);
            }
            Arena.shufflePlayList = levelSelector.selectedLevelsPlaylist.ShuffleStatus;
        }
        else
        {
            // 非房主加载播放列表
            levelSelector.LoadNewPlaylist(Arena.playList, true);
            levelSelector.selectedLevelsPlaylist.ShuffleStatus = Arena.shufflePlayList;
            levelSelector.selectedLevelsPlaylist.shuffleButton.label.text = menu.Translate(levelSelector.selectedLevelsPlaylist.ShuffleStatus ? "Shuffling Levels" : "Playing in order");
            levelSelector.selectedLevelsPlaylist.shuffleButton.UpdateSymbol(levelSelector.selectedLevelsPlaylist.ShuffleStatus ? "Menu_Symbol_Shuffle" : "Menu_Symbol_Dont_Shuffle");
            this.ClearMenuObject(ref startButton);
        }
        // 更新比赛按钮状态
        UpdateMatchButtons();
    }
    
    /// <summary>
    /// 图形更新方法，用于更新视觉元素
    /// </summary>
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        // 更新分隔线位置
        chatLobbyStateDivider.x = chatMenuBox.DrawX(timeStacker) + (chatMenuBox.size.x / 2);
        chatLobbyStateDivider.y = chatMenuBox.DrawY(timeStacker) + chatMenuBox.roundedRect.size.y - 50;
    }
}
