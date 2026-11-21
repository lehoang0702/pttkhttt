using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MedievalRPG.Data;
using MedievalRPG.Quiz;
using MedievalRPG.UI;

namespace MedievalRPG.Combat
{
    /// <summary>
    /// Các trạng thái của một trận chiến theo lượt (Dựa trên Brackeys Turn-Based State Machine)
    /// </summary>
    public enum BattleState 
    { 
        START,             // Bắt đầu trận đấu, thiết lập đội hình và quái vật
        PLAYERTURN,        // Lượt của người chơi (chọn hành động Tấn công / Kỹ năng / Thủ)
        WAITING_FOR_QUIZ,  // Đang chờ người chơi trả lời câu hỏi trắc nghiệm giáo dục
        ENEMYTURN,         // Lượt quái vật phản công
        WON,               // Người chơi chiến thắng
        LOST               // Toàn bộ đội hình bị tiêu diệt (Thất bại)
    }

    /// <summary>
    /// Các loại hành động đang chờ thực thi sau khi trả lời câu hỏi
    /// </summary>
    public enum PendingActionType
    {
        Attack,            // Tấn công cơ bản
        Skill,             // Dùng kỹ năng đặc biệt
        Heal               // Niệm phép hồi máu
    }

    /// <summary>
    /// [Bộ Điều Khiển Chiến Đấu Chính - BattleSystem]
    /// Trái tim của gameplay: Kết hợp giữa Turn-Based RPG (Brackeys) và Hệ Thống Câu Hỏi Giáo Dục (Quiz System).
    /// Người chơi phải trả lời đúng câu hỏi trắc nghiệm thì đòn đánh / phép thuật mới thành công!
    /// </summary>
    public class BattleSystem : MonoBehaviour
    {
        [Header("--- Cấu hình Giáo Dục ---")]
        [Tooltip("Bật/tắt cơ chế hỏi trắc nghiệm trước khi tấn công")]
        public bool enableQuizSystem = true;

        [Header("--- Prefabs Nhân Vật & Quái Vật ---")]
        public GameObject knightPrefab;
        public GameObject magePrefab;
        public GameObject archerPrefab;
        public GameObject enemyPrefab;

        [Header("--- Vị Trí Đứng Trong Trận (Battle Stations) ---")]
        public Transform knightBattleStation;
        public Transform mageBattleStation;
        public Transform archerBattleStation;
        public Transform enemyBattleStation;

        [Header("--- Giao Diện HUD Chuẩn Brackeys (Nếu có Canvas) ---")]
        public Text dialogueText;
        public BattleHUD playerHUD;
        public BattleHUD enemyHUD;

        [Header("--- Danh Sách Đơn Vị Chiến Đấu ---")]
        public List<Unit> partyUnits = new List<Unit>(); // Đội hình 3 nhân vật (Hiệp sĩ, Pháp sư, Cung thủ)
        public Unit enemyUnit;                           // Kẻ địch hiện tại
        private int _currentHeroIndex = 0;               // Vị trí người hùng đang đến lượt

        [Header("--- Trạng Thái Trận Đấu ---")]
        public BattleState state;
        private bool _isActionInProgress = false;

        [Header("--- Chế độ OnGUI dự phòng (chỉ dùng khi không có Canvas UI) ---")]
        public bool enableLegacyOnGUI = true;

        // Lưu thông tin hành động chờ người chơi trả lời câu hỏi xong mới kích hoạt
        private PendingActionType _pendingAction = PendingActionType.Attack;
        private int _pendingSkillIndex = 0;
        private QuizQuestion _activeQuestion = null;
        private string _quizFeedbackMessage = "";
        private bool _isQuizAnswerCorrect = false;
        private bool _quizAnswerSubmitted = false;
        private bool _skipQuizFeedback = false;
        private float _battleSpeed = 1f;

        // Thống kê trận đấu cho UI
        public int TurnCount { get; private set; } = 1;
        public int TotalQuestionsAnswered { get; private set; }
        public int CorrectAnswersCount { get; private set; }
        public int CurrentQuizStreak { get; private set; }
        public int BestQuizStreak { get; private set; }
        public bool IsActionInProgress => _isActionInProgress;
        public bool IsPaused { get; private set; }
        public string CurrentDialogue { get; private set; } = "";

        // Sự kiện đồng bộ giao diện (UI Events)
        public event Action OnBattleInitialized;
        public event Action<BattleState> OnStateChanged;
        public event Action<Unit> OnHeroTurnChanged;
        public event Action<string> OnDialogueChanged;
        public event Action<QuizQuestion, string> OnQuizStarted;
        public event Action<bool, int, int, int, string, float> OnQuizResultShown;
        public event Action OnQuizEnded;
        public event Action<Unit, int, bool> OnDamageDealt;
        public event Action<Unit, int> OnUnitHealed;
        public event Action<Unit, string, Color> OnUnitStatusPop;
        public event Action<BattleState> OnBattleEnded;

        /// <summary>
        /// Nhân vật anh hùng hiện đang đến lượt hành động
        /// </summary>
        public Unit CurrentHero => (partyUnits != null && _currentHeroIndex >= 0 && _currentHeroIndex < partyUnits.Count) 
            ? partyUnits[_currentHeroIndex] 
            : null;

        private void Start()
        {
            ApplySavedSettings();
            SetState(BattleState.START);
            StartCoroutine(SetupBattle());
        }

        private void OnDestroy()
        {
            if (Time.timeScale != 1f)
            {
                Time.timeScale = 1f;
            }
        }

        public void ApplySavedSettings()
        {
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            enableQuizSystem = PlayerPrefs.GetInt("QuizEnabled", enableQuizSystem ? 1 : 0) == 1;
            _battleSpeed = PlayerPrefs.GetFloat("BattleSpeed", 1f);
            if (_battleSpeed < 0.5f) _battleSpeed = 1f;
            if (!IsPaused)
            {
                Time.timeScale = _battleSpeed;
            }
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : _battleSpeed;
        }

        public void SkipQuizFeedbackWait()
        {
            _skipQuizFeedback = true;
        }

        private void SetState(BattleState newState)
        {
            state = newState;
            OnStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Bước 1: Khởi tạo trận đấu, sinh ra các nhân vật và quái vật tại các vị trí đã định
        /// </summary>
        private IEnumerator SetupBattle()
        {
            SetDialogue("Một đàn quái vật hầm ngục xuất hiện! Hãy sẵn sàng chiến đấu!");

            // 1. Sinh nhân vật Hiệp sĩ (Knight - Đỡ đòn)
            if (knightPrefab != null && knightBattleStation != null)
            {
                GameObject kGo = Instantiate(knightPrefab, knightBattleStation.position, Quaternion.identity);
                Unit kUnit = kGo.GetComponent<Unit>();
                if (kUnit != null) partyUnits.Add(kUnit);
            }

            // 2. Sinh nhân vật Pháp sư (Mage - Phép thuật & Hồi máu)
            if (magePrefab != null && mageBattleStation != null)
            {
                GameObject mGo = Instantiate(magePrefab, mageBattleStation.position, Quaternion.identity);
                Unit mUnit = mGo.GetComponent<Unit>();
                if (mUnit != null) partyUnits.Add(mUnit);
            }

            // 3. Sinh nhân vật Cung thủ (Archer - Sát thương vật lý & Bắn chí mạng)
            if (archerPrefab != null && archerBattleStation != null)
            {
                GameObject aGo = Instantiate(archerPrefab, archerBattleStation.position, Quaternion.identity);
                Unit aUnit = aGo.GetComponent<Unit>();
                if (aUnit != null) partyUnits.Add(aUnit);
            }

            // 4. Sinh Quái vật đối thủ
            if (enemyPrefab != null && enemyBattleStation != null)
            {
                GameObject eGo = Instantiate(enemyPrefab, enemyBattleStation.position, Quaternion.identity);
                enemyUnit = eGo.GetComponent<Unit>();
            }

            // Tự động tìm nhân vật trong Scene nếu chưa gán prefab
            if (partyUnits.Count == 0)
            {
                Unit[] allUnits = FindObjectsByType<Unit>();
                foreach (var u in allUnits)
                {
                    if (u.isEnemy) enemyUnit = u;
                    else partyUnits.Add(u);
                }
            }

            // Cập nhật thanh máu HUD nếu có
            if (partyUnits.Count > 0 && playerHUD != null)
            {
                playerHUD.SetHUD(partyUnits[0]);
            }
            if (enemyUnit != null && enemyHUD != null)
            {
                enemyHUD.SetHUD(enemyUnit);
            }

            OnBattleInitialized?.Invoke();

            // Chờ 1.2 giây để người chơi nhìn thấy trận địa
            yield return new WaitForSeconds(1.2f);

            // Bắt đầu lượt của người chơi
            SetState(BattleState.PLAYERTURN);
            _currentHeroIndex = 0;
            PlayerTurn();
        }

        /// <summary>
        /// Bước 2: Bắt đầu lượt của một nhân vật trong đội
        /// </summary>
        private void PlayerTurn()
        {
            // Bỏ qua những nhân vật đã hết máu (tử trận)
            while (_currentHeroIndex < partyUnits.Count && partyUnits[_currentHeroIndex].currentHP <= 0)
            {
                _currentHeroIndex++;
            }

            // Nếu tất cả các thành viên còn sống đều đã đi hết lượt -> Chuyển sang lượt quái vật!
            if (_currentHeroIndex >= partyUnits.Count)
            {
                SetState(BattleState.ENEMYTURN);
                OnHeroTurnChanged?.Invoke(null);
                StartCoroutine(EnemyTurn());
                return;
            }

            Unit hero = partyUnits[_currentHeroIndex];
            hero.OnTurnStart(); // Hồi một lượng nhỏ MP và hết trạng thái thủ

            if (playerHUD != null)
            {
                playerHUD.SetHUD(hero);
            }

            SetState(BattleState.PLAYERTURN);
            OnHeroTurnChanged?.Invoke(hero);
            SetDialogue($"Đến lượt của <b>{hero.unitName}</b>! Hãy chọn hành động:");
        }

        // =====================================================================
        // CƠ CHẾ CÂU HỎI TRẮC NGHIỆM GIÁO DỤC (QUIZ SYSTEM)
        // =====================================================================

        /// <summary>
        /// Bắt đầu quy trình hỏi trắc nghiệm trước khi thi triển hành động
        /// </summary>
        private void TriggerQuizForAction(PendingActionType actionType, int skillIndex = 0)
        {
            if (!enableQuizSystem)
            {
                // Nếu tắt cơ chế trắc nghiệm, đánh luôn
                ExecutePendingAction(actionType, skillIndex, 0);
                return;
            }

            _pendingAction = actionType;
            _pendingSkillIndex = skillIndex;
            _quizAnswerSubmitted = false;
            _skipQuizFeedback = false;

            // Lấy câu hỏi ngẫu nhiên từ QuizManager
            if (QuizManager.Instance != null)
            {
                _activeQuestion = QuizManager.Instance.GetRandomQuestion();
            }
            else
            {
                // Fallback nếu chưa khởi tạo QuizManager
                _activeQuestion = new QuizQuestion(
                    "Toán học",
                    "7 x 8 bằng bao nhiêu?",
                    new string[] { "54", "56", "64", "48" },
                    1,
                    "7 nhân 8 bằng 56.",
                    5
                );
            }

            SetState(BattleState.WAITING_FOR_QUIZ);
            string heroLabel = CurrentHero != null ? CurrentHero.unitName : "Anh Hùng";
            string actionLabel = GetPendingActionDisplayName(actionType, skillIndex);
            string actionContext = $"{heroLabel} đang tụ lực [{actionLabel}] — Trả lời đúng để xuất chiêu!";

            SetDialogue($"<b>{heroLabel}</b> đang tập trung! Hãy trả lời câu hỏi trắc nghiệm để thi triển <b>{actionLabel}</b>.");

            if (OnQuizStarted != null)
            {
                OnQuizStarted.Invoke(_activeQuestion, actionContext);
            }
            else if (BattleUIManager.Instance != null)
            {
                BattleUIManager.Instance.ShowQuizPrompt(_activeQuestion);
            }
        }

        private string GetPendingActionDisplayName(PendingActionType actionType, int skillIndex)
        {
            if (actionType == PendingActionType.Attack) return "Tấn Công";
            if (actionType == PendingActionType.Heal) return "Hồi Phục";
            if (CurrentHero != null && CurrentHero.skills != null && skillIndex >= 0 && skillIndex < CurrentHero.skills.Count)
            {
                return CurrentHero.skills[skillIndex].skillName;
            }
            return "Kỹ Năng";
        }

        /// <summary>
        /// Xử lý khi người chơi bấm chọn đáp án (0 = A, 1 = B, 2 = C, 3 = D)
        /// </summary>
        public void ProcessQuizAnswer(int answerIndex)
        {
            if (state != BattleState.WAITING_FOR_QUIZ || _activeQuestion == null || _quizAnswerSubmitted) return;
            _quizAnswerSubmitted = true;

            int bonusDamage = 0;
            string feedback = "";

            if (QuizManager.Instance != null)
            {
                _isQuizAnswerCorrect = QuizManager.Instance.SubmitAnswer(answerIndex, out feedback, out bonusDamage);
            }
            else
            {
                _isQuizAnswerCorrect = _activeQuestion.IsCorrect(answerIndex);
                bonusDamage = _isQuizAnswerCorrect ? _activeQuestion.bonusDamage : 0;
                feedback = _isQuizAnswerCorrect ? "Chính xác!" : "Sai rồi!";
            }

            TotalQuestionsAnswered++;
            if (_isQuizAnswerCorrect)
            {
                CorrectAnswersCount++;
                CurrentQuizStreak++;
                if (CurrentQuizStreak > BestQuizStreak) BestQuizStreak = CurrentQuizStreak;
            }
            else
            {
                CurrentQuizStreak = 0;
            }

            _quizFeedbackMessage = feedback;
            SetDialogue(feedback);

            float waitTime = 2.4f;
            OnQuizResultShown?.Invoke(
                _isQuizAnswerCorrect,
                answerIndex,
                _activeQuestion.correctAnswerIndex,
                bonusDamage,
                _activeQuestion.explanation,
                waitTime
            );

            StartCoroutine(HandleQuizResultRoutine(_isQuizAnswerCorrect, bonusDamage, waitTime));
        }

        /// <summary>
        /// Tiến trình xử lý sau khi biết kết quả đúng hay sai
        /// </summary>
        private IEnumerator HandleQuizResultRoutine(bool isCorrect, int bonusDamage, float waitTime)
        {
            _isActionInProgress = true;

            // Cho người chơi thời gian đọc giải thích kiến thức (có thể bấm Tiếp tục để bỏ qua chờ)
            float elapsed = 0f;
            while (elapsed < waitTime && !_skipQuizFeedback)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            OnQuizEnded?.Invoke();

            if (isCorrect)
            {
                // Trả lời ĐÚNG -> Thực hiện đòn tấn công với sát thương được cộng thêm!
                SetState(BattleState.PLAYERTURN);
                ExecutePendingAction(_pendingAction, _pendingSkillIndex, bonusDamage);
            }
            else
            {
                // Trả lời SAI -> Bị mất lượt, không thể tấn công!
                if (CurrentHero != null)
                {
                    OnUnitStatusPop?.Invoke(CurrentHero, "MẤT LƯỢT!", MedievalUITheme.DangerRed);
                }
                SetDialogue($"<color=#FF6B6B><b>BỊ MẤT LƯỢT!</b></color> Do trả lời sai, đòn tấn công của <b>{CurrentHero.unitName}</b> đã thất bại!");
                yield return new WaitForSeconds(1.2f);

                _isActionInProgress = false;
                _currentHeroIndex++;
                PlayerTurn();
            }
        }

        /// <summary>
        /// Thực thi hành động cụ thể sau khi đã vượt qua bài trắc nghiệm
        /// </summary>
        private void ExecutePendingAction(PendingActionType actionType, int skillIndex, int bonusDamage)
        {
            switch (actionType)
            {
                case PendingActionType.Attack:
                    StartCoroutine(PlayerAttackRoutine(bonusDamage));
                    break;
                case PendingActionType.Skill:
                    StartCoroutine(PlayerSkillRoutine(skillIndex, bonusDamage));
                    break;
                case PendingActionType.Heal:
                    StartCoroutine(PlayerHealRoutine(bonusDamage));
                    break;
            }
        }

        // =====================================================================
        // LOGIC TẤN CÔNG & KỸ NĂNG CỦA CÁC CLASS
        // =====================================================================

        /// <summary>
        /// Đòn đánh thường của nhân vật
        /// </summary>
        private IEnumerator PlayerAttackRoutine(int bonusDamage)
        {
            _isActionInProgress = true;
            Unit hero = CurrentHero;
            SetDialogue($"<b>{hero.unitName}</b> lao lên tấn công quái vật!");

            // Chạy hoạt ảnh chém / bắn cung
            if (hero.pixelAnimator != null)
            {
                hero.pixelAnimator.PlayAttack(enemyUnit.transform.position);
            }
            else if (hero.visual != null)
            {
                hero.visual.PlayAttackAnimation(enemyUnit.transform.position);
            }

            yield return new WaitForSeconds(0.4f);

            // Tính toán chí mạng và sát thương
            bool isCrit = UnityEngine.Random.value < hero.critRate;
            int finalDmg = isCrit ? Mathf.RoundToInt(hero.damage * hero.critMultiplier) : hero.damage;
            finalDmg += bonusDamage; // Thêm điểm thưởng do trả lời đúng câu hỏi

            bool isDead = enemyUnit.TakeDamage(finalDmg);
            int actualDmg = enemyUnit.LastDamageTaken;

            if (enemyHUD != null)
            {
                enemyHUD.SetHP(enemyUnit.currentHP);
            }

            OnDamageDealt?.Invoke(enemyUnit, actualDmg, isCrit);

            string critText = isCrit ? "<color=#F2CA6B><b>CHÍ MẠNG!</b> </color>" : "";
            SetDialogue($"Đòn đánh trúng đích! {critText}Gây <b>{actualDmg}</b> sát thương lên quái vật!");

            yield return new WaitForSeconds(1.3f);
            _isActionInProgress = false;

            if (isDead)
            {
                SetState(BattleState.WON);
                EndBattle();
            }
            else
            {
                // Chuyển sang lượt thành viên tiếp theo trong tổ đội
                _currentHeroIndex++;
                PlayerTurn();
            }
        }

        /// <summary>
        /// Kỹ năng đặc biệt của từng Class (Knight: Chém khiên, Mage: Quả cầu lửa, Archer: Mưa tên)
        /// </summary>
        private IEnumerator PlayerSkillRoutine(int skillIndex, int bonusDamage)
        {
            _isActionInProgress = true;
            Unit hero = CurrentHero;
            SkillData skill = (hero.skills != null && skillIndex < hero.skills.Count) ? hero.skills[skillIndex] : null;

            if (skill == null)
            {
                yield return PlayerAttackRoutine(bonusDamage);
                yield break;
            }

            // Trừ điểm Mana
            hero.UseMana(skill.manaCost);
            SetDialogue($"<b>{hero.unitName}</b> kích hoạt kỹ năng <color=#6ED2FF><b>[{skill.skillName}]</b></color>!");

            // Xử lý kỹ năng tự tăng giáp (ví dụ: Knight Iron Bulwark)
            if (skill.targetType == SkillTargetType.Self && skill.defenseBonus > 0)
            {
                yield return new WaitForSeconds(0.35f);
                int totalDef = skill.defenseBonus + Mathf.Max(0, bonusDamage / 2);
                hero.ApplyDefenseBuff(totalDef, Mathf.Max(1, skill.effectDurationTurns));
                OnUnitStatusPop?.Invoke(hero, $"+{totalDef} GIÁP!", MedievalUITheme.GoldSoft);
                SetDialogue($"<b>{hero.unitName}</b> dựng khiên sắt kiên cố! Tăng <b>+{totalDef} Giáp</b> trong {Mathf.Max(1, skill.effectDurationTurns)} lượt!");

                yield return new WaitForSeconds(1.2f);
                _isActionInProgress = false;
                _currentHeroIndex++;
                PlayerTurn();
                yield break;
            }

            // Hoạt ảnh tấn công / niệm phép
            if (hero.pixelAnimator != null)
            {
                hero.pixelAnimator.PlayAttack(enemyUnit.transform.position);
            }

            yield return new WaitForSeconds(0.45f);

            // Xử lý kỹ năng hồi máu đồng đội (Mage Healing Light)
            if (skill.targetType == SkillTargetType.SingleAlly || skill.targetType == SkillTargetType.AllAllies)
            {
                Unit woundedAlly = GetMostWoundedAlly();
                int healAmount = skill.baseHealAmount + bonusDamage;
                woundedAlly.Heal(healAmount);
                OnUnitHealed?.Invoke(woundedAlly, healAmount);
                SetDialogue($"Luồng ánh sáng phục hồi bao phủ lấy <b>{woundedAlly.unitName}</b>! Hồi phục <b>+{healAmount} HP</b>!");
                
                yield return new WaitForSeconds(1.3f);
                _isActionInProgress = false;
                _currentHeroIndex++;
                PlayerTurn();
                yield break;
            }

            // Sát thương kỹ năng
            int baseAtk = (skill.damageType == DamageType.Magical) ? hero.magicDamage : hero.damage;
            bool isSkillCrit = UnityEngine.Random.value < (hero.critRate + skill.bonusCritRate);
            float mult = skill.powerMultiplier * (isSkillCrit ? hero.critMultiplier : 1f);
            int skillDmg = Mathf.RoundToInt(baseAtk * mult) + skill.flatBonusDamage + bonusDamage;

            bool isDead = enemyUnit.TakeDamage(skillDmg);
            int actualSkillDmg = enemyUnit.LastDamageTaken;

            if (enemyHUD != null) enemyHUD.SetHP(enemyUnit.currentHP);
            OnDamageDealt?.Invoke(enemyUnit, actualSkillDmg, isSkillCrit);

            string critTag = isSkillCrit ? "<color=#F2CA6B><b>CHÍ MẠNG!</b> </color>" : "";
            SetDialogue($"Kỹ năng trúng đích! {critTag}Gây <b>{actualSkillDmg}</b> sát thương lên quái vật!");

            yield return new WaitForSeconds(1.3f);
            _isActionInProgress = false;

            if (isDead)
            {
                SetState(BattleState.WON);
                EndBattle();
            }
            else
            {
                _currentHeroIndex++;
                PlayerTurn();
            }
        }

        /// <summary>
        /// Kỹ năng hồi máu cơ bản
        /// </summary>
        private IEnumerator PlayerHealRoutine(int bonusAmount)
        {
            _isActionInProgress = true;
            Unit hero = CurrentHero;
            Unit target = GetMostWoundedAlly();

            int healValue = 25 + bonusAmount;
            target.Heal(healValue);
            OnUnitHealed?.Invoke(target, healValue);
            SetDialogue($"<b>{hero.unitName}</b> dùng thảo dược hồi máu cho <b>{target.unitName}</b> (+{healValue} HP)!");

            yield return new WaitForSeconds(1.25f);
            _isActionInProgress = false;
            _currentHeroIndex++;
            PlayerTurn();
        }

        /// <summary>
        /// Phòng thủ (Defend) - Giảm 50% sát thương lượt tới, không cần trả lời trắc nghiệm
        /// </summary>
        public IEnumerator PlayerDefend()
        {
            if (_isActionInProgress || CurrentHero == null) yield break;
            _isActionInProgress = true;

            Unit hero = CurrentHero;
            hero.isDefending = true;
            hero.RestoreMana(8); // Phòng thủ giúp tập trung hồi thêm 8 MP
            OnUnitStatusPop?.Invoke(hero, "THỦ +8 MP", MedievalUITheme.MpSapphire);
            SetDialogue($"<b>{hero.unitName}</b> nâng cao khiên vào tư thế thủ vững chắc! Giảm 50% sát thương và hồi <b>+8 MP</b>!");

            yield return new WaitForSeconds(1.1f);
            _isActionInProgress = false;

            _currentHeroIndex++;
            PlayerTurn();
        }

        // =====================================================================
        // LƯỢT CỦA KẺ ĐỊCH (ENEMY TURN)
        // =====================================================================

        /// <summary>
        /// Kẻ địch tự động chọn một mục tiêu còn sống trong đội hình để phản công
        /// </summary>
        private IEnumerator EnemyTurn()
        {
            SetDialogue($"Đến lượt phản công của <b>{enemyUnit.unitName}</b>!");
            yield return new WaitForSeconds(0.9f);

            // Tìm những nhân vật trong đội còn sống
            List<Unit> aliveHeroes = new List<Unit>();
            foreach (var h in partyUnits)
            {
                if (h != null && h.currentHP > 0)
                {
                    aliveHeroes.Add(h);
                }
            }

            // Nếu toàn bộ đội hình đã tử trận -> Thua cuộc
            if (aliveHeroes.Count == 0)
            {
                SetState(BattleState.LOST);
                EndBattle();
                yield break;
            }

            // Chọn ngẫu nhiên 1 mục tiêu còn sống (ưu tiên hiệp sĩ nếu đang bật thủ)
            Unit targetHero = aliveHeroes[UnityEngine.Random.Range(0, aliveHeroes.Count)];

            // Quái kích hoạt hoạt ảnh tấn công
            if (enemyUnit.pixelAnimator != null)
            {
                enemyUnit.pixelAnimator.PlayAttack(targetHero.transform.position);
            }
            else if (enemyUnit.visual != null)
            {
                enemyUnit.visual.PlayAttackAnimation(targetHero.transform.position);
            }

            yield return new WaitForSeconds(0.4f);

            // Gây sát thương lên người hùng
            bool heroDied = targetHero.TakeDamage(enemyUnit.damage);
            int takenDmg = targetHero.LastDamageTaken;

            if (playerHUD != null && targetHero == CurrentHero)
            {
                playerHUD.SetHP(targetHero.currentHP);
            }

            OnDamageDealt?.Invoke(targetHero, takenDmg, false);
            SetDialogue($"<b>{enemyUnit.unitName}</b> hung hãn tấn công trúng <b>{targetHero.unitName}</b>, gây <b>{takenDmg}</b> sát thương!");

            yield return new WaitForSeconds(1.3f);

            // Kiểm tra xem sau đòn đánh này, toàn đội có còn ai sống sót không
            bool allDead = true;
            foreach (var h in partyUnits)
            {
                if (h != null && h.currentHP > 0)
                {
                    allDead = false;
                    break;
                }
            }

            if (allDead)
            {
                SetState(BattleState.LOST);
                EndBattle();
            }
            else
            {
                // Bắt đầu vòng đấu mới cho phe người chơi!
                TurnCount++;
                _currentHeroIndex = 0;
                PlayerTurn();
            }
        }

        /// <summary>
        /// Kết thúc trận đấu: Xử lý thắng hoặc thua
        /// </summary>
        private void EndBattle()
        {
            if (state == BattleState.WON)
            {
                SetDialogue("<color=#5CE09A><b>CHIẾN THẮNG VANG DỘI!</b></color> Bạn đã trả lời xuất sắc các câu hỏi và đánh bại toàn bộ quái vật hầm ngục!");
                if (BattleUIManager.Instance != null) BattleUIManager.Instance.ShowVictory();
            }
            else if (state == BattleState.LOST)
            {
                SetDialogue("<color=#FF6B6B><b>THẤT BẠI!</b></color> Toàn bộ đội hình đã gục ngã. Hãy ôn lại kiến thức và thử sức lại nhé!");
                if (BattleUIManager.Instance != null) BattleUIManager.Instance.ShowGameOver();
            }
            OnBattleEnded?.Invoke(state);
        }

        /// <summary>
        /// Tìm đồng đội mất nhiều máu nhất để ưu tiên hồi máu
        /// </summary>
        private Unit GetMostWoundedAlly()
        {
            Unit mostWounded = partyUnits[0];
            float lowestPct = 1.0f;

            foreach (var ally in partyUnits)
            {
                if (ally == null || ally.currentHP <= 0) continue;
                float pct = (float)ally.currentHP / ally.maxHP;
                if (pct < lowestPct)
                {
                    lowestPct = pct;
                    mostWounded = ally;
                }
            }
            return mostWounded;
        }

        private void SetDialogue(string message)
        {
            CurrentDialogue = message;
            if (dialogueText != null) dialogueText.text = message;
            OnDialogueChanged?.Invoke(message);
            Debug.Log($"[Combat Dialogue] {message}");
        }

        // =====================================================================
        // SỰ KIỆN GỌI TỪ NÚT BẤM (BUTTON HANDLERS)
        // =====================================================================

        public void OnAttackButton()
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;
            TriggerQuizForAction(PendingActionType.Attack);
        }

        public void OnSkill1Button()
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;
            if (CurrentHero != null && CurrentHero.skills.Count > 1)
            {
                SkillData skill = CurrentHero.skills[1];
                if (CurrentHero.currentMP < skill.manaCost)
                {
                    SetDialogue($"Không đủ Mana để dùng {skill.skillName}! Cần {skill.manaCost} MP.");
                    return;
                }
            }
            TriggerQuizForAction(PendingActionType.Skill, 1);
        }

        public void OnSkill2Button()
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;
            if (CurrentHero != null && CurrentHero.skills.Count > 2)
            {
                SkillData skill = CurrentHero.skills[2];
                if (CurrentHero.currentMP < skill.manaCost)
                {
                    SetDialogue($"Không đủ Mana để dùng {skill.skillName}! Cần {skill.manaCost} MP.");
                    return;
                }
            }
            TriggerQuizForAction(PendingActionType.Skill, 2);
        }

        public void OnHealButton()
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;
            TriggerQuizForAction(PendingActionType.Heal);
        }

        public void OnDefendButton()
        {
            if (state != BattleState.PLAYERTURN || _isActionInProgress) return;
            StartCoroutine(PlayerDefend());
        }

        // =====================================================================
        // GIAO DIỆN TƯƠNG TÁC OnGUI (TỰ ĐỘNG HIỂN THỊ ĐẦY ĐỦ TRÊN GAME VIEW)
        // Giúp người mới có thể test chơi ngay lập tức mà không sợ lỗi kéo thả UI Canvas!
        // =====================================================================
        private void OnGUI()
        {
            if (!enableLegacyOnGUI) return;
            if (BattleUIManager.Instance != null && BattleUIManager.Instance.HasCanvasUI) return;

            GUI.skin.box.fontSize = 13;
            GUI.skin.button.fontSize = 13;
            GUI.skin.label.fontSize = 12;

            // 1. BẢNG THÔNG TIN ĐỘI HÌNH & QUÁI VẬT (Góc trên bên trái)
            GUILayout.BeginArea(new Rect(15, 15, 290, 240), GUI.skin.box);
            GUILayout.Label("<b>⚔️ ĐỘI HÌNH HIỆP SĨ (3 Classes):</b>");
            for (int i = 0; i < partyUnits.Count; i++)
            {
                Unit u = partyUnits[i];
                if (u == null) continue;
                bool isCurrent = (state == BattleState.PLAYERTURN && i == _currentHeroIndex);
                string arrow = isCurrent ? "▶ " : "  ";
                string col = isCurrent ? "<color=yellow>" : (u.currentHP <= 0 ? "<color=red>" : "<color=white>");
                GUILayout.Label($"{arrow}{col}<b>{u.unitName}</b>: HP {u.currentHP}/{u.maxHP} | MP {u.currentMP}/{u.maxMP}</color>");
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>👹 QUÁI VẬT ĐỐI THỦ:</b>");
            if (enemyUnit != null)
            {
                string eCol = enemyUnit.currentHP <= 0 ? "<color=gray>[Đã bị hạ gục] " : "<color=#ff5555>";
                GUILayout.Label($"{eCol}<b>{enemyUnit.unitName}</b>: HP {enemyUnit.currentHP}/{enemyUnit.maxHP}</color>");
            }
            GUILayout.EndArea();

            // 2. BẢNG CÂU HỎI TRẮC NGHIỆM GIÁO DỤC (HIỂN THỊ CHÍNH GIỮA MÀN HÌNH KHI ĐANG HỎI)
            if (state == BattleState.WAITING_FOR_QUIZ && _activeQuestion != null)
            {
                float qWidth = Mathf.Min(650, Screen.width - 40);
                float qHeight = 280;
                Rect qRect = new Rect((Screen.width - qWidth) / 2, (Screen.height - qHeight) / 2 - 20, qWidth, qHeight);

                GUI.backgroundColor = new Color(0.12f, 0.12f, 0.2f, 0.95f);
                GUILayout.BeginArea(qRect, GUI.skin.box);
                GUI.backgroundColor = Color.white;

                GUILayout.Label($"<size=15><b>📜 CÂU HỎI TRẮC NGHIỆM [{_activeQuestion.category}]</b></size>", GUI.skin.label);
                GUILayout.Label("<i>(Trả lời đúng để xuất chiêu thành công! Trả lời sai sẽ bị mất lượt)</i>");
                GUILayout.Space(8);

                // Nội dung câu hỏi
                GUILayout.Label($"<size=14><b>{_activeQuestion.questionText}</b></size>");
                GUILayout.Space(12);

                // 4 Nút đáp án A, B, C, D
                for (int i = 0; i < 4; i++)
                {
                    char opt = (char)('A' + i);
                    string ansText = (i < _activeQuestion.answers.Length) ? _activeQuestion.answers[i] : "";
                    
                    if (GUILayout.Button($"<b>{opt}.</b> {ansText}", GUILayout.Height(36)))
                    {
                        ProcessQuizAnswer(i);
                    }
                }

                GUILayout.EndArea();
                return; // Khi hiện bảng câu hỏi thì không hiện nút đánh bên dưới
            }

            // 3. HỘP THOẠI DIỄN BIẾN & CÁC NÚT HÀNH ĐỘNG (Dưới đáy màn hình)
            float bWidth = Mathf.Min(700, Screen.width - 40);
            float bHeight = 145;
            Rect bRect = new Rect((Screen.width - bWidth) / 2, Screen.height - bHeight - 15, bWidth, bHeight);

            GUILayout.BeginArea(bRect, GUI.skin.box);
            GUILayout.Label($"<b>MEDIEVAL TURN-BASED RPG</b> | Trạng thái: <b>{state}</b>", GUI.skin.label);

            string currentDiag = dialogueText != null ? dialogueText.text : "Trận chiến đang diễn ra...";
            GUILayout.Label($"<size=13>{currentDiag}</size>");

            GUILayout.FlexibleSpace();

            // Các nút chọn hành động trong lượt của người chơi
            if (state == BattleState.PLAYERTURN && !_isActionInProgress && CurrentHero != null)
            {
                GUILayout.BeginHorizontal();

                // Nút Đánh thường
                if (GUILayout.Button("⚔️ Tấn công", GUILayout.Height(35)))
                {
                    OnAttackButton();
                }

                // Nút Kỹ năng 1
                if (CurrentHero.skills != null && CurrentHero.skills.Count > 1)
                {
                    SkillData s1 = CurrentHero.skills[1];
                    if (GUILayout.Button($"✨ {s1.skillName} ({s1.manaCost} MP)", GUILayout.Height(35)))
                    {
                        OnSkill1Button();
                    }
                }

                // Nút Kỹ năng 2
                if (CurrentHero.skills != null && CurrentHero.skills.Count > 2)
                {
                    SkillData s2 = CurrentHero.skills[2];
                    if (GUILayout.Button($"🔥 {s2.skillName} ({s2.manaCost} MP)", GUILayout.Height(35)))
                    {
                        OnSkill2Button();
                    }
                }

                // Nút Hồi phục
                if (GUILayout.Button("💚 Hồi phục", GUILayout.Height(35)))
                {
                    OnHealButton();
                }

                // Nút Phòng thủ
                if (GUILayout.Button("🛡️ Phòng thủ", GUILayout.Height(35)))
                {
                    OnDefendButton();
                }

                GUILayout.EndHorizontal();
            }
            // Màn hình kết thúc khi Chiến thắng hoặc Thất bại
            else if (state == BattleState.WON || state == BattleState.LOST)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("🔄 Chơi lại trận này", GUILayout.Height(35)))
                {
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                if (GUILayout.Button("🏰 Về Menu chính", GUILayout.Height(35)))
                {
                    // Chuyển sang màn hình Menu chính nếu có
                    if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                    {
                        SceneManager.LoadScene("MainMenu");
                    }
                    else
                    {
                        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }
    }
}
