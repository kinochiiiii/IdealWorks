using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class IdealWorks : MonoBehaviour
{
    [System.Serializable]
    public class QuestionAndAnswer
    {
        public string category;
        [TextArea(2, 4)]
        public string question;
        public string answer;

        public QuestionAndAnswer(string cat, string q, string a)
        {
            category = cat;
            question = q;
            answer = a;
        }
    }

    [Header("Questions & Answers (Embedded Directly in Code)")]
    [SerializeField]
    private List<QuestionAndAnswer> questionsAndAnswers = new List<QuestionAndAnswer>
    {
        new QuestionAndAnswer("INVENTIONS & LIGHT", "What is the glowing wire thread inside an incandescent lightbulb called?", "FILAMENT"),
        new QuestionAndAnswer("CLASSIC GAMES", "What classic trivia game features a cheeky kangaroo and inspired this lightbulb puzzle?", "HANGAROO"),
        new QuestionAndAnswer("SCIENCE & POWER", "What form of energy flows through wires to illuminate lightbulbs and power circuits?", "ELECTRICITY"),
        new QuestionAndAnswer("POWER GRID", "What term describes a sudden, complete loss of electrical power in a neighborhood or city?", "BLACKOUT"),
        new QuestionAndAnswer("CHEMISTRY", "Which heat-resistant metal element (symbol W) is used to make bulb filaments?", "TUNGSTEN"),
        new QuestionAndAnswer("CREATIVITY", "What creative thinking session is symbolized by a glowing lightbulb over your head?", "BRAINSTORM"),
        new QuestionAndAnswer("PHYSICS", "What measure of electrical potential difference pushes electric current through a wire?", "VOLTAGE"),
        new QuestionAndAnswer("LIGHT & MAGIC", "What word describes shining brightly with quick, glittering flashes of light?", "SPARKLE"),
        new QuestionAndAnswer("INVENTIONS", "What iconic invention turned electrical current into visible light to illuminate homes?", "LIGHTBULB"),
        new QuestionAndAnswer("ENERGY & METRICS", "What measurement indicates the amount of electrical power a lightbulb consumes?", "WATTAGE"),
        new QuestionAndAnswer("LIGHT & OPTICS", "What unit measurement tells you the actual brightness or visible light output of a bulb?", "LUMENS"),
        new QuestionAndAnswer("ELECTRONICS", "What complete, closed loop or pathway must electric current follow to light up a bulb?", "CIRCUIT"),
        new QuestionAndAnswer("POWER & MACHINES", "What machine converts mechanical motion into electricity during a power outage?", "GENERATOR"),
        new QuestionAndAnswer("ENERGY STORAGE", "What portable chemical cell stores electrical energy to power flashlights and gadgets?", "BATTERY"),
        new QuestionAndAnswer("PHYSICS & METALS", "What type of material (like copper or aluminum) allows electric current to flow freely?", "CONDUCTOR")
    };

    [Header("Audio Sound Effects (.wav Clips)")]
    [Tooltip("Drag bulbaroo_correct.wav here (played when a correct letter is guessed).")]
    public AudioClip correctSound;

    [Tooltip("Optional: Sound for Mistake 1 (Electric Zap / Jolt). Uses crack sound if left empty.")]
    public AudioClip mistake1Sound;

    [Tooltip("Drag bulbaroo_mistake2_crack.wav here (played on Mistake 2 glass crack).")]
    public AudioClip mistake2Sound;

    [Tooltip("Drag bulbaroo_mistake3_overheat.wav here (played on Mistake 3 critical overheat).")]
    public AudioClip mistake3Sound;

    [Tooltip("Drag bulbaroo_mistake4_shatter.wav here (played on Mistake 4 glass shatter).")]
    public AudioClip mistake4Sound;

    [Tooltip("Drag bulbaroo_win_fanfare.wav here (played when a puzzle word is completely solved).")]
    public AudioClip winSound;

    private AudioSource audioSource;

    [Header("Lightbulb Sprite Sheets (24x5 Normal & 24x4 Cheer)")]
    [Tooltip("Slice bulbaroo_unity_24x5_spritesheet (24 Columns x 5 Rows = 120 sprites) and drag _0 to _119 here!")]
    public Sprite[] bulbSprites;

    [Tooltip("Slice bulbaroo_cheer_all_states_24x4 (24 Columns x 4 Rows = 96 sprites) and drag _0 to _95 here! Plays whenever a correct letter is clicked.")]
    public Sprite[] cheerSprites;

    [Tooltip("Playback speed. For 24 frames/row, 18 to 24 FPS gives silky-smooth, fluid animation!")]
    [Range(4f, 30f)]
    public float framesPerSecond = 20f;

    [Tooltip("Adds a smooth procedural breathing bob and subtle squash-and-stretch between frame changes.")]
    public bool enableSmoothBreathing = true;

    [Header("Optional Manual UI References (Auto-Built if Left Empty)")]
    public bool autoBuildUI = false;
    public Canvas mainCanvas;
    public Image bulbImage;
    public TextMeshProUGUI speechText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI scoreStatsText;
    public TextMeshProUGUI clueText;
    public TextMeshProUGUI integrityBadgeText;
    public Image integrityBadgeBg;
    public Image[] strikePills = new Image[4];
    public RectTransform wordSlotsContainer;
    public RectTransform keyboardContainer;
    public TextMeshProUGUI hintButtonText;

    private const int MAX_MISTAKES = 4; // 5 rows in bulbSprites (0..4), 4 rows in cheerSprites (0..3)

    private string currentAnswer;
    private string currentQuestion;
    private string currentCategory;
    private HashSet<char> guessedLetters = new HashSet<char>();
    private int mistakes = 0;
    private bool roundOver = false;
    private bool isCheering = false;
    private int wins = 0;
    private int streak = 0;
    private int hintsLeft = 2;
    private float frameTimer = 0f;
    private int currentFrameIndex = 0;
    private int lastQuestionIndex = -1;

    private Vector2 baseBulbPosition;
    private float shakeTimer = 0f;
    private float shakeIntensity = 0f;

    private List<Button> letterButtons = new List<Button>();
    private List<Image> letterButtonBgs = new List<Image>();
    private List<TextMeshProUGUI> letterButtonLabels = new List<TextMeshProUGUI>();

    private readonly string[] QUIPS_IDLE = {
        "\"Look at this glow! Four wrong guesses and I'm toast, pal.\"",
        "\"Bright idea time! Guess a letter—just don't short-circuit me!\"",
        "\"Running at 100% wattage. Keep those good letters coming!\""
    };
    private readonly string[] QUIPS_M1 = {
        "\"BZZZT! Ouch! That wrong letter just surged my bottom coil!\"",
        "\"Hey! Watch the voltage! That's Mistake #1—3 lives left!\""
    };
    private readonly string[] QUIPS_M2 = {
        "\"CRACK! Did you hear my glass?! 2 mistakes—I'm sweating here!\"",
        "\"Yikes! My bulb just cracked and my glow is dropping! Careful!\""
    };
    private readonly string[] QUIPS_M3 = {
        "\"CRITICAL OVERHEAT! 3 Mistakes! One more wrong letter and I SHATTER!\"",
        "\"MAYDAY! Smoke is coming out of my dome! Don't miss the next letter!\""
    };
    private readonly string[] QUIPS_M4 = {
        "\"PZT-CRASH!! 4 Mistakes... My glass shattered and lights out!\"",
        "\"Broken bulb! Hit Next Puzzle to repair my glass.\""
    };
    private readonly string QUIP_WIN = "\"EUREKA! You solved the word and saved my glass! Maximum brightness!\"";

    void Awake()
    {
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        if (autoBuildUI && (mainCanvas == null || wordSlotsContainer == null || keyboardContainer == null))
        {
            BuildCleanGameUI();
        }
    }

    void Start()
    {
        if (bulbImage != null)
        {
            baseBulbPosition = bulbImage.rectTransform.anchoredPosition;
        }
        LoadPuzzle();
    }

    void Update()
    {
        AnimateBulbSprite();
        ApplySmoothBulbMotion();

        // Physical keyboard input (A-Z)
        if (!roundOver)
        {
            try
            {
                for (KeyCode kc = KeyCode.A; kc <= KeyCode.Z; kc++)
                {
                    if (Input.GetKeyDown(kc))
                    {
                        char letter = (char)('A' + (kc - KeyCode.A));
                        HandleGuess(letter);
                    }
                }
            }
            catch (System.InvalidOperationException)
            {
                // Graceful fallback if Unity New Input System is active
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Dynamically detects frames per row for bulbSprites (e.g. 120 / 5 = 24 frames/row).
    /// </summary>
    private int GetFramesPerRow()
    {
        if (bulbSprites == null || bulbSprites.Length == 0) return 1;
        int calculated = bulbSprites.Length / (MAX_MISTAKES + 1);
        return Mathf.Max(1, calculated);
    }

    /// <summary>
    /// Dynamically detects frames per row for cheerSprites (e.g. 96 / 4 = 24 frames/row across states 0..3).
    /// </summary>
    private int GetCheerFramesPerRow()
    {
        if (cheerSprites == null || cheerSprites.Length == 0) return 1;
        int calculated = cheerSprites.Length / MAX_MISTAKES;
        return Mathf.Max(1, calculated);
    }

    private void TriggerCheerAnimation()
    {
        if (cheerSprites != null && cheerSprites.Length > 0)
        {
            isCheering = true;
            currentFrameIndex = 0;
            frameTimer = 0f;
            UpdateBulbSprite();
        }
    }

    private void AnimateBulbSprite()
    {
        if (bulbImage == null) return;

        // 1. If currently playing the Cheer animation (when a correct letter was clicked)
        if (isCheering && cheerSprites != null && cheerSprites.Length > 0)
        {
            int cheerFrames = GetCheerFramesPerRow();
            frameTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, framesPerSecond);

            if (frameTimer >= interval)
            {
                frameTimer = 0f;
                currentFrameIndex++;

                if (currentFrameIndex >= cheerFrames)
                {
                    if (roundOver)
                    {
                        // Keep looping the happy cheer animation on word win until next round loads
                        currentFrameIndex = 0;
                    }
                    else
                    {
                        // Finished playing the 24-frame cheer burst; return to normal state loop
                        isCheering = false;
                        currentFrameIndex = 0;
                    }
                }
                UpdateBulbSprite();
            }
            return;
        }

        // 2. Normal state animation (Rows 0 to 4 in bulbSprites)
        if (bulbSprites == null || bulbSprites.Length == 0) return;

        int framesPerRow = GetFramesPerRow();
        if (framesPerRow > 1)
        {
            frameTimer += Time.deltaTime;
            float interval = 1f / Mathf.Max(1f, framesPerSecond);

            if (frameTimer >= interval)
            {
                frameTimer = 0f;
                // On Mistake 4 (Broken), play through the explosion and hold on the final shattered frame
                if (mistakes >= MAX_MISTAKES)
                {
                    if (currentFrameIndex < framesPerRow - 1)
                    {
                        currentFrameIndex++;
                    }
                }
                else
                {
                    currentFrameIndex = (currentFrameIndex + 1) % framesPerRow;
                }
                UpdateBulbSprite();
            }
        }
    }

    private void ApplySmoothBulbMotion()
    {
        if (bulbImage == null || !enableSmoothBreathing) return;

        float time = Time.time;
        float bobY;
        float scaleX;
        float scaleY;

        if (isCheering)
        {
            // Happy celebratory bounce when a correct letter is clicked
            float cheerWave = Mathf.Abs(Mathf.Sin(time * 12f));
            bobY = cheerWave * 8f;
            scaleY = 1f + Mathf.Sin(time * 16f) * 0.035f;
            scaleX = 1f - Mathf.Sin(time * 16f) * 0.025f;
        }
        else
        {
            // Smooth continuous 60 FPS breathing bob and subtle squash & stretch
            float breath = Mathf.Sin(time * 3.2f);
            bobY = breath * 3.5f;
            scaleY = 1f + breath * 0.02f;
            scaleX = 1f - breath * 0.015f;
        }

        // Dampened reactive shake on wrong guesses
        float shakeOffset = 0f;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float factor = Mathf.Clamp01(shakeTimer / 0.45f);
            shakeOffset = Mathf.Sin(time * 45f) * shakeIntensity * factor;
        }

        bulbImage.rectTransform.anchoredPosition = baseBulbPosition + new Vector2(shakeOffset, bobY);
        bulbImage.rectTransform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private void TriggerBulbShake(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeTimer = duration;
    }

    private void UpdateBulbSprite()
    {
        if (bulbImage == null) return;

        // If cheering after a correct letter guess, display the matching state row from cheerSprites (24x4)
        if (isCheering && cheerSprites != null && cheerSprites.Length > 0)
        {
            int cheerRow = Mathf.Clamp(mistakes, 0, MAX_MISTAKES - 1); // Rows 0..3
            int cheerFrames = GetCheerFramesPerRow();
            int cheerIndex = cheerRow * cheerFrames + Mathf.Clamp(currentFrameIndex, 0, cheerFrames - 1);
            if (cheerIndex < cheerSprites.Length && cheerSprites[cheerIndex] != null)
            {
                bulbImage.sprite = cheerSprites[cheerIndex];
                return;
            }
        }

        if (bulbSprites == null || bulbSprites.Length == 0) return;

        int clampedStage = Mathf.Clamp(mistakes, 0, MAX_MISTAKES);
        int framesPerRow = GetFramesPerRow();

        if (framesPerRow > 1)
        {
            // Calculate index: Row = clampedStage (0..4), Column = currentFrameIndex (0..framesPerRow-1)
            int spriteIndex = clampedStage * framesPerRow + Mathf.Clamp(currentFrameIndex, 0, framesPerRow - 1);
            if (spriteIndex < bulbSprites.Length && bulbSprites[spriteIndex] != null)
            {
                bulbImage.sprite = bulbSprites[spriteIndex];
            }
        }
        else
        {
            int spriteIndex = Mathf.Clamp(clampedStage, 0, bulbSprites.Length - 1);
            bulbImage.sprite = bulbSprites[spriteIndex];
        }
    }

    public void LoadPuzzle()
    {
        if (questionsAndAnswers.Count == 0) return;

        int r = Random.Range(0, questionsAndAnswers.Count);
        if (questionsAndAnswers.Count > 1 && r == lastQuestionIndex)
        {
            r = (r + 1) % questionsAndAnswers.Count;
        }
        lastQuestionIndex = r;

        QuestionAndAnswer entry = questionsAndAnswers[r];
        currentCategory = entry.category.ToUpper();
        currentQuestion = entry.question;
        currentAnswer = entry.answer.Trim().ToUpper();

        guessedLetters.Clear();
        mistakes = 0;
        roundOver = false;
        isCheering = false;
        hintsLeft = 2;
        currentFrameIndex = 0;
        frameTimer = 0f;
        shakeTimer = 0f;

        if (categoryText != null) categoryText.text = "CATEGORY: " + currentCategory;
        if (clueText != null) clueText.text = "Clue: " + currentQuestion;
        if (hintButtonText != null) hintButtonText.text = "💡 Spark Hint (" + hintsLeft + ")";
        if (scoreStatsText != null) scoreStatsText.text = "Wins: " + wins + " | Streak: " + streak;

        SetSpeech(QUIPS_IDLE[Random.Range(0, QUIPS_IDLE.Length)]);
        UpdateStatusUI();
        UpdateBulbSprite();
        RenderWordSlots();
        RenderKeyboard();
    }

    public void HandleGuess(char letter)
    {
        letter = char.ToUpperInvariant(letter);
        if (roundOver || guessedLetters.Contains(letter)) return;
        guessedLetters.Add(letter);

        if (currentAnswer.IndexOf(letter) >= 0)
        {
            bool allSolved = true;
            foreach (char ch in currentAnswer)
            {
                if (!guessedLetters.Contains(ch)) { allSolved = false; break; }
            }

            // Show the 24x4 cheer animation matching the current mistake state!
            TriggerCheerAnimation();

            if (allSolved)
            {
                roundOver = true;
                wins++;
                streak++;
                PlaySound(winSound);
                SetSpeech(QUIP_WIN);
                if (clueText != null) clueText.text = "🌟 CORRECT! The answer is \"" + currentAnswer + "\"!";
                StartCoroutine(NextRoundDelay(3f));
            }
            else
            {
                PlaySound(correctSound);
                SetSpeech("\"BINGO! That letter lit me right up!\"");
            }
        }
        else
        {
            isCheering = false;
            mistakes = Mathf.Min(MAX_MISTAKES, mistakes + 1);
            currentFrameIndex = 0;
            frameTimer = 0f;
            TriggerBulbShake(6f + mistakes * 2f, 0.45f);
            UpdateBulbSprite();

            if (mistakes == 1)
            {
                PlaySound(mistake1Sound != null ? mistake1Sound : mistake2Sound);
                SetSpeech(QUIPS_M1[Random.Range(0, QUIPS_M1.Length)]);
            }
            else if (mistakes == 2)
            {
                PlaySound(mistake2Sound);
                SetSpeech(QUIPS_M2[Random.Range(0, QUIPS_M2.Length)]);
            }
            else if (mistakes == 3)
            {
                PlaySound(mistake3Sound);
                SetSpeech(QUIPS_M3[Random.Range(0, QUIPS_M3.Length)]);
            }
            else if (mistakes >= 4)
            {
                roundOver = true;
                streak = 0;
                PlaySound(mistake4Sound);
                SetSpeech(QUIPS_M4[Random.Range(0, QUIPS_M4.Length)]);
                if (clueText != null) clueText.text = "💥 OUT OF LIVES! The answer was \"" + currentAnswer + "\"!";
                StartCoroutine(NextRoundDelay(3f));
            }
        }

        UpdateStatusUI();
        RenderWordSlots();
        RenderKeyboard();
    }

    public void UseSparkHint()
    {
        if (roundOver || hintsLeft <= 0) return;
        List<char> unguessed = new List<char>();
        foreach (char ch in currentAnswer)
        {
            if (!guessedLetters.Contains(ch) && !unguessed.Contains(ch))
            {
                unguessed.Add(ch);
            }
        }

        if (unguessed.Count > 0)
        {
            hintsLeft--;
            if (hintButtonText != null) hintButtonText.text = "💡 Spark Hint (" + hintsLeft + ")";
            char pick = unguessed[Random.Range(0, unguessed.Count)];
            HandleGuess(pick);
        }
    }

    private IEnumerator NextRoundDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadPuzzle();
    }

    private void SetSpeech(string text)
    {
        if (speechText != null) speechText.text = text;
    }

    private void UpdateStatusUI()
    {
        int livesLeft = Mathf.Max(0, MAX_MISTAKES - mistakes);

        if (integrityBadgeText != null)
        {
            if (mistakes == 0) integrityBadgeText.text = "0 / 4 Mistakes (100% Lit)";
            else if (mistakes == 1) integrityBadgeText.text = "1 / 4 Mistakes (75% Power)";
            else if (mistakes == 2) integrityBadgeText.text = "2 / 4 Mistakes (50% Power)";
            else if (mistakes == 3) integrityBadgeText.text = "3 / 4 Mistakes (DANGER - 1 Left!)";
            else integrityBadgeText.text = "4 / 4 Mistakes (BROKEN!)";
        }

        if (integrityBadgeBg != null)
        {
            if (mistakes == 0) integrityBadgeBg.color = new Color(0.06f, 0.72f, 0.50f, 0.25f);
            else if (mistakes == 1) integrityBadgeBg.color = new Color(0.92f, 0.70f, 0.03f, 0.25f);
            else if (mistakes == 2) integrityBadgeBg.color = new Color(0.98f, 0.45f, 0.09f, 0.25f);
            else integrityBadgeBg.color = new Color(0.95f, 0.25f, 0.37f, 0.25f);
        }

        for (int i = 0; i < strikePills.Length; i++)
        {
            if (strikePills[i] != null)
            {
                strikePills[i].color = (i < livesLeft)
                    ? HexColor("#facc15") // bright yellow life pill
                    : new Color(0.95f, 0.25f, 0.37f, 0.35f); // drained red/gray
            }
        }

        if (scoreStatsText != null) scoreStatsText.text = "Wins: " + wins + " | Streak: " + streak;
    }

    private void RenderWordSlots()
    {
        if (wordSlotsContainer == null) return;
        foreach (Transform child in wordSlotsContainer) Destroy(child.gameObject);

        for (int i = 0; i < currentAnswer.Length; i++)
        {
            char ch = currentAnswer[i];
            bool revealed = guessedLetters.Contains(ch) || roundOver;
            bool wasGuessed = guessedLetters.Contains(ch);

            GameObject slot = new GameObject("Slot_" + i);
            slot.transform.SetParent(wordSlotsContainer, false);
            RectTransform rt = slot.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(44f, 50f);

            Image bg = slot.AddComponent<Image>();
            bg.color = revealed
                ? (wasGuessed ? HexColor("#facc15") : new Color(0.95f, 0.25f, 0.37f, 0.35f))
                : HexColor("#0f172a");

            Outline ol = slot.AddComponent<Outline>();
            ol.effectColor = revealed
                ? (wasGuessed ? HexColor("#fde047") : HexColor("#f43f5e"))
                : HexColor("#334155");
            ol.effectDistance = new Vector2(2f, -2f);

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(slot.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI label = txtObj.AddComponent<TextMeshProUGUI>();
            label.text = revealed ? ch.ToString() : "";
            label.fontSize = 22;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = (revealed && wasGuessed) ? HexColor("#090d16") : Color.white;
        }
    }

    private void EnsureKeyboardButtons()
    {
        if (keyboardContainer == null || letterButtons.Count == 26) return;

        // Clear existing children if any
        foreach (Transform child in keyboardContainer)
        {
            Destroy(child.gameObject);
        }

        letterButtons.Clear();
        letterButtonBgs.Clear();
        letterButtonLabels.Clear();

        for (int i = 0; i < 26; i++)
        {
            char letter = (char)('A' + i);
            GameObject keyObj = new GameObject("Key_" + letter);
            keyObj.transform.SetParent(keyboardContainer, false);
            RectTransform keyRect = keyObj.AddComponent<RectTransform>();
            keyRect.sizeDelta = new Vector2(58, 48);

            Image keyBg = keyObj.AddComponent<Image>();
            keyBg.color = HexColor("#1e293b");
            AddOutline(keyObj, HexColor("#334155"), 1.5f);

            Button btn = keyObj.AddComponent<Button>();
            char captured = letter;
            btn.onClick.AddListener(() => HandleGuess(captured));

            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(keyObj.transform, false);
            RectTransform lblRt = lblObj.AddComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI lbl = lblObj.AddComponent<TextMeshProUGUI>();
            lbl.text = letter.ToString();
            lbl.fontSize = 16;
            lbl.fontStyle = FontStyles.Bold;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color = HexColor("#f8fafc");

            letterButtons.Add(btn);
            letterButtonBgs.Add(keyBg);
            letterButtonLabels.Add(lbl);
        }
    }

    private void RenderKeyboard()
    {
        EnsureKeyboardButtons();
        if (letterButtons.Count != 26) return;

        for (int i = 0; i < 26; i++)
        {
            char letter = (char)('A' + i);
            bool used = guessedLetters.Contains(letter);
            bool inWord = currentAnswer.IndexOf(letter) >= 0;

            letterButtons[i].interactable = (!used && !roundOver);
            if (!used)
            {
                letterButtonBgs[i].color = HexColor("#1e293b");
                letterButtonLabels[i].color = HexColor("#f8fafc");
            }
            else if (inWord)
            {
                letterButtonBgs[i].color = new Color(0.06f, 0.72f, 0.50f, 0.35f);
                letterButtonLabels[i].color = HexColor("#6ee7b7");
            }
            else
            {
                letterButtonBgs[i].color = new Color(0.95f, 0.25f, 0.37f, 0.25f);
                letterButtonLabels[i].color = HexColor("#fda4af");
            }
        }
    }

    // ==========================================
    // CLEAN SCREENSHOT-MATCHING UI BUILDER
    // ==========================================
    private void BuildCleanGameUI()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject cObj = new GameObject("IdealWorks_Canvas");
        mainCanvas = cObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        cObj.AddComponent<GraphicRaycaster>();

        // Root Background
        RectTransform root = CreateRect("RootBg", cObj.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.gameObject.AddComponent<Image>().color = HexColor("#0f172a");

        // ------------------------------------------
        // LEFT CARD: BULB STAGE & INTEGRITY
        // ------------------------------------------
        RectTransform leftCard = CreateRect("LeftCard", root, new Vector2(0.04f, 0.05f), new Vector2(0.44f, 0.95f), Vector2.zero, Vector2.zero);
        leftCard.gameObject.AddComponent<Image>().color = HexColor("#1e293b");
        AddOutline(leftCard.gameObject, HexColor("#334155"), 2f);

        // Top Integrity Ribbon
        RectTransform ribbon = CreateRect("Ribbon", leftCard, new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        CreateTMP("Lbl", ribbon, "BULB INTEGRITY:", 12, FontStyles.Bold, HexColor("#94a3b8"), TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(0.35f, 1));

        RectTransform badge = CreateRect("Badge", ribbon, new Vector2(0.32f, 0.1f), new Vector2(0.70f, 0.9f), Vector2.zero, Vector2.zero);
        integrityBadgeBg = badge.gameObject.AddComponent<Image>();
        integrityBadgeBg.color = new Color(0.06f, 0.72f, 0.50f, 0.25f);
        integrityBadgeText = CreateTMP("BadgeTxt", badge, "0 / 4 Mistakes (100% Lit)", 11, FontStyles.Bold, HexColor("#6ee7b7"), TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

        for (int i = 0; i < 4; i++)
        {
            RectTransform pill = CreateRect("Pill_" + i, ribbon, new Vector2(0.73f + i * 0.068f, 0.35f), new Vector2(0.78f + i * 0.068f, 0.65f), Vector2.zero, Vector2.zero);
            strikePills[i] = pill.gameObject.AddComponent<Image>();
            strikePills[i].color = HexColor("#facc15");
        }

        // White Lightbulb Stage Box
        RectTransform stageBox = CreateRect("StageBox", leftCard, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.88f), Vector2.zero, Vector2.zero);
        stageBox.gameObject.AddComponent<Image>().color = Color.white;
        AddOutline(stageBox.gameObject, HexColor("#281a14"), 2.5f);

        // Speech Bubble
        RectTransform bubble = CreateRect("SpeechBubble", stageBox, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
        bubble.gameObject.AddComponent<Image>().color = Color.white;
        AddOutline(bubble.gameObject, HexColor("#281a14"), 2.5f);
        speechText = CreateTMP("SpeechTxt", bubble, QUIPS_IDLE[0], 13, FontStyles.Bold, HexColor("#1e293b"), TextAlignmentOptions.Center, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));

        // Lightbulb Image on Stage
        RectTransform bulbObj = CreateRect("BulbImage", stageBox, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(-128, -160), new Vector2(128, 160));
        bulbImage = bulbObj.gameObject.AddComponent<Image>();
        bulbImage.preserveAspect = true;
        if (bulbSprites != null && bulbSprites.Length > 0 && bulbSprites[0] != null)
        {
            bulbImage.sprite = bulbSprites[0];
        }

        // ------------------------------------------
        // RIGHT CARD: GAMEPLAY (QUESTIONS, WORD SLOTS, KEYBOARD)
        // ------------------------------------------
        RectTransform rightCard = CreateRect("RightCard", root, new Vector2(0.46f, 0.05f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);
        rightCard.gameObject.AddComponent<Image>().color = HexColor("#1e293b");
        AddOutline(rightCard.gameObject, HexColor("#334155"), 2f);

        // Header: Category, Stats, Spark Hint & Next Puzzle Buttons
        RectTransform catObj = CreateRect("CatPill", rightCard, new Vector2(0.03f, 0.89f), new Vector2(0.40f, 0.96f), Vector2.zero, Vector2.zero);
        catObj.gameObject.AddComponent<Image>().color = HexColor("#3730a3");
        categoryText = CreateTMP("CatTxt", catObj, "CATEGORY: INVENTIONS & LIGHT", 11, FontStyles.Bold, HexColor("#c7d2fe"), TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

        scoreStatsText = CreateTMP("StatsTxt", rightCard, "Wins: 0 | Streak: 0", 12, FontStyles.Bold, HexColor("#94a3b8"), TextAlignmentOptions.Left, new Vector2(0.42f, 0.89f), new Vector2(0.60f, 0.96f));

        // Spark Hint Button
        RectTransform hintBtn = CreateRect("HintBtn", rightCard, new Vector2(0.62f, 0.89f), new Vector2(0.79f, 0.96f), Vector2.zero, Vector2.zero);
        hintBtn.gameObject.AddComponent<Image>().color = HexColor("#78350f");
        AddOutline(hintBtn.gameObject, HexColor("#d97706"), 1.5f);
        Button hBtn = hintBtn.gameObject.AddComponent<Button>();
        hBtn.onClick.AddListener(UseSparkHint);
        hintButtonText = CreateTMP("HintTxt", hintBtn, "💡 Spark Hint (2)", 11, FontStyles.Bold, HexColor("#fde047"), TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

        // Next Puzzle Button
        RectTransform nextBtn = CreateRect("NextBtn", rightCard, new Vector2(0.81f, 0.89f), new Vector2(0.97f, 0.96f), Vector2.zero, Vector2.zero);
        nextBtn.gameObject.AddComponent<Image>().color = HexColor("#334155");
        AddOutline(nextBtn.gameObject, HexColor("#475569"), 1.5f);
        Button nBtn = nextBtn.gameObject.AddComponent<Button>();
        nBtn.onClick.AddListener(LoadPuzzle);
        CreateTMP("NextTxt", nextBtn, "↻ Next Puzzle", 11, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

        // Clue / Question Text
        clueText = CreateTMP("ClueTxt", rightCard, "Clue: What is the glowing wire thread inside an incandescent lightbulb called?", 13, FontStyles.Normal, HexColor("#cbd5e1"), TextAlignmentOptions.Left, new Vector2(0.03f, 0.80f), new Vector2(0.97f, 0.88f));

        // Word Slots Container
        wordSlotsContainer = CreateRect("WordSlots", rightCard, new Vector2(0.03f, 0.58f), new Vector2(0.97f, 0.76f), Vector2.zero, Vector2.zero);
        wordSlotsContainer.gameObject.AddComponent<Image>().color = HexColor("#0f172a");
        AddOutline(wordSlotsContainer.gameObject, HexColor("#334155"), 1.5f);
        HorizontalLayoutGroup hlg = wordSlotsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 6f;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // On-Screen A-Z Keyboard Container
        keyboardContainer = CreateRect("KeyboardContainer", rightCard, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.52f), Vector2.zero, Vector2.zero);
        GridLayoutGroup glg = keyboardContainer.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(58, 48);
        glg.spacing = new Vector2(8, 8);
        glg.childAlignment = TextAnchor.MiddleCenter;

        EnsureKeyboardButtons();
    }

    private RectTransform CreateRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
        return rt;
    }

    private TextMeshProUGUI CreateTMP(string name, Transform parent, string content, float size, FontStyles style, Color col, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax)
    {
        RectTransform rt = CreateRect(name, parent, aMin, aMax, Vector2.zero, Vector2.zero);
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = col;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private void AddOutline(GameObject go, Color col, float dist)
    {
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = col;
        ol.effectDistance = new Vector2(dist, -dist);
    }

    private Color HexColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color c)) return c;
        return Color.white;
    }
}

// Compatibility alias in case a script file is named BulbArooGame.cs
public class BulbArooGame : IdealWorks { }