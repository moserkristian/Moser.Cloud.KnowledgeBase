using Moser.Enterprise.Blueprint.People.Application;

using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moser.Enterprise.Blueprint.People.API.Controllers.V1;

[ApiController]
[Route("api/v1/employees")]
public sealed class EmployeesController : ControllerBase
{
    private readonly IPeopleDirectory _directory;

    public EmployeesController(IPeopleDirectory directory)
    {
        _directory = directory;
    }

    [HttpGet]
    public async Task<ActionResult<EmployeeDto[]>> List(CancellationToken cancellationToken)
    {
        var people = await _directory.ListAsync(cancellationToken).ConfigureAwait(false);
        return Ok(people);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmployeeDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var person = await _directory.GetAsync(id, cancellationToken).ConfigureAwait(false);
        return person is null ? NotFound() : Ok(person);
    }
}
