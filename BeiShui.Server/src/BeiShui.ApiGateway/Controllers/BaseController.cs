using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace BeiShui.ApiGateway.Controllers;

/// <summary>
/// 基类控制器：提供通用方法消除重复代码
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// 从 JWT Claims 中提取用户 ID
    /// </summary>
    protected long GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && long.TryParse(claim.Value, out var id) ? id : 0;
    }

    /// <summary>
    /// 从 JWT Claims 中提取用户名
    /// </summary>
    protected string GetUsername()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? "";
    }
}
