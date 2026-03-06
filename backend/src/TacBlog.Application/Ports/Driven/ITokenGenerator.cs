namespace TacBlog.Application.Ports.Driven;

public interface ITokenGenerator
{
    string Generate(string email);
}
