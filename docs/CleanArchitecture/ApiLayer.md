+ Root
    + src
        + Microservice_1
            + Api
                + Controllers
                    + <AggregateRootName>sController.cs
                + DTOs
                    + <AggregateRootName>RequestDto.cs
                    + <AggregateRootName>ResponseDto.cs
                + Middleware
                    + ExceptionHandlerMiddleware.cs

## Api/Controllers/<AggregateRootName>sController.cs

```csharp
[ApiController]
[Route("api/[controller]")]
public class <AggregateRootName>sController : ControllerBase
{
    private readonly IMediator _mediator;

    public <AggregateRootName>sController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{<AggregateRootName>Id:guid}/{<ChildEntityName>Id:guid}", Name = nameof(<FeatureName>))]
    public async Task<IActionResult> <FeatureName>(
        [FromRoute] Guid <AggregateRootName>Id,
        [FromRoute] Guid <ChildEntityName>Id,
        [FromBody] <AggregateRootName>RequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new <FeatureName>Command(
            <AggregateRootName>Id,
            <ChildEntityName>Id,
            request.<ChildEntityName>s);

        var result = await _mediator.Send(command);

        var response = new <AggregateRootName>ResponseDto(
            result.<AggregateRootName>Id,
            result.<ChildEntityName>Id,
            result.<ChildEntityName>s);

        return Ok(response);
    }
}
```