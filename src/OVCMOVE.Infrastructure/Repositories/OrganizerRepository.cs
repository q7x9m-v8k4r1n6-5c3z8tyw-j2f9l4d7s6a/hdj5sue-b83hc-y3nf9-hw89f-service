using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OVCMOVE.Domain.Entities;
using OVCMOVE.Infrastructure.Options;

namespace OVCMOVE.Infrastructure.Repositories;

public class OrganizerRepository : OVCMOVE.Application.Abstractions.Repositories.IOrganizerRepository
{
    private readonly string _connectionString;

    public OrganizerRepository(IOptions<DbConfigOptions> dbConfigOptions)
    {
        var connectionString = dbConfigOptions.Value.SQLServer?.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DbConfig:SQLServer:ConnectionString' is not configured.");
        }

        _connectionString = connectionString;
    }

    public async Task<Organizer?> GetByEmailAsync(string email)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            SELECT
                Id,
                Name,
                Username,
                Email,
                PasswordHash,
                IsActive,
                CreatedAt
            FROM Organizers
            WHERE Email = @Email";

        return await connection.QueryFirstOrDefaultAsync<Organizer>(sql, new { Email = email });
    }

    public async Task AddAsync(Organizer organizer)
    {
        using var connection = new SqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO Organizers (Id, Name, Username, PasswordHash, Email, IsActive, CreatedAt)
            VALUES (@Id, @Name, @Username, @PasswordHash, @Email, @IsActive, @CreatedAt)";

        await connection.ExecuteAsync(sql, organizer);
    }
}
