using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BeiShuiCS2
{
    /// <summary>
    /// 用户名/文本合法性验证器
    /// 过滤敏感词、违法内容、广告等
    /// </summary>
    public static class NameValidator
    {
        // 基础敏感词库 — 违反中国法律和公序良俗的内容
        private static readonly string[] BannedWords =
        {
            // 政治敏感
            "法轮", "falun", "邪教", "藏独", "疆独", "台独", "港独",
            // 暴力恐怖
            "自杀", "恐怖", "爆炸", "杀人", "贩毒", "枪支", "毒品",
            // 色情低俗
            "色情", "裸聊", "约炮", "援交", "一夜情", "成人", "av", "三级",
            // 诈骗广告
            "代练", "代打", "卖挂", "外挂", "作弊器", "脚本", "刷分", "刷级",
            "金币", "充值", "q币", "q币", "微信", "QQ群", "淘宝", "兼职",
            // 辱骂歧视
            "傻逼", "尼玛", "草泥马", "fuck", "shit", "dick", "asshole",
            " bitch", "cnm", "nmsl", "wqnmlgb", "sb", "煞笔", "脑残", "操你妈",
            // 违禁内容
            "赌博", "赌场", "博彩", "洗钱", "传销",
            // 其他违规
            "系统", "管理员", "客服", "官方", "admin", "root", "master"
        };

        // 正则模式 — 匹配特殊符号/纯数字/纯标点
        private static readonly Regex InvalidPattern = new Regex(
            @"^[\d\s\x21-\x2F\x3A-\x40\x5B-\x60\x7B-\x7E]+$",
            RegexOptions.Compiled);

        // 连续重复字符检测 (≥5个相同字符)
        private static readonly Regex RepeatPattern = new Regex(
            @"(.)\1{4,}",
            RegexOptions.Compiled);

        // 表情符号检测
        private static readonly Regex EmojiPattern = new Regex(
            @"[\uD800-\uDBFF][\uDC00-\uDFFF]",
            RegexOptions.Compiled);

        /// <summary>
        /// 验证用户名是否合法
        /// </summary>
        public static ValidationResult ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return ValidationResult.Fail("用户名不能为空");

            if (username.Length < 2 || username.Length > 20)
                return ValidationResult.Fail("用户名长度需在 2-20 个字符之间");

            // 不能纯数字或纯符号
            if (InvalidPattern.IsMatch(username))
                return ValidationResult.Fail("用户名不能包含纯数字或纯符号");

            // 不能包含表情符号
            if (EmojiPattern.IsMatch(username))
                return ValidationResult.Fail("用户名不能包含表情符号");

            // 不能连续重复字符
            if (RepeatPattern.IsMatch(username))
                return ValidationResult.Fail("用户名不能包含连续重复字符");

            // 检查敏感词
            string lower = username.ToLowerInvariant();
            foreach (var banned in BannedWords)
            {
                if (lower.Contains(banned))
                    return ValidationResult.Fail("用户名包含违规内容，请重新输入");
            }

            // 检查是否全是黑名单字符
            if (username.All(c => c < 0x4E00 || c > 0x9FFF) && // 不是中文
                username.All(c => (c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && (c < '0' || c > '9') && c != '_'))
                return ValidationResult.Fail("用户名包含无效字符");

            return ValidationResult.Ok();
        }

        /// <summary>
        /// 验证聊天/房间名称等文本内容
        /// </summary>
        public static ValidationResult ValidateChatText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return ValidationResult.Fail("内容不能为空");

            if (text.Length > 100)
                return ValidationResult.Fail("内容过长");

            string lower = text.ToLowerInvariant();
            foreach (var banned in BannedWords)
            {
                if (lower.Contains(banned))
                    return ValidationResult.Fail("内容包含违规词汇");
            }

            return ValidationResult.Ok();
        }

        /// <summary>
        /// 过滤文本中的敏感词（替换为***）
        /// </summary>
        public static string FilterBannedWords(string text)
        {
            string result = text;
            string lower = text.ToLowerInvariant();
            foreach (var banned in BannedWords)
            {
                int index;
                while ((index = lower.IndexOf(banned, StringComparison.Ordinal)) != -1)
                {
                    result = result.Remove(index, banned.Length)
                                   .Insert(index, new string('*', banned.Length));
                    lower = result.ToLowerInvariant();
                }
            }
            return result;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; private set; }
        public string ErrorMessage { get; private set; }

        private ValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            ErrorMessage = message;
        }

        public static ValidationResult Ok() => new ValidationResult(true, "");
        public static ValidationResult Fail(string msg) => new ValidationResult(false, msg);
    }
}
