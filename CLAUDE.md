# Decorations
    - Make sure to always update css when it comes to light vs dark mode
    - Attributations have their own separate page that can be accessed in the footer using a "Credits" link

# Tests
    - Always create a test for a method implementation
    - dotnet code needs to be tested in a UnitTests dotnet project 
    - dotnet code needs tests to be implemented using xUnit 

# Implementation
    - Always remove unused code and tests that don't match current requirements
    - There needs to be three layers of implementation Controller-Service-Respository code architecture pattern
    - Controller layer is the management of REST interface to the business logic
    - Service layer is the business logic implementation
    - Repository layer is the logic around the storage entities
    - Each layer needs to have its own dotnet project
    - Instead of instantiating objects in various places, it should be done instead in Mapper classes where the instantiated objects are done in Map methods
    - Mapper classes should be named according to the outputobject. For example, given Entity class that needs to be instantiated, a class called EntityMapper should be created.
    - Mapper classes can assume that parameters are not nullable, unless it is marked so.
    - Mapper classes are generally singletons in dependency injection and will need interfaces
    - All classes that are create that serve a purposes of logic, should be added to dependency injection for ease of use
    - Controller input validation should be done in specific service type classes
    - Other validations that are dependant on some business or repository logic, should be done in its respective layer with specific service type classes
    - All validation class name should end with Validation
    - When handling validation make use of a specific class ServiceResponse 
    - Avoid throwing exceptions, instead make use of a class ServiceResponse which can be used to pass generic data around and errors
    - All REST endpoints should return a response body (ApiResponse) that follows this format:
    {
        "data": {
            "field1": "value1",
            "field2": "value2"
        },
        "errors": [
            {
                "code": "some_error_code_value",
                "message": "The message specific to this error code",
                "metadata": {
                    "key1": "value1",
                    "key2": "value2"
                }
            }
        ]
    }
    - The ServiceResponse can be mapped to ApiResponse
    - The "data" in the REST response is mainly used when there is a positive response status code
    - The "errors" in the REST response is mainly used when there is a negative response status code, and there could be multiple negative outcomes, where possible
    - Both "data" and "errors" can be null, but it entirely depends on the status code.
    - The "errors[index].code" in the REST response can be used for handling various scenarios that can tie to that error code
    - The "errors[index].message" in the REST response can be used for displaying the error message or for diagnosing a specific problem around the error
    - The "errors[index].metadata" is the REST response is a dictionary of string key and string value. and can be used for giving context information for that particular error. Additionally if that error comes up multiple times in one request the context information can be index within the dictionary keys like so:
    {
        "errors": [
            {
                "code": "person_not_found",
                "message": "The person could not be found",
                "metadata": {
                    "person_id_1": "31254561",
                    "person_id_2": "90393473"
                }
            }
        ]
    }
    - Http status code 204 - No Content is the only response that doesn't need a response body
    - Make use of dotnet middlewares where all REST responses and/or requests need to be handled in a specific way.
    - All classes need their own file
    - All models need to be in Models folders
    - Where hardcoded values are needed, then it is best to put those values in a static class with const/static fields.
    - For all models with non-nullable properties, best to use "= default!", unless described otherwise
    - All REST endpoints will need to be described in swagger
    - All the responses will need to be described in swagger
    