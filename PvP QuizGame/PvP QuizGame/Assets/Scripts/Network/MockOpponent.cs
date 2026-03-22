using UnityEngine;
using System.Collections;

/// <summary>
/// [OFFLINE BOT] Giả lập Player 2 trong chế độ test offline.
/// Khi mỗi câu hỏi mới xuất hiện, Bot sẽ "suy nghĩ" trong
/// 1.5 - 4.0 giây rồi tự động nộp một đáp án ngẫu nhiên.
///
/// *** SCALE LÊN ONLINE ***
/// Class này sẽ bị XOÁ hoàn toàn khi lên Firebase Online.
/// (Thay vào đó là Firebase listener lắng nghe đáp án của Player 2 thật từ mây)
/// </summary>
public class MockOpponent : MonoBehaviour
{
    [Header("Cài đặt Bot")]
    [SerializeField] private float minThinkTime = 1.5f;
    [SerializeField] private float maxThinkTime = 4.0f;

    [Tooltip("Xác suất Bot trả lời đúng (0 = luôn sai, 1 = luôn đúng)")]
    [Range(0f, 1f)]
    [SerializeField] private float correctAnswerChance = 0.5f;

    private Coroutine _thinkRoutine;

    // ==================== LIFECYCLE ====================
    private void Start()
    {
        LocalMatchProvider.OnNewQuestionStarted += HandleNewQuestion;
    }

    private void OnDestroy()
    {
        LocalMatchProvider.OnNewQuestionStarted -= HandleNewQuestion;
    }

    // ==================== INTERNAL ====================
    private void HandleNewQuestion()
    {
        // Huỷ coroutine cũ nếu đang chạy (câu bị skip)
        if (_thinkRoutine != null)
            StopCoroutine(_thinkRoutine);

        _thinkRoutine = StartCoroutine(ThinkAndAnswer());
    }

    private IEnumerator ThinkAndAnswer()
    {
        float thinkTime = Random.Range(minThinkTime, maxThinkTime);
        Debug.Log($"[MockOpponent] 🤖 Bot đang suy nghĩ... ({thinkTime:F1}s)");

        yield return new WaitForSeconds(thinkTime);

        // Quyết định đáp án: có xác suất trả lời đúng
        int answerIndex;
        var currentQ = QuizManager.Instance?.CurrentQuestion;

        if (currentQ != null && Random.value < correctAnswerChance)
        {
            // Trả lời đúng
            answerIndex = currentQ.correctAnswerIndex;
        }
        else
        {
            // Random một đáp án sai
            answerIndex = Random.Range(0, 4);
        }

        Debug.Log($"[MockOpponent] 🤖 Bot chọn đáp án: {answerIndex}");
        LocalMatchProvider.Instance?.SubmitAnswerP2(answerIndex);
    }
}
