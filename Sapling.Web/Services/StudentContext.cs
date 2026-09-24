using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Sapling.Web.Data;

namespace Sapling.Web.Services;

/// <summary>Resolves the signed-in student from either an HTTP request (API, static SSR) or a Blazor circuit.</summary>
public sealed class StudentContext(
    SaplingDbContext db,
    IHttpContextAccessor http,
    IServiceProvider services,
    CareerCatalogueFile catalogue)
{
    private StudentProfile? _cached;

    public async Task<string?> GetUserIdAsync()
    {
        var principal = http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            return principal.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        if (services.GetService<AuthenticationStateProvider>() is { } provider)
        {
            var state = await provider.GetAuthenticationStateAsync();
            if (state.User.Identity?.IsAuthenticated == true)
            {
                return state.User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
        }

        return null;
    }

    public async Task<StudentProfile> GetProfileAsync(CancellationToken ct = default)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var userId = await GetUserIdAsync()
            ?? throw new UnauthorizedAccessException("No signed-in student.");

        _cached = await db.StudentProfiles
            .Include(p => p.User)
            .Include(p => p.Skills).ThenInclude(s => s.Skill)
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        if (_cached is null)
        {
            var defaultCode = catalogue.DefaultRoleFor(null).Onet;
            var defaultRole = await db.CareerRoles.FirstAsync(r => r.OnetCode == defaultCode, ct);
            _cached = new StudentProfile
            {
                UserId = userId,
                GraduationYear = DateTime.Today.Year + 1,
                TargetRoleId = defaultRole.Id,
            };
            db.StudentProfiles.Add(_cached);
            await db.SaveChangesAsync(ct);
            _cached.User = await db.Users.FirstAsync(u => u.Id == userId, ct);
        }

        return _cached;
    }
}
