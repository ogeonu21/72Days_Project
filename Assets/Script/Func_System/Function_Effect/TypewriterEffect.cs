using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

public static class TypewriterEffect
{
    public const float PagePauseSeconds = 0.7f;

    public static string FormatSentences(string text)
    {
        var result = new StringBuilder();
        text = text ?? string.Empty;
        bool pendingBreak = false;
        var groups = new Stack<char>();
        for (int i = 0; i < text.Length; i++)
        {
            // TMP 태그 속성의 마침표는 문장 구분자로 해석하지 않는다.
            if (text[i] == '<')
            {
                int end = text.IndexOf('>', i);
                if (end >= 0) { result.Append(text, i, end-i+1); i=end; continue; }
            }
            if (pendingBreak && char.IsWhiteSpace(text[i])) continue;
            if (pendingBreak && text[i]=='.') { result.Append('.'); continue; }
            if (pendingBreak) { result.Append('\n'); pendingBreak=false; }
            bool escaped=IsEscaped(text,i);
            if (!escaped && groups.Count>0 && text[i]==groups.Peek())
            {
                result.Append(text[i]); groups.Pop();
                if(groups.Count==0) pendingBreak=true;
                continue;
            }
            char closing;
            if (!escaped && GroupClosers.TryGetValue(text[i],out closing) &&
                !(text[i]=='\'' && i>0 && char.IsLetterOrDigit(text[i-1])))
            {
                // 묶음의 안쪽 마침표보다 바깥쪽 여는/닫는 기호를 우선한다.
                if(groups.Count==0) {
                    while(result.Length>0 && result[result.Length-1]!='\n' && char.IsWhiteSpace(result[result.Length-1])) result.Length--;
                    if(result.Length>0 && result[result.Length-1]!='\n') result.Append('\n');
                }
                result.Append(text[i]); groups.Push(closing); continue;
            }
            result.Append(text[i]);
            if (text[i] != '.' || groups.Count>0) continue;
            while (i+1 < text.Length && text[i+1] == '.') result.Append(text[++i]);
            pendingBreak = true;
        }
        return result.ToString();
    }

    private static readonly Dictionary<char,char> GroupClosers=new Dictionary<char,char> {
        {'"','"'}, {'\'','\''}, {'“','”'}, {'‘','’'}, {'[',']'}, {'(',')'}, {'{','}'}, {'「','」'}, {'『','』'}, {'«','»'}
    };
    private static bool IsEscaped(string text,int index)
    { int count=0; for(int i=index-1;i>=0 && text[i]=='\\';i--) count++; return count%2!=0; }

    public static List<string> BuildPages(TMP_Text target, string formattedText)
    {
        var pages = new List<string>();
        if (string.IsNullOrEmpty(formattedText)) { pages.Add(string.Empty); return pages; }
        Canvas.ForceUpdateCanvases();
        if (target.font == null || target.rectTransform.rect.width <= 0 || target.rectTransform.rect.height <= 0)
        { pages.Add(formattedText); return pages; }
        var oldOverflow = target.overflowMode;
        bool oldAutoSize = target.enableAutoSizing;
        int oldVisible = target.maxVisibleCharacters, oldPage = target.pageToDisplay;
        string oldText = target.text;
        try
        {
            // 영역을 넘는 글을 축소하지 않고 실제 폰트·행간·여백으로 페이지를 계산한다.
            target.enableAutoSizing = false;
            target.overflowMode = TextOverflowModes.Page;
            target.maxVisibleCharacters = int.MaxValue;
            target.pageToDisplay = 1;
            target.text = formattedText;
            target.ForceMeshUpdate(true, true);
            var info = target.textInfo;
            if (info.characterCount == 0) { pages.Add(formattedText); return pages; }
            int start = 0;
            for (int p = 1; p < info.pageCount; p++)
            {
                int next = info.pageInfo[p].firstCharacterIndex;
                if (next < 0 || next >= info.characterCount) continue;
                int end = info.characterInfo[next].index;
                if (end <= start || end > formattedText.Length) continue;
                pages.Add(PageText(formattedText, start, end, target.richText));
                start = end;
            }
            pages.Add(PageText(formattedText, start, formattedText.Length, target.richText));
            return pages;
        }
        finally
        {
            target.text = oldText;
            target.overflowMode = oldOverflow;
            target.enableAutoSizing = oldAutoSize;
            target.maxVisibleCharacters = oldVisible;
            target.pageToDisplay = oldPage;
        }
    }

    private static readonly Regex TagPattern = new Regex(@"<(/?)([a-zA-Z][a-zA-Z0-9-]*)(?:[ =][^>]*)?>");
    private static readonly HashSet<string> PairedTags = new HashSet<string> {
        "b","i","u","s","color","size","font","material","mark","voffset","align","line-height",
        "cspace","mspace","indent","line-indent","margin","margin-left","margin-right","width",
        "rotate","style","gradient","link","allcaps","lowercase","uppercase","smallcaps","nobr"
    };
    private static List<KeyValuePair<string,string>> OpenTags(string prefix)
    {
        var open = new List<KeyValuePair<string,string>>();
        foreach (Match match in TagPattern.Matches(prefix))
        {
            string name=match.Groups[2].Value.ToLowerInvariant();
            if (!PairedTags.Contains(name)) continue;
            if (match.Groups[1].Value == "/")
            { for (int i=open.Count-1;i>=0;i--) if(open[i].Key==name){open.RemoveAt(i);break;} }
            else open.Add(new KeyValuePair<string,string>(name,match.Value));
        }
        return open;
    }
    private static string PageText(string text,int start,int end,bool richText)
    {
        string content=text.Substring(start,end-start).Trim('\r','\n');
        if (!richText) return content;
        var result=new StringBuilder();
        foreach (var tag in OpenTags(text.Substring(0,start))) result.Append(tag.Value);
        result.Append(content);
        var remaining=OpenTags(text.Substring(0,end));
        for(int i=remaining.Count-1;i>=0;i--) result.Append("</").Append(remaining[i].Key).Append('>');
        return result.ToString();
    }
    // 코루틴 자체를 반환하여 호출하는 쪽에서 제어하도록 함
    public static IEnumerator TypeTextCoroutine(TMP_Text targetTextComponent, string textToType, float typingSpeed = 0.05f)
    {
        if (targetTextComponent == null)
        {
            Debug.LogError("타이핑 효과를 적용할 텍스트 컴포넌트가 null입니다.");
            yield break;
        }

        var pages = BuildPages(targetTextComponent, FormatSentences(textToType));
        float delay = Mathf.Max(0, typingSpeed-0.03f);
        int oldVisible = targetTextComponent.maxVisibleCharacters;
        try
        {
            for (int page=0;page<pages.Count;page++)
            {
                if (targetTextComponent==null || (Application.isPlaying && !targetTextComponent.gameObject.activeInHierarchy)) yield break;
                targetTextComponent.maxVisibleCharacters=int.MaxValue;
                targetTextComponent.text=string.Empty;
                var prefix=new StringBuilder();
                string pageText=pages[page];
                for(int index=0;index<pageText.Length;)
                {
                    if(targetTextComponent==null || (Application.isPlaying && !targetTextComponent.gameObject.activeInHierarchy)) yield break;
                    if(targetTextComponent.richText && pageText[index]=='<')
                    {
                        int end=pageText.IndexOf('>',index);
                        if(end>=0) { prefix.Append(pageText,index,end-index+1); index=end+1; continue; }
                    }
                    // 기존처럼 표시 문자열 자체를 한 글자씩 늘린다. 조합 문자·이모지는 쪼개지 않는다.
                    string letter=StringInfo.GetNextTextElement(pageText,index);
                    prefix.Append(letter); index+=letter.Length;
                    string visible=prefix.ToString();
                    if(targetTextComponent.richText) {
                        var tags=OpenTags(visible);
                        for(int i=tags.Count-1;i>=0;i--) visible+="</"+tags[i].Key+">";
                    }
                    targetTextComponent.text=visible;
                    yield return new WaitForSeconds(delay);
                }
                targetTextComponent.text=pageText;
                if(page+1<pages.Count) yield return new WaitForSeconds(PagePauseSeconds);
            }
        }
        finally { if(targetTextComponent!=null) targetTextComponent.maxVisibleCharacters=oldVisible; }
    }
}
