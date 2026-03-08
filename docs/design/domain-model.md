# Domain Model -- The Augmented Craftsman v1

**Approach**: DDD-Lite -- Value Objects + Lightweight Aggregate + Bounded Context Awareness
**Rules**: Object Calisthenics (wrap all primitives, no getters/setters, 50 lines/class, 2 args/method)
**Philosophy**: Make illegal states non-representable through the type system

---

## 1. Aggregate Design

### BlogPost (Aggregate Root)

The only aggregate in v1. Owns its tag associations and controls its lifecycle transitions.

**Invariants:**
- Title is required and non-empty (max 200 chars)
- Content is required and non-empty
- Slug is generated from title at creation time and is immutable thereafter
- Slug must be unique across all posts (enforced at repository level)
- Status transitions: Draft --> Published only. No reverse. No re-publish.
- Published date is set when status transitions to Published
- Featured image is optional (nullable)
- Tag associations are managed through the aggregate (add/remove/replace)

**Lifecycle:**

```
[Create] --> Draft --> [Publish] --> Published
                                       |
                           (no reverse transition)
```

### Tag (Independent Entity)

Tag is NOT nested under BlogPost. Tags are shared across posts and managed independently. The post-tag relationship is many-to-many, managed through the BlogPost aggregate when associating and through the Tag repository when deleting a tag (cascade removes associations).

**Invariants:**
- Name is required, non-empty, max 50 characters, unique
- Slug is generated from name and regenerates on rename
- Deleting a tag removes associations but never deletes posts

---

## 2. Value Objects

Every primitive is wrapped per Object Calisthenics Rule 3. Each Value Object encodes its domain rules in the constructor, making invalid states non-representable.

### PostId

Wraps a `Guid`. Identity of a BlogPost.

```csharp
public sealed class PostId : IEquatable<PostId>
{
    private readonly Guid _value;

    private PostId(Guid value) => _value = value;

    public static PostId Create() =>
        new(Guid.NewGuid());

    public static PostId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Post ID cannot be empty.")
            : new(value);

    public Guid ToGuid() => _value;

    public bool Equals(PostId? other) =>
        other is not null && _value == other._value;

    public override bool Equals(object? obj) =>
        obj is PostId other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value.ToString();

    public static implicit operator Guid(PostId id) => id._value;
}
```

### Title

Wraps a non-empty string, max 200 characters.

```csharp
public sealed class Title : IEquatable<Title>
{
    private const int MaxLength = 200;
    private readonly string _value;

    private Title(string value) => _value = value;

    public static Title From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Title is required");

        if (value.Length > MaxLength)
            throw new ArgumentException($"Title must be {MaxLength} characters or fewer");

        return new(value.Trim());
    }

    public bool Equals(Title? other) =>
        other is not null && _value == other._value;

    public override bool Equals(object? obj) =>
        obj is Title other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => _value;

    public static implicit operator string(Title title) => title._value;
}
```

### Slug

Wraps a URL-safe, lowercase, hyphenated string. Generated from a source string (title or tag name). Immutable after creation on BlogPost.

```csharp
public sealed class Slug : IEquatable<Slug>
{
    private readonly string _value;

    private Slug(string value) => _value = value;

    public static Slug FromTitle(Title title) =>
        new(Slugify(title.ToString()));

    public static Slug FromTagName(TagName name) =>
        new(Slugify(name.ToString()));

    public static Slug From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Slug cannot be empty.");

        return new(value);
    }

    private static string Slugify(string input)
    {
        // Algorithm: lowercase, replace non-alphanumeric with hyphens,
        // collapse multiple hyphens, trim leading/trailing hyphens
        // Implementation is the crafter's decision.
        // Contract: "TDD Is Not About Testing!" --> "tdd-is-not-about-testing"
        throw new NotImplementedException("Crafter implements during GREEN phase");
    }

    public bool Equals(Slug? other) =>
        other is not null
        && string.Equals(_value, other._value, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is Slug other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => _value;

    public static implicit operator string(Slug slug) => slug._value;
}
```

**Slug generation rules** (acceptance criteria for the crafter):
- Lowercase all characters
- Replace spaces and non-alphanumeric characters with hyphens
- Collapse consecutive hyphens into a single hyphen
- Trim leading and trailing hyphens
- Examples:
  - `"TDD Is Not About Testing!"` --> `"tdd-is-not-about-testing"`
  - `"Value Objects Are Not DTOs"` --> `"value-objects-are-not-dtos"`
  - `"The  Walking   Skeleton  Pattern"` --> `"the-walking-skeleton-pattern"`
  - `"SOLID Principles in Practice"` --> `"solid-principles-in-practice"`

### PostContent

Wraps a non-empty Markdown string.

```csharp
public sealed class PostContent : IEquatable<PostContent>
{
    private readonly string _value;

    private PostContent(string value) => _value = value;

    public static PostContent From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Content is required");

        return new(value);
    }

    public bool Equals(PostContent? other) =>
        other is not null
        && string.Equals(_value, other._value, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is PostContent other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => _value;

    public static implicit operator string(PostContent content) => content._value;
}
```

### PostStatus

Enum representing the post lifecycle state.

```csharp
public enum PostStatus
{
    Draft,
    Published
}
```

**Transition rule**: Draft --> Published only. Enforced by the BlogPost entity. No reverse transition. No method to go back to Draft.

### TagId

Wraps a `Guid`. Identity of a Tag.

```csharp
public sealed class TagId : IEquatable<TagId>
{
    private readonly Guid _value;

    private TagId(Guid value) => _value = value;

    public static TagId Create() =>
        new(Guid.NewGuid());

    public static TagId From(Guid value) =>
        value == Guid.Empty
            ? throw new ArgumentException("Tag ID cannot be empty.")
            : new(value);

    public Guid ToGuid() => _value;

    public bool Equals(TagId? other) =>
        other is not null && _value == other._value;

    public override bool Equals(object? obj) =>
        obj is TagId other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value.ToString();

    public static implicit operator Guid(TagId id) => id._value;
}
```

### TagName

Wraps a non-empty string, max 50 characters.

```csharp
public sealed class TagName : IEquatable<TagName>
{
    private const int MaxLength = 50;
    private readonly string _value;

    private TagName(string value) => _value = value;

    public static TagName From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tag name is required");

        if (value.Length > MaxLength)
            throw new ArgumentException("Tag name must be 50 characters or fewer");

        return new(value.Trim());
    }

    public bool Equals(TagName? other) =>
        other is not null
        && string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) =>
        obj is TagName other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode(StringComparison.OrdinalIgnoreCase);

    public override string ToString() => _value;

    public static implicit operator string(TagName name) => name._value;
}
```

**Note**: TagName equality is case-insensitive. `"TDD"` and `"tdd"` are considered the same tag name.

### TagSlug

Identical pattern to Slug but for tag context. Generated from TagName.

```csharp
public sealed class TagSlug : IEquatable<TagSlug>
{
    private readonly string _value;

    private TagSlug(string value) => _value = value;

    public static TagSlug FromTagName(TagName name) =>
        new(Slugify(name.ToString()));

    public static TagSlug From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tag slug cannot be empty.");

        return new(value);
    }

    // Slugify implementation same as Slug -- crafter may extract shared logic
    private static string Slugify(string input)
    {
        throw new NotImplementedException("Crafter implements during GREEN phase");
    }

    public bool Equals(TagSlug? other) =>
        other is not null
        && string.Equals(_value, other._value, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is TagSlug other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => _value;

    public static implicit operator string(TagSlug slug) => slug._value;
}
```

**Design decision for the crafter**: Slug and TagSlug share the same slugification logic. The crafter decides whether to extract a shared `SlugGenerator` or use a shared private static method. This is a REFACTOR decision, not an architecture decision.

### ImageUrl

Wraps a valid URI (ImageKit URL). Optional on BlogPost.

```csharp
public sealed class ImageUrl : IEquatable<ImageUrl>
{
    private readonly Uri _value;

    private ImageUrl(Uri value) => _value = value;

    public static ImageUrl From(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Image URL cannot be empty.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new ArgumentException("Image URL must be a valid absolute URI.");

        return new(uri);
    }

    public bool Equals(ImageUrl? other) =>
        other is not null && _value == other._value;

    public override bool Equals(object? obj) =>
        obj is ImageUrl other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value.ToString();

    public static implicit operator string(ImageUrl url) => url._value.ToString();
}
```

---

## 3. Entity Sketches

### BlogPost Entity

```csharp
public sealed class BlogPost
{
    // Two instance variables per Object Calisthenics Rule 8:
    // Option A: PostIdentity (id + slug) + PostBody (title + content + status + ...)
    // Option B: Relax to ~5 for pragmatism in a domain entity
    //
    // The crafter decides the exact decomposition during REFACTOR.
    // The key invariants that MUST be enforced:

    // Construction: always via factory method, never public constructor
    // Slug: set at creation, immutable thereafter
    // Status: Draft on creation, transitions to Published via Publish()
    // Tags: managed as a collection, add/remove/replace operations

    public static BlogPost Create(
        Title title,
        PostContent content,
        IClock clock)
    {
        // Generates slug from title
        // Sets status to Draft
        // Sets createdAt and updatedAt from clock
        // Returns new BlogPost
    }

    public void Publish(IClock clock)
    {
        // Guard: already Published --> throw
        // Set status to Published
        // Set publishedAt from clock
    }

    public void UpdateContent(Title title, PostContent content, IClock clock)
    {
        // Update title and content
        // Slug does NOT change
        // Update updatedAt from clock
    }

    public void SetFeaturedImage(ImageUrl imageUrl)
    {
        // Set featured image URL
    }

    public void RemoveFeaturedImage()
    {
        // Clear featured image URL to null
    }

    public void ReplaceTags(IReadOnlyCollection<Tag> tags)
    {
        // Replace all tag associations
    }
}
```

**Note on Object Calisthenics Rule 8 (max 2 instance variables)**: A domain entity with ID, title, slug, content, status, publishedAt, featuredImageUrl, tags, createdAt, updatedAt clearly exceeds 2 variables. The crafter should group related fields into intermediate Value Objects during REFACTOR (e.g., `PostIdentity` for id+slug, `PostTimestamps` for createdAt+updatedAt+publishedAt). This is a refactoring decision, not an architecture decision. Start with the simple version and extract when the Rule of Three signals it.

### Tag Entity

```csharp
public sealed class Tag
{
    // Construction via factory method
    // Slug regenerates on rename

    public static Tag Create(TagName name)
    {
        // Generate TagSlug from name
        // Set createdAt
        // Return new Tag
    }

    public void Rename(TagName newName)
    {
        // Update name
        // Regenerate slug from new name
    }
}
```

---

## 4. Driven Port Interfaces

### IBlogPostRepository

```csharp
public interface IBlogPostRepository
{
    Task Add(BlogPost post);
    Task<BlogPost?> FindBySlug(Slug slug);
    Task<BlogPost?> FindById(PostId id);
    Task<IReadOnlyCollection<BlogPost>> ListPublished(TagSlug? tagFilter = null);
    Task<IReadOnlyCollection<BlogPost>> ListAll();
    Task Update(BlogPost post);
    Task Delete(PostId id);
    Task<bool> SlugExists(Slug slug);
}
```

**Notes:**
- Methods use domain types (PostId, Slug), not primitives.
- `ListPublished` returns only Published posts sorted by publishedAt descending.
- `ListAll` returns all posts (Draft + Published) sorted by createdAt descending.
- No generic `IRepository<T>`. Methods have domain-meaningful names.

### ITagRepository

```csharp
public interface ITagRepository
{
    Task Add(Tag tag);
    Task<Tag?> FindById(TagId id);
    Task<IReadOnlyCollection<TagWithPostCount>> ListAllWithPostCounts();
    Task Update(Tag tag);
    Task Delete(TagId id);
    Task<bool> NameExists(TagName name);
}
```

**TagWithPostCount** is a read model (query result):

```csharp
public sealed record TagWithPostCount(
    TagId Id,
    TagName Name,
    TagSlug Slug,
    int PostCount);
```

### IImageStorage

```csharp
public interface IImageStorage
{
    Task<ImageUrl> Upload(Stream imageStream, string fileName);
}
```

### IPasswordHasher

```csharp
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hashedPassword);
}
```

### ITokenGenerator

```csharp
public interface ITokenGenerator
{
    string Generate(string email);
}
```

### IClock

```csharp
public interface IClock
{
    DateTime UtcNow();
}
```

**Rationale**: Wrapping `DateTime.UtcNow` in a port allows deterministic testing. The handler injects `IClock`, tests provide a fixed clock, production uses `SystemClock`.

---

## 5. Aggregate Design Rules (Vernon)

| Rule | Application |
|------|-------------|
| Protect invariants inside aggregate boundaries | BlogPost enforces: non-empty title, immutable slug, valid status transitions |
| Reference other aggregates by identity only | BlogPost references Tags by TagId (or Tag entity in collection, managed via join table) |
| Use eventual consistency across aggregates | Not needed in v1 (single DB, no async) |
| Design small aggregates | BlogPost is the only aggregate. Tag is an independent entity. |
| Expect to update one aggregate per transaction | Create/Update/Delete BlogPost is one transaction. Tag operations are separate transactions. |

---

## 6. Comparison with Original Tutorial Code

The original `blog/` codebase used anemic domain models with public getters/setters and no Value Objects:

| Aspect | Original (blog/) | New Design |
|--------|------------------|------------|
| BlogPost.Id | `Guid` (public setter) | `PostId` Value Object, private constructor |
| Heading/Title | `string` (no validation) | `Title` Value Object, max 200, non-empty |
| UrlHandle/Slug | `string` (mutable) | `Slug` Value Object, immutable after creation |
| Content | `string` (no validation) | `PostContent` Value Object, non-empty |
| Status | `bool Visible` | `PostStatus` enum with transition rules |
| Tags | `ICollection<Tag>` with public setter | Encapsulated collection, managed via Tell Don't Ask |
| Repository | Generic CRUD (`GetAll`, `GetById`, `Add`, `Update`, `Delete`) | Domain-meaningful methods (`FindBySlug`, `ListPublished`, `SlugExists`) |

Every feature must be rebuilt test-first. No code is copied from `blog/`.
