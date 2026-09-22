using CityYouth.Application.Abstractions;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Leaders.ManageLeaders;

public sealed record LeaderInput(
    string Name,
    string? RoleTitle,
    string? PhotoUrl,
    string? Bio,
    int SortOrder,
    bool IsVisible);

public sealed record CreateLeaderCommand(LeaderInput Input) : IRequest<YouthLeader>;
public sealed record UpdateLeaderCommand(Guid Id, LeaderInput Input) : IRequest<YouthLeader>;
public sealed record DeleteLeaderCommand(Guid Id) : IRequest;

public sealed class LeaderInputValidator : AbstractValidator<LeaderInput>
{
    public LeaderInputValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public sealed class ManageLeadersHandler(ISupabaseGateway db)
    : IRequestHandler<CreateLeaderCommand, YouthLeader>,
      IRequestHandler<UpdateLeaderCommand, YouthLeader>,
      IRequestHandler<DeleteLeaderCommand>
{
    public async Task<YouthLeader> Handle(CreateLeaderCommand c, CancellationToken ct)
    {
        var item = new YouthLeader
        {
            Name = c.Input.Name.Trim(),
            RoleTitle = c.Input.RoleTitle,
            PhotoUrl = c.Input.PhotoUrl,
            Bio = c.Input.Bio,
            SortOrder = c.Input.SortOrder,
            IsVisible = c.Input.IsVisible,
        };
        return await db.InsertAsync<YouthLeader>("youth_leaders", item, ct);
    }

    public async Task<YouthLeader> Handle(UpdateLeaderCommand c, CancellationToken ct)
    {
        var item = await FindAsync(c.Id, ct);
        item.Name = c.Input.Name.Trim();
        item.RoleTitle = c.Input.RoleTitle;
        item.PhotoUrl = c.Input.PhotoUrl;
        item.Bio = c.Input.Bio;
        item.SortOrder = c.Input.SortOrder;
        item.IsVisible = c.Input.IsVisible;
        await db.UpdateAsync("youth_leaders", "id", c.Id.ToString(), item, ct);
        return item;
    }

    public async Task Handle(DeleteLeaderCommand c, CancellationToken ct)
    {
        await FindAsync(c.Id, ct);
        await db.DeleteAsync("youth_leaders", "id", c.Id.ToString(), ct);
    }

    private async Task<YouthLeader> FindAsync(Guid id, CancellationToken ct) =>
        await db.SingleAsync<YouthLeader>("youth_leaders", $"select=*&id=eq.{id}", ct: ct)
        ?? throw new CityYouth.Domain.Common.NotFoundException($"Leader '{id}' not found.");
}
