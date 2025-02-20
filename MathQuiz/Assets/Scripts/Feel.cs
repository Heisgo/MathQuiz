using System.Collections;
using TMPro;
using UnityEngine;

public class Feel : SingletonMonoBehaviour<Feel>
{
    public void AnimateButton(GameObject submitButton)
    {
        LeanTween.scale(submitButton, new Vector3(1.1f, 1.1f, 1), 0.2f).setLoopPingPong(1);
    }
    public IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText, float speed = 0.05f)
    {
        textComponent.text = string.Empty; // Limpa o texto antes de começar
        foreach (char letter in fullText)
        {
            textComponent.text += letter; // Adiciona uma letra
            yield return new WaitForSeconds(speed); // Aguarda um curto intervalo
        }
    }
}
