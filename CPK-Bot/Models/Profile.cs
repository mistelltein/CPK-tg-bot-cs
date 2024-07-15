using System.ComponentModel.DataAnnotations;

namespace CPK_Bot.Models;

public class Profile
{
    public long Id { get; set; }
    [MaxLength(100)]
    public string? Username { get; set; }

    public int Rating { get; set; } = 0;
    
    [MaxLength(100)]
    public string? Role { get; set; }
}