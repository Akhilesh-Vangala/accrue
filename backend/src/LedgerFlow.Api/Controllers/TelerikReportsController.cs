#if TELERIK
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telerik.Reporting.Services;
using Telerik.Reporting.Services.AspNetCore;

namespace LedgerFlow.Api.Controllers;

/// <summary>Telerik Reporting REST service backing the Angular report viewer. Auth is the same-origin app cookie.</summary>
[Route("api/telerik-reports")]
[Authorize]
public sealed class TelerikReportsController(IReportServiceConfiguration configuration) : ReportsControllerBase(configuration);
#endif
