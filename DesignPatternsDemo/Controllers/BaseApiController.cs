using Microsoft.AspNetCore.Mvc;

namespace DesignPatternsDemo.Controllers;

/// <summary>
/// Shared base controller providing consistent route prefix and API behavior conventions
/// for all controllers in this application.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
}
