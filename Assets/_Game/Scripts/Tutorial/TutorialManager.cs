using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrates the 4-phase tutorial sequence. Placed in the Tutorial scene.
/// Phase 1: Movement (left, right, jump hold/tap).
/// Phase 2: Crate pickup -> Hammer + Screwdriver milestone.
/// Phase 3: Kill one Walker enemy.
/// Phase 4: Jump into the hole (game over).
///
/// Text placeholders (replaced at runtime):
///   {MOVE_LEFT}  – left movement key display name
///   {MOVE_RIGHT} – right movement key display name
///   {JUMP}       – jump key display name
///   {ATTACK}     – attack key display name
///   {FIRST_TOOL} – name of the guaranteed first-pickup tool (from GameManager)
/// </summary>
public class TutorialManager : MonoBehaviour
{
    private enum TutorialPhase { Movement, Crate, Enemy, Finish }

    [Header("Player")]
    [SerializeField] private PlayerController _player;
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("UI")]
    [SerializeField] private TutorialUI _tutorialUI;

    [Header("Crate (Phase 2)")]
    [SerializeField] private Toolbox _toolboxPrefab;
    [SerializeField] private Transform _crateSpawnPoint;
    [Tooltip("Highest tool pickup-threshold the tutorial is allowed to unlock. Tools above this are blocked.")]
    [SerializeField] private ToolData _maxUnlockTool;

    [Header("Enemy (Phase 3)")]
    [SerializeField] private GameObject _walkerEnemyPrefab;
    [SerializeField] private Transform _enemySpawnPoint;

    [Header("Map Spawn Config (synced with EnemySpawner)")]
    [Tooltip("Uses the same MapSpawnConfig SO assigned to EnemySpawner so angry-respawn positions stay consistent.")]
    [SerializeField] private MapSpawnConfig _mapSpawnConfig;

    [Header("Event Channels")]
    [SerializeField] private IntEventChannel _onToolPickedUp;
    [SerializeField] private VoidEventChannel _onEnemyKilled;
    [SerializeField] private EnemyFallChannel _onEnemyFell;
    [SerializeField] private VoidEventChannel _onGameOver;

    [Header("Timing")]
    [SerializeField] private float _moveDetectDuration = 0.3f;
    [SerializeField] private float _jumpHoldThreshold = 0.15f;
    [SerializeField] private float _phaseTransitionDelay = 1.5f;

    [Header("Phase 1 — Movement Text")]
    [Tooltip("{MOVE_LEFT} = left key")]
    [SerializeField] private string _moveLeftText = "Move LEFT [{MOVE_LEFT}].";
    [Tooltip("{MOVE_RIGHT} = right key")]
    [SerializeField] private string _moveRightText = "Move RIGHT [{MOVE_RIGHT}].";
    [Tooltip("{JUMP} = jump key")]
    [SerializeField] private string _jumpText = "JUMP! [{JUMP}]";
    [Tooltip("{JUMP} = jump key")]
    [SerializeField] private string _holdFirstText = "Great hold! Now try TAPPING [{JUMP}] for a short hop.";
    [Tooltip("{JUMP} = jump key")]
    [SerializeField] private string _tapFirstText = "Quick tap! Now try HOLDING [{JUMP}] for a higher jump.";
    [Tooltip("{JUMP} = jump key")]
    [SerializeField] private string _holdHintText = "Keep HOLDING [{JUMP}] longer!";
    [Tooltip("{JUMP} = jump key")]
    [SerializeField] private string _tapHintText = "Just TAP [{JUMP}] quickly!";
    [SerializeField] private string _movementCompleteText = "Nice! You've mastered the basics.";

    [Header("Phase 2 — Crate Text")]
    [SerializeField] private string _pickUpCrateText = "See that crate? Pick it up!";
    [Tooltip("{FIRST_TOOL} = first tool name, {ATTACK} = attack key")]
    [SerializeField] private string _cratePickedUpText =
        "You got a {FIRST_TOOL}! Press [{ATTACK}] to use it. " +
        "There are many tools that can be unlocked later in the run. " +
        "These tools can deal damage to enemies.";

    [Header("Phase 3 — Enemy Text")]
    [Tooltip("{ATTACK} = attack key")]
    [SerializeField] private string _killEnemyText =
        "Kill the enemy with your tool [{ATTACK}]. " +
        "Don't let them fall into the hole, or they will get angry!";
    [SerializeField] private string _enemyDefeatedText = "Enemy defeated!";

    [Header("Phase 4 — Finish Text")]
    [SerializeField] private string _jumpIntoHoleText = "Great job! Now jump into the hole.";

    private TutorialPhase _currentPhase;
    private bool _cratePickedUp;
    private bool _enemyKilled;

    private EnemySpawner _spawner;
    private float _spawnXLeft;
    private float _spawnXRight;
    private float _spawnYOffset;
    private Coroutine _tutorialCoroutine;

    private void Start()
    {
        ApplyTutorialUnlockPickupCap();
        CacheSpawnerConfig();
        DisableEnemySpawner();
        DestroyAllToolboxes();

        _tutorialCoroutine = StartCoroutine(RunTutorial());
    }

    private void ApplyTutorialUnlockPickupCap()
    {
        if (_maxUnlockTool != null)
            GameManager.TutorialUnlockPickupCap = _maxUnlockTool.unlockPickupCount;
    }

    private void OnEnable()
    {
        if (_onToolPickedUp != null) _onToolPickedUp.Register(OnCratePickedUp);
        if (_onEnemyKilled != null) _onEnemyKilled.Register(OnEnemyKilled);
        if (_onEnemyFell != null) _onEnemyFell.Register(HandleEnemyFell);
        if (_onGameOver != null) _onGameOver.Register(OnGameOver);
    }

    private void OnDisable()
    {
        if (_onToolPickedUp != null) _onToolPickedUp.Unregister(OnCratePickedUp);
        if (_onEnemyKilled != null) _onEnemyKilled.Unregister(OnEnemyKilled);
        if (_onEnemyFell != null) _onEnemyFell.Unregister(HandleEnemyFell);
        if (_onGameOver != null) _onGameOver.Unregister(OnGameOver);
    }

    private void OnGameOver()
    {
        if (_tutorialCoroutine != null)
        {
            StopCoroutine(_tutorialCoroutine);
            _tutorialCoroutine = null;
        }

        if (_tutorialUI != null)
            _tutorialUI.HideMessage();
    }

    // ─────────────────────── Initialization ───────────────────────

    private void CacheSpawnerConfig()
    {
        _spawner = Object.FindFirstObjectByType<EnemySpawner>();

        if (_mapSpawnConfig != null)
        {
            _spawnXLeft = _mapSpawnConfig.spawnXLeft;
            _spawnXRight = _mapSpawnConfig.spawnXRight;
            _spawnYOffset = _mapSpawnConfig.spawnYOffset;
        }
        else
        {
            _spawnXLeft = -4f;
            _spawnXRight = 4f;
            _spawnYOffset = 1f;
        }
    }

    private void DisableEnemySpawner()
    {
        if (_spawner != null)
            _spawner.enabled = false;
    }

    private void DestroyAllToolboxes()
    {
        var boxes = Object.FindObjectsByType<Toolbox>(FindObjectsSortMode.None);
        foreach (var box in boxes)
            Destroy(box.gameObject);
    }

    // ─────────────────────── Text Formatting ───────────────────────

    /// Replaces placeholders with live keybind names and game data.
    private string Fmt(string template)
    {
        if (string.IsNullOrEmpty(template)) return template;

        string moveLeft = _inputHandler != null ? _inputHandler.GetKeyDisplayName("Move", "left") : "?";
        string moveRight = _inputHandler != null ? _inputHandler.GetKeyDisplayName("Move", "right") : "?";
        string jump = _inputHandler != null ? _inputHandler.GetKeyDisplayName("Jump") : "?";
        string attack = _inputHandler != null ? _inputHandler.GetKeyDisplayName("Attack") : "?";

        string firstTool = "Hammer";
        if (GameManager.Instance != null && GameManager.Instance.FirstPickupTool != null)
            firstTool = GameManager.Instance.FirstPickupTool.toolName;

        return template
            .Replace("{MOVE_LEFT}", moveLeft)
            .Replace("{MOVE_RIGHT}", moveRight)
            .Replace("{JUMP}", jump)
            .Replace("{ATTACK}", attack)
            .Replace("{FIRST_TOOL}", firstTool);
    }

    private void ShowText(string template)
    {
        if (_tutorialUI != null)
            _tutorialUI.ShowMessage(Fmt(template));
    }

    // ─────────────────────── Main Tutorial Coroutine ───────────────────────

    private IEnumerator RunTutorial()
    {
        yield return new WaitForSeconds(0.5f);

        yield return RunPhase1_Movement();
        yield return new WaitForSeconds(_phaseTransitionDelay);
        yield return RunPhase2_Crate();
        yield return new WaitForSeconds(_phaseTransitionDelay);
        yield return RunPhase3_Enemy();
        yield return new WaitForSeconds(_phaseTransitionDelay);
        yield return RunPhase4_Finish();
    }

    // ─────────────────────── Phase 1: Movement ───────────────────────

    private IEnumerator RunPhase1_Movement()
    {
        _currentPhase = TutorialPhase.Movement;

        ShowText(_moveLeftText);
        yield return WaitForMoveInput(-1f);

        ShowText(_moveRightText);
        yield return WaitForMoveInput(1f);

        ShowText(_jumpText);
        bool firstJumpWasHold = false;
        yield return WaitForJump(held => firstJumpWasHold = held);

        if (firstJumpWasHold)
        {
            ShowText(_holdFirstText);
            yield return WaitForJumpOfType(isHold: false);
        }
        else
        {
            ShowText(_tapFirstText);
            yield return WaitForJumpOfType(isHold: true);
        }

        ShowText(_movementCompleteText);
        yield return new WaitForSeconds(1.5f);
        if (_tutorialUI != null) _tutorialUI.HideMessage();
    }

    private IEnumerator WaitForMoveInput(float direction)
    {
        float accumulated = 0f;
        while (accumulated < _moveDetectDuration)
        {
            if (_inputHandler == null) { yield return null; continue; }

            bool match = direction < 0f
                ? _inputHandler.MoveInput < -0.1f
                : _inputHandler.MoveInput > 0.1f;

            accumulated = match ? accumulated + Time.deltaTime : 0f;
            yield return null;
        }
    }

    private IEnumerator WaitForJump(System.Action<bool> onComplete)
    {
        while (_inputHandler == null || !_inputHandler.JumpPressed)
            yield return null;

        float holdStart = Time.time;

        while (_inputHandler != null && !_inputHandler.JumpReleased)
            yield return null;

        float holdDuration = Time.time - holdStart;
        bool wasHold = holdDuration > _jumpHoldThreshold;

        yield return WaitUntilGrounded();

        onComplete?.Invoke(wasHold);
    }

    private IEnumerator WaitForJumpOfType(bool isHold)
    {
        while (true)
        {
            while (_inputHandler == null || !_inputHandler.JumpPressed)
                yield return null;

            float holdStart = Time.time;

            while (_inputHandler != null && !_inputHandler.JumpReleased)
                yield return null;

            float holdDuration = Time.time - holdStart;
            bool wasHold = holdDuration > _jumpHoldThreshold;

            yield return WaitUntilGrounded();

            if (wasHold == isHold)
                yield break;

            string hint = isHold ? _holdHintText : _tapHintText;
            ShowText(hint);
        }
    }

    private IEnumerator WaitUntilGrounded()
    {
        yield return new WaitForSeconds(0.1f);

        var rb = _player != null ? _player.GetComponent<Rigidbody2D>() : null;
        if (rb == null) yield break;

        while (rb.linearVelocity.y > 0.01f || !IsPlayerGrounded())
            yield return null;
    }

    private bool IsPlayerGrounded()
    {
        if (_player == null) return false;
        var animator = _player.GetComponent<Animator>();
        if (animator != null)
            return animator.GetBool("IsGrounded");
        return false;
    }

    // ─────────────────────── Phase 2: Crate ───────────────────────

    private IEnumerator RunPhase2_Crate()
    {
        _currentPhase = TutorialPhase.Crate;
        _cratePickedUp = false;

        ShowText(_pickUpCrateText);

        if (_toolboxPrefab != null && _crateSpawnPoint != null)
            Instantiate(_toolboxPrefab, _crateSpawnPoint.position, Quaternion.identity);

        while (!_cratePickedUp)
            yield return null;

        DestroyAllToolboxes();
        ShowText(_cratePickedUpText);
        yield return new WaitForSeconds(4f);
        if (_tutorialUI != null) _tutorialUI.HideMessage();
    }

    private void OnCratePickedUp(int _)
    {
        if (_currentPhase == TutorialPhase.Crate)
            _cratePickedUp = true;
    }

    // ─────────────────────── Phase 3: Enemy ───────────────────────

    private IEnumerator RunPhase3_Enemy()
    {
        _currentPhase = TutorialPhase.Enemy;
        _enemyKilled = false;

        ShowText(_killEnemyText);

        SpawnWalkerEnemy();

        while (!_enemyKilled)
            yield return null;

        ShowText(_enemyDefeatedText);
        yield return new WaitForSeconds(1.5f);
        if (_tutorialUI != null) _tutorialUI.HideMessage();
    }

    private void SpawnWalkerEnemy()
    {
        if (_walkerEnemyPrefab == null || _enemySpawnPoint == null) return;
        Instantiate(_walkerEnemyPrefab, _enemySpawnPoint.position, Quaternion.identity);
    }

    private void OnEnemyKilled()
    {
        if (_currentPhase == TutorialPhase.Enemy)
            _enemyKilled = true;
    }

    /// Handles the angry-respawn mechanic when EnemySpawner is disabled.
    /// Positions and shake values are read from MapSpawnConfig / EnemySpawner
    /// so they stay in sync with the real game.
    private void HandleEnemyFell(EnemyFallData data)
    {
        if (_currentPhase != TutorialPhase.Enemy) return;
        Camera cam = Camera.main;
        if (data.enemyData == null || _walkerEnemyPrefab == null || cam == null) return;

        float x = Random.value < 0.5f ? _spawnXLeft : _spawnXRight;
        float y = cam.transform.position.y + cam.orthographicSize + _spawnYOffset;

        GameObject newEnemy = Instantiate(_walkerEnemyPrefab, new Vector2(x, y), Quaternion.identity);
        BaseEnemy baseEnemy = newEnemy.GetComponent<BaseEnemy>();

        if (baseEnemy != null)
        {
            baseEnemy.MarkAsRespawned();

            if (CameraShake.Instance != null)
            {
                float dur = _spawner != null ? _spawner.AngryDropShakeDuration : 0.15f;
                float mag = _spawner != null ? _spawner.AngryDropShakeMagnitude : 0.1f;
                CameraShake.Instance.Shake(dur, mag);
            }
        }
    }

    // ─────────────────────── Phase 4: Finish ───────────────────────

    private IEnumerator RunPhase4_Finish()
    {
        _currentPhase = TutorialPhase.Finish;

        ShowText(_jumpIntoHoleText);

        yield break;
    }
}
