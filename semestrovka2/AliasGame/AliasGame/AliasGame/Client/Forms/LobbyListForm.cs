using AliasGame.Client.Network;
using AliasGame.Shared.Models;

namespace AliasGame.Client.Forms;

public partial class LobbyListForm : Form
{
    private readonly GameClient _client;
    private readonly GameStateManager _stateManager;

    private ListView _lobbyList = null!;
    private Button _refreshButton = null!;
    private Button _createButton = null!;
    private Button _joinButton = null!;
    private Label _statusLabel = null!;

    public LobbyListForm(GameClient client, GameStateManager stateManager)
    {
        _client = client;
        _stateManager = stateManager;

        InitializeComponent();
        SetupEvents();
        
                _stateManager.RequestLobbyList();
    }

    private void InitializeComponent()
    {
        Text = "Alias - Список лобби";
        Size = new Size(600, 450);
        StartPosition = FormStartPosition.CenterScreen;

        var titleLabel = new Label
        {
            Text = "Доступные лобби",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Location = new Point(20, 15),
            AutoSize = true
        };

        _lobbyList = new ListView
        {
            Location = new Point(20, 50),
            Size = new Size(540, 280),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        _lobbyList.Columns.Add("Название", 200);
        _lobbyList.Columns.Add("Игроков", 80);
        _lobbyList.Columns.Add("Хост", 120);
        _lobbyList.Columns.Add("Статус", 80);
        _lobbyList.Columns.Add("Пароль", 50);
        _lobbyList.DoubleClick += LobbyList_DoubleClick;

        _refreshButton = new Button { Text = "🔄 Обновить", Location = new Point(20, 345), Width = 120, Height = 35 };
        _refreshButton.Click += (s, e) => _stateManager.RequestLobbyList();

        _createButton = new Button { Text = "➕ Создать лобби", Location = new Point(160, 345), Width = 150, Height = 35 };
        _createButton.Click += CreateButton_Click;

        _joinButton = new Button { Text = "🚪 Войти", Location = new Point(330, 345), Width = 120, Height = 35 };
        _joinButton.Click += JoinButton_Click;

        _statusLabel = new Label
        {
            Location = new Point(20, 390),
            Width = 540,
            ForeColor = Color.Gray
        };

        Controls.AddRange(new Control[] { titleLabel, _lobbyList, _refreshButton, _createButton, _joinButton, _statusLabel });
    }

    private void SetupEvents()
    {
        _stateManager.LobbyListReceived += lobbies => Invoke(() =>
        {
            _lobbyList.Items.Clear();
            foreach (var lobby in lobbies)
            {
                var item = new ListViewItem(lobby.Name);
                item.SubItems.Add($"{lobby.PlayerCount}/{lobby.MaxPlayers}");
                item.SubItems.Add(lobby.HostName);
                item.SubItems.Add(lobby.State == GameState.Waiting ? "Ожидание" : "В игре");
                item.SubItems.Add(lobby.HasPassword ? "🔒" : "");
                item.Tag = lobby;
                _lobbyList.Items.Add(item);
            }
            _statusLabel.Text = $"Найдено лобби: {lobbies.Count}";
        });

        _stateManager.CreateLobbyResult += (success, message) => Invoke(() =>
        {
            if (success)
            {
                OpenGameLobby();
            }
            else
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });

        _stateManager.JoinLobbyResult += (success, message) => Invoke(() =>
        {
            if (success)
            {
                OpenGameLobby();
            }
            else
            {
                MessageBox.Show(message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        });

        _stateManager.ErrorReceived += error => Invoke(() =>
        {
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Text = error;
        });

        _client.Disconnected += reason => Invoke(() =>
        {
            MessageBox.Show($"Соединение потеряно: {reason}", "Отключено", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
        });
    }

    private void CreateButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new CreateLobbyDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _stateManager.CreateLobby(dialog.LobbyName, dialog.MaxPlayers, dialog.Password);
        }
    }

    private void JoinButton_Click(object? sender, EventArgs e)
    {
        if (_lobbyList.SelectedItems.Count == 0)
        {
            MessageBox.Show("Выберите лобби", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var lobby = (LobbySummary)_lobbyList.SelectedItems[0].Tag;
        
        if (lobby.HasPassword)
        {
            using var passwordDialog = new PasswordDialog();
            if (passwordDialog.ShowDialog() == DialogResult.OK)
            {
                _stateManager.JoinLobby(lobby.Id, passwordDialog.Password);
            }
        }
        else
        {
            _stateManager.JoinLobby(lobby.Id);
        }
    }

    private void LobbyList_DoubleClick(object? sender, EventArgs e)
    {
        JoinButton_Click(sender, e);
    }

    private void OpenGameLobby()
    {
        var gameLobbyForm = new GameLobbyForm(_client, _stateManager);
        gameLobbyForm.FormClosed += (s, e) =>
        {
            Show();
            _stateManager.RequestLobbyList();
        };
        Hide();
        gameLobbyForm.Show();
    }
}

public class CreateLobbyDialog : Form
{
    public string LobbyName { get; private set; } = "";
    public int MaxPlayers { get; private set; } = 10;
    public string Password { get; private set; } = "";

    private TextBox _nameBox = null!;
    private NumericUpDown _maxPlayersBox = null!;
    private TextBox _passwordBox = null!;

    public CreateLobbyDialog()
    {
        Text = "Создать лобби";
        Size = new Size(350, 250);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var nameLabel = new Label { Text = "Название лобби:", Location = new Point(20, 20), AutoSize = true };
        _nameBox = new TextBox { Location = new Point(20, 45), Width = 290 };

        var maxLabel = new Label { Text = "Макс. игроков:", Location = new Point(20, 80), AutoSize = true };
        _maxPlayersBox = new NumericUpDown { Location = new Point(20, 105), Width = 100, Minimum = 2, Maximum = 20, Value = 10 };

        var passLabel = new Label { Text = "Пароль (опционально):", Location = new Point(20, 140), AutoSize = true };
        _passwordBox = new TextBox { Location = new Point(20, 165), Width = 290 };

        var okButton = new Button { Text = "Создать", Location = new Point(130, 200), Width = 80, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Отмена", Location = new Point(220, 200), Width = 80, DialogResult = DialogResult.Cancel };

        okButton.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(_nameBox.Text))
            {
                MessageBox.Show("Введите название лобби", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            LobbyName = _nameBox.Text.Trim();
            MaxPlayers = (int)_maxPlayersBox.Value;
            Password = _passwordBox.Text;
        };

        Controls.AddRange(new Control[] { nameLabel, _nameBox, maxLabel, _maxPlayersBox, passLabel, _passwordBox, okButton, cancelButton });
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}

public class PasswordDialog : Form
{
    public string Password { get; private set; } = "";
    private TextBox _passwordBox = null!;

    public PasswordDialog()
    {
        Text = "Введите пароль";
        Size = new Size(300, 150);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var label = new Label { Text = "Пароль:", Location = new Point(20, 20), AutoSize = true };
        _passwordBox = new TextBox { Location = new Point(20, 45), Width = 240, UseSystemPasswordChar = true };

        var okButton = new Button { Text = "OK", Location = new Point(100, 80), Width = 80, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Отмена", Location = new Point(190, 80), Width = 80, DialogResult = DialogResult.Cancel };

        okButton.Click += (s, e) => Password = _passwordBox.Text;

        Controls.AddRange(new Control[] { label, _passwordBox, okButton, cancelButton });
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
