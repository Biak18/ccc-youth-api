using MediatR;

namespace CityYouth.Application.Features.Categories.GetCategories;

public sealed record GetCategoriesQuery : IRequest<List<string>>;

public sealed class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, List<string>>
{
    public Task<List<string>> Handle(GetCategoriesQuery _, CancellationToken ct) =>
        Task.FromResult(new List<string>
        {
            "Fellowship", "Worship", "Bible Study", "Outreach", "Community Service",
            "Sports", "Conference", "Retreat", "Celebration",
        });
}
