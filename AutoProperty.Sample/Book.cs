namespace AutoProperty.Sample;


public interface IHasId
{
    public int Id { get; set; }
}

public interface IHasTitle
{
    public string Title { get; set; }
}

public interface IHasActiveFlag
{
    public bool IsActive { get; set; }
}

public interface IAuditMetadata
{
    DateTimeOffset LastUpdated { get; set; }

    bool IsEditable { get; }
}

public partial class Book : IHasId, IHasTitle, IHasActiveFlag, IAuditMetadata
{
    public required string Title { get; set; }

    public required string Author { get; set; }
}

public partial class Author : IHasId, IHasActiveFlag, IAuditMetadata
{
    public required string Name { get; set; }
}
