using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DevLife.Backend.Domain;

public class CodeSnippet
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public int UserId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string CorrectSnippet { get; set; } = string.Empty;
    public string BuggySnippet { get; set; } = string.Empty;

    public string CorrectOption { get; set; } = string.Empty;

    public bool HasGuessed { get; set; } = false;


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}