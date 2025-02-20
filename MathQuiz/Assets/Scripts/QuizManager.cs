using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gerencia a l�gica e a interface do Quiz na cena.
/// Respons�vel por configurar os n�veis, exibir as perguntas, tratar respostas e mostrar feedback visual.
/// </summary>
public class QuizManager : MonoBehaviour
{
    [Header("Configura��es dos N�veis")]
    [Tooltip("Dados do n�vel, que cont�m as quest�es e outras configura��es")]
    public Level levelData;

    [Header("Pain�is de UI")]
    [Tooltip("Painel principal onde o quiz � exibido.")]
    public GameObject quizPanel;

    [Header("Elementos da Tela de Quiz")]
    [Tooltip("Campo de texto para exibir a explica��o da quest�o.")]
    public TextMeshProUGUI explanationText;
    [Tooltip("Campo de texto para exibir a pergunta.")]
    public TextMeshProUGUI questionText;
    [Tooltip("Campo de entrada onde o usu�rio digita a resposta (para respostas do tipo Input).")]
    public TMP_InputField answerInput;
    [Tooltip("Bot�o para submeter a resposta digitada.")]
    public Button submitButton;
    [Tooltip("Container que agrupa os bot�es de m�ltipla escolha.")]
    public GameObject multipleChoiceContainer;
    [Tooltip("Prefab de bot�o usado para as op��es de m�ltipla escolha.")]
    public Button multipleChoiceButtonPrefab;
    [Tooltip("Bot�o para avan�ar para a pr�xima quest�o.")]
    public Button nextButton;

    [Header("Feedback Visual")]
    [Tooltip(" Painel que exibe o feedback visual (cor e mensagem) ap�s a resposta")]
    public GameObject feedbackPanel;
    [Tooltip("Texto que exibe o feedback (\"Resposta Correta!\" ou \"Resposta Incorreta\").")]
    public TextMeshProUGUI feedbackText;

    
    private QuizSession session; // Inst�ncia que gerencia a sess�o do quiz, contendo as perguntas e o estado atual

    /// <summary>
    /// M�todo chamado no in�cio. Verifica se h� dados de n�vel e inicia a sess�o do quiz
    /// </summary>
    void Start()
    {
        if(levelData != null) SetLevel(levelData);
    }

    #region Inicializa��o e Troca de Telas

    /// <summary>
    /// Alterna para o painel de quiz.
    /// </summary>
    private void SwitchToQuizPanel() => ShowPanel(quizPanel);

    /// <summary>
    /// Ativa o painel especificado e desativa os outros, se houver.
    /// </summary>
    /// <param name="panelToShow">Painel a ser exibido.</param>
    private void ShowPanel(GameObject panelToShow)
    {
        quizPanel.SetActive(panelToShow == quizPanel);
    }

    #endregion

    #region Sele��o e Exibi��o do Quiz

    /// <summary>
    /// Inicializa a sess�o do quiz com o n�vel especificado e exibe a primeira pergunta.
    /// Chamado ao carregar a cena.
    /// </summary>
    /// <param name="level">Dados do n�vel que cont�m as quest�es.</param>
    public void SetLevel(Level level)
    {
        session = new QuizSession(level);
        SwitchToQuizPanel();
        DisplayCurrentQuestion();
    }

    /// <summary>
    /// Exibe a quest�o atual da sess�o.
    /// Caso a sess�o esteja finalizada, finaliza o quiz.
    /// </summary>
    private void DisplayCurrentQuestion()
    {
        if (session == null) // Caso a sess�o seja nula, d� um erro e impede que a fun��o continue a executar.
        {
            Debug.LogError("Session n�o inicializada!");
            return;
        }

        if (session.IsFinished()) // Caso j� tenha terminado, chama a fun��o EndQuiz()
        {
            EndQuiz();
            return;
        }

        // Obt�m a quest�o atual da sess�o
        Question currentQuestion = session.GetCurrentQuestion();
        if (currentQuestion == null)
        {
            Debug.LogError("Quest�o atual � null");
            return;
        }

        // Atualiza os textos da explica��o e da pergunta
        UpdateQuestionTexts(currentQuestion);
        // Reseta o estado dos bot�es e inputs
        ResetAnswerState();
        // Configura a UI de acordo com o tipo de resposta (Input ou M�ltipla Escolha)
        ConfigureUIForQuestion(currentQuestion);
    }

    /// <summary>
    /// Atualiza os textos de explica��o e pergunta utilizando anima��o de digita��o.
    /// </summary>
    /// <param name="q">Quest�o atual com seus textos.</param>
    private void UpdateQuestionTexts(Question q)
    {
        StopAllCoroutines();
        //Utiliza uma coroutine para escrever o texto
        StartCoroutine(Feel.Instance.TypeText(explanationText, q.explanation));
        StartCoroutine(Feel.Instance.TypeText(questionText, q.questionText));
    }

    /// <summary>
    /// Reseta o estado do bot�o de avan�ar para a pr�xima quest�o.
    /// </summary>
    private void ResetAnswerState() => nextButton.gameObject.SetActive(false);

    /// <summary>
    /// Configura os elementos da interface de acordo com o tipo de resposta da quest�o.
    /// </summary>
    /// <param name="q">Quest�o atual.</param>
    private void ConfigureUIForQuestion(Question q)
    {
        if (q.answerType == AnswerType.Input)
            SetupInputUI();
        else if (q.answerType == AnswerType.MultipleChoice)
            SetupMultipleChoiceUI(q);
    }

    /// <summary>
    /// Define quais elementos de resposta devem estar ativos.
    /// </summary>
    /// <param name="inputActive">Ativa ou desativa o campo de entrada.</param>
    /// <param name="submitActive">Ativa ou desativa o bot�o de submeter.</param>
    /// <param name="multipleChoiceActive">Ativa ou desativa o container de m�ltipla escolha.</param>
    private void SetAnswerUIActive(bool inputActive, bool submitActive, bool multipleChoiceActive)
    {
        if(answerInput != null)
            answerInput.gameObject.SetActive(inputActive);

        submitButton.gameObject.SetActive(submitActive);

        if (multipleChoiceContainer == null) return;
        multipleChoiceContainer.SetActive(multipleChoiceActive);
    }

    /// <summary>
    /// Configura a UI para uma quest�o que utiliza resposta por input.
    /// </summary>
    private void SetupInputUI()
    {
        SetAnswerUIActive(true, true, false);
        answerInput.text = "";
    }

    /// <summary>
    /// Configura a UI para uma quest�o de m�ltipla escolha.
    /// Limpa op��es antigas e cria novos bot�es com base nas op��es fornecidas.
    /// </summary>
    /// <param name="q">Quest�o atual com as op��es de resposta.</param>
    private void SetupMultipleChoiceUI(Question q)
    {
        SetAnswerUIActive(false, false, true);
        ClearMultipleChoiceButtons();
        CreateMultipleChoiceButtons(q.options);
    }

    /// <summary>
    /// Remove todos os bot�es de m�ltipla escolha existentes.
    /// </summary>
    private void ClearMultipleChoiceButtons()
    {
        foreach (Transform child in multipleChoiceContainer.transform)
            Destroy(child.gameObject);
    }

    /// <summary>
    /// Cria bot�es de m�ltipla escolha para cada op��o dispon�vel.
    /// Cada bot�o, ao ser clicado, chama o m�todo OnAnswerSelected com a op��o correspondente.
    /// </summary>
    /// <param name="options">Array de op��es de resposta.</param>
    private void CreateMultipleChoiceButtons(string[] options)
    {
        foreach (string option in options)
        {
            Button btn = Instantiate(multipleChoiceButtonPrefab, multipleChoiceContainer.transform);
            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();

            if (btnText != null)
                btnText.text = option;
            else
                Debug.LogError("O bot�o n�o possui um componente o TextMeshProUGUI");

            btn.onClick.AddListener(() => OnAnswerSelected(option));
        }
    }

    #endregion

    #region Processamento de Respostas

    /// <summary>
    /// Chamado ao clicar no bot�o de submit. Processa a resposta digitada.
    /// </summary>
    public void SubmitAnswer()
    {
        if (session == null) return;
        ProcessAnswer(answerInput.text);
    }

    /// <summary>
    /// Chamado quando uma op��o de m�ltipla escolha � selecionada.
    /// Encaminha a resposta selecionada para processamento.
    /// </summary>
    /// <param name="option">Op��o selecionada.</param>
    private void OnAnswerSelected(string option)
    {
        ProcessAnswer(option);
    }

    /// <summary>
    /// Processa a resposta do usu�rio:
    /// - Verifica se a quest�o j� foi respondida;
    /// - Checa se a resposta est� correta;
    /// - Exibe feedback visual;
    /// - Caso correto, habilita o bot�o para avan�ar.
    /// </summary>
    /// <param name="answer">Resposta fornecida pelo usu�rio.</param>
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

    /// <summary>
    /// Avan�a para a pr�xima quest�o e atualiza a tela do quiz.
    /// </summary>
    public void NextQuestion()
    {
        if (session != null)
        {
            session.MoveToNextQuestion();
            DisplayCurrentQuestion();
        }
    }

    /// <summary>
    /// Finaliza o quiz, carregando a pr�xima cena se houver, ou uma cena de conclus�o.
    /// </summary>
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
    /// <summary>
    /// Exibe um feedback visual indicando se a resposta foi correta ou incorreta.
    /// Altera a cor e o texto do painel de feedback e aplica uma anima��o de "balan�o".
    /// </summary>
    /// <param name="isCorrect">Verdadeiro se a resposta estiver correta; falso caso contr�rio.</param>
    private void ShowFeedback(bool isCorrect)
    {
        feedbackPanel.SetActive(true);
        feedbackPanel.GetComponent<Image>().color = isCorrect ? new Color(.231f, .78f, 0.498039f) : new Color(.78f, .23f, .29f);
        feedbackText.text = isCorrect ? "Resposta Correta!" : "Resposta Incorreta";

        RectTransform panelTransform = feedbackPanel.GetComponent<RectTransform>();

        // Pegamos a posi��o inicial para garantir que ele sempre volte ao lugar certo
        Vector3 startPosition = panelTransform.anchoredPosition;

        // Movemos para os lados com um efeito de "balan�o" 3 vezes antes de desativar
        LeanTween.moveX(panelTransform, startPosition.x + 20f, 0.1f)
            .setEaseInOutSine()
            .setLoopPingPong(3)
            .setOnComplete(() => {
                feedbackPanel.SetActive(false);
                panelTransform.anchoredPosition = startPosition; // Reseta posi��o no final
            });
    }
    #endregion
}

/// <summary>
/// Gerencia a sess�o do quiz, armazenando o n�vel atual, �ndice da quest�o e se a quest�o foi respondida corretamente.
/// </summary>
public class QuizSession
{
    /// <summary>
    /// N�vel atual, que cont�m o array de quest�es.
    /// </summary>
    private Level currentLevel;
    /// <summary>
    /// �ndice da quest�o atual no array.
    /// </summary>
    private int currentQuestionIndex;
    /// <summary>
    /// Indica se a quest�o atual foi respondida corretamente.
    /// </summary>
    private bool answeredCorrectly;

    /// <summary>
    /// Propriedade que informa se a quest�o atual j� foi respondida corretamente.
    /// </summary>
    public bool CurrentQuestionAnswered => answeredCorrectly;

    /// <summary>
    /// Construtor da sess�o do quiz. Inicializa com o n�vel fornecido e prepara a primeira quest�o.
    /// </summary>
    /// <param name="level">N�vel com as quest�es a serem respondidas.</param>
    public QuizSession(Level level)
    {
        currentLevel = level;
        currentQuestionIndex = 0;
        answeredCorrectly = false;
    }

    /// <summary>
    /// Verifica se o quiz foi finalizado.
    /// Considera terminado se a �ltima quest�o foi respondida corretamente.
    /// </summary>
    /// <returns>Verdadeiro se o quiz estiver finalizado; caso contr�rio, falso.</returns>
    public bool IsFinished() => currentQuestionIndex >= currentLevel.questions.Length - 1 && answeredCorrectly;

    /// <summary>
    /// Retorna a quest�o atual da sess�o.
    /// Se o quiz estiver finalizado, retorna null.
    /// </summary>
    /// <returns>Objeto Question da quest�o atual ou null se finalizado.</returns>
    public Question GetCurrentQuestion()
    {
        if (IsFinished()) return null;
        return currentLevel.questions[currentQuestionIndex];
    }

    /// <summary>
    /// Verifica se a resposta fornecida est� correta, comparando com a resposta correta da quest�o atual.
    /// Aplica normaliza��o (trim e lower case) para evitar discrep�ncias.
    /// </summary>
    /// <param name="answer">Resposta fornecida pelo usu�rio.</param>
    /// <returns>Verdadeiro se a resposta estiver correta; caso contr�rio, falso.</returns>
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

    /// <summary>
    /// Avan�a para a pr�xima quest�o, se houver.
    /// Reseta o estado da resposta para a nova quest�o.
    /// </summary>
    public void MoveToNextQuestion()
    {
        if (currentQuestionIndex < currentLevel.questions.Length - 1)
        {
            currentQuestionIndex++;
            answeredCorrectly = false;
            return;
        }
        Debug.Log("Todas as quest�es foram respondidas!");
    }

    /// <summary>
    /// Normaliza o texto removendo espa�os extras e convertendo para min�sculas.
    /// Facilita a compara��o de respostas.
    /// </summary>
    /// <param name="text">Texto a ser normalizado.</param>
    /// <returns>Texto normalizado.</returns>
    private string Normalize(string text)
    {
        return text.Trim().ToLower();
    }
}
