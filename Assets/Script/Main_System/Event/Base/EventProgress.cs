using System;
using System.Collections.Generic;

// SO에 실행 상태를 쓰지 않는다. GameManager가 소유하고 SaveManager가 저장한다.
[Serializable]
public sealed class EventProgress
{
    public List<string> claimed = new List<string>();
    public List<string> acceptedQuests = new List<string>();
    public List<string> completedQuests = new List<string>();
}
