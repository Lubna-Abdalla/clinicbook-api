using ClinicBook.Core.Entities;
using ClinicBook.Core.Enums;
using ClinicBook.Core.Exceptions;

namespace ClinicBook.UnitTests.Entities;

/// <summary>
/// Tests for the appointment rules. These are plain unit tests: no database and no web server,
/// because the rules live in the Core entity itself.
/// Each test follows Arrange - Act - Assert.
/// </summary>
public class AppointmentTests
{
    private static readonly DateTime Now = new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SlotStart = new(2026, 10, 6, 9, 0, 0, DateTimeKind.Utc);

    private static Appointment CreateScheduledAppointment() =>
        Appointment.Schedule(
            doctorId: 1,
            patientId: 2,
            startsAt: SlotStart,
            endsAt: SlotStart.AddMinutes(30),
            reason: "Headache",
            createdAt: Now);

    [Fact]
    public void Schedule_SetsTheExpectedStartingValues()
    {
        var appointment = CreateScheduledAppointment();

        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.Equal(SlotStart, appointment.StartsAt);
        Assert.Equal(SlotStart.AddMinutes(30), appointment.EndsAt);
        Assert.Equal(Now, appointment.CreatedAt);
        Assert.Null(appointment.CancelledAt);
    }

    [Fact]
    public void Schedule_Throws_WhenEndIsBeforeStart()
    {
        var exception = Assert.Throws<BusinessRuleException>(() =>
            Appointment.Schedule(1, 2, SlotStart, SlotStart.AddMinutes(-30), null, Now));

        Assert.Contains("after the start time", exception.Message);
    }

    [Fact]
    public void Schedule_Throws_WhenEndEqualsStart()
    {
        Assert.Throws<BusinessRuleException>(() =>
            Appointment.Schedule(1, 2, SlotStart, SlotStart, null, Now));
    }

    [Fact]
    public void Cancel_ChangesStatusAndRecordsWhen_ButKeepsTheRecord()
    {
        var appointment = CreateScheduledAppointment();

        appointment.Cancel(Now);

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal(Now, appointment.CancelledAt);
        // History is preserved: the times and the reason are still there after cancelling.
        Assert.Equal(SlotStart, appointment.StartsAt);
        Assert.Equal("Headache", appointment.Reason);
    }

    [Fact]
    public void Cancel_FreesTheTimeSlot()
    {
        var appointment = CreateScheduledAppointment();
        Assert.True(appointment.OccupiesTimeSlot);

        appointment.Cancel(Now);

        // This is the rule that lets another patient book the same slot afterwards.
        Assert.False(appointment.OccupiesTimeSlot);
    }

    [Fact]
    public void Cancel_Throws_WhenAlreadyCancelled()
    {
        var appointment = CreateScheduledAppointment();
        appointment.Cancel(Now);

        var exception = Assert.Throws<BusinessRuleException>(() => appointment.Cancel(Now));

        Assert.Contains("already cancelled", exception.Message);
    }

    [Fact]
    public void Cancel_Throws_WhenAppointmentIsAlreadyCompleted()
    {
        var appointment = CreateScheduledAppointment();
        appointment.Complete("All good");

        Assert.Throws<BusinessRuleException>(() => appointment.Cancel(Now));
    }

    [Fact]
    public void Complete_StoresTheDoctorNotes()
    {
        var appointment = CreateScheduledAppointment();

        appointment.Complete("Prescribed rest for two days");

        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.Equal("Prescribed rest for two days", appointment.DoctorNotes);
        // A completed visit still occupies its slot, so nobody else can take it.
        Assert.True(appointment.OccupiesTimeSlot);
    }

    [Fact]
    public void Complete_Throws_WhenAppointmentWasCancelled()
    {
        var appointment = CreateScheduledAppointment();
        appointment.Cancel(Now);

        Assert.Throws<BusinessRuleException>(() => appointment.Complete("Too late"));
    }

    [Fact]
    public void MarkAsNoShow_SetsTheStatus()
    {
        var appointment = CreateScheduledAppointment();

        appointment.MarkAsNoShow();

        Assert.Equal(AppointmentStatus.NoShow, appointment.Status);
        Assert.True(appointment.OccupiesTimeSlot);
    }

    [Theory]
    [InlineData(AppointmentStatus.Completed)]
    [InlineData(AppointmentStatus.NoShow)]
    [InlineData(AppointmentStatus.Cancelled)]
    public void MarkAsNoShow_Throws_WhenAppointmentIsNoLongerScheduled(AppointmentStatus status)
    {
        // A Theory runs the same test once per InlineData row.
        var appointment = CreateScheduledAppointment();
        appointment.Status = status;

        Assert.Throws<BusinessRuleException>(appointment.MarkAsNoShow);
    }

    [Theory]
    [InlineData(AppointmentStatus.Scheduled, true)]
    [InlineData(AppointmentStatus.Completed, true)]
    [InlineData(AppointmentStatus.NoShow, true)]
    [InlineData(AppointmentStatus.Cancelled, false)]
    public void OccupiesTimeSlot_IsFalseOnlyForCancelledAppointments(
        AppointmentStatus status,
        bool expected)
    {
        var appointment = CreateScheduledAppointment();
        appointment.Status = status;

        Assert.Equal(expected, appointment.OccupiesTimeSlot);
    }
}
