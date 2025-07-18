# DatabaseHandler Unit Test Coverage

## Overview
This document outlines the comprehensive unit test coverage for the `DatabaseHandler` class in the SchoolCommsIntegration.Infrastructure project.

## Test Structure

### Test Projects
- **SchoolCommsIntegration.Infrastructure.Tests**: Contains all unit and integration tests
- **Test Framework**: xUnit with Moq for mocking and FluentAssertions for readable assertions

### Test Categories

#### 1. Unit Tests (`DatabaseHandlerTests.cs`)
These tests focus on testing individual methods in isolation using mocks and stubs.

#### 2. Integration Tests (`DatabaseHandlerIntegrationTests.cs`)
These tests verify the DatabaseHandler works with a real database (currently configured for SQL Server LocalDB).

## Test Coverage Details

### GetFirstAsync<T> Method Coverage

#### ✅ Happy Path Tests
- **GetFirstAsync_WithValidParameters_ReturnsExpectedResult**: Tests successful data retrieval with valid parameters
- **GetFirstAsync_WithValidConnectionString_CallsDapperCorrectly**: Verifies correct Dapper method invocation

#### ✅ Error Handling Tests
- **GetFirstAsync_WithNullParameters_ThrowsArgumentNullException**: Tests null parameter validation
- **GetFirstAsync_WithInvalidConnectionString_ThrowsArgumentException**: Tests invalid connection string scenarios
- **GetFirstAsync_WhenNoDataFound_ThrowsInvalidOperationException**: Tests behavior when no data is found

#### ✅ Edge Case Tests
- **GetFirstAsync_WithInvalidConnectionString_ThrowsException**: Tests various invalid connection string formats (empty, null, whitespace)

### GetListAsync<T> Method Coverage

#### ✅ Happy Path Tests
- **GetListAsync_WithValidParameters_ReturnsExpectedResults**: Tests successful list retrieval
- **GetListAsync_WhenNoDataFound_ReturnsEmptyCollection**: Tests behavior when query returns no results

#### ✅ Error Handling Tests
- **GetListAsync_WithNullParameters_ThrowsArgumentNullException**: Tests null parameter validation
- **GetListAsync_WithInvalidStoredProcedureName_ThrowsException**: Tests invalid stored procedure name scenarios

### GetDataTableAsync Method Coverage

#### ✅ Happy Path Tests
- **GetDataTableAsync_WithValidParameters_ReturnsDataTable**: Tests successful DataTable creation
- **GetDataTableAsync_WithNullParameters_ReturnsDataTable**: Tests handling of optional null parameters

#### ✅ Mocking Strategy
- Uses mock `IDataReader` to simulate database reader behavior
- Tests `DataTable.Load()` functionality without requiring a real database

### ExecuteAsync Method Coverage

#### ✅ Happy Path Tests
- **ExecuteAsync_WithValidParameters_ReturnsAffectedRows**: Tests successful command execution
- **ExecuteAsync_WithNullParameters_ReturnsAffectedRows**: Tests handling of optional null parameters

#### ✅ Error Handling Tests
- **ExecuteAsync_WhenSqlExceptionOccurs_ThrowsSqlException**: Tests SQL exception propagation

### Infrastructure Testing

#### ✅ Connection Management Tests
- **CreateConnection_WithValidConnectionString_ReturnsSqlConnection**: Tests connection creation
- **AllMethods_EnsureConnectionIsDisposed**: Verifies proper connection disposal using the `await using` pattern

#### ✅ Cross-Cutting Concerns
- Tests for parameter validation across all methods
- Tests for connection string validation
- Tests for stored procedure name validation

## Testing Patterns and Strategies

### 1. Test Doubles
- **Mocks**: Used for `SqlConnection`, `IDataReader`, and custom `IDapperWrapper` interface
- **Testable Classes**: `TestableDatabaseHandler` and `TestableDatabaseHandlerWithDapper` for dependency injection

### 2. Dependency Injection for Testing
- Created `IDapperWrapper` interface to abstract Dapper calls for easier mocking
- Allows testing business logic without hitting actual database

### 3. Parametric Testing
- Uses `[Theory]` and `[InlineData]` for testing multiple input scenarios
- Covers edge cases like empty strings, null values, and whitespace

### 4. Assertion Strategies
- **FluentAssertions**: Provides readable and maintainable assertions
- **Verification**: Uses Moq's `Verify()` to ensure correct method calls
- **Exception Testing**: Uses xUnit's `Assert.ThrowsAsync()` for exception scenarios

## Test Data Management

### Test Models
- **TestModel**: Simple POCO for generic method testing
- **TestEntity**: More complex model for integration testing

### Test Constants
- Consistent connection strings and stored procedure names across tests
- Centralized test data creation for maintainability

## Integration Test Strategy

### Database Setup
- Automatic test database and schema creation
- Stored procedure creation for testing
- Test data seeding
- Cleanup handling

### Test Isolation
- Each test can run independently
- Database state is managed per test class
- Tests are marked with `[Skip]` attribute for environments without LocalDB

## Code Coverage Metrics

The test suite covers:
- **100% of public methods** in the DatabaseHandler class
- **All error paths** and exception scenarios
- **Parameter validation** for all methods
- **Connection management** and disposal
- **Generic type handling** for all applicable methods

## Running the Tests

### Unit Tests Only
```bash
dotnet test --filter "Category!=Integration"
```

### All Tests (including Integration)
```bash
dotnet test
```

### With Coverage Report
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Best Practices Demonstrated

1. **Separation of Concerns**: Unit tests vs Integration tests
2. **Mocking Strategy**: Proper use of mocks without over-mocking
3. **Test Naming**: Clear, descriptive test names following Given-When-Then pattern
4. **Arrange-Act-Assert**: Consistent test structure
5. **Exception Testing**: Proper testing of error conditions
6. **Resource Management**: Testing of disposable resources
7. **Generic Testing**: Testing generic methods with concrete types

## Future Enhancements

### Potential Additional Tests
- Performance testing for large datasets
- Concurrency testing for multiple simultaneous connections
- Memory leak testing for connection disposal
- Database timeout scenarios
- Connection pool exhaustion scenarios

### Test Infrastructure Improvements
- Docker-based test database for better isolation
- Test data builders for complex scenarios
- Custom assertions for database-specific validations
- Automated test coverage reporting in CI/CD pipeline

This comprehensive test suite ensures the DatabaseHandler class is robust, reliable, and maintains high quality standards across all supported scenarios.