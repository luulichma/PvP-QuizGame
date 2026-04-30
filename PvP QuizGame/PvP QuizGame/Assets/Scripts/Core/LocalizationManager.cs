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
    private string _previousLanguage = "vi";   // Dùng để rollback khi tải ngôn ngữ mới thất bại
    private bool _isReady = false;
    public bool IsReady => _isReady;

    private Coroutine _switchCoroutine;        // Coroutine đang chạy SwitchLanguage (để cancel khi cần)

    private const string CACHE_FILE_NAME = "localization_cache.csv";

    /// <summary>
    /// Các ngôn ngữ có file JSON local. Nếu thêm ngôn ngữ mới, cần thêm file .json vào
    /// StreamingAssets/Localization/ và khai báo ở đây.
    /// </summary>
    private static readonly HashSet<string> _supportedLocalLangs = new HashSet<string> { "vi", "en" };

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
            yield return StartCoroutine(LoadLocalLanguageCoroutine(savedLang, null));
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
    /// BUG FIX: Hủy coroutine cũ trước khi bắt đầu mới; rollback về ngôn ngữ cũ nếu tải thất bại.
    /// </summary>
    public void SwitchLanguage(string langCode)
    {
        // Hủy coroutine đang chạy (nếu có) để tránh race condition
        if (_switchCoroutine != null)
        {
            StopCoroutine(_switchCoroutine);
            _switchCoroutine = null;
        }

        _previousLanguage = _currentLanguage; // Lưu ngôn ngữ cũ để rollback nếu cần
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
        // Dùng coroutine và truyền _previousLanguage để rollback nếu file không tồn tại
        _switchCoroutine = StartCoroutine(LoadLocalLanguageCoroutine(langCode, _previousLanguage));
    }

    /// <summary>
    /// Nạp tệp ngôn ngữ từ StreamingAssets (Dự phòng).
    /// Public vì InitLocalization gọi fallback về đây.
    /// </summary>
    public void LoadLocalLanguage(string langCode)
    {
        if (_switchCoroutine != null) StopCoroutine(_switchCoroutine);
        _switchCoroutine = StartCoroutine(LoadLocalLanguageCoroutine(langCode, null));
    }

    /// <summary>
    /// BUG FIX: Nhận thêm tham số fallbackLang để rollback nếu file ngôn ngữ không tồn tại.
    /// Sử dụng string nối thay vì Path.Combine để đảm bảo URI jar:file:// hợp lệ trên Android.
    /// </summary>
    private IEnumerator LoadLocalLanguageCoroutine(string langCode, string fallbackLang)
    {
        // BUG FIX: Dùng string concat thay vì Path.Combine — Path.Combine không thiết kế cho URI
        // "jar:file:///...!/assets" + "/" + "Localization/vi.json" luôn cho kết quả đúng trên mọi platform
        string filePath = Application.streamingAssetsPath + "/Localization/" + langCode + ".json";
        string jsonContent = null;

        // Trên Android/WebGL, StreamingAssets là URI (jar:file:// hoặc http://), phải dùng UnityWebRequest
        if (filePath.Contains("://"))
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
                    Debug.LogWarning($"[Localization] Không tải được file ngôn ngữ '{langCode}': {request.error} | Path: {filePath}");
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
                Debug.LogWarning($"[Localization] Không tìm thấy file ngôn ngữ '{langCode}': {filePath}");
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
            _switchCoroutine = null;
            Debug.Log($"[Localization] Đã nạp JSON local: {_localizedText.Count} key (lang={langCode}).");
            OnLanguageChanged?.Invoke();
        }
        else
        {
            // BUG FIX: Khi tải thất bại, ROLLBACK về ngôn ngữ cũ thay vì giữ _currentLanguage sai
            // (trước đây: _localizedText ??= ... → giữ dữ liệu cũ nhưng _currentLanguage đã bị đổi)
            if (!string.IsNullOrEmpty(fallbackLang) && fallbackLang != langCode)
            {
                Debug.LogWarning($"[Localization] Rollback về ngôn ngữ trước: '{fallbackLang}' ('{langCode}' không có file local).");
                _currentLanguage = fallbackLang;
                PlayerPrefs.SetString("Language", fallbackLang);
                PlayerPrefs.Save();
                // _localizedText vẫn còn dữ liệu của fallbackLang từ lần load trước → không cần load lại
            }
            else
            {
                // Trường hợp init lần đầu mà không có file: set dict rỗng, đánh dấu ready để game không bị kẹt
                _localizedText ??= new Dictionary<string, string>();
            }

            _isReady = true;
            _switchCoroutine = null;
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
