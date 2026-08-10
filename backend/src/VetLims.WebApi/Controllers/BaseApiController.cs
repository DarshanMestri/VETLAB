using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VetLims.WebApi.Controllers;

[ApiController]
[Authorize] // default: auth required; override with [AllowAnonymous] where needed
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase { }
