# ClinicBook API

A REST API for booking appointments in a small clinic, built with **ASP.NET Core 10**, **Entity Framework Core** and **SQL Server**.

Patients register, browse doctors by specialty, see which slots a doctor still has free and book one.
Doctors publish a recurring weekly schedule and close their visits. Admins manage specialties and doctors.

This is a learning/portfolio project, written to be small, readable and explainable rather than "enterprise".

---

## What it demonstrates

- ASP.NET Core Web API with **controllers** and **dependency injection**
- **Clean Architecture**-style layering (`Api → Infrastructure → Core`)
- **EF Core** with Fluent API configurations, migrations, indexes and check constraints
- **SQL Server** database design: relationships, unique constraints, a filtered unique index
- **ASP.NET Core Identity** + **JWT** authentication and **role-based** authorization
- **DTOs** and model **validation**
- **Business rules** in the domain entities, with **unit tests** (xUnit)
- Central **error handling** returning `ProblemDetails`
- `async`/`await` throughout

---

## Project structure

```
ClinicBook.slnx
├─ src/ClinicBook.Core            Domain layer - no dependencies at all
│  ├─ Entities                    Specialty, Doctor, Patient, DoctorSchedule, Appointment
│  ├─ Enums                       AppointmentStatus
│  ├─ DTOs                        Request/response models used by the API
│  ├─ Interfaces                  Service contracts (implemented in Infrastructure)
│  ├─ Exceptions                  NotFound / BusinessRule / Forbidden / Unauthorized
│  ├─ Validators                  Custom validation attribute (PastDate)
│  └─ Constants                   Role names
├─ src/ClinicBook.Infrastructure  Everything database- and Identity-related
│  ├─ Data                        DbContext, Fluent API configurations, migrations, seeder
│  ├─ Identity                    ApplicationUser, JWT settings and token generation
│  └─ Services                    The service implementations (business logic + queries)
├─ src/ClinicBook.Api             Controllers, middleware, startup
└─ tests/ClinicBook.UnitTests     xUnit tests for the domain rules
```

**Dependency rule:** `Api → Infrastructure → Core`, and `Core` references nothing.
That is why `Core` contains no EF Core or Identity code: the entities are plain C# classes,
and all database mapping lives in `Infrastructure/Data/Configurations`.

---

## Database

| Table | Purpose |
|---|---|
| `AspNetUsers` (+ other Identity tables) | Login accounts. Extended with `FullName` and `CreatedAt`. |
| `Specialties` | e.g. Cardiology. `Name` is unique. |
| `Doctors` | Clinic data for a doctor; `UserId` is a unique FK to `AspNetUsers`. |
| `Patients` | Clinic data for a patient; `UserId` is a unique FK to `AspNetUsers`. |
| `DoctorSchedules` | A *recurring weekly* working window. Unique on `(DoctorId, DayOfWeek)`. |
| `Appointments` | One booking. Never deleted; cancelling only changes the status. |

Notable constraints:

- `UX_Appointments_DoctorId_StartsAt_Active`: **unique filtered index** on `(DoctorId, StartsAt)`
  `WHERE Status <> 'Cancelled'` &rarr; a doctor cannot have two active appointments at the same
  moment, while a cancelled appointment frees its slot again.
- `IX_Appointments_PatientId_StartsAt` for "my appointments" queries.
- Appointment foreign keys use **Restrict**, so appointment history can never be deleted by
  cascade (and SQL Server's "multiple cascade paths" problem is avoided).
- `ConsultationFee` is `decimal(10,2)`; `Status` is stored as readable text (`nvarchar(30)`).
- Check constraints mirror the C# rules: `EndsAt > StartsAt`, `EndTime > StartTime`,
  `SlotMinutes BETWEEN 5 AND 120`.

---

## Running it locally

**Requirements:** .NET 10 SDK, SQL Server (a local instance or LocalDB), `dotnet-ef` tool
(`dotnet tool install --global dotnet-ef`).

Secrets are **not** committed. Configure them once with User Secrets:

```bash
cd src/ClinicBook.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ClinicBookDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet user-secrets set "Jwt:Key" "a-long-random-development-signing-key-at-least-32-chars"
dotnet user-secrets set "SeedAdmin:Email" "admin@clinicbook.local"
dotnet user-secrets set "SeedAdmin:Password" "<choose-your-own-password>"
```

`appsettings.json` is committed and holds only non-secret values (the JWT issuer, audience and
lifetime). The connection string, the signing key and the admin password exist only in User Secrets
on the developer's machine.

(For LocalDB use `Server=(localdb)\MSSQLLocalDB;...` instead.)

Create the database and run:

```bash
dotnet ef database update --project src/ClinicBook.Infrastructure --startup-project src/ClinicBook.Api
dotnet run --project src/ClinicBook.Api
```

In **Development** the app also applies pending migrations and seeds the three roles plus the
admin account on startup. Swagger UI is then at `https://localhost:7169/swagger`.
`src/ClinicBook.Api/ClinicBook.Api.http` contains a ready-made request walkthrough for Visual Studio.

Run the tests with:

```bash
dotnet test
```

---

## Endpoints

| Method | Route | Who |
|---|---|---|
| POST | `/api/auth/register` | anyone (creates a patient) |
| POST | `/api/auth/login` | anyone |
| GET | `/api/auth/me` | any signed-in user |
| GET | `/api/specialties` | anyone |
| POST | `/api/specialties` | Admin |
| GET | `/api/doctors` (`?specialtyId=`) | anyone |
| GET | `/api/doctors/{id}` | anyone |
| POST | `/api/doctors` | Admin (also creates the login account) |
| PUT | `/api/doctors/{id}` | Admin |
| GET | `/api/doctors/{id}/schedules` | anyone |
| POST | `/api/doctors/{id}/schedules` | Admin, or that doctor |
| DELETE | `/api/doctors/{id}/schedules/{scheduleId}` | Admin, or that doctor |
| GET | `/api/doctors/{id}/available-slots?date=` | anyone |
| POST | `/api/appointments` | Patient |
| GET | `/api/appointments/me` | Patient or Doctor |
| GET | `/api/appointments/{id}` | its patient, its doctor, or Admin |
| POST | `/api/appointments/{id}/cancel` | its patient, its doctor, or Admin |
| POST | `/api/appointments/{id}/complete` | the treating doctor |
| POST | `/api/appointments/{id}/no-show` | the treating doctor |

Errors are returned as `ProblemDetails`: `400` validation, `401` not signed in,
`403` not your record, `404` unknown id, `409` a clinic rule was broken.

---

## Business rules

1. A doctor cannot have two **active** appointments starting at the same time
   (checked in code for a friendly message, and enforced by the filtered unique index).
2. **Cancelling frees the slot**: a cancelled appointment stops occupying its time.
3. **History is preserved**: appointments are never deleted, only their status changes,
   and `CancelledAt` records when.
4. A schedule is a **recurring weekly** window, so a date is bookable only if the doctor
   works on that weekday.
5. A booking must start exactly on one of the doctor's slot start times, and must be in the future.
6. The **end time is calculated by the server** from the schedule's slot length, so it is always
   after the start time.
7. `SlotMinutes` must be between 5 and 120 and must fit inside the working window.
8. Status transitions are one-way: only a `Scheduled` appointment can be cancelled, completed
   or marked as a no-show.
9. Inactive doctors keep their history but take no new bookings.

Where each rule lives:

- Rules 2, 3, 4, 7 and 8, and the slot-boundary half of rule 5, are in the `Core` entities
  (`Appointment`, `DoctorSchedule`) and are covered by the unit tests.
- Rule 1, rule 6, rule 9 and the "must be in the future" half of rule 5 are enforced in
  `AppointmentService`, which combines those entity rules with the database queries.
- The database constraints (the filtered unique index and the check constraints) are the second
  line of defence, and the unique index is what makes rule 1 safe when two people book at once.

---

## Things I would add next

- Integration tests for the controllers/services (with a test database)
- Paging on the list endpoints
- Refresh tokens, and email confirmation on registration
- Time zone handling (times are currently treated as UTC everywhere)
