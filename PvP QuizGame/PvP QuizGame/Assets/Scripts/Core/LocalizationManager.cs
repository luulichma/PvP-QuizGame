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

    /// <summary>[FIX-LOAD] Fire khi cả Sheet/Cache/JSON đều fail HOẶC số key parse được ít hơn ngưỡng.
    /// InitScene listen event này để hiện popup retry thay vì silent đi tiếp.</summary>
    public static event Action OnLocalizationFailed;

    [Header("Remote Configuration")]
    [Tooltip("Link CSV của Google Sheet (Publish to Web → CSV). Để trống nếu chỉ dùng JSON local.")]
    public string sheetUrl = "";

    [Tooltip("Timeout (giây) cho LẦN tải Sheet đầu tiên. Cold-start mạng cần thời gian DNS+TLS, nên giá trị này dùng cho lần đầu.")]
    public int sheetTimeoutSeconds = 6;

    [Header("[FIX-LOAD] Retry & Validation")]
    [Tooltip("Số lần retry tải Sheet trước khi fallback sang cache. Mỗi lần backoff timeout tăng dần.")]
    public int sheetMaxRetries = 3;

    [Tooltip("Backoff timeout (giây) cho từng lần retry. Mặc định 8 / 12 / 15.")]
    public int[] sheetRetryTimeouts = new int[] { 8, 12, 15 };

    [Tooltip("Số key tối thiểu mà ParseCSV cần parse được để được coi là HỢP LỆ. " +
             "Nếu parse ra ít hơn → coi như sheet hỏng, KHÔNG ghi đè cache, KHÔNG set _isReady.")]
    public int minExpectedKeys = 50;

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

    /// <summary>[FIX-LOAD] Public để InitScene gọi lại từ nút "Thử lại" trên popup.</summary>
    public void RetryInit()
    {
        if (_isReady) return; // đã có dữ liệu, không cần retry
        StartCoroutine(InitLocalization());
    }

    private IEnumerator InitLocalization()
    {
        string savedLang = PlayerPrefs.GetString("Language", _currentLanguage);

        // 1. Ưu tiên tải từ Google Sheet nếu có Link — có retry với backoff
        if (!string.IsNullOrEmpty(sheetUrl))
        {
            yield return StartCoroutine(DownloadFromSheetWithRetry(savedLang));
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

        // [FIX-LOAD][B3] Nếu sau cả 3 nguồn vẫn không ready hoặc dict quá nhỏ → báo Init biết
        if (!_isReady || _localizedText == null || _localizedText.Count < minExpectedKeys)
        {
            Debug.LogError($"[Localization] TẤT CẢ nguồn fallback đều fail hoặc thiếu key. " +
                           $"_isReady={_isReady}, keys={(_localizedText?.Count ?? 0)}/{minExpectedKeys}. " +
                           $"Fire OnLocalizationFailed.");
            OnLocalizationFailed?.Invoke();
        }
    }

    /// <summary>[FIX-LOAD][B1] Retry tải Sheet tối đa sheetMaxRetries lần với backoff timeout.</summary>
    private IEnumerator DownloadFromSheetWithRetry(string langCode)
    {
        for (int attempt = 0; attempt < sheetMaxRetries; attempt++)
        {
            int timeout = (sheetRetryTimeouts != null && attempt < sheetRetryTimeouts.Length)
                ? sheetRetryTimeouts[attempt]
                : sheetTimeoutSeconds;

            Debug.Log($"[Localization] Sheet attempt {attempt + 1}/{sheetMaxRetries}, timeout={timeout}s");
            yield return StartCoroutine(DownloadFromSheet(langCode, timeout));

            if (_isReady)
            {
                Debug.Log($"[Localization] Sheet OK ở attempt {attempt + 1}.");
                yield break;
            }

            // Nghỉ ngắn giữa các lần retry để mạng có thời gian phục hồi
            if (attempt < sheetMaxRetries - 1)
                yield return new WaitForSeconds(1.5f);
        }
        Debug.LogWarning($"[Localization] Đã retry {sheetMaxRetries} lần — Sheet vẫn fail, fallback sang cache/local.");
    }

    /// <summary>[FIX-LOAD] timeoutOverride > 0 sẽ override sheetTimeoutSeconds — dùng cho retry với backoff.</summary>
    private IEnumerator DownloadFromSheet(string langCode, int timeoutOverride = 0)
    {
        int effectiveTimeout = timeoutOverride > 0 ? timeoutOverride : sheetTimeoutSeconds;
        Debug.Log($"[Localization] Đang tải dữ liệu từ Google Sheet (timeout={effectiveTimeout}s)...");
        using (UnityWebRequest request = UnityWebRequest.Get(sheetUrl))
        {
            request.timeout = effectiveTimeout;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string csv = request.downloadHandler.text;

                // [FIX-LOAD][B2] Parse TRƯỚC, validate count TRƯỚC khi ghi cache.
                // Tránh trường hợp Sheet bị thay đổi format → parse được 1-2 key cũng ghi đè cache tốt cũ.
                if (ParseCSV(csv, langCode))
                {
                    int parsedCount = _localizedText?.Count ?? 0;
                    if (parsedCount < minExpectedKeys)
                    {
                        Debug.LogWarning($"[Localization] Sheet parse được {parsedCount} key < minExpected={minExpectedKeys}. " +
                                         "Coi như fail, KHÔNG ghi cache để giữ cache tốt cũ.");
                        // Rollback _localizedText vì ParseCSV đã gán
                        _localizedText = null;
                    }
                    else
                    {
                        // Parse đủ key → mới ghi cache + set ready
                        TrySaveCache(csv);
                        _currentLanguage = langCode;
                        _isReady = true;
                        Debug.Log($"[Localization] Đã nạp từ Sheet: {parsedCount} key (lang={langCode}).");
                        OnLanguageChanged?.Invoke();
                    }
                }
                else
                {
                    Debug.LogWarning("[Localization] Sheet tải về nhưng parse thất bại.");
                }
            }
            else
            {
                Debug.LogWarning($"[Localization] Lỗi tải từ Sheet ({request.error}). Sẽ retry / fallback.");
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
                // [FIX-LOAD][B2] Validate min keys cũng cho cache (đề phòng cache bị corrupt).
                int parsedCount = _localizedText?.Count ?? 0;
                if (parsedCount < minExpectedKeys)
                {
                    Debug.LogWarning($"[Localization] Cache có nhưng chỉ {parsedCount}/{minExpectedKeys} key — bỏ qua, thử JSON local.");
                    _localizedText = null;
                    return;
                }
                _currentLanguage = langCode;
                _isReady = true;
                Debug.Log($"[Localization] Đã nạp từ cache: {parsedCount} key (lang={langCode}).");
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

    public void SwitchLanguage(string langCode)
    {
        // Hủy coroutine đang chạy (nếu có) để tránh race condition
        if (_switchCoroutine != null)
        {
            StopCoroutine(_switchCoroutine);
            _switchCoroutine = null;
        }

        _switchCoroutine = StartCoroutine(SwitchLanguageCoroutine(langCode));
    }

    private IEnumerator SwitchLanguageCoroutine(string langCode)
    {
        _previousLanguage = _currentLanguage; // Lưu ngôn ngữ cũ để rollback nếu cần
        
        // 1. Thử parse từ cache CSV đang có sẵn (nhanh nhất)
        string cachePath = Path.Combine(Application.persistentDataPath, CACHE_FILE_NAME);
        if (File.Exists(cachePath))
        {
            string csv = File.ReadAllText(cachePath);
            if (ParseCSV(csv, langCode))
            {
                Debug.Log($"[Localization] Đã chuyển ngôn ngữ: {langCode} (từ cache CSV)");
                _currentLanguage = langCode;
                PlayerPrefs.SetString("Language", langCode);
                PlayerPrefs.Save();
                _isReady = true;
                OnLanguageChanged?.Invoke();
                _switchCoroutine = null;
                yield break;
            }
        }

        // 2. Nếu cache không có ngôn ngữ này, thử tải lại từ Google Sheet (nếu có mạng)
        if (!string.IsNullOrEmpty(sheetUrl))
        {
            Debug.Log($"[Localization] Ngôn ngữ '{langCode}' không có trong cache, đang tải lại từ Sheet...");
            yield return StartCoroutine(DownloadFromSheet(langCode));
            
            // Nếu download thành công và đã set _currentLanguage mới
            if (_isReady && _currentLanguage == langCode)
            {
                PlayerPrefs.SetString("Language", langCode);
                PlayerPrefs.Save();
                _switchCoroutine = null;
                yield break;
            }
        }

        // 3. Nếu Sheet thất bại (offline), fallback về JSON local
        Debug.LogWarning($"[Localization] Không tải được '{langCode}' từ Cache/Sheet, fallback về JSON local.");
        yield return StartCoroutine(LoadLocalLanguageCoroutine(langCode, _previousLanguage));
        _switchCoroutine = null;
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

            var dict = new Dictionary<string, string>();
            foreach (var item in data.items)
            {
                dict[item.key] = item.value;
            }

            // [FIX-LOAD][B2] Validate min keys cho JSON local. Nếu thiếu nghiêm trọng → KHÔNG set ready.
            if (dict.Count < minExpectedKeys)
            {
                Debug.LogError($"[Localization] JSON local chỉ có {dict.Count} key < minExpected={minExpectedKeys}. " +
                               $"Không set ready, để InitLocalization fire OnLocalizationFailed.");
                // Vẫn gán dict để game không hoàn toàn rỗng nếu user vẫn chọn vào — nhưng không set _isReady
                _localizedText = dict;
                _switchCoroutine = null;
                // KHÔNG set _isReady = true, KHÔNG fire OnLanguageChanged
                yield break;
            }

            _localizedText = dict;
            _currentLanguage = langCode;
            _isReady = true;
            _switchCoroutine = null;
            Debug.Log($"[Localization] Đã nạp JSON local: {_localizedText.Count} key (lang={langCode}).");
            OnLanguageChanged?.Invoke();
        }
        else
        {
            // BUG FIX: Khi tải thất bại, ROLLBACK về ngôn ngữ cũ thay vì giữ _currentLanguage sai
            if (!string.IsNullOrEmpty(fallbackLang) && fallbackLang != langCode
                && _localizedText != null && _localizedText.Count >= minExpectedKeys)
            {
                // [FIX-LOAD][B3] Chỉ rollback khi ngôn ngữ cũ THỰC SỰ còn data đủ.
                Debug.LogWarning($"[Localization] Rollback về ngôn ngữ trước: '{fallbackLang}' ('{langCode}' không có file local).");
                _currentLanguage = fallbackLang;
                PlayerPrefs.SetString("Language", fallbackLang);
                PlayerPrefs.Save();
                _isReady = true;
                _switchCoroutine = null;
                OnLanguageChanged?.Invoke();
            }
            else
            {
                // [FIX-LOAD][B3] KHÔNG còn silent set _isReady=true với dict rỗng nữa.
                // Để InitLocalization phát hiện và fire OnLocalizationFailed → InitScene hiện popup retry.
                Debug.LogError($"[Localization] JSON local '{langCode}' không tải được và không có fallback đủ data. " +
                               $"KHÔNG set _isReady — chờ InitLocalization fire OnLocalizationFailed.");
                _switchCoroutine = null;
            }
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
