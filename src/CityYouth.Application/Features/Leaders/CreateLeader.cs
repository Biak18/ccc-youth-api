using CityYouth.Application.Abstractions;
using CityYouth.Domain.Entities;
using FluentValidation;
using MediatR;

namespace CityYouth.Application.Features.Leaders;

public sealed record CreateLeaderCommand(
    Guid? UserId,
    string Name,
    string? RoleTitle,
    string? PhotoUrl,
    string? Bio,
    int SortOrder = 0,
    bool IsVisible = true) : IRequest<LeaderDto>;

public sealed class CreateLeaderCommandValidator : AbstractValidator<CreateLeaderCommand>
{
    public CreateLeaderCommandValidator()
    {
        _ = RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        _ = RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateLeaderCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreateLeaderCommand, LeaderDto>
{
    public async Task<LeaderDto> Handle(CreateLeaderCommand request, CancellationToken cancellationToken)
    {
        var leader = new YouthLeader
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name.Trim(),
            RoleTitle = request.RoleTitle,
            PhotoUrl = request.PhotoUrl,
            Bio = request.Bio,
            SortOrder = request.SortOrder,
            IsVisible = request.IsVisible,
        };

        _ = context.YouthLeaders.Add(leader);
        _ = await context.SaveChangesAsync(cancellationToken);

        return new LeaderDto(
            leader.Id, leader.UserId, leader.Name, leader.RoleTitle,
            leader.PhotoUrl, leader.Bio, leader.SortOrder, leader.IsVisible);
    }
}
