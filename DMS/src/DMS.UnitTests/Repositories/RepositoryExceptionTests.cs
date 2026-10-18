using DMS.BLL.Exceptions;
using DMS.DAL;
using DMS.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;

namespace DMS.UnitTests.Repositories;

public class RepositoryExceptionTests
{
    public static IEnumerable<object[]> FailureCases()
    {
        foreach (var repository in new[]
                 {
                     "documents", "collections", "tags"
                 })
        {
            yield return new object[]
            {
                repository, PostgresErrorCodes.UniqueViolation, true
            };

            yield return new object[]
            {
                repository, PostgresErrorCodes.ForeignKeyViolation, true
            };

            yield return new object[]
            {
                repository, PostgresErrorCodes.InsufficientPrivilege, false
            };
        }
    }

    [Theory]
    [MemberData(nameof(FailureCases))]
    public async Task SaveChanges_ClassifiesDatabaseFailure(
        string repository,
        string sqlState,
        bool isConflict)
    {
        var postgresError = new PostgresException(
            "Database rejected the write.",
            "ERROR",
            "ERROR",
            sqlState);

        var databaseError = new DbUpdateException(
            "Saving changes failed.",
            postgresError);

        var context = new Mock<DmsDbContext>(
            new DbContextOptionsBuilder<DmsDbContext>().Options);

        context.Setup(db => db.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(databaseError);

        await using var dbContext = context.Object;

        Func<Task> saveChanges = repository switch
        {
            "documents" => () =>
                new DocumentRepository(dbContext).SaveChangesAsync(),

            "collections" => () =>
                new CollectionRepository(dbContext).SaveChangesAsync(),

            "tags" => () =>
                new TagRepository(dbContext).SaveChangesAsync(),

            _ => throw new ArgumentException(
                "Unknown repository.", nameof(repository))
        };

        if (isConflict)
        {
            var exception =
                await Assert.ThrowsAsync<ConflictException>(saveChanges);

            Assert.Same(databaseError, exception.InnerException);
        }
        else
        {
            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(
                    saveChanges);

            Assert.Same(databaseError, exception.InnerException);
        }
    }
}