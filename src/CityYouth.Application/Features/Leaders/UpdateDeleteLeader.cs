using CityYouth.Application.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Leaders;

public sealed record UpdateLeaderCommand(
    Guid Id,
    Guid? UserId,
    string Name,
    string? RoleTitle,
    string? PhotoUrl,
    string? Bio,
    int SortOrder,
    bool IsVisible) : IRequest<LeaderDto>;

public sealed class UpdateLeaderCommandValidator : AbstractValidator<UpdateLeaderCommand>
{
    public UpdateLeaderCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();
        _ = RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        _ = RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateLeaderCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateLeaderCommand, LeaderDto>
{
    public async Task<LeaderDto> Handle(UpdateLeaderCommand request, CancellationToken cancellationToken)
    {
        var leader = await context.YouthLeaders.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Leader not found.");

        leader.UserId = request.UserId;
        leader.Name = request.Name.Trim();
        leader.RoleTitle = request.RoleTitle;
        leader.PhotoUrl = request.PhotoUrl;
        leader.Bio = request.Bio;
        leader.SortOrder = request.SortOrder;
        leader.IsVisible = request.IsVisible;

        _ = await context.SaveChangesAsync(cancellationToken);

        return new LeaderDto(
            leader.Id, leader.UserId, leader.Name, leader.RoleTitle,
            leader.PhotoUrl, leader.Bio, leader.SortOrder, leader.IsVisible);
    }
}

public sealed record DeleteLeaderCommand(Guid Id) : IRequest;

public sealed class DeleteLeaderCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteLeaderCommand>
{
    public async Task Handle(DeleteLeaderCommand request, CancellationToken cancellationToken)
    {
        var leader = await context.YouthLeaders.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Leader not found.");

        _ = context.YouthLeaders.Remove(leader);
        _ = await context.SaveChangesAsync(cancellationToken);
    }
}
