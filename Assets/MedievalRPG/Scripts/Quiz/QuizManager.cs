using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Quiz
{
    /// <summary>
    /// [Bộ Quản Lý Câu Hỏi Trắc Nghiệm - QuizManager]
    /// Chịu trách nhiệm:
    /// 1. Lưu trữ ngân hàng câu hỏi kiến thức (Lịch sử, Toán, Tiếng Anh...).
    /// 2. Bốc ngẫu nhiên câu hỏi khi nhân vật chuẩn bị hành động.
    /// 3. Chấm điểm đáp án và gửi kết quả về hệ thống chiến đấu (BattleSystem).
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        // Singleton pattern giúp dễ dàng truy cập từ bất kỳ script nào
        public static QuizManager Instance { get; private set; }

        [Header("Ngân hàng câu hỏi")]
        [Tooltip("Danh sách câu hỏi được thiết lập sẵn trong Inspector")]
        public List<QuizQuestion> questionBank = new List<QuizQuestion>();

        [Header("Trạng thái hiện tại")]
        public QuizQuestion currentQuestion;
        private List<int> _usedQuestionIndices = new List<int>();

        // Sự kiện thông báo khi người chơi trả lời xong (đúng/sai, câu hỏi vừa trả lời)
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

            // Nếu danh sách câu hỏi chưa được gán trong Inspector, tự động nạp ngân hàng câu hỏi mẫu
            if (questionBank == null || questionBank.Count == 0)
            {
                LoadDefaultQuestions();
            }
        }

        /// <summary>
        /// Nạp bộ câu hỏi giáo dục mẫu phong phú (Lịch sử thời trung cổ, Toán học, Tiếng Anh)
        /// Phù hợp hoàn hảo cho báo cáo môn học!
        /// </summary>
        public void LoadDefaultQuestions()
        {
            questionBank = new List<QuizQuestion>
            {
                // Câu 1: Lịch sử
                new QuizQuestion(
                    "Lịch sử",
                    "Ai là vị tướng lãnh đạo nhân dân ta ba lần đánh thắng quân Nguyên Mông?",
                    new string[] { "Lý Thường Kiệt", "Trần Hưng Đạo", "Quang Trung", "Lê Lợi" },
                    1,
                    "Hưng Đạo Đại Vương Trần Quốc Tuấn chỉ huy quân dân Đại Việt 3 lần đánh tan giặc Mông - Nguyên.",
                    8
                ),

                // Câu 2: Lịch sử
                new QuizQuestion(
                    "Lịch sử",
                    "Cuộc khởi nghĩa Lam Sơn (1418 - 1427) do ai lãnh đạo?",
                    new string[] { "Lê Lợi", "Ngô Quyền", "Đinh Bộ Lĩnh", "Trần Thủ Độ" },
                    0,
                    "Lê Thái Tổ (Lê Lợi) dựng cờ khởi nghĩa tại Lam Sơn, đánh đuổi giặc Minh, lập nên nhà Hậu Lê.",
                    8
                ),

                // Câu 3: Lịch sử
                new QuizQuestion(
                    "Lịch sử",
                    "Trận thủy chiến Bạch Đằng lịch sử năm 938 gắn liền với vị anh hùng dân tộc nào?",
                    new string[] { "Hai Bà Trưng", "Phan Bội Châu", "Ngô Quyền", "Lý Công Uẩn" },
                    2,
                    "Ngô Quyền dùng chiến thuật cọc ngầm đánh bại quân Nam Hán trên sông Bạch Đằng năm 938.",
                    8
                ),

                // Câu 4: Toán học
                new QuizQuestion(
                    "Toán học",
                    "Kết quả của phép tính: 15 x 4 - 25 là bao nhiêu?",
                    new string[] { "35", "45", "25", "30" },
                    0,
                    "15 x 4 = 60; 60 - 25 = 35.",
                    6
                ),

                // Câu 5: Toán học
                new QuizQuestion(
                    "Toán học",
                    "Số nguyên tố chẵn duy nhất trong tập hợp số tự nhiên là số nào?",
                    new string[] { "0", "4", "2", "6" },
                    2,
                    "Số 2 là số nguyên tố chẵn duy nhất vì nó chỉ chia hết cho 1 và chính nó.",
                    6
                ),

                // Câu 6: Tiếng Anh
                new QuizQuestion(
                    "Tiếng Anh",
                    "Từ nào trong tiếng Anh có nghĩa là 'Thanh kiếm' của hiệp sĩ?",
                    new string[] { "Shield", "Sword", "Armor", "Bow" },
                    1,
                    "'Sword' nghĩa là thanh kiếm; 'Shield' là khiên; 'Armor' là giáp; 'Bow' là cây cung.",
                    7
                ),

                // Câu 7: Tiếng Anh
                new QuizQuestion(
                    "Tiếng Anh",
                    "Chọn từ đúng điền vào chỗ trống: 'The brave knight _____ the dragon.'",
                    new string[] { "defeated", "defeats", "defeating", "defeat" },
                    0,
                    "Thì quá khứ đơn (Past Simple) diễn tả hành động đã xảy ra trong quá khứ -> 'defeated'.",
                    7
                ),

                // Câu 8: Khoa học
                new QuizQuestion(
                    "Khoa học",
                    "Kim loại nào dẫn điện tốt nhất ở điều kiện tiêu chuẩn?",
                    new string[] { "Vàng (Au)", "Đồng (Cu)", "Bạc (Ag)", "Nhôm (Al)" },
                    2,
                    "Bạc (Ag) là kim loại có độ dẫn điện cao nhất trong các kim loại.",
                    8
                )
            };
        }

        /// <summary>
        /// Bốc ngẫu nhiên một câu hỏi từ ngân hàng câu hỏi
        /// Đảm bảo không trùng lặp ngay các câu vừa trả lời
        /// </summary>
        public QuizQuestion GetRandomQuestion()
        {
            if (questionBank == null || questionBank.Count == 0)
            {
                LoadDefaultQuestions();
            }

            // Nếu đã hỏi hết tất cả các câu, reset danh sách đã dùng
            if (_usedQuestionIndices.Count >= questionBank.Count)
            {
                _usedQuestionIndices.Clear();
            }

            // Tìm một câu hỏi chưa dùng gần đây
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

        /// <summary>
        /// Người chơi gửi câu trả lời (0: A, 1: B, 2: C, 3: D)
        /// Trả về kết quả đúng hay sai cùng với lời giải thích
        /// </summary>
        public bool SubmitAnswer(int answerIndex, out string feedbackMessage, out int bonusDamage)
        {
            bonusDamage = 0;
            if (currentQuestion == null)
            {
                feedbackMessage = "Lỗi: Không tìm thấy câu hỏi đang kích hoạt!";
                return false;
            }

            bool correct = currentQuestion.IsCorrect(answerIndex);

            if (correct)
            {
                bonusDamage = currentQuestion.bonusDamage;
                feedbackMessage = $"<b>CHÍNH XÁC!</b> Đòn tấn công được tung ra với toàn bộ sức mạnh! (Thưởng +{bonusDamage} sát thương)\n<i>{currentQuestion.explanation}</i>";
            }
            else
            {
                string rightAns = currentQuestion.GetCorrectAnswerString();
                feedbackMessage = $"<b>RẤT TIẾC, SAI RỒI!</b> Đáp án đúng là <b>{rightAns}</b>.\nBạn bị mất lượt tấn công!\n<i>{currentQuestion.explanation}</i>";
            }

            // Kích hoạt sự kiện để UI hoặc các hệ thống khác cập nhật
            OnAnswerEvaluated?.Invoke(correct, currentQuestion);

            return correct;
        }
    }
}
