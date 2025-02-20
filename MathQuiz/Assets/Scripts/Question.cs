using UnityEngine;

public enum AnswerType
{
    Input,
    MultipleChoice
}

[System.Serializable]
public class Question
{
    [TextArea]
    public string explanation;   // Texto explicativo à esquerda
    public string questionText;  // Enunciado da questão
    public AnswerType answerType; // Tipo de resposta
    public string[] options;      // Opções para múltipla escolha
    public string correctAnswer;  // Resposta correta para verificação
}

