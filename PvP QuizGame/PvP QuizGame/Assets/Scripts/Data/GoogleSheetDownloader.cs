using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Tự động tải CSV từ Google Sheet và nạp vào QuizDatabase mỗi khi mở game.
/// Attach script này vào một GameObject InitLoader ở Scene khởi tạo.
/// </summary>
public class GoogleSheetDownloader : MonoBehaviour
{
    [Header("Cấu hình Google Sheet")]
    [Tooltip("Link tải file dạng CSV lấy từ Publish to Web của Google Sheet")]
    public string csvExportUrl = "ĐIỀN_LINK_PUBLISH_CSV_VÀO_ĐÂY";

    [Header("Tham chiếu Database")]
    public QuizDatabase quizDatabase;

    /// <summary>Phát sự kiện khi tải xong dữ liệu</summary>
    public static event Action<bool> OnDataLoaded;

    private void Start()
    {
        if (string.IsNullOrEmpty(csvExportUrl) || csvExportUrl == "ĐIỀN_LINK_PUBLISH_CSV_VÀO_ĐÂY")
        {
            Debug.LogWarning("[GoogleSheet] Chưa có link CSV Google Sheet. Đang xài tạm data cũ.");
            OnDataLoaded?.Invoke(false);
            return;
        }

        StartCoroutine(DownloadSheetData());
    }

    private IEnumerator DownloadSheetData()
    {
        Debug.Log("[GoogleSheet] Đang kéo tải dữ liệu câu hỏi từ Cẩu Miêng...");
        using (UnityWebRequest request = UnityWebRequest.Get(csvExportUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("[GoogleSheet] Lỗi tải dữ liệu: " + request.error);
                OnDataLoaded?.Invoke(false);
            }
            else
            {
                string csvText = request.downloadHandler.text;
                ParseCSV(csvText);
                Debug.Log("[GoogleSheet] Tải và ép kiểu 100% thành công!");
                OnDataLoaded?.Invoke(true);
            }
        }
    }

    private void ParseCSV(string csvText)
    {
        // Tách theo dòng. Lưu ý Trim để xoá khoảng trắng thừa
        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Trừ đi 1 dòng Header ở trên cùng
        List<QuestionData> updatedQuestions = new List<QuestionData>();

        for (int i = 1; i < lines.Length; i++)
        {
            try
            {
                // Cắt theo dấu phẩy. Nếu câu hỏi có dấu phẩy thì xài Regex, nhưng bài này ta dùng ký tự đặc biệt như dấu | hoặc cố gắng tránh dấu phẩy, 
                // hoặc dùng parse CSV nâng cao sau. Tạm thời Split(",") chuẩn mức cơ bản.
                string[] columns = lines[i].Split(',');

                if (columns.Length >= 6)
                {
                    QuestionData newQObj = ScriptableObject.CreateInstance<QuestionData>();
                    
                    // Cấu trúc cột giả định: 
                    // 0: ID
                    // 1: QuestionText
                    // 2: Option A
                    // 3: Option B
                    // 4: Option C
                    // 5: Option D
                    // 6: Cột đáp án đúng (0, 1, 2, 3)

                    newQObj.questionText = columns[1];
                    newQObj.answers = new string[4] { columns[2], columns[3], columns[4], columns[5] };
                    
                    if (int.TryParse(columns[6], out int correctIdx))
                    {
                        newQObj.correctAnswerIndex = correctIdx;
                    }

                    // Để cho đẹp và dễ theo dõi trong Editor memory
                    newQObj.name = "Question_" + columns[0];

                    updatedQuestions.Add(newQObj);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GoogleSheet] Lỗi parse dòng {i}: {ex.Message}");
            }
        }

        // Nạp vào thẳng Database
        // CHÚ Ý: Vì ta thay thế data trong Runtime, nó sẽ biến mất khi tắt Game (tránh file rác)
        quizDatabase.questions = updatedQuestions.ToArray();
    }
}
