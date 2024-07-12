using System.ComponentModel.DataAnnotations;

namespace CPK_Bot.Models;

public class BackendQuestion
{
    public int QuestionId { get; set; }

    [MaxLength(1000)]
    public string? Question { get; set; }
    
    [MaxLength(10000)]
    public string? Answer { get; set; }
}