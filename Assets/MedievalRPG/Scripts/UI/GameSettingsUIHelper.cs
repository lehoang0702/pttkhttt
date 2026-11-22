using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MedievalRPG.Audio;
using MedievalRPG.Data;

namespace MedievalRPG.UI
{
    /// <summary>
    /// Helper dùng chung cho bảng Cài đặt giữa MainMenu, CampHub và Battle PauseModal.
    /// </summary>
    public static class GameSettingsUIHelper
    {
        public static void BindSettingsControls(
            Slider musicSlider, TextMeshProUGUI musicValueTMP,
            Slider sfxSlider, TextMeshProUGUI sfxValueTMP,
            Toggle quizToggle, Toggle fastSpeedToggle,
            Toggle quizTimerToggle = null, Toggle cameraShakeToggle = null, Toggle fullscreenToggle = null,
            Action onGameplayPreferenceChanged = null)
        {
            AudioManager audio = AudioManager.EnsureExists();

            if (musicSlider != null)
            {
                musicSlider.minValue = 0f;
                musicSlider.maxValue = 1f;
                musicSlider.SetValueWithoutNotify(audio.MusicVolume);
                UpdatePercentText(musicValueTMP, musicSlider.value);
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.onValueChanged.AddListener(val =>
                {
                    AudioManager.EnsureExists().SetMusicVolume(val);
                    UpdatePercentText(musicValueTMP, val);
                });
            }

            if (sfxSlider != null)
            {
                sfxSlider.minValue = 0f;
                sfxSlider.maxValue = 1f;
                sfxSlider.SetValueWithoutNotify(audio.SFXVolume);
                UpdatePercentText(sfxValueTMP, sfxSlider.value);
                sfxSlider.onValueChanged.RemoveAllListeners();
                sfxSlider.onValueChanged.AddListener(val =>
                {
                    AudioManager.EnsureExists().SetSFXVolume(val);
                    UpdatePercentText(sfxValueTMP, val);
                });
            }

            if (quizToggle != null)
            {
                quizToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("QuizEnabled", 1) == 1);
                quizToggle.onValueChanged.RemoveAllListeners();
                quizToggle.onValueChanged.AddListener(isOn =>
                {
                    PlayerPrefs.SetInt("QuizEnabled", isOn ? 1 : 0);
                    PlayerPrefs.Save();
                    AudioManager.Instance?.PlayButtonClick();
                    onGameplayPreferenceChanged?.Invoke();
                });
            }

            if (fastSpeedToggle != null)
            {
                fastSpeedToggle.SetIsOnWithoutNotify(PlayerPrefs.GetFloat("BattleSpeed", 1f) > 1.1f);
                fastSpeedToggle.onValueChanged.RemoveAllListeners();
                fastSpeedToggle.onValueChanged.AddListener(isOn =>
                {
                    PlayerPrefs.SetFloat("BattleSpeed", isOn ? 1.5f : 1f);
                    PlayerPrefs.Save();
                    AudioManager.Instance?.PlayButtonClick();
                    onGameplayPreferenceChanged?.Invoke();
                });
            }

            PlayerCampProgressManager camp = PlayerCampProgressManager.EnsureLoaded();

            if (quizTimerToggle != null)
            {
                quizTimerToggle.SetIsOnWithoutNotify(camp.QuizTimerEnabled);
                quizTimerToggle.onValueChanged.RemoveAllListeners();
                quizTimerToggle.onValueChanged.AddListener(isOn =>
                {
                    PlayerCampProgressManager mgr = PlayerCampProgressManager.EnsureLoaded();
                    mgr.SetGameplayPreferences(isOn, mgr.CameraShakeEnabled);
                    AudioManager.Instance?.PlayButtonClick();
                    onGameplayPreferenceChanged?.Invoke();
                });
            }

            if (cameraShakeToggle != null)
            {
                cameraShakeToggle.SetIsOnWithoutNotify(camp.CameraShakeEnabled);
                cameraShakeToggle.onValueChanged.RemoveAllListeners();
                cameraShakeToggle.onValueChanged.AddListener(isOn =>
                {
                    PlayerCampProgressManager mgr = PlayerCampProgressManager.EnsureLoaded();
                    mgr.SetGameplayPreferences(mgr.QuizTimerEnabled, isOn);
                    AudioManager.Instance?.PlayButtonClick();
                    onGameplayPreferenceChanged?.Invoke();
                });
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
                fullscreenToggle.onValueChanged.RemoveAllListeners();
                fullscreenToggle.onValueChanged.AddListener(isOn =>
                {
                    Screen.fullScreen = isOn;
                    AudioManager.Instance?.PlayButtonClick();
                });
            }
        }

        public static void WireResetSaveButton(
            MonoBehaviour host,
            Button resetButton,
            TextMeshProUGUI resetLabelTMP,
            Action onResetCompleted = null)
        {
            if (resetButton == null) return;

            bool armed = false;
            Coroutine disarmCoroutine = null;
            string defaultLabel = "XÓA DỮ LIỆU CHƠI";

            if (resetLabelTMP != null)
            {
                resetLabelTMP.text = defaultLabel;
                resetLabelTMP.color = MedievalUITheme.IvoryText;
            }

            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(() =>
            {
                if (!armed)
                {
                    armed = true;
                    AudioManager.Instance?.PlayButtonHover();
                    if (resetLabelTMP != null)
                    {
                        resetLabelTMP.text = "BẤM LẦN NỮA ĐỂ XÁC NHẬN!";
                        resetLabelTMP.color = MedievalUITheme.GoldPrimary;
                    }

                    if (host != null && host.isActiveAndEnabled)
                    {
                        if (disarmCoroutine != null) host.StopCoroutine(disarmCoroutine);
                        disarmCoroutine = host.StartCoroutine(DisarmAfterDelay());
                    }
                    return;
                }

                armed = false;
                if (host != null && disarmCoroutine != null)
                {
                    host.StopCoroutine(disarmCoroutine);
                    disarmCoroutine = null;
                }

                PlayerCampProgressManager.EnsureLoaded().ResetAllProgress(true);
                AudioManager.Instance?.PlayButtonClick();
                if (resetLabelTMP != null)
                {
                    resetLabelTMP.text = "ĐÃ ĐẶT LẠI DỮ LIỆU!";
                    resetLabelTMP.color = MedievalUITheme.SuccessGreen;
                }
                onResetCompleted?.Invoke();
            });

            IEnumerator DisarmAfterDelay()
            {
                yield return new WaitForSecondsRealtime(4f);
                armed = false;
                if (resetLabelTMP != null)
                {
                    resetLabelTMP.text = defaultLabel;
                    resetLabelTMP.color = MedievalUITheme.IvoryText;
                }
            }
        }

        private static void UpdatePercentText(TextMeshProUGUI label, float val)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }
    }
}
