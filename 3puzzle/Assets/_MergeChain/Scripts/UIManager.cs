using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;

#if EASY_MOBILE
using EasyMobile;
#endif

namespace MergeChain
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Object References")]
        public GameObject settingInGridBtn;
        public GameObject mainCanvas;
        public GameObject header;
        public GameObject title;
        public GameObject settingInGame;

        public Text score;
        public Text bestScore;
        public Text maxNumber;
        public Text bestMaxNumber;

        public GameObject newBestScore;
        public GameObject playBtn;
        public GameObject restartBtn;
        public GameObject menuButtons;
        public GameObject dailyRewardBtn;
        public Text dailyRewardBtnText;
        public GameObject rewardUI;
        public GameObject settingsUI;
        public GameObject soundOnBtn;
        public GameObject soundOffBtn;
        public GameObject musicOnBtn;
        public GameObject musicOffBtn;
        public GameObject musicOnInGameBtn;
        public GameObject musicOffInGameBtn;
        public GameObject soundOnInGameBtn;
        public GameObject soundOffInGameBtn;

        [Header("PowerUp")]
        public GameObject powerupGroup;
        public Button destroyPowerupButton;
        public Button swapPowerupButton;
        public Button scramblePowerupButton;
        public Color powerupButtonNormalColor;
        public Color powerupButtonHighlightColor;
        public GameObject PowerUpBoard;
        public Text powerUpText;

        [Header("PowerUp Notifitcation")]
        public GameObject powerupNotificationBoard;
        public GameObject ScrambleNoti;
        public GameObject swapNoti;
        public GameObject destroyNoti;
        public GameObject notEnoughEnergyNoti;

        [Header("Premium Features Buttons")]
        public GameObject watchRewardedAdBtn;
        public GameObject leaderboardBtn;
        public GameObject achievementBtn;
        public GameObject removeAdsBtn;
        public GameObject restorePurchaseBtn;
        public Image watchForPowerUpImage;

        [Header("In-App Purchase Store")]
        public GameObject storeUI;

        [Header("Sharing-Specific")]
        public GameObject shareUI;
        public ShareUIController shareUIController;

        [Header("Config")]
        public Color disablePowerUpBtnColor;
    

        Animator scoreAnimator;
        Animator maxNumberAnimator;
        //Animator dailyRewardAnimator;
        bool isWatchAdsForCoinBtnActive;
        bool isPressedSettingBtn;

        public static event System.Action BackToMenu;

        public static Action ShowMenuInGame;
        public static Action HideMenuInGame;
        public static Action RenewGameEvent;

        #if EASY_MOBILE
    Color originalPowerupBtnColor;
    #endif

        void OnEnable()
        {
            GameManager.GameStateChanged += GameManager_GameStateChanged;
            ScoreManager.ScoreUpdated += OnScoreUpdated;
            MaxNumberManager.MaxNumberUpdated += OnMaxNumberScoreUpdated;
            PowerupManager.PowerupChanged += OnPowerupChanged;
            PowerupManager.PowerUpsUpdated += OnPowerUpdate;
        }

        void OnDisable()
        {
            GameManager.GameStateChanged -= GameManager_GameStateChanged;
            ScoreManager.ScoreUpdated -= OnScoreUpdated;
            MaxNumberManager.MaxNumberUpdated -= OnMaxNumberScoreUpdated;
            PowerupManager.PowerupChanged -= OnPowerupChanged;
            PowerupManager.PowerUpsUpdated -= OnPowerUpdate;
        }

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                DestroyImmediate(gameObject);
            }

            #if EASY_MOBILE
        originalPowerupBtnColor = watchForPowerUpImage.color;
            #endif
        }

        // Use this for initialization
        void Start()
        {
            scoreAnimator = score.GetComponent<Animator>();
            maxNumberAnimator = maxNumber.GetComponent<Animator>();

            Reset();
            ShowStartUI();
        }

        public void UseDestroyCell()
        {
            if (PowerupManager.Instance.currentType == PowerupType.None && PowerupManager.Instance.PowerUps >= PowerupManager.Instance.destroyCellCost)
            {
                PowerupManager.Instance.SetType(PowerupType.DestroyCell);
                ShowDestroyNoti();
            }
            else if (PowerupManager.Instance.PowerUps < PowerupManager.Instance.destroyCellCost)
            {
                ShowNotEnoughEnergyNotif();
            }
        }

        public void UseSwapCells()
        {
            if (PowerupManager.Instance.currentType == PowerupType.None && PowerupManager.Instance.PowerUps >= PowerupManager.Instance.swapCellsCost)
            {
                PowerupManager.Instance.SetType(PowerupType.SwapCells);
                ShowSwapNoti();
            }
            else if (PowerupManager.Instance.PowerUps < PowerupManager.Instance.swapCellsCost)
            {
                ShowNotEnoughEnergyNotif();
            }
        }

        public void UseScramble()
        {
            if (PowerupManager.Instance.currentType == PowerupType.None && PowerupManager.Instance.PowerUps >= PowerupManager.Instance.scrambleCost)
            {
                PowerupManager.Instance.SetType(PowerupType.Scramble);
                ShowScrambleNoti();
            }
            else if (PowerupManager.Instance.PowerUps < PowerupManager.Instance.scrambleCost)
            {
                ShowNotEnoughEnergyNotif();
            }
        }

        void OnPowerUpdate(int powerUps)
        {
            powerUpText.text = powerUps.ToString();
        }

        // Update is called once per frame
        void Update()
        {

            score.text = ScoreManager.Instance.Score.ToString();
            bestScore.text = ScoreManager.Instance.HighScore.ToString();
            switch (GridItemManager.Instance.displayWay)
            {
                case DisplayContent.Numbers:
                    maxNumber.text = ItemDataManager.Instance.ItemDataList[MaxNumberManager.Instance.MaxNumber].number.ToString();
                    bestMaxNumber.text = ItemDataManager.Instance.ItemDataList[MaxNumberManager.Instance.HighMaxNumber].number.ToString();
                    break;
                case DisplayContent.Characters:
                    if (MaxNumberManager.Instance.MaxNumber > GridItemManager.Instance.minValueRandom)
                        maxNumber.text = ItemDataManager.Instance.ItemDataList[MaxNumberManager.Instance.MaxNumber].character.ToString();
                    else
                        maxNumber.text = "";
                    if (MaxNumberManager.Instance.HighMaxNumber > GridItemManager.Instance.minValueRandom)
                        bestMaxNumber.text = ItemDataManager.Instance.ItemDataList[MaxNumberManager.Instance.HighMaxNumber].character.ToString();
                    else
                        bestMaxNumber.text = "";
                    break;
            }

            if (settingsUI.activeSelf || settingInGame.activeSelf)
            {
                UpdateMuteButtons();
                UpdateMusicButtons();
            }

            ShowWatchForPowerUp();
        }

        void GameManager_GameStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Prepare)
            {
                ShowStartUI();
            }
            if (newState == GameState.Playing)
            {
                ShowGameUI();
            }
            else if (newState == GameState.PreGameOver)
            {
                // Before game over, i.e. game potentially will be recovered
            }
            else if (newState == GameState.GameOver)
            {
                Invoke("ShowGameOverUI", 1f);
            }
        }

        void OnScoreUpdated(int newScore)
        {
            scoreAnimator.Play("NewScore");
        }

        void OnMaxNumberScoreUpdated(int newMaxNumber)
        {
            maxNumberAnimator.Play("NewScore");
        }

        void Reset()
        {
            mainCanvas.SetActive(true);
            header.SetActive(false);
            title.SetActive(false);
            //        score.gameObject.SetActive(false);
            newBestScore.SetActive(false);
            playBtn.SetActive(false);
            menuButtons.SetActive(false);
            dailyRewardBtn.SetActive(false);
            PowerUpBoard.SetActive(false);
            settingInGame.SetActive(false);
            powerupNotificationBoard.SetActive(false);

            // Enable or disable premium stuff
            bool enablePremium = IsPremiumFeaturesEnabled();
            leaderboardBtn.SetActive(enablePremium);
            removeAdsBtn.SetActive(enablePremium);
            restorePurchaseBtn.SetActive(enablePremium);

            // Hidden by default
            storeUI.SetActive(false);
            settingsUI.SetActive(false);
            shareUI.SetActive(false);

            // These premium feature buttons are hidden by default
            // and shown when certain criteria are met (e.g. rewarded ad is loaded)
            watchRewardedAdBtn.gameObject.SetActive(false);
        }

        public void StartGame()
        {
            GameManager.Instance.StartGame();
        }

        public void EndGame()
        {
            GameManager.Instance.GameOver();
        }

        public void RestartGame()
        {
            GameManager.Instance.RestartGame(0.2f);
        }

        public void ShowStartUI()
        {
            settingInGridBtn.SetActive(false);
            settingsUI.SetActive(false);
            header.SetActive(true);
            title.SetActive(true);
            playBtn.SetActive(true);
            restartBtn.SetActive(false);
            menuButtons.SetActive(true);
            powerupGroup.SetActive(false);
            powerupNotificationBoard.SetActive(false);
        }

        public void ShowGameUI()
        {
            powerUpText.text = PowerupManager.Instance.PowerUps.ToString();
            settingInGridBtn.SetActive(true);
            header.SetActive(true);
            title.SetActive(false);
            //        score.gameObject.SetActive(true);
            playBtn.SetActive(false);
            menuButtons.SetActive(false);
            dailyRewardBtn.SetActive(false);
            watchRewardedAdBtn.SetActive(false);
            powerupGroup.SetActive(true);
            PowerUpBoard.SetActive(true);
        }

        public void ShowGameOverUI()
        {
            powerupGroup.SetActive(false);
            settingInGridBtn.SetActive(false);
            header.SetActive(true);
            title.SetActive(false);
            newBestScore.SetActive(ScoreManager.Instance.HasNewHighScore);
            playBtn.SetActive(false);
            restartBtn.SetActive(true);
            menuButtons.SetActive(true);
            settingsUI.SetActive(false);

            // Show 'daily reward' button
            //ShowDailyRewardBtn();

            // Show these if premium features are enabled (and relevant conditions are met)
            if (IsPremiumFeaturesEnabled())
            {
                ShowShareUI();
                //ShowWatchForCoinsBtn();
            }
        }

        void ShowWatchForCoinsBtn()
        {
            // Only show "watch for coins button" if a rewarded ad is loaded and premium features are enabled
#if EASY_MOBILE
        if (IsPremiumFeaturesEnabled() && AdDisplayer.Instance.CanShowRewardedAd() && AdDisplayer.Instance.watchAdToEarnCoins)
        {
            watchRewardedAdBtn.SetActive(true);
            watchRewardedAdBtn.GetComponent<Animator>().SetTrigger("activate");
        }
        else
        {
            watchRewardedAdBtn.SetActive(false);
        }
#endif
        }

        void ShowWatchForPowerUp()
        {
#if EASY_MOBILE
        if (IsPremiumFeaturesEnabled() && AdDisplayer.Instance.CanShowRewardedAd() && AdDisplayer.Instance.watchAdToEarnPowerUp)
        {
            watchForPowerUpImage.color = originalPowerupBtnColor;
        }
        else
        {
            watchForPowerUpImage.color = disablePowerUpBtnColor;
        }
#endif
        }

        void ShowDailyRewardBtn()
        {
            // Not showing the daily reward button if the feature is disabled
            if (!DailyRewardController.Instance.disable)
            {
                dailyRewardBtn.SetActive(true);
            }
        }

        public void BackMenuHandler()
        {
            if (settingInGame.activeSelf)
                settingInGame.SetActive(false);
            if (BackToMenu != null)
            {
                BackToMenu();
                GameManager.GameCount++;
                if (RenewGameEvent != null)
                    RenewGameEvent();
            }
            //GridItemManager.Instance.SaveGridItem();
        }

        public void ShowSettingsUI()
        {
            settingsUI.SetActive(true);
        }

        public void HideSettingsUI()
        {
            settingsUI.SetActive(false);
        }

        public void ClickSettingInGameButton()
        {
            if (isPressedSettingBtn)
            {
                if (HideMenuInGame != null)
                    HideMenuInGame();
                HideSettingInGame();
                GameManager.Instance.isPaused = false;
            }
            else
            {
                if (ShowMenuInGame != null)
                    ShowMenuInGame();
                ShowSettingInGame();
                GameManager.Instance.isPaused = true;
            }

            isPressedSettingBtn = !isPressedSettingBtn;
        }

        public void HideSettingInGame()
        {
            settingInGame.SetActive(false);
        }

        public void ShowSettingInGame()
        {
            StartCoroutine(CR_ShowSettingInGame());
        }

        IEnumerator CR_ShowSettingInGame()
        {
            yield return new WaitForSeconds(0.01f);
            settingInGame.SetActive(true);
        }

        public void ShowStoreUI()
        {
            storeUI.SetActive(true);
        }

        public void HideStoreUI()
        {
            storeUI.SetActive(false);
        }

        public void WatchRewardedAd()
        {
#if EASY_MOBILE
        // Hide the button
        //watchRewardedAdBtn.SetActive(false);

        AdDisplayer.CompleteRewardedAdToEarnCoins += OnCompleteRewardedAdToEarnCoins;
        AdDisplayer.Instance.ShowRewardedAdToEarnCoins();
#endif
        }

        void OnCompleteRewardedAdToEarnCoins()
        {
#if EASY_MOBILE
        // Unsubscribe
        AdDisplayer.CompleteRewardedAdToEarnCoins -= OnCompleteRewardedAdToEarnCoins;

        // Give the coins!
        ShowRewardUI(AdDisplayer.Instance.rewardedPowerUps);
#endif
        }

        public void GrabDailyReward()
        {
            if (DailyRewardController.Instance.CanRewardNow())
            {
                int reward = DailyRewardController.Instance.GetRandomReward();

                // Round the number and make it mutiplies of 5 only.
                int roundedReward = (reward / 5) * 5;

                // Show the reward UI
                ShowRewardUI(roundedReward);

                // Update next time for the reward
                DailyRewardController.Instance.ResetNextRewardTime();
            }
        }

        public void NewGameHandler()
        {
            GridItemManager.Instance.IsNewGame = true;
            GridItemManager.Instance.currentMaxID = 0;
            MaxNumberManager.Instance.UpdateMaxNumber(0);
            GameManager.GameCount++;
            GameManager.Instance.RestartGame();
            if (RenewGameEvent != null)
                RenewGameEvent();
            if (settingInGame.activeSelf)
                settingInGame.SetActive(false);
        }

        public void ShowRewardUI(int reward)
        {
            rewardUI.SetActive(true);
            rewardUI.GetComponent<RewardUIController>().Reward(reward);
        }

        public void HideRewardUI()
        {
            rewardUI.GetComponent<RewardUIController>().Close();
        }

        public void ShowLeaderboardUI()
        {
#if EASY_MOBILE
        if (GameServices.IsInitialized())
        {
            GameServices.ShowLeaderboardUI();
        }
        else
        {
#if UNITY_IOS
            NativeUI.Alert("Service Unavailable", "The user is not logged in to Game Center.");
#elif UNITY_ANDROID
            GameServices.Init();
#endif
        }
#endif
        }

        public void ShowAchievementsUI()
        {
#if EASY_MOBILE
        if (GameServices.IsInitialized())
        {
            GameServices.ShowAchievementsUI();
        }
        else
        {
#if UNITY_IOS
            NativeUI.Alert("Service Unavailable", "The user is not logged in to Game Center.");
#elif UNITY_ANDROID
            GameServices.Init();
#endif
        }
#endif
        }

        public void PurchaseRemoveAds()
        {
#if EASY_MOBILE
        InAppPurchaser.Instance.Purchase(InAppPurchaser.Instance.removeAds);
#endif
        }

        public void RestorePurchase()
        {
#if EASY_MOBILE
        InAppPurchaser.Instance.RestorePurchase();
#endif
        }

        public void ShowShareUI()
        {
            if (!ScreenshotSharer.Instance.disableSharing)
            {
                Texture2D texture = ScreenshotSharer.Instance.CapturedScreenshot;
                shareUIController.ImgTex = texture;

#if EASY_MOBILE
            AnimatedClip clip = ScreenshotSharer.Instance.RecordedClip;
            shareUIController.AnimClip = clip;
#endif

                shareUI.SetActive(true);
            }
        }

        public void HideShareUI()
        {
            shareUI.SetActive(false);
        }

        public void ToggleSound()
        {
            SoundManager.Instance.ToggleMute();
        }

        public void ToggleMusic()
        {
            SoundManager.Instance.ToggleMusic();
        }

        public void RateApp()
        {
            Utilities.RateApp();
        }

        public void OpenTwitterPage()
        {
            Utilities.OpenTwitterPage();
        }

        public void OpenFacebookPage()
        {
            Utilities.OpenFacebookPage();
        }

        public void ButtonClickSound()
        {
            Utilities.ButtonClickSound();
        }

        void UpdateMuteButtons()
        {
            if (SoundManager.Instance.IsMuted())
            {
                soundOnBtn.gameObject.SetActive(false);
                soundOnInGameBtn.gameObject.SetActive(false);

                soundOffBtn.gameObject.SetActive(true);
                soundOffInGameBtn.gameObject.SetActive(true);
            }
            else
            {
                soundOnBtn.gameObject.SetActive(true);
                soundOnInGameBtn.gameObject.SetActive(true);

                soundOffBtn.gameObject.SetActive(false);
                soundOffInGameBtn.gameObject.SetActive(false);
            }
        }

        void UpdateMusicButtons()
        {
            if (SoundManager.Instance.IsMusicOff())
            {
                musicOffBtn.gameObject.SetActive(true);
                musicOffInGameBtn.gameObject.SetActive(true);

                musicOnBtn.gameObject.SetActive(false);
                musicOnInGameBtn.gameObject.SetActive(false);
            }
            else
            {
                musicOffBtn.gameObject.SetActive(false);
                musicOffInGameBtn.gameObject.SetActive(false);

                musicOnBtn.gameObject.SetActive(true);
                musicOnInGameBtn.gameObject.SetActive(true);
            }
        }

        bool IsPremiumFeaturesEnabled()
        {
            return PremiumFeaturesManager.Instance != null && PremiumFeaturesManager.Instance.enablePremiumFeatures;
        }

        private void OnPowerupChanged(PowerupType newType, PowerupType oldType)
        {
            if (destroyPowerupButton.targetGraphic != null)
            {
                destroyPowerupButton.targetGraphic.color =
                newType == PowerupType.DestroyCell ?
                powerupButtonHighlightColor :
                powerupButtonNormalColor;
            }

            if (swapPowerupButton.targetGraphic != null)
            {
                swapPowerupButton.targetGraphic.color =
                newType == PowerupType.SwapCells ?
                powerupButtonHighlightColor :
                powerupButtonNormalColor;
            }

            if (scramblePowerupButton.targetGraphic != null)
            {
                scramblePowerupButton.targetGraphic.color =
                newType == PowerupType.Scramble ?
                powerupButtonHighlightColor :
                powerupButtonNormalColor;
            }
        }

        public void ShowNotEnoughEnergyNotif()
        {
            powerupNotificationBoard.SetActive(true);
            notEnoughEnergyNoti.SetActive(true);
            ScrambleNoti.SetActive(false);
            swapNoti.SetActive(false);
            destroyNoti.SetActive(false);

            Invoke("HidePowerUpNotiBoard", 5);
        }

        public void ShowScrambleNoti()
        {
            powerupNotificationBoard.SetActive(true);
            notEnoughEnergyNoti.SetActive(false);
            ScrambleNoti.SetActive(true);
            swapNoti.SetActive(false);
            destroyNoti.SetActive(false);
        }

        public void ShowSwapNoti()
        {
            powerupNotificationBoard.SetActive(true);
            notEnoughEnergyNoti.SetActive(false);
            ScrambleNoti.SetActive(false);
            swapNoti.SetActive(true);
            destroyNoti.SetActive(false);
        }

        public void ShowDestroyNoti()
        {
            powerupNotificationBoard.SetActive(true);
            notEnoughEnergyNoti.SetActive(false);
            ScrambleNoti.SetActive(false);
            swapNoti.SetActive(false);
            destroyNoti.SetActive(true);
        }

        public void HidePowerUpNotiBoard()
        {
            powerupNotificationBoard.SetActive(false);
            notEnoughEnergyNoti.SetActive(false);
            ScrambleNoti.SetActive(false);
            swapNoti.SetActive(false);
            destroyNoti.SetActive(false);
        }

    }
}