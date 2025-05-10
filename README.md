# MVC SSR Library Management System

my first C# .NET project, it seems nice enough. includes:

* Argon2 + salting + peppering
* JWT
* Sqlite
* Books, Users, Loans

### Auth

* admins see everything
* every user sees every book
    * can take them on loan. -normally this would require a request-confirmation mechanism, but not here-
    * can return the books they take.
* every user sees only their own loans.
* users dont see any user.
* some endpoints are open only to owner users and admins
    * like users' profiles, their loans etc.

### TODO

* showing proper errors (like "amount cannot be string") -wont do it here-
* sanitization and validation for all user inputs (both in frontend-maybe not- and backend) -will do on the term project, not here-
* searching, filtering, and sorting books, users (for admins), loans (for admins)   -will do on the term project, not here-
* maybe a better frontend using bootstrap etc. -will do on the term project, not here-

### TODO but a little bit advanced

* password changing -will do on the term project, not here-
* email validation -will do on the term project, not here-
* forgot password -will do on the term project, not here-
* friend functionality. users see their friends loans -ik its not that logical-      -will do on the term project, not here-
* MFA?
* read, reading, to read lists?