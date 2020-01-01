using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MergeChain
{
    public enum PowerupType
    {
        None,
        DestroyCell,
        SwapCells,
        Scramble
    }

    public class PowerupManager : MonoBehaviour
    {
        public delegate void PowerupChangedHandler(PowerupType newType,PowerupType oldType);

        public static event PowerupChangedHandler PowerupChanged;

        const string PPK_POWERUPS = "SGLIB_POWERUPS";


        public static PowerupManager Instance { get; private set; }

        public static event System.Action<int> PowerUpsUpdated = delegate { };

        public PowerupType currentType;

        [SerializeField]
        int intialPowerUp = 0;

        [SerializeField]
        int _powerUps = 0;

        public int PowerUps
        {
            get { return _powerUps; }
            set { _powerUps = value; }
        }

        [Header("Scamble")]
        public int scrambleCost = 4;

        [Header("SwapCells")]
        public int swapCellsCost = 2;

        [Header("DestroyCell")]
        public int destroyCellCost = 1;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                GameManager.GameStateChanged += OnGameStateChanged;
            }
        }

        void Start()
        {
            Reset();
        }

        private void OnGameStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Playing)
                SetType(PowerupType.None);
        }

        private void OnDestroy()
        {
            GameManager.GameStateChanged -= OnGameStateChanged;
        }

        public void SetType(PowerupType t, bool toggle = true)
        {
            PowerupType oldValue = currentType;
            PowerupType newValue = t;
            if (oldValue == newValue && toggle)
            {
                newValue = PowerupType.None;
            }
            currentType = newValue;
            UIManager.Instance.HidePowerUpNotiBoard();
            if (oldValue != newValue && PowerupChanged != null)
                PowerupChanged(newValue, oldValue);

        }

        public void Reset()
        {
            // Initialize coins
            PowerUps = PlayerPrefs.GetInt(PPK_POWERUPS, intialPowerUp);
        }

        public void AddPowerUps(int amount)
        {
            PowerUps += amount;


            // Store new coin value
            PlayerPrefs.SetInt(PPK_POWERUPS, PowerUps);

            // Fire event
            PowerUpsUpdated(PowerUps);
        }

        public void RemovePowerUps(int amount)
        {
            PowerUps -= amount;

            // Store new coin value
            PlayerPrefs.SetInt(PPK_POWERUPS, PowerUps);

            // Fire event
            PowerUpsUpdated(PowerUps);
        }
    }
}