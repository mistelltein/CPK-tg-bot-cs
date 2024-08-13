using System.ComponentModel.DataAnnotations;

namespace CPK_Bot.Entities;

public class Profile
{
    public long Id { get; set; }
    [MaxLength(100)]
    public string? Username { get; set; }
    
    [MaxLength(100)]
    public string? FirstName { get; set; }
    
    public int Rating { get; set; } = 30;

    [MaxLength(100)] public string? Role { get; set; } = "Newbie-Developer";
}