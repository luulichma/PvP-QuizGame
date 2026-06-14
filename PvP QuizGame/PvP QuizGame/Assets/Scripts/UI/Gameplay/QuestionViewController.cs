using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;

/// <summary>
/// [REFACTOR-P2] Hiển thị câu hỏi: text + animation card + counter + progress bar.
/// Tách từ GameplayUIController_UXML.HandleQuestionChanged().
/// (Phần reset trạng thái đối thủ + timer khi sang câu mới nằm ở GameplayHUDController.OnNewQuestion.)
/// </summary>
public class QuestionViewController
{
    private readonly Label _questionText;
    private readonly Label _questionCounter;
    private readonly Label _questionProgressText;
    private readonly VisualElement _progressBarFill;

    public QuestionViewController(VisualElement root)
    {
        _questionText = root.Q<Label>("question-text");
        _questionCounter = root.Q<Label>("question-counter");
        _questionProgressText = root.Q<Label>("question-progress-text");
        _progressBarFill = root.Q<VisualElement>("progress-bar-fill");
    }

    public void ShowQuestion(QuestionData question)
    {
        if (question == null) return;

        if (_questionText != null)
        {
            string qKey = question.questionText;
            _questionText.text = LocalizationManager.Instance != null
                                 ? LocalizationManager.Instance.GetText(qKey)
                                 : qKey;

            // Animation: slide down + fade in cho question card
            var questionCard = _questionText.parent;
            if (questionCard != null)
            {
                questionCard.style.translate = new StyleTranslate(new Translate(new Length(0), new Length(-40)));
                questionCard.style.opacity = 0f;
                questionCard.style.scale = new StyleScale(new Scale(new Vector2(0.95f, 0.95f)));
                UIAnimator.DOFade(questionCard, 1f, 0.25f);
                UIAnimator.DOTranslate(questionCard, Vector2.zero, 0.35f).SetEase(Ease.OutBack);
                UIAnimator.DOScale(questionCard, Vector2.one, 0.35f).SetEase(Ease.OutBack);
            }
        }

        // Swoosh sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.swooshSound);

        // Counter + progress bar
        if (_questionCounter != null && QuizManager.Instance != null)
        {
            int answered = QuizManager.Instance.AnsweredCount + 1;
            int total    = QuizManager.Instance.TotalCount;

            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
            {
                string fmt = LocalizationManager.Instance.GetText("game_question_counter_title"); // e.g. "CÂU HỎI {0}"
                if (string.IsNullOrEmpty(fmt) || fmt.StartsWith("["))
                    fmt = "CÂU HỎI {0}";
                _questionCounter.text = string.Format(fmt, answered);
            }
            else
            {
                _questionCounter.text = $"CÂU HỎI {answered}";
            }

            if (_questionProgressText != null)
                _questionProgressText.text = $"{answered} / {total}";

            if (_progressBarFill != null)
            {
                float fillPercent = Mathf.Clamp01((float)answered / total) * 100f;
                DOTween.To(() => _progressBarFill.style.width.value.value,
                           x => _progressBarFill.style.width = Length.Percent(x),
                           fillPercent,
                           0.5f).SetEase(Ease.OutCubic);
            }
        }
    }

    /// <summary>Hiện text "đang tải câu hỏi" khi chưa có QuizManager (gọi từ LocalizeHUD).</summary>
    public void ShowLoadingText()
    {
        if (_questionText != null && QuizManager.Instance == null &&
            LocalizationManager.Instance != null && LocalizationManager.Instance.IsReady)
        {
            _questionText.text = LocalizationManager.Instance.GetText("game_loading_question");
        }
    }
}
