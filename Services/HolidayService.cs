using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace BeiShuiCS2.Services
{
    public class HolidayInfo
    {
        public string Name { get; set; } = "";
        public string Date { get; set; } = "";
        public string Message { get; set; } = "";
        public string Color1 { get; set; } = "#4ADE80";
        public string Color2 { get; set; } = "#2DD4BF";
        public string Emoji { get; set; } = "🎉";
    }

    public class WelfareInfo
    {
        public string Title { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Link { get; set; } = "";
        public string Color1 { get; set; } = "#4ADE80";
        public string Color2 { get; set; } = "#2DD4BF";
        public string Icon { get; set; } = "❤️";
    }

    public class BirthdayResult
    {
        public bool IsBirthday { get; set; }
        public string Birthday { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public static class HolidayService
    {
        // ===== 本地 fallback 数据（API 不可用时使用）=====
        private static readonly List<HolidayInfo> _fallbackHolidays = new()
        {
            new() { Name = "元旦", Date = "01-01", Message = "新年快乐。愿你的每一次瞄准都有归处。", Color1 = "#FFD700", Color2 = "#FF6B6B", Emoji = "🎆" },
            new() { Name = "春节", Date = "01-29", Message = "新春快乐，平安喜乐。回家吃饭。", Color1 = "#DC143C", Color2 = "#FF8C00", Emoji = "🧧" },
            // 母亲节日期动态计算（5月第二个周日），fallback 用 05-10 只是占位
            // 父亲节日期动态计算（6月第三个周日）
            new() { Name = "中秋节", Date = "10-06", Message = "月圆人团圆。中秋快乐。", Color1 = "#FF8C00", Color2 = "#FFD700", Emoji = "🥮" },
            new() { Name = "国庆节", Date = "10-01", Message = "国庆快乐。", Color1 = "#FF0000", Color2 = "#FFD700", Emoji = "🇨🇳" },
            new() { Name = "除夕", Date = "01-28", Message = "除夕快乐，万事顺遂。", Color1 = "#FF4500", Color2 = "#FFD700", Emoji = "🎇" },
            new() { Name = "重阳节", Date = "10-29", Message = "重阳安康。登高望远，想念的人也在想你。", Color1 = "#D2691E", Color2 = "#FFD700", Emoji = "🍁" },
            new() { Name = "圣诞节", Date = "12-25", Message = "圣诞快乐，平安喜乐。", Color1 = "#DC143C", Color2 = "#228B22", Emoji = "🎄" },
        };

        private static readonly List<WelfareInfo> _fallbackWelfare = new()
        {
            new() { Title = "困境儿童的CS梦", Summary = "帮助山区孩子拥有自己的第一台电脑", Link = "https://www.iesdouyin.com/share/video/7618221279753861363/", Color1 = "#FF6B35", Color2 = "#FFD700", Icon = "🎮" },
            new() { Title = "重阳敬老 · 爱在深秋", Summary = "陪伴是最长情的告白，关爱空巢老人", Link = "https://www.iesdouyin.com/share/video/7619318959103315826/", Color1 = "#D2691E", Color2 = "#FFD700", Icon = "🍁" },
        };

        /// <summary>
        /// 从 API 获取今天的节日，失败时用本地 fallback
        /// </summary>
        public static async Task<HolidayInfo?> GetTodayHolidayAsync()
        {
            try
            {
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement>("/api/holiday/today");
                if (result.Success && result.Data.ValueKind != System.Text.Json.JsonValueKind.Undefined
                    && result.Data.TryGetProperty("name", out _))
                {
                    return new HolidayInfo
                    {
                        Name = result.Data.GetProperty("name").GetString() ?? "",
                        Date = result.Data.TryGetProperty("date", out var d) ? d.GetString() ?? "" : "",
                        Message = result.Data.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "",
                        Color1 = result.Data.TryGetProperty("color1", out var c1) ? c1.GetString() ?? "#4ADE80" : "#4ADE80",
                        Color2 = result.Data.TryGetProperty("color2", out var c2) ? c2.GetString() ?? "#2DD4BF" : "#2DD4BF",
                        Emoji = result.Data.TryGetProperty("emoji", out var e) ? e.GetString() ?? "🎉" : "🎉",
                    };
                }
            }
            catch { }

            // Fallback 到本地（含动态节日：母亲节/父亲节）
            var today = DateTime.Now.ToString("MM-dd");

            // 动态计算母亲节（5月第二个星期日）
            var mothersDay = GetNthWeekday(2026, 5, 2, DayOfWeek.Sunday);
            if (today == mothersDay)
            {
                return new HolidayInfo { Name = "母亲节", Date = mothersDay, Message = "给妈妈打个电话吧。", Color1 = "#FF69B4", Color2 = "#FFB6C1", Emoji = "🌷" };
            }

            // 动态计算父亲节（6月第三个星期日）
            var fathersDay = GetNthWeekday(2026, 6, 3, DayOfWeek.Sunday);
            if (today == fathersDay)
            {
                return new HolidayInfo { Name = "父亲节", Date = fathersDay, Message = "陪老爸喝杯茶。", Color1 = "#4169E1", Color2 = "#87CEEB", Emoji = "👔" };
            }

            return _fallbackHolidays.FirstOrDefault(h => h.Date == today);
        }

        /// <summary>
        /// 从 API 获取公益项目列表
        /// </summary>
        public static async Task<List<WelfareInfo>> GetWelfareItemsAsync()
        {
            try
            {
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement[]>("/api/welfare/list");
                if (result.Success && result.Data != null)
                {
                    return result.Data.Select(item => new WelfareInfo
                    {
                        Title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                        Summary = item.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                        Link = item.TryGetProperty("link", out var l) ? l.GetString() ?? "" : "",
                        Color1 = item.TryGetProperty("color1", out var c1) ? c1.GetString() ?? "#4ADE80" : "#4ADE80",
                        Color2 = item.TryGetProperty("color2", out var c2) ? c2.GetString() ?? "#2DD4BF" : "#2DD4BF",
                        Icon = item.TryGetProperty("icon", out var i) ? i.GetString() ?? "❤️" : "❤️",
                    }).ToList();
                }
            }
            catch { }

            return new List<WelfareInfo>(_fallbackWelfare);
        }

        /// <summary>
        /// 检测用户今天是否是生日
        /// </summary>
        public static async Task<BirthdayResult?> CheckBirthdayAsync(long userId)
        {
            if (userId <= 0) return null;

            try
            {
                var result = await ApiClient.GetAsync<System.Text.Json.JsonElement>($"/api/holiday/birthday?userId={userId}");
                if (result.Success && result.Data.ValueKind != System.Text.Json.JsonValueKind.Undefined)
                {
                    return new BirthdayResult
                    {
                        IsBirthday = result.Data.TryGetProperty("isBirthday", out var ib) && ib.GetBoolean(),
                        Birthday = result.Data.TryGetProperty("birthday", out var b) ? b.GetString() ?? "" : "",
                        Message = result.Data.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "",
                    };
                }
            }
            catch { }

            return null;
        }

        public static List<HolidayInfo> GetFallbackHolidays() => _fallbackHolidays;

        /// <summary>
        /// 计算某个月的第N个星期几（用于浮动节日）
        /// </summary>
        private static string GetNthWeekday(int year, int month, int nth, DayOfWeek dayOfWeek)
        {
            var firstDay = new DateTime(year, month, 1);
            int daysToAdd = ((int)dayOfWeek - (int)firstDay.DayOfWeek + 7) % 7;
            daysToAdd += (nth - 1) * 7;
            return firstDay.AddDays(daysToAdd).ToString("MM-dd");
        }

        public static (Color c1, Color c2) ParseColors(string hex1, string hex2)
        {
            var c1 = (Color)ColorConverter.ConvertFromString(hex1);
            var c2 = (Color)ColorConverter.ConvertFromString(hex2);
            return (c1, c2);
        }
    }
}
