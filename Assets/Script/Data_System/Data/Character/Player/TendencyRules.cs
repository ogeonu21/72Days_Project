/// <summary>저장된 성향 수치는 유지하고 분류와 아이콘 단계만 계산한다.</summary>
public static class TendencyRules
{
    public const int AlignmentThreshold = 4;
    public const int StrongThreshold = 8;

    // 왼쪽(현실주의)부터 오른쪽(이상주의) 순서.
    public static int GetIconIndex(int value)
    {
        if (value <= -StrongThreshold) return 0;
        if (value <= -AlignmentThreshold) return 1;
        if (value < AlignmentThreshold) return 2;
        return value < StrongThreshold ? 3 : 4;
    }

    public static string GetLabel(int value)
    {
        if (value <= -AlignmentThreshold) return "현실주의자";
        return value >= AlignmentThreshold ? "이상주의자" : "중립";
    }
}
