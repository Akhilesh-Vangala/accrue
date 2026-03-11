using LedgerFlow.Application.Abstractions;
using LedgerFlow.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerFlow.Api.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize(Roles = Roles.Controller)]
public sealed class JobsController(IJobScheduler scheduler) : ControllerBase
{
    [HttpGet]
    public IReadOnlyList<RecurringJobInfo> List() => scheduler.GetRecurringJobs();

    [HttpPost("{id}/trigger")]
    public ActionResult<TriggeredJobDto> Trigger(string id) =>
        Accepted(new TriggeredJobDto(id, scheduler.TriggerNow(id)));
}
