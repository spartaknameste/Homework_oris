using AliasGame.Shared.Models;

namespace AliasGame.Shared.ORM;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Category> Categories { get; }
    IRepository<Word> Words { get; }
    IRepository<GameHistory> GameHistories { get; }
    IRepository<GameSettingsPreset> GameSettingsPresets { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly AliasDbContext _context;
    private IRepository<User>? _users;
    private IRepository<Category>? _categories;
    private IRepository<Word>? _words;
    private IRepository<GameHistory>? _gameHistories;
    private IRepository<GameSettingsPreset>? _gameSettingsPresets;
    private bool _disposed = false;

    public UnitOfWork(AliasDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users =>
        _users ??= new Repository<User>(_context);

    public IRepository<Category> Categories =>
        _categories ??= new Repository<Category>(_context);

    public IRepository<Word> Words =>
        _words ??= new Repository<Word>(_context);

    public IRepository<GameHistory> GameHistories =>
        _gameHistories ??= new Repository<GameHistory>(_context);

    public IRepository<GameSettingsPreset> GameSettingsPresets =>
        _gameSettingsPresets ??= new Repository<GameSettingsPreset>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CommitTransactionAsync();
        }
    }

    public async Task RollbackAsync()
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.RollbackTransactionAsync();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

public class UnitOfWorkFactory
{
    private readonly string _connectionString;

    public UnitOfWorkFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

                    public IUnitOfWork Create()
    {
        var context = new AliasDbContext(_connectionString);
        return new UnitOfWork(context);
    }
}