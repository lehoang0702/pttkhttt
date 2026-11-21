using System;
using UnityEngine;

namespace MedievalRPG.Quiz
{
    /// <summary>
    /// [Lớp Dữ Liệu Câu Hỏi Trắc Nghiệm]
    /// Định nghĩa cấu trúc của 1 câu hỏi giáo dục trong game:
    /// Gồm nội dung câu hỏi, 4 đáp án lựa chọn (A, B, C, D), vị trí đáp án đúng,
    /// độ khó và lời giải thích khi người chơi trả lời xong.
    /// </summary>
    [Serializable]
    public class QuizQuestion
    {
        [Header("Thông tin câu hỏi")]
        [Tooltip("Chủ đề câu hỏi (Ví dụ: Lịch sử, Toán học, Tiếng Anh, Địa lý...)")]
        public string category = "Kiến thức chung";

        [TextArea(2, 4)]
        [Tooltip("Nội dung câu hỏi hiển thị trên màn hình")]
        public string questionText;

        [Header("Các lựa chọn đáp án")]
        [Tooltip("Mảng 4 phương án trả lời tương ứng với A, B, C, D")]
        public string[] answers = new string[4];

        [Header("Đáp án chính xác")]
        [Range(0, 3)]
        [Tooltip("Chỉ số của đáp án đúng (0 = A, 1 = B, 2 = C, 3 = D)")]
        public int correctAnswerIndex = 0;

        [TextArea(1, 3)]
        [Tooltip("Lời giải thích chi tiết giúp người chơi học hỏi thêm sau khi trả lời")]
        public string explanation;

        [Header("Thưởng phạt")]
        [Tooltip("Điểm kinh nghiệm hoặc sát thương cộng thêm khi trả lời đúng")]
        public int bonusDamage = 5;

        /// <summary>
        /// Constructor tiện lợi để khởi tạo câu hỏi nhanh qua code
        /// </summary>
        public QuizQuestion(string category, string questionText, string[] answers, int correctAnswerIndex, string explanation = "", int bonusDamage = 5)
        {
            this.category = category;
            this.questionText = questionText;
            this.answers = answers;
            this.correctAnswerIndex = correctAnswerIndex;
            this.explanation = explanation;
            this.bonusDamage = bonusDamage;
        }

        /// <summary>
        /// Kiểm tra đáp án người chơi chọn có đúng không
        /// </summary>
        /// <param name="selectedIndex">Chỉ số đáp án người chơi chọn (0 - 3)</param>
        /// <returns>True nếu chọn đúng, False nếu chọn sai</returns>
        public bool IsCorrect(int selectedIndex)
        {
            return selectedIndex == correctAnswerIndex;
        }

        /// <summary>
        /// Lấy chuỗi ký tự hiển thị đáp án đúng (A, B, C, hoặc D kèm nội dung)
        /// </summary>
        public string GetCorrectAnswerString()
        {
            char label = (char)('A' + correctAnswerIndex);
            string text = (answers != null && correctAnswerIndex < answers.Length) ? answers[correctAnswerIndex] : "";
            return $"{label}. {text}";
        }
    }
}
