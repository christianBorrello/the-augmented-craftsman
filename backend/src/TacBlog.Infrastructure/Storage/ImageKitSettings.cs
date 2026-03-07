namespace TacBlog.Infrastructure.Storage;

public sealed record ImageKitSettings(
    string UrlEndpoint,
    string PublicKey,
    string PrivateKey);
