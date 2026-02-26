using AliasGame.Client.Network;
using AliasGame.Shared.Models;

namespace AliasGame.Client.Forms;

public partial class GameLobbyForm : Form
{
    private readonly GameClient _client;
    private readonly GameStateManager _stateManager;

    private Panel _teamsPanel = null!;
    private Panel _gamePanel = null!;
    private Panel _chatPanel = null!;
    private Button _startButton = null!;
    private Button _settingsButton = null!;
    private Button _leaveButton = null!;
    private Label _timerLabel = null!;
    private Label _wordLabel = null!;
    private Label _roundLabel = null!;
    private Button _guessedButton = null!;
    private Button _skipButton = null!;
    private Button _finishRoundButton = null!;
    private ListBox _guessedWordsList = null!;
    private TextBox _chatInput = null!;
    private ListBox _chatMessages = null!;
    private List<Panel> _teamPanels = new();

    public GameLobbyForm(GameClient client, GameStateManager stateManager)
    {
        _client = client;
        _stateManager = stateManager;
        InitializeComponent();
        SetupEvents();
        UpdateUI();
    }

    private void InitializeComponent()
    {
        Text = "Alias - Игра";
        Size = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterScreen;

                _teamsPanel = new Panel { Location = new Point(10, 10), Size = new Size(300, 480), BorderStyle = BorderStyle.FixedSingle, AutoScroll = true };

                _gamePanel = new Panel { Location = new Point(320, 10), Size = new Size(400, 480), BorderStyle = BorderStyle.FixedSingle };

        _roundLabel = new Label { Text = "Ожидание игроков...", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
        _timerLabel = new Label { Text = "⏰ --:--", Font = new Font("Segoe UI", 24, FontStyle.Bold), Location = new Point(150, 50), AutoSize = true, ForeColor = Color.DarkBlue };
        _wordLabel = new Label { Text = "", Font = new Font("Segoe UI", 20, FontStyle.Bold), Location = new Point(50, 120), Size = new Size(300, 60), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.LightYellow, BorderStyle = BorderStyle.FixedSingle, Visible = false };

        _guessedButton = new Button { Text = "✓ Угадано (+1)", Location = new Point(50, 200), Size = new Size(140, 50), BackColor = Color.LightGreen, Visible = false };
        _guessedButton.Click += (s, e) => _stateManager.NextWord(true);

        _skipButton = new Button { Text = "✗ Пропуск", Location = new Point(210, 200), Size = new Size(140, 50), BackColor = Color.LightCoral, Visible = false };
        _skipButton.Click += (s, e) => _stateManager.NextWord(false);

        _finishRoundButton = new Button { Text = "Завершить раунд", Location = new Point(100, 270), Size = new Size(200, 40), Visible = false };
        _finishRoundButton.Click += (s, e) => _stateManager.FinishRound(false);

        _guessedWordsList = new ListBox { Location = new Point(10, 350), Size = new Size(380, 120) };
        _gamePanel.Controls.AddRange(new Control[] { _roundLabel, _timerLabel, _wordLabel, _guessedButton, _skipButton, _finishRoundButton, new Label { Text = "Угаданные слова:", Location = new Point(10, 330), AutoSize = true }, _guessedWordsList });

                _chatPanel = new Panel { Location = new Point(730, 10), Size = new Size(250, 480), BorderStyle = BorderStyle.FixedSingle };
        _chatMessages = new ListBox { Location = new Point(5, 30), Size = new Size(238, 400) };
        _chatInput = new TextBox { Location = new Point(5, 440), Size = new Size(238, 25) };
        _chatInput.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Enter) { _stateManager.SendChatMessage(_chatInput.Text); _chatInput.Clear(); e.Handled = true; } };
        _chatPanel.Controls.AddRange(new Control[] { new Label { Text = "💬 Чат", Location = new Point(10, 5), AutoSize = true }, _chatMessages, _chatInput });

                _startButton = new Button { Text = "🎮 НАЧАТЬ ИГРУ", Location = new Point(10, 510), Size = new Size(200, 45), BackColor = Color.LightGreen, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        _startButton.Click += (s, e) => _stateManager.StartGame();

        _settingsButton = new Button { Text = "⚙️ Настройки", Location = new Point(220, 510), Size = new Size(120, 45) };
        _settingsButton.Click += SettingsButton_Click;

        _leaveButton = new Button { Text = "🚪 Выйти", Location = new Point(350, 510), Size = new Size(100, 45), BackColor = Color.LightCoral };
        _leaveButton.Click += (s, e) => { _stateManager.LeaveLobby(); Close(); };

        Controls.AddRange(new Control[] { _teamsPanel, _gamePanel, _chatPanel, _startButton, _settingsButton, _leaveButton });
    }

    private void SetupEvents()
    {
        _stateManager.LobbyUpdated += () => Invoke(UpdateUI);
        _stateManager.ChatMessageReceived += (sender, msg) => Invoke(() => _chatMessages.Items.Add($"{sender}: {msg}"));
        _stateManager.GameCountdown += (sec) => Invoke(() => _roundLabel.Text = $"Игра начнётся через {sec}...");
        _stateManager.GameStarted += () => Invoke(() => { _roundLabel.Text = "Игра началась!"; _startButton.Visible = false; _settingsButton.Visible = false; });

        _stateManager.RoundStarted += (round, expl, name) => Invoke(() =>
        {
            _roundLabel.Text = $"Раунд {round} | Объясняет: {name}";
            _guessedWordsList.Items.Clear();
                        _wordLabel.Visible = false;
            _wordLabel.Text = "";
            _guessedButton.Visible = false;
            _skipButton.Visible = false;
            _finishRoundButton.Visible = false;
                        UpdateExplainerUI();
        });

        _stateManager.WordReceived += (word) => Invoke(() => { _wordLabel.Text = word; _wordLabel.Visible = true; });
        _stateManager.WordGuessed += (word, pts) => Invoke(() => _guessedWordsList.Items.Add($"✓ {word} (+{pts})"));
        _stateManager.WordSkipped += (word, pen) => Invoke(() => _guessedWordsList.Items.Add($"✗ {word}" + (pen > 0 ? $" (-{pen})" : "")));
        _stateManager.TimerUpdated += (sec, last) => Invoke(() => { _timerLabel.Text = $"⏰ {sec / 60:D2}:{sec % 60:D2}"; _timerLabel.ForeColor = sec <= 10 ? Color.Red : Color.DarkBlue; });

        _stateManager.LastWordPhaseStarted += () => Invoke(() =>
        {
            _roundLabel.Text += " [ПОСЛЕДНЕЕ СЛОВО]";
            if (_stateManager.IsExplainer) { _guessedButton.Visible = false; _skipButton.Visible = false; _finishRoundButton.Visible = true; }
        });

        _stateManager.RoundEnded += (r, pts) => Invoke(() => { _wordLabel.Text = ""; _wordLabel.Visible = false; _guessedButton.Visible = false; _skipButton.Visible = false; _finishRoundButton.Visible = false; });

        _stateManager.GameEnded += (winner, scores) => Invoke(() =>
        {
            var winnerTeam = _stateManager.CurrentLobby?.Teams.FirstOrDefault(t => t.Id == winner);
            MessageBox.Show($"Победила команда: {winnerTeam?.Name ?? "???"}\nСчёт: {scores.GetValueOrDefault(winner)}", "Игра окончена!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _startButton.Visible = _stateManager.IsHost;
            _settingsButton.Visible = _stateManager.IsHost;
            UpdateUI();
        });

        _stateManager.ErrorReceived += (err) => Invoke(() => MessageBox.Show(err, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning));
        _client.Disconnected += (r) => Invoke(() => { MessageBox.Show($"Соединение потеряно: {r}", "Отключено"); Close(); });
    }

    private void UpdateUI()
    {
        var lobby = _stateManager.CurrentLobby;
        if (lobby == null) return;

        Text = $"Alias - {lobby.Name}";
        _startButton.Visible = _stateManager.IsHost && lobby.State == GameState.Waiting;
        _settingsButton.Visible = _stateManager.IsHost && lobby.State == GameState.Waiting;

                _teamsPanel.Controls.Clear();
        _teamPanels.Clear();
        int y = 10;

        foreach (var team in lobby.Teams)
        {
            var teamPanel = new Panel { Location = new Point(10, y), Size = new Size(270, 80 + team.Players.Count * 25), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.WhiteSmoke };
            var header = new Label { Text = $"{team.Name} (Очки: {team.Score})", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(5, 5), AutoSize = true };
            teamPanel.Controls.Add(header);

            int py = 30;
            foreach (var player in team.Players)
            {
                var icon = player.IsHost ? "⭐" : (player.Id == lobby.CurrentExplainer?.Id ? "🎤" : "👤");
                var playerLabel = new Label { Text = $"{icon} {player.Username}", Location = new Point(10, py), AutoSize = true };
                teamPanel.Controls.Add(playerLabel);
                py += 25;
            }

            var joinBtn = new Button { Text = "Перейти", Location = new Point(180, 5), Size = new Size(80, 25) };
            int teamId = team.Id;
            joinBtn.Click += (s, e) => _stateManager.JoinTeam(teamId);
            if (lobby.State != GameState.Waiting) joinBtn.Enabled = false;
            teamPanel.Controls.Add(joinBtn);

            _teamsPanel.Controls.Add(teamPanel);
            _teamPanels.Add(teamPanel);
            y += teamPanel.Height + 10;
        }
    }

    private void UpdateExplainerUI()
    {
        bool isExplainer = _stateManager.IsExplainer;
        _wordLabel.Visible = isExplainer;
        _guessedButton.Visible = isExplainer && !_stateManager.IsLastWordPhase;
        _skipButton.Visible = isExplainer && !_stateManager.IsLastWordPhase;
        _finishRoundButton.Visible = isExplainer && _stateManager.IsLastWordPhase;
    }

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        var lobby = _stateManager.CurrentLobby;
        if (lobby == null) return;

        using var dialog = new SettingsDialog(lobby.Settings);
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _stateManager.UpdateSettings(dialog.Settings);
        }
    }
}

public class SettingsDialog : Form
{
    public GameSettings Settings { get; private set; }
    private NumericUpDown _roundTime = null!, _totalRounds = null!, _scoreToWin = null!, _lastWordTime = null!, _skipPenalty = null!;
    private CheckBox _allowManualScore = null!, _allowHostPass = null!;

    public SettingsDialog(GameSettings current)
    {
        Settings = current;
        Text = "Настройки игры";
        Size = new Size(350, 350);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        int y = 20;
        AddSetting("Время раунда (сек):", ref _roundTime, current.RoundTimeSeconds, 10, 300, ref y);
        AddSetting("Всего раундов:", ref _totalRounds, current.TotalRounds, 1, 50, ref y);
        AddSetting("Очков до победы:", ref _scoreToWin, current.ScoreToWin, 10, 200, ref y);
        AddSetting("Время посл. слова:", ref _lastWordTime, current.LastWordTimeSeconds, 0, 60, ref y);
        AddSetting("Штраф за пропуск:", ref _skipPenalty, current.SkipPenalty, 0, 5, ref y);

        _allowManualScore = new CheckBox { Text = "Ручное изменение очков", Location = new Point(20, y), AutoSize = true, Checked = current.AllowManualScoreChange };
        Controls.Add(_allowManualScore); y += 30;

        _allowHostPass = new CheckBox { Text = "Хост может передать ход", Location = new Point(20, y), AutoSize = true, Checked = current.AllowHostPassTurn };
        Controls.Add(_allowHostPass); y += 40;

        var okBtn = new Button { Text = "Сохранить", Location = new Point(80, y), Size = new Size(80, 30), DialogResult = DialogResult.OK };
        var cancelBtn = new Button { Text = "Отмена", Location = new Point(170, y), Size = new Size(80, 30), DialogResult = DialogResult.Cancel };
        okBtn.Click += (s, e) => Settings = new GameSettings
        {
            RoundTimeSeconds = (int)_roundTime.Value,
            TotalRounds = (int)_totalRounds.Value,
            ScoreToWin = (int)_scoreToWin.Value,
            LastWordTimeSeconds = (int)_lastWordTime.Value,
            SkipPenalty = (int)_skipPenalty.Value,
            AllowManualScoreChange = _allowManualScore.Checked,
            AllowHostPassTurn = _allowHostPass.Checked
        };
        Controls.AddRange(new Control[] { okBtn, cancelBtn });
    }

    private void AddSetting(string label, ref NumericUpDown control, int value, int min, int max, ref int y)
    {
        Controls.Add(new Label { Text = label, Location = new Point(20, y + 3), AutoSize = true });
        control = new NumericUpDown { Location = new Point(220, y), Width = 80, Minimum = min, Maximum = max, Value = value };
        Controls.Add(control);
        y += 30;
    }
}