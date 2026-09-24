using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Users;

public sealed record ProfileDto(
    Guid Id,
    string? Email,
    string? DisplayName,
    string? AvatarUrl,
    string Role,
    DateTime CreatedAt);

public sealed record ListProfilesQuery(string? Search, int Page = 1, int PageSize = 20)
    : IRequest<Common.PagedResult<ProfileDto>>;

public sealed record UpdateProfileRoleCommand(Guid Id, string Role) : IRequest<ProfileDto>;

public sealed class UpdateProfileRoleCommandValidator : AbstractValidator<UpdateProfileRoleCommand>
{
    public UpdateProfileRoleCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();
        _ = RuleFor(x => x.Role).Must(r => UserRoleExtensions.TryParseApiString(r, out _)).WithMessage("Role must be admin or leader.");
    }
}

public sealed class ListProfilesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<ListProfilesQuery, Common.PagedResult<ProfileDto>>
{
    public async Task<Common.PagedResult<ProfileDto>> Handle(ListProfilesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = context.Profiles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(p =>
                (p.Email != null && p.Email.Contains(s)) ||
                (p.DisplayName != null && p.DisplayName.Contains(s)));
        }

        query = query.OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProfileDto(p.Id, p.Email, p.DisplayName, p.AvatarUrl, p.Role.ToApiString(), p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new Common.PagedResult<ProfileDto>(items, page, pageSize, total);
    }
}

public sealed class UpdateProfileRoleCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateProfileRoleCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(UpdateProfileRoleCommand request, CancellationToken cancellationToken)
    {
        var profile = await context.Profiles.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Profile not found.");

        if (!UserRoleExtensions.TryParseApiString(request.Role, out var role))
        {
            throw new InvalidOperationException("Role must be admin or leader.");
        }

        profile.Role = role;
        _ = await context.SaveChangesAsync(cancellationToken);

        return new ProfileDto(
            profile.Id, profile.Email, profile.DisplayName, profile.AvatarUrl, profile.Role.ToApiString(), profile.CreatedAt);
    }
}
