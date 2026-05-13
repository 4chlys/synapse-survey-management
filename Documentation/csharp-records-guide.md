# C# Records — Team Knowledge Share

## What is a Record?

A `record` is a special type introduced in **C# 9** designed for immutable data models with value-based equality. It looks like a class but behaves differently in key ways.

```csharp
// One line — that's it
public record Person(string Name, int Age);
```

---

## Record vs Class — Key Differences

| Feature | `class` | `record` |
|---|---|---|
| Equality | By **reference** | By **value** |
| Immutable by default | ❌ | ✅ |
| `with` expression | ❌ | ✅ |
| Boilerplate needed | A lot | Almost none |
| `ToString()` | Type name only | All property values |
| Inheritance | ✅ | ✅ |

### Equality — the biggest difference

```csharp
// Class
var c1 = new PersonClass("Alice", 30);
var c2 = new PersonClass("Alice", 30);
Console.WriteLine(c1 == c2); // false — different references

// Record
var r1 = new Person("Alice", 30);
var r2 = new Person("Alice", 30);
Console.WriteLine(r1 == r2); // true — same values
```

### Immutability

```csharp
var p = new Person("Alice", 30);
p.Name = "Bob"; // ❌ compile error — init-only
```

### Non-destructive mutation with `with`

```csharp
var p1 = new Person("Alice", 30);
var p2 = p1 with { Age = 31 }; // new copy, one property changed
```

### Built-in `ToString()`

```csharp
Console.WriteLine(p1); // Person { Name = Alice, Age = 30 }
```

---

## When to Use Records

| Use case | Record? |
|---|---|
| DTOs (API request/response) | ✅ Yes |
| Value objects (Money, Address) | ✅ Yes |
| Configuration/settings | ✅ Yes |
| Entities with mutable state | ❌ Use class |
| Objects compared by identity | ❌ Use class |

---

## Records for DTOs — Our Context

Records are a natural fit for DTOs. Compare:

```csharp
// ❌ Class DTO — verbose
public class CreateUserDto
{
    public string Name { get; init; }
    public string Email { get; init; }
    public int Age { get; init; }

    public CreateUserDto(string name, string email, int age)
    {
        Name = name;
        Email = email;
        Age = age;
    }
    // + Equals, GetHashCode, ToString...
}

// ✅ Record DTO — one line, same result
public record CreateUserDto(string Name, string Email, int Age);
```

Serialization with `System.Text.Json` works out of the box on **.NET 7+**.

---

## Bonus: `record struct`

For small, high-frequency value types where performance matters:

```csharp
public readonly record struct Point(double X, double Y);
public readonly record struct Money(decimal Amount, string Currency);
```

- Stack allocated → no GC pressure
- Mutable by default, so add `readonly` to enforce immutability
- Best kept under ~16 bytes

---

## Quick Reference

```csharp
public record Person(string Name, int Age);                        // record (reference type)
public record struct Point(double X, double Y);                    // record struct (value type)
public readonly record struct Point(double X, double Y);           // immutable record struct
```

---

## Bottom Line

> If the type represents **data** → use `record`.  
> If the type has **identity, mutable state, or complex behavior** → use `class`.
