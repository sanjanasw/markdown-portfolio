using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Xunit;
using FluentAssertions;
using SchoolCommsIntegration.Infrastructure;

namespace SchoolCommsIntegration.Infrastructure.Tests;

/// <summary>
/// Integration tests for DatabaseHandler that would work with a real database.
/// These tests are marked with a custom trait to allow running them separately.
/// </summary>
[Trait("Category", "Integration")]
public class DatabaseHandlerIntegrationTests : IDisposable
{
    private const string TestConnectionString = "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=true;";
    private readonly DatabaseHandler _databaseHandler;
    private bool _databaseInitialized;

    public DatabaseHandlerIntegrationTests()
    {
        _databaseHandler = new DatabaseHandler();
    }

    [Fact(Skip = "Requires SQL Server LocalDB to be installed")]
    public async Task GetFirstAsync_WithRealDatabase_ReturnsExpectedResult()
    {
        // Arrange
        await EnsureDatabaseSetup();
        var parameters = new DynamicParameters();
        parameters.Add("@Id", 1);

        // Act
        var result = await _databaseHandler.GetFirstAsync<TestEntity>(
            TestConnectionString, 
            "sp_GetTestEntity", 
            parameters);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
    }

    [Fact(Skip = "Requires SQL Server LocalDB to be installed")]
    public async Task GetListAsync_WithRealDatabase_ReturnsExpectedResults()
    {
        // Arrange
        await EnsureDatabaseSetup();
        var parameters = new DynamicParameters();

        // Act
        var results = await _databaseHandler.GetListAsync<TestEntity>(
            TestConnectionString, 
            "sp_GetAllTestEntities", 
            parameters);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCountGreaterThan(0);
    }

    [Fact(Skip = "Requires SQL Server LocalDB to be installed")]
    public async Task GetDataTableAsync_WithRealDatabase_ReturnsDataTable()
    {
        // Arrange
        await EnsureDatabaseSetup();
        var parameters = new DynamicParameters();

        // Act
        var result = await _databaseHandler.GetDataTableAsync(
            TestConnectionString, 
            "sp_GetAllTestEntities", 
            parameters);

        // Assert
        result.Should().NotBeNull();
        result.Rows.Count.Should().BeGreaterThan(0);
        result.Columns.Should().Contain(c => c.ColumnName == "Id");
        result.Columns.Should().Contain(c => c.ColumnName == "Name");
    }

    [Fact(Skip = "Requires SQL Server LocalDB to be installed")]
    public async Task ExecuteAsync_WithRealDatabase_ReturnsAffectedRows()
    {
        // Arrange
        await EnsureDatabaseSetup();
        var parameters = new DynamicParameters();
        parameters.Add("@Name", "Integration Test Entity");
        parameters.Add("@Description", "Created during integration test");

        // Act
        var result = await _databaseHandler.ExecuteAsync(
            TestConnectionString, 
            "sp_InsertTestEntity", 
            parameters);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    private async Task EnsureDatabaseSetup()
    {
        if (_databaseInitialized) return;

        try
        {
            await using var connection = new SqlConnection(TestConnectionString);
            await connection.OpenAsync();

            // Create test table if it doesn't exist
            await connection.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestEntities')
                BEGIN
                    CREATE TABLE TestEntities (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Name NVARCHAR(100) NOT NULL,
                        Description NVARCHAR(500),
                        CreatedDate DATETIME2 DEFAULT GETDATE()
                    )
                END");

            // Create stored procedures
            await connection.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetTestEntity')
                BEGIN
                    EXEC('
                    CREATE PROCEDURE sp_GetTestEntity
                        @Id INT
                    AS
                    BEGIN
                        SELECT Id, Name, Description, CreatedDate
                        FROM TestEntities
                        WHERE Id = @Id
                    END')
                END");

            await connection.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetAllTestEntities')
                BEGIN
                    EXEC('
                    CREATE PROCEDURE sp_GetAllTestEntities
                    AS
                    BEGIN
                        SELECT Id, Name, Description, CreatedDate
                        FROM TestEntities
                        ORDER BY Id
                    END')
                END");

            await connection.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_InsertTestEntity')
                BEGIN
                    EXEC('
                    CREATE PROCEDURE sp_InsertTestEntity
                        @Name NVARCHAR(100),
                        @Description NVARCHAR(500)
                    AS
                    BEGIN
                        INSERT INTO TestEntities (Name, Description)
                        VALUES (@Name, @Description)
                        
                        SELECT @@ROWCOUNT
                    END')
                END");

            // Insert some test data
            await connection.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM TestEntities WHERE Id = 1)
                BEGIN
                    INSERT INTO TestEntities (Name, Description)
                    VALUES ('Test Entity 1', 'First test entity')
                END");

            await connection.ExecuteAsync(@"
                IF NOT EXISTS (SELECT * FROM TestEntities WHERE Id = 2)
                BEGIN
                    INSERT INTO TestEntities (Name, Description)
                    VALUES ('Test Entity 2', 'Second test entity')
                END");

            _databaseInitialized = true;
        }
        catch (SqlException)
        {
            // Database setup failed - tests will be skipped
            throw new InvalidOperationException("Could not set up test database. Integration tests require SQL Server LocalDB.");
        }
    }

    public void Dispose()
    {
        // Cleanup is handled by the database itself since we're using LocalDB
        // In a real scenario, you might want to clean up test data
    }
}

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
}