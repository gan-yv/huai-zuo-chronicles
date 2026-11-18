using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 《春江花月夜：异本》Demo v0.1 的临时启动入口。
///
/// 当前职责：
/// 1. 初始化中文字体。
/// 2. 创建输入事件系统和 Canvas。
/// 3. 生成标题页。
/// 4. 完成修复室、书坊调查与异文比对。
/// 5. 根据“待/照”显示对应结果和校勘笺。
/// 6. 支持从结束页完整重开。
///
/// 这一阶段故意不依赖预制体和场景手工布局，
/// 便于先验证文字、按钮和场景切换链路。
/// </summary>
[DisallowMultipleComponent]
public class DemoApp : MonoBehaviour
{
    // 主背景色：接近墨色的深青黑。
    private static readonly Color DeepInk = new Color32(14, 24, 25, 255);

    // 书坊占位背景色：偏暖的深木色。
    private static readonly Color PanelBrown = new Color32(39, 29, 24, 255);

    // 正文和标题使用的主纸张色。
    private static readonly Color Paper = new Color32(228, 216, 188, 255);

    // 次级文字颜色，用于提示和说明。
    private static readonly Color MutedPaper = new Color32(172, 160, 135, 255);

    // 朱砂色，用于主要按钮和视觉强调。
    private static readonly Color Cinnabar = new Color32(159, 54, 43, 255);

    // 金色，用于副标题和少量装饰。
    private static readonly Color Gold = new Color32(190, 155, 91, 255);

    private Canvas canvas;
    private Font uiFont;
    private GameObject titlePage;
    private GameObject restorationPage;
    private Text restorationSpeaker;
    private Text restorationBody;
    private Text restorationHint;
    private Button restorationPaperButton;
    private Button restorationInkButton;
    private Button restorationContinueButton;
    private bool paperInspected;
    private bool inkTouched;
    private GameObject bookshopPage;
    private Text dialogueSpeaker;
    private Text dialogueBody;
    private Text dialogueHint;
    private Button dialogueAdvanceButton;
    private GameObject investigationChoice;
    private int dialogueIndex;
    private StoryLine[] activeDialogue;
    private Action onDialogueComplete;
    private Investigation investigation;
    private string missedWitness;
    private GameObject comparisonPage;
    private GameObject printConfirmation;
    private Text comparisonSlot;
    private Text variantExplanation;
    private Text evidenceSummary;
    private Button daiBlockButton;
    private Button zhaoBlockButton;
    private Button placeBlockButton;
    private Button printButton;
    private Variant selectedVariant;
    private Variant confirmedVariant;
    private bool blockPlaced;
    private GameObject resultPage;
    private Text resultVariantMark;
    private Text resultTitle;
    private Text resultNarration;
    private Text resultDialogue;
    private Text resultEndingCard;
    private Text[] resultSceneCaptions;
    private GameObject collationPage;
    private Text collationLeft;
    private Text collationRight;
    private GameObject endPage;
    private Text endSummary;
    private Text endTime;
    private float demoStartTime;

    /// <summary>
    /// Demo 中唯一一次互斥调查的两种结果。
    /// </summary>
    private enum Investigation
    {
        None,
        Printer,
        Performer
    }

    /// <summary>
    /// 玩家最终选择印入节目册的异文字。
    /// </summary>
    private enum Variant
    {
        None,
        Dai,
        Zhao
    }

    /// <summary>
    /// 书坊公共开场的一句话数据。
    /// </summary>
    private readonly struct StoryLine
    {
        public string Speaker { get; }
        public string Text { get; }

        public StoryLine(string speaker, string text)
        {
            Speaker = speaker;
            Text = text;
        }
    }

    /// <summary>
    /// 第一阶段接入的六句对白。
    /// 每句对应一次点击，完成身份、目标与灯序规则的建立。
    /// </summary>
    private static readonly StoryLine[] Introduction =
    {
        new StoryLine("陆掌柜", "校书人，你总算来了。"),
        new StoryLine("主角", "我没有见过他。"),
        new StoryLine("顾师父", "她连自己怎么来的都不知道，你还指望她校字？"),
        new StoryLine("陆掌柜", "节目册今夜必须开印。再晚一刻，三城乐工就合不上灯序。"),
        new StoryLine("主角", "灯序？"),
        new StoryLine("顾师父", "乐工唱节目册，灯队听乐工。"),
        new StoryLine("陆掌柜", "这一页写什么字，整座扬州就怎么点灯。"),
        new StoryLine("主角", "只差一个字，为什么要拿一条船来赌？"),
        new StoryLine("陆掌柜", "唱“照”，灯转向江面。船上的人会被看见，也能看清暗礁。"),
        new StoryLine("顾师父", "唱“待”，灯停在两岸。船能趁黑走，也可能趁黑沉。"),
        new StoryLine("主角", "船上到底有什么？"),
        new StoryLine("陆掌柜", "有不该被盐商会馆扣下的人。"),
        new StoryLine("顾师父", "也有知道你为何会出现在这里的人。"),
        new StoryLine("顾师父", "我们都在说对自己有用的真话。")
    };

    /// <summary>
    /// 选择陆掌柜后获得的关键证词。
    /// </summary>
    private static readonly StoryLine[] PrinterInvestigation =
    {
        new StoryLine("陆掌柜", "这版节目册不是我刻的。"),
        new StoryLine("主角", "那“照”是谁改的？"),
        new StoryLine("陆掌柜", "盐商会馆送来纸和字。我只知道，江面会因此亮起来。"),
        new StoryLine("主角", "你为什么愿意冒这个险？"),
        new StoryLine("陆掌柜", "我师兄在码头。今夜不走，以后也走不了。")
    };

    /// <summary>
    /// 选择顾师父后获得的关键证词。
    /// </summary>
    private static readonly StoryLine[] PerformerInvestigation =
    {
        new StoryLine("顾师父", "旧词到这里要停一拍。"),
        new StoryLine("顾师父", "让灯队等。等，就是不给任何人指向。"),
        new StoryLine("主角", "“照”是谁送进节目册的？"),
        new StoryLine("顾师父", "我知道。但“照”最初不是给盐商会馆用的。"),
        new StoryLine("顾师父", "那是给一条载着码头人的夜船。")
    };

    /// <summary>
    /// Unity 生命周期入口。
    /// 所有运行时界面都在这里按顺序初始化。
    /// </summary>
    private void Start()
    {
        PrepareFont();
        EnsureEventSystem();
        CreateCanvas();

        BuildTitlePage();
        BuildRestorationRoom();
        BuildBookshopScene();
        BuildComparisonPage();
        BuildResultPage();
        BuildCollationPage();
        BuildEndPage();

        ShowTitlePage();
    }

    /// <summary>
    /// 从 Windows 系统字体中创建动态字体。
    ///
    /// UI Text 默认字体通常不包含完整中文字符，
    /// 因此优先使用微软雅黑，找不到时依次尝试黑体和宋体。
    /// 如果全部失败，则退回 Unity 内置字体。
    /// </summary>
    private void PrepareFont()
    {
        uiFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "SimSun" },
            36);

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    /// <summary>
    /// 确保场景中存在 EventSystem。
    ///
    /// 本项目启用了新版 Input System，
    /// 因此必须使用 InputSystemUIInputModule，
    /// 否则鼠标无法点击 uGUI 按钮。
    /// </summary>
    private void EnsureEventSystem()
    {
        // 如果场景里已经有事件系统，就不重复创建。
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule));

        eventSystemObject.transform.SetParent(transform, false);

        // 为鼠标、触摸和键盘导航绑定默认 UI Action。
        InputSystemUIInputModule inputModule =
            eventSystemObject.GetComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    /// <summary>
    /// 创建一个 1920×1080 设计分辨率的屏幕空间 Canvas。
    ///
    /// CanvasScaler 会在不同窗口尺寸下缩放整个界面，
    /// 从而保证 16:9 窗口中的布局比例稳定。
    /// </summary>
    private void CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "DemoCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();

        // 以 1920×1080 为设计分辨率。
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 同时参考宽高变化，避免窗口比例微调时文字突然溢出。
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    /// <summary>
    /// 构建标题页。
    ///
    /// 当前版本使用纯色块、线条和文字完成灰盒构图，
    /// 后续替换正式美术时，可以保留文案和按钮结构。
    /// </summary>
    private void BuildTitlePage()
    {
        titlePage = CreateFullscreenPanel(
            "TitlePage",
            canvas.transform,
            DeepInk);

        // 左侧竖线作为版式边界。
        AddImage(
            titlePage.transform,
            "LeftRule",
            Cinnabar,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(118f, 180f),
            new Vector2(124f, -180f));

        // 顶部细线用于压住画面，增强书页排版感。
        AddImage(
            titlePage.transform,
            "TopRule",
            new Color32(93, 102, 87, 120),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(118f, -115f),
            new Vector2(-118f, -113f));

        // 底部蓝色区域代表江水。
        AddImage(
            titlePage.transform,
            "RiverBand",
            new Color32(23, 42, 43, 255),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 154f));

        // 一条较亮的横线表示水面反光。
        AddImage(
            titlePage.transform,
            "RiverLine",
            new Color32(65, 91, 88, 170),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(118f, 153f),
            new Vector2(-118f, 155f));

        // 标题页左上角的修复记录编号。
        CreateText(
            titlePage.transform,
            "Record",
            "修复记录 043  ·  《春江花月夜》残页",
            24,
            MutedPaper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(172f, -156f),
            new Vector2(820f, 42f));

        // 游戏主标题。
        CreateText(
            titlePage.transform,
            "MainTitle",
            "春江花月夜",
            88,
            Paper,
            TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(164f, 92f),
            new Vector2(980f, 128f));

        // 主标题下的“异本”副标题。
        CreateText(
            titlePage.transform,
            "VariantTitle",
            "异  本",
            38,
            Gold,
            TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(174f, 16f),
            new Vector2(580f, 64f));

        // 一句话概括本 Demo 的核心冲突。
        CreateText(
            titlePage.transform,
            "Subtitle",
            "一字之差，三城灯序。",
            26,
            MutedPaper,
            TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(176f, -70f),
            new Vector2(760f, 50f));

        // 主要交互按钮。
        AddButton(
            titlePage.transform,
            "开始校对",
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(172f, -188f),
            new Vector2(328f, 76f),
            StartRestoration);

        // 左下角版本说明。
        CreateText(
            titlePage.transform,
            "Version",
            "Demo v0.1  ·  第一处异文",
            20,
            new Color32(133, 143, 126, 255),
            TextAnchor.LowerLeft,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(172f, 62f),
            new Vector2(620f, 38f));
    }

    /// <summary>
    /// 构建现代文献修复室。
    ///
    /// 玩家先看到残页，再依次点击残页和墨痕，
    /// 用两次明确点击完成“发现异本”的入口。
    /// </summary>
    private void BuildRestorationRoom()
    {
        restorationPage = CreateFullscreenPanel(
            "RestorationRoom",
            canvas.transform,
            new Color32(10, 15, 16, 255));

        // 桌面占据画面下半部分，形成修复室的封闭空间。
        AddImage(
            restorationPage.transform,
            "Desk",
            new Color32(47, 34, 28, 255),
            new Vector2(0f, 0f),
            new Vector2(1f, 0.73f),
            Vector2.zero,
            Vector2.zero);

        AddImage(
            restorationPage.transform,
            "DeskEdge",
            new Color32(92, 66, 47, 255),
            new Vector2(0f, 0.72f),
            new Vector2(1f, 0.725f),
            Vector2.zero,
            Vector2.zero);

        // 台灯只用几何色块表示，当前阶段不依赖正式美术。
        AddImage(
            restorationPage.transform,
            "LampStem",
            new Color32(35, 39, 37, 255),
            new Vector2(0.78f, 0.64f),
            new Vector2(0.79f, 0.92f),
            Vector2.zero,
            Vector2.zero);

        AddImage(
            restorationPage.transform,
            "LampShade",
            new Color32(104, 80, 50, 255),
            new Vector2(0.71f, 0.88f),
            new Vector2(0.85f, 0.98f),
            Vector2.zero,
            Vector2.zero);

        AddImage(
            restorationPage.transform,
            "LampGlow",
            new Color32(182, 139, 69, 38),
            new Vector2(0.62f, 0.28f),
            new Vector2(0.9f, 0.93f),
            Vector2.zero,
            Vector2.zero);

        CreateText(
            restorationPage.transform,
            "RestorationTitle",
            "修复室 · 灯下",
            42,
            Paper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(112f, -54f),
            new Vector2(620f, 64f));

        CreateText(
            restorationPage.transform,
            "RestorationRecord",
            "修复记录 043",
            21,
            MutedPaper,
            TextAnchor.UpperRight,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-132f, -66f),
            new Vector2(380f, 40f));

        // 可点击残页。
        RectTransform paper = AddImage(
            restorationPage.transform,
            "Manuscript",
            new Color32(223, 211, 183, 255),
            new Vector2(0.24f, 0.29f),
            new Vector2(0.61f, 0.73f),
            Vector2.zero,
            Vector2.zero);

        Image paperImage = paper.GetComponent<Image>();
        paperImage.raycastTarget = true;

        restorationPaperButton = paper.gameObject.AddComponent<Button>();
        restorationPaperButton.targetGraphic = paperImage;
        restorationPaperButton.transition = Selectable.Transition.ColorTint;

        ColorBlock paperColors = restorationPaperButton.colors;
        paperColors.normalColor = Color.white;
        paperColors.highlightedColor = new Color(1f, 0.94f, 0.78f, 1f);
        paperColors.pressedColor = new Color(0.8f, 0.76f, 0.68f, 1f);
        restorationPaperButton.colors = paperColors;
        restorationPaperButton.onClick.AddListener(InspectPaper);

        CreateText(
            paper,
            "ManuscriptLabel",
            "《春江花月夜》残页",
            22,
            new Color32(103, 77, 54, 255),
            TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -46f),
            new Vector2(520f, 44f));

        CreateText(
            paper,
            "MissingLine",
            "不知江月____何人",
            44,
            new Color32(49, 39, 31, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.52f),
            new Vector2(0.5f, 0.52f),
            Vector2.zero,
            new Vector2(620f, 92f));

        CreateText(
            paper,
            "PaperHint",
            "点击残页",
            20,
            new Color32(126, 99, 70, 255),
            TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 38f),
            new Vector2(360f, 38f));

        restorationInkButton = AddButton(
            restorationPage.transform,
            "墨痕",
            new Vector2(0.62f, 0.48f),
            new Vector2(0.62f, 0.48f),
            Vector2.zero,
            new Vector2(128f, 64f),
            TouchInk);

        restorationInkButton.gameObject.SetActive(false);

        // 修复室对白框与书坊共用相同的阅读语言。
        RectTransform dialoguePanel = AddImage(
            restorationPage.transform,
            "RestorationDialogue",
            new Color32(9, 10, 10, 225),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(92f, 46f),
            new Vector2(-92f, 270f));

        AddImage(
            dialoguePanel,
            "RestorationAccent",
            Cinnabar,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(7f, 0f));

        restorationSpeaker = CreateText(
            dialoguePanel,
            "RestorationSpeaker",
            "主角",
            27,
            Gold,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(54f, -32f),
            new Vector2(420f, 42f));

        restorationBody = CreateText(
            dialoguePanel,
            "RestorationBody",
            string.Empty,
            30,
            Paper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(54f, -82f),
            new Vector2(1420f, 108f));

        restorationHint = CreateText(
            dialoguePanel,
            "RestorationHint",
            string.Empty,
            20,
            MutedPaper,
            TextAnchor.LowerLeft,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(54f, 26f),
            new Vector2(720f, 34f));

        restorationContinueButton = AddButton(
            restorationPage.transform,
            "触碰墨痕",
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-210f, 91f),
            new Vector2(242f, 58f),
            StartBookshopScene);

        restorationContinueButton.gameObject.SetActive(false);

        AddButton(
            restorationPage.transform,
            "返回标题",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-124f, -62f),
            new Vector2(170f, 48f),
            ShowTitlePage);

        restorationPage.SetActive(false);
    }

    /// <summary>
    /// 从标题进入修复室，清空上一轮残页检查状态。
    /// </summary>
    private void StartRestoration()
    {
        titlePage.SetActive(false);
        restorationPage.SetActive(true);
        bookshopPage.SetActive(false);
        comparisonPage.SetActive(false);
        resultPage.SetActive(false);
        collationPage.SetActive(false);
        endPage.SetActive(false);

        paperInspected = false;
        inkTouched = false;
        demoStartTime = Time.unscaledTime;

        restorationPaperButton.interactable = true;
        restorationInkButton.gameObject.SetActive(false);
        restorationContinueButton.gameObject.SetActive(false);
        restorationSpeaker.text = "主角";
        restorationBody.text =
            "这张残页已经在我桌上放了三天。\n诗里缺了一个字。";
        restorationHint.text = "点击桌面上的残页开始校对。";
    }

    /// <summary>
    /// 检查残页，揭示缺字与人为刮痕。
    /// </summary>
    private void InspectPaper()
    {
        if (paperInspected)
        {
            return;
        }

        paperInspected = true;
        restorationPaperButton.interactable = false;
        restorationBody.text =
            "不知江月____何人。\n" +
            "缺字处有刮痕，不是自然破损——有人故意把它去掉。";
        restorationHint.text = "残页旁还有一处墨迹。";
        restorationInkButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// 触碰纸背墨痕，触发进入异本书坊的转场。
    /// </summary>
    private void TouchInk()
    {
        if (!paperInspected || inkTouched)
        {
            return;
        }

        inkTouched = true;
        restorationInkButton.gameObject.SetActive(false);
        restorationBody.text =
            "纸背还有墨。像一个字，也像一条河。\n" +
            "我用手碰了一下，台灯熄灭，水声涌进耳朵。";
        restorationHint.text = "墨迹顺着纸纹流动。";

        restorationContinueButton.gameObject.SetActive(true);
        Text continueLabel =
            restorationContinueButton.GetComponentInChildren<Text>();
        continueLabel.text = "进入书坊";
    }

    /// <summary>
    /// 构建书坊场景和开场对白框。
    ///
    /// 当前场景使用色块表示印台、窗和人物，
    /// 主要验证信息是否能在阅读节奏中清楚出现。
    /// </summary>
    private void BuildBookshopScene()
    {
        bookshopPage = CreateFullscreenPanel(
            "BookshopPage",
            canvas.transform,
            PanelBrown);

        // 书坊顶部的木梁。
        AddImage(
            bookshopPage.transform,
            "CeilingBeam",
            new Color32(27, 21, 18, 255),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -150f),
            Vector2.zero);

        // 远处的窗，代表书坊外的夜色和河岸。
        AddImage(
            bookshopPage.transform,
            "NightWindow",
            new Color32(16, 34, 37, 255),
            new Vector2(0.72f, 0.37f),
            new Vector2(0.92f, 0.76f),
            Vector2.zero,
            Vector2.zero);

        AddImage(
            bookshopPage.transform,
            "WindowCrossVertical",
            new Color32(71, 55, 42, 255),
            new Vector2(0.82f, 0.37f),
            new Vector2(0.825f, 0.76f),
            Vector2.zero,
            Vector2.zero);

        AddImage(
            bookshopPage.transform,
            "WindowCrossHorizontal",
            new Color32(71, 55, 42, 255),
            new Vector2(0.72f, 0.56f),
            new Vector2(0.92f, 0.57f),
            Vector2.zero,
            Vector2.zero);

        // 印台和悬挂纸张的灰盒轮廓。
        AddImage(
            bookshopPage.transform,
            "PrintingPress",
            new Color32(63, 45, 35, 255),
            new Vector2(0.34f, 0.32f),
            new Vector2(0.52f, 0.69f),
            Vector2.zero,
            Vector2.zero);

        AddImage(
            bookshopPage.transform,
            "PrinterSilhouette",
            new Color32(74, 53, 40, 255),
            new Vector2(0.56f, 0.31f),
            new Vector2(0.66f, 0.70f),
            Vector2.zero,
            Vector2.zero);

        AddImage(
            bookshopPage.transform,
            "PerformerSilhouette",
            new Color32(54, 60, 51, 255),
            new Vector2(0.84f, 0.34f),
            new Vector2(0.92f, 0.71f),
            Vector2.zero,
            Vector2.zero);

        // 书坊页标题。
        CreateText(
            bookshopPage.transform,
            "BookshopTitle",
            "扬州 · 书坊",
            44,
            Paper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(116f, -62f),
            new Vector2(620f, 70f));

        CreateText(
            bookshopPage.transform,
            "BookshopTime",
            "灯会 · 戌时",
            22,
            MutedPaper,
            TextAnchor.UpperRight,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-150f, -72f),
            new Vector2(360f, 44f));

        // 人物名称作为灰盒场景中的空间标记。
        CreateText(
            bookshopPage.transform,
            "PrinterName",
            "陆掌柜",
            22,
            new Color32(202, 184, 150, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.61f, 0.28f),
            new Vector2(0.61f, 0.28f),
            Vector2.zero,
            new Vector2(200f, 40f));

        CreateText(
            bookshopPage.transform,
            "PerformerName",
            "顾师父",
            22,
            new Color32(185, 194, 171, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.88f, 0.31f),
            new Vector2(0.88f, 0.31f),
            Vector2.zero,
            new Vector2(200f, 40f));

        CreateText(
            bookshopPage.transform,
            "PressLabel",
            "印台",
            19,
            new Color32(146, 126, 101, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.43f, 0.35f),
            new Vector2(0.43f, 0.35f),
            Vector2.zero,
            new Vector2(160f, 36f));

        // 底部对白区域。整块区域都可以点击推进。
        RectTransform dialoguePanel = AddImage(
            bookshopPage.transform,
            "DialoguePanel",
            new Color32(12, 12, 11, 232),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(90f, 54f),
            new Vector2(-90f, 328f));

        dialogueAdvanceButton =
            dialoguePanel.gameObject.AddComponent<Button>();

        // AddImage 默认关闭鼠标检测，避免装饰图层挡住交互。
        // 对白框本身必须重新开启，否则整块区域只能看见却不能点击。
        Image dialogueGraphic = dialoguePanel.GetComponent<Image>();
        dialogueGraphic.raycastTarget = true;

        dialogueAdvanceButton.targetGraphic = dialogueGraphic;
        dialogueAdvanceButton.transition = Selectable.Transition.None;
        dialogueAdvanceButton.onClick.AddListener(AdvanceDialogue);

        AddImage(
            dialoguePanel,
            "DialogueAccent",
            Cinnabar,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0f),
            new Vector2(7f, 0f));

        dialogueSpeaker = CreateText(
            dialoguePanel,
            "DialogueSpeaker",
            string.Empty,
            28,
            Gold,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(58f, -38f),
            new Vector2(420f, 46f));

        dialogueBody = CreateText(
            dialoguePanel,
            "DialogueBody",
            string.Empty,
            32,
            Paper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(58f, -94f),
            new Vector2(1580f, 130f));

        dialogueHint = CreateText(
            dialoguePanel,
            "DialogueHint",
            "点击继续  ▸",
            20,
            new Color32(157, 143, 116, 255),
            TextAnchor.LowerRight,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-42f, 28f),
            new Vector2(260f, 36f));

        // 任何时候都可以返回标题，方便反复检查开场。
        AddButton(
            bookshopPage.transform,
            "返回标题",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-124f, -62f),
            new Vector2(170f, 48f),
            ShowTitlePage);

        BuildInvestigationChoice();

        // 场景构建时保持隐藏，由开始按钮切换进入。
        bookshopPage.SetActive(false);
    }

    /// <summary>
    /// 构建调查二选一面板。
    ///
    /// 两个按钮使用相同的颜色和尺寸，
    /// 避免用视觉颜色暗示某个选择是“正确答案”。
    /// </summary>
    private void BuildInvestigationChoice()
    {
        investigationChoice = CreateFullscreenPanel(
            "InvestigationChoice",
            bookshopPage.transform,
            new Color32(9, 8, 7, 242));

        CreateText(
            investigationChoice.transform,
            "ChoiceEyebrow",
            "更鼓已响 · 节目册即将上版",
            22,
            MutedPaper,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 236f),
            new Vector2(900f, 42f));

        CreateText(
            investigationChoice.transform,
            "ChoiceTitle",
            "你只能先听一个人的。",
            48,
            Paper,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 170f),
            new Vector2(1000f, 78f));

        CreateText(
            investigationChoice.transform,
            "ChoiceWarning",
            "选择后，另一条线索将在本次 Demo 中关闭。",
            24,
            new Color32(183, 124, 100, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 112f),
            new Vector2(1000f, 46f));

        AddButton(
            investigationChoice.transform,
            "留在书坊\n查节目册的印版",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-330f, -6f),
            new Vector2(560f, 154f),
            () => SelectInvestigation(Investigation.Printer));

        AddButton(
            investigationChoice.transform,
            "随顾师父去窗边\n听一遍旧唱法",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(330f, -6f),
            new Vector2(560f, 154f),
            () => SelectInvestigation(Investigation.Performer));

        CreateText(
            investigationChoice.transform,
            "ChoiceFooter",
            "两人所说的都是真话，但都只说了对自己有用的那一部分。",
            21,
            new Color32(142, 132, 110, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -154f),
            new Vector2(1100f, 42f));

        investigationChoice.SetActive(false);
    }

    /// <summary>
    /// 进入书坊并从第一句对白开始。
    /// </summary>
    private void StartBookshopScene()
    {
        titlePage.SetActive(false);
        restorationPage.SetActive(false);
        comparisonPage.SetActive(false);
        resultPage.SetActive(false);
        collationPage.SetActive(false);
        endPage.SetActive(false);
        printConfirmation.SetActive(false);
        bookshopPage.SetActive(true);

        investigation = Investigation.None;
        missedWitness = string.Empty;
        investigationChoice.SetActive(false);
        dialogueAdvanceButton.interactable = true;

        BeginDialogue(Introduction, ShowInvestigationChoice);
    }

    /// <summary>
    /// 点击对白框时推进一句。
    /// 到最后一句后执行该段对白注册的完成回调。
    /// </summary>
    private void AdvanceDialogue()
    {
        if (activeDialogue == null || activeDialogue.Length == 0)
        {
            Action continued = onDialogueComplete;
            onDialogueComplete = null;
            continued?.Invoke();
            return;
        }

        if (dialogueIndex < activeDialogue.Length - 1)
        {
            dialogueIndex++;
            RenderDialogue();
            return;
        }

        Action completed = onDialogueComplete;
        activeDialogue = null;
        onDialogueComplete = null;
        completed?.Invoke();
    }

    /// <summary>
    /// 把当前对白索引对应的说话人和正文写入界面。
    /// </summary>
    private void RenderDialogue()
    {
        if (activeDialogue == null || activeDialogue.Length == 0)
        {
            return;
        }

        StoryLine line = activeDialogue[dialogueIndex];
        dialogueSpeaker.text = line.Speaker;
        dialogueBody.text = line.Text;
        dialogueHint.text =
            $"{dialogueIndex + 1} / {activeDialogue.Length}  ·  点击继续  ▸";
    }

    /// <summary>
    /// 开始一组新的对白，并指定读完后要执行的步骤。
    /// </summary>
    private void BeginDialogue(StoryLine[] lines, Action completed)
    {
        activeDialogue = lines;
        onDialogueComplete = completed;
        dialogueIndex = 0;
        dialogueAdvanceButton.interactable = true;
        RenderDialogue();
    }

    /// <summary>
    /// 公共开场结束后显示互斥调查选项。
    /// </summary>
    private void ShowInvestigationChoice()
    {
        dialogueAdvanceButton.interactable = false;
        investigationChoice.SetActive(true);
    }

    /// <summary>
    /// 记录玩家的调查选择，关闭另一条入口，并进入对应证词。
    /// </summary>
    private void SelectInvestigation(Investigation selected)
    {
        // 防止界面连点导致同一轮 Demo 被写入两次调查结果。
        if (investigation != Investigation.None)
        {
            return;
        }

        investigation = selected;
        investigationChoice.SetActive(false);
        dialogueAdvanceButton.interactable = true;

        if (selected == Investigation.Printer)
        {
            missedWitness = "顾师父";
            BeginDialogue(PrinterInvestigation, FinishPrinterInvestigation);
            return;
        }

        missedWitness = "陆掌柜";
        BeginDialogue(PerformerInvestigation, FinishPerformerInvestigation);
    }

    /// <summary>
    /// 完成陆掌柜调查，并展示获得的证词摘要。
    /// </summary>
    private void FinishPrinterInvestigation()
    {
        dialogueAdvanceButton.interactable = true;
        onDialogueComplete = OpenComparisonPage;
        dialogueSpeaker.text = "调查记录";
        dialogueBody.text =
            "获得证词：陆掌柜愿意用“照”字照见夜航船。\n" +
            $"未听取：{missedWitness}的旧唱法。";
        dialogueHint.text = "点击进入异文比对  ▸";
    }

    /// <summary>
    /// 完成顾师父调查，并展示获得的证词摘要。
    /// </summary>
    private void FinishPerformerInvestigation()
    {
        dialogueAdvanceButton.interactable = true;
        onDialogueComplete = OpenComparisonPage;
        dialogueSpeaker.text = "调查记录";
        dialogueBody.text =
            "获得证词：“照”字最初是救人的夜船信号。\n" +
            $"未听取：{missedWitness}愿意冒险印字的理由。";
        dialogueHint.text = "点击进入异文比对  ▸";
    }

    /// <summary>
    /// 构建双页异文比对界面。
    ///
    /// 左右两页分别展示残页与节目册，
    /// 中央展示待上版的字块槽位。
    /// </summary>
    private void BuildComparisonPage()
    {
        comparisonPage = CreateFullscreenPanel(
            "ComparisonPage",
            canvas.transform,
            new Color32(15, 20, 20, 255));

        AddImage(
            comparisonPage.transform,
            "ComparisonRule",
            new Color32(92, 75, 53, 170),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(112f, -116f),
            new Vector2(-112f, -113f));

        CreateText(
            comparisonPage.transform,
            "ComparisonTitle",
            "异文比对 · 第一处",
            42,
            Paper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(112f, -48f),
            new Vector2(720f, 62f));

        CreateText(
            comparisonPage.transform,
            "ComparisonSubtitle",
            "同一句诗，两种会改变灯队动作的字。",
            22,
            MutedPaper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(114f, -98f),
            new Vector2(900f, 38f));

        // 左侧：主角带入异本世界的残页。
        RectTransform leftPage = AddImage(
            comparisonPage.transform,
            "LeftManuscriptPage",
            Paper,
            new Vector2(0.08f, 0.44f),
            new Vector2(0.47f, 0.84f),
            Vector2.zero,
            Vector2.zero);

        CreateText(
            leftPage,
            "LeftPageLabel",
            "原作残页",
            22,
            new Color32(102, 72, 52, 255),
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(42f, -32f),
            new Vector2(360f, 42f));

        CreateText(
            leftPage,
            "LeftPageLine",
            "不知江月 待 何人",
            42,
            new Color32(48, 38, 30, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.53f),
            new Vector2(0.5f, 0.53f),
            Vector2.zero,
            new Vector2(650f, 90f));

        CreateText(
            leftPage,
            "LeftPageNote",
            "缺字处留有刮痕，衬纸却残留旧墨。",
            20,
            new Color32(111, 87, 67, 255),
            TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 42f),
            new Vector2(650f, 44f));

        // 右侧：陆掌柜准备开印的节目册。
        RectTransform rightPage = AddImage(
            comparisonPage.transform,
            "RightProgramPage",
            new Color32(214, 202, 175, 255),
            new Vector2(0.53f, 0.44f),
            new Vector2(0.92f, 0.84f),
            Vector2.zero,
            Vector2.zero);

        CreateText(
            rightPage,
            "RightPageLabel",
            "扬州灯会节目册 · 印样",
            22,
            new Color32(102, 72, 52, 255),
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(42f, -32f),
            new Vector2(520f, 42f));

        CreateText(
            rightPage,
            "RightPageLine",
            "不知江月 照 何人",
            42,
            new Color32(48, 38, 30, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.53f),
            new Vector2(0.5f, 0.53f),
            Vector2.zero,
            new Vector2(650f, 90f));

        CreateText(
            rightPage,
            "RightPageNote",
            "节目册上的这一字，决定乐工与灯队如何收尾。",
            20,
            new Color32(111, 87, 67, 255),
            TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 42f),
            new Vector2(650f, 44f));

        CreateText(
            comparisonPage.transform,
            "SlotCaption",
            "印版空槽",
            20,
            MutedPaper,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.38f),
            new Vector2(0.5f, 0.38f),
            new Vector2(0f, 34f),
            new Vector2(280f, 34f));

        RectTransform slotFrame = AddImage(
            comparisonPage.transform,
            "SlotFrame",
            new Color32(76, 54, 39, 255),
            new Vector2(0.5f, 0.38f),
            new Vector2(0.5f, 0.38f),
            new Vector2(-76f, -58f),
            new Vector2(76f, 58f));

        comparisonSlot = CreateText(
            slotFrame,
            "SlotCharacter",
            "□",
            58,
            Gold,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(140f, 100f));

        // 两个木字块使用完全相同的默认样式。
        daiBlockButton = AddButton(
            comparisonPage.transform,
            "待",
            new Vector2(0.5f, 0.285f),
            new Vector2(0.5f, 0.285f),
            new Vector2(-220f, 0f),
            new Vector2(154f, 96f),
            () => SelectVariant(Variant.Dai));

        zhaoBlockButton = AddButton(
            comparisonPage.transform,
            "照",
            new Vector2(0.5f, 0.285f),
            new Vector2(0.5f, 0.285f),
            new Vector2(220f, 0f),
            new Vector2(154f, 96f),
            () => SelectVariant(Variant.Zhao));

        variantExplanation = CreateText(
            comparisonPage.transform,
            "VariantExplanation",
            "先选择一个字，查看它会让灯队做什么。",
            24,
            Paper,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.195f),
            new Vector2(0.5f, 0.195f),
            Vector2.zero,
            new Vector2(1160f, 54f));

        evidenceSummary = CreateText(
            comparisonPage.transform,
            "EvidenceSummary",
            string.Empty,
            21,
            new Color32(172, 154, 124, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.128f),
            new Vector2(0.5f, 0.128f),
            Vector2.zero,
            new Vector2(1380f, 62f));

        placeBlockButton = AddButton(
            comparisonPage.transform,
            "上版",
            new Vector2(0.5f, 0.066f),
            new Vector2(0.5f, 0.066f),
            new Vector2(-145f, 0f),
            new Vector2(240f, 64f),
            PlaceSelectedBlock);

        printButton = AddButton(
            comparisonPage.transform,
            "交印",
            new Vector2(0.5f, 0.066f),
            new Vector2(0.5f, 0.066f),
            new Vector2(145f, 0f),
            new Vector2(240f, 64f),
            ShowPrintConfirmation);

        placeBlockButton.interactable = false;
        printButton.interactable = false;

        AddButton(
            comparisonPage.transform,
            "返回标题",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-124f, -62f),
            new Vector2(170f, 48f),
            ShowTitlePage);

        BuildPrintConfirmation();
        comparisonPage.SetActive(false);
    }

    /// <summary>
    /// 从调查结果进入异文比对，并清空本轮比对状态。
    /// </summary>
    private void OpenComparisonPage()
    {
        bookshopPage.SetActive(false);
        comparisonPage.SetActive(true);
        printConfirmation.SetActive(false);

        selectedVariant = Variant.None;
        blockPlaced = false;
        comparisonSlot.text = "□";
        comparisonSlot.color = Gold;
        daiBlockButton.GetComponent<Image>().color = Cinnabar;
        zhaoBlockButton.GetComponent<Image>().color = Cinnabar;
        daiBlockButton.interactable = true;
        zhaoBlockButton.interactable = true;
        placeBlockButton.interactable = false;
        printButton.interactable = false;

        variantExplanation.text = "先选择一个字，查看它会让灯队做什么。";
        evidenceSummary.text = GetEvidenceSummary();
    }

    /// <summary>
    /// 选择候选异文并显示该字的后果。
    /// </summary>
    private void SelectVariant(Variant variant)
    {
        selectedVariant = variant;
        blockPlaced = false;
        comparisonSlot.text = variant == Variant.Dai ? "待" : "照";
        comparisonSlot.color = new Color32(215, 181, 111, 255);

        daiBlockButton.GetComponent<Image>().color =
            variant == Variant.Dai ? Gold : Cinnabar;
        zhaoBlockButton.GetComponent<Image>().color =
            variant == Variant.Zhao ? Gold : Cinnabar;

        variantExplanation.text = variant == Variant.Dai
            ? "灯停在两岸。船可以趁黑走，也可能沉在黑暗里。"
            : "灯转向江面。船会被看见，也会被追查。";

        placeBlockButton.interactable = true;
        printButton.interactable = false;
    }

    /// <summary>
    /// 将选中的字块放入印版空槽。
    /// </summary>
    private void PlaceSelectedBlock()
    {
        if (selectedVariant == Variant.None)
        {
            return;
        }

        blockPlaced = true;
        placeBlockButton.interactable = false;
        printButton.interactable = true;
        variantExplanation.text =
            "字块已经上版。交印后，今晚的节目册将无法更改。";
    }

    /// <summary>
    /// 构建交印前的二次确认窗口。
    /// </summary>
    private void BuildPrintConfirmation()
    {
        printConfirmation = CreateFullscreenPanel(
            "PrintConfirmation",
            comparisonPage.transform,
            new Color32(4, 4, 4, 210));

        RectTransform dialog = AddImage(
            printConfirmation.transform,
            "ConfirmationDialog",
            new Color32(39, 30, 24, 255),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-430f, -230f),
            new Vector2(430f, 230f));

        CreateText(
            dialog,
            "ConfirmationTitle",
            "交印",
            42,
            Paper,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 128f),
            new Vector2(640f, 64f));

        CreateText(
            dialog,
            "ConfirmationWarning",
            "一经印出，今晚无法收回。",
            28,
            new Color32(208, 155, 126, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 54f),
            new Vector2(640f, 56f));

        CreateText(
            dialog,
            "ConfirmationVariant",
            "当前字块",
            20,
            MutedPaper,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 2f),
            new Vector2(320f, 36f));

        AddButton(
            dialog,
            "确认交印",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-160f, -126f),
            new Vector2(260f, 68f),
            ConfirmPrint);

        AddButton(
            dialog,
            "再看一眼",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(160f, -126f),
            new Vector2(260f, 68f),
            CancelPrint);

        printConfirmation.SetActive(false);
    }

    /// <summary>
    /// 打开二次确认，避免误触直接提交异文。
    /// </summary>
    private void ShowPrintConfirmation()
    {
        if (!blockPlaced || selectedVariant == Variant.None)
        {
            return;
        }

        printConfirmation.SetActive(true);
    }

    /// <summary>
    /// 关闭二次确认并返回比对界面。
    /// </summary>
    private void CancelPrint()
    {
        printConfirmation.SetActive(false);
    }

    /// <summary>
    /// 确认交印，并进入所选异文对应的河岸结果。
    /// </summary>
    private void ConfirmPrint()
    {
        printConfirmation.SetActive(false);
        printButton.interactable = false;
        ShowResult(selectedVariant);
    }

    /// <summary>
    /// 构建交印后的河岸结果页。
    ///
    /// 两版共用同一套版式，通过文本和主色区分结果，
    /// 避免为单个 Demo 维护两套重复界面。
    /// </summary>
    private void BuildResultPage()
    {
        resultPage = CreateFullscreenPanel(
            "RiverResult",
            canvas.transform,
            new Color32(10, 20, 28, 255));

        AddImage(
            resultPage.transform,
            "RiverSky",
            new Color32(12, 26, 35, 255),
            new Vector2(0f, 0.36f),
            new Vector2(1f, 1f),
            Vector2.zero,
            Vector2.zero);

        AddImage(
            resultPage.transform,
            "RiverWater",
            new Color32(14, 34, 43, 255),
            new Vector2(0f, 0f),
            new Vector2(1f, 0.38f),
            Vector2.zero,
            Vector2.zero);

        AddImage(
            resultPage.transform,
            "RiverShore",
            new Color32(37, 32, 26, 255),
            new Vector2(0f, 0f),
            new Vector2(1f, 0.13f),
            Vector2.zero,
            Vector2.zero);

        CreateText(
            resultPage.transform,
            "ResultEyebrow",
            "扬州灯会 · 交印之后",
            21,
            MutedPaper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(104f, -54f),
            new Vector2(540f, 38f));

        resultTitle = CreateText(
            resultPage.transform,
            "ResultTitle",
            string.Empty,
            54,
            Paper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(102f, -92f),
            new Vector2(1040f, 82f));

        resultNarration = CreateText(
            resultPage.transform,
            "ResultNarration",
            string.Empty,
            24,
            new Color32(183, 173, 148, 255),
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(106f, -174f),
            new Vector2(1220f, 64f));

        resultVariantMark = CreateText(
            resultPage.transform,
            "ResultVariantMark",
            string.Empty,
            190,
            new Color32(93, 93, 77, 150),
            TextAnchor.UpperRight,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-74f, -72f),
            new Vector2(250f, 240f));

        // 四格静态画面概括灯队动作。
        resultSceneCaptions = new Text[4];
        string[] cardTitles = { "乐工", "灯队", "江面", "印台" };
        string[] cardLabels = { "一", "二", "三", "四" };

        for (int i = 0; i < resultSceneCaptions.Length; i++)
        {
            float x = -570f + 380f * i;

            RectTransform card = AddImage(
                resultPage.transform,
                "ResultCard_" + i,
                new Color32(23, 34, 34, 255),
                new Vector2(0.5f, 0.38f),
                new Vector2(0.5f, 0.38f),
                new Vector2(x - 165f, -122f),
                new Vector2(x + 165f, 122f));

            CreateText(
                card,
                "CardNumber",
                cardLabels[i],
                18,
                Gold,
                TextAnchor.UpperLeft,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(22f, -18f),
                new Vector2(80f, 30f));

            CreateText(
                card,
                "CardTitle",
                cardTitles[i],
                22,
                MutedPaper,
                TextAnchor.UpperRight,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-22f, -18f),
                new Vector2(130f, 30f));

            resultSceneCaptions[i] = CreateText(
                card,
                "CardCaption",
                string.Empty,
                20,
                Paper,
                TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -12f),
                new Vector2(280f, 112f));
        }

        resultDialogue = CreateText(
            resultPage.transform,
            "ResultDialogue",
            string.Empty,
            25,
            new Color32(225, 211, 181, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.205f),
            new Vector2(0.5f, 0.205f),
            Vector2.zero,
            new Vector2(1420f, 84f));

        resultEndingCard = CreateText(
            resultPage.transform,
            "ResultEndingCard",
            string.Empty,
            22,
            Gold,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.11f),
            new Vector2(0.5f, 0.11f),
            Vector2.zero,
            new Vector2(1160f, 54f));

        AddButton(
            resultPage.transform,
            "收起河岸",
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-160f, 58f),
            new Vector2(220f, 58f),
            ShowCollationPage);

        resultPage.SetActive(false);
    }

    /// <summary>
    /// 根据已确认的异文填充结果页。
    /// </summary>
    private void ShowResult(Variant variant)
    {
        confirmedVariant = variant;
        comparisonPage.SetActive(false);
        resultPage.SetActive(true);

        if (variant == Variant.Dai)
        {
            resultVariantMark.text = "待";
            resultVariantMark.color = new Color32(111, 147, 121, 150);
            resultTitle.text = "灯队停在两岸";
            resultNarration.text =
                "乐工唱到“待”时，停了半拍。船没有被照亮。";

            resultSceneCaptions[0].text = "唱到“待”时，声音停了半拍。";
            resultSceneCaptions[1].text = "灯队停在两岸，没有转向江面。";
            resultSceneCaptions[2].text = "一条船影从黑暗里滑过。";
            resultSceneCaptions[3].text = "海陵来的船信被放到印台上。";

            resultDialogue.text =
                "陆掌柜：你替这座城选择了等待。\n" +
                "顾师父：等待不是没有代价。";
            resultEndingCard.text = "你保住了“待”。那艘船没有被看见。";
            return;
        }

        resultVariantMark.text = "照";
        resultVariantMark.color = new Color32(173, 118, 67, 150);
        resultTitle.text = "灯火转向江面";
        resultNarration.text =
            "乐工唱到“照”时抬高了灯，一条夜船暴露在光里。";

        resultSceneCaptions[0].text = "唱到“照”时，乐工抬高灯火。";
        resultSceneCaptions[1].text = "灯队转向江面，岸边的人开始争论。";
        resultSceneCaptions[2].text = "夜航船被照得清清楚楚。";
        resultSceneCaptions[3].text = "旧唱词最后一页被风吹进河里。";

        resultDialogue.text =
            "顾师父：你们照见了船，也照见了谁不该活着。\n" +
            "陆掌柜：至少今夜有人能走。";
        resultEndingCard.text = "你印出了“照”。有人得救，有人从此不再开口。";
    }

    /// <summary>
    /// 构建由本次选择即时生成的残缺校勘笺。
    /// </summary>
    private void BuildCollationPage()
    {
        collationPage = CreateFullscreenPanel(
            "CollationPage",
            canvas.transform,
            new Color32(13, 18, 18, 255));

        CreateText(
            collationPage.transform,
            "CollationTitle",
            "校勘笺 · 扬州本",
            42,
            Paper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(104f, -48f),
            new Vector2(720f, 64f));

        CreateText(
            collationPage.transform,
            "CollationSubtitle",
            "这不是结局总结，而是会被带入下一座城市的记录。",
            21,
            MutedPaper,
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(106f, -98f),
            new Vector2(1000f, 38f));

        RectTransform paper = AddImage(
            collationPage.transform,
            "CollationPaper",
            new Color32(218, 207, 181, 255),
            new Vector2(0.06f, 0.16f),
            new Vector2(0.94f, 0.86f),
            Vector2.zero,
            Vector2.zero);

        AddImage(
            paper,
            "CenterFold",
            new Color32(139, 118, 88, 130),
            new Vector2(0.5f, 0f),
            new Vector2(0.502f, 1f),
            Vector2.zero,
            Vector2.zero);

        collationLeft = CreateText(
            paper,
            "CollationLeft",
            string.Empty,
            24,
            new Color32(54, 43, 34, 255),
            TextAnchor.UpperLeft,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(64f, -54f),
            new Vector2(720f, 520f));

        collationRight = CreateText(
            paper,
            "CollationRight",
            string.Empty,
            24,
            new Color32(54, 43, 34, 255),
            TextAnchor.UpperLeft,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(64f, -54f),
            new Vector2(720f, 520f));

        AddButton(
            collationPage.transform,
            "收起校勘笺",
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-160f, 54f),
            new Vector2(250f, 58f),
            ShowEndPage);

        collationPage.SetActive(false);
    }

    /// <summary>
    /// 将玩家的调查、异文与未解信息写入校勘笺。
    /// </summary>
    private void ShowCollationPage()
    {
        resultPage.SetActive(false);
        collationPage.SetActive(true);

        string chosenWord =
            confirmedVariant == Variant.Dai ? "待" : "照";
        string consequence =
            confirmedVariant == Variant.Dai
                ? "灯队停于两岸。"
                : "灯队转向江面。";

        collationLeft.text =
            "原始残句\n" +
            "不知江月____何人。\n\n" +
            $"本次填入\n{chosenWord}\n\n" +
            $"{consequence}\n\n" +
            "传播方式\n" +
            "扬州灯会节目册 · 印刷";

        if (investigation == Investigation.Printer)
        {
            collationRight.text =
                "证词来源\n陆掌柜\n\n" +
                "他愿意用“照”字照见夜航船。\n\n" +
                "未解记录\n顾师父为何知道“照”字的来历？\n\n" +
                "当前解释：部分确认。\n" +
                "海陵船信：待启封。";
            return;
        }

        collationRight.text =
            "证词来源\n顾师父\n\n" +
            "他知道“照”最初是救人的夜船信号。\n\n" +
            "未解记录\n陆掌柜为何愿意承担印版责任？\n\n" +
            "当前解释：部分确认。\n" +
            "海陵船信：待启封。";
    }

    /// <summary>
    /// 构建 Demo 结束页。
    /// </summary>
    private void BuildEndPage()
    {
        endPage = CreateFullscreenPanel(
            "EndPage",
            canvas.transform,
            new Color32(8, 16, 17, 255));

        CreateText(
            endPage.transform,
            "EndEyebrow",
            "第一处异文 · 测试段落",
            23,
            Gold,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 216f),
            new Vector2(900f, 46f));

        CreateText(
            endPage.transform,
            "EndTitle",
            "本次校对结束",
            58,
            Paper,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 126f),
            new Vector2(1000f, 86f));

        endSummary = CreateText(
            endPage.transform,
            "EndSummary",
            string.Empty,
            27,
            MutedPaper,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 32f),
            new Vector2(1200f, 92f));

        endTime = CreateText(
            endPage.transform,
            "EndTime",
            string.Empty,
            21,
            new Color32(151, 144, 123, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -48f),
            new Vector2(800f, 40f));

        AddButton(
            endPage.transform,
            "重新校对",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -142f),
            new Vector2(280f, 70f),
            ResetDemo);

        CreateText(
            endPage.transform,
            "EndNote",
            "本段为玩法测试，完整故事尚未制作。",
            19,
            new Color32(118, 127, 111, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -222f),
            new Vector2(900f, 38f));

        endPage.SetActive(false);
    }

    /// <summary>
    /// 记录选择与用时，显示最终结束卡。
    /// </summary>
    private void ShowEndPage()
    {
        collationPage.SetActive(false);
        endPage.SetActive(true);

        string chosenWord =
            confirmedVariant == Variant.Dai ? "待" : "照";
        endSummary.text =
            $"你让扬州节目册印出了“{chosenWord}”。\n" +
            "你所记录的词句，会进入下一座城市。";

        float elapsedSeconds = Mathf.Max(0f, Time.unscaledTime - demoStartTime);
        int minutes = Mathf.FloorToInt(elapsedSeconds / 60f);
        int seconds = Mathf.FloorToInt(elapsedSeconds % 60f);
        endTime.text = $"本次校对用时  {minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// 完整清理本轮 Demo 状态并回到标题。
    /// </summary>
    private void ResetDemo()
    {
        investigation = Investigation.None;
        missedWitness = string.Empty;
        selectedVariant = Variant.None;
        confirmedVariant = Variant.None;
        blockPlaced = false;
        paperInspected = false;
        inkTouched = false;
        dialogueIndex = 0;
        activeDialogue = null;
        onDialogueComplete = null;
        demoStartTime = 0f;

        if (comparisonSlot != null)
        {
            comparisonSlot.text = "□";
        }

        if (variantExplanation != null)
        {
            variantExplanation.text =
                "先选择一个字，查看它会让灯队做什么。";
        }

        if (evidenceSummary != null)
        {
            evidenceSummary.text = string.Empty;
        }

        ShowTitlePage();
    }

    /// <summary>
    /// 根据玩家实际听取的证词生成比对页证据摘要。
    /// </summary>
    private string GetEvidenceSummary()
    {
        if (investigation == Investigation.Printer)
        {
            return "证词：陆掌柜的师兄在夜航船上，他愿意用“照”为船示警。";
        }

        if (investigation == Investigation.Performer)
        {
            return "证词：“照”最初是救船信号，旧唱法唱“待”时要停一拍。";
        }

        return "证词：尚未调查任何人。";
    }

    /// <summary>
    /// 创建铺满父对象的纯色面板。
    /// </summary>
    /// <param name="name">场景层级中显示的对象名称。</param>
    /// <param name="parent">父级 UI 节点。</param>
    /// <param name="color">面板颜色。</param>
    /// <returns>新建的面板对象。</returns>
    private GameObject CreateFullscreenPanel(
        string name,
        Transform parent,
        Color color)
    {
        GameObject panel = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image));

        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();

        // 四边锚定父对象，因此会自动铺满 Canvas。
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        panel.GetComponent<Image>().color = color;
        return panel;
    }

    /// <summary>
    /// 创建一个矩形装饰线或色块。
    ///
    /// anchorMin 和 anchorMax 决定图像跟随父对象哪一部分缩放；
    /// offsetMin 和 offsetMax 用于设置左右、上下边距。
    /// </summary>
    private RectTransform AddImage(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image));

        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;

        // 装饰元素不拦截鼠标，避免挡住按钮。
        image.raycastTarget = false;

        return rect;
    }

    /// <summary>
    /// 创建一段 UI 文本。
    ///
    /// 所有文本都复用 PrepareFont 中准备好的中文字体，
    /// 并使用同一种溢出方式，方便后续统一调整排版。
    /// </summary>
    /// <param name="parent">文本所属的 UI 节点。</param>
    /// <param name="name">对象名称。</param>
    /// <param name="content">显示内容。</param>
    /// <param name="fontSize">字号。</param>
    /// <param name="color">文字颜色。</param>
    /// <param name="alignment">对齐方式。</param>
    /// <param name="anchorMin">最小锚点。</param>
    /// <param name="anchorMax">最大锚点。</param>
    /// <param name="anchoredPosition">相对锚点位置。</param>
    /// <param name="size">文本框大小。</param>
    /// <returns>创建完成的 Text 组件。</returns>
    private Text CreateText(
        Transform parent,
        string name,
        string content,
        int fontSize,
        Color color,
        TextAnchor alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Text));

        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        // 文字组件默认以矩形中心为枢轴点。
        // 如果左对齐文本仍沿用中心枢轴，
        // anchoredPosition 会把文本框的一半推到锚点左侧，
        // 在窄窗口或屏幕边缘就会造成标题被裁切。
        // 因此让枢轴点与文字对齐方式保持一致。
        switch (alignment)
        {
            case TextAnchor.UpperLeft:
                rect.pivot = new Vector2(0f, 1f);
                break;
            case TextAnchor.UpperCenter:
                rect.pivot = new Vector2(0.5f, 1f);
                break;
            case TextAnchor.UpperRight:
                rect.pivot = new Vector2(1f, 1f);
                break;
            case TextAnchor.MiddleLeft:
                rect.pivot = new Vector2(0f, 0.5f);
                break;
            case TextAnchor.MiddleCenter:
                rect.pivot = new Vector2(0.5f, 0.5f);
                break;
            case TextAnchor.MiddleRight:
                rect.pivot = new Vector2(1f, 0.5f);
                break;
            case TextAnchor.LowerLeft:
                rect.pivot = new Vector2(0f, 0f);
                break;
            case TextAnchor.LowerCenter:
                rect.pivot = new Vector2(0.5f, 0f);
                break;
            case TextAnchor.LowerRight:
                rect.pivot = new Vector2(1f, 0f);
                break;
            default:
                rect.pivot = new Vector2(0.5f, 0.5f);
                break;
        }

        Text text = textObject.GetComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        return text;
    }

    /// <summary>
    /// 创建一个可点击按钮，并使用 Text 生成按钮文字。
    ///
    /// 按钮使用 ColorBlock 提供悬停和按下反馈，
    /// 点击事件通过 Action 回调传入。
    /// </summary>
    private Button AddButton(
        Transform parent,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        Action onClick)
    {
        GameObject buttonObject = new GameObject(
            "Button_" + label,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));

        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image background = buttonObject.GetComponent<Image>();
        background.color = Cinnabar;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;

        // 悬停时略微变暖，按下时降低亮度。
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.93f, 0.78f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        // 将外部传入的方法注册为按钮点击事件。
        button.onClick.AddListener(() => onClick());

        // 按钮文字铺满按钮区域。
        CreateText(
            buttonObject.transform,
            "Label",
            label,
            26,
            Paper,
            TextAnchor.MiddleCenter,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);

        return button;
    }

    /// <summary>
    /// 显示标题页，并隐藏书坊页。
    /// </summary>
    private void ShowTitlePage()
    {
        titlePage.SetActive(true);
        restorationPage.SetActive(false);
        bookshopPage.SetActive(false);
        comparisonPage.SetActive(false);
        printConfirmation.SetActive(false);
        resultPage.SetActive(false);
        collationPage.SetActive(false);
        endPage.SetActive(false);
    }
}
