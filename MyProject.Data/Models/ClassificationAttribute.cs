namespace MyProject.Data.Models;

public class ClassificationAttribute
{
    public int ClassificationAttributeId { get; set; }

    public int ClassificationId { get; set; }

    public string AttributeName { get; set; } = string.Empty;
}
