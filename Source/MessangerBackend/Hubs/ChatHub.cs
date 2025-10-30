using Microsoft.AspNetCore.SignalR;
using MessangerBackend.Data;
using System.Security.Claims;

namespace MessangerBackend.Hubs;

public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(int recipientId, string messageBody)
    {
        var senderId = GetUserId();
        if (senderId == null) return;

        var message = new Models.Message
        {
            SenderId = senderId.Value,
            RecipientId = recipientId,
            Body = messageBody,
            SendTime = DateTime.UtcNow,
            IsRead = false,
            IsDelivered = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        await Clients.Group($"User_{recipientId}").SendAsync("ReceiveMessage", new
        {
            Id = message.Id,
            SenderId = message.SenderId,
            RecipientId = message.RecipientId,
            Body = message.Body,
            SendTime = message.SendTime,
            IsRead = message.IsRead
        });

        await Clients.Caller.SendAsync("MessageSent", new { MessageId = message.Id });
    }

    public async Task MarkAsRead(int messageId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message != null)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();

            await Clients.Group($"User_{message.SenderId}").SendAsync("MessageRead", messageId);
        }
    }

    public async Task Typing(int recipientId, bool isTyping)
    {
        var senderId = GetUserId();
        if (senderId != null)
        {
            await Clients.Group($"User_{recipientId}").SendAsync("UserTyping", new
            {
                UserId = senderId,
                IsTyping = isTyping
            });
        }
    }

    private int? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userIdClaim != null ? int.Parse(userIdClaim) : null;
    }
}