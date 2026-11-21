namespace Demo02.Narrative
{
    public static class TimeAnchorPuzzleRules
    {
        private static readonly string[] OrderedIds =
        {
            "past-02",
            "present-01",
            "present-03"
        };

        private static readonly string[] DisplayNames =
        {
            "西仓锚点",
            "东验锚点",
            "中册锚点"
        };

        public static int Count => OrderedIds.Length;
        public static string GetExpectedId(int stabilizedCount) =>
            stabilizedCount >= 0 && stabilizedCount < OrderedIds.Length
                ? OrderedIds[stabilizedCount]
                : string.Empty;

        public static string GetDisplayName(string anchorId)
        {
            int index = System.Array.IndexOf(OrderedIds, anchorId);
            return index >= 0 ? DisplayNames[index] : "未知锚点";
        }

        public static string OrderHint => "西仓 → 东验 → 中册";
    }
}
