namespace Accrue.Api.Controllers;

public sealed record TriggeredJobDto(string RecurringJobId, string BackgroundJobId);
