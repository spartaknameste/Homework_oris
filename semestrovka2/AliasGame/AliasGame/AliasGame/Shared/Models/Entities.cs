using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AliasGame.Shared.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("email")]
    public string? Email { get; set; }

    [Column("is_admin")]
    public bool IsAdmin { get; set; } = false;

    [Column("is_banned")]
    public bool IsBanned { get; set; } = false;

    [Column("ban_reason")]
    public string? BanReason { get; set; }

    [Column("ban_until")]
    public DateTime? BanUntil { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Column("games_played")]
    public int GamesPlayed { get; set; } = 0;

    [Column("games_won")]
    public int GamesWon { get; set; } = 0;

    [Column("total_score")]
    public int TotalScore { get; set; } = 0;

        public virtual ICollection<GameHistory> GameHistories { get; set; } = new List<GameHistory>();
}

[Table("categories")]
public class Category
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Word> Words { get; set; } = new List<Word>();
}

[Table("words")]
public class Word
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("word_text")]
    public string WordText { get; set; } = string.Empty;

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("difficulty")]
    public int Difficulty { get; set; } = 1; 
    [Column("times_used")]
    public int TimesUsed { get; set; } = 0;

    [Column("times_guessed")]
    public int TimesGuessed { get; set; } = 0;

    [Column("times_skipped")]
    public int TimesSkipped { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }
}

[Table("game_history")]
public class GameHistory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("game_date")]
    public DateTime GameDate { get; set; } = DateTime.UtcNow;

    [Column("team_name")]
    public string? TeamName { get; set; }

    [Column("final_score")]
    public int FinalScore { get; set; }

    [Column("is_winner")]
    public bool IsWinner { get; set; }

    [Column("words_explained")]
    public int WordsExplained { get; set; }

    [Column("words_guessed")]
    public int WordsGuessed { get; set; }

    [Column("game_duration_seconds")]
    public int GameDurationSeconds { get; set; }

        [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}

[Table("game_settings_presets")]
public class GameSettingsPreset
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("round_time_seconds")]
    public int RoundTimeSeconds { get; set; } = 60;

    [Column("total_rounds")]
    public int TotalRounds { get; set; } = 10;

    [Column("score_to_win")]
    public int ScoreToWin { get; set; } = 50;

    [Column("last_word_time_seconds")]
    public int LastWordTimeSeconds { get; set; } = 10;

    [Column("allow_manual_score_change")]
    public bool AllowManualScoreChange { get; set; } = true;

    [Column("allow_host_pass_turn")]
    public bool AllowHostPassTurn { get; set; } = true;

    [Column("skip_penalty")]
    public int SkipPenalty { get; set; } = 0;

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("created_by_user_id")]
    public int? CreatedByUserId { get; set; }
}
