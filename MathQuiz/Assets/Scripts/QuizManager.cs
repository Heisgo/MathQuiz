using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    [Header("Configurações dos Níveis")]
    public Level levelData;

    [Header("Painéis de UI")]
    public GameObject quizPanel;

    [Header("Elementos da Tela de Quiz")]
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI questionText;
    public TMP_InputField answerInput;
    public Button submitButton;
    public GameObject multipleChoiceContainer;
    public Button multipleChoiceButtonPrefab;
    public Button nextButton;

    [Header("Feedback Visual")]
    public GameObject feedbackPanel;
    public TextMeshProUGUI feedbackText;

    private QuizSession session;

    void Start()
    {
        if(levelData != null) SetLevel(levelData);
    }

    #region Inicialização e Troca de Telas

    private void SwitchToQuizPanel() => ShowPanel(quizPanel);

    // Método auxiliar para trocar de painel
    private void ShowPanel(GameObject panelToShow)
    {
        quizPanel.SetActive(panelToShow == quizPanel);
    }

    #endregion

    #region Seleção e Exibição do Quiz

    // Chamado ao carregar a cena
    public void SetLevel(Level level)
    {
        session = new QuizSession(level);
        SwitchToQuizPanel();
        DisplayCurrentQuestion();
    }

    private void DisplayCurrentQuestion()
    {
        if (session == null)
        {
            Debug.LogError("Session não inicializada!");
            return;
        }

        if (session.IsFinished())
        {
            EndQuiz();
            return;
        }

        Question currentQuestion = session.GetCurrentQuestion();
        if (currentQuestion == null)
        {
            Debug.LogError("Questão atual é null");
            return;
        }
        UpdateQuestionTexts(currentQuestion);
        ResetAnswerState();
        ConfigureUIForQuestion(currentQuestion);
    }

    private void UpdateQuestionTexts(Question q)
    {
        StopAllCoroutines();
        StartCoroutine(Feel.Instance.TypeText(explanationText, q.explanation));
        StartCoroutine(Feel.Instance.TypeText(questionText, q.questionText));
    }

    private void ResetAnswerState() => nextButton.gameObject.SetActive(false);

    private void ConfigureUIForQuestion(Question q)
    {
        if (q.answerType == AnswerType.Input)
            SetupInputUI();
        else if (q.answerType == AnswerType.MultipleChoice)
            SetupMultipleChoiceUI(q);
    }

    private void SetAnswerUIActive(bool inputActive, bool submitActive, bool multipleChoiceActive)
    {
        if(answerInput != null)
            answerInput.gameObject.SetActive(inputActive);

        submitButton.gameObject.SetActive(submitActive);

        if (multipleChoiceContainer == null) return;
        multipleChoiceContainer.SetActive(multipleChoiceActive);
    }

    private void SetupInputUI()
    {
        SetAnswerUIActive(true, true, false);
        answerInput.text = "";
    }

    private void SetupMultipleChoiceUI(Question q)
    {
        SetAnswerUIActive(false, false, true);
        ClearMultipleChoiceButtons();
        CreateMultipleChoiceButtons(q.options);
    }

    private void ClearMultipleChoiceButtons()
    {
        foreach (Transform child in multipleChoiceContainer.transform)
            Destroy(child.gameObject);
    }

    private void CreateMultipleChoiceButtons(string[] options)
    {
        foreach (string option in options)
        {
            Button btn = Instantiate(multipleChoiceButtonPrefab, multipleChoiceContainer.transform);
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
                btnText.text = option;
            else
                Debug.LogError("O botão não possui um componente o TextMeshProUGUI");

            btn.onClick.AddListener(() => OnAnswerSelected(option));
        }
    }

    #endregion

    #region Processamento de Respostas

    public void SubmitAnswer()
    {
        if (session == null) return;
        ProcessAnswer(answerInput.text);
    }

    private void OnAnswerSelected(string option)
    {
        ProcessAnswer(option);
    }

    private void ProcessAnswer(string answer)
    {
        if (session.CurrentQuestionAnswered) return;
        bool isCorrect = session.CheckAnswer(answer);
        ShowFeedback(isCorrect);
        if (isCorrect)
        {
            Debug.Log("Resposta correta!");
            nextButton.gameObject.SetActive(true);
            return;
        }
        Debug.Log("Resposta incorreta!");
    }

    public void NextQuestion()
    {
        if (session != null)
        {
            session.MoveToNextQuestion();
            DisplayCurrentQuestion();
        }
    }

    private void EndQuiz()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextSceneIndex);
        else
            SceneManager.LoadScene("QuizCompleted");
    }

    #endregion
    #region Feedback & Visual
    private void ShowFeedback(bool isCorrect)
    {
        feedbackPanel.SetActive(true);
        feedbackPanel.GetComponent<Image>().color = isCorrect ? new Color(.231f, .78f, 0.498039f) : new Color(.78f, .23f, .29f);
        feedbackText.text = isCorrect ? "Resposta Correta!" : "Resposta Incorreta";

        RectTransform panelTransform = feedbackPanel.GetComponent<RectTransform>();

        // Pegamos a posição inicial para garantir que ele sempre volte ao lugar certo
        Vector3 startPosition = panelTransform.anchoredPosition;

        // Movemos para os lados com um efeito de "balanço" 3 vezes antes de desativar
        LeanTween.moveX(panelTransform, startPosition.x + 20f, 0.1f)
            .setEaseInOutSine()
            .setLoopPingPong(3)
            .setOnComplete(() => {
                feedbackPanel.SetActive(false);
                panelTransform.anchoredPosition = startPosition; // Reseta posição no final
            });
    }
    #endregion
}


public class QuizSession
{
    private Level currentLevel;
    private int currentQuestionIndex;
    private bool answeredCorrectly;

    public bool CurrentQuestionAnswered => answeredCorrectly;

    public QuizSession(Level level)
    {
        currentLevel = level;
        currentQuestionIndex = 0;
        answeredCorrectly = false;
    }

    public bool IsFinished() => currentQuestionIndex >= currentLevel.questions.Length - 1 && answeredCorrectly;

    public Question GetCurrentQuestion()
    {
        if (IsFinished()) return null;
        return currentLevel.questions[currentQuestionIndex];
    }

    // Verifica se a resposta está correta e atualiza o estado
    public bool CheckAnswer(string answer)
    {
        Question q = GetCurrentQuestion();
        if (q == null) return false;

        if (Normalize(answer) == Normalize(q.correctAnswer))
        {
            answeredCorrectly = true;
            return true;
        }
        return false;
    }

    public void MoveToNextQuestion()
    {
        if (currentQuestionIndex < currentLevel.questions.Length - 1)
        {
            currentQuestionIndex++;
            answeredCorrectly = false;
            return;
        }
        Debug.Log("Todas as questões foram respondidas!");
    }

    private string Normalize(string text)
    {
        return text.Trim().ToLower();
    }
}
