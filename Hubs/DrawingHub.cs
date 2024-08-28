using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

public class DrawingHub : Hub
{
    // Dictionary to hold room names and the list of players in each room
    private static ConcurrentDictionary<string, Room> Rooms = new ConcurrentDictionary<string, Room>();

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var room = Rooms.Values.FirstOrDefault(r => r.Players.ContainsKey(Context.ConnectionId));
        if (room != null)
        {
            room.Players.Remove(Context.ConnectionId);
            await Clients.Group(room.Name).SendAsync("UpdatePlayerList", room.Players.Values.ToList());
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Method to create a room
    public async Task CreateRoom(string roomName, string playerName)
    {
        var room = new Room(roomName);
        Rooms[roomName] = room;

        await JoinRoom(roomName, playerName);
    }

    // Method to join a room
    public async Task JoinRoom(string roomName, string playerName)
    {
        if (Rooms.TryGetValue(roomName, out Room room))
        {
            var playerId = "Player" + new Random().Next(1000, 9999).ToString();
            room.Players[Context.ConnectionId] = new Player { Id = playerId, Name = playerName, Score = 0 };

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            await Clients.Group(roomName).SendAsync("UpdatePlayerList", room.Players.Values.ToList());
        }
    }

    // Method to send a drawing to others in the same room
    public async Task SendDrawing(string roomName, string drawingData)
    {
        await Clients.OthersInGroup(roomName).SendAsync("ReceiveDrawing", drawingData);
    }

    // Method to send a guess and check if it's correct
    public async Task SendGuess(string roomName, string playerName, string guess)
    {
        if (Rooms.TryGetValue(roomName, out Room room))
        {
            var correctWord = room.CurrentWord; // Assume word is set
            if (guess.Equals(correctWord, StringComparison.OrdinalIgnoreCase))
            {
                room.Players[Context.ConnectionId].Score += 10; // Increment score
                await Clients.Group(roomName).SendAsync("ReceiveMessage", $"{playerName} guessed the word correctly!");
                await Clients.Group(roomName).SendAsync("UpdatePlayerList", room.Players.Values.ToList());
            }
            else
            {
                await Clients.Group(roomName).SendAsync("ReceiveGuess", playerName, guess);
            }
        }
    }
}

// Room class to hold room data
public class Room
{
    public string Name { get; }
    public Dictionary<string, Player> Players { get; } = new Dictionary<string, Player>();
    public string CurrentWord { get; set; } = "cat"; // Placeholder for the current word

    public Room(string name)
    {
        Name = name;
    }
}

// Player class to hold player data
public class Player
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Score { get; set; }
}
