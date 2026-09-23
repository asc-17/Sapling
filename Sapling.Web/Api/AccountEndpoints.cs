using Sapling.Shared.Contracts;
using Sapling.Web.Services;

namespace Sapling.Web.Api;

/// <summary>Email-verified sign-up and password reset for the app. The web pages call <see cref="AccountFlowService"/> directly.</summary>
public static class AccountEndpoints
{
    // Identity's own versions skip email verification, so they are switched off in favour of the routes below.
    private static readonly HashSet<string> RetiredIdentityRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/identity/register",
        "/api/identity/forgotPassword",
        "/api/identity/resetPassword",
    };

    public static TBuilder RetireUnverifiedIdentityRoutes<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter((context, next) =>
            RetiredIdentityRoutes.Contains(context.HttpContext.Request.Path.Value ?? "")
                ? ValueTask.FromResult<object?>(Results.Json(
                    new AccountError("This endpoint is retired. Use /api/account instead."), statusCode: StatusCodes.Status410Gone))
                : next(context));

    public static void MapAccountApi(this IEndpointRouteBuilder app)
    {
        var account = app.MapGroup("/api/account").AllowAnonymous();

        account.MapPost("/{purpose}/code", async (string purpose, SendCodeRequest request, AccountFlowService flow, CancellationToken ct) =>
        {
            if (!CodePurposes.IsKnown(purpose))
            {
                return Results.NotFound(new AccountError("Unknown purpose."));
            }

            var result = purpose == CodePurposes.Register
                ? await flow.SendRegistrationCodeAsync(request.Email, ct)
                : await flow.SendPasswordResetCodeAsync(request.Email, ct);

            return ToResult(result);
        });

        account.MapPost("/{purpose}/verify", (string purpose, VerifyCodeRequest request, AccountFlowService flow) =>
        {
            if (!CodePurposes.IsKnown(purpose))
            {
                return Results.NotFound(new AccountError("Unknown purpose."));
            }

            var result = flow.VerifyCode(purpose, request.Email, request.Code);
            return result.Succeeded ? Results.Ok(new VerifyCodeResponse(result.Ticket!)) : ToResult(result);
        });

        account.MapPost("/register", async (CompleteRegistrationRequest request, AccountFlowService flow) =>
            ToResult((await flow.CompleteRegistrationAsync(request.Email, request.Ticket, request.Password, request.FullName)).Result));

        account.MapPost("/reset-password", async (ResetPasswordRequest request, AccountFlowService flow) =>
            ToResult((await flow.ResetPasswordAsync(request.Email, request.Ticket, request.Password)).Result));
    }

    // Errors always carry a body, which also keeps the status-code pages middleware from swapping in an HTML page.
    private static IResult ToResult(AccountFlowResult result) => result.Succeeded
        ? Results.NoContent()
        : Results.BadRequest(new AccountError(result.Error!, result.Restart));
}
