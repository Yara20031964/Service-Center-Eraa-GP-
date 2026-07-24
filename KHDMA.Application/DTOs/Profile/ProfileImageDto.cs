namespace KHDMA.Application.DTOs.Profile;

/// <summary>
/// One stored certificate or portfolio image.
/// </summary>
/// <remarks>
/// The id is not optional detail: the delete endpoints take a Guid, so a bare
/// list of URLs left the client with no way to remove anything it had uploaded.
/// </remarks>
public class ProfileImageDto
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
