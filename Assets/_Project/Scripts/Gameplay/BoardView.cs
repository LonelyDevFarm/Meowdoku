using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Gameplay.Input;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

namespace Meowdoku.Gameplay
{
    // Script quản lý toàn bộ Bàn cờ (Board).
    // Gắn vào một GameObject có GridLayoutGroup để tự động xếp các ô.
    public class BoardView : MonoBehaviour, IBoardStateReader, IPointerDownHandler,
        IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
    {
        public Action OnGestureEnded;
        public Action<Vector2, Vector2Int, int> OnGesturePointerStarted;
        public Action<Vector2, int> OnGesturePointerMoved;
        public event Action LayoutChanged;

        [Header("Board Settings")]
        public GameObject cellPrefab;    // Kéo Prefab CellView vào đây
        public Transform cellsContainer; // Nơi chứa các ô (thường là chính GameObject này)

        [Header("Input")]
        [SerializeField] private InputSystemUIInputModule inputSystemUiModule;
        
        [Header("Colors (Palette)")]
        public Color[] regionColors;     // Bảng màu cho các khu vực
        [SerializeField] private Shader roundedBackgroundShader;

        private CellView[,] _cells;
        private readonly Queue<CellView> _cellPool = new Queue<CellView>();
        private int _puzzleSize = 0;
        private int[][] _regions;
        private GameGridUiConfig _gridUiConfig;
        private BoardSizeBigConfig _boardSizeBigConfig;
        private BoardGridOverlayGraphic _gridOverlay;
        private float _visibleBoardPixels = SourceBoardLayout.FixedBoardWidth;
        private float _visibleBoardPixelsOverride;
        private int[][] _solutionCols;
        private int? _activePointerId;
        private readonly PointerPressPositionLatch _pressPositionLatch =
            new PointerPressPositionLatch();
        private InputAction _pointAction;
        private InputAction _leftClickAction;
        private bool _rawMousePressed;
        private bool _rawMouseOwnsGesture;
        private bool _suppressUiMouseSequence;
        private int _mouseSuppressionReleaseFrame = -1;
        private Vector2 _lastRawMousePosition;
        private PointerEventData _rawRaycastEventData;
        private EventSystem _rawRaycastEventSystem;
        private readonly List<RaycastResult> _rawRaycastResults = new List<RaycastResult>(8);
        private int _gridPadding = SourceBoardLayout.BoardPadding;
        private int _gridGap = SourceBoardLayout.CellGap;
        private int _gridSlot = SourceBoardLayout.GridSlot;

        private void OnEnable()
        {
            BindInputActions();
        }

        private void Start()
        {
            BindInputActions();
        }

        // Xóa sạch bàn cờ cũ nếu có (Trả các ô về Pool thay vì Destroy)
        public void ClearBoard()
        {
            CancelActiveGesture();
            if (_cells != null)
            {
                for (int row = 0; row < _cells.GetLength(0); row++)
                {
                    for (int column = 0; column < _cells.GetLength(1); column++)
                    {
                        CellView cell = _cells[row, column];
                        if (cell == null) continue;
                        cell.ReleaseToPool();
                        _cellPool.Enqueue(cell);
                    }
                }
            }
            _cells = null;
            _puzzleSize = 0;
            _regions = null;
            if (_gridOverlay != null) _gridOverlay.gameObject.SetActive(false);
        }

        // Khởi tạo bàn cờ mới dựa trên kích thước và dữ liệu bản đồ
        public void SetupBoard(
            int puzzleSize,
            int[][] regions,
            int[] colorMap,
            IReadOnlyList<int> patternRegions = null,
            RegionColorConfig regionColorConfig = null,
            GameGridUiConfig gridUiConfig = null,
            BoardSizeBigConfig boardSizeBigConfig = null,
            float visibleBoardPixelsOverride = 0f)
        {
            ClearBoard();
            _puzzleSize = puzzleSize;
            _regions = regions;
            _gridUiConfig = gridUiConfig ?? new GameGridUiConfig();
            _boardSizeBigConfig = boardSizeBigConfig ?? new BoardSizeBigConfig();
            _visibleBoardPixelsOverride = Mathf.Max(0f, visibleBoardPixelsOverride);
            _cells = new CellView[puzzleSize, puzzleSize];
            RegionColorResult colorResult = RegionColorPipeline.Resolve(
                puzzleSize,
                regions,
                colorMap,
                patternRegions,
                regionColorConfig);
            regionColors = colorResult.Palette;
            colorMap = colorResult.ColorMap;
            SourceBoardLayout.GridLayout layout = ConfigureGrid(puzzleSize, _gridUiConfig);
            float boardScale = layout.ScaleFor(puzzleSize, _visibleBoardPixels);

            for (int r = 0; r < puzzleSize; r++)
            {
                for (int c = 0; c < puzzleSize; c++)
                {
                    CellView cellView = AcquireCell();
                    cellView.name = $"Cell_{r}_{c}";
                    cellView.PrepareForUse(r, c);
                    
                    // Xác định màu nền của ô dựa vào region (khu vực)
                    int regionIdx = regions[r][c];
                    int colorIdx = colorMap != null && regionIdx >= 0 && regionIdx < colorMap.Length
                                   ? colorMap[regionIdx] 
                                   : regionIdx % regionColors.Length;
                    colorIdx = (colorIdx % regionColors.Length + regionColors.Length) % regionColors.Length;
                    cellView.SetRegionColor(regionColors[colorIdx]);
                    cellView.ConfigureBackgroundShape(
                        ResolveCellCornerRadii(r, c, puzzleSize, boardScale, layout),
                        _gridUiConfig.IsSingleLine());
                    
                    _cells[r, c] = cellView;
                }
            }

            ConfigureGridOverlay(layout, boardScale);
            LayoutChanged?.Invoke();
        }

        private SourceBoardLayout.GridLayout ConfigureGrid(
            int puzzleSize,
            GameGridUiConfig gridUiConfig)
        {
            GridLayoutGroup grid = cellsContainer != null
                ? cellsContainer.GetComponent<GridLayoutGroup>()
                : null;
            RectTransform boardRect = cellsContainer as RectTransform;
            SourceBoardLayout.GridLayout layout =
                SourceBoardLayout.Resolve(puzzleSize, gridUiConfig);
            if (grid == null || boardRect == null || puzzleSize <= 0) return layout;

            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = puzzleSize;

            // Godot places a 100 px cell after a 4 px inset inside every 108 px
            // slot. GridLayoutGroup represents that as 19 px outer padding and
            // 8 px between visible cells; input keeps using the logical 15/108
            // padding/slot contract from board_view.gd.
            _gridPadding = layout.Padding;
            _gridGap = layout.Gap;
            _gridSlot = layout.Slot;
            int visiblePadding = _gridPadding + _gridGap;
            grid.padding = new RectOffset(
                visiblePadding, visiblePadding, visiblePadding, visiblePadding);
            grid.spacing = new Vector2(_gridGap * 2f, _gridGap * 2f);
            grid.cellSize = new Vector2(
                SourceBoardLayout.CellPixels, SourceBoardLayout.CellPixels);

            int intrinsicSize = layout.IntrinsicSizeFor(puzzleSize);
            _visibleBoardPixels = _visibleBoardPixelsOverride > 0f
                ? _visibleBoardPixelsOverride
                : SourceBoardLayout.TargetVisibleWidthFor(
                    puzzleSize, _boardSizeBigConfig);
            float boardScale = layout.ScaleFor(puzzleSize, _visibleBoardPixels);
            boardRect.sizeDelta = new Vector2(intrinsicSize, intrinsicSize);
            boardRect.localScale = new Vector3(boardScale, boardScale, 1f);
            Image boardBackground = boardRect.GetComponent<Image>();
            if (boardBackground != null)
            {
                boardBackground.sprite = null;
                boardBackground.type = Image.Type.Simple;
                boardBackground.color = Color.white;
                int radius = Mathf.RoundToInt(layout.BackgroundCorner / boardScale);
                boardBackground.material = RoundedRectMaterialCache.Get(
                    roundedBackgroundShader,
                    new Vector2(intrinsicSize, intrinsicSize),
                    new Vector4(radius, radius, radius, radius),
                    false);
            }
            return layout;
        }

        private CellView AcquireCell()
        {
            CellView cell = _cellPool.Count > 0
                ? _cellPool.Dequeue()
                : Instantiate(cellPrefab, cellsContainer, false).GetComponent<CellView>();
            cell.transform.SetParent(cellsContainer, false);
            cell.transform.SetAsLastSibling();
            return cell;
        }

        private Vector4 ResolveCellCornerRadii(
            int row,
            int column,
            int puzzleSize,
            float boardScale,
            SourceBoardLayout.GridLayout layout)
        {
            int baseRadius = Mathf.Max(1, Mathf.RoundToInt(10f / boardScale));
            if (_gridUiConfig.IsDifferentCorners())
                baseRadius = _gridUiConfig.DifferenceSizeCellCorners(puzzleSize);
            else if (_gridUiConfig.IsSingleLine() || _gridUiConfig.IsReduceSpacing())
                baseRadius = 0;

            int outerRadius = Mathf.Max(
                1, Mathf.RoundToInt(layout.BackgroundCorner / boardScale));
            int topLeft = baseRadius;
            int topRight = baseRadius;
            int bottomRight = baseRadius;
            int bottomLeft = baseRadius;
            if (_gridUiConfig.IsSingleLine())
            {
                if (row == 0 && column == 0) topLeft = outerRadius;
                if (row == 0 && column == puzzleSize - 1) topRight = outerRadius;
                if (row == puzzleSize - 1 && column == puzzleSize - 1) bottomRight = outerRadius;
                if (row == puzzleSize - 1 && column == 0) bottomLeft = outerRadius;
            }
            return new Vector4(topLeft, topRight, bottomRight, bottomLeft);
        }

        private void ConfigureGridOverlay(
            SourceBoardLayout.GridLayout layout,
            float boardScale)
        {
            if (!_gridUiConfig.IsSingleLine())
            {
                if (_gridOverlay != null) _gridOverlay.gameObject.SetActive(false);
                return;
            }
            if (_gridOverlay == null)
            {
                var root = new GameObject(
                    "GridOverlay",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(LayoutElement),
                    typeof(BoardGridOverlayGraphic));
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.SetParent(cellsContainer, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                root.GetComponent<LayoutElement>().ignoreLayout = true;
                _gridOverlay = root.GetComponent<BoardGridOverlayGraphic>();
            }
            _gridOverlay.gameObject.SetActive(true);
            _gridOverlay.transform.SetAsLastSibling();
            _gridOverlay.Configure(_regions, _puzzleSize, layout.Padding, layout.Slot, boardScale);
        }

        public void PlayGridIntro(float synchronizedDuration = 0f)
        {
            _gridOverlay?.BeginGridIntro(synchronizedDuration);
        }

        // Thay đổi trạng thái của một ô cụ thể khi người chơi bấm vào
        public void SetCellState(int r, int c, CellStateType state, bool playAnim = true)
        {
            if (r < 0 || r >= _puzzleSize || c < 0 || c >= _puzzleSize) return;
            if (_cells == null || _cells[r, c] == null) return;

            _cells[r, c].ChangeState(state, playAnim);
        }

        // Lấy trạng thái của một ô
        public CellStateType GetCellState(int r, int c)
        {
            if (r < 0 || r >= _puzzleSize || c < 0 || c >= _puzzleSize) return CellStateType.EMPTY;
            if (_cells == null || _cells[r, c] == null) return CellStateType.EMPTY;

            return _cells[r, c].GetState();
        }

        // Returns a snapshot using the same row/column layout as Godot's get_board().
        public CellStateType[][] GetBoard()
        {
            var board = new CellStateType[_puzzleSize][];
            for (int r = 0; r < _puzzleSize; r++)
            {
                board[r] = new CellStateType[_puzzleSize];
                for (int c = 0; c < _puzzleSize; c++)
                {
                    board[r][c] = _cells != null && _cells[r, c] != null
                        ? _cells[r, c].GetState()
                        : CellStateType.EMPTY;
                }
            }

            return board;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_suppressUiMouseSequence) return;
            if (_activePointerId.HasValue || eventData.button != PointerEventData.InputButton.Left)
                return;
            Vector2 pressPosition = eventData.pressPosition;
            int eventDeviceId = eventData is ExtendedPointerEventData extended && extended.device != null
                ? extended.device.deviceId
                : -1;
            if (_pressPositionLatch.TryConsume(eventDeviceId, out Vector2 latchedPosition))
                pressPosition = latchedPosition;
            if (!TryScreenPointToBoardPosition(
                    pressPosition,
                    eventData.pressEventCamera,
                    out Vector2 boardPosition))
                return;
            Vector2Int startCell = PointerToCell(boardPosition);
            if (startCell.x < 0) return;
            _activePointerId = eventData.pointerId;
            OnGesturePointerStarted?.Invoke(
                boardPosition,
                startCell,
                NowMilliseconds());
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // Godot forwards every pressed pointer motion without a threshold.
            eventData.useDragThreshold = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_suppressUiMouseSequence) return;
            if (!_activePointerId.HasValue || eventData.pointerId != _activePointerId.Value) return;
            if (TryScreenPointToBoardPosition(
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 boardPosition))
            {
                OnGesturePointerMoved?.Invoke(boardPosition, NowMilliseconds());
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_suppressUiMouseSequence)
            {
                _suppressUiMouseSequence = false;
                _mouseSuppressionReleaseFrame = -1;
                return;
            }
            if (!_activePointerId.HasValue || eventData.pointerId != _activePointerId.Value) return;
            _activePointerId = null;
            OnGestureEnded?.Invoke();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelActiveGesture();
                _rawMousePressed = false;
                _pressPositionLatch.Reset();
            }
        }

        private void OnDisable()
        {
            CancelActiveGesture();
            UnbindInputActions();
        }

        private void LateUpdate()
        {
            if (_mouseSuppressionReleaseFrame < 0 ||
                Time.frameCount <= _mouseSuppressionReleaseFrame)
                return;
            _mouseSuppressionReleaseFrame = -1;
            _suppressUiMouseSequence = false;
        }

        private void BindInputActions()
        {
            if (_pointAction != null || _leftClickAction != null) return;
            if (inputSystemUiModule == null && EventSystem.current != null)
                inputSystemUiModule = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            if (inputSystemUiModule == null) return;

            _pointAction = inputSystemUiModule.point?.action;
            _leftClickAction = inputSystemUiModule.leftClick?.action;
            if (_pointAction != null) _pointAction.performed += HandlePointAction;
            if (_leftClickAction != null)
            {
                _leftClickAction.performed += HandleLeftClickAction;
                _leftClickAction.canceled += HandleLeftClickCanceled;
            }
            InputSystem.onEvent += HandleRawInputEvent;
        }

        private void UnbindInputActions()
        {
            if (_pointAction != null) _pointAction.performed -= HandlePointAction;
            if (_leftClickAction != null)
            {
                _leftClickAction.performed -= HandleLeftClickAction;
                _leftClickAction.canceled -= HandleLeftClickCanceled;
            }
            InputSystem.onEvent -= HandleRawInputEvent;
            _pointAction = null;
            _leftClickAction = null;
            _rawMousePressed = false;
            _rawMouseOwnsGesture = false;
            _suppressUiMouseSequence = false;
            _mouseSuppressionReleaseFrame = -1;
            _rawRaycastEventData = null;
            _rawRaycastEventSystem = null;
            _rawRaycastResults.Clear();
            _pressPositionLatch.Reset();
        }

        private void HandleRawInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!(device is Mouse mouse)) return;

            bool hasButton = mouse.leftButton.ReadValueFromEvent(eventPtr, out float buttonValue);
            bool hasPosition = mouse.position.ReadValueFromEvent(eventPtr, out Vector2 eventPosition);
            if (!hasButton && !hasPosition) return;

            bool wasPressed = _rawMousePressed;
            bool isPressed = hasButton
                ? buttonValue >= InputSystem.settings.defaultButtonPressPoint
                : wasPressed;
            Vector2 position = hasPosition ? eventPosition : mouse.position.ReadValue();

            if (isPressed && !wasPressed)
            {
                _pressPositionLatch.RecordPressPosition(position, device.deviceId);
                TryStartRawMouseGesture(position);
            }
            else if (isPressed && wasPressed && _rawMouseOwnsGesture && hasPosition &&
                     position != _lastRawMousePosition)
            {
                MoveRawMouseGesture(position);
            }

            if (!isPressed && wasPressed)
            {
                if (_rawMouseOwnsGesture && hasPosition && position != _lastRawMousePosition)
                    MoveRawMouseGesture(position);
                EndRawMouseGesture();
                if (_suppressUiMouseSequence)
                    _mouseSuppressionReleaseFrame = Time.frameCount;
            }

            _rawMousePressed = isPressed;
        }

        private void HandlePointAction(InputAction.CallbackContext context)
        {
            if (context.control?.device is Mouse) return;
            _pressPositionLatch.RecordPosition(
                context.ReadValue<Vector2>(),
                context.control?.device?.deviceId ?? -1);
        }

        private void HandleLeftClickAction(InputAction.CallbackContext context)
        {
            if (context.control?.device is Mouse) return;
            _pressPositionLatch.RecordButton(
                context.ReadValueAsButton(),
                context.control?.device?.deviceId ?? -1);
        }

        private void HandleLeftClickCanceled(InputAction.CallbackContext context)
        {
            if (context.control?.device is Mouse) return;
            _pressPositionLatch.RecordButton(false, context.control?.device?.deviceId ?? -1);
        }

        private void TryStartRawMouseGesture(Vector2 screenPosition)
        {
            if (_rawMouseOwnsGesture || _activePointerId.HasValue ||
                !IsBoardTopRaycastTarget(screenPosition) ||
                !TryScreenPointToBoardPosition(
                    screenPosition,
                    ResolveBoardEventCamera(),
                    out Vector2 boardPosition))
                return;

            Vector2Int startCell = PointerToCell(boardPosition);
            if (startCell.x < 0) return;

            _rawMouseOwnsGesture = true;
            _suppressUiMouseSequence = true;
            _mouseSuppressionReleaseFrame = -1;
            _lastRawMousePosition = screenPosition;
            OnGesturePointerStarted?.Invoke(boardPosition, startCell, NowMilliseconds());
        }

        private void MoveRawMouseGesture(Vector2 screenPosition)
        {
            _lastRawMousePosition = screenPosition;
            if (TryScreenPointToBoardPosition(
                    screenPosition,
                    ResolveBoardEventCamera(),
                    out Vector2 boardPosition))
                OnGesturePointerMoved?.Invoke(boardPosition, NowMilliseconds());
        }

        private void EndRawMouseGesture()
        {
            if (!_rawMouseOwnsGesture) return;
            _rawMouseOwnsGesture = false;
            OnGestureEnded?.Invoke();
        }

        private bool IsBoardTopRaycastTarget(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (_rawRaycastEventData == null || _rawRaycastEventSystem != eventSystem)
            {
                _rawRaycastEventData = new PointerEventData(eventSystem);
                _rawRaycastEventSystem = eventSystem;
            }
            _rawRaycastEventData.Reset();
            _rawRaycastEventData.position = screenPosition;
            _rawRaycastEventData.button = PointerEventData.InputButton.Left;
            _rawRaycastResults.Clear();
            eventSystem.RaycastAll(_rawRaycastEventData, _rawRaycastResults);
            if (_rawRaycastResults.Count == 0) return false;

            Transform hit = _rawRaycastResults[0].gameObject.transform;
            return hit == transform || hit.IsChildOf(transform);
        }

        private Camera ResolveBoardEventCamera()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }

        private void CancelActiveGesture()
        {
            bool hadGesture = _activePointerId.HasValue || _rawMouseOwnsGesture;
            _activePointerId = null;
            _rawMouseOwnsGesture = false;
            _suppressUiMouseSequence = false;
            _mouseSuppressionReleaseFrame = -1;
            if (hadGesture) OnGestureEnded?.Invoke();
        }

        public Vector2Int PointerToCell(Vector2 boardPosition)
        {
            GridLayoutGroup grid = Grid;
            if (grid == null || _puzzleSize <= 0) return new Vector2Int(-1, -1);

            float slotX = _gridSlot;
            float slotY = _gridSlot;
            float x = boardPosition.x - _gridPadding;
            float y = boardPosition.y - _gridPadding;
            if (x < 0f || y < 0f || slotX <= 0f || slotY <= 0f)
                return new Vector2Int(-1, -1);

            int column = Mathf.FloorToInt(x / slotX);
            int row = Mathf.FloorToInt(y / slotY);
            return row >= 0 && row < _puzzleSize && column >= 0 && column < _puzzleSize
                ? new Vector2Int(column, row)
                : new Vector2Int(-1, -1);
        }

        public int PuzzleSize => _puzzleSize;
        public int GridSlotPixels => _gridSlot;
        public int GridPaddingPixels => _gridPadding;
        public int CellPixels => SourceBoardLayout.CellPixels;
        public float VisibleBoardPixels => _visibleBoardPixels;
        public bool UsesEnlargedBoard => _visibleBoardPixels >
            SourceBoardLayout.FixedBoardWidth + 0.01f;

        public bool TryGetCellCenter(
            RectTransform targetSpace,
            int row,
            int column,
            out Vector2 anchoredPosition)
        {
            anchoredPosition = Vector2.zero;
            if (targetSpace == null || _cells == null ||
                row < 0 || row >= _puzzleSize || column < 0 || column >= _puzzleSize)
                return false;
            CellView cell = _cells[row, column];
            RectTransform cellRect = cell != null ? cell.transform as RectTransform : null;
            if (cellRect == null) return false;
            Vector3 world = cellRect.TransformPoint(cellRect.rect.center);
            Camera camera = ResolveBoardEventCamera();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetSpace,
                screen,
                camera,
                out anchoredPosition);
        }

        public bool TryGetCellLayout(
            RectTransform targetSpace,
            int row,
            int column,
            out Vector2 anchoredPosition,
            out Vector2 size)
        {
            size = Vector2.zero;
            if (!TryGetCellCenter(targetSpace, row, column, out anchoredPosition))
                return false;

            CellView cell = _cells[row, column];
            RectTransform cellRect = cell != null ? cell.transform as RectTransform : null;
            if (cellRect == null) return false;
            Vector3[] corners = new Vector3[4];
            cellRect.GetWorldCorners(corners);
            Camera camera = ResolveBoardEventCamera();
            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetSpace, screenMin, camera, out Vector2 localMin) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetSpace, screenMax, camera, out Vector2 localMax))
                return false;
            size = new Vector2(Mathf.Abs(localMax.x - localMin.x),
                Mathf.Abs(localMax.y - localMin.y));
            return true;
        }

        public bool TryGetCellRegionColor(int row, int column, out Color color)
        {
            color = Color.white;
            if (_cells == null || row < 0 || row >= _puzzleSize ||
                column < 0 || column >= _puzzleSize || _cells[row, column] == null)
                return false;
            color = _cells[row, column].RegionColor;
            return true;
        }

        public bool TryGetBoardVerticalBounds(
            RectTransform targetSpace,
            out float top,
            out float bottom)
        {
            top = bottom = 0f;
            RectTransform boardRect = cellsContainer as RectTransform;
            if (targetSpace == null || boardRect == null) return false;
            Vector3[] corners = new Vector3[4];
            boardRect.GetWorldCorners(corners);
            Camera camera = ResolveBoardEventCamera();
            Vector2 bottomScreen = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 topScreen = RectTransformUtility.WorldToScreenPoint(camera, corners[1]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetSpace, bottomScreen, camera, out Vector2 bottomLocal) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetSpace, topScreen, camera, out Vector2 topLocal))
                return false;
            bottom = bottomLocal.y;
            top = topLocal.y;
            return true;
        }

        public bool TryGetBoardTopCenter(
            RectTransform targetSpace,
            out Vector2 anchoredPosition)
        {
            anchoredPosition = Vector2.zero;
            RectTransform boardRect = cellsContainer as RectTransform;
            if (targetSpace == null || boardRect == null) return false;
            Vector3 world = boardRect.TransformPoint(
                new Vector2(boardRect.rect.center.x, boardRect.rect.yMax));
            Camera camera = ResolveBoardEventCamera();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetSpace,
                screen,
                camera,
                out anchoredPosition);
        }

        private GridLayoutGroup Grid => cellsContainer != null
            ? cellsContainer.GetComponent<GridLayoutGroup>()
            : null;

        private bool TryScreenPointToBoardPosition(
            Vector2 screenPosition,
            Camera eventCamera,
            out Vector2 boardPosition)
        {
            boardPosition = Vector2.zero;
            RectTransform rect = cellsContainer as RectTransform;
            if (rect == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rect,
                    screenPosition,
                    eventCamera,
                    out Vector2 local))
                return false;

            boardPosition = new Vector2(local.x - rect.rect.xMin, rect.rect.yMax - local.y);
            return true;
        }

        private static int NowMilliseconds()
        {
            return (int)(Time.unscaledTimeAsDouble * 1000.0);
        }
    }
}
