namespace KHDMA.Application.Interfaces.Services;

public interface IImageUrlResolver
{
    string? Resolve(string? path);
}
