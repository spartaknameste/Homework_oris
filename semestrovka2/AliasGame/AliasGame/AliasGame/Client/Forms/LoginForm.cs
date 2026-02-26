using System.Text.RegularExpressions;
using AliasGame.Client.Network;

namespace AliasGame.Client.Forms;

public partial class LoginForm : Form
{
    private readonly GameClient _client;
    private readonly GameStateManager _stateManager;
    private readonly string _serverHost;
    private readonly int _serverPort;

    
    private TabControl _tabControl = null!;
    private TextBox _loginUsername = null!;
    private TextBox _loginPassword = null!;
    private Button _loginButton = null!;
    private TextBox _regUsername = null!;
    private TextBox _regPassword = null!;
    private TextBox _regEmail = null!;
    private Button _regButton = null!;
    private Label _statusLabel = null!;

    public LoginForm(string serverHost, int serverPort)
    {
        _serverHost = serverHost;
        _serverPort = serverPort;
        _client = new GameClient(serverHost, serverPort);
        _stateManager = new GameStateManager(_client);

        InitializeComponent();
        SetupEvents();
    }

    private void InitializeComponent()
    {
        Text = "Alias - Вход";
        Size = new Size(400, 350);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        _tabControl = new TabControl
        {
            Dock = DockStyle.Top,
            Height = 250
        };

        
        var loginTab = new TabPage("Вход");
        
        var loginUsernameLabel = new Label { Text = "Имя пользователя:", Location = new Point(20, 30), AutoSize = true };
        _loginUsername = new TextBox { Location = new Point(20, 55), Width = 320 };
        
        var loginPasswordLabel = new Label { Text = "Пароль:", Location = new Point(20, 90), AutoSize = true };
        _loginPassword = new TextBox { Location = new Point(20, 115), Width = 320, UseSystemPasswordChar = true };
        
        _loginButton = new Button { Text = "Войти", Location = new Point(20, 160), Width = 320, Height = 35 };
        _loginButton.Click += LoginButton_Click;

        loginTab.Controls.AddRange(new Control[] { loginUsernameLabel, _loginUsername, loginPasswordLabel, _loginPassword, _loginButton });

        
        var regTab = new TabPage("Регистрация");
        
        var regUsernameLabel = new Label { Text = "Имя пользователя:", Location = new Point(20, 20), AutoSize = true };
        _regUsername = new TextBox { Location = new Point(20, 45), Width = 320 };
        
        var regPasswordLabel = new Label { Text = "Пароль:", Location = new Point(20, 75), AutoSize = true };
        _regPassword = new TextBox { Location = new Point(20, 100), Width = 320, UseSystemPasswordChar = true };
        
        var regEmailLabel = new Label { Text = "Email (опционально):", Location = new Point(20, 130), AutoSize = true };
        _regEmail = new TextBox { Location = new Point(20, 155), Width = 320 };
        
        _regButton = new Button { Text = "Зарегистрироваться", Location = new Point(20, 190), Width = 320, Height = 35 };
        _regButton.Click += RegisterButton_Click;

        regTab.Controls.AddRange(new Control[] { regUsernameLabel, _regUsername, regPasswordLabel, _regPassword, regEmailLabel, _regEmail, _regButton });

        _tabControl.TabPages.Add(loginTab);
        _tabControl.TabPages.Add(regTab);

        _statusLabel = new Label
        {
            Location = new Point(20, 270),
            Width = 340,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Red
        };

        Controls.Add(_tabControl);
        Controls.Add(_statusLabel);
    }

    private void SetupEvents()
    {
        _client.Connected += () => Invoke(() =>
        {
            _statusLabel.ForeColor = Color.Green;
            _statusLabel.Text = "Подключено к серверу";
            _stateManager.PerformHandshake();
        });

        _client.Disconnected += (reason) => Invoke(() =>
        {
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Text = $"Отключено: {reason}";
            SetButtonsEnabled(true);
        });

        _client.Error += (ex) => Invoke(() =>
        {
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Text = $"Ошибка: {ex.Message}";
            SetButtonsEnabled(true);
        });

        _stateManager.LoginResult += (success, message) => Invoke(() =>
        {
            if (success)
            {
                var lobbyForm = new LobbyListForm(_client, _stateManager);
                lobbyForm.FormClosed += (s, e) => Show();
                Hide();
                lobbyForm.Show();
            }
            else
            {
                _statusLabel.ForeColor = Color.Red;
                _statusLabel.Text = message;
            }
            SetButtonsEnabled(true);
        });

        _stateManager.RegisterResult += (success, message) => Invoke(() =>
        {
            _statusLabel.ForeColor = success ? Color.Green : Color.Red;
            _statusLabel.Text = message;
            if (success)
            {
                _tabControl.SelectedIndex = 0;
                _loginUsername.Text = _regUsername.Text;
            }
            SetButtonsEnabled(true);
        });

        _stateManager.ErrorReceived += (error) => Invoke(() =>
        {
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Text = error;
            SetButtonsEnabled(true);
        });
    }

    private async void LoginButton_Click(object? sender, EventArgs e)
    {
        if (!ValidateLogin()) return;

        SetButtonsEnabled(false);
        _statusLabel.Text = "Подключение...";
        _statusLabel.ForeColor = Color.Blue;

        try
        {
            if (!_client.IsConnected)
            {
                await _client.ConnectAsync();
                await Task.Delay(500); 
            }
            _stateManager.Login(_loginUsername.Text.Trim(), _loginPassword.Text);
        }
        catch (Exception ex)
        {
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Text = $"Ошибка подключения: {ex.Message}";
            SetButtonsEnabled(true);
        }
    }

    private async void RegisterButton_Click(object? sender, EventArgs e)
    {
        if (!ValidateRegister()) return;

        SetButtonsEnabled(false);
        _statusLabel.Text = "Подключение...";
        _statusLabel.ForeColor = Color.Blue;

        try
        {
            if (!_client.IsConnected)
            {
                await _client.ConnectAsync();
                await Task.Delay(500);
            }
            _stateManager.Register(_regUsername.Text.Trim(), _regPassword.Text, _regEmail.Text.Trim());
        }
        catch (Exception ex)
        {
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Text = $"Ошибка подключения: {ex.Message}";
            SetButtonsEnabled(true);
        }
    }

    private bool ValidateLogin()
    {
        if (string.IsNullOrWhiteSpace(_loginUsername.Text))
        {
            _statusLabel.Text = "Введите имя пользователя";
            _statusLabel.ForeColor = Color.Red;
            return false;
        }
        if (string.IsNullOrWhiteSpace(_loginPassword.Text))
        {
            _statusLabel.Text = "Введите пароль";
            _statusLabel.ForeColor = Color.Red;
            return false;
        }
        return true;
    }

    private bool ValidateRegister()
    {
        var username = _regUsername.Text.Trim();
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
        {
            _statusLabel.Text = "Имя пользователя должно быть не менее 3 символов";
            _statusLabel.ForeColor = Color.Red;
            return false;
        }
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        {
            _statusLabel.Text = "Имя может содержать только буквы, цифры и _";
            _statusLabel.ForeColor = Color.Red;
            return false;
        }
        if (string.IsNullOrWhiteSpace(_regPassword.Text) || _regPassword.Text.Length < 4)
        {
            _statusLabel.Text = "Пароль должен быть не менее 4 символов";
            _statusLabel.ForeColor = Color.Red;
            return false;
        }
        if (!string.IsNullOrWhiteSpace(_regEmail.Text) && !Regex.IsMatch(_regEmail.Text, @"^[\w\.-]+@[\w\.-]+\.\w+$"))
        {
            _statusLabel.Text = "Неверный формат email";
            _statusLabel.ForeColor = Color.Red;
            return false;
        }
        return true;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        _loginButton.Enabled = enabled;
        _regButton.Enabled = enabled;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _client.Dispose();
        base.OnFormClosing(e);
    }
}
