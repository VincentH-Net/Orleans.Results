using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Example;

namespace Orleans.Results.Tests.UnitTests;

[Collection(TestCluster.Name)]
public class ResultWithoutValueTests(ClusterFixture fixture)
{
    readonly TestingHost.TestCluster cluster = fixture.Cluster;

    ITenant Tenant => cluster.GrainFactory.GetGrain<ITenant>("tenant-a");

    [Fact]
    public async Task GetResult_OnSuccess_ReturnsSuccessResult()
    {
        var result = await Tenant.UpdateUser(1, "VincentH.NET");
        Assert.True(result.IsSuccess);
        Assert.Equal(nameof(Result.Ok), result.ToString());
    }

    [Fact]
    public async Task GetError_OnFailureWithSingleError_ReturnsSingleError()
    {
        var result = await Tenant.UpdateUser(2, "Johnny");

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);

        Assert.Equal(ErrorNr.UserNotFound, error.Nr);
        Assert.Equal("User 2 not found", error.Message);

        Assert.Equal(ErrorNr.UserNotFound, result.ErrorNr);
        string expectedErrorsText = "Error { Nr = UserNotFound, Message = User 2 not found }";
        Assert.Equal(expectedErrorsText, result.ErrorsText);
        Assert.Equal(expectedErrorsText, result.ToString());
    }

    [Fact]
    public void NewResult_WithImmutableArrayOfErrors_CreatesResultWithErrors()
    {
        var errors = ImmutableArray.CreateRange(new Result.Error[] { ErrorNr.UserNotFound, ErrorNr.NoUsersAtAddress });
        var result = new Result(errors);
        Assert.Equal(errors, result.Errors);
    }

    [Fact]
    public void NewResult_WithIEnumerableOfErrors_CreatesResultWithErrors()
    {
        var errors = new Result.Error[] { ErrorNr.UserNotFound }.AsEnumerable();
        var result = new Result(errors);
        Assert.Equal(errors, result.Errors);
    }


    [Fact]
    public void ImplicitConversion_OfErrorToResult_GivesResultWithError()
    {
        Result.Error assignedError = new(ErrorNr.UserNotFound, "Error message");
        Result result = assignedError;

        var errorInResult = Assert.Single(result.Errors);
        Assert.Equal(assignedError, errorInResult);
    }

    [Fact]
    public void ImplicitConversion_OfErrorCodeToResult_GivesResultWithErrorCode()
    {
        Result result = ErrorNr.UserNotFound;

        Assert.Equal(ErrorNr.UserNotFound, result.ErrorNr);
    }

    [Fact]
    public void ImplicitConversion_OfErrorCodeAndMessageToResult_GivesResultWithErrorCodeAndMessage()
    {
        Result result = (ErrorNr.UserNotFound, "Error Message");

        Assert.Equal(ErrorNr.UserNotFound, result.ErrorNr);
        Assert.Equal("Error Message", result.Errors.SingleOrDefault()?.Message);
    }

    [Fact]
    public void ImplicitConversion_OfListOfErrorsToResult_GivesResultWithErrors()
    {
        Result.Error error1 = new(ErrorNr.UserNotFound, "Error message 1");
        var error2 = error1 with { Nr = ErrorNr.NoUsersAtAddress, Message = "Error message 2" };

        Collection<Result.Error> errors = new([error1, error2]);
        Result result = errors;

        Assert.Equal(errors, result.Errors);
    }
}
