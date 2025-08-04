# Decorations
    - Make sure to always update css when it comes to light vs dark mode
    - Attributations have their own separate page that can be accessed in the footer using a "Credits" link

# Tests
    - Always create a test for a method implementation
    - dotnet code needs to be tested in a UnitTests dotnet project 
    - dotnet code needs tests to be implemented using xUnit 

# Implementation
    - There needs to be three layers of implementation Controller-Service-Respository code architecture pattern
    - Controller layer is the management of REST interface to the business logic
    - Service layer is the business logic implementation
    - Repository layer is the logic around the storage entities
    - Each layer needs to have its own dotnet project
    - Instead of instantiating objects in various places, it should be done instead in Mapper classes where the instantiated objects are done in Map methods
    - Mapper classes should be named according to the outputobject. For example, given Entity class that needs to be instantiated, a class called EntityMapper should be created.
    - All classes that are create that serve a purposes of logic, should be added to dependency injection for ease of use
    