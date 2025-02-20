using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gerencia a lógica e a interface do Quiz na cena.
/// Responsável por configurar os níveis, exibir as perguntas, tratar respostas e mostrar feedback visual.
/// </summary>
public class QuizManager : MonoBehaviour
{
    [Header("Configurações dos Níveis")]
    [Tooltip("Dados do nível, que contém as questões e outras configurações")]
    public Level levelData;

    [Header("Painéis de UI")]
    [Tooltip("Painel principal onde o quiz é exibido.")]
    public GameObject quizPanel;

    [Header("Elementos da Tela de Quiz")]
    [Tooltip("Campo de texto para exibir a explicação da questão.")]
    public TextMeshProUGUI explanationText;
    [Tooltip("Campo de texto para exibir a pergunta.")]
    public TextMeshProUGUI questionText;
    [Tooltip("Campo de entrada onde o usuário digita a resposta (para respostas do tipo Input).")]
    public TMP_InputField answerInput;
    [Tooltip("Botão para submeter a resposta digitada.")]
    public Button submitButton;
    [Tooltip("Container que agrupa os botões de múltipla escolha.")]
    public GameObject multipleChoiceContainer;
    [Tooltip("Prefab de botão usado para as opções de múltipla escolha.")]
    public Button multipleChoiceButtonPrefab;
    [Tooltip("Botão para avançar para a próxima questão.")]
    public Button nextButton;

    [Header("Feedback Visual")]
    [Tooltip(" Painel que exibe o feedback visual (cor e mensagem) após a resposta")]
    public GameObject feedbackPanel;
    [Tooltip("Texto que exibe o feedback (\"Resposta Correta!\" ou \"Resposta Incorreta\").")]
    public TextMeshProUGUI feedbackText;

    
    private QuizSession session; // Instância que gerencia a sessão do quiz, contendo as perguntas e o estado atual

    /// <summary>
    /// Método chamado no início. Verifica se há dados de nível e inicia a sessão do quiz
    /// </summary>
    void Start()
    {
        if(levelData != null) SetLevel(levelData);
    }

    #region Inicialização e Troca de Telas

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

    #region Seleção e Exibição do Quiz

    /// <summary>
    /// Inicializa a sessão do quiz com o nível especificado e exibe a primeira pergunta.
    /// Chamado ao carregar a cena.
    /// </summary>
    /// <param name="level">Dados do nível que contém as questões.</param>
    public void SetLevel(Level level)
    {
        session = new QuizSession(level);
        SwitchToQuizPanel();
        DisplayCurrentQuestion();
    }

    /// <summary>
    /// Exibe a questão atual da sessão.
    /// Caso a sessão esteja finalizada, finaliza o quiz.
    /// </summary>
    private void DisplayCurrentQuestion()
    {
        if (session == null) // Caso a sessão seja nula, dá um erro e impede que a função continue a executar.
        {
            Debug.LogError("Session não inicializada!");
            return;
        }

        if (session.IsFinished()) // Caso já tenha terminado, chama a função EndQuiz()
        {
            EndQuiz();
            return;
        }

        // Obtém a questão atual da sessão
        Question currentQuestion = session.GetCurrentQuestion();
        if (currentQuestion == null)
        {
            Debug.LogError("Questão atual é null");
            return;
        }

        // Atualiza os textos da explicação e da pergunta
        UpdateQuestionTexts(currentQuestion);
        // Reseta o estado dos botões e inputs
        ResetAnswerState();
        // Configura a UI de acordo com o tipo de resposta (Input ou Múltipla Escolha)
        ConfigureUIForQuestion(currentQuestion);
    }

    /// <summary>
    /// Atualiza os textos de explicação e pergunta utilizando animação de digitação.
    /// </summary>
    /// <param name="q">Questão atual com seus textos.</param>
    private void UpdateQuestionTexts(Question q)
    {
        StopAllCoroutines();
        //Utiliza uma coroutine para escrever o texto
        StartCoroutine(Feel.Instance.TypeText(explanationText, q.explanation));
        StartCoroutine(Feel.Instance.TypeText(questionText, q.questionText));
    }

    /// <summary>
    /// Reseta o estado do botão de avançar para a próxima questão.
    /// </summary>
    private void ResetAnswerState() => nextButton.gameObject.SetActive(false);

    /// <summary>
    /// Configura os elementos da interface de acordo com o tipo de resposta da questão.
    /// </summary>
    /// <param name="q">Questão atual.</param>
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
    /// <param name="submitActive">Ativa ou desativa o botão de submeter.</param>
    /// <param name="multipleChoiceActive">Ativa ou desativa o container de múltipla escolha.</param>
    private void SetAnswerUIActive(bool inputActive, bool submitActive, bool multipleChoiceActive)
    {
        if(answerInput != null)
            answerInput.gameObject.SetActive(inputActive);

        submitButton.gameObject.SetActive(submitActive);

        if (multipleChoiceContainer == null) return;
        multipleChoiceContainer.SetActive(multipleChoiceActive);
    }

    /// <summary>
    /// Configura a UI para uma questão que utiliza resposta por input.
    /// </summary>
    private void SetupInputUI()
    {
        SetAnswerUIActive(true, true, false);
        answerInput.text = "";
    }

    /// <summary>
    /// Configura a UI para uma questão de múltipla escolha.
    /// Limpa opções antigas e cria novos botões com base nas opções fornecidas.
    /// </summary>
    /// <param name="q">Questão atual com as opções de resposta.</param>
    private void SetupMultipleChoiceUI(Question q)
    {
        SetAnswerUIActive(false, false, true);
        ClearMultipleChoiceButtons();
        CreateMultipleChoiceButtons(q.options);
    }

    /// <summary>
    /// Remove todos os botões de múltipla escolha existentes.
    /// </summary>
    private void ClearMultipleChoiceButtons()
    {
        foreach (Transform child in multipleChoiceContainer.transform)
            Destroy(child.gameObject);
    }

    /// <summary>
    /// Cria botões de múltipla escolha para cada opção disponível.
    /// Cada botão, ao ser clicado, chama o método OnAnswerSelected com a opção correspondente.
    /// </summary>
    /// <param name="options">Array de opções de resposta.</param>
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

    /// <summary>
    /// Chamado ao clicar no botão de submit. Processa a resposta digitada.
    /// </summary>
    public void SubmitAnswer()
    {
        if (session == null) return;
        ProcessAnswer(answerInput.text);
    }

    /// <summary>
    /// Chamado quando uma opção de múltipla escolha é selecionada.
    /// Encaminha a resposta selecionada para processamento.
    /// </summary>
    /// <param name="option">Opção selecionada.</param>
    private void OnAnswerSelected(string option)
    {
        ProcessAnswer(option);
    }

    /// <summary>
    /// Processa a resposta do usuário:
    /// - Verifica se a questão já foi respondida;
    /// - Checa se a resposta está correta;
    /// - Exibe feedback visual;
    /// - Caso correto, habilita o botão para avançar.
    /// </summary>
    /// <param name="answer">Resposta fornecida pelo usuário.</param>
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
    /// Avança para a próxima questão e atualiza a tela do quiz.
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
    /// Finaliza o quiz, carregando a próxima cena se houver, ou uma cena de conclusão.
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
    /// Altera a cor e o texto do painel de feedback e aplica uma animação de "balanço".
    /// </summary>
    /// <param name="isCorrect">Verdadeiro se a resposta estiver correta; falso caso contrário.</param>
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

/// <summary>
/// Gerencia a sessão do quiz, armazenando o nível atual, índice da questão e se a questão foi respondida corretamente.
/// </summary>
public class QuizSession
{
    /// <summary>
    /// Nível atual, que contém o array de questões.
    /// </summary>
    private Level currentLevel;
    /// <summary>
    /// Índice da questão atual no array.
    /// </summary>
    private int currentQuestionIndex;
    /// <summary>
    /// Indica se a questão atual foi respondida corretamente.
    /// </summary>
    private bool answeredCorrectly;

    /// <summary>
    /// Propriedade que informa se a questão atual já foi respondida corretamente.
    /// </summary>
    public bool CurrentQuestionAnswered => answeredCorrectly;

    /// <summary>
    /// Construtor da sessão do quiz. Inicializa com o nível fornecido e prepara a primeira questão.
    /// </summary>
    /// <param name="level">Nível com as questões a serem respondidas.</param>
    public QuizSession(Level level)
    {
        currentLevel = level;
        currentQuestionIndex = 0;
        answeredCorrectly = false;
    }

    /// <summary>
    /// Verifica se o quiz foi finalizado.
    /// Considera terminado se a última questão foi respondida corretamente.
    /// </summary>
    /// <returns>Verdadeiro se o quiz estiver finalizado; caso contrário, falso.</returns>
    public bool IsFinished() => currentQuestionIndex >= currentLevel.questions.Length - 1 && answeredCorrectly;

    /// <summary>
    /// Retorna a questão atual da sessão.
    /// Se o quiz estiver finalizado, retorna null.
    /// </summary>
    /// <returns>Objeto Question da questão atual ou null se finalizado.</returns>
    public Question GetCurrentQuestion()
    {
        if (IsFinished()) return null;
        return currentLevel.questions[currentQuestionIndex];
    }

    /// <summary>
    /// Verifica se a resposta fornecida está correta, comparando com a resposta correta da questão atual.
    /// Aplica normalização (trim e lower case) para evitar discrepâncias.
    /// </summary>
    /// <param name="answer">Resposta fornecida pelo usuário.</param>
    /// <returns>Verdadeiro se a resposta estiver correta; caso contrário, falso.</returns>
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
    /// Avança para a próxima questão, se houver.
    /// Reseta o estado da resposta para a nova questão.
    /// </summary>
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

    /// <summary>
    /// Normaliza o texto removendo espaços extras e convertendo para minúsculas.
    /// Facilita a comparação de respostas.
    /// </summary>
    /// <param name="text">Texto a ser normalizado.</param>
    /// <returns>Texto normalizado.</returns>
    private string Normalize(string text)
    {
        return text.Trim().ToLower();
    }
}
