using System.Collections.Immutable;
using Microsoft.AspNetCore.Mvc;

namespace Example;

[ApiController]
public class Controller(IClusterClient client) : ControllerBase
{
    [HttpGet("mvc/users/{id}")]
    public async Task<ActionResult<string>> GetUser(int id)
     => await client.GetGrain<ITenant>("A").GetUser(id) switch
        {
            { IsSuccess: true                   } r => Ok(r.Value),
            { ErrorNr: ErrorNr.UserNotFound } r => NotFound(r.ErrorsText),
            {                                   } r => throw r.UnhandledErrorException()
        };

    [HttpGet("mvc/usersataddress")]
    public async Task<ActionResult<ImmutableArray<int>>> GetUsersAtAddress(string zip, string nr)
    {
        var result = await client.GetGrain<ITenant>("A").GetUsersAtAddress(zip, nr);

        return result.TryAsValidationErrors(ErrorNr.ValidationError, out var validationErrors)
            ? ValidationProblem(new ValidationProblemDetails(validationErrors))

            : result switch
            {
                { IsSuccess: true                       } r => Ok(r.Value),
                { ErrorNr: ErrorNr.NoUsersAtAddress } r => NotFound(r.ErrorsText),
                {                                       } r => throw r.UnhandledErrorException()
            };
    }
}
