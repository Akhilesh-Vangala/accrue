using Accrue.Application.Common;
using Accrue.Application.Vendors;
using Accrue.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Accrue.Api.Controllers;

[ApiController]
[Route("api/vendors")]
[Authorize]
public sealed class VendorsController(VendorService vendors) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<VendorDto>> List([FromQuery] VendorQuery query, CancellationToken ct) => vendors.ListAsync(query, ct);

    [HttpGet("categories")]
    public IReadOnlyList<string> Categories() => SaveVendorValidator.Categories;

    [HttpGet("{id:int}")]
    public Task<VendorDto> Get(int id, CancellationToken ct) => vendors.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = $"{Roles.Clerk},{Roles.Controller}")]
    public async Task<ActionResult<VendorDto>> Create(SaveVendorRequest request, CancellationToken ct)
    {
        var vendor = await vendors.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = vendor.Id }, vendor);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{Roles.Clerk},{Roles.Controller}")]
    public Task<VendorDto> Update(int id, SaveVendorRequest request, CancellationToken ct) => vendors.UpdateAsync(id, request, ct);

    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = Roles.Controller)]
    public Task<VendorDto> Activate(int id, CancellationToken ct) => vendors.SetActiveAsync(id, true, ct);

    [HttpPost("{id:int}/deactivate")]
    [Authorize(Roles = Roles.Controller)]
    public Task<VendorDto> Deactivate(int id, CancellationToken ct) => vendors.SetActiveAsync(id, false, ct);
}
