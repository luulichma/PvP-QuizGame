using UnityEngine;

/// <summary>
/// ScriptableObject chứa dữ liệu của MỘT câu hỏi Quiz.
/// Tạo mới: chuột phải trong Project > Create > Quiz > Question Data
/// </summary>
[CreateAssetMenu(fileName = "NewQuestion", menuName = "Quiz/Question Data")]
public class QuestionData : ScriptableObject
{
    [Header("Nội dung câu hỏi")]
    [Tooltip("Key trong Unity Localization Table (hoặc nội dung trực tiếp nếu chưa dùng Localization)")]
    [TextArea(2, 4)]
    public string questionText;

    [Header("Đáp án (4 lựa chọn)")]
    [Tooltip("Mảng 4 đáp án A, B, C, D")]
    public string[] answers = new string[4];

    [Header("Đáp án đúng")]
    [Tooltip("Chỉ số của đáp án đúng (0 = A, 1 = B, 2 = C, 3 = D)")]
    [Range(0, 3)]
    public int correctAnswerIndex;
}
