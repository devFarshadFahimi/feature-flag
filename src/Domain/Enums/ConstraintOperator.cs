
// Domain/Enums/ConstraintOperator.cs
public enum ConstraintOperator
{
    // String operators
    In = 0,
    NotIn = 1,
    StrStartsWith = 2,
    StrEndsWith = 3,
    StrContains = 4,

    // Numeric operators
    NumEq = 10,
    NumGt = 11,
    NumGte = 12,
    NumLt = 13,
    NumLte = 14,

    // Date operators
    DateAfter = 20,
    DateBefore = 21,

    // Semantic version operators
    SemverEq = 30,
    SemverGt = 31,
    SemverLt = 32,

    // Special
    AlwaysTrue = 99,
    AlwaysFalse = 100
}
