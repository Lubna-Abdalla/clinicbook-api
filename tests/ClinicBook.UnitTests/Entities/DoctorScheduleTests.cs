using ClinicBook.Core.Entities;
using ClinicBook.Core.Exceptions;

namespace ClinicBook.UnitTests.Entities;

/// <summary>
/// Tests for the weekly schedule rules: what counts as a valid working window,
/// how it is split into slots, and which slots are still free on a date.
/// </summary>
public class DoctorScheduleTests
{
    // 2026-10-06 is a Tuesday.
    private static readonly DateOnly Tuesday = new(2026, 10, 6);
    private static readonly DateTime MondayEvening = new(2026, 10, 5, 20, 0, 0, DateTimeKind.Utc);

    private static DoctorSchedule CreateSchedule(
        TimeOnly? start = null,
        TimeOnly? end = null,
        int slotMinutes = 30,
        DayOfWeek dayOfWeek = DayOfWeek.Tuesday) =>
        new()
        {
            Id = 1,
            DoctorId = 1,
            DayOfWeek = dayOfWeek,
            StartTime = start ?? new TimeOnly(9, 0),
            EndTime = end ?? new TimeOnly(12, 0),
            SlotMinutes = slotMinutes
        };

    [Fact]
    public void Validate_AcceptsASensibleSchedule()
    {
        var schedule = CreateSchedule();

        // No exception means the schedule is valid.
        schedule.Validate();

        Assert.Equal(TimeSpan.FromHours(3), schedule.Duration);
    }

    [Fact]
    public void Validate_Throws_WhenEndTimeIsNotAfterStartTime()
    {
        var schedule = CreateSchedule(start: new TimeOnly(13, 0), end: new TimeOnly(9, 0));

        var exception = Assert.Throws<BusinessRuleException>(schedule.Validate);

        Assert.Contains("after the start time", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(121)]
    [InlineData(1000)]
    public void Validate_Throws_WhenSlotLengthIsOutsideTheAllowedRange(int slotMinutes)
    {
        var schedule = CreateSchedule(slotMinutes: slotMinutes);

        Assert.Throws<BusinessRuleException>(schedule.Validate);
    }

    [Fact]
    public void Validate_Throws_WhenASlotIsLongerThanTheWorkingWindow()
    {
        // A 20 minute window cannot hold a 60 minute appointment.
        var schedule = CreateSchedule(
            start: new TimeOnly(9, 0), end: new TimeOnly(9, 20), slotMinutes: 60);

        Assert.Throws<BusinessRuleException>(schedule.Validate);
    }

    [Fact]
    public void GetSlotStartTimes_SplitsTheWindowIntoSlots()
    {
        var schedule = CreateSchedule(new TimeOnly(9, 0), new TimeOnly(11, 0), slotMinutes: 30);

        var slots = schedule.GetSlotStartTimes();

        Assert.Equal(
            [new TimeOnly(9, 0), new TimeOnly(9, 30), new TimeOnly(10, 0), new TimeOnly(10, 30)],
            slots);
    }

    [Fact]
    public void GetSlotStartTimes_SkipsASlotThatWouldNotFitInTheWindow()
    {
        // 09:00-10:10 fits two 30 minute slots; a third would end at 10:30, past closing time.
        var schedule = CreateSchedule(new TimeOnly(9, 0), new TimeOnly(10, 10), slotMinutes: 30);

        var slots = schedule.GetSlotStartTimes();

        Assert.Equal([new TimeOnly(9, 0), new TimeOnly(9, 30)], slots);
    }

    [Theory]
    [InlineData(9, 0, true)]
    [InlineData(9, 30, true)]
    [InlineData(9, 7, false)]   // not on a slot boundary
    [InlineData(13, 0, false)]  // outside the working window
    public void HasSlotStartingAt_OnlyAcceptsRealSlotStartTimes(int hour, int minute, bool expected)
    {
        var schedule = CreateSchedule();

        Assert.Equal(expected, schedule.HasSlotStartingAt(new TimeOnly(hour, minute)));
    }

    [Fact]
    public void GetAvailableSlotStartTimes_ReturnsNothingForAnotherWeekday()
    {
        var schedule = CreateSchedule(dayOfWeek: DayOfWeek.Monday);

        // The schedule is for Mondays, but we are asking about a Tuesday.
        var slots = schedule.GetAvailableSlotStartTimes(Tuesday, [], MondayEvening);

        Assert.Empty(slots);
    }

    [Fact]
    public void GetAvailableSlotStartTimes_ReturnsEverySlot_WhenNothingIsBooked()
    {
        var schedule = CreateSchedule();

        var slots = schedule.GetAvailableSlotStartTimes(Tuesday, [], MondayEvening);

        Assert.Equal(6, slots.Count);
        Assert.Equal(Tuesday.ToDateTime(new TimeOnly(9, 0)), slots[0]);
    }

    [Fact]
    public void GetAvailableSlotStartTimes_HidesSlotsThatAreAlreadyBooked()
    {
        var schedule = CreateSchedule();
        var bookedSlot = Tuesday.ToDateTime(new TimeOnly(9, 30));
        var booked = Appointment.Schedule(1, 2, bookedSlot, bookedSlot.AddMinutes(30), null, MondayEvening);

        var slots = schedule.GetAvailableSlotStartTimes(Tuesday, [booked], MondayEvening);

        Assert.Equal(5, slots.Count);
        Assert.DoesNotContain(bookedSlot, slots);
    }

    [Fact]
    public void GetAvailableSlotStartTimes_OffersACancelledSlotAgain()
    {
        // The rule that matters most: cancelling must not keep the slot blocked.
        var schedule = CreateSchedule();
        var slot = Tuesday.ToDateTime(new TimeOnly(9, 30));
        var cancelled = Appointment.Schedule(1, 2, slot, slot.AddMinutes(30), null, MondayEvening);
        cancelled.Cancel(MondayEvening);

        var slots = schedule.GetAvailableSlotStartTimes(Tuesday, [cancelled], MondayEvening);

        Assert.Equal(6, slots.Count);
        Assert.Contains(slot, slots);
    }

    [Fact]
    public void GetAvailableSlotStartTimes_StillHidesACompletedOrNoShowSlot()
    {
        var schedule = CreateSchedule();
        var slot = Tuesday.ToDateTime(new TimeOnly(10, 0));

        var completed = Appointment.Schedule(1, 2, slot, slot.AddMinutes(30), null, MondayEvening);
        completed.Complete("Done");

        var noShowSlot = Tuesday.ToDateTime(new TimeOnly(10, 30));
        var noShow = Appointment.Schedule(1, 3, noShowSlot, noShowSlot.AddMinutes(30), null, MondayEvening);
        noShow.MarkAsNoShow();

        var slots = schedule.GetAvailableSlotStartTimes(Tuesday, [completed, noShow], MondayEvening);

        Assert.Equal(4, slots.Count);
        Assert.DoesNotContain(slot, slots);
        Assert.DoesNotContain(noShowSlot, slots);
    }

    [Fact]
    public void GetAvailableSlotStartTimes_DoesNotOfferSlotsInThePast()
    {
        var schedule = CreateSchedule();
        // "Now" is 10:15 on the same Tuesday, so only the 10:30, 11:00 and 11:30 slots are left.
        var now = Tuesday.ToDateTime(new TimeOnly(10, 15));

        var slots = schedule.GetAvailableSlotStartTimes(Tuesday, [], now);

        Assert.Equal(
            [
                Tuesday.ToDateTime(new TimeOnly(10, 30)),
                Tuesday.ToDateTime(new TimeOnly(11, 0)),
                Tuesday.ToDateTime(new TimeOnly(11, 30))
            ],
            slots);
    }
}
