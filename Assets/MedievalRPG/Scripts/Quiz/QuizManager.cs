using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Quiz
{
    /// <summary>
    /// [Bộ Quản Lý Câu Hỏi Trắc Nghiệm Tiếng Anh B1/B2 - QuizManager]
    /// Ngân hàng 30 câu hỏi tiếng Anh chuẩn CEFR B1 & B2 (Grammar, Vocabulary, Phrasal Verbs, Usage).
    /// Bốc ngẫu nhiên không trùng lặp cho tới khi hết vòng và chấm thưởng sát thương + Vàng.
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        public static QuizManager Instance { get; private set; }

        [Header("English B1/B2 Question Bank")]
        public List<QuizQuestion> questionBank = new List<QuizQuestion>();

        [Header("Active Question State")]
        public QuizQuestion currentQuestion;
        private readonly List<int> _usedQuestionIndices = new List<int>();

        public event Action<bool, QuizQuestion> OnAnswerEvaluated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (questionBank == null || questionBank.Count == 0 || !AreAllQuestionsEnglishCEFR())
            {
                LoadDefaultQuestions();
            }
        }

        private bool AreAllQuestionsEnglishCEFR()
        {
            if (questionBank == null || questionBank.Count == 0) return false;
            for (int i = 0; i < questionBank.Count; i++)
            {
                string cat = questionBank[i].category ?? "";
                if (!cat.StartsWith("B1", StringComparison.OrdinalIgnoreCase) &&
                    !cat.StartsWith("B2", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Nạp 30 câu hỏi tiếng Anh trình độ B1 (15 câu) và B2 (15 câu).
        /// </summary>
        public void LoadDefaultQuestions()
        {
            _usedQuestionIndices.Clear();
            questionBank = new List<QuizQuestion>
            {
                // ==================== CEFR B1 (15 QUESTIONS) ====================
                new QuizQuestion(
                    "B1 • Grammar",
                    "If it _____ tomorrow, we will delay our journey across the forest.",
                    new[] { "rain", "rains", "rained", "will rain" },
                    1,
                    "First Conditional uses 'If + Present Simple (rains), will + V1' for real future possibilities.",
                    7
                ),
                new QuizQuestion(
                    "B1 • Grammar",
                    "She has lived in this medieval town _____ nearly ten years.",
                    new[] { "since", "during", "for", "from" },
                    2,
                    "Use 'for' with a duration of time ('nearly ten years') and 'since' with a starting point.",
                    7
                ),
                new QuizQuestion(
                    "B1 • Grammar",
                    "The ancient bridge _____ by local villagers more than two centuries ago.",
                    new[] { "built", "was built", "has built", "was building" },
                    1,
                    "Past Simple Passive ('was built') is required because the bridge received the action in the past.",
                    7
                ),
                new QuizQuestion(
                    "B1 • Grammar",
                    "Would you mind _____ the gate before the night watch begins?",
                    new[] { "close", "to close", "closing", "closed" },
                    2,
                    "The polite structure 'Would you mind + V-ing' always takes a gerund ('closing').",
                    7
                ),
                new QuizQuestion(
                    "B1 • Grammar",
                    "If I _____ enough gold right now, I would buy that enchanted armor.",
                    new[] { "have", "had", "would have", "will have" },
                    1,
                    "Second Conditional ('If + Past Simple, would + V1') expresses an unreal situation in the present.",
                    7
                ),
                new QuizQuestion(
                    "B1 • Grammar",
                    "By the time the archers arrived at the camp, the scouts _____ the fire.",
                    new[] { "already lit", "have already lit", "had already lit", "were lighting" },
                    2,
                    "Past Perfect ('had already lit') describes an action completed before another past event.",
                    7
                ),
                new QuizQuestion(
                    "B1 • Grammar",
                    "This mountain trail is much _____ than the path along the river.",
                    new[] { "steep", "steeper", "more steeper", "steepest" },
                    1,
                    "Short one-syllable adjectives form the comparative with '-er' ('steeper').",
                    7
                ),
                new QuizQuestion(
                    "B1 • Grammar",
                    "The ranger _____ guided us through the mist knows every hidden cave.",
                    new[] { "which", "whom", "who", "whose" },
                    2,
                    "Use the relative pronoun 'who' as the subject referring to a person ('the ranger').",
                    7
                ),
                new QuizQuestion(
                    "B1 • Vocabulary",
                    "Before entering the dungeon, check your gear to make sure nothing is _____.",
                    new[] { "missing", "losing", "dropping", "failing" },
                    0,
                    "'Missing' means absent or not in the expected place ('nothing is missing').",
                    7
                ),
                new QuizQuestion(
                    "B1 • Vocabulary",
                    "He tried hard to _____ his teammates that taking the northern pass was safer.",
                    new[] { "insist", "convince", "suggest", "advise" },
                    1,
                    "'Convince someone that...' means to persuade someone to believe something.",
                    7
                ),
                new QuizQuestion(
                    "B1 • Vocabulary",
                    "Our supplies won't last long unless we use our rations _____.",
                    new[] { "carefully", "hardly", "nearly", "closely" },
                    0,
                    "'Carefully' fits the meaning ('use rations carefully'); 'hardly' means 'almost not'.",
                    7
                ),
                new QuizQuestion(
                    "B1 • Prepositions",
                    "The captain is responsible _____ protecting the outpost during the siege.",
                    new[] { "for", "of", "with", "about" },
                    0,
                    "Fixed collocation: 'be responsible for + V-ing / noun'.",
                    7
                ),
                new QuizQuestion(
                    "B1 • Grammar",
                    "You _____ carry heavy iron shields all day; the horses will transport them.",
                    new[] { "mustn't", "don't have to", "can't", "wouldn't" },
                    1,
                    "'Don't have to' expresses lack of necessity; 'mustn't' means prohibition.",
                    7
                ),
                new QuizQuestion(
                    "B1 • Vocabulary",
                    "Although the monster looked fierce, the knight remained _____ under pressure.",
                    new[] { "calm", "nervous", "anxious", "confused" },
                    0,
                    "'Although' introduces a contrast; 'calm' contrasts with a fierce threat.",
                    7
                ),
                new QuizQuestion(
                    "B1 • Grammar",
                    "Neither the mage nor the archer _____ been inside the dark fortress before.",
                    new[] { "have", "has", "was", "were" },
                    1,
                    "With 'neither... nor...', the verb agrees with the closer subject ('the archer' -> 'has').",
                    7
                ),

                // ==================== CEFR B2 (15 QUESTIONS) ====================
                new QuizQuestion(
                    "B2 • Grammar",
                    "Had the patrol noticed the tracks earlier, they _____ fallen into the ambush.",
                    new[] { "wouldn't", "hadn't", "wouldn't have", "won't have" },
                    2,
                    "Inverted Third Conditional ('Had + S + V3, S + wouldn't have + V3') for unreal past results.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Inversion",
                    "Not only _____ the enemy gate, but the vanguard also rescued the prisoners.",
                    new[] { "they breached", "did they breach", "breached they", "had they breach" },
                    1,
                    "Negative adverbials like 'Not only' at the start of a sentence trigger auxiliary inversion ('did they breach').",
                    9
                ),
                new QuizQuestion(
                    "B2 • Phrasal Verbs",
                    "Due to the violent storm, the expedition had to be _____ until dawn.",
                    new[] { "put off", "called on", "taken over", "broken into" },
                    0,
                    "'Put off' is a B2 phrasal verb meaning to postpone or delay.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Grammar",
                    "The blacksmith can't have forged this blade yesterday; he _____ all week.",
                    new[] { "was travelling", "has been travelling", "travels", "had travel" },
                    1,
                    "'Has been travelling all week' explains a continuing period up to the present.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Phrasal Verbs",
                    "While searching the ruins, the mage _____ a forgotten scroll hidden in a chest.",
                    new[] { "came across", "looked up to", "made up for", "got away with" },
                    0,
                    "'Come across' means to find or meet something/someone unexpectedly by chance.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Vocabulary",
                    "The general gave a _____ account of the battle, leaving out no crucial detail.",
                    new[] { "vague", "comprehensive", "reluctant", "temporary" },
                    1,
                    "'Comprehensive' means thorough and including all or nearly all elements.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Grammar",
                    "_____ exhausted after the long march, the heroes still stood ready to fight.",
                    new[] { "Despite", "Although", "Even", "However" },
                    1,
                    "Reduced concessive clause: 'Although (they were) exhausted' — 'Despite' takes a noun/V-ing.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Modal Verbs",
                    "The torch is still warm; the dark knights _____ left the campsite moments ago.",
                    new[] { "should have", "must have", "needn't have", "would rather" },
                    1,
                    "'Must have + V3' expresses strong logical deduction about a past event based on evidence.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Vocabulary",
                    "New recruits should _____ the courage of veteran knights in battle.",
                    new[] { "look up to", "look down on", "put up with", "run out of" },
                    0,
                    "'Look up to someone' means to admire and respect them.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Grammar",
                    "It is essential that every warrior _____ equipped with a healing potion.",
                    new[] { "is", "be", "was", "will be" },
                    1,
                    "Mandative subjunctive after 'It is essential that...' uses the bare infinitive ('be').",
                    9
                ),
                new QuizQuestion(
                    "B2 • Collocations",
                    "The merchant's generous discount helped _____ for the steep price of the elixir.",
                    new[] { "make up", "take part", "keep pace", "give way" },
                    0,
                    "'Make up for' means to compensate for something lost or negative.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Vocabulary",
                    "Their plan was rejected because the costs far _____ the expected benefits.",
                    new[] { "outweighed", "overcame", "undertook", "withstood" },
                    0,
                    "'Outweigh' means to be greater or more important than something else.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Inversion",
                    "Seldom _____ encountered such a well-coordinated ambush in the deep woods.",
                    new[] { "we have", "have we", "did we had", "we had" },
                    1,
                    "Restrictive adverb 'Seldom' at the beginning requires subject-auxiliary inversion ('have we').",
                    9
                ),
                new QuizQuestion(
                    "B2 • Grammar",
                    "I wish I _____ more mana potions before we climbed the cursed hill.",
                    new[] { "bought", "have bought", "had bought", "would buy" },
                    2,
                    "'Wish + Past Perfect (had bought)' expresses regret about a past action.",
                    9
                ),
                new QuizQuestion(
                    "B2 • Usage",
                    "The scouts worked tirelessly _____ they could map the boss's fortress before dusk.",
                    new[] { "in order to", "so that", "due to", "regardless of" },
                    1,
                    "Use 'so that + clause (they could...)' to express purpose; 'in order to' is followed by V1.",
                    9
                )
            };
        }

        public QuizQuestion GetRandomQuestion()
        {
            if (questionBank == null || questionBank.Count == 0 || !AreAllQuestionsEnglishCEFR())
            {
                LoadDefaultQuestions();
            }

            if (_usedQuestionIndices.Count >= questionBank.Count)
            {
                _usedQuestionIndices.Clear();
            }

            List<int> availableIndices = new List<int>();
            for (int i = 0; i < questionBank.Count; i++)
            {
                if (!_usedQuestionIndices.Contains(i))
                {
                    availableIndices.Add(i);
                }
            }

            int pickedIdx = availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
            _usedQuestionIndices.Add(pickedIdx);

            currentQuestion = questionBank[pickedIdx];
            return currentQuestion;
        }

        public bool SubmitAnswer(int answerIndex, out string feedbackMessage, out int bonusDamage)
        {
            bonusDamage = 0;
            if (currentQuestion == null)
            {
                feedbackMessage = "Error: No active quiz question found!";
                return false;
            }

            bool correct = currentQuestion.IsCorrect(answerIndex);

            if (correct)
            {
                bonusDamage = currentQuestion.bonusDamage;
                feedbackMessage = $"<b>CORRECT!</b> Full power unleashed! (+{bonusDamage} DMG, +10 Gold)\n<i>{currentQuestion.explanation}</i>";
            }
            else
            {
                string rightAns = currentQuestion.GetCorrectAnswerString();
                feedbackMessage = $"<b>INCORRECT!</b> Correct answer: <b>{rightAns}</b>.\nYour hero loses this turn!\n<i>{currentQuestion.explanation}</i>";
            }

            OnAnswerEvaluated?.Invoke(correct, currentQuestion);
            return correct;
        }
    }
}
