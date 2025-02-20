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
    public string explanation;   // Texto explicativo � esquerda
    public string questionText;  // Enunciado da quest�o
    public AnswerType answerType; // Tipo de resposta
    public string[] options;      // Op��es para m�ltipla escolha
    public string correctAnswer;  // Resposta correta para verifica��o
}

