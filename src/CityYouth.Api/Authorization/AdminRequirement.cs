using CityYouth.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace CityYouth.Api.Authorization;

public sealed class AdminRequirement : IAuthorizationRequirement;
