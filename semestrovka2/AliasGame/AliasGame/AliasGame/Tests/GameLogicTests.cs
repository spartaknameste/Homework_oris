using AliasGame.Shared.Models;
using FluentAssertions;
using Xunit;

namespace AliasGame.Tests;

public class GameModelsTests
{
    [Fact]
    public void Lobby_ShouldInitializeWithDefaults()
    {
                var lobby = new Lobby();

                lobby.State.Should().Be(GameState.Waiting);
        lobby.Players.Should().BeEmpty();
        lobby.Teams.Should().BeEmpty();
        lobby.CurrentRound.Should().Be(0);
        lobby.Settings.Should().NotBeNull();
    }

    [Fact]
    public void Lobby_GetPlayer_ShouldFindPlayerById()
    {
                var lobby = new Lobby();
        var player = new Player { Id = 5, Username = "TestPlayer" };
        lobby.Players.Add(player);

                var found = lobby.GetPlayer(5);

                found.Should().NotBeNull();
        found!.Username.Should().Be("TestPlayer");
    }

    [Fact]
    public void Lobby_GetTeam_ShouldFindTeamById()
    {
                var lobby = new Lobby();
        var team = new Team { Id = 3, Name = "Team3" };
        lobby.Teams.Add(team);

                var found = lobby.GetTeam(3);

                found.Should().NotBeNull();
        found!.Name.Should().Be("Team3");
    }

    [Fact]
    public void Lobby_NextTeam_ShouldCycleThroughTeams()
    {
                var lobby = new Lobby();
        lobby.Teams.Add(new Team { Id = 1, Name = "Team1" });
        lobby.Teams.Add(new Team { Id = 2, Name = "Team2" });
        lobby.Teams.Add(new Team { Id = 3, Name = "Team3" });
        lobby.CurrentTeamIndex = 0;

                lobby.CurrentTeam!.Id.Should().Be(1);
        
        lobby.NextTeam();
        lobby.CurrentTeam!.Id.Should().Be(2);
        
        lobby.NextTeam();
        lobby.CurrentTeam!.Id.Should().Be(3);
        
        lobby.NextTeam();
        lobby.CurrentTeam!.Id.Should().Be(1);     }

    [Fact]
    public void Lobby_StartNewRound_ShouldResetRoundState()
    {
                var lobby = new Lobby();
        lobby.Settings.RoundTimeSeconds = 60;
        lobby.CurrentRound = 5;
        lobby.IsLastWordPhase = true;
        lobby.CurrentWord = "OldWord";
        lobby.GuessedWordsThisRound.Add("Word1");

                lobby.StartNewRound();

                lobby.CurrentRound.Should().Be(6);
        lobby.TimeRemaining.Should().Be(60);
        lobby.IsLastWordPhase.Should().BeFalse();
        lobby.CurrentWord.Should().BeNull();
        lobby.GuessedWordsThisRound.Should().BeEmpty();
    }

    [Fact]
    public void Team_NextExplainer_ShouldCycleThroughPlayers()
    {
                var team = new Team { Id = 1, Name = "Team1" };
        team.Players.Add(new Player { Id = 1, Username = "Player1" });
        team.Players.Add(new Player { Id = 2, Username = "Player2" });
        team.Players.Add(new Player { Id = 3, Username = "Player3" });
        team.CurrentExplainerIndex = 0;

                team.CurrentExplainer!.Id.Should().Be(1);
        
        team.NextExplainer();
        team.CurrentExplainer!.Id.Should().Be(2);
        
        team.NextExplainer();
        team.CurrentExplainer!.Id.Should().Be(3);
        
        team.NextExplainer();
        team.CurrentExplainer!.Id.Should().Be(1);     }

    [Fact]
    public void GameSettings_ShouldHaveCorrectDefaults()
    {
                var settings = new GameSettings();

                settings.RoundTimeSeconds.Should().Be(60);
        settings.TotalRounds.Should().Be(10);
        settings.ScoreToWin.Should().Be(50);
        settings.LastWordTimeSeconds.Should().Be(10);
        settings.AllowManualScoreChange.Should().BeTrue();
        settings.AllowHostPassTurn.Should().BeTrue();
        settings.SkipPenalty.Should().Be(0);
    }

    [Fact]
    public void Player_ShouldInitializeWithCorrectDefaults()
    {
                var player = new Player();

                player.TeamId.Should().Be(-1);
        player.IsHost.Should().BeFalse();
        player.IsConnected.Should().BeTrue();
        player.Score.Should().Be(0);
    }

    [Fact]
    public void LobbySummary_ShouldRepresentLobbyCorrectly()
    {
                var lobby = new Lobby
        {
            Id = 1,
            Name = "TestLobby",
            MaxPlayers = 10,
            Password = "secret",
            State = GameState.Waiting
        };
        lobby.Players.Add(new Player { Id = 1, Username = "Host", IsHost = true });
        lobby.Players.Add(new Player { Id = 2, Username = "Player2" });

                var summary = new LobbySummary
        {
            Id = lobby.Id,
            Name = lobby.Name,
            PlayerCount = lobby.PlayerCount,
            MaxPlayers = lobby.MaxPlayers,
            HasPassword = lobby.HasPassword,
            State = lobby.State,
            HostName = lobby.Players.First(p => p.IsHost).Username
        };

                summary.Id.Should().Be(1);
        summary.Name.Should().Be("TestLobby");
        summary.PlayerCount.Should().Be(2);
        summary.MaxPlayers.Should().Be(10);
        summary.HasPassword.Should().BeTrue();
        summary.State.Should().Be(GameState.Waiting);
        summary.HostName.Should().Be("Host");
    }
}

public class RoundResultTests
{
    [Fact]
    public void RoundResult_ShouldStoreCorrectData()
    {
                var result = new RoundResult
        {
            RoundNumber = 5,
            TeamId = 2,
            ExplainerId = 10,
            PointsEarned = 7,
            WordsGuessed = 7,
            WordsSkipped = 2,
            GuessedWords = new List<string> { "word1", "word2", "word3" }
        };

                result.RoundNumber.Should().Be(5);
        result.TeamId.Should().Be(2);
        result.ExplainerId.Should().Be(10);
        result.PointsEarned.Should().Be(7);
        result.WordsGuessed.Should().Be(7);
        result.WordsSkipped.Should().Be(2);
        result.GuessedWords.Should().HaveCount(3);
    }
}

public class GameStateEnumTests
{
    [Fact]
    public void GameState_ShouldHaveCorrectValues()
    {
                ((int)GameState.Waiting).Should().Be(0);
        ((int)GameState.Starting).Should().Be(1);
        ((int)GameState.Playing).Should().Be(2);
        ((int)GameState.RoundEnd).Should().Be(3);
        ((int)GameState.LastWord).Should().Be(4);
        ((int)GameState.Finished).Should().Be(5);
    }
}
