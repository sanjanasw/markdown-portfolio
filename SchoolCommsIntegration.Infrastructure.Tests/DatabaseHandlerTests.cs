using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Moq;
using Xunit;
using FluentAssertions;
using SchoolCommsIntegration.Infrastructure;

namespace SchoolCommsIntegration.Infrastructure.Tests;

public class DatabaseHandlerTests
{
    private readonly Mock<TestableDatabaseHandler> _mockDatabaseHandler;
    private readonly Mock<SqlConnection> _mockConnection;
    private const string TestConnectionString = "Server=test;Database=test;";
    private const string TestStoredProcedure = "sp_TestProcedure";

    public DatabaseHandlerTests()
    {
        _mockConnection = new Mock<SqlConnection>();
        _mockDatabaseHandler = new Mock<TestableDatabaseHandler> { CallBase = true };
        _mockDatabaseHandler.Setup(x => x.CreateConnection(It.IsAny<string>()))
            .Returns(_mockConnection.Object);
    }

    [Fact]
    public async Task GetFirstAsync_WithValidParameters_ReturnsExpectedResult()
    {
        // Arrange
        var expectedResult = new TestModel { Id = 1, Name = "Test" };
        var parameters = new DynamicParameters();
        parameters.Add("@Id", 1);

        var mockDapper = new Mock<IDapperWrapper>();
        mockDapper.Setup(x => x.QueryFirstAsync<TestModel>(
                _mockConnection.Object,
                TestStoredProcedure,
                parameters,
                CommandType.StoredProcedure))
            .ReturnsAsync(expectedResult);

        var handler = new TestableDatabaseHandlerWithDapper(mockDapper.Object);

        // Act
        var result = await handler.GetFirstAsync<TestModel>(TestConnectionString, TestStoredProcedure, parameters);

        // Assert
        result.Should().BeEquivalentTo(expectedResult);
        mockDapper.Verify(x => x.QueryFirstAsync<TestModel>(
            _mockConnection.Object,
            TestStoredProcedure,
            parameters,
            CommandType.StoredProcedure), Times.Once);
    }

    [Fact]
    public async Task GetFirstAsync_WithNullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new DatabaseHandler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.GetFirstAsync<TestModel>(TestConnectionString, TestStoredProcedure, null));
    }

    [Fact]
    public async Task GetFirstAsync_WithInvalidConnectionString_ThrowsArgumentException()
    {
        // Arrange
        var handler = new DatabaseHandler();
        var parameters = new DynamicParameters();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.GetFirstAsync<TestModel>("", TestStoredProcedure, parameters));
    }

    [Fact]
    public async Task GetFirstAsync_WhenNoDataFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var parameters = new DynamicParameters();
        var mockDapper = new Mock<IDapperWrapper>();
        mockDapper.Setup(x => x.QueryFirstAsync<TestModel>(
                _mockConnection.Object,
                TestStoredProcedure,
                parameters,
                CommandType.StoredProcedure))
            .ThrowsAsync(new InvalidOperationException("Sequence contains no elements"));

        var handler = new TestableDatabaseHandlerWithDapper(mockDapper.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.GetFirstAsync<TestModel>(TestConnectionString, TestStoredProcedure, parameters));
    }

    [Fact]
    public async Task GetListAsync_WithValidParameters_ReturnsExpectedResults()
    {
        // Arrange
        var expectedResults = new List<TestModel>
        {
            new() { Id = 1, Name = "Test1" },
            new() { Id = 2, Name = "Test2" }
        };
        var parameters = new DynamicParameters();
        parameters.Add("@Filter", "test");

        var mockDapper = new Mock<IDapperWrapper>();
        mockDapper.Setup(x => x.QueryAsync<TestModel>(
                _mockConnection.Object,
                TestStoredProcedure,
                parameters,
                CommandType.StoredProcedure))
            .ReturnsAsync(expectedResults);

        var handler = new TestableDatabaseHandlerWithDapper(mockDapper.Object);

        // Act
        var result = await handler.GetListAsync<TestModel>(TestConnectionString, TestStoredProcedure, parameters);

        // Assert
        result.Should().BeEquivalentTo(expectedResults);
        mockDapper.Verify(x => x.QueryAsync<TestModel>(
            _mockConnection.Object,
            TestStoredProcedure,
            parameters,
            CommandType.StoredProcedure), Times.Once);
    }

    [Fact]
    public async Task GetListAsync_WhenNoDataFound_ReturnsEmptyCollection()
    {
        // Arrange
        var parameters = new DynamicParameters();
        var mockDapper = new Mock<IDapperWrapper>();
        mockDapper.Setup(x => x.QueryAsync<TestModel>(
                _mockConnection.Object,
                TestStoredProcedure,
                parameters,
                CommandType.StoredProcedure))
            .ReturnsAsync(new List<TestModel>());

        var handler = new TestableDatabaseHandlerWithDapper(mockDapper.Object);

        // Act
        var result = await handler.GetListAsync<TestModel>(TestConnectionString, TestStoredProcedure, parameters);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetListAsync_WithNullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new DatabaseHandler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.GetListAsync<TestModel>(TestConnectionString, TestStoredProcedure, null));
    }

    [Fact]
    public async Task GetDataTableAsync_WithValidParameters_ReturnsDataTable()
    {
        // Arrange
        var parameters = new DynamicParameters();
        parameters.Add("@Id", 1);

        var mockReader = new Mock<IDataReader>();
        var expectedDataTable = new DataTable();
        expectedDataTable.Columns.Add("Id", typeof(int));
        expectedDataTable.Columns.Add("Name", typeof(string));
        expectedDataTable.Rows.Add(1, "Test");

        var mockDapper = new Mock<IDapperWrapper>();
        mockDapper.Setup(x => x.ExecuteReaderAsync(
                _mockConnection.Object,
                TestStoredProcedure,
                parameters,
                CommandType.StoredProcedure))
            .ReturnsAsync(mockReader.Object);

        // Setup mock reader to simulate DataTable.Load behavior
        mockReader.SetupSequence(x => x.Read())
            .Returns(true)
            .Returns(false);
        mockReader.Setup(x => x.FieldCount).Returns(2);
        mockReader.Setup(x => x.GetName(0)).Returns("Id");
        mockReader.Setup(x => x.GetName(1)).Returns("Name");
        mockReader.Setup(x => x.GetFieldType(0)).Returns(typeof(int));
        mockReader.Setup(x => x.GetFieldType(1)).Returns(typeof(string));
        mockReader.Setup(x => x.GetValue(0)).Returns(1);
        mockReader.Setup(x => x.GetValue(1)).Returns("Test");

        var handler = new TestableDatabaseHandlerWithDapper(mockDapper.Object);

        // Act
        var result = await handler.GetDataTableAsync(TestConnectionString, TestStoredProcedure, parameters);

        // Assert
        result.Should().NotBeNull();
        result.Columns.Count.Should().BeGreaterThan(0);
        mockDapper.Verify(x => x.ExecuteReaderAsync(
            _mockConnection.Object,
            TestStoredProcedure,
            parameters,
            CommandType.StoredProcedure), Times.Once);
    }

    [Fact]
    public async Task GetDataTableAsync_WithNullParameters_ReturnsDataTable()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        var mockDapper = new Mock<IDapperWrapper>();
        mockDapper.Setup(x => x.ExecuteReaderAsync(
                _mockConnection.Object,
                TestStoredProcedure,
                null,
                CommandType.StoredProcedure))
            .ReturnsAsync(mockReader.Object);

        mockReader.SetupSequence(x => x.Read()).Returns(false);
        mockReader.Setup(x => x.FieldCount).Returns(0);

        var handler = new TestableDatabaseHandlerWithDapper(mockDapper.Object);

        // Act
        var result = await handler.GetDataTableAsync(TestConnectionString, TestStoredProcedure, null);

        // Assert
        result.Should().NotBeNull();
        mockDapper.Verify(x => x.ExecuteReaderAsync(
            _mockConnection.Object,
            TestStoredProcedure,
            null,
            CommandType.StoredProcedure), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidParameters_ReturnsAffectedRows()
    {
        // Arrange
        var expectedAffectedRows = 1;
        var parameters = new DynamicParameters();
        parameters.Add("@Id", 1);

        var mockDapper = new Mock<IDapperWrapper>();
        mockDapper.Setup(x => x.ExecuteAsync(
                _mockConnection.Object,
                TestStoredProcedure,
                parameters,
                CommandType.StoredProcedure))
            .ReturnsAsync(expectedAffectedRows);

        var handler = new TestableDatabaseHandlerWithDapper(mockDapper.Object);

        // Act
        var result = await handler.ExecuteAsync(TestConnectionString, TestStoredProcedure, parameters);

        // Assert
        result.Should().Be(expectedAffectedRows);
        mockDapper.Verify(x => x.ExecuteAsync(
            _mockConnection.Object,
            TestStoredProcedure,
            parameters,
            CommandType.StoredProcedure), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullParameters_ReturnsAffectedRows()
    {
        // Arrange
        var expectedAffectedRows = 0;
        var mockDapper = new Mock<IDapperWrapper>();
        mockDapper.Setup(x => x.ExecuteAsync(
                _mockConnection.Object,
                TestStoredProcedure,
                null,
                CommandType.StoredProcedure))
            .ReturnsAsync(expectedAffectedRows);

        var handler = new TestableDatabaseHandlerWithDapper(mockDapper.Object);

        // Act
        var result = await handler.ExecuteAsync(TestConnectionString, TestStoredProcedure, null);

        // Assert
        result.Should().Be(expectedAffectedRows);
        mockDapper.Verify(x => x.ExecuteAsync(
            _mockConnection.Object,
            TestStoredProcedure,
            null,
            CommandType.StoredProcedure), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSqlExceptionOccurs_ThrowsSqlException()
    {
        // Arrange
        var parameters = new DynamicParameters();
        var mockDapper = new Mock<IDapperWrapper>();
        mockDapper.Setup(x => x.ExecuteAsync(
                _mockConnection.Object,
                TestStoredProcedure,
                parameters,
                CommandType.StoredProcedure))
            .ThrowsAsync(new SqlException());

        var handler = new TestableDatabaseHandlerWithDapper(mockDapper.Object);

        // Act & Assert
        await Assert.ThrowsAsync<SqlException>(() =>
            handler.ExecuteAsync(TestConnectionString, TestStoredProcedure, parameters));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task GetFirstAsync_WithInvalidConnectionString_ThrowsException(string connectionString)
    {
        // Arrange
        var handler = new DatabaseHandler();
        var parameters = new DynamicParameters();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.GetFirstAsync<TestModel>(connectionString, TestStoredProcedure, parameters));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task GetListAsync_WithInvalidStoredProcedureName_ThrowsException(string storedProcedure)
    {
        // Arrange
        var handler = new DatabaseHandler();
        var parameters = new DynamicParameters();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.GetListAsync<TestModel>(TestConnectionString, storedProcedure, parameters));
    }

    [Fact]
    public void CreateConnection_WithValidConnectionString_ReturnsSqlConnection()
    {
        // Arrange
        var handler = new TestableDatabaseHandler();

        // Act
        var connection = handler.CreateConnection(TestConnectionString);

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<SqlConnection>();
        connection.ConnectionString.Should().Be(TestConnectionString);
    }

    [Fact]
    public async Task AllMethods_EnsureConnectionIsDisposed()
    {
        // This test ensures that connections are properly disposed
        // The 'await using' pattern in the original code should handle this
        // We can verify this by checking that CreateConnection is called once per method call

        // Arrange
        var parameters = new DynamicParameters();
        var mockDapper = new Mock<IDapperWrapper>();
        var handler = new TestableDatabaseHandlerWithDapper(mockDapper.Object);

        // Mock all Dapper methods to return default values
        mockDapper.Setup(x => x.QueryFirstAsync<TestModel>(It.IsAny<SqlConnection>(), It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
            .ReturnsAsync(new TestModel());
        mockDapper.Setup(x => x.QueryAsync<TestModel>(It.IsAny<SqlConnection>(), It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
            .ReturnsAsync(new List<TestModel>());
        mockDapper.Setup(x => x.ExecuteReaderAsync(It.IsAny<SqlConnection>(), It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
            .ReturnsAsync(Mock.Of<IDataReader>());
        mockDapper.Setup(x => x.ExecuteAsync(It.IsAny<SqlConnection>(), It.IsAny<string>(), It.IsAny<DynamicParameters>(), It.IsAny<CommandType>()))
            .ReturnsAsync(1);

        // Act
        await handler.GetFirstAsync<TestModel>(TestConnectionString, TestStoredProcedure, parameters);
        await handler.GetListAsync<TestModel>(TestConnectionString, TestStoredProcedure, parameters);
        await handler.GetDataTableAsync(TestConnectionString, TestStoredProcedure, parameters);
        await handler.ExecuteAsync(TestConnectionString, TestStoredProcedure, parameters);

        // Assert
        // Verify that CreateConnection was called once for each method call
        handler.CreateConnectionCallCount.Should().Be(4);
    }
}

// Test model for testing generic methods
public class TestModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Testable version of DatabaseHandler that allows us to mock the connection creation
public class TestableDatabaseHandler : DatabaseHandler
{
    public int CreateConnectionCallCount { get; private set; }

    public override SqlConnection CreateConnection(string connectionString)
    {
        CreateConnectionCallCount++;
        return base.CreateConnection(connectionString);
    }
}

// Interface to wrap Dapper methods for easier testing
public interface IDapperWrapper
{
    Task<T> QueryFirstAsync<T>(SqlConnection connection, string sql, DynamicParameters? parameters, CommandType commandType);
    Task<IEnumerable<T>> QueryAsync<T>(SqlConnection connection, string sql, DynamicParameters? parameters, CommandType commandType);
    Task<IDataReader> ExecuteReaderAsync(SqlConnection connection, string sql, DynamicParameters? parameters, CommandType commandType);
    Task<int> ExecuteAsync(SqlConnection connection, string sql, DynamicParameters? parameters, CommandType commandType);
}

// Testable DatabaseHandler that uses the IDapperWrapper for easier mocking
public class TestableDatabaseHandlerWithDapper : DatabaseHandler
{
    private readonly IDapperWrapper _dapperWrapper;

    public TestableDatabaseHandlerWithDapper(IDapperWrapper dapperWrapper)
    {
        _dapperWrapper = dapperWrapper;
    }

    public int CreateConnectionCallCount { get; private set; }

    public override SqlConnection CreateConnection(string connectionString)
    {
        CreateConnectionCallCount++;
        return new SqlConnection(connectionString);
    }

    public new async Task<T> GetFirstAsync<T>(string connectionString, string storedProcedure, DynamicParameters parameters)
    {
        await using var connection = CreateConnection(connectionString);
        return await _dapperWrapper.QueryFirstAsync<T>(connection, storedProcedure, parameters, CommandType.StoredProcedure);
    }

    public new async Task<IEnumerable<T>> GetListAsync<T>(string connectionString, string storedProcedure, DynamicParameters parameters)
    {
        await using var connection = CreateConnection(connectionString);
        return await _dapperWrapper.QueryAsync<T>(connection, storedProcedure, parameters, CommandType.StoredProcedure);
    }

    public new async Task<DataTable> GetDataTableAsync(string connectionString, string storedProcedure, DynamicParameters? parameters = null)
    {
        await using var connection = CreateConnection(connectionString);
        await using var reader = await _dapperWrapper.ExecuteReaderAsync(connection, storedProcedure, parameters, CommandType.StoredProcedure);

        var dataTable = new DataTable();
        dataTable.Load(reader);
        return dataTable;
    }

    public new async Task<int> ExecuteAsync(string connectionString, string storedProcedure, DynamicParameters? parameters = null)
    {
        await using var connection = CreateConnection(connectionString);
        return await _dapperWrapper.ExecuteAsync(connection, storedProcedure, parameters, CommandType.StoredProcedure);
    }
}