using UnityEngine;
using TMPro;

public class HUDView : MonoBehaviour
{
    public TextMeshProUGUI premiumText;   // gems
    public TextMeshProUGUI softText;      // coins ★
    public TextMeshProUGUI resourceText;  // energy ⬢
    public TextMeshProUGUI waveText;      // wave

    public void SetPremium(int v)  => premiumText.text  = v.ToString();
    public void SetSoft(int v)     => softText.text     = v.ToString();
    public void SetResource(int v) => resourceText.text = v.ToString();
    public void SetWave(int v)     => waveText.text     = $"Wave {v}";
}