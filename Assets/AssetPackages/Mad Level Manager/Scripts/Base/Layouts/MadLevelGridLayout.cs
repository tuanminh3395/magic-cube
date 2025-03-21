/*
* Mad Level Manager by Mad Pixel Machine
* http://www.madpixelmachine.com
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MadLevelManager;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if !UNITY_3_5
namespace MadLevelManager {
#endif

[ExecuteInEditMode]
public class MadLevelGridLayout : MadLevelAbstractLayout {

    #region Constants

    #endregion

    #region Fields

    public SetupMethod setupMethod = SetupMethod.Generate;

    public MadSprite rightSlideSprite;
    public MadSprite leftSlideSprite;

    public Vector2 iconScale = Vector2.one;
    public Vector2 iconOffset;

    public Vector2 rightSlideScale = Vector2.one;
    public Vector2 rightSlideOffset;

    public Vector2 leftSlideScale = Vector2.one;
    public Vector2 leftSlideOffset;

    public int gridWidth = 3;
    public int gridHeight = 3;
    public bool limitLevelsPerPage;
    public int levelsPerPage;

    public int pixelsWidth = 720;
    public int pixelsHeight = 578;

    public HorizontalAlign horizontalAlign = HorizontalAlign.Center;
    public VerticalAlign verticalAlign = VerticalAlign.Top;

    public bool pagesOffsetFromResolution = true;
    public float pagesOffsetManual = 1000;

    public PagingMethod pagingMethod = PagingMethod.HorizontalSimple;
    public bool pagingInvert = false;
    public float pagesOffsetPercent = 2f;
    public float pagesZoomScale = 0.5f;

    [HideInInspector] [NonSerialized] public bool dirty;
    [HideInInspector] [NonSerialized] public bool deepClean;
    private int hash; // for dirtness check

    private MadDragStopDraggable draggable;
    private MadSprite slideLeft, slideRight;

    private List<Page> pages = new List<Page>();
    private int pageCurrentIndex = 0;

    // hides managed objects for user to not look into it
    public bool hideManagedObjects = true;


    // deprecated    
    [SerializeField] private Vector2 _iconScale = Vector2.one;

    #endregion

    #region Properties

    private float pagesXOffset {
        get {
            if (pagesOffsetFromResolution) {
                var root = MadTransform.FindParent<MadRootNode>(transform);
                var loc = root.ScreenGlobal(1, 1);

                loc = transform.InverseTransformPoint(loc);

                return loc.x * 2;
            } else {
                return pagesOffsetManual;
            }
        }
    }

    private float pagesYOffset {
        get {
            if (pagesOffsetFromResolution) {
                var root = MadTransform.FindParent<MadRootNode>(transform);
                var loc = root.ScreenGlobal(1, 1);

                loc = transform.InverseTransformPoint(loc);

                return loc.y * 2;
            } else {
                return pagesOffsetManual;
            }
        }
    }

    private Page currentPage {
        get { return pages[pageCurrentIndex]; }
    }

    #endregion

    #region Method Overrides

    public override MadLevelIcon GetIcon(string levelName) {
        var icon = MadTransform.FindChild<MadLevelIcon>(transform, (i) => i.level.name == levelName);
        if (icon != null) {
            return icon;
        }

        return null;
    }

    public override MadLevelIcon FindClosestIcon(Vector3 position) {
        MadLevelIcon closestIcon = null;
        float closestDistance = float.MaxValue;

        var icons = MadTransform.FindChildren<MadLevelIcon>(transform);
        foreach (var icon in icons) {
            float distance = Vector3.Distance(position, icon.transform.position);
            if (distance < closestDistance) {
                closestIcon = icon;
                closestDistance = distance;
            }
        }

        return closestIcon;
    }

    public override void LookAtIcon(MadLevelIcon icon, bool animate = false) {
        int pageIndex = PageIndexForLevel(icon.level.name);
        SwitchPage(pageIndex, !animate);
    }

    // TODO: make this a abstract method
    public void LookAtIconAnimate(MadLevelIcon icon) {
        int pageIndex = PageIndexForLevel(icon.level.name);
        SwitchPage(pageIndex, false);
    }

    public override MadLevelIcon GetCurrentIcon() {
        if (gridWidth * gridHeight == 1) {
            return pages[pageCurrentIndex].icons[0];
        }

        return base.GetCurrentIcon();
    }

    public override void Activate(MadLevelIcon icon) {
        if (currentPage.ContainsIcon(icon)) {
            base.Activate(icon);
        } else {
            // go to selected page
            var activeIcon = GetActiveIcon();
            if (activeIcon != null && icon != activeIcon) {
                DeactivateActiveIcon();
            }

            var page = FindPageWithIcon(icon);
            if (page != null) {
                var dragStopIndex = page.dragStopIndex;
                if (currentPage.dragStopIndex < dragStopIndex) {
                    GoToNextPage();
                } else {
                    GoToPrevPage();
                }
            }
        }
    }

    private Page FindPageWithIcon(MadLevelIcon icon) {
        for (int i = 0; i < pages.Count; i++) {
            var page = pages[i];
            if (page.ContainsIcon(icon)) {
                return page;
            }
        }

        return null;
    }

    #endregion

    #region Methods Slots

    private void OnValidate() {
        gridWidth = Mathf.Max(1, gridWidth);
        gridHeight = Mathf.Max(1, gridHeight);
        pixelsWidth = Mathf.Max(1, pixelsWidth);
        pixelsHeight = Mathf.Max(1, pixelsHeight);

        if (!limitLevelsPerPage) {
            levelsPerPage = gridWidth * gridHeight;
        } else {
            levelsPerPage = Mathf.Clamp(levelsPerPage, 1, gridWidth * gridHeight);
        }
    }

    protected override void OnEnable() {
        base.OnEnable();
        Upgrade();
    }

    private void Upgrade() {
        // upgrading icon scale when it was set to other value than default
        if (_iconScale != Vector2.one) {
            iconScale = _iconScale;
            _iconScale = Vector2.one;
        }
    }

    protected override void Start() {
        UpdateLayout(false);
        base.Start();
    }

    protected override void Update() {
        base.Update();

        try {
            MadTransform.registerUndo = false;
            UpdateLayout(false);
        } finally {
            MadTransform.registerUndo = true;
        }

        SlideIconsUpdate();
        UpdatePagingZoom();

        if (!Application.isPlaying) {
            if (!limitLevelsPerPage) {
                levelsPerPage = gridWidth * gridHeight;
            }
        }

        canSwitchPagesInThisFrame = true;
    }

    private void UpdatePagingZoom() {
        if (pagingMethod != PagingMethod.HorizontalZoom && pagingMethod != PagingMethod.VerticalZoom) {
            return;
        }

        var progress = draggable.GetProgress();

        for (int i = 0; i < pages.Count; i++) {
            var page = pages[i];
            float distance = Mathf.Abs(progress - i);
            float scaleChange = pagesZoomScale * distance;
            page.transform.localScale = new Vector3(
                Mathf.Clamp01(1 - scaleChange),
                Mathf.Clamp01(1 - scaleChange),
                Mathf.Clamp01(1 - scaleChange)
                );

            page.transform.localPosition = new Vector3(
                page.transform.localPosition.x,
                page.transform.localPosition.y,
                300 * distance);
        }
    }

    private void UpdateLayout(bool forceDelete) {
        if (IsDirty()) {
            bool deep = !Application.isPlaying && (forceDelete || deepClean || generate);
            CleanUp(deep);
            Build(deep);
            MakeClean();

            currentConfiguration.callbackChanged = () => {
                if (this != null) {
                    CleanUp(generate);
                    Build(generate);
                    MakeClean();
                }
            };
        }
    }

    #endregion
    
    #region SlideIcons
    void SlideIconsUpdate() {
        if (slideLeft == null || slideRight == null) {
            return;
        }

        SlideSetActive(slideLeft, HasPrevPage());
        SlideSetActive(slideRight, HasNextPage());
        
        if (draggable.dragging) {
            SlideIconsHide();
        }
    }

    void SlideIconsHide() {
        SlideSetActive(slideLeft, false);
        SlideSetActive(slideRight, false);
    }

    static void SlideSetActive(MadSprite icon, bool act) {
        if (!MadGameObject.IsActive(icon.gameObject)) {
            MadGameObject.SetActive(icon.gameObject, true);
        }

        bool currentlyActive = icon.visible;

        if (currentlyActive != act) {
            icon.visible = act;
        }
    }

    #endregion

    #region Pages

    bool HasNextPage() {
        return pageCurrentIndex + 1 < pages.Count;
    }
    
    bool HasPrevPage() {
        return pageCurrentIndex > 0;
    }

    // guard for clicking on two pages at once (one under another)
    private bool canSwitchPagesInThisFrame = true;

    void GoToNextPage() {
        if (canSwitchPagesInThisFrame) {
            SwitchPage(pageCurrentIndex + 1, false);
            canSwitchPagesInThisFrame = false;
        }
    }
    
    void GoToPrevPage() {
        if (canSwitchPagesInThisFrame) {
            SwitchPage(pageCurrentIndex - 1, false);
            canSwitchPagesInThisFrame = false;
        }
    }
    
    void SwitchPage(int newIndex, bool now) {
        MadDebug.Assert(newIndex >= 0 && newIndex < pages.Count, "There's no page with index " + newIndex);
        pageCurrentIndex = newIndex;
        draggable.MoveTo(newIndex, now);
    }
    
    int PageIndexForLevel(string levelName) {
        int index = currentConfiguration.FindLevelIndex(MadLevel.Type.Level, configurationGroup, levelName);
        int levelsPerPage = gridWidth * gridHeight;
        int pageIndex = index / levelsPerPage;
        return pageIndex;
    }

    #endregion

    #region Methods

    private bool IsDirty() {
        int currentHash = ComputeHash();

        if (dirty) {
            hash = currentHash;
            return true;
        }

        if (currentConfiguration == null || iconTemplate == null) {
            hash = currentHash;
            return false;
        }

        if (hash != currentHash) {
            hash = currentHash;
            return true;
        }

        return false;
    }

    private int ComputeHash() {
        var h = MadHashCode.FirstPrime;
        h = MadHashCode.Add(h, currentConfiguration);
        h = MadHashCode.Add(h, configurationGroup);
        h = MadHashCode.Add(h, hideManagedObjects);
        h = MadHashCode.Add(h, limitLevelsPerPage);
        h = MadHashCode.Add(h, levelsPerPage);
        h = MadHashCode.Add(h, (int) setupMethod);
        h = MadHashCode.Add(h, iconTemplate);
        h = MadHashCode.Add(h, iconScale);
        h = MadHashCode.Add(h, iconOffset);
        h = MadHashCode.Add(h, leftSlideSprite);
        h = MadHashCode.Add(h, leftSlideScale);
        h = MadHashCode.Add(h, leftSlideOffset);
        h = MadHashCode.Add(h, rightSlideSprite);
        h = MadHashCode.Add(h, rightSlideScale);
        h = MadHashCode.Add(h, rightSlideOffset);
        h = MadHashCode.Add(h, gridWidth);
        h = MadHashCode.Add(h, gridHeight);
        h = MadHashCode.Add(h, (int) horizontalAlign);
        h = MadHashCode.Add(h, (int)verticalAlign);
        h = MadHashCode.Add(h, pixelsWidth);
        h = MadHashCode.Add(h, pixelsHeight);
        h = MadHashCode.Add(h, pagesOffsetManual);
        h = MadHashCode.Add(h, pagesOffsetFromResolution);
        h = MadHashCode.Add(h, (int)enumerationType);
        h = MadHashCode.Add(h, enumerationOffset);
        h = MadHashCode.Add(h, (int)pagingMethod);
        h = MadHashCode.Add(h, pagingInvert);
        h = MadHashCode.Add(h, pagesOffsetPercent);

        return h;
    }

    private void MakeClean() {
        dirty = false;
        deepClean = false;
    }

    private void CleanUp(bool forceDelete) {
        int levelCount = currentConfiguration.LevelCount(MadLevel.Type.Level, configurationGroup);
        var children = MadTransform.FindChildren<MadLevelIcon>(transform, (icon) => icon.hasLevelConfiguration);

        if (forceDelete) {
            foreach (var child in children) {
                DestroyImmediate(child.gameObject);
            }

            // remove all pages
            var pages = MadTransform.FindChildren<Transform>(transform, (t) => t.name.StartsWith("Page "));
            foreach (var page in pages) {
                DestroyImmediate(page.gameObject);
            }
        } else {
            var sorted = from c in children orderby c.levelIndex ascending select c;

            foreach (MadLevelIcon levelIcon in sorted) {
                if (levelIcon.levelIndex >= levelCount) {
                    // disable leftovers
                    MadGameObject.SetActive(levelIcon.gameObject, false);
                }
            }
        }

        // remove slides
        ClearSlide("SlideLeftAnchor");
        ClearSlide("SlideRightAnchor");
    }

    // builds level icons that are absent now
    private void Build(bool forceDelete) {
        // create or get a draggable
        draggable = MadTransform.GetOrCreateChild<MadDragStopDraggable>(transform, "Draggable");

        draggable.dragStopCallback = OnDragStopCallback;

        float startX = -pixelsWidth / 2;
        float startY = pixelsHeight / 2;

        float dx = pixelsWidth / (gridWidth + 1);
        float dy = -pixelsHeight / (gridHeight + 1);

        MadLevelIcon previousIcon = null;

        int levelCount = currentConfiguration.LevelCount(MadLevel.Type.Level, configurationGroup);
        int levelIndex = 0;

        int pageIndex = 0;

        while (levelIndex < levelCount) {
            Transform page = MadTransform.FindChild<Transform>(draggable.transform,
                (t) => t.name == "Page " + (pageIndex + 1));
            bool createPageInstance = page == null;

            if (createPageInstance) {
                page = MadTransform.CreateChild<Transform>(draggable.transform, "Page " + (pageIndex + 1));
                page.hideFlags = generate && hideManagedObjects ? HideFlags.HideInHierarchy : 0;
            }

            Transform pageAnchor = MadTransform.FindChild<Transform>(draggable.transform,
                (t) => t.name == "Anchor " + (pageIndex + 1));
            if (pageAnchor == null) {
                pageAnchor = MadTransform.CreateChild<Transform>(draggable.transform, "Anchor " + (pageIndex + 1));
                pageAnchor.gameObject.AddComponent<MadFollow>();
            }

            var followPage = pageAnchor.GetComponent<MadFollow>();
            followPage.followTransform = page;

            if (createPageInstance || generate) {
                switch (pagingMethod) {
                    case PagingMethod.HorizontalSimple:
                        page.localPosition = ComputePageOffsetHoriz(pageIndex);
                        break;
                    case PagingMethod.VerticalSimple:
                        page.localPosition = ComputePageOffsetVert(pageIndex);
                        break;
                    case PagingMethod.HorizontalZoom:
                        page.localPosition = ComputePageOffsetHoriz(pageIndex);
                        break;
                    case PagingMethod.VerticalZoom:
                        page.localPosition = ComputePageOffsetVert(pageIndex);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
            }

            int levelPageIndex = 0;
            bool stop = false;
            for (int y = 1; y <= gridHeight && levelIndex < levelCount; ++y) {
                if (stop) {
                    break;
                }

                for (int x = 1; x <= gridWidth && levelIndex < levelCount; ++x, levelIndex++, levelPageIndex++) {
                    if (limitLevelsPerPage && generate && levelPageIndex == levelsPerPage) {
                        stop = true;
                        break;
                    }

                    // update info: in previous versions page was nested directly under draggable
                    // now they should be placed inside "Page X" transforms
                    MadLevelIcon levelIcon = null;

                    if (!forceDelete) {
                        // look directly under Draggable
                        levelIcon = MadTransform.FindChild<MadLevelIcon>(
                            draggable.transform, (ic) => ic.levelIndex == levelIndex, 0);
                        if (levelIcon != null) {
                            // move to page
                            levelIcon.transform.parent = page;
                        } else {
                            // find under the page
                            levelIcon = MadTransform.FindChild<MadLevelIcon>(
                                page.transform, (ic) => ic.levelIndex == levelIndex, 0);
                        }
                    }

                    var level = currentConfiguration.GetLevel(MadLevel.Type.Level, configurationGroup, levelIndex);
                    bool createNewInstance = levelIcon == null;

                    if (createNewInstance) {
                        levelIcon = CreateIcon(
                            page.transform, level.name, iconTemplate);
                    } else {
                        levelIcon.name = level.name;
                    }

                    levelIcon.gameObject.hideFlags = generate && hideManagedObjects ? HideFlags.HideInHierarchy : 0;

                    levelIcon.levelGroup = configurationGroup;
                    levelIcon.levelIndex = levelIndex;
                    levelIcon.configuration = currentConfiguration;
                    levelIcon.hasLevelConfiguration = true;

                    if (!MadGameObject.IsActive(levelIcon.gameObject)) {
                        MadGameObject.SetActive(levelIcon.gameObject, true);
                    } else {
                        // re-enable icon to reload its properties
                        MadGameObject.SetActive(levelIcon.gameObject, false);
                        MadGameObject.SetActive(levelIcon.gameObject, true);
                    }

                    if (generate || createNewInstance) {
                        levelIcon.pivotPoint = MadSprite.PivotPoint.Center;

                        if (!generate) {
                            levelIcon.transform.localPosition =
                                new Vector3(startX + dx * x + iconOffset.x, startY + dy * y + iconOffset.y, 0);
                        } else {
                            levelIcon.transform.localPosition = IconGeneratedPosition(levelIndex, levelCount, x - 1,
                                y - 1);
                        }

                        levelIcon.transform.localScale = new Vector3(iconScale.x, iconScale.y, 1);

                        if (levelIcon.levelNumber != null) {
                            levelIcon.levelNumber.text = GetEnumerationValue(levelIndex);
                        }
                    }

                    if (previousIcon != null) {
                        if (createNewInstance) {
                            previousIcon.unlockOnComplete.Add(levelIcon);
                        }
                    }

                    if (!Application.isPlaying || !MadLevelProfile.IsLockedSet(level.name)) {
                        levelIcon.locked = levelIcon.level.lockedByDefault;
                    }

                    previousIcon = levelIcon;
                }
            }

            pageIndex++;
        }

        BuildSlideIcons();
        BuildDragging();

        // enable/disable selection based on hide settings
        var sprites = GetComponentsInChildren<MadSprite>();
        foreach (var sprite in sprites) {
            sprite.editorSelectable = !generate;
        }

#if UNITY_EDITOR
        EditorApplication.delayCall += () => EditorUtility.SetDirty(this);
#endif
    }

    private Vector3 ComputePageOffsetHoriz(int pageIndex) {
        if (!pagesOffsetFromResolution) {
            return new Vector3(pagesXOffset * pageIndex * (pagingInvert ? -1 : 1), 0, 0);
        } else {
            return new Vector3(pagesXOffset * (pagesOffsetPercent / 2) * pageIndex * (pagingInvert ? -1 : 1), 0, 0);
        }
    }

    private Vector3 ComputePageOffsetVert(int pageIndex) {
        if (!pagesOffsetFromResolution) {
            return new Vector3(0, -pagesYOffset * pageIndex * (pagingInvert ? -1 : 1), 0);
        } else {
            return new Vector3(0, -pagesYOffset * (pagesOffsetPercent / 2) * pageIndex * (pagingInvert ? -1 : 1), 0);
        }
    }

    private void OnDragStopCallback(int index) {
        if (Application.isPlaying) {
            pageCurrentIndex = index;
        }
    }

    private Vector3 IconGeneratedPosition(int levelIndex, int levelCount, int xIndex, int yIndex) {
        float startX = -pixelsWidth / 2;
        float endX = pixelsWidth / 2;
        float startY = pixelsHeight / 2;
        float endY = -pixelsHeight / 2;

        float dx = pixelsWidth / (gridWidth + 1);
        float dy = -pixelsHeight / (gridHeight + 1);

        int pageSize = gridWidth * gridHeight;
        int pageIndex = (levelIndex / pageSize);

        int levelsOnPage = Mathf.Min(levelCount - pageIndex * pageSize, pageSize);
        if (limitLevelsPerPage && generate) {
            levelsOnPage = levelsPerPage;
        }

        int iconsInColumn = (int) Mathf.Ceil(levelsOnPage / (float) gridWidth);

        int iconsInRow;
        if (yIndex < levelsOnPage / gridWidth) {
            iconsInRow = gridWidth;
        } else {
            iconsInRow = levelsOnPage % gridWidth;
        }

        //Debug.Log(levelsOnPage);

        float x = dx * xIndex + iconOffset.x;
        float y = dy * yIndex + iconOffset.y;

        float xMax = dx * (iconsInRow - 1);
        float yMax = dy * (iconsInColumn - 1);

        switch (horizontalAlign) {
            case HorizontalAlign.Left:
                x = startX + dx + x;
                break;
            case HorizontalAlign.Center:
                x -= xMax / 2;
                break;
            case HorizontalAlign.Right:
                x = endX - dx - (xMax - x);
                break;
        }

        switch (verticalAlign) {
            case VerticalAlign.Top:
                y = startY + dy + y;
                break;
            case VerticalAlign.Middle:
                y -= yMax / 2;
                break;
            case VerticalAlign.Bottom:
                y = endY - dy - (yMax - y);
                break;
        }

        //x -= xMax / 2;
        //y -= yMax / 2;

        return new Vector3(x, y, 0);

        //new Vector3(startX + dx * xIndex + iconOffset.x, startY + dy * yIndex + iconOffset.y, 0);
    }

    private void BuildDragging() {
        var pages = MadTransform.FindChildren<Transform>(draggable.transform, (t) => t.name.StartsWith("Page"), 0);
        pages.Sort((a, b) => {
            var aNum = int.Parse(a.name.Split(' ')[1]);
            var bNum = int.Parse(b.name.Split(' ')[1]);
            return aNum.CompareTo(bNum);
        });

        draggable.ClearDragStops();

        this.pages.Clear();
        for (int i = 0; i < pages.Count; ++i) {
            int dragStopIndex = draggable.AddDragStop(pages[i].localPosition.x, pages[i].localPosition.y);
            var page = new Page(dragStopIndex, pages[i].transform);
            this.pages.Add(page);
        }

        switch (pagingMethod) {
            case PagingMethod.HorizontalSimple:
                draggable.direction = MadDragStopDraggable.Direction.Horizontal;
                break;
            case PagingMethod.VerticalSimple:
                draggable.direction = MadDragStopDraggable.Direction.Vertical;
                break;
            case PagingMethod.HorizontalZoom:
                draggable.direction = MadDragStopDraggable.Direction.Horizontal;
                break;
            case PagingMethod.VerticalZoom:
                draggable.direction = MadDragStopDraggable.Direction.Vertical;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        draggable.directionInvert = pagingInvert;
    }

    private void BuildSlideIcons() {
        if (leftSlideSprite == null || rightSlideSprite == null) {
            return;
        }

        slideLeft = BuildSlide(leftSlideSprite, "SlideLeftAnchor", true);
        slideRight = BuildSlide(rightSlideSprite, "SlideRightAnchor", false);

        slideLeft.transform.localScale = new Vector3(leftSlideScale.x, leftSlideScale.y, 1);
        slideRight.transform.localScale = new Vector3(rightSlideScale.x, rightSlideScale.y, 1);

        slideLeft.transform.localPosition += (Vector3) leftSlideOffset;
        slideRight.transform.localPosition += (Vector3) rightSlideOffset;

        MadSprite.Action goToPrevPage = (sprite) => {
            if (HasPrevPage()) {
                GoToPrevPage();
            }
        };
        slideLeft.onTap += goToPrevPage;
        slideLeft.onMouseUp += goToPrevPage;

        MadSprite.Action goToNextPage = (sprite) => {
            if (HasNextPage()) {
                GoToNextPage();
            }
        };
        slideRight.onTap += goToNextPage;
        slideRight.onMouseUp += goToNextPage;
    }

    private void ClearSlide(string anchorName) {
        MadAnchor slideAnchor = MadTransform.FindChildWithName<MadAnchor>(transform, anchorName);
        if (slideAnchor != null) {
            DestroyImmediate(slideAnchor.gameObject);
        }
    }

    private MadSprite BuildSlide(MadSprite template, string anchorName, bool left) {
        var slideAnchor = MadTransform.CreateChild<MadAnchor>(transform, anchorName);
        if (hideManagedObjects) {
            slideAnchor.gameObject.hideFlags = HideFlags.HideInHierarchy;
        }

        slideAnchor.position = left ? MadAnchor.Position.Left : MadAnchor.Position.Right;
        slideAnchor.Update(); // explict update call because position has changed

        var offset = MadTransform.CreateChild(slideAnchor.transform, "Offset");
        offset.transform.localPosition =
            new Vector3(left ? template.texture.width / 2 : -template.texture.width / 2, 0, 0);

        var slide = MadTransform.CreateChild<MadSprite>(offset.transform, "slide", template);
        slide.transform.localScale = Vector3.one;
        slide.transform.localPosition = Vector3.zero;
        slide.guiDepth = 50;

        return slide;
    }

    private bool generate {
        get { return setupMethod == SetupMethod.Generate; }
    }

    #endregion

    #region Inner Types

    private class Page {
        public readonly Transform transform;

        public MadSprite[] sprites;
        public MadLevelIcon[] icons;

        public int dragStopIndex { get; private set; }

        public Page(int dragStopIndex, Transform transform) {
            this.transform = transform;
            this.dragStopIndex = dragStopIndex;

            sprites = transform.GetComponentsInChildren<MadSprite>();
            icons = transform.GetComponentsInChildren<MadLevelIcon>();
        }

        public bool ContainsIcon(MadLevelIcon icon) {
            return Array.IndexOf(icons, icon) != -1;
        }
    }

    public enum SetupMethod {
        Generate,
        Manual,
    }

    public enum HorizontalAlign {
        Left,
        Center,
        Right,
    }

    public enum VerticalAlign {
        Top,
        Middle,
        Bottom,
    }

    public enum PagingMethod {
        HorizontalSimple,
        VerticalSimple,
        HorizontalZoom,
        VerticalZoom,
    }

    #endregion


}

#if !UNITY_3_5
} // namespace
#endif