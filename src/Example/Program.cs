using Orleans.Runtime;
using Example;
using Microsoft.OpenApi.Models;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleans((_, silo) => silo
    .UseLocalhostClustering()
    .AddMemoryGrainStorageAsDefault()
);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo { Title = "Example API with Orleans.Results for Orleans 9" }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.UseSwagger()
           .UseSwaggerUI(options => options.EnableTryItOutByDefault());
}

app.MapGet("minimalapis/users/{id}", async (IClusterClient client, int id)
 => await client.GetGrain<ITenant>("A").GetUser(id) switch
    {
        { IsSuccess: true                   } r => Results.Ok(r.Value),
        { ErrorNr: ErrorNr.UserNotFound } r => Results.NotFound(r.ErrorsText),
        {                                   } r => throw r.UnhandledErrorException()
    }
);

app.MapGet("minimalapis/usersataddress", async (IClusterClient client, string zip, string nr)
 => {
        var result = await client.GetGrain<ITenant>("A").GetUsersAtAddress(zip, nr);

        return result.TryAsValidationErrors(ErrorNr.ValidationError, out var validationErrors)
            ? Results.ValidationProblem(validationErrors)

            : result switch
            {
                { IsSuccess: true                       } r => Results.Ok(r.Value),
                { ErrorNr: ErrorNr.NoUsersAtAddress } r => Results.NotFound(r.ErrorsText),
                {                                       } r => throw r.UnhandledErrorException()
            };
    }
);

app.MapControllers();

app.Run();

interface ITenant : IGrainWithStringKey
{
    Task<Result<string>> GetUser(int id);
    Task<Result<ImmutableArray<int>>> GetUsersAtAddress(string zip, string nr);
}

sealed partial class Tenant([PersistentState("state")] IPersistentState<Tenant.State> state) : Grain, ITenant
{
    State S => state.State;

    public Task<Result<string>> GetUser(int id) => Task.FromResult<Result<string>>(
        id >= 0 && id < S.Users.Count ?
            S.Users[id] :
            Errors.UserNotFound(id)
    );

    public async Task<Result<ImmutableArray<int>>> GetUsersAtAddress(string zip, string nr)
    {
        Collection<Result.Error> errors = [];

        // First check for validation errors - don't perform the operation if there are any.
        if (!ZipRegex().IsMatch(zip)) errors.Add(Errors.InvalidZipCode(zip));
        if (!HouseNrRegex().IsMatch(nr)) errors.Add(Errors.InvalidHouseNr(nr));
        if (errors.Count != 0) return errors;

        // If there are no validation errors, perform the operation - this may return non-validation errors
        await Task.CompletedTask; // Simulate an operation
        if (!(int.TryParse(nr, out int number) && number % 2 == S.Users.Count % 2)) errors.Add(Errors.NoUsersAtAddress($"{zip} {nr}")); // Success for 50% of numbers that are int
        return errors.Count != 0 ? errors : ImmutableArray.Create(0);
    }

    [GenerateSerializer]
    internal sealed class State
    {
        [Id(0)] public List<string> Users { get; set; } = new(["John", "Vincent"]);
    }

    [GeneratedRegex("^\\d\\d\\d\\d[A-Z]{2}$")]
    private static partial Regex ZipRegex();

    [GeneratedRegex("^\\d+[a-z]?$")]
    private static partial Regex HouseNrRegex();
}

static class Errors
{
    public static Result.Error UserNotFound(int id) => new(ErrorNr.UserNotFound, $"User {id} not found");
    public static Result.Error InvalidZipCode(string zip) => new(ErrorNr.InvalidZipCode, $"Zip nr {zip} is not valid - must be 4 digits plus 2 capital letters");
    public static Result.Error InvalidHouseNr(string nr) => new(ErrorNr.InvalidHouseNr, $"House number {nr} is not valid - must be digit(s) plus optionally a lowercase letter a-z");
    public static Result.Error NoUsersAtAddress(string address) => new(ErrorNr.NoUsersAtAddress, $"No users found at address {address}");
}
