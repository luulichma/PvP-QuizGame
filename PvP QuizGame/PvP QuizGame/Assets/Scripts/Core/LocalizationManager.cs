using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using System.Collections;

/// <summary>
/// Quản lý đa ngôn ngữ cho toàn bộ Game.
/// Thứ tự ưu tiên nguồn dữ liệu (cao → thấp):
///   1. Google Sheet CSV (nếu có sheetUrl + có mạng).
///   2. Cache CSV đã tải về trước đó (PersistentDataPath).
///   3. JSON local trong StreamingAssets (vi.json, en.json...).
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public static event Action OnLanguageChanged;

    [Header("Remote Configuration")]
    [Tooltip("Link CSV của Google Sheet (Publish to Web → CSV). Để trống nếu chỉ dùng JSON local.")]
    public string sheetUrl = "";

    [Tooltip("Timeout (giây) khi tải Sheet. Quá thời gian này → fallback sang cache/local.")]
    public int sheetTimeoutSeconds = 6;

    private Dictionary<string, string> _localizedText;
    private string _currentLanguage = "vi";
    private bool _isReady = false;
    public bool IsReady => _isReady;

    private const string CACHE_FILE_NAME = "localization_cache.csv";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(InitLocalization());
    }

    private IEnumerator InitLocalization()
    {
        string savedLang = PlayerPrefs.GetString("Language", _currentLanguage);

        // 1. Ưu tiên tải từ Google Sheet nếu có Link
        if (!string.IsNullOrEmpty(sheetUrl))
        {
            yield return StartCoroutine(DownloadFromSheet(savedLang));
        }

        // 2. Nếu Sheet thất bại, thử cache CSV cũ
        if (!_isReady)
        {
            TryLoadFromCache(savedLang);
        }

        // 3. Cuối cùng nếu vẫn chưa có dữ liệu, nạp JSON local
        if (!_isReady)
        {
            yield return StartCoroutine(LoadLocalLanguageCoroutine(savedLang));
        }
    }

    private IEnumerator DownloadFromSheet(string langCode)
    {
        Debug.Log("[Localization] Đang tải dữ liệu từ Google Sheet...");
        using (UnityWebRequest request = UnityWebRequest.Get(sheetUrl))
        {
            request.timeout = sheetTimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string csv = request.downloadHandler.text;

                // Lưu cache để lần sau dùng được khi offline
                TrySaveCache(csv);

                if (ParseCSV(csv, langCode))
                {
                    _currentLanguage = langCode;
                    _isReady = true;
                    Debug.Log($"[Localization] Đã nạp từ Sheet: {_localizedText.Count} key (lang={langCode}).");
                    OnLanguageChanged?.Invoke();
                }
                else
                {
                    Debug.LogWarning("[Localization] Sheet tải về nhưng parse thất bại.");
                }
            }
            else
            {
                Debug.LogWarning($"[Localization] Lỗi tải từ Sheet ({request.error}). Sẽ thử cache/local.");
            }
        }
    }

    private void TryLoadFromCache(string langCode)
    {
        string cachePath = Path.Combine(Application.persistentDataPath, CACHE_FILE_NAME);
        if (!File.Exists(cachePath)) return;

        try
        {
            string csv = File.ReadAllText(cachePath);
            if (ParseCSV(csv, langCode))
            {
                _currentLanguage = langCode;
                _isReady = true;
                Debug.Log($"[Localization] Đã nạp từ cache: {_localizedText.Count} key (lang={langCode}).");
                OnLanguageChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Localization] Đọc cache thất bại: {ex.Message}");
        }
    }

    private void TrySaveCache(string csvText)
    {
        try
        {
            string cachePath = Path.Combine(Application.persistentDataPath, CACHE_FILE_NAME);
            File.WriteAllText(cachePath, csvText);
            Debug.Log($"[Localization] Đã lưu cache CSV: {cachePath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Localization] Không lưu được cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Chuyển đổi ngôn ngữ runtime. Ưu tiên nạp lại từ cache CSV để đảm bảo dữ liệu mới nhất.
    /// </summary>
    public void SwitchLanguage(string langCode)
    {
        _currentLanguage = langCode;
        PlayerPrefs.SetString("Language", langCode);
        PlayerPrefs.Save();

        // Thử parse lại từ cache CSV đang có sẵn (để lấy dữ liệu từ Google Sheet)
        string cachePath = Path.Combine(Application.persistentDataPath, CACHE_FILE_NAME);
        if (File.Exists(cachePath))
        {
            string csv = File.ReadAllText(cachePath);
            if (ParseCSV(csv, langCode))
            {
                Debug.Log($"[Localization] Đã chuyển ngôn ngữ: {langCode} (từ cache CSV)");
                _isReady = true;
                OnLanguageChanged?.Invoke();
                return;
            }
        }

        // Nếu không có cache hoặc parse lỗi, fallback về JSON local
        LoadLocalLanguage(langCode);
    }

    /// <summary>
    /// Nạp tệp ngôn ngữ từ StreamingAssets (Dự phòng).
    /// Public vì SwitchLanguage gọi fallback về đây.
    /// </summary>
    public void LoadLocalLanguage(string langCode)
    {
        StartCoroutine(LoadLocalLanguageCoroutine(langCode));
    }

    private IEnumerator LoadLocalLanguageCoroutine(string langCode)
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, $"Localization/{langCode}.json");
        string jsonContent = null;

        // Trên Android/WebGL, StreamingAssets là URI (jar:file:// hoặc http://), phải dùng UnityWebRequest
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            using (UnityWebRequest request = UnityWebRequest.Get(filePath))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    jsonContent = request.downloadHandler.text;
                }
                else
                {
                    Debug.LogError($"[Localization] Không tìm thấy hoặc lỗi tải tệp ngôn ngữ local: {filePath} - {request.error}");
                }
            }
        }
        else
        {
            // Trên PC/Editor/iOS có thể dùng File I/O bình thường
            if (File.Exists(filePath))
            {
                jsonContent = File.ReadAllText(filePath);
            }
            else
            {
                Debug.LogError($"[Localization] Không tìm thấy tệp ngôn ngữ: {filePath}");
            }
        }

        if (!string.IsNullOrEmpty(jsonContent))
        {
            LocalizationData data = JsonUtility.FromJson<LocalizationData>(jsonContent);

            _localizedText = new Dictionary<string, string>();
            foreach (var item in data.items)
            {
                _localizedText[item.key] = item.value;
            }

            _currentLanguage = langCode;
            _isReady = true;
            Debug.Log($"[Localization] Đã nạp JSON local: {_localizedText.Count} key (lang={langCode}).");
            OnLanguageChanged?.Invoke();
        }
        else
        {
            _localizedText ??= new Dictionary<string, string>();
            _isReady = true; // Đánh dấu sẵn sàng để không bị kẹt game, dù dữ liệu trống
            OnLanguageChanged?.Invoke();
        }
    }

    // ==================== CSV PARSE ====================

    /// <summary>
    /// Regex split CSV tôn trọng dấu nháy kép — không bị gãy khi câu hỏi có dấu phẩy.
    /// </summary>
    private static readonly Regex CsvSplitter =
        new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", RegexOptions.Compiled);

    /// <summary>
    /// Parse CSV với header đa ngôn ngữ. Trả về true nếu parse thành công và có ít nhất 1 key.
    /// </summary>
    private bool ParseCSV(string csvText, string targetLang)
    {
        if (string.IsNullOrWhiteSpace(csvText)) return false;

        // Tách dòng (xử lý cả \r\n, \n, \r)
        string[] lines = csvText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return false;

        // Header: Key, Vietnamese, English, French, Italian, German, Spanish, Japanese, Korean
        string[] headers = SplitCsvLine(lines[0]);
        int langColumnIndex = -1;

        // Ánh xạ mã ngôn ngữ sang tên cột trong Sheet
        string searchHeader = targetLang.ToLower() switch
        {
            "vi" => "vietnamese",
            "en" => "english",
            "fr" => "french",
            "it" => "italian",
            "de" => "german",
            "es" => "spanish",
            "ja" => "japanese",
            "ko" => "korean",
            _    => targetLang.ToLower()
        };

        for (int i = 1; i < headers.Length; i++)
        {
            if (headers[i].Trim().ToLower().Contains(searchHeader))
            {
                langColumnIndex = i;
                break;
            }
        }

        if (langColumnIndex == -1)
        {
            Debug.LogWarning($"[Localization] Không tìm thấy cột cho ngôn ngữ: {targetLang}. Header: [{string.Join(",", headers)}]");
            return false;
        }

        var dict = new Dictionary<string, string>();
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = SplitCsvLine(lines[i]);
            if (cols.Length > langColumnIndex)
            {
                string key = cols[0].Trim();
                string val = cols[langColumnIndex].Trim();

                if (string.IsNullOrEmpty(key)) continue;

                dict[key] = val;
            }
        }

        if (dict.Count == 0) return false;

        _localizedText = dict;
        return true;
    }

    /// <summary>
    /// Split 1 dòng CSV. Hỗ trợ giá trị có dấu phẩy nếu được bọc dấu nháy kép.
    /// Ví dụ: `q_001,"Theo bạn, AI có đáng tin?",Yes,No,Maybe`
    /// </summary>
    private static string[] SplitCsvLine(string line)
    {
        string[] cells = CsvSplitter.Split(line);
        for (int i = 0; i < cells.Length; i++)
        {
            // Bỏ dấu nháy kép bao ngoài và unescape `""` → `"`
            string c = cells[i].Trim();
            if (c.Length >= 2 && c[0] == '"' && c[c.Length - 1] == '"')
            {
                c = c.Substring(1, c.Length - 2).Replace("\"\"", "\"");
            }
            cells[i] = c;
        }
        return cells;
    }

    /// <summary>
    /// Lấy chuỗi đã dịch theo Key
    /// </summary>
    public string GetText(string key, string defaultValue = "")
    {
        if (_localizedText != null && _localizedText.ContainsKey(key))
        {
            return _localizedText[key];
        }
        return string.IsNullOrEmpty(defaultValue) ? $"[{key}]" : defaultValue;
    }

    public string CurrentLanguage => _currentLanguage;

    /// <summary>
    /// Trả về toàn bộ danh sách các Key hiện có trong bộ nhớ.
    /// Dùng để tự động lọc các key câu hỏi (q_...).
    /// </summary>
    public List<string> GetAllKeys()
    {
        if (_localizedText == null) return new List<string>();
        return new List<string>(_localizedText.Keys);
    }
}

[Serializable]
public class LocalizationItem
{
    public string key;
    public string value;
}

[Serializable]
public class LocalizationData
{
    public List<LocalizationItem> items;
}
