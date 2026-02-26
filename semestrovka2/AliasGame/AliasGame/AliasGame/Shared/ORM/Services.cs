using AliasGame.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AliasGame.Shared.ORM;

public class UserService
{
    private readonly UnitOfWorkFactory _factory;

    public UserService(UnitOfWorkFactory factory)
    {
        _factory = factory;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var uow = _factory.Create();
        return await uow.Users.GetByIdAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var uow = _factory.Create();
        return await uow.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var uow = _factory.Create();
        return await uow.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> CreateUserAsync(string username, string passwordHash, string? email = null)
    {
        using var uow = _factory.Create();
        var user = new User
        {
            Username = username,
            PasswordHash = passwordHash,
            Email = string.IsNullOrWhiteSpace(email) ? null : email
        };

        await uow.Users.AddAsync(user);
        await uow.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string passwordHash)
    {
        using var uow = _factory.Create();
        var user = await uow.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user != null && user.PasswordHash == passwordHash && !user.IsBanned;
    }

    public async Task<bool> IsUserBannedAsync(int userId)
    {
        using var uow = _factory.Create();
        var user = await uow.Users.GetByIdAsync(userId);
        if (user == null) return false;

        if (user.IsBanned)
        {
                        if (user.BanUntil.HasValue && user.BanUntil.Value < DateTime.UtcNow)
            {
                user.IsBanned = false;
                user.BanReason = null;
                user.BanUntil = null;
                await uow.SaveChangesAsync();
                return false;
            }
            return true;
        }
        return false;
    }

    public async Task BanUserAsync(int userId, string reason, int? durationMinutes = null)
    {
        using var uow = _factory.Create();
        var user = await uow.Users.GetByIdAsync(userId);
        if (user == null) return;

        user.IsBanned = true;
        user.BanReason = reason;
        user.BanUntil = durationMinutes.HasValue
            ? DateTime.UtcNow.AddMinutes(durationMinutes.Value)
            : null;

        await uow.SaveChangesAsync();
    }

    public async Task UnbanUserAsync(int userId)
    {
        using var uow = _factory.Create();
        var user = await uow.Users.GetByIdAsync(userId);
        if (user == null) return;

        user.IsBanned = false;
        user.BanReason = null;
        user.BanUntil = null;

        await uow.SaveChangesAsync();
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        using var uow = _factory.Create();
        var user = await uow.Users.GetByIdAsync(userId);
        if (user == null) return;

        user.LastLogin = DateTime.UtcNow;
        await uow.SaveChangesAsync();
    }

    public async Task UpdateStatsAsync(int userId, bool won, int score)
    {
        using var uow = _factory.Create();
        var user = await uow.Users.GetByIdAsync(userId);
        if (user == null) return;

        user.GamesPlayed++;
        if (won) user.GamesWon++;
        user.TotalScore += score;

        await uow.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetUsersPagedAsync(int page, int pageSize, string? search = null)
    {
        using var uow = _factory.Create();
        var query = uow.Users.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Username.Contains(search) ||
                                    (u.Email != null && u.Email.Contains(search)));
        }

        return await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalUsersCountAsync(string? search = null)
    {
        using var uow = _factory.Create();
        if (string.IsNullOrWhiteSpace(search))
            return await uow.Users.CountAsync();

        return await uow.Users.CountAsync(u =>
            u.Username.Contains(search) ||
            (u.Email != null && u.Email.Contains(search)));
    }
}

public class WordService
{
    private readonly UnitOfWorkFactory _factory;
    private readonly Random _random = new();

    public WordService(UnitOfWorkFactory factory)
    {
        _factory = factory;
    }

    public async Task<Word?> GetByIdAsync(int id)
    {
        using var uow = _factory.Create();
        return await uow.Words.GetByIdAsync(id);
    }

    public async Task<Word?> GetRandomWordAsync(int? categoryId = null, IEnumerable<int>? excludeIds = null)
    {
        using var uow = _factory.Create();
        var query = uow.Words.Query()
            .Where(w => w.IsActive);

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(w => w.CategoryId == categoryId.Value);
        }

        if (excludeIds != null && excludeIds.Any())
        {
            var excludeList = excludeIds.ToList();
            query = query.Where(w => !excludeList.Contains(w.Id));
        }

        var count = await query.CountAsync();
        if (count == 0) return null;

        var skip = _random.Next(count);
        return await query.Skip(skip).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Word>> GetWordsPagedAsync(int page, int pageSize, int? categoryId = null, string? search = null)
    {
        using var uow = _factory.Create();
        IQueryable<Word> query = uow.Words.Query()
            .Include(w => w.Category)
            .AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(w => w.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(w => w.WordText.Contains(search));
        }

        return await query
            .OrderBy(w => w.WordText)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalWordsCountAsync(int? categoryId = null, string? search = null)
    {
        using var uow = _factory.Create();
        var query = uow.Words.Query();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(w => w.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(w => w.WordText.Contains(search));
        }

        return await query.CountAsync();
    }

    public async Task<Word> AddWordAsync(string wordText, int categoryId, int difficulty = 1)
    {
        using var uow = _factory.Create();
        var word = new Word
        {
            WordText = wordText,
            CategoryId = categoryId,
            Difficulty = difficulty
        };

        await uow.Words.AddAsync(word);
        await uow.SaveChangesAsync();
        return word;
    }

    public async Task UpdateWordAsync(int wordId, string wordText, int categoryId, int difficulty)
    {
        using var uow = _factory.Create();
        var word = await uow.Words.GetByIdAsync(wordId);
        if (word == null) return;

        word.WordText = wordText;
        word.CategoryId = categoryId;
        word.Difficulty = difficulty;

        await uow.SaveChangesAsync();
    }

    public async Task DeleteWordAsync(int wordId)
    {
        using var uow = _factory.Create();
        var word = await uow.Words.GetByIdAsync(wordId);
        if (word == null) return;

        uow.Words.Remove(word);
        await uow.SaveChangesAsync();
    }

    public async Task RecordWordUsageAsync(int wordId, bool wasGuessed)
    {
        using var uow = _factory.Create();
        var word = await uow.Words.GetByIdAsync(wordId);
        if (word == null) return;

        word.TimesUsed++;
        if (wasGuessed)
            word.TimesGuessed++;
        else
            word.TimesSkipped++;

        await uow.SaveChangesAsync();
    }

    public async Task<bool> WordExistsAsync(string wordText, int categoryId)
    {
        using var uow = _factory.Create();
        return await uow.Words.ExistsAsync(w =>
            w.WordText == wordText && w.CategoryId == categoryId);
    }
}

public class CategoryService
{
    private readonly UnitOfWorkFactory _factory;

    public CategoryService(UnitOfWorkFactory factory)
    {
        _factory = factory;
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        using var uow = _factory.Create();
        return await uow.Categories.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Category>> GetAllAsync(bool includeInactive = false)
    {
        using var uow = _factory.Create();
        if (includeInactive)
            return await uow.Categories.GetAllAsync();

        return await uow.Categories.FindAsync(c => c.IsActive);
    }

    public async Task<IEnumerable<object>> GetAllWithWordCountAsync()
    {
        using var uow = _factory.Create();
        return await uow.Categories.Query()
            .Where(c => c.IsActive)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                WordCount = c.Words.Count(w => w.IsActive)
            })
            .ToListAsync();
    }

    public async Task<Category> AddCategoryAsync(string name, string? description = null)
    {
        using var uow = _factory.Create();
        var category = new Category
        {
            Name = name,
            Description = description
        };

        await uow.Categories.AddAsync(category);
        await uow.SaveChangesAsync();
        return category;
    }

    public async Task UpdateCategoryAsync(int categoryId, string name, string? description)
    {
        using var uow = _factory.Create();
        var category = await uow.Categories.GetByIdAsync(categoryId);
        if (category == null) return;

        category.Name = name;
        category.Description = description;

        await uow.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(int categoryId)
    {
        using var uow = _factory.Create();
        var category = await uow.Categories.GetByIdAsync(categoryId);
        if (category == null) return;

        uow.Categories.Remove(category);
        await uow.SaveChangesAsync();
    }

    public async Task<bool> CategoryExistsAsync(string name)
    {
        using var uow = _factory.Create();
        return await uow.Categories.ExistsAsync(c => c.Name == name);
    }
}

public class GameHistoryService
{
    private readonly UnitOfWorkFactory _factory;

    public GameHistoryService(UnitOfWorkFactory factory)
    {
        _factory = factory;
    }

    public async Task<GameHistory> RecordGameAsync(int userId, string? teamName, int finalScore,
        bool isWinner, int wordsExplained, int wordsGuessed, int durationSeconds)
    {
        using var uow = _factory.Create();
        var history = new GameHistory
        {
            UserId = userId,
            TeamName = teamName,
            FinalScore = finalScore,
            IsWinner = isWinner,
            WordsExplained = wordsExplained,
            WordsGuessed = wordsGuessed,
            GameDurationSeconds = durationSeconds
        };

        await uow.GameHistories.AddAsync(history);
        await uow.SaveChangesAsync();
        return history;
    }

    public async Task<IEnumerable<GameHistory>> GetUserHistoryAsync(int userId, int limit = 10)
    {
        using var uow = _factory.Create();
        return await uow.GameHistories.Query()
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.GameDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<object> GetUserStatsAsync(int userId)
    {
        using var uow = _factory.Create();
        var games = await uow.GameHistories.FindAsync(g => g.UserId == userId);
        var gamesList = games.ToList();

        return new
        {
            TotalGames = gamesList.Count,
            Wins = gamesList.Count(g => g.IsWinner),
            TotalScore = gamesList.Sum(g => g.FinalScore),
            TotalWordsExplained = gamesList.Sum(g => g.WordsExplained),
            TotalWordsGuessed = gamesList.Sum(g => g.WordsGuessed),
            AverageScore = gamesList.Any() ? gamesList.Average(g => g.FinalScore) : 0
        };
    }
}