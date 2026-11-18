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
/// 4. 点击“开始校对”后切换到书坊占位页。
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
    private GameObject bookshopPlaceholder;

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
        BuildBookshopPlaceholder();

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
            ShowBookshopPlaceholder);

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
    /// 构建书坊占位页。
    ///
    /// 此页面当前只验证点击链路，
    /// 下一阶段会由对白系统接管。
    /// </summary>
    private void BuildBookshopPlaceholder()
    {
        bookshopPlaceholder = CreateFullscreenPanel(
            "BookshopPlaceholder",
            canvas.transform,
            PanelBrown);

        CreateText(
            bookshopPlaceholder.transform,
            "PlaceholderTitle",
            "扬州 · 书坊",
            62,
            Paper,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 118f),
            new Vector2(1000f, 100f));

        CreateText(
            bookshopPlaceholder.transform,
            "PlaceholderStatus",
            "标题、界面与按钮点击已经接通。",
            26,
            MutedPaper,
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 35f),
            new Vector2(1000f, 56f));

        CreateText(
            bookshopPlaceholder.transform,
            "PlaceholderNext",
            "下一段将在这里接入第一组对白。",
            22,
            new Color32(155, 145, 124, 255),
            TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -10f),
            new Vector2(1000f, 48f));

        // 临时返回按钮，方便反复测试页面切换。
        AddButton(
            bookshopPlaceholder.transform,
            "返回标题",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -122f),
            new Vector2(250f, 68f),
            ShowTitlePage);
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
    private void AddButton(
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
    }

    /// <summary>
    /// 显示标题页，并隐藏书坊占位页。
    /// </summary>
    private void ShowTitlePage()
    {
        titlePage.SetActive(true);
        bookshopPlaceholder.SetActive(false);
    }

    /// <summary>
    /// 显示书坊占位页，并隐藏标题页。
    /// </summary>
    private void ShowBookshopPlaceholder()
    {
        titlePage.SetActive(false);
        bookshopPlaceholder.SetActive(true);
    }
}
