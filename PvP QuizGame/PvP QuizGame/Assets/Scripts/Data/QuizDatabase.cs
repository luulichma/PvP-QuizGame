using UnityEngine;

/// <summary>
/// ScriptableObject chứa TOÀN BỘ ngân hàng câu hỏi.
/// Tạo mới: chuột phải trong Project > Create > Quiz > Quiz Database
/// </summary>
[CreateAssetMenu(fileName = "QuizDatabase", menuName = "Quiz/Quiz Database")]
public class QuizDatabase : ScriptableObject
{
    [Header("Danh sách câu hỏi")]
    [Tooltip("Kéo thả các QuestionData vào đây")]
    public QuestionData[] questions;

    /// <summary>Tổng số câu hỏi trong database</summary>
    public int Count => questions?.Length ?? 0;
}
