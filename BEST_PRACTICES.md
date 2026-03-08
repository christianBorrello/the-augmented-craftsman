# BEST PRACTICES - Agile Technical Practices Distilled

## Core Philosophy

> "We are what we repeatedly do. Hence, excellence is not an act, but a habit." - Will Durant

> "As tests get more specific code gets more generic." - Robert C. Martin

---

## Test-Driven Development (TDD)

### The Three Laws of TDD

1. **You are not allowed to write any more of a unit test that is sufficient to fail**
   - Compilation failures are failures
   - Make sure the test fails for the RIGHT reason
   
2. **You are not allowed to write any production code unless it is to make a failing unit test pass**
   - Only write code in response to a failing test
   
3. **You are not allowed to write any more production code that is sufficient to pass the one failing unit test**
   - Stop as soon as the test passes

### The Red-Green-Refactor Cycle

```
RED → GREEN → REFACTOR → RED → ...
```

1. **RED**: Write a failing test
2. **GREEN**: Make it pass with the simplest code
3. **REFACTOR**: Clean up while keeping tests green

### Writing Tests

**DO:**
- ✅ Write the assertion FIRST and work backward
- ✅ Organize tests in Arrange-Act-Assert (Given-When-Then) blocks
- ✅ Test ONE thing only per test
- ✅ Give tests meaningful, behavior-oriented names
- ✅ See the test fail for the right reason before implementing
- ✅ Keep tests fast, isolated, repeatable, and self-validating (FIRST)
- ✅ Use the Rule of Three before extracting duplication

**DON'T:**
- ❌ Use technical names (e.g., `myMethodNameReturnsSomething`)
- ❌ Leak implementation details (e.g., `CommandPatternTest`)
- ❌ Mix test code with production code
- ❌ Break tests during refactoring

### Three Methods of Moving Forward

1. **Fake It**: Return the exact value needed (when unsure)
2. **Obvious Implementation**: Write it if you're sure
3. **Triangulation**: Write specific tests to force generic code

### Transformation Priority Premise (TPP)

Apply transformations in order (simpler → complex):

1. `{}` → nil
2. nil → constant
3. constant → constant+
4. constant → scalar
5. statement → statements
6. unconditional → conditional
7. scalar → array
8. array → container
9. statement → tail recursion
10. if → loop
11. statement → recursion
12. expression → function
13. variable → mutation

**Keep complexity low by using simpler transformations when possible**

---

## Object Calisthenics (10 Design Rules)

### 1. Only One Level of Indentation Per Method
```csharp
// BAD
public void Process() {
    foreach(var item in items) {
        if(item.IsValid()) {
            // nested logic
        }
    }
}

// GOOD
public void Process() {
    foreach(var item in items) {
        ProcessItem(item);
    }
}
```

### 2. Don't Use the ELSE Keyword
- Promotes polymorphism and State Pattern
- Use early returns, Null Object pattern
- Creates a main execution lane with fewer special cases

### 3. Wrap All Primitives and Strings
```csharp
// BAD
public void SendEmail(string recipient, string sender)

// GOOD
public void SendEmail(Recipient recipient, Sender sender)
```
- Make invalid states unrepresentable
- Primitives are behavior attractors
- Cure for Primitive Obsession

### 4. First Class Collections
```csharp
// BAD
public class Invoice {
    private List<InvoiceLine> lines;
}

// GOOD
public class InvoiceDetails {
    private List<InvoiceLine> lines;
}
public class Invoice {
    private InvoiceDetails details;
}
```
- Any class with a collection should contain NO other member variables
- Give collection behaviors a home

### 5. One Dot Per Line
```csharp
// BAD
dog.Body.Tail.Wag()

// GOOD
dog.ExpressHappiness()
```
- **Law of Demeter**: Talk only to immediate neighbors
- Hide dependencies
- Exposes intent, hides implementation

### 6. Don't Abbreviate
- Use clear, expressive names
- If you need a long name, you might be missing a concept

### 7. Keep All Entities Small
- **10 files** per package
- **50 lines** per class
- **5 lines** per method
- **2 arguments** per method

### 8. No Classes with More Than Two Instance Variables
- More variables = lower Cohesion
- Separate orchestrators from actuators

### 9. No Getters/Setters/Properties
```csharp
// BAD - Asking for data
var name = person.GetName();
ProcessName(name);

// GOOD - Telling what to do
person.Process();
```
- **Tell, Don't Ask**
- Override equality for testing instead of adding getters

### 10. All Classes Must Have State
- No static methods (unless absolutely necessary)
- No utility classes
- Create classes with clear responsibilities

---

## SOLID Principles

### Single Responsibility Principle (SRP)
> A class should have only ONE reason to change

- Gather together things that change for the same reason
- Separate things that change for different reasons
- Can describe what a class does without using "and" or "or"

### Open/Closed Principle (OCP)
> Open for extension, closed for modification

- Use abstractions and interfaces
- Plugin-ability through dependency injection
- Apply Rule of Three before abstracting

### Liskov Substitution Principle (LSP)
> Subtypes must be substitutable for their base types

- Derived classes should keep promises made by base classes
- Preconditions cannot be more restrictive
- Postconditions cannot be less restrictive
- "Is-a" relationship based on BEHAVIOR, not structure

### Interface Segregation Principle (ISP)
> Clients should not depend on interfaces they don't use

- Split large interfaces into smaller, focused ones
- High Cohesion for interfaces

### Dependency Inversion Principle (DIP)
> Depend on abstractions, not concretions

- High-level modules should not depend on low-level modules
- Both should depend on abstractions
- Abstractions should not depend on details

### Balanced Abstraction Principle (BAP)
> All code at the same level should be at the same level of abstraction

- Methods in a class at same abstraction level
- Statements in a method at same abstraction level

### Principle of Least Astonishment (PoLA)
> Code should behave as users expect

- Don't mislead with naming
- Method behavior should match method name

---

## Code Smells to Avoid

### Critical Smells (Fix Immediately)

**Primitive Obsession**
- Using primitives instead of classes
- Leads to: Duplicated Code, Shotgun Surgery

**Feature Envy**
- Class uses methods/properties of another class excessively
- Behavior should be close to the data it uses

**Message Chains**
- `dog.body.tail.wag()` violates Law of Demeter
- Leads to: Shotgun Surgery, high Coupling

**Duplicated Code**
- Apply DRY principle
- Use Rule of Three before extracting

### Other Important Smells

- **Long Method** - Methods doing too much (>5 lines)
- **Large Class** - Classes with too many responsibilities (>50 lines)
- **Long Parameter List** - More than 2 parameters
- **Data Class** - Classes with only data, no behavior
- **Divergent Change** - One class changes for different reasons
- **Shotgun Surgery** - One change requires changes everywhere

---

## Refactoring Guidelines

### When to Refactor

1. When you find duplication (Rule of Three)
2. When you break Object Calisthenics rules
3. When code exhibits code smells
4. When code has low Cohesion or high Coupling
5. When code violates SOLID principles

### How to Refactor

**STAY ON GREEN**
- Never break tests during refactoring
- If tests break, undo and start over
- Use IDE automated refactoring

**Refactor Readability First (80% of value)**
1. Format code consistently
2. Rename bad names, make abbreviations explicit
3. Delete unnecessary comments and dead code
4. Extract constants from magic numbers
5. Extract conditionals into named methods
6. Reorder to minimize scope

**Then Refactor Design**
1. Extract private methods
2. Return early from methods
3. Encapsulate missing encapsulation
4. Remove duplication
5. Refactor to patterns when appropriate

### Five Atomic Refactors

1. **Rename** - Change names of classes, methods, variables
2. **Extract** - Create new abstractions
3. **Inline** - Deconstruct abstractions
4. **Move** - Relocate code
5. **Safe Delete** - Remove code and usages

### Parallel Change (Expand-Migrate-Contract)

1. **Expand**: Add new functionality alongside old
2. **Migrate**: Move clients to new functionality
3. **Contract**: Remove old functionality

---

## Cohesion and Coupling

### Cohesion (How focused is a component?)

**HIGH Cohesion (GOOD)**
- Functional Cohesion - all parts work together for one purpose
- Components do one thing well

**LOW Cohesion (BAD)**
- Coincidental - unrelated elements together
- Logical - similar operations grouped incorrectly
- Temporal - grouped only by timing

**Signs of Low Cohesion:**
- Static methods in a class
- Helper/Utility classes
- Data Classes
- Lazy Classes

### Coupling (How dependent are components?)

**LOW Coupling (GOOD)**
- Message Coupling - communicate via messages
- Data Coupling - share data through parameters

**HIGH Coupling (BAD)**
- Content Coupling - depends on internal data
- Common Coupling - share global data
- Control Coupling - controls flow of another

**Signs of High Coupling:**
- Feature Envy
- Message Chains
- Inappropriate Intimacy

### Balance is Key
```
Too much Cohesion → God Class
Too little Cohesion → Scattered behavior
Too much Coupling → Rigid, fragile system
Too little Coupling → Nothing works together
```

---

## Connascence (Unified Theory of Design)

> Two elements are connascent if a change in one requires a change in the other

### Three Dimensions

1. **Degree** - How many elements affected?
2. **Locality** - How close are the elements?
3. **Strength** - How hard to refactor?

### Refactoring Direction

```
Stronger → Weaker (GOOD)
More Degree → Less Degree (GOOD)
Further Locality → Closer Locality (GOOD)
```

### Types (Weak → Strong)

**Static (discoverable at compile time)**
1. **Connascence of Name (CoN)** - Same name used multiple times (GOOD)
2. **Connascence of Type (CoT)** - Same type used (GOOD)
3. **Connascence of Meaning (CoM)** - Agreed meaning of values
4. **Connascence of Position (CoP)** - Order of parameters matters (BAD)
5. **Connascence of Algorithm (CoA)** - Same algorithm used

**Dynamic (discoverable at runtime)**
6. **Connascence of Execution Order (CoEO)** - Order of calls matters
7. **Connascence of Timing (CoTm)** - Timing of calls matters
8. **Connascence of Value (CoV)** - Values must be related
9. **Connascence of Identity (CoI)** - Reference same instance (WORST)

---

## The Four Elements of Simple Design

In priority order:

1. **Passes its tests** - System implements expected behavior
2. **Minimizes duplication** - DRY principle, no repeated knowledge
3. **Maximizes clarity** - Reveals intention, obvious code
4. **Has fewer elements** - Minimal classes, methods, lines

> "Make it work, make it right, make it fast" - Kent Beck

---

## Naming Conventions

### Tests

```csharp
// Pattern: ClassNameShould
class CarShould {
    void decrease_speed_when_brakes_are_applied() {}
    void increase_speed_when_accelerator_is_applied() {}
}
```

- Use behavior-oriented names
- Avoid technical names
- No implementation details
- Readable as full sentences

### Code

**Naming Process (Arlo Belshee's stages):**
1. Nonsense → 2. Accurate-but-vague → 3. Precise → 4. Meaningful/Intention-revealing

- Don't agonize over first name
- Refactor names as understanding improves
- Express WHAT not HOW
- Use domain language (Ubiquitous Language)

---

## Test Doubles

### Command-Query Separation (CQS)

**Queries** - Return data, no side effects (idempotent)
- Use **STUBS** for queries in Arrange section

**Commands** - Change state, return void
- Use **MOCKS** for commands in Assert section

### Types of Test Doubles

- **Dummy** - Fill parameter lists, never used
- **Stub** - Provide canned responses to queries
- **Fake** - Hand-made stubs with working implementation
- **Mock** - Verify commands were called correctly
- **Spy** - Hand-made mocks

### Guidelines

**DO:**
- ✅ Only mock/stub classes YOU OWN
- ✅ Verify as little as possible
- ✅ Mock only immediate neighbors
- ✅ Use for commands (mocks) and queries (stubs)

**DON'T:**
- ❌ Mock third-party libraries (wrap them first)
- ❌ Add behavior in test doubles
- ❌ Mock isolated objects with no collaborators
- ❌ Use localStorage/sessionStorage in artifacts

---

## Acceptance Tests & BDD

### User Story Structure

```gherkin
Title: Clear description

Narrative:
As a [role]
I want [feature]
So that [benefit]

Acceptance Criteria:
Scenario: Title
  Given [context]
  And [more context]
  When [event]
  Then [outcome]
  And [another outcome]
```

### Acceptance Test Properties

1. **Readable by business folks** - Use plain language
2. **Feature complete** - Cover all scenarios
3. **Fast** - Run quickly for rapid feedback
4. **Boundaries** - Test at application layer, not infrastructure

### 5 Whys Technique

Ask "Why?" five times to reach root cause:
```
Problem: System is slow
Why? Database queries are slow
Why? Queries are not optimized
Why? No indexes on tables
Why? Requirements didn't specify performance needs
Why? No performance testing in workflow
→ Root: Need performance testing process
```

---

## Outside-In Development

### Outside-In Mindset

- From outside to inside
- From high-level to low-level
- From strategy to tactics
- From goal to steps
- Following dependency flow

### Double Loop TDD

```
OUTER LOOP (Acceptance Test):
  RED → GREEN → REFACTOR
    ↓
  INNER LOOP (Unit Tests):
    RED → GREEN → REFACTOR → RED → ...
```

1. Write failing acceptance test
2. Drop to unit tests
3. TDD until acceptance passes
4. Next acceptance test

### When to Use Each Approach

**Classic TDD**
- Exploring unknown domains
- Learning new patterns
- Beginners

**Outside-In TDD**
- Known problem space
- Experienced developers
- Business-driven development

---

## Code Quality Metrics

### Method Coupling Premises (Best → Worst)

1. **No parameters** (niladic) - BEST
2. **1 parameter** (monadic) - GOOD
3. **2 parameters** (dyadic) - ACCEPTABLE
4. **3 parameters** (triadic) - DEBATABLE
5. **3+ parameters** (polyadic) - AVOID

### Class Complexity

- Instance variables: Maximum 2
- Lines per class: ~50
- Methods per class: Minimal
- Public methods: Few, clear interface

---

## Legacy Code Guidelines

### Breaking Dependencies

1. **Identify the constraint** (seam)
2. **Use inheritance** to create testable subclass
3. **Add characterization tests** to document current behavior
4. **Refactor safely** with tests as safety net

### Golden Master Technique

1. Capture current output
2. Create test that asserts against it
3. Make changes
4. Verify output unchanged (or changed as expected)

### Approval Tests

- Lock code for refactoring
- Serialize output and mark as accepted
- Any change triggers test failure
- Review and accept/reject changes

---

## Continuous Improvement

### Great Habits Checklist

**Before Writing Code:**
- [ ] Do I understand the requirement?
- [ ] Have I written the test first?
- [ ] Is the test name behavior-oriented?
- [ ] Does the test fail for the right reason?

**While Writing Code:**
- [ ] Am I writing the simplest code?
- [ ] Am I using TPP to keep complexity low?
- [ ] Am I following Object Calisthenics?
- [ ] Am I considering SOLID principles?

**After Test Passes:**
- [ ] Can I see duplication? (Rule of Three)
- [ ] Are there code smells?
- [ ] Is Cohesion balanced?
- [ ] Is Coupling minimized?
- [ ] Can I improve naming?
- [ ] Are all tests still passing?

### Team Practices

1. **Pair Programming** - Two perspectives, better code
2. **Code Reviews** - Share knowledge, catch issues
3. **Mob Programming** - Whole team on hard problems
4. **Retrospectives** - Continuous improvement
5. **Knowledge Sharing** - No silos, collective ownership

---

## Key Quotes to Remember

> "Make it work, make it right, make it fast." - Kent Beck

> "Duplication is far easier to refactor than the wrong abstraction."

> "Tests should tell you when you are done."

> "The best code is the code that is not written."

> "Inertia is your enemy." - Claudio Perrone

> "Software development is a learning process, working code is a side effect." - Alberto Brandolini

> "Build projects around motivated individuals. Give them the environment and support they need, and trust them to get the job done." - Agile Manifesto

---

## Decision Framework

### When in doubt, ask:

1. **Does this increase complexity?** → Prefer simpler
2. **Does this violate Object Calisthenics?** → Refactor
3. **Does this create duplication?** → Wait for Rule of Three
4. **Does this break SOLID?** → Reconsider design
5. **Will this be clear in 6 months?** → Improve naming
6. **Can I test this easily?** → If no, redesign
7. **Does the business need this?** → YAGNI if not

---

## Remember

**Technical excellence is not enough**
- Understand the business domain
- Communicate effectively
- Work as a team
- Focus on delivering value
- Continuous learning and improvement

**"The fact is that the system that people work in and the interaction with people may account for 90 or 95 percent of performance." - W. Edwards Deming**