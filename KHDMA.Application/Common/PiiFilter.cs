using System.Text;
using System.Text.RegularExpressions;

namespace KHDMA.Application.Common
{
    /// <summary>
    /// Blocks contact details in chat messages (SRS 7.3).
    /// </summary>
    /// <remarks>
    /// The purpose is to stop customer and provider taking the job off-platform,
    /// so the check runs against a *normalised* copy of the text. Matching the raw
    /// string only would be trivially defeated by "0 1 0 - 1 2 3".
    /// This is a deterrent, not a guarantee: a determined pair can always spell a
    /// number in words. It is deliberately tuned to avoid false positives on
    /// ordinary conversation ("I'll be there in 20 minutes, flat 15").
    /// </remarks>
    public static class PiiFilter
    {
        private static readonly Regex EmailPattern = new(
            @"[\w\.\-\+]+\s*(@|\(at\)|\[at\])\s*[\w\-]+\s*(\.|\(dot\)|\[dot\])\s*\w{2,}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 7+ digits in a row after separators are stripped. An Egyptian mobile is
        // 11 digits; 7 is the shortest landline worth blocking. Prices ("250"),
        // durations ("45") and dates stay well under the threshold.
        private static readonly Regex LongDigitRun = new(@"\d{7,}", RegexOptions.Compiled);

        private static readonly Regex MessagingHandle = new(
            @"\b(whats\s*app|wa\.me|telegram|t\.me|insta(gram)?|facebook|messenger|viber|signal)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Dates, removed before normalisation.
        /// </summary>
        /// <remarks>
        /// Normalisation strips the separators used to disguise a phone number -
        /// but those are the same separators dates use, so "2026-07-22" would
        /// collapse to the 8-digit run "20260722" and be blocked. Scheduling talk is
        /// exactly what this chat is for, so the false positive is worse than the
        /// narrow evasion it opens (a number disguised as a date).
        /// </remarks>
        private static readonly Regex DatePattern = new(
            @"\b(\d{4}[-/.]\d{1,2}[-/.]\d{1,2}|\d{1,2}[-/.]\d{1,2}[-/.]\d{2,4})\b",
            RegexOptions.Compiled);

        /// <summary>Arabic-Indic and Eastern Arabic-Indic digits, which the Arabic keyboard produces.</summary>
        private const string ArabicDigits = "٠١٢٣٤٥٦٧٨٩۰۱۲۳۴۵۶۷۸۹";

        public static bool ContainsPii(string? text) => ContainsPii(text, out _);

        public static bool ContainsPii(string? text, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(text)) return false;

            if (EmailPattern.IsMatch(text))
            {
                reason = "Email addresses cannot be shared in chat.";
                return true;
            }

            if (MessagingHandle.IsMatch(text))
            {
                reason = "Links to outside messaging apps cannot be shared in chat.";
                return true;
            }

            if (LongDigitRun.IsMatch(Normalise(text)))
            {
                reason = "Phone numbers cannot be shared in chat.";
                return true;
            }

            return false;
        }

        /// <summary>
        /// Folds Arabic digits to ASCII and drops the separators used to disguise a
        /// number, so "٠١٠-١٢٣ ٤٥٦٧" and "010 123 4567" both collapse to one run.
        /// </summary>
        private static string Normalise(string text)
        {
            // Dates first - see DatePattern.
            text = DatePattern.Replace(text, " ");

            var sb = new StringBuilder(text.Length);

            foreach (var ch in text)
            {
                var idx = ArabicDigits.IndexOf(ch);
                if (idx >= 0)
                {
                    sb.Append((char)('0' + (idx % 10)));
                    continue;
                }

                if (char.IsDigit(ch)) { sb.Append(ch); continue; }

                // Anything a person might wedge between digits is removed entirely;
                // every other character becomes a hard separator so unrelated
                // numbers in one sentence are not concatenated into a false match.
                if (ch is ' ' or '-' or '.' or '_' or '/' or '\\' or '(' or ')' or '+' or '*' or '‏' or '‎')
                    continue;

                sb.Append('|');
            }

            return sb.ToString();
        }
    }
}
