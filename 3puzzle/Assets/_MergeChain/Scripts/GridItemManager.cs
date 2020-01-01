using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MergeChain
{
    [System.Serializable]
    public class GridPosition
    {
        [Tooltip("X axis of grid.\n- Value = 0, grid is local center follow x axis.\n- Negative value: it move left.\n- Positive value: it move right.")]
        public float xValue;
        [Tooltip("Y axis of grid.\n- Value = 0, grid is local center follow y axis.\n- Negative value: it move down.\n- Positive value: it move up.")]
        public float yValue = 1;
    }

    public enum DisplayContent
    {
        Numbers,
        Characters
    }

    public class GridItemManager : MonoBehaviour
    {
        public static GridItemManager Instance { get; private set; }

        public static event System.Action GridOver;

        [Header("Object referrences")]
        public GameObject gridItem;
        public GameObject matchItemPrefab;
        public GameObject rewardEffectPrefab;

        [Range(0.01f, 1)]
        public float powerUpFrequency = 0.01f;

        [Header("Option about way to display content for items.")]
        public DisplayContent displayWay;

        [Header("Min and Max of item's score value")]
        public int minValueRandom;
        public int maxValueRandom;
        public int distanceBetweenMinAndMax;
        //Min distance between minValue and maxValue

        [Header("Min number of items must be combined")]
        [SerializeField]
        private int minCellMustCombine;

        [Header("Custom item in Grid")]
        public float itemScale = 0.8f;

        [SerializeField]
        private float spaceBetweenItems = 0.1f;

        [Header("Size of Grid")]
        [SerializeField]
        private int columnCount;
        [SerializeField]
        private int rowCount;

        [Header("Animation for move down items")]
        [SerializeField]
        private AnimationCurve moveEaseType;

        [Header("Amimation for scale item")]
        [SerializeField]
        private AnimationCurve scaleEaseType;

        List<MatchItem> itemsList;
        //Contain all items of grid
        List<GameObject> itemsSpawnNeedMoveDown;
        //List contain items which have just spawned and need move down
        List<MatchItem> itemsCanCombine = new List<MatchItem>();
        //Contain items can combine whenever check game over is false
        public static List<GameObject> itemListWasDragged;

        [Header("Item")]
        public float itemMovementSpeed = 10;
        public float scrambleLockTime = 2;
        public float levelUpScaleIndex = 1.4f;
        public float levelUpTime = 0.7f;

        int[,] maxtrixCountStep;
        //count step of above item whenever player have completed for drag items

        [HideInInspector]
        public bool canPlayGame;
        //decide whether game can play
        [HideInInspector]
        public int currentMaxID;
        //current max score of game
        [HideInInspector]
        private bool isDraggingItem;
        //check whether player is dragging item
        [SerializeField]
        private Cell[,] cells;

        const string TEMP_MIN_VALUE = "TMP_NEW_VALUE";
        const string TEMP_MAX_VALUE = "TMP_MAX_VALUE";
        const string IS_NEW_GAME = "IS_NEW_GAME";

        [Header("Minimize item when drag")]
        public bool minimizeItemDragged = true;
        [Range(1, 3)]
        public float scaleProportion = 1.5f;

        [Header("Allow hint for player and its stats")]
        [SerializeField]
        private bool isAllowHint;
        [Tooltip("Delay time and times do animation when hint time")]
        [SerializeField]
        private float delayTimeDoAnim;
        //Time delay to do animation for items can combine
        [SerializeField]
        private int numberTimesDoAnim;
        //number of times do animation for hint
        [Tooltip("Waiting time to do hint for player")]
        [SerializeField]
        private int waitingTimeSecondToHint;
        //Time waiting to hint for player

        [Header("Grid position. Mouse focus it's properties to see tooltip guide")]
        public GridPosition gridPosition;

        private Coroutine destroyPowerupCoroutine;
        private Coroutine swapPowerupCoroutine;
        private Coroutine scramblePowerupCoroutine;
        private bool isScrambling = false;
        private float posIndex;

        /// <summary>
        /// Raises the validate event.
        /// </summary>
        void OnValidate()
        {
            //Increment for max value whenever it less than total value of minValue and minDistance them
            if (maxValueRandom - minValueRandom > distanceBetweenMinAndMax)
            {
                maxValueRandom = minValueRandom + distanceBetweenMinAndMax;
            }
            if (minValueRandom < 0)
            {
                minValueRandom = 0;
                maxValueRandom = 1;
            }
            if (maxValueRandom < minValueRandom + 1)
                maxValueRandom = minValueRandom + 1;
            if (minCellMustCombine < 2)
                minCellMustCombine = 2;
            if (minCellMustCombine > 8)
                minCellMustCombine = 8;

            if (TempRowCount != rowCount || TempColumnCount != columnCount || TempMaxValue != maxValueRandom || TempMinValue != minValueRandom || TempMinCellMustCombine != minCellMustCombine)
            {
                TempRowCount = rowCount;
                TempColumnCount = columnCount;
                TempMinValue = minValueRandom;
                TempMaxValue = maxValueRandom;
                TempMinCellMustCombine = minCellMustCombine;
                TempMinCellMustCombine = minCellMustCombine;
                //			TempDisplayTextItem = displayNumberOrAlphabet;
                IsNewGame = true;
            }
            if (waitingTimeSecondToHint <= 0)
            {
                waitingTimeSecondToHint = 1;
            }
            if (delayTimeDoAnim <= 0)
                delayTimeDoAnim = 0.5f;
            if (numberTimesDoAnim <= 0)
                numberTimesDoAnim = 1;
        }

        /// <summary>
        /// Process move camera to grid's center point when size of grid is changed 
        /// </summary>
        void ProcessMoveCamera()
        {
            float x = (float)((float)columnCount - (gridPosition.xValue * (-1) + 1)) / 2;
            float y = (float)((float)rowCount - (gridPosition.yValue * (-1) + 1)) / 2;
            Vector3 newPos = new Vector3(x * itemScale, y * -itemScale, 0);
            GameObject newItem = Instantiate(matchItemPrefab);
            newItem.transform.SetParent(GetParentOfItem());
            newItem.transform.localPosition = newPos;
            newItem.SetActive(false);
            Camera.main.transform.position = new Vector3(newItem.transform.position.x, newItem.transform.position.y, Camera.main.transform.position.z);
            Destroy(newItem);
        }

        /// <summary>
        /// Game is start the first time? If true, it will initial some begin values
        /// </summary>
        /// <returns><c>true</c> if this instance is game start the first time; otherwise, <c>false</c>.</returns>
        void IsGameStartTheFirstTime()
        {
            if (!PlayerPrefs.HasKey("IsGameStartTheFirstTime"))
            {
                PlayerPrefs.SetInt("IsGameStartTheFirstTime", 0);
                TempMinValue = minValueRandom;
                TempMaxValue = maxValueRandom;
                TempRowCount = rowCount;
                TempColumnCount = columnCount;
                IsNewGame = true;
            }
        }

        private void OnDrawGizmos()
        {
            if (cells == null)
                return;
            for (int i = 0; i < cells.GetLength(0); ++i)
            {
                for (int j = 0; j < cells.GetLength(1); ++j)
                {
                    Debug.DrawRay(cells[i, j].positionInParent, Vector3.right * 0.5f, Color.cyan);
                    Debug.DrawRay(cells[i, j].positionInParent, Vector3.down * 0.5f, Color.cyan);
                }
            }
        }

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                DestroyImmediate(gameObject);
            }
            posIndex = itemScale + spaceBetweenItems;
            for (int i = 0; i < GetParentOfItem().childCount; i++)
            {
                Destroy(GetParentOfItem().transform.GetChild(i).gameObject);
            }
            gridItem.SetActive(false);
        }

        void Start()
        {
            if (GameManager.resetGame)
                IsNewGame = true;
        }

        void OnEnable()
        {
            GameManager.GameStateChanged += OnGameStateChanged;
            PowerupManager.PowerupChanged += OnPowerupChanged;
        }

        void OnDisable()
        {
            GameManager.GameStateChanged -= OnGameStateChanged;
            PowerupManager.PowerupChanged -= OnPowerupChanged;
        }

        private void Update()
        {
            if (GameManager.Instance.isPaused == false)
            {
                if (isScrambling) // lock interaction on scrambling
                return;
                if (Input.GetMouseButtonDown(0))
                {
                    BeginDragging();
                }
                if (Input.GetMouseButton(0))
                {
                    Drag();
                }
                if (Input.GetMouseButtonUp(0))
                {
                    EndDragging();
                }
            }
        }

        private void StopCoroutineIfNotNull(ref Coroutine c)
        {
            if (c != null)
            {
                StopCoroutine(c);
                c = null;
            }
        }

        private void OnPowerupChanged(PowerupType newType, PowerupType oldType)
        {
            StopCoroutineIfNotNull(ref destroyPowerupCoroutine);
            StopCoroutineIfNotNull(ref swapPowerupCoroutine);
            StopCoroutineIfNotNull(ref scramblePowerupCoroutine);
            isScrambling = false;

            if (newType == PowerupType.DestroyCell)
            {
                destroyPowerupCoroutine = StartCoroutine(CrDestroyCellPowerup());
            }
            else if (newType == PowerupType.SwapCells)
            {
                swapPowerupCoroutine = StartCoroutine(CrSwapCellsPowerup());
            }
            else if (newType == PowerupType.Scramble)
            {
                scramblePowerupCoroutine = StartCoroutine(CrScramblePowerup());
            }
        }

        private IEnumerator CrDestroyCellPowerup()
        {
            yield return new WaitUntil(() => itemListWasDragged.Count == 1);

            EndDragging(false, false);

            PowerupManager.Instance.SetType(PowerupType.None);
        }

        private IEnumerator CrSwapCellsPowerup()
        {
            yield return new WaitUntil(() => itemListWasDragged.Count == 2);
            MatchItem item1 = itemListWasDragged[0].GetComponent<MatchItem>();
            MatchItem item2 = itemListWasDragged[1].GetComponent<MatchItem>();
            if (item1 != null && item2 != null && item1.ID != item2.ID)
            {
                SwapTwoItem(item1, item2);
                if (PowerupManager.Instance.currentType == PowerupType.SwapCells)
                    PowerupManager.Instance.RemovePowerUps(PowerupManager.Instance.swapCellsCost);
                EndDragging();
            }
            else
            {
                EndDragging();
            }
            PowerupManager.Instance.SetType(PowerupType.None);
        }

        private void SwapTwoItem(MatchItem item1, MatchItem item2)
        {
            Cell tmpCell = item1.cell;
            item1.cell = item2.cell;
            item2.cell = tmpCell;

            ItemPoint tmpPoint = item1.point;
            item1.point = item2.point;
            item2.point = tmpPoint;

            cells[item1.point.x, item1.point.y] = item1.cell;
            cells[item2.point.x, item2.point.y] = item2.cell;

            Vector3 pos1 = item1.transform.position;
            Vector3 pos2 = item2.transform.position;

            StartCoroutine(CrMoveTo(item1.transform, pos2));
            StartCoroutine(CrMoveTo(item2.transform, pos1));

        }

        private void MoveItem(MatchItem item, ItemPoint newAddress)
        {
            cells[item.point.x, item.point.y].cellType = StatusCell.Empty;
            Cell c = cells[newAddress.x, newAddress.y];
            c.cellType = StatusCell.NonEmpty;
            //c.positionInParent = newAddress.ToVector3();
            item.cell = c;
            item.point = newAddress;
            Vector3 targetPos = item.transform.parent.TransformPoint(item.cell.positionInParent);
            StartCoroutine(CrMoveTo(item.transform, targetPos));

        }

        private IEnumerator CrMoveTo(Transform objectToMove, Vector3 target)
        {
            while (objectToMove.position != target)
            {
                objectToMove.position = Vector3.MoveTowards(objectToMove.position, target, itemMovementSpeed * Time.smoothDeltaTime);
                yield return null;
            }
        }

        private IEnumerator CrScramblePowerup()
        {
            PowerupManager.Instance.RemovePowerUps(PowerupManager.Instance.scrambleCost);
            int[] shuffleIndices = Utilities.GenerateShuffleIndices(itemsList.Count);
            for (int i = 0; i < shuffleIndices.Length - 1; i += 2)
            {
                isScrambling = true;
                int index1 = shuffleIndices[i];
                int index2 = shuffleIndices[i + 1];
                if (index1 < itemsList.Count && index2 < itemsList.Count)
                {
                    MatchItem item1 = itemsList[index1];
                    MatchItem item2 = itemsList[index2];
                    SwapTwoItem(item1, item2);
                }
            }

            yield return new WaitForSeconds(scrambleLockTime);
            isScrambling = false;
            PowerupManager.Instance.SetType(PowerupType.None);
        }

        void OnGameStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Prepare)
            {
                gridItem.SetActive(false);
            }
            if (newState == GameState.Playing)
            {
                IsGameStartTheFirstTime();
                if (MaxNumberManager.Instance.MaxNumber < currentMaxID)
                {
                    MaxNumberManager.Instance.UpdateMaxNumber(currentMaxID);
                }
                ProcessMoveCamera();
                gridItem.SetActive(true);
                canPlayGame = true;
                isDraggingItem = false;
                itemsSpawnNeedMoveDown = new List<GameObject>();
                itemListWasDragged = new List<GameObject>();
                cells = new Cell[rowCount, columnCount];
                CreateCellList();
                InitItemGrid();
                if (IsNewGame)
                {
                    IsNewGame = false;
                    ScoreManager.Instance.StoredCurrentScore = 0;
                    ScoreManager.Instance.UpdateScore(ScoreManager.Instance.StoredCurrentScore);
                    DisplayItemGrid();
                }
                else
                {
                    //LoadGridItem();
                }
            }
        }

        /// <summary>
        /// Grid is over
        /// </summary>
        IEnumerator CrResetGrid()
        {
            canPlayGame = false;
            IsNewGame = true;
            currentMaxID = 0;
            yield return new WaitForSeconds(1f);
            if (GridOver != null)
                GridOver();
            yield return new WaitForSeconds(2f);
            gridItem.SetActive(false);
        }

        /// <summary>
        /// Init item Grid
        /// </summary>
        public void InitItemGrid()
        {
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    cells[i, j].InitCell();
                }
            }
        }

        /// <summary>
        /// Display item grid
        /// </summary>
        public void DisplayItemGrid()
        {
            currentMaxID = maxValueRandom;
            itemsList = new List<MatchItem>();
            for (int i = 0; i < rowCount; i++)
            {
                float newY = (columnCount - i + 1) * spaceBetweenItems;
                for (int j = 0; j < columnCount; j++)
                {
                    int numberId = Random.Range(minValueRandom, maxValueRandom + 1);
                    ItemData itemData = ItemDataManager.Instance.ItemDataList[numberId];
                    GameObject newItem = Instantiate(matchItemPrefab);
                    newItem.transform.SetParent(GetParentOfItem());
                    newItem.GetComponent<SpriteRenderer>().color = itemData.color;
                    newItem.transform.localScale = new Vector3(itemScale, itemScale, 1f);
                    Vector3 newPos = new Vector3(j * posIndex, i * -posIndex, 1f);
                    newPos.x = newPos.x - (rowCount - j - 1) * spaceBetweenItems;
                    newPos.y += newY; 
                    newItem.transform.localPosition = newPos;
                    if (!newItem.activeSelf)
                        newItem.SetActive(true);
                    MatchItem item = newItem.GetComponent<MatchItem>();
                    var main = item.par.main;
                    main.startColor = itemData.color;
                    item.cell = cells[i, j];
                    item.cell.positionInParent = newPos;
                    item.point = new ItemPoint(i, j);
                    item.ID = numberId;
                    item.SetItemData(itemData);
                    itemsList.Add(item);
                    if (MaxNumberManager.Instance.MaxNumber < currentMaxID)
                    {
                        MaxNumberManager.Instance.UpdateMaxNumber(currentMaxID);
                    }
                }
            }
            while (CheckGameOver() == true)
            {
                RandomAgainGrid();
            }
            HintFintItemCanCombine();
        }

        /// <summary>
        /// Random grid again if game is new game and it's state is over
        /// </summary>
        void RandomAgainGrid()
        {
            Debug.Log("Load grid again");
            for (int i = 0; i < GetParentOfItem().childCount; i++)
            {
                Destroy(GetParentOfItem().GetChild(i).gameObject);
            }
            cells = new Cell[rowCount, columnCount];
            CreateCellList();
            InitItemGrid();
            DisplayItemGrid();
        }

        /// <summary>
        /// Pick color whenever spawn new item or player made a new one 
        /// </summary>
        /// <returns>The color when first display.</returns>
        /// <param name="itemNumberScore">Item number score.</param>
        //Color InitColorWhenFirstDisplay(int itemNumberScore)
        //{
        //    if (itemNumberScore <= minValueRandom)
        //        return ItemColorManager.Instance.itemColorList[0];
        //    else if (itemNumberScore > minValueRandom)
        //    {
        //        int distanceBetweenMinMax = maxValueRandom - minValueRandom;
        //        if (distanceBetweenMinMax == 1)
        //            return ItemColorManager.Instance.itemColorList[1];              //error
        //        else if (distanceBetweenMinMax == 2)
        //        {
        //            if (itemNumberScore < maxValueRandom)
        //                return ItemColorManager.Instance.itemColorList[1];
        //            else
        //                return ItemColorManager.Instance.itemColorList[2];
        //        }
        //        else if (distanceBetweenMinMax == 3)
        //        {
        //            if (itemNumberScore > minValueRandom)       //minvalue +1 
        //                return ItemColorManager.Instance.itemColorList[1];
        //            else if (itemNumberScore > minValueRandom + 1 && itemNumberScore < maxValueRandom)
        //                return ItemColorManager.Instance.itemColorList[2];
        //            else
        //                return ItemColorManager.Instance.itemColorList[3];
        //        }
        //    }
        //    return ItemColorManager.Instance.itemColorList[0];
        //}

        /// <summary>
        /// Create item Grid
        /// </summary>
        public void CreateCellList()
        {
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    cells[i, j] = new Cell();
                }
            }
        }

        /// <summary>
        /// Remove item in list of UI
        /// </summary>
        /// <param name="itemNeedRemove">Item need remove.</param>
        public MatchItem RemoveAndSpawnNewItem(MatchItem itemNeedRemove)
        {
            foreach (MatchItem item in itemsList)
            {
                if (item.point.CompareItemPoint(itemNeedRemove.point))
                {
                    itemsList.Remove(item);
                    cells[item.point.x, item.point.y].cellType = StatusCell.Empty;
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Move item with animation curve type.
        /// </summary>
        /// <returns>The do swap motion local.</returns>
        /// <param name="itemNeedSwap">Item need swap.</param>
        /// <param name="targetPos">Target position.</param>
        IEnumerator CrDoSwapMotionLocal(GameObject itemNeedSwap, Vector3 targetPos)
        {
            float currentTime = 0;
            float duration = 0.7f;
            Vector3 pos = itemNeedSwap.transform.localPosition;
            while (currentTime <= duration)
            {
                currentTime += Time.deltaTime;
                float t = Mathf.Clamp01(currentTime / duration);
                itemNeedSwap.transform.localPosition = Vector3.Lerp(pos, targetPos, moveEaseType.Evaluate(t));
                yield return null;
            }
        }

        /// <summary>
        /// Browse Grid to calculate maxtrixCell and count empty cell on every column
        /// </summary>
        public void BrowseGrid()
        {
            //Init value matrixCell
            maxtrixCountStep = new int[rowCount, columnCount];
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    maxtrixCountStep[i, j] = 0;
                }
            }
            for (int col = 0; col < columnCount; col++)
            {
                for (int row = 0; row < rowCount; row++)
                {
                    int countEmpty = 0;
                    if (cells[row, col].isEmpty)
                        continue;
                    for (int k = row + 1; k < rowCount; k++)
                    {
                        if (!cells[k, col].isEmpty)
                            continue;
                        else
                            countEmpty++;
                    }
                    maxtrixCountStep[row, col] = countEmpty;
                }
            }
            FillDownByMatrixStep();
        }

        /// <summary>
        /// Swap above cell with downside cell
        /// </summary>
        /// <param name="p1">P1.</param>
        /// <param name="p2">P2.</param>
        void DoSwapCell(ItemPoint p1, ItemPoint p2)
        {
            //GameObject itemNeedSwap = FindItem(p1).gameObject;
            //MatchItem itemComponent = itemNeedSwap.GetComponent<MatchItem>();
            //itemComponent.point = p2;
            //StartCoroutine(CrDoSwapMotionLocal(itemNeedSwap, cells[p2.x, p2.y].positionInParent));
            //Vector3 tempPosA = cells[p1.x, p1.y].positionInParent;
            //Vector3 tempPosB = cells[p2.x, p2.y].positionInParent;
            //Cell tempCell = cells[p1.x, p1.y];
            //cells[p1.x, p1.y] = cells[p2.x, p2.y];
            //cells[p1.x, p1.y].positionInParent = tempPosA;
            //cells[p2.x, p2.y] = tempCell;
            //cells[p2.x, p2.y].positionInParent = tempPosB;
            //itemComponent.cell.cellType = tempCell.cellType;
            MatchItem item1 = FindItem(p1);
            MatchItem item2 = FindItem(p2);
            if (item1 != null && item2 != null)
                SwapTwoItem(item1, item2);
            else if (item1 != null && item2 == null)
                MoveItem(item1, p2);
            else if (item1 == null && item2 != null)
                MoveItem(item2, p1);
        }

        /// <summary>
        /// Fill down above cell by maxtrix step which calculated by BrowseGrid function
        /// </summary>
        void FillDownByMatrixStep()
        {
            for (int row = rowCount - 1; row >= 0; row--)
            {
                for (int col = columnCount - 1; col >= 0; col--)
                {
                    if (maxtrixCountStep[row, col] == 0)
                    {
                        continue;
                    }
                    else
                    {
                        for (int value = 1; value < rowCount; value++)
                        {
                            if (maxtrixCountStep[row, col] == value)
                            {
                                DoSwapCell(new ItemPoint(row, col), new ItemPoint(row + value, col));
                                break;
                            }
                            else
                                continue;
                        }
                    }
                }
            }
            CountEmptyAndSpawn();
        }

        /// <summary>
        /// Count empty cell on grid and spawn new item to fill amount it
        /// </summary>
        void CountEmptyAndSpawn()
        {
            for (int col = 0; col < columnCount; col++)
            {
                int countEmpty = 0;
                for (int row = 0; row < rowCount; row++)
                {
                    if (!cells[row, col].isEmpty)
                        continue;
                    else
                        countEmpty++;
                }
                if (countEmpty == 0)
                    continue;
                for (int i = 1; i <= countEmpty; i++)
                {
                    Vector3 newPos = new Vector3(cells[0, col].positionInParent.x, cells[0, col].positionInParent.y + i * itemScale, 1f);
                    GameObject newItem = SpawnRandomItem(newPos);
                    itemsSpawnNeedMoveDown.Add(newItem);
                }
                MoveItemHaveJustSpawned(col);
            }
        }

        /// <summary>
        /// Return parent of item to set parent for it
        /// </summary>
        /// <returns>The parent of item.</returns>
        Transform GetParentOfItem()
        {
            return transform.GetChild(0).GetChild(0);
        }

        /// <summary>
        /// Move items have just spawned
        /// </summary>
        /// <param name="columnIndex">Column index.</param>
        void MoveItemHaveJustSpawned(int columnIndex)
        {
            for (int i = 0; i < itemsSpawnNeedMoveDown.Count; i++)
            {
                StartCoroutine(CrDoSwapMotionLocal(itemsSpawnNeedMoveDown[i].gameObject, cells[(itemsSpawnNeedMoveDown.Count - 1) - i, columnIndex].positionInParent));
                MatchItem itemComponent = itemsSpawnNeedMoveDown[i].GetComponent<MatchItem>();
                itemComponent.point = new ItemPoint(itemsSpawnNeedMoveDown.Count - 1 - i, columnIndex);
                cells[itemsSpawnNeedMoveDown.Count - 1 - i, columnIndex].cellType = itemComponent.cell.cellType;
                itemComponent.cell = cells[itemsSpawnNeedMoveDown.Count - 1 - i, columnIndex];
            }
            SoundManager.Instance.PlaySound(SoundManager.Instance.fillDownCell);
            itemsSpawnNeedMoveDown = new List<GameObject>();
        }

        /// <summary>
        /// Spawn random new item
        /// </summary>
        /// <returns>The random item.</returns>
        /// <param name="newPos">New position.</param>
        GameObject SpawnRandomItem(Vector3 newPos)
        {
            int numberID = Random.Range(minValueRandom, maxValueRandom + 1);
            ItemData itemData = ItemDataManager.Instance.ItemDataList[numberID];
            GameObject newItem = Instantiate(matchItemPrefab);
            newItem.transform.SetParent(GetParentOfItem());
            newItem.transform.localPosition = newPos;
            newItem.transform.localScale = new Vector3(itemScale, itemScale, 1f);
            newItem.GetComponent<SpriteRenderer>().color = itemData.color;
            MatchItem itemComponent = newItem.GetComponent<MatchItem>();
            itemComponent.cell = new Cell();
            itemComponent.cell.cellType = StatusCell.NonEmpty;
            itemComponent.cell.positionInParent = newPos;
            itemComponent.ID = numberID;
            itemComponent.SetItemData(itemData);
            var main = itemComponent.par.main;
            main.startColor = itemData.color;
            if (!newItem.activeSelf)
                newItem.SetActive(true);
            itemsList.Add(itemComponent);
            return newItem;
        }

        /// <summary>
        /// Spawn new item at last position which combine items
        /// </summary>
        /// <returns>The item with old type.</returns>
        /// <param name="newPos">New position.</param>
        /// <param name="oldItem">Old item.</param>
        /// <param name="totalScore">Total score.</param>
        GameObject SpawnItemWithOldType(Vector3 newPos, MatchItem oldItem, int totalScore)
        {
            int newID = oldItem.ID + 1;
            if (newID > currentMaxID)
            {
                currentMaxID = newID;
                if (MaxNumberManager.Instance.MaxNumber < currentMaxID)
                {
                    MaxNumberManager.Instance.UpdateMaxNumber(newID);
                }
            }
            GameObject newItem = Instantiate(matchItemPrefab);
            ItemData itemData = ItemDataManager.Instance.ItemDataList[newID];
            newItem.transform.SetParent(GetParentOfItem());
            newItem.transform.localPosition = newPos;
            newItem.transform.localScale = new Vector3(itemScale, itemScale, 1f);
            newItem.GetComponent<SpriteRenderer>().color = itemData.color;
            var main = newItem.GetComponent<MatchItem>().par.main;
            main.startColor = itemData.color;
            MatchItem itemComponent = newItem.GetComponent<MatchItem>();
            //itemComponent.cell = new Cell();
            itemComponent.cell = oldItem.cell;
            itemComponent.cell.cellType = StatusCell.NonEmpty;
            Debug.DrawRay(itemComponent.cell.positionInParent, Vector3.one, Color.yellow, 10);
            //itemComponent.cell.positionInParent = newPos;
            itemComponent.point = oldItem.point;
            itemComponent.SetItemData(itemData);
            itemComponent.ID = newID;
            ScoreManager.Instance.AddScore(totalScore);
            if (!newItem.activeSelf)
                newItem.SetActive(true);
            itemsList.Add(itemComponent);
            itemComponent.LevelUp();
            int powerUp = 0;
            if (isPowerUp())
            {
                powerUp = 1;
                PowerupManager.Instance.AddPowerUps(powerUp);
            }
            GameObject rewardEffect = Instantiate(rewardEffectPrefab);
            rewardEffect.GetComponent<RewardEffectController>().SetRewardUI(newItem.transform.position, totalScore, powerUp);
            return newItem;
        }

        /// <summary>
        /// Find item by item point in grid
        /// </summary>
        MatchItem FindItem(ItemPoint itemPoint)
        {
            foreach (MatchItem item in itemsList)
            {
                if (item.point.CompareItemPoint(itemPoint))
                    return item;
            }
            return null;
        }

        /// <summary>
        /// Spawn Item At position which player was dragged
        /// </summary>
        /// <param name="itemLastDrag">Item last drag.</param>
        /// <param name="oldType">Old type.</param>
        /// <param name="totalScore">Total score.</param>
        public GameObject SpawnItemAtPosLastDrag(GameObject itemLastDrag, int totalScoreDragged)
        {
            MatchItem oldItemComponent = itemLastDrag.GetComponent<MatchItem>();
            ItemPoint posLastDrag = oldItemComponent.point;
            Cell cellDragLast = cells[posLastDrag.x, posLastDrag.y];
            if (cellDragLast.isEmpty)
            {
                Vector3 newPos = cellDragLast.positionInParent;
                GameObject newItem = SpawnItemWithOldType(newPos, oldItemComponent, totalScoreDragged);
                cellDragLast.cellType = StatusCell.NonEmpty;
                //cells[posLastDrag.x, posLastDrag.y] = cellDragLast;
                return newItem;
            }
            return null;
        }

        /// <summary>
        /// Check game over by number of user's input
        /// </summary>
        bool CheckWhetherCanContinue(MatchItem firstItem, MatchItem secondItem)
        {
            itemsCanCombine.Add(firstItem);
            itemsCanCombine.Add(secondItem);
            if (minCellMustCombine == 2)
            {
                return false;
            }
            else
            {
                //minCellCombine greater than > 2
                int tempCellChecked = 2;
                MatchItem previousItem = firstItem;
                MatchItem currentItem = secondItem;
                MatchItem nextItem;
                while (tempCellChecked < minCellMustCombine)
                {
                    tempCellChecked++;
                    //continue check on forward of itemCompare
                    if (currentItem.point.y < columnCount - 1)
                    {
                        nextItem = FindItem(new ItemPoint(currentItem.point.x, currentItem.point.y + 1));
                        if (nextItem.point.x != previousItem.point.x || nextItem.point.y != previousItem.point.y)
                        {
                            if (currentItem.ID == nextItem.ID)
                            {
                                itemsCanCombine.Add(nextItem);
                                if (tempCellChecked == minCellMustCombine)
                                    return false;
                                //Continue check
                                previousItem = currentItem;
                                currentItem = nextItem;
                                continue;
                            }
                        }
                    }
                    //continue check on benhind itemCopare
                    if (currentItem.point.y > 0)
                    {
                        nextItem = FindItem(new ItemPoint(currentItem.point.x, currentItem.point.y - 1));
                        if (nextItem.point.x != previousItem.point.x || nextItem.point.y != previousItem.point.y)
                        {
                            if (currentItem.ID == nextItem.ID)
                            {
                                itemsCanCombine.Add(nextItem);
                                if (tempCellChecked == minCellMustCombine)
                                    return false;
                                previousItem = currentItem;
                                currentItem = nextItem;
                                continue;
                            }
                        }
                    }
                    //continue check on above of itemCompare
                    if (currentItem.point.x > 0)
                    {
                        nextItem = FindItem(new ItemPoint(currentItem.point.x - 1, currentItem.point.y));
                        if (nextItem.point.x != previousItem.point.x || nextItem.point.y != previousItem.point.y)
                        {
                            if (currentItem.ID == nextItem.ID)
                            {
                                itemsCanCombine.Add(nextItem);
                                if (tempCellChecked == minCellMustCombine)
                                    return false;
                                previousItem = currentItem;
                                currentItem = nextItem;
                                continue;
                            }
                        }
                    }
                    //continue check on under of itemCompare
                    if (currentItem.point.x < rowCount - 1)
                    {
                        nextItem = FindItem(new ItemPoint(currentItem.point.x + 1, currentItem.point.y));
                        if (nextItem.point.x != previousItem.point.x || nextItem.point.y != previousItem.point.y)
                        {
                            if (currentItem.ID == nextItem.ID)
                            {
                                itemsCanCombine.Add(nextItem);
                                if (tempCellChecked == minCellMustCombine)
                                    return false;
                                previousItem = currentItem;
                                currentItem = nextItem;
                                continue;
                            }
                        }
                    }
                }
            }
            itemsCanCombine = new List<MatchItem>();
            return true;
        }

        /// <summary>
        /// Check whether game over
        /// </summary>
        /// <returns><c>true</c>, if game over was checked, <c>false</c> otherwise.</returns>
        public bool CheckGameOver()
        {
            //Search follow horizontal 
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    int secondCol = col + 1;
                    if (secondCol > columnCount - 1)
                        break;
                    MatchItem firstItem = FindItem(new ItemPoint(row, col));
                    MatchItem secondItem = FindItem(new ItemPoint(row, secondCol));
                    if (firstItem.ID != secondItem.ID)
                        continue;
                    else
                    {
                        if (CheckWhetherCanContinue(firstItem, secondItem) == false)
                            return false;
                    }

                }
            }

            //Search follow vertical
            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    int secondRow = row + 1;
                    if (secondRow > rowCount - 1)
                        break;
                    MatchItem firstItem = FindItem(new ItemPoint(row, col));
                    MatchItem secondItem = FindItem(new ItemPoint(secondRow, col));
                    if (firstItem.ID != secondItem.ID)
                        continue;
                    else
                    {
                        if (CheckWhetherCanContinue(firstItem, secondItem) == false)
                            return false;
                    }
                }
            }

            //Search on horizaltal
            for (int row = rowCount - 1; row >= 0; row--)
            {
                for (int col = columnCount - 1; col >= 0; col--)
                {
                    int secondCol = col - 1;
                    if (secondCol < 0)
                        break;
                    MatchItem firstItem = FindItem(new ItemPoint(row, col));
                    MatchItem secondItem = FindItem(new ItemPoint(row, secondCol));
                    if (firstItem.ID != secondItem.ID)
                    {
                        continue;
                    }
                    else
                    {
                        if (CheckWhetherCanContinue(firstItem, secondItem) == false)
                            return false;
                    }
                }
            }

            //Serach on vertical
            for (int row = rowCount - 1; row >= 0; row--)
            {
                for (int col = columnCount - 1; col >= 0; col--)
                {
                    int secondRow = row - 1;
                    if (secondRow < 0)
                        break;
                    MatchItem firstItem = FindItem(new ItemPoint(row, col));
                    MatchItem secondItem = FindItem(new ItemPoint(secondRow, col));
                    if (firstItem.ID != secondItem.ID)
                        continue;
                    else
                    {
                        if (CheckWhetherCanContinue(firstItem, secondItem) == false)
                            return false;
                    }
                }
            }
            itemsCanCombine = new List<MatchItem>();
            return true;
        }

        void SetupLineRender(GameObject item, Vector2 endPos, Color endColor)
        {
            LineRenderer lineRender = item.GetComponent<LineRenderer>();
            lineRender.positionCount = 2;
            lineRender.useWorldSpace = true;
            lineRender.SetPosition(0, item.transform.position);
            lineRender.SetPosition(1, endPos);
            lineRender.startColor = endColor;
            lineRender.endColor = endColor;
        }

        private void BeginDragging()
        {
            if (canPlayGame)
            {
                if (isDraggingItem == false)
                {
                    RaycastHit2D hit;
                    Vector2 worldTouch = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    hit = Physics2D.Raycast(worldTouch, Vector2.zero, 1);
                    if (hit.collider != null)
                    {
                        if (hit.collider.gameObject.tag == "Item")
                        {
                            SoundManager.Instance.PlaySound(SoundManager.Instance.dragging);
                            for (int i = 0; i < itemsCanCombine.Count; i++)
                            {
                                itemsCanCombine[i].transform.localScale = new Vector3(itemScale, itemScale, 1);
                            }
                            isDraggingItem = true;
                            if (minimizeItemDragged)
                            {
                                float haftOfItemScale = itemScale / scaleProportion;
                                hit.transform.localScale = new Vector3(haftOfItemScale, haftOfItemScale, 1f);
                            }
                            itemListWasDragged.Add(hit.collider.gameObject);
                            if (PowerupManager.Instance.currentType == PowerupType.DestroyCell)
                            {
                                PowerupManager.Instance.RemovePowerUps(PowerupManager.Instance.destroyCellCost);
                            }
                            itemsCanCombine = new List<MatchItem>();
                        }
                    }
                }
            }
        }

        private void Drag()
        {
            if (isDraggingItem)
            {
                FindItemsByMouse();
            }
        }

        private void EndDragging(bool mustCombineEnoughCellToMatch = true, bool spawnNewItemAtLastDragPos = true)
        {
            if (canPlayGame)
            {
                if (isDraggingItem)
                {
                    isDraggingItem = false;
                    int totalScoreDragged = 0;
                    GameObject itemLastDrag = GridItemManager.itemListWasDragged[GridItemManager.itemListWasDragged.Count - 1];
                    for (int i = 0; i < itemListWasDragged.Count; i++)
                    {
                        totalScoreDragged += itemListWasDragged[i].GetComponent<MatchItem>().score;
                    }
                    if ((mustCombineEnoughCellToMatch && itemListWasDragged.Count >= minCellMustCombine) ||
                    !mustCombineEnoughCellToMatch)
                    {
                        SoundManager.Instance.PlaySound(SoundManager.Instance.match);
                        List<MatchItem> removedItems = new List<MatchItem>();
                        for (int i = itemListWasDragged.Count - 1; i >= 0; i--)
                        {
                            MatchItem item = RemoveAndSpawnNewItem(itemListWasDragged[i].GetComponent<MatchItem>());
                            if (item)
                                removedItems.Add(item);
                        }
                        GameObject newItem = null;
                        if (spawnNewItemAtLastDragPos)
                            newItem = SpawnItemAtPosLastDrag(itemLastDrag, totalScoreDragged);

                        if (newItem)
                        {
                            for (int i = 0; i < removedItems.Count; ++i)
                            {
                                StartCoroutine(CrRemoveItemAnim(removedItems[i].transform, newItem.transform));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < removedItems.Count; ++i)
                            {
                                Destroy(removedItems[i].gameObject);
                            }
                        }

                        itemListWasDragged = new List<GameObject>();
                        BrowseGrid();
                        if (CheckGameOver())
                        {
                            StartCoroutine(CrResetGrid());
                        }
                        else
                        {
                            HintFintItemCanCombine();
                        }
                        //SaveGridItem();
                        return;
                    }
                    for (int i = 0; i < itemListWasDragged.Count; i++)
                    {
                        itemListWasDragged[i].transform.localScale = new Vector3(itemScale, itemScale, 1f);
                        itemListWasDragged[i].GetComponent<LineRenderer>().positionCount = 0;
                    }
                    HintFintItemCanCombine();
                    SoundManager.Instance.PlaySound(SoundManager.Instance.dragFailed);
                    itemListWasDragged = new List<GameObject>();
                }
            }
        }

        private IEnumerator CrRemoveItemAnim(Transform removedItem, Transform newSpawnedItem)
        {
            while (newSpawnedItem != null && removedItem != null &&
               newSpawnedItem.position != removedItem.position)
            {
                removedItem.position = Vector3.MoveTowards(removedItem.position, newSpawnedItem.position, itemMovementSpeed * Time.smoothDeltaTime);
                yield return null;
            }
            Destroy(removedItem.gameObject);
        }

        void HintFintItemCanCombine()
        {
            if (isAllowHint)
            {
                if (CheckGameOver() == false)
                {
                    StartCoroutine(CrHintFintItemCanCombine());
                }
            }
        }

        IEnumerator ScaleAnim(GameObject itemNeedScale, Vector3 end, float duration)
        {
            float currentTime = 0;
            Vector3 tempScale = itemNeedScale.transform.localScale;
            while (currentTime < duration)
            {
                if (itemsCanCombine.Count <= 0)
                {
                    itemNeedScale.transform.localScale = new Vector3(itemScale, itemScale, 1);
                    if (itemListWasDragged.Count > 0)
                    {
                        if (itemNeedScale == itemListWasDragged[0])
                            itemNeedScale.transform.localScale = new Vector3(itemScale / scaleProportion, itemScale / scaleProportion, 1);
                    }
                    break;
                }
                currentTime += Time.deltaTime;
                float t = Mathf.Clamp01(currentTime / duration);
                itemNeedScale.transform.localScale = Vector3.Lerp(tempScale, end, scaleEaseType.Evaluate(t));
                yield return null;
            }
            yield return null;
        }

        /// <summary>
        /// Hint for player to find items can combine
        /// </summary>
        IEnumerator CrHintFintItemCanCombine()
        {
            int timesDo = 0;
            yield return new WaitForSeconds(waitingTimeSecondToHint * 1.0f);
            while (true)
            {
                if (itemsCanCombine.Count <= 0)
                    break;
                while (timesDo <= numberTimesDoAnim)
                {
                    for (int i = 0; i < itemsCanCombine.Count; i++)
                    {
                        Vector3 targetScale = new Vector3(itemScale / 1.5f, itemScale / 1.5f, 1f);
                        StartCoroutine(ScaleAnim(itemsCanCombine[i].gameObject, targetScale, delayTimeDoAnim * 2));
                    }
                    yield return new WaitForSeconds(delayTimeDoAnim * 4);
                    for (int i = 0; i < itemsCanCombine.Count; i++)
                    {
                        Vector3 targetScale = new Vector3(itemScale, itemScale, 1f);
                        StartCoroutine(ScaleAnim(itemsCanCombine[i].gameObject, targetScale, delayTimeDoAnim * 2));
                    }
                    timesDo++;
                    yield return new WaitForSeconds(delayTimeDoAnim);
                }
                yield return new WaitForSeconds(delayTimeDoAnim * 8);
                timesDo = 0;
            }
        }

        /// <summary>
        /// Find item from mouse whether it is item
        /// </summary>
        void FindItemsByMouse()
        {
            RaycastHit2D hit;
            Vector2 worldTouch = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            hit = Physics2D.Raycast(worldTouch, Vector2.zero, 1);
            if (hit.collider != null)
            {
                if (hit.collider.gameObject.tag == "Item")
                    CheckMatchItem(hit.collider.gameObject);

            }
            else
            {
                //Color targetColor = ItemColorManager.Instance.itemColorList[itemListWasDragged[itemListWasDragged.Count - 1].GetComponent<MatchItem>().ScoreItem - minValueRandom];
                MatchItem curMatchItem = itemListWasDragged[itemListWasDragged.Count - 1].GetComponent<MatchItem>();
                int curID = curMatchItem.ID;
                Color targetColor = ItemDataManager.Instance.ItemDataList[curID].color;
                SetupLineRender(itemListWasDragged[itemListWasDragged.Count - 1].gameObject, worldTouch, targetColor);
            }
        }

        /// <summary>
        /// Check whether item can continue drag
        /// </summary>
        /// <param name="anotherItem">Another item.</param>
        void CheckMatchItem(GameObject anotherItem)
        {
            float haftOfItemScale = itemScale / scaleProportion;
            MatchItem itemPrevLastDrag = null;
            MatchItem itemNeedCompare = anotherItem.GetComponent<MatchItem>();
            MatchItem itemLastDrag = itemListWasDragged[itemListWasDragged.Count - 1].GetComponent<MatchItem>();
            //Color targetColor = ItemColorManager.Instance.itemColorList[itemListWasDragged[itemListWasDragged.Count - 1].GetComponent<MatchItem>().ScoreItem - minValueRandom];\
            int curID = itemLastDrag.ID;
            Color targetColor = ItemDataManager.Instance.ItemDataList[curID].color;
            if (itemListWasDragged.Count > 1)
            {
                itemPrevLastDrag = itemListWasDragged[itemListWasDragged.Count - 2].GetComponent<MatchItem>();
            }
            if (itemLastDrag.gameObject != anotherItem)
            {
                //If the next item is dragging difference from the last item is dragged
                if (itemPrevLastDrag != null)
                {
                    if (itemNeedCompare.point.CompareItemPoint(itemPrevLastDrag.point))
                    {       //if another item == item previous last dragged
                        itemLastDrag.transform.localScale = new Vector3(itemScale, itemScale, 1f);
                        itemLastDrag.gameObject.GetComponent<LineRenderer>().positionCount = 0;
                        itemListWasDragged.Remove(itemLastDrag.gameObject);
                        Vector2 worldTouch = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        SetupLineRender(itemListWasDragged[itemListWasDragged.Count - 1].gameObject, worldTouch, targetColor);
                        SoundManager.Instance.PlaySound(SoundManager.Instance.turnBack);
                        return;
                    }
                }
                if (itemLastDrag.ID != itemNeedCompare.ID &&
                PowerupManager.Instance.currentType != PowerupType.SwapCells) //still connect them if swaping is in use
                {
                    //If both of them are not equal type
                    Vector2 worldTouch = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    SetupLineRender(itemListWasDragged[itemListWasDragged.Count - 1].gameObject, worldTouch, targetColor);
                    return;
                }
                else
                {
                    //If both of them are equal type
                    int distanceX = itemNeedCompare.point.y - itemLastDrag.point.y;
                    if (distanceX <= 1 && distanceX > 0)
                    {
                        if (itemLastDrag.point.x == itemNeedCompare.point.x)
                        {
                            //y pos greater than item last
                            if (minimizeItemDragged)
                                anotherItem.transform.localScale = new Vector3(haftOfItemScale, haftOfItemScale, 1f);
                            SetupLineRender(itemLastDrag.gameObject, anotherItem.transform.position, targetColor);
                            itemListWasDragged.Add(anotherItem);
                            SoundManager.Instance.PlaySound(SoundManager.Instance.dragging);
                            return;
                        }
                    }
                    else if (distanceX >= -1 && distanceX < 0)
                    {
                        if (itemLastDrag.point.x == itemNeedCompare.point.x)
                        {
                            //y pos less than item last
                            if (minimizeItemDragged)
                                anotherItem.transform.localScale = new Vector3(haftOfItemScale, haftOfItemScale, 1f);
                            SetupLineRender(itemLastDrag.gameObject, anotherItem.transform.position, targetColor);
                            itemListWasDragged.Add(anotherItem);
                            SoundManager.Instance.PlaySound(SoundManager.Instance.dragging);
                            return;
                        }
                    }
                    int distanceY = itemNeedCompare.point.x - itemLastDrag.point.x;
                    if (distanceY <= 1 && distanceY > 0)
                    {
                        if (itemLastDrag.point.y == itemNeedCompare.point.y)
                        {
                            if (minimizeItemDragged)
                                anotherItem.transform.localScale = new Vector3(haftOfItemScale, haftOfItemScale, 1f);
                            SetupLineRender(itemLastDrag.gameObject, anotherItem.transform.position, targetColor);
                            itemListWasDragged.Add(anotherItem);
                            SoundManager.Instance.PlaySound(SoundManager.Instance.dragging);
                            return;
                        }
                    }
                    else if (distanceY >= -1 && distanceY < 0)
                    {
                        if (itemLastDrag.point.y == itemNeedCompare.point.y)
                        {
                            if (minimizeItemDragged)
                                anotherItem.transform.localScale = new Vector3(haftOfItemScale, haftOfItemScale, 1f);
                            SetupLineRender(itemLastDrag.gameObject, anotherItem.transform.position, targetColor);
                            itemListWasDragged.Add(anotherItem);
                            SoundManager.Instance.PlaySound(SoundManager.Instance.dragging);
                            return;
                        }
                    }
                    Vector2 worldTouch = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    SetupLineRender(itemListWasDragged[itemListWasDragged.Count - 1].gameObject, worldTouch, targetColor);
                }
            }
        }

        #region SAVE AND LOAD GRID ITEM

        ///// <summary>
        ///// Saved grid whenever player out game or back menu
        ///// </summary>
        //public void SaveGridItem()
        //{
        //    IsNewGame = false;
        //    ItemList_CountStore = itemsList.Count;
        //    for (int i = 0; i < itemsList.Count; i++)
        //    {
        //        MatchItem item = itemsList[i];
        //        SetScoreNumberItemStoredByIndex(i, item.ScoreItem);
        //        SetX_PosItemStoredByIndex(i, item.point.x);
        //        SetY_PosItemStoredByIndex(i, item.point.y);
        //    }
        //    ScoreManager.Instance.StoredCurrentScore = ScoreManager.Instance.Score;
        //}


        ///// <summary>
        ///// Load grid after game had saved
        ///// </summary>
        //public void LoadGridItem()
        //{
        //    int itemListCount = ItemList_CountStore;
        //    itemsList = new List<MatchItem>();
        //    ScoreManager.Instance.UpdateScore(ScoreManager.Instance.StoredCurrentScore);

        //    for (int row = 0; row < rowCount; row++)
        //    {
        //        for (int col = 0; col < columnCount; col++)
        //        {
        //            for (int i = 0; i < itemListCount; i++)
        //            {
        //                if (GetX_PosItemStoredByIndex(i) == row && GetY_PosItemStoredByIndex(i) == col)
        //                {
        //                    currentMaxScore = GetScoreNumberItemStoredByIndex(i);
        //                    GameObject newItem = Instantiate(matchItemPrefab);
        //                    newItem.transform.SetParent(GetParentOfItem());
        //                    newItem.transform.localScale = new Vector3(itemScale, itemScale, 1f);
        //                    Vector3 newPos = new Vector3(col * itemScale, row * -itemScale, 1f);
        //                    newItem.transform.localPosition = newPos;
        //                    newItem.GetComponent<SpriteRenderer>().color = ItemColorManager.Instance.itemColorList[GetScoreNumberItemStoredByIndex(i) - minValueRandom];
        //                    if (!newItem.activeSelf)
        //                        newItem.SetActive(true);
        //                    MatchItem itemComponent = newItem.GetComponent<MatchItem>();
        //                    itemComponent.cell = cells[row, col];
        //                    itemComponent.cell.positionInParent = newPos;
        //                    itemComponent.point = new ItemPoint(row, col);
        //                    itemComponent.ScoreItem = GetScoreNumberItemStoredByIndex(i);
        //                    if (MaxNumberManager.Instance.MaxNumber < currentMaxScore)
        //                    {
        //                        MaxNumberManager.Instance.UpdateMaxNumber(currentMaxScore);
        //                    }
        //                    itemsList.Add(itemComponent);
        //                }
        //            }
        //        }
        //    }
        //    if (CheckGameOver())
        //        StartCoroutine(CrResetGrid());
        //    HintFintItemCanCombine();
        //}
        #endregion

    #region SET/GET
    public bool TempDisplayTextItem
        {
            get
            {
                if (PlayerPrefs.GetInt("DisplayTextItem") == 0)
                    return false;
                else
                    return true;
            }
            set
            {
                if (value == false)
                    PlayerPrefs.SetInt("DisplayTextItem", 0);
                else
                    PlayerPrefs.SetInt("DisplayTextItem", 1);
            }
        }


        public int TempRowCount
        {
            get { return PlayerPrefs.GetInt("Row_Count"); }
            set { PlayerPrefs.SetInt("Row_Count", value); }
        }

        public int TempColumnCount
        {
            get { return PlayerPrefs.GetInt("Column_Count"); }
            set { PlayerPrefs.SetInt("Column_Count", value); }
        }

        public int TempMinValue
        {
            get { return PlayerPrefs.GetInt(TEMP_MIN_VALUE); }
            set { PlayerPrefs.SetInt(TEMP_MIN_VALUE, value); }
        }

        public int TempMaxValue
        {
            get { return PlayerPrefs.GetInt(TEMP_MAX_VALUE); }
            set { PlayerPrefs.SetInt(TEMP_MAX_VALUE, value); }
        }

        public int TempMinCellMustCombine
        {
            get { return PlayerPrefs.GetInt("MinCellMustCombine"); }
            set { PlayerPrefs.SetInt("MinCellMustCombine", value); }
        }

        public bool IsNewGame
        {
            get
            {
                if (PlayerPrefs.GetInt(IS_NEW_GAME) == 1)
                    return true;
                else
                    return false;
            }
            set
            {
                if (value == false)
                    PlayerPrefs.SetInt(IS_NEW_GAME, 0);
                else
                    PlayerPrefs.SetInt(IS_NEW_GAME, 1);
            }
        }

        public int ItemList_CountStore
        {
            get { return PlayerPrefs.GetInt("ItemList.Count"); }
            set { PlayerPrefs.SetInt("ItemList.Count", value); }
        }

        public string ScoreNumberOfItemKey(int itemIndex)
        {
            return string.Format("Item.{0:000}.Score", itemIndex);
        }

        public string X_PosOfItemKey(int itemIndex)
        {
            return string.Format("Item.{0:000}.PosX", itemIndex);
        }

        public string Y_PosOfItemKey(int itemIndex)
        {
            return string.Format("Item.{0:000}.PosY", itemIndex);
        }

        public void SetScoreNumberItemStoredByIndex(int index, int scoreNumber)
        {
            PlayerPrefs.SetInt(ScoreNumberOfItemKey(index), scoreNumber);
        }

        public int GetScoreNumberItemStoredByIndex(int index)
        {
            return PlayerPrefs.GetInt(ScoreNumberOfItemKey(index));
        }

        public void SetX_PosItemStoredByIndex(int index, int xPos)
        {
            PlayerPrefs.SetInt(X_PosOfItemKey(index), xPos);
        }

        public int GetX_PosItemStoredByIndex(int index)
        {
            return PlayerPrefs.GetInt(X_PosOfItemKey(index));
        }

        public void SetY_PosItemStoredByIndex(int index, int xPos)
        {
            PlayerPrefs.SetInt(Y_PosOfItemKey(index), xPos);
        }

        public int GetY_PosItemStoredByIndex(int index)
        {
            return PlayerPrefs.GetInt(Y_PosOfItemKey(index));
        }

        public bool isPowerUp()
        {
            float x = Random.Range(0, 1 + 0.01F);
            if (x < powerUpFrequency)
                return true;
            else
                return false;
        }

        #endregion
    }
}