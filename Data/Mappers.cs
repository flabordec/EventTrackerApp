using System.Diagnostics;
using System.Diagnostics.Contracts;
using EventTrackerApp.ViewModel;
using Riok.Mapperly.Abstractions;

namespace EventTrackerApp.Data.Mappers;

[Mapper]
internal static partial class EventMapper
{
    [MapperIgnoreSource(nameof(EventDto.UserId))]
    public static partial EventViewModel ToEventViewModel(this EventDto e);

    [MapperIgnoreSource(nameof(EventValueDto.EventId))]
    public static partial EventValueViewModel ToValueViewModel(this EventValueDto ev);

    [MapperIgnoreSource(nameof(EventInstanceDto.EventValueId))]
    public static partial EventInstanceViewModel ToInstanceViewModel(this EventInstanceDto ei);


    [MapperIgnoreSource(nameof(Event.User))]
    public static partial EventDto ToEventDto(this Event e);

    [MapperIgnoreSource(nameof(EventValue.Event))]
    public static partial EventValueDto ToValueDto(this EventValue ev);

    [MapperIgnoreSource(nameof(EventInstance.EventValue))]
    public static partial EventInstanceDto ToInstanceDto(this EventInstance ei);


    [MapperIgnoreTarget(nameof(Event.User))]
    public static partial Event ToDatabaseEvent(this EventDto dto);

    [MapperIgnoreTarget(nameof(EventValue.Event))]
    public static partial EventValue ToDatabaseValue(this EventValueDto dto);

    [MapperIgnoreTarget(nameof(EventInstance.EventValue))]
    public static partial EventInstance ToDatabaseInstance(this EventInstanceDto dto);

}