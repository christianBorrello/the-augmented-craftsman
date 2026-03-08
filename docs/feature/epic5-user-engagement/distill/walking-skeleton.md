# Walking Skeleton -- Epic 5: User Engagement

## What It Proves

The walking skeleton proves that a reader can like a post through the API and see the updated like count. This is the simplest possible engagement vertical slice that delivers observable user value.

**User goal answered**: "Can a reader express appreciation for a post and see that others have too?"

**Stakeholder demo**: Given a published post, a visitor likes it via the API and the count changes from 0 to 1. A second request confirms the count persists. This is demo-able without any frontend.

---

## What It Exercises

```
Walking Skeleton vertical slice:

  API (driving adapter)
    LikeEndpoints
      POST /api/posts/{slug}/likes
      GET /api/posts/{slug}/likes/count
        |
  Application (use cases)
    LikePost (records a like, validates post exists)
    GetLikeCount (returns like count)
      |
  Domain (entities + value objects)
    Like entity (PostSlug, VisitorId, CreatedAtUtc)
    VisitorId value object (validated UUID)
    Slug value object (existing, from Content context)
      |
  Infrastructure (driven adapter)
    EfLikeRepository -> PostgreSQL `likes` table
      |
  Database
    likes table (post_slug, visitor_id, created_at_utc)
    FK to blog_posts(slug)
```

**Components NOT exercised by the skeleton** (covered by focused scenarios later):
- Unlike flow
- Like deduplication (idempotency)
- CheckIfLiked endpoint
- OAuth / sessions
- Comments
- Moderation
- Share / OG tags

---

## Scenarios

### Scenario 1: Reader likes a post and the like count increments

```gherkin
Given "TDD Is Not About Testing" has 0 likes
And a visitor has not previously liked "TDD Is Not About Testing"
When the visitor likes "TDD Is Not About Testing"
Then the like is recorded successfully
And the like count for "TDD Is Not About Testing" is 1
```

**Why this scenario**: It proves the full write path works -- request enters through the API, LikePost use case validates the slug, creates a Like entity, persists it via EfLikeRepository, and returns success with the updated count.

### Scenario 2: Reader checks like count for a post

```gherkin
Given "TDD Is Not About Testing" has been liked by 3 visitors
When a visitor requests the like count for "TDD Is Not About Testing"
Then the like count for "TDD Is Not About Testing" is 3
```

**Why this scenario**: It proves the read path works -- GetLikeCount use case queries the repository and returns the correct aggregate count. It also validates that test data setup works for multiple likes.

---

## Implementation Order

1. **Create domain entities**: Like entity, VisitorId value object, CommentId value object (with TDD inner loop)
2. **Create driven port**: ILikeRepository interface
3. **Create use cases**: LikePost, GetLikeCount (with TDD inner loop, repository stubbed)
4. **Create driven adapter**: EfLikeRepository + LikeConfiguration + migration
5. **Create driving adapter**: LikeEndpoints (POST likes, GET likes/count)
6. **Run walking skeleton**: Both scenarios should pass green

---

## Test Infrastructure Requirements

The walking skeleton requires:
- **Testcontainers PostgreSQL** (already configured in TacBlogWebApplicationFactory)
- **Database migration** adding the `likes` table (EF Core migration)
- **A published post** in the Given step (reuses existing PostApiDriver for test data setup)
- **New LikeApiDriver** for making like-related API calls
- **Extended TestHooks** to clean the `likes` table between scenarios

No OAuth mocking needed. No reader sessions. No comment infrastructure. The skeleton is intentionally minimal.

---

## Definition of Done for Walking Skeleton

- [ ] Both walking skeleton scenarios pass (no @skip tag)
- [ ] LikeEndpoints registered in API startup
- [ ] likes table created via EF Core migration
- [ ] LikeApiDriver created following existing Driver pattern
- [ ] EngagementSteps created following existing StepDefinitions pattern
- [ ] TestHooks updated to clean likes table between scenarios
- [ ] DependencyConfig updated to register new Driver
