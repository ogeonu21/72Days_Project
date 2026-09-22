using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>인벤토리 내부 상세 페이지. 게임의 시간/능력치를 변경하지 않는다.</summary>
public sealed class PlayerStatsDetailsUI : MonoBehaviour
{
    public Slider healthBar;
    public TMP_Text healthText;
    public TMP_Text attributesText;
    public TMP_Text attackText;
    public TMP_Text dodgeText;
    public TMP_Text accuracyText;
    public TMP_Text rangeText;
    private Player player;

    public void Bind(Player value)
    {
        if (player != null) player.StatsChanged -= Refresh;
        player = value;
        if (isActiveAndEnabled && player != null) player.StatsChanged += Refresh;
        Refresh();
    }

    private void OnEnable()
    {
        if (player != null) player.StatsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (player != null) player.StatsChanged -= Refresh;
    }

    public void Close() => gameObject.SetActive(false);

    public void Refresh()
    {
        if (player == null)
        {
            healthBar.value = 0;
            healthText.text = "플레이어 정보를 불러오는 중입니다.";
            attributesText.text = attackText.text = dodgeText.text = accuracyText.text = rangeText.text = "—";
            return;
        }
        var gear = player.GetEquipmentTuningStats();
        var basic = new Stats(player.baseStats, new TuningStats());
        healthBar.value = player.MaxHP > 0 ? Mathf.Clamp01((float)player.CurrentHP / player.MaxHP) : 0;
        healthText.text = $"체력  {player.CurrentHP} / {player.MaxHP}";
        attributesText.text = $"STR  {player.baseStats.str}     DEX  {player.baseStats.dex}     CON  {player.baseStats.con}";
        attackText.text = $"공격력  {player.AttackPower}\n기본·능력치 {basic.attackPower}  +  장비 {gear.attackBonus}  +  기타 보정 {Signed(player.tuningStats.attackBonus - gear.attackBonus)}";
        dodgeText.text = $"회피율  {player.DodgeRate * 100:F2}%  (상한 70%)\n능력치 {player.baseStats.dex * 2f:F2}%  +  장비 {gear.dodgeBonus * 100:F2}%  +  기타 {Signed((player.tuningStats.dodgeBonus - gear.dodgeBonus) * 100)}%p";
        accuracyText.text = $"명중 보정  +{player.AccuracyRate * 100:F2}%p\nDEX 기여분 · 장비 보정 없음\n실제 명중률은 공격 부위와 적 회피율에 따라 달라집니다.";
        rangeText.text = $"사거리  {player.AttackRange}\n무기 {gear.rangeBonus}  +  기타 보정 {Signed(player.tuningStats.rangeBonus - gear.rangeBonus)}";
    }

    private static string Signed(float value) => value.ToString("+0.##;-0.##;0");
}
