using TMPro;
using UnityEngine;

public sealed class HudView
{
    private readonly TMP_Text day;
    private readonly TMP_Text location;
    private readonly TMP_Text gold;
    public HudView(TMP_Text day, TMP_Text location, TMP_Text gold) { this.day = day; this.location = location; this.gold = gold; }
    public void ShowNode(Node node) { if (day != null) day.text = node.surviveDate + " 일차"; if (location != null) location.text = node.worldLocation.ToString(); }
    public void ShowCurrency(CurrencyData data) { if (data != null && data.Name == "Gold" && gold != null) gold.text = data.Amount + "금"; }
}

public sealed class PlayerStatsView
{
    private readonly TMP_Text str;
    private readonly TMP_Text dex;
    private readonly TMP_Text con;
    public PlayerStatsView(TMP_Text str, TMP_Text dex, TMP_Text con) { this.str = str; this.dex = dex; this.con = con; }
    public void Show(Player player) { if (player == null) return; if (str != null) str.text = " : " + player.baseStats.str; if (dex != null) dex.text = " : " + player.baseStats.dex; if (con != null) con.text = " : " + player.baseStats.con; }
}

public sealed class LevelUpModal
{
    private readonly GameObject view;
    private bool isOpen;
    private float previousTimeScale;
    public LevelUpModal(GameObject view) { this.view = view; }
    public void Open()
    {
        if (isOpen) return;
        if (view == null)
        {
            Debug.LogError("[LevelUpModal] 레벨업 화면 참조가 없습니다.");
            return;
        }
        previousTimeScale = Time.timeScale;
        isOpen = true;
        Time.timeScale = 0;
        view.SetActive(true);
    }
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        Time.timeScale = previousTimeScale;
        if (view != null) view.SetActive(false);
    }
}
