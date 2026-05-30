using System.Collections.Concurrent;
using System.Text.Json;

namespace BeiShui.AntiCheat;

/// <summary>
/// 反作弊数据存储 — JSON 文件，零数据库依赖
/// </summary>
public class AntiCheatStore
{
    private readonly string _dataDir;
    private readonly string _violationsFile;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ConcurrentQueue<ViolationEntry> _recentViolations = new();
    private int _totalCount;

    public AntiCheatStore()
    {
        _dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        Directory.CreateDirectory(_dataDir);
        _violationsFile = Path.Combine(_dataDir, "violations.json");
        _totalCount = 0;

        // 启动时加载已有记录
        LoadExisting();
    }

    /// <summary>
    /// 保存违规记录
    /// </summary>
    public async Task SaveViolationsAsync(List<string> alerts)
    {
        await _lock.WaitAsync();
        try
        {
            var entries = new List<ViolationEntry>();
            var now = DateTime.UtcNow;

            foreach (var alert in alerts)
            {
                var entry = new ViolationEntry
                {
                    Id = Guid.NewGuid().ToString("N")[..12],
                    Alert = alert,
                    Timestamp = now,
                    Source = "client"
                };
                entries.Add(entry);

                _recentViolations.Enqueue(entry);
                _totalCount++;
            }

            // 只保留最近1000条在内存
            while (_recentViolations.Count > 1000)
                _recentViolations.TryDequeue(out _);

            // 追加到 JSON 文件
            var existing = new List<ViolationEntry>();
            if (File.Exists(_violationsFile))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_violationsFile);
                    existing = JsonSerializer.Deserialize<List<ViolationEntry>>(json) ?? new();
                }
                catch
                {
                    existing = new();
                }
            }

            existing.AddRange(entries);

            // 只保留最近10000条在文件
            if (existing.Count > 10000)
                existing = existing.Skip(existing.Count - 10000).ToList();

            await File.WriteAllTextAsync(_violationsFile,
                JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 获取最近的违规记录
    /// </summary>
    public Task<List<ViolationEntry>> GetRecentViolationsAsync(int count)
    {
        var result = _recentViolations.Reverse().Take(count).ToList();
        return Task.FromResult(result);
    }

    /// <summary>
    /// 获取总违规数
    /// </summary>
    public int GetViolationCount() => _totalCount;

    private void LoadExisting()
    {
        try
        {
            if (File.Exists(_violationsFile))
            {
                var json = File.ReadAllText(_violationsFile);
                var existing = JsonSerializer.Deserialize<List<ViolationEntry>>(json);
                if (existing != null)
                {
                    _totalCount = existing.Count;
                    foreach (var e in existing.TakeLast(1000))
                        _recentViolations.Enqueue(e);
                }
            }
        }
        catch
        {
            // 文件损坏则重新开始
        }
    }

    public class ViolationEntry
    {
        public string Id { get; set; } = "";
        public string Alert { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = "";
    }
}
