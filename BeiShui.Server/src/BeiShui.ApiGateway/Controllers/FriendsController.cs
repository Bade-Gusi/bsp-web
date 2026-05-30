using BeiShui.ApiGateway.Data;
using BeiShui.ApiGateway.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeiShui.ApiGateway.Controllers;

[Route("api/friends")]
[Authorize]
public class FriendsController : BaseController
{
    private readonly AppDbContext _db;

    public FriendsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetFriends()
    {
        var userId = GetUserId();
        var friends = await _db.Friends
            .Include(f => f.FriendUser)
            .Where(f => f.UserId == userId && f.Status == 1)
            .Select(f => new
            {
                f.FriendUser!.Id,
                f.FriendUser.Username,
                f.FriendUser.Nickname,
                f.FriendUser.MMR,
                f.FriendUser.Status,
                f.FriendUser.LastLoginAt
            })
            .ToListAsync();

        return Ok(friends);
    }

    [HttpPost("request/{friendId}")]
    public async Task<IActionResult> RequestFriend(long friendId)
    {
        var userId = GetUserId();
        if (userId == friendId) return BadRequest(new { Error = "不能加自己为好友" });

        var exists = await _db.Friends.AnyAsync(f =>
            (f.UserId == userId && f.FriendId == friendId) ||
            (f.UserId == friendId && f.FriendId == userId));
        if (exists) return BadRequest(new { Error = "已经是好友或已有请求" });

        _db.Friends.Add(new Friend { UserId = userId, FriendId = friendId, Status = 0 });
        await _db.SaveChangesAsync();

        return Ok(new { Message = "好友请求已发送" });
    }

    [HttpPost("accept/{friendId}")]
    public async Task<IActionResult> AcceptFriend(long friendId)
    {
        var userId = GetUserId();
        var request = await _db.Friends.FirstOrDefaultAsync(f =>
            f.UserId == friendId && f.FriendId == userId && f.Status == 0);
        if (request == null) return NotFound(new { Error = "请求不存在" });

        request.Status = 1;
        request.UpdatedAt = DateTime.UtcNow;

        // 建立双向关系
        _db.Friends.Add(new Friend { UserId = userId, FriendId = friendId, Status = 1 });
        await _db.SaveChangesAsync();

        return Ok(new { Message = "已接受好友请求" });
    }

    [HttpDelete("{friendId}")]
    public async Task<IActionResult> DeleteFriend(long friendId)
    {
        var userId = GetUserId();
        var friendships = await _db.Friends.Where(f =>
            (f.UserId == userId && f.FriendId == friendId) ||
            (f.UserId == friendId && f.FriendId == userId)).ToListAsync();

        _db.Friends.RemoveRange(friendships);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "已删除好友" });
    }

}
