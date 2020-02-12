﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using System;
using DG.Tweening;
public enum NowActive { None, Pause, Sound, Gameover }

public class UIManager : MonoBehaviour
{
    //상태창
    [BoxGroup("Playing UI")] [SerializeField] Text countdownText;
    [BoxGroup("Playing UI")] [SerializeField] GameObject orbitalRadiusUI;
    [BoxGroup("Playing UI")] [SerializeField] Slider starHpBar;
    [BoxGroup("Playing UI")] [SerializeField] Slider planetHpBar;
    [BoxGroup("Playing UI")] [SerializeField] Text scoreText;

    //가속 버튼 관련
    [BoxGroup("Button")] [SerializeField] Image accelBackground;
    [BoxGroup("Button")] [SerializeField] Color accelColorIdle;
    [BoxGroup("Button")] [SerializeField] Color accelColorActive;
    [BoxGroup("Button")] [SerializeField] GameObject accelIconIdle;
    [BoxGroup("Button")] [SerializeField] GameObject accelIconActive;

    //팝업창 관련 - 공통
    [BoxGroup("Pop-Up Window")] [SerializeField] GameObject allPopUpWindow;
    [BoxGroup("Pop-Up Window")] [SerializeField] PopUpWindow popUpWindow;
    [BoxGroup("Pop-Up Window")] [SerializeField] Text titleText;
    [BoxGroup("Pop-Up Window")] [SerializeField] Image popUpBackgroundColor;
    [BoxGroup("Pop-Up Window")] [SerializeField] RectTransform popUpBasicTransform;
    [BoxGroup("Pop-Up Window")] [SerializeField] RectTransform gameoverTransform;
    [BoxGroup("Pop-Up Window")] [SerializeField] RectTransform pauseTransform;
    [BoxGroup("Pop-Up Window")] [SerializeField] RectTransform soundTransform;
    [BoxGroup("Pop-Up Window")] [SerializeField] GameObject warningWindow;
    [BoxGroup("Pop-Up Window")] [SerializeField] WarningText warningText;

    [BoxGroup("Object")] [SerializeField] Star star;
    [BoxGroup("Object")] [SerializeField] Planet planet;

    public event Action EventCountDownDone = () => { };

    bool isPopUpClosing = false;
    bool isWarningActive = false;

    private NowActive _nowActive;
    public NowActive nowActive
    {
        get => _nowActive;
        set
        {
            switch (value)
            {
                case NowActive.None:
                    _nowActive = NowActive.None;
                    break;

                case NowActive.Pause:
                    _nowActive = NowActive.Pause;
                    if (gameoverTransform.gameObject.activeSelf) gameoverTransform.gameObject.SetActive(false);
                    if (soundTransform.gameObject.activeSelf) soundTransform.gameObject.SetActive(false);

                    titleText.text = "OPTION";
                    pauseTransform.gameObject.SetActive(true);
                    break;

                case NowActive.Sound:
                    _nowActive = NowActive.Sound;
                    if (gameoverTransform.gameObject.activeSelf) gameoverTransform.gameObject.SetActive(false);
                    if (pauseTransform.gameObject.activeSelf) pauseTransform.gameObject.SetActive(false);

                    titleText.text = "SOUND";
                    soundTransform.gameObject.SetActive(true);
                    break;

                case NowActive.Gameover:
                    _nowActive = NowActive.Gameover;
                    if (pauseTransform.gameObject.activeSelf) pauseTransform.gameObject.SetActive(false);
                    if (soundTransform.gameObject.activeSelf) soundTransform.gameObject.SetActive(false);

                    titleText.text = "GAME OVER";
                    gameoverTransform.gameObject.SetActive(true);
                    break;
            }
        }
    }

    private void Awake()
    {
        StartCoroutine(CountdownToPlay());

        if(popUpWindow == null) popUpWindow = allPopUpWindow.GetComponent<PopUpWindow>();
        if (warningText == null) warningText = FindObjectOfType<WarningText>();

        star.EventRadiusChange += RadiusChange;
        star.EventHpChanged += OnPlayerHpChanged;
        star.EventMaxHpChanged += OnPlayerMaxHpChanged;

        planet.EventHpChanged += OnPlayerHpChanged;
        planet.EventMaxHpChanged += OnPlayerMaxHpChanged;

        popUpWindow.EventOnPopUpOpen += OnPopUp;
        warningText.EventOnTextEnable += WarningTextSetting;

        accelIconActive.SetActive(false);
        allPopUpWindow.SetActive(false);
    }

    private void Start()
    {
        scoreText.text = "점수: 0";
    }

    private void OnDestroy()
    {
        star.EventRadiusChange -= RadiusChange;
        star.EventHpChanged -= OnPlayerHpChanged;
        star.EventMaxHpChanged -= OnPlayerMaxHpChanged;

        planet.EventHpChanged -= OnPlayerHpChanged;
        planet.EventMaxHpChanged -= OnPlayerMaxHpChanged;

        popUpWindow.EventOnPopUpOpen -= OnPopUp;
        warningText.EventOnTextEnable -= WarningTextSetting;
    }

    void RadiusChange(float radius)
    {
        orbitalRadiusUI.transform.localScale = Vector3.one * radius * 2;
    }

    IEnumerator CountdownToPlay()
    {
        countdownText.text = "3";
        yield return new WaitForSeconds(0.8f);
        countdownText.text = "2";
        yield return new WaitForSeconds(0.8f);
        countdownText.text = "1";
        yield return new WaitForSeconds(0.8f);
        countdownText.gameObject.SetActive(false);
        EventCountDownDone();
    }

    public void OnPlayerHpChanged(int value, Player player)
    {
        if (player.playerType == PlayerType.Star) starHpBar.value = value;
        else if (player.playerType == PlayerType.Planet) planetHpBar.value = value;
    }

    public void OnPlayerMaxHpChanged(int value, Player player)
    {
        if (player.playerType == PlayerType.Star) starHpBar.maxValue = value;
        else if (player.playerType == PlayerType.Planet) planetHpBar.maxValue = value;
    }

    public void ScoreTextChange(int value)
    {
        scoreText.text = $"점수: {value.ToString()}";
    }

    public void OnAccelerateClick()
    {
        accelBackground.color = accelColorActive;
        accelIconIdle.SetActive(false);
        accelIconActive.SetActive(true);

        star.Accelerate();
        GameManager.Instance.SoundManager.PlayFXSound(SoundTypeFX.Booster);
    }
    public void ExitAccelerateClick()
    {
        accelBackground.color = accelColorIdle;
        accelIconActive.SetActive(false);
        accelIconIdle.SetActive(true);

        star.CancelAccelerate();
    }

    //팝업창 관련 설정
    public void OnGameStateChanged(GameState gameState) //게임 상태 변화에 따른 동작 설정
    {
        switch (gameState)
        {
            case GameState.Ready:
                if (allPopUpWindow.activeSelf) allPopUpWindow.SetActive(false);
                break;
            case GameState.Playing:
                break;
            case GameState.Pause:
                allPopUpWindow.SetActive(true);
                nowActive = NowActive.Pause;
                PopUpWindowOnAnimation(pauseTransform);
                break;
            case GameState.GameOver:
                allPopUpWindow.SetActive(true);
                nowActive = NowActive.Gameover;
                PopUpWindowOnAnimation(gameoverTransform);
                break;
        }
    }

    public void Escape()
    {
        if (isWarningActive)
        {
            isWarningActive = false;
            warningWindow.SetActive(false);
            return;
        }

        switch (nowActive)
        {
            case NowActive.None:
                break;
            case NowActive.Pause:
                ButtonPauseToResume();
                break;
            case NowActive.Sound:
                ButtonSoundToPause();
                break;
            case NowActive.Gameover:
                OpenWarningWindow();
                break;
        }
    }

    public void ButtonGameoverToTitle()
    {
        if (isPopUpClosing) return;
        PopUpWindowOffAnimation(gameoverTransform);
        DOVirtual.DelayedCall(0.2f, GameManager.Instance.LoadTitleScene, true);
    }
    public void ButtonGameoverToRestart()
    {
        if (isPopUpClosing) return;
        PopUpWindowOffAnimation(gameoverTransform);
        DOVirtual.DelayedCall(0.2f, GameManager.Instance.ReStartScene, true);
    }
    public void ButtonPauseToResume()
    {
        if (isPopUpClosing) return;
        PopUpWindowOffAnimation(pauseTransform);
        DOVirtual.DelayedCall(0.2f, () => { GameManager.Instance.gameState = GameState.Playing; }, true);
    }
    public void ButtonPauseToTitle()
    {
        if (isPopUpClosing) return;
        OpenWarningWindow();
    }

    public void ButtonPauseToSound()
    {
        if (isPopUpClosing) return;
        nowActive = NowActive.Sound;
    }
    public void ButtonSoundToPause()
    {
        if (isPopUpClosing) return;
        nowActive = NowActive.Pause;
    }
    public void ButtonWarningClose()
    {
        if (isPopUpClosing) return;
        warningWindow.SetActive(false);
        isWarningActive = false;
    }
    public void ButtonWarningToTitle()
    {
        if (isPopUpClosing) return;
        warningWindow.SetActive(false);
        isWarningActive = false;

        isPopUpClosing = true;
        if (gameoverTransform.gameObject.activeSelf) gameoverTransform.gameObject.SetActive(false);
        if (pauseTransform.gameObject.activeSelf) pauseTransform.gameObject.SetActive(false);
        popUpBasicTransform.DOScale(0f, 0.15f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(OffAllWindow);

        DOVirtual.DelayedCall(0.2f, GameManager.Instance.LoadTitleScene, true);
    }

    void OpenWarningWindow()
    {
        warningWindow.SetActive(true);
        isWarningActive = true;
    }

    void OnPopUp() //팝업창을 처음 열 때 공통적으로 처리할 부분. UI를 제외한 게임화면을 50% 검게 처리하고 경고창이 활성화되어 있다면 경고창을 꺼 준다. 
    {
        warningWindow.SetActive(false);
        isWarningActive = false;

        popUpBackgroundColor.color = Color.clear;
        popUpBackgroundColor.DOColor(new Color(0, 0, 0, 0.5f), 0.3f).SetUpdate(true);

        if (!popUpBasicTransform.gameObject.activeSelf) popUpBasicTransform.gameObject.SetActive(true);
    }

    void WarningTextSetting(Text text)
    {
        if (GameManager.Instance.gameState == GameState.Pause) text.text = "정말 게임을 중단하고 처음 화면으로 돌아갑니까?";
        else if (GameManager.Instance.gameState == GameState.GameOver) text.text = "처음 화면으로 돌아가시겠습니까?";
    }

    void PopUpWindowOnAnimation(RectTransform windowScale)
    {
        popUpBasicTransform.localScale = Vector3.one * 0.5f;
        windowScale.localScale = Vector3.one * 0.5f;

        popUpBasicTransform.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
        windowScale.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    void PopUpWindowOffAnimation(RectTransform windowScale)
    {
        isPopUpClosing = true;
        windowScale.DOScale(0f, 0.15f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => { windowScale.gameObject.SetActive(false); });
        popUpBasicTransform.DOScale(0f, 0.15f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(OffAllWindow);
    }
    public void OffAllWindow()
    {
        allPopUpWindow.SetActive(false);
        isPopUpClosing = false;
        nowActive = NowActive.None;
    }
}
