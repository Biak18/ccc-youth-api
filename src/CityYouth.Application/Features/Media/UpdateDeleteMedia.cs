using CityYouth.Application.Abstractions;
using CityYouth.Application.Common;
using CityYouth.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CityYouth.Application.Features.Media;

public sealed record UpdateMediaCommand(
    Guid Id,
    string? ThumbnailUrl,
    string? Title,
    string? Description,
    int SortOrder) : IRequest<MediaDto>;

public sealed class UpdateMediaCommandValidator : AbstractValidator<UpdateMediaCommand>
{
    public UpdateMediaCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();
        _ = RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateMediaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<UpdateMediaCommand, MediaDto>
{
    public async Task<MediaDto> Handle(UpdateMediaCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var media = await context.Media.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Media not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, media.UploadedBy, isAdmin);

        media.ThumbnailUrl = request.ThumbnailUrl;
        media.Title = request.Title;
        media.Description = request.Description;
        media.SortOrder = request.SortOrder;

        _ = await context.SaveChangesAsync(cancellationToken);

        return new MediaDto(
            media.Id, media.ActivityId, media.Type, media.Source, media.Url,
            media.ThumbnailUrl, media.Title, media.Description, media.SortOrder,
            media.UploadedBy, media.CreatedAt);
    }
}

public sealed record DeleteMediaCommand(Guid Id) : IRequest;

public sealed class DeleteMediaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<DeleteMediaCommand>
{
    public async Task Handle(DeleteMediaCommand request, CancellationToken cancellationToken)
    {
        var userId = AuthorizationHelper.RequireUser(currentUser.UserId);
        var media = await context.Media.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Media not found.");

        var isAdmin = await AuthorizationHelper.IsAdminAsync(context, userId, cancellationToken);
        AuthorizationHelper.RequireOwnerOrAdmin(userId, media.UploadedBy, isAdmin);

        _ = context.Media.Remove(media);
        _ = await context.SaveChangesAsync(cancellationToken);
    }
}
